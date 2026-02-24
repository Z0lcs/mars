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
            File.WriteAllText(logPath, "Ido;Pozicio;Akku;Sebesseg;Tavolsag;Asvanyok;Statusz;Napszak\n");
            Console.WriteLine($"Eltelt idő: 0 óra | Nappal van e? : True | KezdőPoz: {rover.Pozicio} | Akku: {rover.Akkumulator}% | Seb: 0");
            string logElso = $"0;({rover.Pozicio.X},{rover.Pozicio.Y});" +
                      $"{rover.Akkumulator};0;{osszesTavolsag};" +
                      $"{rover.OsszegyujtottAsvany};{terkep.KezdoPont}; Nappal" + "\n";
            File.WriteAllText(logPath, logElso);
            while (ido.FuthatMegAProgram)
            {
                bool nappal = ido.NappalVanE();
                string aktualisStatusz = "Haladás";
                int tenylegesSebesseg = 0;
                bool banyaszik = false;

                double tavHazateresig = Math.Max(Math.Abs(rover.Pozicio.X - terkep.KezdoPont.X),
                                Math.Abs(rover.Pozicio.Y - terkep.KezdoPont.Y));
                double hatralevoIdo = (ido.teljesIdo * 2) - ido.ElteltFelOra;
                bool vészhelyzet = tavHazateresig >= (hatralevoIdo - 3);
                Point celpont;
                var asvanyok = terkep.Vizjeg.Concat(terkep.RitkaArany).Concat(terkep.RitkaAsvany).ToList();

                if (vészhelyzet)
                {
                    celpont = terkep.KezdoPont;
                    aktualisStatusz = "VÉSZ-HAZATÉRÉS";
                    if (rover.Akkumulator > 70) tenylegesSebesseg = 3;
                    else if (rover.Akkumulator > 45) tenylegesSebesseg = 2;
                    else tenylegesSebesseg = 1;
                }
                else if (asvanyok.Any())
                {
                    celpont = asvanyok.OrderBy(a =>
                      Math.Sqrt(Math.Pow(a.X - rover.Pozicio.X, 2) + Math.Pow(a.Y - rover.Pozicio.Y, 2))).First();

                    if (rover.Pozicio == celpont)
                    {
                        banyaszik = true;
                        aktualisStatusz = "Bányászat";
                        tenylegesSebesseg = 0;
                    }
                    else
                    {
                        if (nappal)
                        {
                            if (rover.Akkumulator > 70) tenylegesSebesseg = 3;
                            else if (rover.Akkumulator > 45) tenylegesSebesseg = 2;
                            else tenylegesSebesseg = 1;
                        }
                        else
                        {
                            if (rover.Akkumulator > 70) tenylegesSebesseg = 2;
                            else tenylegesSebesseg = 1;
                        }
                    }
                }
                else
                {
                    celpont = terkep.KezdoPont;
                    tenylegesSebesseg = 1;
                    aktualisStatusz = "Visszatérés";
                }

                // 3. VÉGREHAJTÁS (MOZGÁS VAGY BÁNYÁSZAT)
                if (banyaszik)
                {
                    rover.OsszegyujtottAsvany++;
                    terkep.Vizjeg.Remove(rover.Pozicio);
                    terkep.RitkaArany.Remove(rover.Pozicio);
                    terkep.RitkaAsvany.Remove(rover.Pozicio);
                }
                else
                {
                    for (int s = 0; s < tenylegesSebesseg; s++)
                    {
                        Point kovetkezo = KeresLegjobbSzomszed(rover.Pozicio, celpont, terkep);
                        if (kovetkezo != rover.Pozicio)
                        {
                            rover.Pozicio = kovetkezo;
                            osszesTavolsag++;
                        }
                        // Ha útközben ásványra ér, megáll (a köv. körben bányászik)
                        if (terkep.Vizjeg.Contains(rover.Pozicio) ||
              terkep.RitkaArany.Contains(rover.Pozicio) ||
              terkep.RitkaAsvany.Contains(rover.Pozicio)) break;
                    }
                }

                // 4. ENERGIA SZÁMÍTÁS (FONTOS SORREND!)
                // A SzamolFogyasztas már tartalmazza a +10 töltést nappal!
                double energiaValtozas = rover.SzamolFogyasztas(tenylegesSebesseg, nappal, banyaszik);
                rover.FrissitEnergia(energiaValtozas);

                // 5. LOGOLÁS
                string logSor = $"{ido.ElteltFelOra * 0.5:F1};({rover.Pozicio.X},{rover.Pozicio.Y});" +
                $"{rover.Akkumulator};{tenylegesSebesseg};{osszesTavolsag};" +
                $"{rover.OsszegyujtottAsvany};{aktualisStatusz};{(nappal ? "Nappal" : "Éjszaka")}\n";
                File.AppendAllText(logPath, logSor);

                Console.WriteLine($"{ido.Statusz()} | Poz: {rover.Pozicio} | Akku: {rover.Akkumulator}% | Seb: {tenylegesSebesseg}");

                ido.Lepes();

                if (rover.Akkumulator <= 0) { Console.WriteLine("LEMERÜLT!"); break; }
                if (vészhelyzet && rover.Pozicio == terkep.KezdoPont) { Console.WriteLine("HAZAÉRT!"); break; }
            }
        }

        static Point KeresLegjobbSzomszed(Point jelenlegi, Point cel, MarsMap terkep)
        {
            Point legjobb = jelenlegi;
            double minTav = Math.Sqrt(Math.Pow(jelenlegi.X - cel.X, 2) + Math.Pow(jelenlegi.Y - cel.Y, 2));

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    Point vizsgalt = new Point(jelenlegi.X + dx, jelenlegi.Y + dy);
                    if (vizsgalt.X >= 0 && vizsgalt.X < 50 && vizsgalt.Y >= 0 && vizsgalt.Y < 50 &&
                      !terkep.Akadalyok.Contains(vizsgalt))
                    {
                        double tav = Math.Sqrt(Math.Pow(vizsgalt.X - cel.X, 2) + Math.Pow(vizsgalt.Y - cel.Y, 2));
                        if (tav < minTav) { minTav = tav; legjobb = vizsgalt; }
                    }
                }
            }
            return legjobb;
        }
    }
}