using Vadász_Mars_Dénes;
using System.Drawing;
using System.IO; // A fájlkezeléshez szükséges

namespace Vadász_Mars_Dénes
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            MarsMap terkep = new MarsMap();
            terkep.LoadFromFile("mars_map_50x50.csv");

            Rover rover = new Rover();
            rover.Pozicio = terkep.KezdoPont;
            IdoKezelo ido = new IdoKezelo(72);

            double osszesTavolsag = 0; // A megtett távolság követéséhez
            string logPath = "rover_log.csv";

            // Log fájl fejlécének létrehozása
            File.WriteAllText(logPath, "Ido;Pozicio;Akku;Sebesseg;Tavolsag;Asvanyok;Napszak\n");

            while (ido.FuthatMegAProgram)
            {
                bool nappal = ido.NappalVanE();
                int sebesseg = 2;
                bool banyaszik = false;

                // 1. Ellenőrizzük, bányászunk-e éppen
                if (terkep.Vizjeg.Contains(rover.Pozicio) ||
                    terkep.RitkaArany.Contains(rover.Pozicio) ||
                    terkep.RitkaAsvany.Contains(rover.Pozicio))
                {
                    banyaszik = true;
                    rover.OsszegyujtottAsvany++;
                    // Eltávolítjuk, hogy ne bányásszuk újra ugyanazt
                    terkep.Vizjeg.Remove(rover.Pozicio);
                    terkep.RitkaArany.Remove(rover.Pozicio);
                    terkep.RitkaAsvany.Remove(rover.Pozicio);
                    sebesseg = 0; // Bányászat alatt nem mozgunk
                }

                // 2. Energia számítás
                double valtozas = rover.SzamolFogyasztas(sebesseg, nappal, banyaszik);
                rover.FrissitEnergia(valtozas);

                // 3. Mozgás logika (csak ha nem bányászunk)
                if (!banyaszik)
                {
                    List<Point> szomszedok = new List<Point>();
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            Point vizsgalt = new Point(rover.Pozicio.X + dx, rover.Pozicio.Y + dy);
                            if (vizsgalt.X >= 0 && vizsgalt.X < 50 && vizsgalt.Y >= 0 && vizsgalt.Y < 50 &&
                                !terkep.Akadalyok.Contains(vizsgalt))
                            {
                                szomszedok.Add(vizsgalt);
                            }
                        }
                    }

                    var mindenAsvany = terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();

                    if (mindenAsvany.Any() && szomszedok.Any())
                    {
                        Point celpont = mindenAsvany.OrderBy(a =>
                            Math.Sqrt(Math.Pow(a.X - rover.Pozicio.X, 2) + Math.Pow(a.Y - rover.Pozicio.Y, 2))).First();

                        Point legjobbLepes = szomszedok.OrderBy(sz =>
                            Math.Sqrt(Math.Pow(sz.X - celpont.X, 2) + Math.Pow(sz.Y - celpont.Y, 2))).First();

                        // Távolság frissítése (átlósan is 1 egység a feladat szerint)
                        osszesTavolsag += 1;
                        rover.Pozicio = legjobbLepes;
                    }
                }

                // 4. LOGOLÁS (Minden fél órában)
                string logSor = $"{ido.ElteltFelOra * 0.5};" +
                                $"({rover.Pozicio.X},{rover.Pozicio.Y});" +
                                $"{rover.Akkumulator:F1};" +
                                $"{sebesseg};" +
                                $"{osszesTavolsag};" +
                                $"{rover.OsszegyujtottAsvany};" +
                                $"{(nappal ? "Nappal" : "Ejszaka")}\n";

                File.AppendAllText(logPath, logSor);

                // Konzolos visszajelzés
                Console.WriteLine(ido.Statusz());
                Console.WriteLine($"Poz: {rover.Pozicio} | Akku: {rover.Akkumulator:F1}% | Tav: {osszesTavolsag}");

                ido.Lepes();

                if (rover.Akkumulator <= 0)
                {
                    Console.WriteLine("A rover lemerült!");
                    break;
                }
            }
        }
    }
}