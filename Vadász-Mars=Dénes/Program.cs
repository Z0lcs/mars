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
            MarsMap terkep = new MarsMap();
            try { terkep.LoadFromFile("mars_map_50x50.csv"); }
            catch (Exception ex) { Console.WriteLine($"Hiba: {ex.Message}"); return; }

            // Állítsuk be a küldetés hosszát (72 óra)
            int kulsdetesMaxOra = 72;
            Rover rover = new Rover();
            rover.Pozicio = terkep.KezdoPont;
            IdoKezelo ido = new IdoKezelo(kulsdetesMaxOra);
            double osszesTavolsag = 0;
            string logPath = "rover_log.csv";
            bool kenyszeritettHazateres = false;
            int taktusSzamlalo = 0;

            File.WriteAllText(logPath, "Taktus;Ido_Ora;Start_Poz;Cel_Poz;Akku;Sebesseg;Ossz_Tav;Asvanyok;Statusz;Napszak\n");

            Console.WriteLine($"=== MARS ROVER STRATÉGIAI SZIMULÁCIÓ INDUL ({kulsdetesMaxOra}h) ===");

            // JAVÍTVA: 11 paraméter
            LogEsKiir(0, ido, rover.Pozicio, rover.Pozicio, 0, "START", true, rover.Akkumulator, 0, 0, logPath);

            while (ido.FuthatMegAProgram)
            {
                taktusSzamlalo++;
                bool nappal = ido.NappalVanE();
                int sebessegMod = 0;
                Point induloPozicio = rover.Pozicio;
                var asvanyok = terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();

                int tavBazisig = Math.Max(Math.Abs(rover.Pozicio.X - terkep.KezdoPont.X), Math.Abs(rover.Pozicio.Y - terkep.KezdoPont.Y));

                // JAVÍTVA: Dinamikus időszámítás a kulsdetesMaxOra használatával
                double hatralevoPerc = (kulsdetesMaxOra * 60) - ido.ElteltPerc;

                if ((tavBazisig * 15) >= (hatralevoPerc - 30)) kenyszeritettHazateres = true;
                if (!asvanyok.Any()) kenyszeritettHazateres = true;

                if (rover.Pozicio == terkep.KezdoPont && kenyszeritettHazateres && taktusSzamlalo > 1) break;

                if (asvanyok.Any(a => a == rover.Pozicio) && !kenyszeritettHazateres)
                {
                    rover.OsszegyujtottAsvany++;
                    terkep.Vizjeg.RemoveAll(p => p == rover.Pozicio);
                    terkep.RitkaArany.RemoveAll(p => p == rover.Pozicio);
                    terkep.RitkaAsvany.RemoveAll(p => p == rover.Pozicio);
                    rover.FrissitEnergia(rover.SzamolFogyasztas(0, nappal, true));
                    ido.IdoUgras(30);
                    // JAVÍTVA: 11 paraméter
                    LogEsKiir(taktusSzamlalo, ido, induloPozicio, rover.Pozicio, 0, "Bányászat", nappal, rover.Akkumulator, osszesTavolsag, rover.OsszegyujtottAsvany, logPath);
                }
                else
                {
                    Point celpont = kenyszeritettHazateres ? terkep.KezdoPont :
                                    asvanyok.OrderBy(a => Math.Max(Math.Abs(a.X - rover.Pozicio.X), Math.Abs(a.Y - rover.Pozicio.Y))).First();

                    int tav = Math.Max(Math.Abs(celpont.X - rover.Pozicio.X), Math.Abs(celpont.Y - rover.Pozicio.Y));

                    if (tav >= 3 && (nappal || rover.Akkumulator > 30)) sebessegMod = 3;
                    else if (tav >= 2) sebessegMod = 2;
                    else sebessegMod = 1;

                    for (int i = 0; i < sebessegMod; i++)
                    {
                        if (rover.Pozicio == celpont) break;
                        Point kov = KeresLegjobbSzomszed(rover.Pozicio, celpont, terkep);
                        if (kov != rover.Pozicio) { rover.Pozicio = kov; osszesTavolsag++; }
                    }

                    rover.FrissitEnergia(rover.SzamolFogyasztas(sebessegMod, nappal, false));
                    ido.IdoUgras(30);

                    string stat = kenyszeritettHazateres ? "VÉSZ-HAZATÉRÉS" : "Haladás";
                    // JAVÍTVA: 11 paraméter
                    LogEsKiir(taktusSzamlalo, ido, induloPozicio, rover.Pozicio, sebessegMod, stat, nappal, rover.Akkumulator, osszesTavolsag, rover.OsszegyujtottAsvany, logPath);
                }

                if (rover.Akkumulator <= 0) { Console.WriteLine("LEMERÜLT!"); break; }
            }

            Console.WriteLine($"\nSZIMULÁCIÓ VÉGE. Összesen: {rover.OsszegyujtottAsvany} db ásvány.");
            Console.ReadLine();
        }

        static void LogEsKiir(int taktus, IdoKezelo ido, Point start, Point veg, int seb, string statusz, bool nappal, double akku, double tav, int asvany, string path)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"#{taktus:D3} | ");
            Console.ForegroundColor = nappal ? ConsoleColor.Yellow : ConsoleColor.Blue;
            Console.Write($"[{ido.ElteltPerc / 60.0,5:F2}h] ");
            Console.ResetColor();
            Console.Write($"({start.X,2},{start.Y,2})->({veg.X,2},{veg.Y,2}) | Akku:{Math.Round(akku),3}% | Seb:{seb} | ");

            if (statusz == "Bányászat") Console.ForegroundColor = ConsoleColor.Cyan;
            else if (statusz.Contains("VÉSZ")) Console.ForegroundColor = ConsoleColor.Magenta;
            else Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine(statusz);
            Console.ResetColor();

            string logSor = $"{taktus};{ido.ElteltPerc / 60.0:F2};({start.X},{start.Y});({veg.X},{veg.Y});{Math.Round(akku)};{seb};{tav};{asvany};{statusz};{(nappal ? "Nappal" : "Ejszaka")}\n";
            File.AppendAllText(path, logSor);
        }

        static Point KeresLegjobbSzomszed(Point jelenlegi, Point cel, MarsMap terkep)
        {
            Point legjobb = jelenlegi;
            double minTav = double.MaxValue;
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Point v = new Point(jelenlegi.X + dx, jelenlegi.Y + dy);
                    if (v.X >= 0 && v.X < 50 && v.Y >= 0 && v.Y < 50 && !terkep.Akadalyok.Any(a => a == v))
                    {
                        double tav = Math.Max(Math.Abs(v.X - cel.X), Math.Abs(v.Y - cel.Y));
                        if (tav < minTav) { minTav = tav; legjobb = v; }
                    }
                }
            return legjobb;
        }
    }
}