using System.Drawing;
using Vadász_Mars_Dénes;

namespace Vadász_Mars_Dénes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MarsMap terkep = new MarsMap();
            terkep.LoadFromFile("mars_map_50x50.csv");

            Rover rover = new Rover();
            rover.Pozicio = terkep.KezdoPont;

            IdoKezelo ido = new IdoKezelo(72);
            double osszesTavolsag = 0;
            string logPath = "rover_log.csv";

            // 1. KEZDŐÁLLAPOT RÖGZÍTÉSE (Csak egyszer, a ciklus előtt)
            File.WriteAllText(logPath, "Ido;Pozicio;Akku;Sebesseg;Tavolsag;Asvanyok;Statusz;Napszak\n");
            string kezdoLog = $"0.0;({rover.Pozicio.X},{rover.Pozicio.Y});100;0;0;0;Kezdés;Nappal\n";
            File.AppendAllText(logPath, kezdoLog);

            Console.WriteLine($"Kezdés | Poz: {rover.Pozicio} | Akku: {rover.Akkumulator}% | Idő: 0.0 óra");

            while (ido.FuthatMegAProgram)
            {
                // JAVÍTÁS: Az időt a kör elején léptetjük, így az első mozgás már 0.5 lesz
                ido.Lepes();

                bool nappal = ido.NappalVanE();
                string aktualisStatusz = "Várakozás";
                int tenylegesSebesseg = 0;
                bool banyaszik = false;
                Point celpont = terkep.KezdoPont;

                var asvanyok = terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();

                // HELYZETÉRTÉKELÉS
                int tavBazisig = Math.Max(Math.Abs(rover.Pozicio.X - terkep.KezdoPont.X),
                                          Math.Abs(rover.Pozicio.Y - terkep.KezdoPont.Y));
                double hatralevoIdo = (ido.teljesIdo * 2) - ido.ElteltFelOra;
                bool vészhelyzet = tavBazisig >= (hatralevoIdo - 5);

                // DÖNTÉSHOZATAL
                if (vészhelyzet)
                {
                    celpont = terkep.KezdoPont;
                    aktualisStatusz = "VÉSZ-HAZATÉRÉS";
                }
                else if (asvanyok.Any(a => a.X == rover.Pozicio.X && a.Y == rover.Pozicio.Y))
                {
                    banyaszik = true;
                    aktualisStatusz = "Bányászat";
                }
                else if (asvanyok.Any())
                {
                    celpont = asvanyok.OrderBy(a => Math.Max(Math.Abs(a.X - rover.Pozicio.X),
                                                            Math.Abs(a.Y - rover.Pozicio.Y))).First();
                    aktualisStatusz = "Haladás";
                }

                // MOZGÁS VAGY BÁNYÁSZAT
                if (banyaszik)
                {
                    rover.OsszegyujtottAsvany++;
                    terkep.Vizjeg.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaArany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                    terkep.RitkaAsvany.RemoveAll(p => p.X == rover.Pozicio.X && p.Y == rover.Pozicio.Y);
                }
                else
                {
                    int maxVagyott = (vészhelyzet) ? ((rover.Akkumulator > 40) ? 2 : 1) :
                                                    ((nappal && rover.Akkumulator > 70) ? 3 : 2);
                    int tavolsagACelpontig = Math.Max(Math.Abs(celpont.X - rover.Pozicio.X),
                                                      Math.Abs(celpont.Y - rover.Pozicio.Y));
                    tenylegesSebesseg = Math.Min(maxVagyott, tavolsagACelpontig);

                    for (int s = 0; s < tenylegesSebesseg; s++)
                    {
                        Point kovetkezo = KeresLegjobbSzomszed(rover.Pozicio, celpont, terkep);
                        if (kovetkezo != rover.Pozicio)
                        {
                            rover.Pozicio = kovetkezo;
                            osszesTavolsag++;
                        }
                        if (asvanyok.Any(a => a.X == rover.Pozicio.X && a.Y == rover.Pozicio.Y)) break;
                    }
                }

                // ENERGIA FRISSÍTÉSE
                double energiaValtozas = rover.SzamolFogyasztas(tenylegesSebesseg, nappal, banyaszik);
                rover.FrissitEnergia(energiaValtozas);

                // LOGOLÁS
                string logSor = $"{ido.ElteltFelOra * 0.5:F1};({rover.Pozicio.X},{rover.Pozicio.Y});" +
                                $"{Math.Round(rover.Akkumulator)};{tenylegesSebesseg};{osszesTavolsag};" +
                                $"{rover.OsszegyujtottAsvany};{aktualisStatusz};{(nappal ? "Nappal" : "Éjszaka")}\n";
                File.AppendAllText(logPath, logSor);

                Console.WriteLine($"{ido.Statusz()} | Poz: {rover.Pozicio} | Akku: {rover.Akkumulator:F0}% | Stat: {aktualisStatusz}");

                // KILÉPÉSI FELTÉTELEK
                if (rover.Akkumulator <= 0) { Console.WriteLine("LEMERÜLT!"); break; }
                if (vészhelyzet && rover.Pozicio == terkep.KezdoPont) { Console.WriteLine("KÜLDETÉS SIKERES: Hazaért."); break; }
            }
        }

        static Point KeresLegjobbSzomszed(Point jelenlegi, Point cel, MarsMap terkep)
        {
            Point legjobb = jelenlegi;
            // A jelenlegi távolság a célponttól
            double minTav = Math.Sqrt(Math.Pow(jelenlegi.X - cel.X, 2) + Math.Pow(jelenlegi.Y - cel.Y, 2));

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Point vizsgalt = new Point(jelenlegi.X + dx, jelenlegi.Y + dy);

                    // Pályán belül van és NEM akadály
                    if (vizsgalt.X >= 0 && vizsgalt.X < 50 && vizsgalt.Y >= 0 && vizsgalt.Y < 50 &&
                        !terkep.Akadalyok.Any(a => a.X == vizsgalt.X && a.Y == vizsgalt.Y))
                    {
                        // Itt Euklideszi távolságot használunk a precízebb irányért
                        double tav = Math.Sqrt(Math.Pow(vizsgalt.X - cel.X, 2) + Math.Pow(vizsgalt.Y - cel.Y, 2));

                        // Ha találtunk közelebbi pontot, azt választjuk
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