//using System.Drawing;
//using Vadász_Mars_Dénes;
//using System.IO;
//using System.Linq;

//namespace Vadász_Mars_Dénes
//{
//    internal class Program
//    {
//        static void Main(string[] args)
//        {
//            MarsMap terkep = new MarsMap();
//            terkep.LoadFromFile("mars_map_50x50.csv");
//            Rover rover = new Rover();
//            rover.Pozicio = terkep.KezdoPont;
//            IdoKezelo ido = new IdoKezelo(24);
//            double osszesTavolsag = 0;
//            string logPath = "rover_log.csv";

//            File.WriteAllText(logPath, "Ido;Pozicio;Akku;Sebesseg;Tavolsag;Asvanyok;Statusz;Napszak\n");
//            File.AppendAllText(logPath, $"0.0;({rover.Pozicio.X},{rover.Pozicio.Y});100;0;0;0;Kezdés;Nappal\n");

//            Console.WriteLine("=== MARS ROVER SZIMULÁCIÓ INDUL ===");
//            Console.WriteLine($"Kezdőpont: {terkep.KezdoPont} | Összes idő: 72 óra");
//            Console.WriteLine("--------------------------------------------------");

//            while (ido.FuthatMegAProgram)
//            {
//                bool nappal = ido.NappalVanE();
//                string aktualisStatusz = "Várakozás";
//                int tenylegesSebesseg = 0;
//                bool banyaszik = false;
//                Point celpont = terkep.KezdoPont;

//                var asvanyok = terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();
//                int tavBazisig = Math.Max(Math.Abs(rover.Pozicio.X - terkep.KezdoPont.X),
//                                          Math.Abs(rover.Pozicio.Y - terkep.KezdoPont.Y));
//                double hatralevoIdo = (ido.teljesIdo * 2) - ido.ElteltFelOra;
//                bool vészhelyzet = tavBazisig >= (hatralevoIdo - 1.5);
//                if (vészhelyzet)
//                {
//                    celpont = terkep.KezdoPont;
//                    aktualisStatusz = "VÉSZ-HAZATÉRÉS";
//                }
//                else if (asvanyok.Any(a => a.X == rover.Pozicio.X && a.Y == rover.Pozicio.Y))
//                {
//                    banyaszik = true;
//                    aktualisStatusz = "Bányászat";
//                }
//                else if (asvanyok.Any())
//                {
//                    celpont = asvanyok.OrderBy(a =>
//                    {
//                        double tav = Math.Max(Math.Abs(a.X - rover.Pozicio.X), Math.Abs(a.Y - rover.Pozicio.Y));
//                        int suruseg = asvanyok.Count(m => Math.Abs(m.X - a.X) <= 1 && Math.Abs(m.Y - a.Y) <= 1);
//                        double ertek = (terkep.RitkaArany.Contains(a) || terkep.RitkaAsvany.Contains(a)) ? 5.0 : 0.0;
//                        return tav - (suruseg * 0.725) - ertek;
//                    }).First();
//                    aktualisStatusz = "Haladás";
//                }
//                if (banyaszik)
//                {
//                    rover.OsszegyujtottAsvany++;
//                    terkep.Vizjeg.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
//                    terkep.RitkaArany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
//                    terkep.RitkaAsvany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
//                }
//                else
//                {
//                    int maxV = (vészhelyzet) ? ((rover.Akkumulator > 55) ? 3 : 2) :
//                                               ((nappal && rover.Akkumulator > 35) ? 2 : 1);
//                    int tavCelpontig = Math.Max(Math.Abs(celpont.X - rover.Pozicio.X), Math.Abs(celpont.Y - rover.Pozicio.Y));
//                    tenylegesSebesseg = Math.Min(maxV, tavCelpontig);

//                    for (int s = 0; s < tenylegesSebesseg; s++)
//                    {
//                        Point x = KeresLegjobbSzomszed(rover.Pozicio, celpont, terkep);
//                        if (x != rover.Pozicio)
//                        {
//                            rover.Pozicio = x;
//                            osszesTavolsag++;
//                        }
//                        if (terkep.Vizjeg.Any(a => a == rover.Pozicio) ||
//                            terkep.RitkaArany.Any(a => a == rover.Pozicio) ||
//                            terkep.RitkaAsvany.Any(a => a == rover.Pozicio)) break;
//                    }
//                }
//                double energiaValtozas = rover.SzamolFogyasztas(tenylegesSebesseg, nappal, banyaszik);
//                rover.FrissitEnergia(energiaValtozas);
//                ido.Lepes();

//                string logSor = $"{ido.ElteltFelOra * 0.5:F1};({rover.Pozicio.X},{rover.Pozicio.Y});" +
//                                $"{Math.Round(rover.Akkumulator)};{tenylegesSebesseg};{osszesTavolsag};" +
//                                $"{rover.OsszegyujtottAsvany};{aktualisStatusz};{(nappal ? "Nappal" : "Ejszaka")}\n";
//                File.AppendAllText(logPath, logSor);
//                Console.WriteLine($"{ido.Statusz()} | Poz: ({rover.Pozicio.X,2},{rover.Pozicio.Y,2}) | Akku: {rover.Akkumulator,3:F0}% | Seb: {tenylegesSebesseg} | {aktualisStatusz}");

//                if (rover.Akkumulator <= 0) { Console.WriteLine("\n!!! A ROVER LEMERÜLT !!!"); break; }
//                if (vészhelyzet && rover.Pozicio == terkep.KezdoPont) { Console.WriteLine("\n>>> KÜLDETÉS SIKERES: HAZAÉRT <<<"); break; }
//            }
//            Console.WriteLine("--------------------------------------------------");
//            Console.WriteLine("SZIMULÁCIÓ VÉGE");
//            Console.WriteLine($"Gyűjtött ásványok: {rover.OsszegyujtottAsvany} db");
//            Console.WriteLine($"Megtett távolság: {osszesTavolsag} egység");
//            Console.WriteLine($"Végső akkumulátor: {rover.Akkumulator:F1}%");
//            Console.WriteLine("--------------------------------------------------");
//            Console.ReadLine();
//        }

//        static Point KeresLegjobbSzomszed(Point jelenlegi, Point cel, MarsMap terkep)
//        {
//            Point legjobb = jelenlegi;
//            double minTav = Math.Sqrt(Math.Pow(jelenlegi.X - cel.X, 2) + Math.Pow(jelenlegi.Y - cel.Y, 2));
//            for (int dx = -1; dx <= 1; dx++)
//            {
//                for (int dy = -1; dy <= 1; dy++)
//                {
//                    if (dx == 0 && dy == 0) continue;
//                    Point vizsgalt = new Point(jelenlegi.X + dx, jelenlegi.Y + dy);
//                    if (vizsgalt.X >= 0 && vizsgalt.X < 50 && vizsgalt.Y >= 0 && vizsgalt.Y < 50 &&
//                        !terkep.Akadalyok.Any(a => a.X == vizsgalt.X && a.Y == vizsgalt.Y))
//                    {
//                        double tav = Math.Sqrt(Math.Pow(vizsgalt.X - cel.X, 2) + Math.Pow(vizsgalt.Y - cel.Y, 2));
//                        if (tav < minTav) { minTav = tav; legjobb = vizsgalt; }
//                    }
//                }
//            }
//            return legjobb;
//        }
//    }
//}