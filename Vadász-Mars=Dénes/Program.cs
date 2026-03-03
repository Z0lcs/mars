using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Vadász_Mars_Dénes
{
    internal class Program
    {
        static int MaxOra = 72;
        static string LogPath = "rover_log.csv";
        static string PathLogPath = "rover_path.csv";
        static bool menekulesAktiv = false;
        static int lepesSzamlalo = 0;

        static void Main(string[] args)
        {
            MarsMap terkep = new MarsMap();
            try { terkep.LoadFromFile("mars_map_50x50.csv"); }
            catch (Exception ex) { Console.WriteLine($"Hiba: {ex.Message}"); return; }

            Rover rover = new Rover();
            rover.Pozicio = terkep.KezdoPont;
            IdoKezelo ido = new IdoKezelo(MaxOra);
            double osszesTavolsag = 0;
            int taktus = 0;

            File.WriteAllText(LogPath, "Taktus;Ido;Start_Poz;Cel_Poz;Akku;Sebesseg;Ossz_Tav;Asvanyok;Statusz;Napszak\n");
            File.WriteAllText(PathLogPath, "Lépés szám;X;Y\n");

            Console.Clear();
            Console.WriteLine($"\n=== MARS ROVER: VÉGLEGES VERZIÓ ({MaxOra}h) ===\n");
            Console.WriteLine(string.Format(" {0,-6} | {1,-9} | {2,-13} | {3,-8} | {4,-3} | {5,-16} | {6}",
                "Sorsz", "Idő", "Pozíció", "Akku", "Seb", "Státusz", "Ásvány"));
            Console.WriteLine(new string('-', 90));

            while (ido.FuthatMegAProgram && rover.Akkumulator > 0)
            {
                taktus++;
                Point induloPoz = rover.Pozicio;
                bool nappal = ido.NappalVanE();
                string aktualisStatusz = "Várakozás";
                int sebessegMod = 1;
                Point celpont = terkep.KezdoPont;

                var asvanyok = GetAsvanyok(terkep);
                int tavBazisig = Tavolsag(rover.Pozicio, terkep.KezdoPont);
                double hatralevoPerc = (ido.MaxOra * 60) - ido.ElteltPerc;

                if (!menekulesAktiv)
                {
                    bool kellMenekulni = ((tavBazisig * 12.0) >= (hatralevoPerc - 60)) || (rover.Akkumulator < 30);
                    if (kellMenekulni || !asvanyok.Any())
                    {
                        menekulesAktiv = true;
                    }
                }

                if (rover.Pozicio == terkep.KezdoPont && menekulesAktiv && taktus > 1) break;

                if (asvanyok.Any(a => a.X == rover.Pozicio.X && a.Y == rover.Pozicio.Y) && !menekulesAktiv)
                {
                    aktualisStatusz = "Bányászat";
                    rover.OsszegyujtottAsvany++;
                    terkep.Vizjeg.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaArany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaAsvany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);

                    rover.FrissitEnergia(rover.SzamolFogyasztas(0, nappal, true));
                    ido.IdoUgras(30);

                    LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, 0, aktualisStatusz, nappal, rover, osszesTavolsag);
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

                    double lepes = MozgasVegrehajtas(rover, celpont, sebessegMod, terkep);

                    if (lepes > 0) osszesTavolsag += lepes;

                    rover.FrissitEnergia(rover.SzamolFogyasztas(sebessegMod, nappal, false));
                    ido.IdoUgras(30);

                    int kiirtSebesseg = (lepes > 0) ? (int)lepes : sebessegMod;

                    LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, kiirtSebesseg, aktualisStatusz, nappal, rover, osszesTavolsag);
                }

                if (rover.Akkumulator <= 0) break;
            }

            KiirEredmeny(rover, terkep, osszesTavolsag);
        }

        static Point ValasztCelpont(Point akt, List<Point> asvanyok)
        {
            var szomszed = asvanyok.FirstOrDefault(a => Tavolsag(a, akt) <= 1);
            if (szomszed != new Point(0, 0)) return szomszed;

            return asvanyok.OrderBy(a => {
                double tav = Tavolsag(a, akt);
                int suruseg = asvanyok.Count(m => Tavolsag(m, a) <= 2);
                return tav - (suruseg * 0.725);
            }).First();
        }
        static int ValasztSebesseg(Rover r, Point cel, bool nappal, bool vesz)
        {
            int maxSeb = 1;

            if (vesz)
            {
                maxSeb = (r.Akkumulator > 35) ? 3 : 1;
            }
            else
            {
                if (nappal && r.Akkumulator > 40) maxSeb = 3;
                else maxSeb = 1;
            }

            int tav = Tavolsag(r.Pozicio, cel);
            return Math.Min(tav, maxSeb);
        }

        static double MozgasVegrehajtas(Rover r, Point cel, int seb, MarsMap m)
        {
            double megtett = 0;
            for (int i = 0; i < seb; i++)
            {
                if (r.Pozicio == cel) break;
                Point kov = KeresLegjobbSzomszed(r.Pozicio, cel, m);
                if (kov != r.Pozicio)
                {
                    r.Pozicio = kov;
                    megtett++;

                    lepesSzamlalo++;
                    string pathSor = $"{lepesSzamlalo};{r.Pozicio.X};{r.Pozicio.Y}\n";
                    File.AppendAllText(PathLogPath, pathSor);
                }
            }
            return megtett;
        }
        static int Tavolsag(Point p1, Point p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));
        static Point KeresLegjobbSzomszed(Point akt, Point cel, MarsMap m)
        {
            Point legjobb = akt;
            double min = double.MaxValue;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Point v = new Point(akt.X + dx, akt.Y + dy);
                    if (v.X >= 0 && v.X < 50 && v.Y >= 0 && v.Y < 50 && !m.Akadalyok.Any(a => a.X == v.X && a.Y == v.Y))
                    {
                        double t = Tavolsag(v, cel);
                        if (t < min) { min = t; legjobb = v; }
                    }
                }
            return legjobb;
        }

        static List<Point> GetAsvanyok(MarsMap m) => m.Vizjeg.Concat(m.RitkaArany).Concat(m.RitkaAsvany).ToList();
        static void LogEsKiir(int taktus, IdoKezelo ido, Point s, Point v, int seb, string stat, bool nappal, Rover r, double tav)
        {
            int ora = ido.ElteltPerc / 60;
            int perc = ido.ElteltPerc % 60;
            string idoSzoveg = $"{ora:D2}:{perc:D2}";

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" #{taktus:D3}   | ");

            Console.ForegroundColor = nappal ? ConsoleColor.Yellow : ConsoleColor.Blue;
            Console.Write($"[{idoSzoveg}] ");
            Console.ResetColor();
            Console.Write("| ");

            Console.Write($"({s.X,2},{s.Y,2})->({v.X,2},{v.Y,2}) | ");

            if (r.Akkumulator < 25) Console.ForegroundColor = ConsoleColor.Red;
            else Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{Math.Round(r.Akkumulator),3}%");
            Console.ResetColor();
            Console.Write("     | ");

            Console.Write($"{seb}   | ");

            if (stat == "Bányászat") Console.ForegroundColor = ConsoleColor.Cyan;
            else if (stat.Contains("VÉSZ")) Console.ForegroundColor = ConsoleColor.Magenta;
            else Console.ForegroundColor = ConsoleColor.White;

            Console.Write(string.Format("{0,-16}", stat));
            Console.ResetColor();
            Console.Write("| ");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{r.OsszegyujtottAsvany,2} db");
            Console.ResetColor();

            string logSor = $"{taktus};{idoSzoveg};({s.X},{s.Y});({v.X},{v.Y});{Math.Round(r.Akkumulator)};{seb};{tav};{r.OsszegyujtottAsvany};{stat};{(nappal ? "Nappal" : "Ejszaka")}\n";
            File.AppendAllText(LogPath, logSor);
        }

        static void KiirEredmeny(Rover r, MarsMap m, double tav)
        {
            Console.WriteLine(new string('-', 90));
            Console.WriteLine($"\nSZIMULÁCIÓ VÉGE");
            Console.WriteLine($"Gyűjtött ásványok: {r.OsszegyujtottAsvany} db");
            Console.WriteLine($"Megtett távolság:  {tav} egység");

            bool sikeres = (r.Pozicio == m.KezdoPont);
            Console.Write("Helyzet: ");
            if (sikeres)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("BÁZISON (Sikeres)");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("TEREPEN (Kudarc)");
            }
            Console.ResetColor();
            Console.WriteLine(new string('-', 90));
            Console.ReadLine();
        }
    }
}