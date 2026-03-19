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

        private bool[,] akadalyMatrix;
        private int[,] tavolsagokBazistol;

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

            akadalyMatrix = new bool[50, 50];
            foreach (var a in terkep.Akadalyok)
            {
                if (a.X >= 0 && a.X < 50 && a.Y >= 0 && a.Y < 50)
                {
                    akadalyMatrix[a.X, a.Y] = true;
                }
            }

            tavolsagokBazistol = BfsMindenTavolsag(terkep.KezdoPont);

            this.kijelzo = new Megjelenito(logPath, maxOra, konzolraIr);
            File.WriteAllText(logPath, "Taktus;Ido;Start_Poz;Cel_Poz;Akku;Sebesseg;Ossz_Tav;Asvanyok;Statusz;Napszak\n");
        }

        private int[,] BfsMindenTavolsag(Point start)
        {
            int[,] dist = new int[50, 50];
            for (int i = 0; i < 50; i++)
                for (int j = 0; j < 50; j++)
                    dist[i, j] = 9999;

            Queue<(int X, int Y, int D)> q = new Queue<(int, int, int)>();
            q.Enqueue((start.X, start.Y, 0));
            dist[start.X, start.Y] = 0;

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            while (q.Count > 0)
            {
                var curr = q.Dequeue();

                for (int i = 0; i < 8; i++)
                {
                    int nx = curr.X + dx[i];
                    int ny = curr.Y + dy[i];

                    if (nx >= 0 && nx < 50 && ny >= 0 && ny < 50)
                    {
                        if (!akadalyMatrix[nx, ny] && dist[nx, ny] == 9999)
                        {
                            dist[nx, ny] = curr.D + 1;
                            q.Enqueue((nx, ny, curr.D + 1));
                        }
                    }
                }
            }
            return dist;
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

                int[,] tavolsagokAktol = BfsMindenTavolsag(rover.Pozicio);
                double hatralevoPerc = (maxOra * 60) - ido.ElteltPerc;

                var asvanyok = GetAsvanyok();

                var elerhetoAsvanyok = asvanyok.Where(a =>
                {
                    if (tavolsagokAktol[a.X, a.Y] >= 9999) return false;
                    double minTavIdo = ((tavolsagokAktol[a.X, a.Y] + tavolsagokBazistol[a.X, a.Y]) * 15.0) + 30.0;
                    return minTavIdo <= hatralevoPerc;
                }).ToList();

                int tavBazisig = tavolsagokAktol[terkep.KezdoPont.X, terkep.KezdoPont.Y];

                double simAkkuAkt = rover.Akkumulator;
                double legkisebbSzint = rover.Akkumulator;
                int simPerc = ido.ElteltPerc;
                int hatralevoTav = tavBazisig;

                while (hatralevoTav > 0)
                {
                    double simOra = (simPerc / 60.0) % 24;
                    bool simNappal = simOra < 16.0;

                    int maxSeb = 1;
                    if (simNappal)
                    {
                        if (simOra >= 13.0 && simOra < 16.0 && simAkkuAkt < 90) maxSeb = 1;
                        else if (simAkkuAkt >= 20) maxSeb = 3;
                        else if (simAkkuAkt >= 8) maxSeb = 2;
                    }
                    else
                    {
                        if (simAkkuAkt >= 60) maxSeb = 2;
                    }

                    int lepes = Math.Min(hatralevoTav, maxSeb);

                    double fogy = 2 * Math.Pow(lepes, 2);
                    if (simNappal) fogy -= 10;

                    simAkkuAkt -= fogy;
                    if (simAkkuAkt > 100) simAkkuAkt = 100;
                    if (simAkkuAkt < legkisebbSzint) legkisebbSzint = simAkkuAkt;

                    simPerc += 30;
                    hatralevoTav -= lepes;
                }

                double deficit = rover.Akkumulator - legkisebbSzint;
                //double biztonsagiAkku = Math.Max(12.0, deficit + 10.0);
                double biztonsagiAkku = Math.Max(5.0, deficit + 2.0);

                // 60 perc helyett csak 0 vagy maximum 30 perc (1 lépés) puffert hagyunk
                double szuksegesIdo = (simPerc - ido.ElteltPerc) + 0.0;
                bool idoFogy = (hatralevoPerc <= szuksegesIdo);

                if (!menekulesAktiv)
                {
                    if (idoFogy || (rover.Akkumulator < biztonsagiAkku) || !elerhetoAsvanyok.Any())
                    {
                        menekulesAktiv = true;
                    }
                }
                else
                {
                    if (rover.Pozicio != terkep.KezdoPont &&
                        nappal &&
                        (hatralevoPerc > szuksegesIdo + 30) && //90 volt
                        (rover.Akkumulator >= biztonsagiAkku + 5) && // 10 volt
                        elerhetoAsvanyok.Any())
                    {
                        menekulesAktiv = false;
                    }
                }

                if (rover.Pozicio == terkep.KezdoPont && menekulesAktiv && taktus > 1)
                {
                    if (elerhetoAsvanyok.Any())
                    {
                        var cel = elerhetoAsvanyok.OrderBy(a => tavolsagokBazistol[a.X, a.Y]).First();
                        int tavCelhoz = tavolsagokBazistol[cel.X, cel.Y];
                        double szuksegesAktivAkku = (tavCelhoz * 2.0 * 2) + 15.0;

                        if (rover.Akkumulator < szuksegesAktivAkku && rover.Akkumulator < 95)
                        {
                            if (rover.Akkumulator <= 2 && !nappal) break;

                            aktualisStatusz = "Töltés";
                            rover.FrissitEnergia(rover.SzamolFogyasztas(0, nappal, false));
                            ido.IdoUgras(30);
                            kijelzo.LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, 0, aktualisStatusz, nappal, rover, osszesTavolsag);
                            continue;
                        }
                        else
                        {
                            menekulesAktiv = false;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                bool tudBanyaszniVeszhelyzetben = menekulesAktiv &&
                                                  (rover.Akkumulator > biztonsagiAkku + 2.0) &&
                                                  (hatralevoPerc > szuksegesIdo + 30.0);

                // --- DÖNTÉSHOZATAL ---
                if (asvanyok.Any(a => a.X == rover.Pozicio.X && a.Y == rover.Pozicio.Y) && (!menekulesAktiv || tudBanyaszniVeszhelyzetben))
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
                        celpont = ValasztCelpont(rover.Pozicio, elerhetoAsvanyok, tavolsagokAktol);
                        aktualisStatusz = "Haladás";
                    }
                    else
                    {
                        celpont = terkep.KezdoPont;
                        aktualisStatusz = "VÉSZ-HAZATÉRÉS";
                    }

                    int valosTavolsag = tavolsagokAktol[celpont.X, celpont.Y];
                    sebessegMod = ValasztSebesseg(rover, celpont, nappal, menekulesAktiv, valosTavolsag, biztonsagiAkku);

                    double lepes = MozgasVegrehajtas(celpont, sebessegMod);

                    if (lepes > 0) osszesTavolsag += lepes;

                    rover.FrissitEnergia(rover.SzamolFogyasztas(sebessegMod, nappal, false));
                    ido.IdoUgras(30);

                    if (celpont == terkep.KezdoPont && rover.Pozicio == terkep.KezdoPont)
                    {
                        aktualisStatusz = "HAZAÉRT / Bázison";
                    }

                    int kiirtSebesseg = (lepes > 0) ? (int)lepes : sebessegMod;
                    kijelzo.LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, kiirtSebesseg, aktualisStatusz, nappal, rover, osszesTavolsag);
                }

                if (rover.Akkumulator <= 0) break;
            }

            kijelzo.LogOsszegzes(ido.ElteltPerc, (ido.MaxOra * 60), osszesTavolsag,
                                 kezdetiVizjeg - terkep.Vizjeg.Count, kezdetiVizjeg,
                                 kezdetiArany - terkep.RitkaArany.Count, kezdetiArany,
                                 kezdetiRitka - terkep.RitkaAsvany.Count, kezdetiRitka,
                                 rover.OsszegyujtottAsvany, kezdetiOsszes);
            kijelzo.KiirEredmeny(rover, terkep, osszesTavolsag);
        }

        // --- CÉLPONT ÉS MOZGÁS ---

        private Point ValasztCelpont(Point akt, List<Point> elerhetoAsvanyok, int[,] tavMatrix)
        {
            if (!elerhetoAsvanyok.Any()) return terkep.KezdoPont;

            // --- TUNING PARAMÉTEREK (Ideális értékek teszteléséhez) ---

            // 1. A Porszívó: Milyen távolságon belül szedjen fel azonnal mindent, gondolkodás nélkül? (Alap: 3)
            int porszivoSugar = 3;

            // 2. Távolság büntetése: Kisebb szám = bátrabban indul el messzire az elején. (Alap: 5.0)
            double tavolsagBuntetes = 5.0;

            // 3. Sűrűség vizsgálata: Mekkora körzetben számolja a szomszédos ásványokat egy célpont körül? (Alap: 3)
            int surusegSugar = 3;

            // 4. Sűrűség jutalma: Nagyobb szám = jobban vonzzák a nagy kupacok, akár távolabb is. (Alap: 5.0)
            double surusegJutalom = 5.0;

            // 5. Hazahúzás ereje: Milyen erősen vonzza a bázis a szimuláció vége felé? (Alap: 25.0)
            double hazaHuzasSzorzo = 25.0;

            // 6. Hazahúzás görbéje: Magasabb szám (pl. 4) esetén sokáig kint marad, és csak az utolsó pillanatban kezd el durván hazahúzni. (Alap: 3.0)
            double hazaHuzasHatvany = 3.0;
            // -----------------------------------------------------------

            var kornyezet = elerhetoAsvanyok.Where(a => tavMatrix[a.X, a.Y] <= porszivoSugar).ToList();
            if (kornyezet.Any())
            {
                return kornyezet.OrderBy(a => tavMatrix[a.X, a.Y]).First();
            }

            double progress = (double)ido.ElteltPerc / (maxOra * 60.0);
            double hazaHuzas = Math.Pow(progress, hazaHuzasHatvany) * hazaHuzasSzorzo;

            return elerhetoAsvanyok.OrderBy(a =>
            {
                double tavAktol = tavMatrix[a.X, a.Y];
                double tavBazistol = tavolsagokBazistol[a.X, a.Y];
                int suruseg = elerhetoAsvanyok.Count(m => Tavolsag(m, a) <= surusegSugar);

                return (tavAktol * tavolsagBuntetes) - (suruseg * surusegJutalom) + (tavBazistol * hazaHuzas);
            }).First();
        }

        private int ValasztSebesseg(Rover r, Point cel, bool nappal, bool vesz, int valosTavolsag, double biztonsagiAkku)
        {
            int maxSeb = 1;
            double aktualisOra = (ido.ElteltPerc / 60.0) % 24;

            if (vesz)
            {
                if (nappal)
                {
                    if (r.Akkumulator > biztonsagiAkku + 15) maxSeb = 3;
                    else if (r.Akkumulator > biztonsagiAkku + 5) maxSeb = 2;
                    else maxSeb = 1;
                }
                else
                {
                    if (r.Akkumulator > biztonsagiAkku + 10) maxSeb = 2;
                    else maxSeb = 1;
                }
            }
            else
            {
                if (nappal)
                {
                    if (aktualisOra >= 13.0 && aktualisOra < 16.0 && r.Akkumulator < 90) maxSeb = 1;
                    else if (r.Akkumulator >= 20) maxSeb = 3;
                    else if (r.Akkumulator >= 8) maxSeb = 2;
                }
                else
                {
                    if (r.Akkumulator >= 60) maxSeb = 3;
                    else if (r.Akkumulator >= 30) maxSeb = 2;
                    else maxSeb = 1;
                }
            }

            return Math.Min(valosTavolsag, maxSeb);
        }

        private double MozgasVegrehajtas(Point cel, int seb)
        {
            double megtett = 0;
            if (rover.Pozicio == cel) return 0;

            int[,] tavolsagCeltol = BfsMindenTavolsag(cel);

            for (int i = 0; i < seb; i++)
            {
                if (rover.Pozicio == cel) break;

                Point kov = KeresLegjobbSzomszed(rover.Pozicio, cel, tavolsagCeltol);
                if (kov != rover.Pozicio)
                {
                    rover.Pozicio = kov;
                    megtett++;
                    lepesSzamlalo++;
                    kijelzo.LogLepes(lepesSzamlalo, rover.Pozicio);
                }
                else break;
            }
            return megtett;
        }

        private int Tavolsag(Point p1, Point p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));

        private Point KeresLegjobbSzomszed(Point akt, Point cel, int[,] tavolsagCeltol)
        {
            Point legjobb = akt;
            int minTav = int.MaxValue;
            double minEuklideszi = double.MaxValue;

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int nx = akt.X + dx[i];
                int ny = akt.Y + dy[i];

                if (nx >= 0 && nx < 50 && ny >= 0 && ny < 50 && !akadalyMatrix[nx, ny])
                {
                    int t = tavolsagCeltol[nx, ny];
                    double euklideszi = Math.Sqrt(Math.Pow(nx - cel.X, 2) + Math.Pow(ny - cel.Y, 2));

                    if (t < minTav || (t == minTav && euklideszi < minEuklideszi))
                    {
                        minTav = t;
                        minEuklideszi = euklideszi;
                        legjobb = new Point(nx, ny);
                    }
                }
            }
            return legjobb;
        }

        private List<Point> GetAsvanyok() => terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();
    }
}