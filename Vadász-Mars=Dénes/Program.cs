using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Vadász_Mars_Dénes
{
    internal class Program
    {
        // Globális beállítások
        static int MaxOra = 72; // A 67 ásványhoz 72 óra kell
        static string LogPath = "rover_log.csv";

        static void Main(string[] args)
        {
            // --- INICIALIZÁLÁS ---
            MarsMap terkep = new MarsMap();
            try { terkep.LoadFromFile("mars_map_50x50.csv"); }
            catch (Exception ex) { Console.WriteLine($"Hiba: {ex.Message}"); return; }

            Rover rover = new Rover();
            rover.Pozicio = terkep.KezdoPont;
            IdoKezelo ido = new IdoKezelo(MaxOra);
            double osszesTavolsag = 0;
            int taktus = 0;

            // --- FEJLÉC KIÍRÁSA ---
            File.WriteAllText(LogPath, "Taktus;Ora;Start_Poz;Cel_Poz;Akku;Sebesseg;Ossz_Tav;Asvanyok;Statusz;Napszak\n");

            Console.Clear();
            Console.WriteLine($"\n=== MARS ROVER STRATÉGIAI SZIMULÁCIÓ ({MaxOra}h) ===\n");
            Console.WriteLine(string.Format(" {0,-6} | {1,-9} | {2,-13} | {3,-8} | {4,-3} | {5,-16} | {6}",
                "Sorsz", "Idő", "Pozíció", "Akku", "Seb", "Státusz", "Ásvány"));
            Console.WriteLine(new string('-', 90));

            // --- FŐ CIKLUS (A TE EREDETI LOGIKÁD) ---
            while (ido.FuthatMegAProgram && rover.Akkumulator > 0)
            {
                taktus++;
                Point induloPoz = rover.Pozicio;
                bool nappal = ido.NappalVanE();
                string aktualisStatusz = "Várakozás";
                int sebessegMod = 1;
                Point celpont = terkep.KezdoPont;

                var asvanyok = terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();

                int tavBazisig = Math.Max(Math.Abs(rover.Pozicio.X - terkep.KezdoPont.X), Math.Abs(rover.Pozicio.Y - terkep.KezdoPont.Y));
                double hatralevoPerc = (ido.MaxOra * 60) - ido.ElteltPerc;

                // Eredeti vészhelyzet logika
                bool vészhelyzet = (tavBazisig * 30) >= (hatralevoPerc - 60);

                // --- DÖNTÉSHOZATAL (VÁLTOZATLAN) ---
                if (asvanyok.Any(a => a.X == rover.Pozicio.X && a.Y == rover.Pozicio.Y) && !vészhelyzet)
                {
                    aktualisStatusz = "Bányászat";
                    rover.OsszegyujtottAsvany++;
                    terkep.Vizjeg.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaArany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaAsvany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);

                    rover.FrissitEnergia(rover.SzamolFogyasztas(0, nappal, true));
                    ido.IdoUgras(30);

                    // Szép kiírás
                    LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, 0, aktualisStatusz, nappal, rover, osszesTavolsag);
                }
                else if (asvanyok.Any() && !vészhelyzet)
                {
                    // Eredeti célpont választás (sűrűség alapú)
                    celpont = asvanyok.OrderBy(a => {
                        double tav = Math.Max(Math.Abs(a.X - rover.Pozicio.X), Math.Abs(a.Y - rover.Pozicio.Y));
                        int suruseg = asvanyok.Count(m => Math.Abs(m.X - a.X) <= 1 && Math.Abs(m.Y - a.Y) <= 1);
                        return tav - (suruseg * 0.725);
                    }).First();

                    aktualisStatusz = "Haladás";
                    // Eredeti sebesség logika
                    sebessegMod = (nappal && rover.Akkumulator > 40) ? 3 : 1;

                    Point kovetkezo = KeresLegjobbSzomszed(rover.Pozicio, celpont, terkep);
                    if (kovetkezo != rover.Pozicio)
                    {
                        rover.Pozicio = kovetkezo;
                        osszesTavolsag++;
                        int idoKoltseg = (sebessegMod == 3) ? 10 : 30;
                        rover.FrissitEnergia(rover.SzamolFogyasztas(sebessegMod, nappal, false));
                        ido.IdoUgras(idoKoltseg);
                    }
                    else { ido.IdoUgras(10); } // Ha elakadt

                    // Szép kiírás
                    LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, sebessegMod, aktualisStatusz, nappal, rover, osszesTavolsag);
                }
                else
                {
                    // HAZATÉRÉS
                    if (rover.Pozicio != terkep.KezdoPont)
                    {
                        aktualisStatusz = vészhelyzet ? "VÉSZ-HAZATÉRÉS" : "Hazatérés";
                        sebessegMod = (vészhelyzet && rover.Akkumulator > 50) ? 3 : 1;

                        Point kovetkezo = KeresLegjobbSzomszed(rover.Pozicio, terkep.KezdoPont, terkep);
                        rover.Pozicio = kovetkezo;
                        osszesTavolsag++;

                        int idoKoltseg = (sebessegMod == 3) ? 10 : 30;
                        rover.FrissitEnergia(rover.SzamolFogyasztas(sebessegMod, nappal, false));
                        ido.IdoUgras(idoKoltseg);

                        // Szép kiírás
                        LogEsKiir(taktus, ido, induloPoz, rover.Pozicio, sebessegMod, aktualisStatusz, nappal, rover, osszesTavolsag);
                    }
                    else
                    {
                        break; // Hazaértünk
                    }
                }

                if (rover.Akkumulator <= 0) break;
            }

            // --- ZÁRÁS ---
            KiirEredmeny(rover, terkep, osszesTavolsag);
        }

        // --- SEGÉDFÜGGVÉNYEK (Változatlan logika, csak kiszervezve) ---

        static Point KeresLegjobbSzomszed(Point jelenlegi, Point cel, MarsMap terkep)
        {
            Point legjobb = jelenlegi;
            double minTav = double.MaxValue;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Point vizsgalt = new Point(jelenlegi.X + dx, jelenlegi.Y + dy);

                    if (vizsgalt.X >= 0 && vizsgalt.X < 50 && vizsgalt.Y >= 0 && vizsgalt.Y < 50 &&
                        !terkep.Akadalyok.Any(a => a.X == vizsgalt.X && a.Y == vizsgalt.Y))
                    {
                        double tav = Math.Max(Math.Abs(vizsgalt.X - cel.X), Math.Abs(vizsgalt.Y - cel.Y));
                        if (tav < minTav)
                        {
                            minTav = tav;
                            legjobb = vizsgalt;
                        }
                    }
                }
            }
            return legjobb;
        }

        // --- SZÉP MEGJELENÍTÉS (Ez az új rész) ---

        static void LogEsKiir(int taktus, IdoKezelo ido, Point s, Point v, int seb, string stat, bool nappal, Rover r, double tav)
        {
            // 1. Oszlop: Taktus
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" #{taktus:D3}   | ");

            // 2. Oszlop: Idő
            Console.ForegroundColor = nappal ? ConsoleColor.Yellow : ConsoleColor.Blue;
            Console.Write($"[{ido.ElteltPerc / 60.0,5:F2}h] ");
            Console.ResetColor();
            Console.Write("| ");

            // 3. Oszlop: Pozíció
            Console.Write($"({s.X,2},{s.Y,2})->({v.X,2},{v.Y,2}) | ");

            // 4. Oszlop: Akku
            if (r.Akkumulator < 25) Console.ForegroundColor = ConsoleColor.Red;
            else Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{Math.Round(r.Akkumulator),3}%");
            Console.ResetColor();
            Console.Write("     | ");

            // 5. Oszlop: Sebesség
            Console.Write($"{seb}   | ");

            // 6. Oszlop: Státusz
            if (stat == "Bányászat") Console.ForegroundColor = ConsoleColor.Cyan;
            else if (stat.Contains("VÉSZ")) Console.ForegroundColor = ConsoleColor.Magenta;
            else Console.ForegroundColor = ConsoleColor.White;

            Console.Write(string.Format("{0,-16}", stat));
            Console.ResetColor();
            Console.Write("| ");

            // 7. Oszlop: Ásványok
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{r.OsszegyujtottAsvany,2} db");
            Console.ResetColor();

            // Fájlba írás
            string logSor = $"{taktus};{ido.ElteltPerc / 60.0:F2};({s.X},{s.Y});({v.X},{v.Y});{Math.Round(r.Akkumulator)};{seb};{tav};{r.OsszegyujtottAsvany};{stat};{(nappal ? "Nappal" : "Ejszaka")}\n";
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