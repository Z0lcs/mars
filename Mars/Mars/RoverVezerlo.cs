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

                double hatralevoPerc = (maxOra * 60) - ido.ElteltPerc;
                var asvanyok = GetAsvanyok();

                var elerhetoAsvanyok = asvanyok.Where(a =>
                {
                    int becsultTavAktol = Heurisztika(rover.Pozicio, a);
                    double minTavIdo = ((becsultTavAktol + tavolsagokBazistol[a.X, a.Y]) * 15.0) + 30.0;
                    return minTavIdo <= hatralevoPerc;
                }).ToList();

                int tavBazisig = tavolsagokBazistol[rover.Pozicio.X, rover.Pozicio.Y];

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
                double biztonsagiAkku = Math.Max(12.0, deficit + 10.0);

                double szuksegesIdo = (simPerc - ido.ElteltPerc) + 60.0;
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
                        (hatralevoPerc > szuksegesIdo + 120) &&
                        (rover.Akkumulator >= biztonsagiAkku + 20) &&
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
                        celpont = ValasztCelpont(rover.Pozicio, elerhetoAsvanyok);
                        aktualisStatusz = "Haladás";
                    }
                    else
                    {
                        celpont = terkep.KezdoPont;
                        aktualisStatusz = "VÉSZ-HAZATÉRÉS";
                    }

                    int valosTavolsag = Heurisztika(rover.Pozicio, celpont);
                    sebessegMod = ValasztSebesseg(rover, celpont, nappal, menekulesAktiv, valosTavolsag, biztonsagiAkku);

                    double lepes = MozgasVegrehajtas(celpont, sebessegMod);

                    if (lepes > 0) osszesTavolsag += lepes;

                    rover.FrissitEnergia(rover.SzamolFogyasztas(sebessegMod, nappal, false));
                    ido.IdoUgras(30);

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

        // --- CÉLPONT ÉS MOZGÁS (A* Logika) ---

        private Point ValasztCelpont(Point akt, List<Point> elerhetoAsvanyok)
        {
            if (!elerhetoAsvanyok.Any()) return terkep.KezdoPont;

            // --- TUNING PARAMÉTEREK (Ezeket változtasd tesztenként!) ---
            double forduloPont = 0.55;       // 0.0 - 1.0 (Pl: 0.55 = az idő 55%-ánál indul hazafelé)
            double bazisEro = 35.0;          // Milyen erősen lökje ki / húzza be a bázis (Alap: 35.0)
            double lustasag = 2;           // Távolság büntetése. Kisebb szám = bátrabban ugrik messzire (Alap: 2.0)
            double surusegJutalom = 8.0;    // Mennyire vonzzák a nagy kupacok (Alap: 6.0)
            int porszivoSugar = 2;           // Milyen messziről szedjen fel azonnal mindent (Alap: 2)
                                             // -----------------------------------------------------------

            double progress = (double)ido.ElteltPerc / (maxOra * 60.0);
            double bazisSuly = (progress - forduloPont) * bazisEro;

            var vizsgalandoLista = elerhetoAsvanyok;
            if (progress > 0.10)
            {
                var nagyonKozeli = elerhetoAsvanyok.Where(a => Heurisztika(akt, a) <= porszivoSugar).ToList();
                if (nagyonKozeli.Any()) vizsgalandoLista = nagyonKozeli;
            }

            var topJeloltek = vizsgalandoLista.OrderBy(a =>
            {
                double tavAktol = Heurisztika(akt, a);
                double tavBazistol = tavolsagokBazistol[a.X, a.Y];
                int suruseg = elerhetoAsvanyok.Count(m => Heurisztika(m, a) <= 4);

                return (tavAktol * tavAktol * lustasag) - (suruseg * surusegJutalom) + (tavBazistol * bazisSuly);
            })
            .Take(5)
            .ToList();

            Point legjobbCelpont = topJeloltek.First();
            int minKoltseg = int.MaxValue;

            foreach (var jelolt in topJeloltek)
            {
                var utvonal = AStarUtvonal(akt, jelolt);

                if (utvonal.Count > 0 && utvonal.Count < minKoltseg)
                {
                    minKoltseg = utvonal.Count;
                    legjobbCelpont = jelolt;
                }
            }

            return legjobbCelpont;
        }

        private int ValasztSebesseg(Rover r, Point cel, bool nappal, bool vesz, int valosTavolsag, double biztonsagiAkku)
        {
            int maxSeb = 1;
            double aktualisOra = (ido.ElteltPerc / 60.0) % 24;

            if (vesz)
            {
                if (nappal)
                {
                    if (r.Akkumulator > biztonsagiAkku + 10) maxSeb = 3;
                    else if (r.Akkumulator > biztonsagiAkku + 5) maxSeb = 2;
                    else maxSeb = 1;
                }
                else
                {
                    if (r.Akkumulator > biztonsagiAkku + 20) maxSeb = 3;
                    else if (r.Akkumulator > biztonsagiAkku + 8) maxSeb = 2;
                    else maxSeb = 1;
                }
            }
            else
            {
                if (nappal)
                {
                    if (aktualisOra >= 13.0 && aktualisOra < 16.0 && r.Akkumulator < 75)
                    {
                        if (r.Akkumulator >= 50) maxSeb = 2;
                        else maxSeb = 1;
                    }
                    else
                    {
                        if (r.Akkumulator >= 25) maxSeb = 3;
                        else if (r.Akkumulator >= 12) maxSeb = 2;
                        else maxSeb = 1;
                    }
                }
                else
                {
                    if (r.Akkumulator >= 80) maxSeb = 2;
                    else maxSeb = 1; 
                }
            }

            return Math.Min(valosTavolsag, maxSeb);
        }

        private double MozgasVegrehajtas(Point cel, int seb)
        {
            if (rover.Pozicio == cel || seb == 0) return 0;

            var utvonal = AStarUtvonal(rover.Pozicio, cel);

            if (utvonal.Count == 0) return 0;

            double megtett = 0;
            int lepesSzam = Math.Min(seb, utvonal.Count);

            for (int i = 0; i < lepesSzam; i++)
            {
                rover.Pozicio = utvonal[i];
                megtett++;
                lepesSzamlalo++;

                kijelzo.LogLepes(lepesSzamlalo, rover.Pozicio);
            }

            return megtett;
        }

        // --- A* ALGORITMUS ---

        private List<Point> AStarUtvonal(Point start, Point cel)
        {
            var openSet = new PriorityQueue<Point, int>();
            var cameFrom = new Dictionary<Point, Point>();

            int[,] gScore = new int[50, 50];
            for (int i = 0; i < 50; i++)
                for (int j = 0; j < 50; j++)
                    gScore[i, j] = int.MaxValue;

            gScore[start.X, start.Y] = 0;
            openSet.Enqueue(start, Heurisztika(start, cel));

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            while (openSet.Count > 0)
            {
                Point curr = openSet.Dequeue();

                if (curr == cel)
                {
                    return RekonstrualoUtvonal(cameFrom, curr);
                }

                for (int i = 0; i < 8; i++)
                {
                    int nx = curr.X + dx[i];
                    int ny = curr.Y + dy[i];

                    if (nx >= 0 && nx < 50 && ny >= 0 && ny < 50 && !akadalyMatrix[nx, ny])
                    {
                        Point szomszed = new Point(nx, ny);
                        int tentative_gScore = gScore[curr.X, curr.Y] + 1;

                        if (tentative_gScore < gScore[nx, ny])
                        {
                            cameFrom[szomszed] = curr;
                            gScore[nx, ny] = tentative_gScore;

                            int fScore = tentative_gScore + Heurisztika(szomszed, cel);
                            openSet.Enqueue(szomszed, fScore);
                        }
                    }
                }
            }

            return new List<Point>();
        }

        private int Heurisztika(Point a, Point b)
        {
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        private List<Point> RekonstrualoUtvonal(Dictionary<Point, Point> cameFrom, Point current)
        {
            var ut = new List<Point>();
            while (cameFrom.ContainsKey(current))
            {
                ut.Add(current);
                current = cameFrom[current];
            }
            ut.Reverse();
            return ut;
        }

        private List<Point> GetAsvanyok() => terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();
    }
}