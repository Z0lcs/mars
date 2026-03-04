using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Vadász_Mars_Dénes
{
    public class RoverVezerlo
    {
        private MarsMap terkep;
        private Rover rover;
        private IdoKezelo ido;
        private Megjelenito kijelzo;

        private int maxOra;
        private double osszesTavolsag = 0;
        private int taktus = 0;
        private bool menekulesAktiv = false;
        private int lepesSzamlalo = 0;

        private int kezdetiVizjeg;
        private int kezdetiArany;
        private int kezdetiRitka;
        private int kezdetiOsszes;

        public RoverVezerlo(MarsMap terkep, int maxOra, string logPath, bool konzolraIr = true)
        {
            this.terkep = terkep;
            this.maxOra = maxOra;
            this.rover = new Rover { Pozicio = terkep.KezdoPont };
            this.ido = new IdoKezelo(maxOra);

            this.kezdetiVizjeg = terkep.Vizjeg.Count;
            this.kezdetiArany = terkep.RitkaArany.Count;
            this.kezdetiRitka = terkep.RitkaAsvany.Count;
            this.kezdetiOsszes = kezdetiVizjeg + kezdetiArany + kezdetiRitka;

            this.kijelzo = new Megjelenito(logPath, maxOra, konzolraIr);
            File.WriteAllText(logPath, "Taktus;Ido;Start_Poz;Cel_Poz;Akku;Sebesseg;Ossz_Tav;Asvanyok;Statusz;Napszak\n");
        }

        public void SzimulacioInditasa()
        {
            kijelzo.ElsoSor();

            while (ido.FuthatMegAProgram && rover.Akkumulator > 0)
            {
                taktus++;
                Point induloPoz = rover.Pozicio;
                bool nappal = ido.NappalVanE();
                string aktualisStatusz = "Várakozás";
                int sebessegMod = 1;
                Point celpont = terkep.KezdoPont;

                var asvanyok = GetAsvanyok();

                // --- VÉSZHELYZET VIZSGÁLATA ---
                int tavBazisig = Tavolsag(rover.Pozicio, terkep.KezdoPont);
                double hatralevoPerc = (maxOra * 60) - ido.ElteltPerc;

                if (!menekulesAktiv)
                {
                    //bool kellMenekulni = ((tavBazisig * 12.0) >= (hatralevoPerc - 60)) || (rover.Akkumulator < 30);
                    bool kellMenekulni = ((tavBazisig * 11.0) >= (hatralevoPerc - 30)) || (rover.Akkumulator < 25);
                    if (kellMenekulni || !asvanyok.Any())
                    {
                        menekulesAktiv = true;
                    }
                }

                if (rover.Pozicio == terkep.KezdoPont && menekulesAktiv && taktus > 1) break;

                // --- DÖNTÉSHOZATAL ---
                if (asvanyok.Any(a => a.X == rover.Pozicio.X && a.Y == rover.Pozicio.Y) && !menekulesAktiv)
                {
                    aktualisStatusz = "Bányászat";
                    rover.OsszegyujtottAsvany++;
                    terkep.Vizjeg.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaArany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaAsvany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);

                    rover.FrissitEnergia(rover.SzamolFogyasztas(0, nappal, true));
                    ido.IdoUgras(30);

                    kijelzo.LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, 0, aktualisStatusz, nappal, rover, osszesTavolsag);
                }
                else
                {
                    if (!menekulesAktiv)
                    {
                        celpont = ValasztCelpont(rover.Pozicio, asvanyok);
                        aktualisStatusz = "Haladás";
                    }
                    else
                    {
                        celpont = terkep.KezdoPont;
                        aktualisStatusz = "VÉSZ-HAZATÉRÉS";
                    }

                    sebessegMod = ValasztSebesseg(rover, celpont, nappal, menekulesAktiv);
                    double lepes = MozgasVegrehajtas(celpont, sebessegMod);

                    if (lepes > 0) osszesTavolsag += lepes;

                    rover.FrissitEnergia(rover.SzamolFogyasztas(sebessegMod, nappal, false));
                    ido.IdoUgras(30);

                    int kiirtSebesseg = (lepes > 0) ? (int)lepes : sebessegMod;
                    kijelzo.LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, kiirtSebesseg, aktualisStatusz, nappal, rover, osszesTavolsag);
                }

                if (rover.Akkumulator <= 0) break;
            }
            int maradekVizjeg = terkep.Vizjeg.Count;
            int maradekArany = terkep.RitkaArany.Count;
            int maradekRitka = terkep.RitkaAsvany.Count;

            // 2. A különbség adja meg a begyűjtött mennyiséget fajtánként
            int gyujtottVizjeg = kezdetiVizjeg - maradekVizjeg;
            int gyujtottArany = kezdetiArany - maradekArany;
            int gyujtottRitka = kezdetiRitka - maradekRitka;

            // 3. Eredmény kiírása a konzolra
            kijelzo.KiirEredmeny(rover, terkep, osszesTavolsag);

            // 4. A 3. fájl (Összegzés) legenerálása
            kijelzo.LogOsszegzes(ido.ElteltPerc, (ido.MaxOra * 60), osszesTavolsag,
                                 gyujtottVizjeg, kezdetiVizjeg,
                                 gyujtottArany, kezdetiArany,
                                 gyujtottRitka, kezdetiRitka,
                                 rover.OsszegyujtottAsvany, kezdetiOsszes);
            kijelzo.KiirEredmeny(rover, terkep, osszesTavolsag);
        }

        // --- Belső logikai segédfüggvények ---
            
        private Point ValasztCelpont(Point akt, List<Point> asvanyok)
        {
            var szomszed = asvanyok.FirstOrDefault(a => Tavolsag(a, akt) <= 1);
            if (szomszed != new Point(0, 0)) return szomszed;

            return asvanyok.OrderBy(a =>
            {
                double tav = Tavolsag(a, akt);
                int suruseg = asvanyok.Count(m => Tavolsag(m, a) <= 2);
                return tav - (suruseg * 0.725);
            }).First();
        }

        private int ValasztSebesseg(Rover r, Point cel, bool nappal, bool vesz)
        {
            int maxSeb = 1;

            if (vesz)
            {
                if (r.Akkumulator > 35) maxSeb = 3;
                else if (r.Akkumulator > 15) maxSeb = 2;
                else maxSeb = 1;
            }
            else
            {
                if (nappal && r.Akkumulator > 30) maxSeb = 3;
                else if (r.Akkumulator > 15) maxSeb = 2;
                else maxSeb = 1;
            }

            return Math.Min(Tavolsag(r.Pozicio, cel), maxSeb);
        }

        private double MozgasVegrehajtas(Point cel, int seb)
        {
            double megtett = 0;
            for (int i = 0; i < seb; i++)
            {
                if (rover.Pozicio == cel) break;
                Point kov = KeresLegjobbSzomszed(rover.Pozicio, cel);
                if (kov != rover.Pozicio)
                {
                    rover.Pozicio = kov;
                    megtett++;

                    lepesSzamlalo++;
                    kijelzo.LogLepes(lepesSzamlalo, rover.Pozicio);
                }
            }
            return megtett;
        }

        private int Tavolsag(Point p1, Point p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));

        private Point KeresLegjobbSzomszed(Point akt, Point cel)
        {
            Point legjobb = akt;
            double min = double.MaxValue;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Point v = new Point(akt.X + dx, akt.Y + dy);
                    if (v.X >= 0 && v.X < 50 && v.Y >= 0 && v.Y < 50 && !terkep.Akadalyok.Any(a => a.X == v.X && a.Y == v.Y))
                    {
                        double t = Tavolsag(v, cel);
                        if (t < min) { min = t; legjobb = v; }
                    }
                }
            return legjobb;
        }

        private List<Point> GetAsvanyok() => terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();
    }
}