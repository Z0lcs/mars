using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Vadász_Mars_Dénes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // --- Inicializálás ---
            MarsMap terkep = new MarsMap();
            try
            {
                terkep.LoadFromFile("mars_map_50x50.csv");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a térkép betöltésekor: {ex.Message}");
                return;
            }

            Rover rover = new Rover();
            rover.Pozicio = terkep.KezdoPont;

            // 48 órás küldetés
            IdoKezelo ido = new IdoKezelo(24);
            double osszesTavolsag = 0;
            string logPath = "rover_log.csv";

            // Log fájl fejléc
            File.WriteAllText(logPath, "Ido;Pozicio;Akku;Sebesseg;Tavolsag;Asvanyok;Statusz;Napszak\n");

            Console.WriteLine("=== MARS ROVER STRATÉGIAI SZIMULÁCIÓ INDUL ===");
            Console.WriteLine($"Bázis: {terkep.KezdoPont} | Időkeret: {ido.MaxOra} óra");
            Console.WriteLine("--------------------------------------------------\n");

            // --- Fő szimulációs hurok ---
            while (ido.FuthatMegAProgram)
            {
                bool nappal = ido.NappalVanE();
                string aktualisStatusz = "Várakozás";
                int sebessegMod = 1;
                Point celpont = terkep.KezdoPont;

                var asvanyok = terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();

                int tavBazisig = Math.Max(Math.Abs(rover.Pozicio.X - terkep.KezdoPont.X),
                                          Math.Abs(rover.Pozicio.Y - terkep.KezdoPont.Y));

                double hatralevoPerc = (ido.MaxOra * 60) - ido.ElteltPerc;
                bool vészhelyzet = (tavBazisig * 30) >= (hatralevoPerc - 60);

                // --- LOGIKA MÓDOSÍTÁSA ---
                if (asvanyok.Any(a => a.X == rover.Pozicio.X && a.Y == rover.Pozicio.Y) && !vészhelyzet)
                {
                    aktualisStatusz = "Bányászat";
                    rover.OsszegyujtottAsvany++;
                    terkep.Vizjeg.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaArany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaAsvany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);

                    rover.FrissitEnergia(rover.SzamolFogyasztas(0, nappal, true));
                    ido.IdoUgras(30);
                }
                else if (asvanyok.Any() && !vészhelyzet)
                {
                    celpont = asvanyok.OrderBy(a => {
                        double tav = Math.Max(Math.Abs(a.X - rover.Pozicio.X), Math.Abs(a.Y - rover.Pozicio.Y));
                        int suruseg = asvanyok.Count(m => Math.Abs(m.X - a.X) <= 1 && Math.Abs(m.Y - a.Y) <= 1);
                        return tav - (suruseg * 0.725);
                    }).First();

                    aktualisStatusz = "Haladás";
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
                }
                else
                {
                    // HAZATÉRÉS ÁG: Ide lép be, ha vészhelyzet van VAGY elfogyott az ásvány
                    if (rover.Pozicio != terkep.KezdoPont)
                    {
                        aktualisStatusz = vészhelyzet ? "VÉSZ-HAZATÉRÉS" : "Hazatérés (Kész)";
                        sebessegMod = (vészhelyzet && rover.Akkumulator > 50) ? 3 : 1;

                        Point kovetkezo = KeresLegjobbSzomszed(rover.Pozicio, terkep.KezdoPont, terkep);
                        rover.Pozicio = kovetkezo;
                        osszesTavolsag++;

                        int idoKoltseg = (sebessegMod == 3) ? 10 : 30;
                        rover.FrissitEnergia(rover.SzamolFogyasztas(sebessegMod, nappal, false));
                        ido.IdoUgras(idoKoltseg);
                    }
                    else
                    {
                        // A rover a bázison van és nincs több dolga
                        break;
                    }
                }

                // --- Megjelenítés és Logolás ---
                KiirasKonzolra(ido, rover, sebessegMod, aktualisStatusz, nappal);

                string logSor = $"{ido.ElteltPerc / 60.0:F2};({rover.Pozicio.X},{rover.Pozicio.Y});" +
                                $"{Math.Round(rover.Akkumulator)};{sebessegMod};{osszesTavolsag};" +
                                $"{rover.OsszegyujtottAsvany};{aktualisStatusz};{(nappal ? "Nappal" : "Ejszaka")}\n";
                File.AppendAllText(logPath, logSor);

                // Biztonsági leállás
                if (rover.Akkumulator <= 0) break;
                if (rover.Pozicio == terkep.KezdoPont && (vészhelyzet || !asvanyok.Any())) break;
            }

            // --- Befejezés ---
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n" + new string('=', 50));
            Console.WriteLine("SZIMULÁCIÓ VÉGE");
            Console.WriteLine($"Gyűjtött ásványok: {rover.OsszegyujtottAsvany} db");
            Console.WriteLine($"Megtett távolság: {osszesTavolsag} egység");
            Console.WriteLine($"Végső akkumulátor: {rover.Akkumulator:F1}%");
            Console.WriteLine(new string('=', 50));
            Console.ReadLine();
        }

        static void KiirasKonzolra(IdoKezelo ido, Rover rover, int seb, string statusz, bool nappal)
        {
            Console.ForegroundColor = nappal ? ConsoleColor.Yellow : ConsoleColor.Blue;
            Console.Write($"[{ido.ElteltPerc / 60.0:F2}h] ");

            Console.ResetColor();
            Console.Write($"Poz: ({rover.Pozicio.X,2},{rover.Pozicio.Y,2}) | ");

            if (rover.Akkumulator < 25) Console.ForegroundColor = ConsoleColor.Red;
            else Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"Akku: {Math.Round(rover.Akkumulator),3}% | ");

            Console.ResetColor();
            Console.Write($"Seb: {seb} | ");

            if (statusz == "Bányászat") Console.ForegroundColor = ConsoleColor.Cyan;
            else if (statusz.Contains("VÉSZ")) Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{statusz}");
            Console.ResetColor();
        }

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
                        // Csebisev-távolság (rácson való mozgáshoz ideális)
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
    }
}