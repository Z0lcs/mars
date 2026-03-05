using Mars;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Point = System.Drawing.Point;

namespace Vadász_Mars_Dénes
{
    public partial class MainWindow : Window
    {
        private static string LogPath = "rover_log.csv";
        private static string PathLogPath = "rover_path.csv";

        public MarsMap Terkep { get; set; }
        public Rover DénesRover { get; set; }

        private List<string[]> szimulaciosLog = new List<string[]>();
        private List<Point> utvonalPontok = new List<Point>();

        private int aktualisLogIndex = 0;
        private int aktualisUtvonalIndex = 0;
        private bool folyamatban = false;

        public MainWindow()
        {
            InitializeComponent();

            // 1. FÁZIS: SZIMULÁCIÓ (Minden számítás lefut a háttérben)
            Terkep = new MarsMap();
            try
            {
                Terkep.LoadFromFile("mars_map_50x50.csv");
            }
            catch (Exception ex) { MessageBox.Show($"Hiba a térkép betöltésekor: {ex.Message}"); }

            // A szimuláció elindítása (ez generálja a CSV-ket)
            RoverVezerlo vezerlo = new RoverVezerlo(Terkep, 72, LogPath, false);
            vezerlo.SzimulacioInditasa();

            // 2. FÁZIS: ADATOK BETÖLTÉSE A VISSZAJÁTSZÁSHOZ
            AdatokBetoltese();

            // ROVER INICIALIZÁLÁSA A BÁZISRA
            DénesRover = new Rover();
            // Kifejezetten a térkép kezdőpontjára állítjuk
            DénesRover.Pozicio = Terkep.KezdoPont;

            // UI alaphelyzetbe állítása
            aktualisLogIndex = 0;
            aktualisUtvonalIndex = 0;

            FrissitMegjelenites();
            this.KeyDown += MainWindow_KeyDown;
        }

        private void AdatokBetoltese()
        {
            try
            {
                if (File.Exists(LogPath) && File.Exists(PathLogPath))
                {
                    // Log sorok (statisztikák)
                    szimulaciosLog = File.ReadAllLines(LogPath).Skip(1).Select(line => line.Split(';')).ToList();

                    // Útvonal koordináták (pontos mozgás)
                    var utvonalSorok = File.ReadAllLines(PathLogPath).Skip(1);
                    utvonalPontok.Clear();
                    foreach (var sor in utvonalSorok)
                    {
                        var adatok = sor.Split(';');
                        utvonalPontok.Add(new Point(int.Parse(adatok[1]), int.Parse(adatok[2])));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Hiba az adatok beolvasásakor: {ex.Message}");
            }
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (folyamatban) return;

            // W gomb: Visszajátssza a következő taktus eseményeit
            if (e.Key == Key.W)
            {
                if (aktualisLogIndex >= szimulaciosLog.Count)
                {
                    MessageBox.Show("A rover hazaért vagy befejezte a küldetést!");
                    return;
                }

                folyamatban = true;
                await KovetkezoTaktusLejatszasa();
                folyamatban = false;
            }
        }

        private async Task KovetkezoTaktusLejatszasa()
        {
            var sor = szimulaciosLog[aktualisLogIndex];

            // Adatok: [5] - Sebesség (hányat lépett), [8] - Státusz
            int sebesseg = int.Parse(sor[5]);
            string statusz = sor[8];

            if (statusz == "Bányászat")
            {
                // Egy helyben áll és bányászik: eltüntetjük az ásványt a rácsról
                Terkep.Grid[DénesRover.Pozicio.X][DénesRover.Pozicio.Y] = ".";
                UpdateUIInfo(sor);
                FrissitMegjelenites();
                await Task.Delay(200);
            }
            else
            {
                // Mozgás: annyi pontot olvasunk ki a sorrendi listából, amennyi a sebesség volt
                for (int i = 0; i < sebesseg; i++)
                {
                    if (aktualisUtvonalIndex < utvonalPontok.Count)
                    {
                        DénesRover.Pozicio = utvonalPontok[aktualisUtvonalIndex];
                        aktualisUtvonalIndex++;

                        UpdateUIInfo(sor);
                        FrissitMegjelenites();
                        await Task.Delay(100); // Animációs sebesség
                    }
                }
            }

            aktualisLogIndex++;
        }

        private void UpdateUIInfo(string[] logSor)
        {
            // Statisztikai szöveg frissítése a CSV sor alapján
            StatText.Text = $"Idő: {logSor[1]} ({logSor[9]}) | Akku: {logSor[4]}% | Ásvány: {logSor[7]} db | Táv: {logSor[6]} | Státusz: {logSor[8]}";
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

        private void StatisztikaGomb_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow statAblak = new DashboardWindow();
            statAblak.ShowDialog();
        }
    }
}