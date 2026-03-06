using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Mars;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        // --- DASHBOARD TULAJDONSÁGOK ---
        public SeriesCollection AkkuSeries { get; set; }
        public SeriesCollection AsvanySeries { get; set; }
        public SeriesCollection StatuszSeries { get; set; }
        public SeriesCollection SebessegSeries { get; set; }
        public SeriesCollection NapszakSeries { get; set; }
        public SeriesCollection TeljesitmenySeries { get; set; }

        public string[] SebessegLabels { get; set; }
        public string[] TeljesitmenyLabels { get; set; }
        public Func<double, string> XFormatter { get; set; }

        // --- SZIMULÁCIÓS ADATOK ---
        private static string LogPath = "rover_log.csv";
        private static string PathLogPath = "rover_path.csv";
        private static string SummaryPath = "rover_summary.csv";

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

            // 1. Dashboard inicializálása
            XFormatter = val => val.ToString("F1");
            AkkuSeries = new SeriesCollection();
            AsvanySeries = new SeriesCollection();
            StatuszSeries = new SeriesCollection();
            SebessegSeries = new SeriesCollection();
            NapszakSeries = new SeriesCollection();
            TeljesitmenySeries = new SeriesCollection();

            SebessegLabels = new[] { "1-es", "2-es", "3-as" };
            TeljesitmenyLabels = new[] { "Vízjég", "Arany", "Ritka" };

            // 2. Szimuláció futtatása
            Terkep = new MarsMap();
            try { Terkep.LoadFromFile("mars_map_50x50.csv"); }
            catch (Exception ex) { MessageBox.Show($"Hiba: {ex.Message}"); }

            RoverVezerlo vezerlo = new RoverVezerlo(Terkep, 72, LogPath, false);
            vezerlo.SzimulacioInditasa();

            // 3. Adatok betöltése (CSV -> Grafikonok és Listák)
            BetoltDashboardAdatok();
            BetoltUtvonalAdatok();

            // 4. Kezdőállapot beállítása
            DénesRover = new Rover { Pozicio = Terkep.KezdoPont };
            this.DataContext = this; // Nagyon fontos a Binding-hoz!

            FrissitMegjelenites();
            this.KeyDown += MainWindow_KeyDown;
        }

        private void BetoltDashboardAdatok()
        {
            try
            {
                if (!File.Exists(LogPath)) return;

                var logSorok = File.ReadAllLines(LogPath);
                var akkuPontok = new ChartValues<ObservablePoint>();
                var asvanyPontok = new ChartValues<ObservablePoint>();

                int s1 = 0, s2 = 0, s3 = 0;
                int nBany = 0, eBany = 0;
                int hal = 0, bany = 0, haz = 0;

                // Elmentjük a logot a visszajátszáshoz is (fejléc nélkül)
                szimulaciosLog = logSorok.Skip(1).Select(line => line.Split(';')).ToList();

                foreach (var d in szimulaciosLog)
                {
                    // Idő számítása órában (X tengely)
                    double oraVal = 0;
                    if (d[1].Contains(":"))
                    {
                        var idoReszek = d[1].Split(':');
                        oraVal = double.Parse(idoReszek[0]) + (double.Parse(idoReszek[1]) / 60.0);
                    }

                    double akkuVal = double.Parse(d[4], CultureInfo.InvariantCulture);
                    double asvanyVal = double.Parse(d[7], CultureInfo.InvariantCulture);

                    akkuPontok.Add(new ObservablePoint(oraVal, akkuVal));
                    asvanyPontok.Add(new ObservablePoint(oraVal, asvanyVal));

                    int seb = int.Parse(d[5]);
                    if (seb == 1) s1++; else if (seb == 2) s2++; else if (seb == 3) s3++;

                    if (d[8] == "Bányászat")
                    {
                        bany++;
                        if (d[9].Trim() == "Nappal") nBany++; else eBany++;
                    }
                    else if (d[8].Contains("Haladás")) hal++;
                    else if (d[8].Contains("HAZA")) haz++;
                }

                // Grafikonok feltöltése
                AkkuSeries.Add(new LineSeries { Title = "Akku %", Values = akkuPontok, PointGeometry = null, Fill = Brushes.Transparent, Stroke = Brushes.Orange });
                AsvanySeries.Add(new LineSeries { Title = "Ásvány", Values = asvanyPontok, PointGeometry = null, Stroke = Brushes.Cyan });

                Func<ChartPoint, string> pieLabel = p => $"{p.Y} db";
                StatuszSeries.Add(new PieSeries { Title = "Haladás", Values = new ChartValues<int> { hal }, DataLabels = true, LabelPoint = pieLabel });
                StatuszSeries.Add(new PieSeries { Title = "Bányászat", Values = new ChartValues<int> { bany }, DataLabels = true, LabelPoint = pieLabel });
                StatuszSeries.Add(new PieSeries { Title = "Haza", Values = new ChartValues<int> { haz }, DataLabels = true, LabelPoint = pieLabel });

                SebessegSeries.Add(new ColumnSeries { Title = "Gyakoriság", Values = new ChartValues<int> { s1, s2, s3 }, DataLabels = true });

                NapszakSeries.Add(new StackedColumnSeries { Title = "Nappal", Values = new ChartValues<int> { nBany, 0 }, DataLabels = true });
                NapszakSeries.Add(new StackedColumnSeries { Title = "Éjjel", Values = new ChartValues<int> { 0, eBany }, DataLabels = true });

                if (File.Exists(SummaryPath))
                {
                    var sumSorok = File.ReadAllLines(SummaryPath);
                    var sd = sumSorok[1].Split(';');
                    int gV = int.Parse(sd[3]), oV = int.Parse(sd[4]);
                    int gA = int.Parse(sd[5]), oA = int.Parse(sd[6]);
                    int gR = int.Parse(sd[7]), oR = int.Parse(sd[8]);

                    TeljesitmenySeries.Add(new RowSeries { Title = "Sikeres", Values = new ChartValues<int> { gV, gA, gR }, DataLabels = true });
                    TeljesitmenySeries.Add(new RowSeries { Title = "Veszteség", Values = new ChartValues<int> { oV - gV, oA - gA, oR - gR }, DataLabels = true });
                }
            }
            catch (Exception ex) { MessageBox.Show("Hiba a Dashboard adatoknál: " + ex.Message); }
        }

        private void BetoltUtvonalAdatok()
        {
            if (File.Exists(PathLogPath))
            {
                var lines = File.ReadAllLines(PathLogPath).Skip(1);
                foreach (var line in lines)
                {
                    var p = line.Split(';');
                    utvonalPontok.Add(new Point(int.Parse(p[1]), int.Parse(p[2])));
                }
            }
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (folyamatban || aktualisLogIndex >= szimulaciosLog.Count) return;

            if (e.Key == Key.W)
            {
                folyamatban = true;
                await KovetkezoLepes();
                folyamatban = false;
            }
        }

        private async Task KovetkezoLepes()
        {
            var sor = szimulaciosLog[aktualisLogIndex];
            int seb = int.Parse(sor[5]);

            if (sor[8] == "Bányászat")
            {
                Terkep.Grid[DénesRover.Pozicio.X][DénesRover.Pozicio.Y] = ".";
                FrissitMegjelenites();
                UpdateStatusUI(sor);
                await Task.Delay(200);
            }
            else
            {
                for (int i = 0; i < seb; i++)
                {
                    if (aktualisUtvonalIndex < utvonalPontok.Count)
                    {
                        DénesRover.Pozicio = utvonalPontok[aktualisUtvonalIndex++];
                        UpdateStatusUI(sor);
                        FrissitMegjelenites();
                        await Task.Delay(100);
                    }
                }
            }
            aktualisLogIndex++;
        }

        private void UpdateStatusUI(string[] d)
        {
            StatText.Text = $"Idő: {d[1]} | Akku: {d[4]}% | Ásvány: {d[7]} | Táv: {d[6]} | Status: {d[8]}";
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
            _ => new SolidColorBrush(Color.FromRgb(210, 105, 30))
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
            // Mivel a dashboard már a main része, ez a gomb most csak frissítheti az adatokat
            BetoltDashboardAdatok();
        }
    }
}