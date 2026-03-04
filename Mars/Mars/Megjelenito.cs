using System;
using System.Drawing;
using System.IO;

namespace Vadász_Mars_Dénes
{
    public class Megjelenito
    {
        private readonly string logPath;
        private readonly int maxOra;

        public Megjelenito(string logPath, int maxOra)
        {
            this.logPath = logPath;
            this.maxOra = maxOra;
        }

        //public void ElsoSor()
        //{
        //    Console.Clear();
        //    Console.WriteLine($"\n=== MARS ROVER: VÉGLEGES VERZIÓ ({maxOra}h) ===\n");
        //    Console.WriteLine(string.Format(" {0,-6} | {1,-9} | {2,-13} | {3,-8} | {4,-3} | {5,-16} | {6}",
        //        "Sorsz", "Idő", "Pozíció", "Akku", "Seb", "Státusz", "Ásvány"));
        //    Console.WriteLine(new string('-', 90));
        //}

        public void LogEsKiir(int taktus, IdoKezelo ido, Point s, Point v, int seb, string stat, bool nappal, Rover r, double tav)
        {
            int ora = ido.ElteltPerc / 60;
            int perc = ido.ElteltPerc % 60;
            string idoSzoveg = $"{ora:D2}:{perc:D2}";

            //Console.ForegroundColor = ConsoleColor.Gray;
            //Console.Write($" #{taktus:D3}   | ");

            //Console.ForegroundColor = nappal ? ConsoleColor.Yellow : ConsoleColor.Blue;
            //Console.Write($"[{idoSzoveg}] ");
            //Console.ResetColor();
            //Console.Write("| ");

            //Console.Write($"({s.X,2},{s.Y,2})->({v.X,2},{v.Y,2}) | ");

            //if (r.Akkumulator < 25) Console.ForegroundColor = ConsoleColor.Red;
            //else Console.ForegroundColor = ConsoleColor.Green;
            //Console.Write($"{Math.Round(r.Akkumulator),3}%");
            //Console.ResetColor();
            //Console.Write("     | ");

            //Console.Write($"{seb}   | ");

            //if (stat == "Bányászat") Console.ForegroundColor = ConsoleColor.Cyan;
            //else if (stat.Contains("VÉSZ")) Console.ForegroundColor = ConsoleColor.Magenta;
            //else Console.ForegroundColor = ConsoleColor.White;

            //Console.Write(string.Format("{0,-16}", stat));
            //Console.ResetColor();
            //Console.Write("| ");

            //Console.ForegroundColor = ConsoleColor.Yellow;
            //Console.WriteLine($"{r.OsszegyujtottAsvany,2} db");
            //Console.ResetColor();

            string logSor = $"{taktus};{idoSzoveg};({s.X},{s.Y});({v.X},{v.Y});{Math.Round(r.Akkumulator)};{seb};{tav};{r.OsszegyujtottAsvany};{stat};{(nappal ? "Nappal" : "Ejszaka")}\n";
            File.AppendAllText(logPath, logSor);
        }

        //public void KiirEredmeny(Rover r, MarsMap m, double tav)
        //{
        //    Console.WriteLine(new string('-', 90));
        //    Console.WriteLine($"\nSZIMULÁCIÓ VÉGE");
        //    Console.WriteLine($"Gyűjtött ásványok: {r.OsszegyujtottAsvany} db");
        //    Console.WriteLine($"Megtett távolság:  {tav} egység");

        //    bool sikeres = (r.Pozicio == m.KezdoPont);
        //    Console.Write("Helyzet: ");
        //    if (sikeres)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Green;
        //        Console.WriteLine("BÁZISON (Sikeres)");
        //    }
        //    else
        //    {
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine("TEREPEN (Kudarc)");
        //    }
        //    Console.ResetColor();
        //    Console.WriteLine(new string('-', 90));
        //    Console.ReadLine();
        //}
    }
}