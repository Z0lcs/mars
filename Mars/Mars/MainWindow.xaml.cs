using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Drawing;
using System.IO;
using System.Threading.Tasks; // A késleltetett megjelenítéshez
using Point = System.Drawing.Point;

namespace Vadász_Mars_Dénes
{
    public partial class MainWindow : Window
    {
        // Konstansok és útvonalak a logikából
        private static string LogPath = "rover_log.csv";
        private static string PathLogPath = "rover_path.csv";

        // Objektumok
        public MarsMap Terkep { get; set; }
        public Rover DénesRover { get; set; }
        public IdoKezelo Ido { get; set; }

        // Állapotváltozók (Változatlan logika)
        private double osszesTavolsag = 0;
        private bool szimulacioVége = false;
        private bool menekulesAktiv = false;
        private int taktus = 0;
        private int lepesSzamlalo = 0;
        private bool folyamatban = false; // UI zárolás a mozgás alatt

        public MainWindow()
        {
            InitializeComponent();


            //MarsMap terkep = new MarsMap();
            //try
            //{
            //    terkep.LoadFromFile("mars_map_50x50.csv");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"Hiba a térkép betöltésekor: {ex.Message}");
            //    Console.ReadLine();
            //    return;
            //}









            Terkep = new MarsMap();
            try { Terkep.LoadFromFile("mars_map_50x50.csv"); }
            catch (Exception ex) { MessageBox.Show($"Hiba: {ex.Message}"); }

            DénesRover = new Rover();
            DénesRover.Pozicio = Terkep.KezdoPont;

            // A kért 72 órás keret
            Ido = new IdoKezelo(72);

            // Fájlok inicializálása
            File.WriteAllText(LogPath, "Taktus;Ido;Start_Poz;Cel_Poz;Akku;Sebesseg;Ossz_Tav;Asvanyok;Statusz;Napszak\n");
            File.WriteAllText(PathLogPath, "Lépés szám;X;Y\n");




            RoverVezerlo vezerlo = new RoverVezerlo(Terkep, 72, "rover_log.csv");

            vezerlo.SzimulacioInditasa();




            FrissitMegjelenites();
            this.KeyDown += MainWindow_KeyDown;
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Ha épp fut egy animáció vagy vége a szimulációnak, ne csináljon semmit
            if (folyamatban || szimulacioVége) return;

            if (e.Key == Key.Space && Ido.FuthatMegAProgram && DénesRover.Akkumulator > 0)
            {
                folyamatban = true;
                await LepesSzimulacio();
                FrissitMegjelenites();
                folyamatban = false;
            }
        }

        private async Task LepesSzimulacio()
        {
            taktus++;
            Point induloPoz = DénesRover.Pozicio;
            bool nappal = Ido.NappalVanE();
            string aktualisStatusz = "Várakozás";
            Point celpont = Terkep.KezdoPont;

            var asvanyok = GetAsvanyok(Terkep);
            int tavBazisig = Tavolsag(DénesRover.Pozicio, Terkep.KezdoPont);
            double hatralevoPerc = (Ido.MaxOra * 60) - Ido.ElteltPerc;

            // --- EREDETI MENEKÜLÉSI LOGIKA ---
            if (!menekulesAktiv)
            {
                bool kellMenekulni = ((tavBazisig * 12.0) >= (hatralevoPerc - 60)) || (DénesRover.Akkumulator < 30);
                if (kellMenekulni || !asvanyok.Any())
                {
                    menekulesAktiv = true;
                }
            }

            // Hazaérkezés ellenőrzése
            if (DénesRover.Pozicio == Terkep.KezdoPont && menekulesAktiv && taktus > 1)
            {
                szimulacioVége = true;
                MessageBox.Show("Küldetés sikeresen befejezve a bázison!");
                return;
            }

            // --- EREDETI BÁNYÁSZATI LOGIKA ---
            if (asvanyok.Any(a => a.X == DénesRover.Pozicio.X && a.Y == DénesRover.Pozicio.Y) && !menekulesAktiv)
            {
                aktualisStatusz = "Bányászat";
                DénesRover.OsszegyujtottAsvany++;

                Terkep.Grid[DénesRover.Pozicio.X][DénesRover.Pozicio.Y] = ".";
                Terkep.Vizjeg.RemoveAll(p => p.X == DénesRover.Pozicio.X && p.Y == DénesRover.Pozicio.Y);
                Terkep.RitkaArany.RemoveAll(p => p.X == DénesRover.Pozicio.X && p.Y == DénesRover.Pozicio.Y);
                Terkep.RitkaAsvany.RemoveAll(p => p.X == DénesRover.Pozicio.X && p.Y == DénesRover.Pozicio.Y);

                DénesRover.FrissitEnergia(DénesRover.SzamolFogyasztas(0, nappal, true));
                Ido.IdoUgras(30);
                LogToFile(taktus, Ido, induloPoz, DénesRover.Pozicio, 0, aktualisStatusz, nappal, DénesRover, osszesTavolsag);
            }
            else // --- EREDETI MOZGÁSI LOGIKA ---
            {
                if (!menekulesAktiv)
                {
                    celpont = ValasztCelpont(DénesRover.Pozicio, asvanyok);
                    aktualisStatusz = "Haladás";
                }
                else
                {
                    celpont = Terkep.KezdoPont;
                    aktualisStatusz = "VÉSZ-HAZATÉRÉS";
                }

                int sebessegMod = ValasztSebesseg(DénesRover, celpont, nappal, menekulesAktiv);

                // ANIMÁLT MOZGÁS: A ciklus ugyanaz, de minden lépés után rajzolunk
                double megtettEbbenATaktusban = 0;
                for (int i = 0; i < sebessegMod; i++)
                {
                    if (DénesRover.Pozicio == celpont) break;

                    Point kov = KeresLegjobbSzomszed(DénesRover.Pozicio, celpont, Terkep);
                    if (kov != DénesRover.Pozicio)
                    {
                        DénesRover.Pozicio = kov;
                        megtettEbbenATaktusban++;
                        lepesSzamlalo++;

                        // Útvonal naplózása lépésenként
                        File.AppendAllText(PathLogPath, $"{lepesSzamlalo};{DénesRover.Pozicio.X};{DénesRover.Pozicio.Y}\n");

                        // Vizuális frissítés minden egyes mezőváltásnál
                        FrissitMegjelenites();
                        await Task.Delay(150); // Rövid szünet a szemnek
                    }
                }

                osszesTavolsag += megtettEbbenATaktusban;

                DénesRover.FrissitEnergia(DénesRover.SzamolFogyasztas(sebessegMod, nappal, false));
                Ido.IdoUgras(30);

                LogToFile(taktus, Ido, induloPoz, DénesRover.Pozicio, (int)megtettEbbenATaktusban, aktualisStatusz, nappal, DénesRover, osszesTavolsag);
            }

            if (DénesRover.Akkumulator <= 0)
            {
                szimulacioVége = true;
                MessageBox.Show("Kudarc: A rover lemerült.");
            }
        }

        // --- ALAP LOGIKAI FÜGGVÉNYEK (VÁLTOZATLANOK) ---

        static int Tavolsag(Point p1, Point p2) => Math.Max(Math.Abs(p1.X - p2.X), Math.Abs(p1.Y - p2.Y));

        static List<Point> GetAsvanyok(MarsMap m) => m.Vizjeg.Concat(m.RitkaArany).Concat(m.RitkaAsvany).ToList();

        static Point ValasztCelpont(Point akt, List<Point> asvanyok)
        {
            if (!asvanyok.Any()) return akt;
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
            if (vesz) maxSeb = (r.Akkumulator > 35) ? 3 : 1;
            else
            {
                if (nappal && r.Akkumulator > 40) maxSeb = 3;
                else maxSeb = 1;
            }
            return Math.Min(Tavolsag(r.Pozicio, cel), maxSeb);
        }

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

        private void LogToFile(int taktus, IdoKezelo ido, Point s, Point v, int seb, string stat, bool nappal, Rover r, double tav)
        {
            int ora = ido.ElteltPerc / 60;
            int perc = ido.ElteltPerc % 60;
            string logSor = $"{taktus};{ora:D2}:{perc:D2};({s.X},{s.Y});({v.X},{v.Y});{Math.Round(r.Akkumulator)};{seb};{tav};{r.OsszegyujtottAsvany};{stat};{(nappal ? "Nappal" : "Ejszaka")}\n";
            File.AppendAllText(LogPath, logSor);
        }

        private void FrissitMegjelenites()
        {
            List<MapCell> cells = new List<MapCell>();
            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    if (DénesRover.Pozicio.X == i && DénesRover.Pozicio.Y == j)
                        cells.Add(new MapCell { Color = Brushes.White, Type = "ROVER", Coordinates = $"[{i};{j}]" });
                    else
                    {
                        string jel = Terkep.Grid[i][j];
                        cells.Add(new MapCell { Color = GetColorBySign(jel), Type = GetNameBySign(jel), Coordinates = $"[{i};{j}]" });
                    }
                }
            }
            MapDisplay.ItemsSource = cells;

            int o = Ido.ElteltPerc / 60;
            int p = Ido.ElteltPerc % 60;
            StatText.Text = $"Idő: {o:D2}:{p:D2} | Akku: {DénesRover.Akkumulator:F1}% | Ásvány: {DénesRover.OsszegyujtottAsvany} | Táv: {osszesTavolsag} | Státusz: {(menekulesAktiv ? "VÉSZ-HAZA" : "KUTATÁS")}";
        }

        private Brush GetColorBySign(string sign) => sign switch
        {
            "#" => Brushes.DarkGray,
            "Y" => Brushes.Gold,
            "B" => Brushes.DeepSkyBlue,
            "G" => Brushes.LimeGreen,
            "S" => Brushes.Red,
            _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 105, 30))
        };

        private string GetNameBySign(string sign) => sign switch
        {
            "#" => "Akadály",
            "Y" => "Arany",
            "B" => "Vízjég",
            "G" => "Ásvány",
            "S" => "Start",
            _ => "Talaj"
        };
    }

}