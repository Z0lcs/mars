using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Mars;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;

namespace Vadász_Mars_Dénes
{
    public partial class MainWindow : Window
    {
        // --- TÉRKÉP VÁLTOZÓK ---
        public ObservableCollection<MapCell> MapCells { get; set; } = new ObservableCollection<MapCell>();
        private static Dictionary<string, BitmapImage> ImageCache = new Dictionary<string, BitmapImage>();
        private List<string[]> szimulaciosLog = new List<string[]>();
        private List<Point> utvonalPontok = new List<Point>();
        private int aktualisLogIndex = 0;
        private int aktualisUtvonalIndex = 0;
        private bool folyamatban = false;
        private bool autoLejatszas = false;

        public MarsMap Terkep { get; set; }
        public Rover DénesRover { get; set; }

        // --- DASHBOARD VÁLTOZÓK ---
        public SeriesCollection AkkuSeries { get; set; }
        public SeriesCollection AsvanySeries { get; set; }
        public SeriesCollection StatuszSeries { get; set; }
        public SeriesCollection SebessegSeries { get; set; }
        public SeriesCollection NapszakSeries { get; set; }
        public SeriesCollection TeljesitmenySeries { get; set; }

        public string[] SebessegLabels { get; set; }
        public string[] TeljesitmenyLabels { get; set; }
        public Func<double, string> XFormatter { get; set; }

        // --- ÉLŐ ADATPONTOK (A lépések során ezek növekednek) ---
        private ChartValues<ObservablePoint> akkuPts = new ChartValues<ObservablePoint>();
        private ChartValues<ObservablePoint> asvanyPts = new ChartValues<ObservablePoint>();
        private ChartValues<int> sebessegVals = new ChartValues<int> { 0, 0, 0 };
        private ChartValues<int> haladasVal = new ChartValues<int> { 0 };
        private ChartValues<int> banyaszatVal = new ChartValues<int> { 0 };
        private ChartValues<int> hazaVal = new ChartValues<int> { 0 };
        private ChartValues<int> nBanyVals = new ChartValues<int> { 0, 0 }; // Nappal
        private ChartValues<int> eBanyVals = new ChartValues<int> { 0, 0 }; // Éjjel
        private ChartValues<int> sikeresVals = new ChartValues<int> { 0, 0, 0 }; 
        private ChartValues<int> maradtVals = new ChartValues<int> { 0, 0, 0 };
        private int maxOraErtek = 0; 

        public MainWindow()
        {
            InitializeComponent();

            XFormatter = val => val.ToString("F0") + "h";
            AkkuSeries = new SeriesCollection();
            AsvanySeries = new SeriesCollection();
            StatuszSeries = new SeriesCollection();
            SebessegSeries = new SeriesCollection();
            NapszakSeries = new SeriesCollection();
            TeljesitmenySeries = new SeriesCollection();

            SebessegLabels = new[] { "1", "2", "3" };
            TeljesitmenyLabels = new[] { "Vízjég", "Arany", "Ritka" };

            DataContext = this;

            this.Loaded += MainWindow_Loaded;
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string settingsFajl = "last_settings.txt";
            if (File.Exists(settingsFajl))
            {
                try
                {
                    string[] sorok = File.ReadAllLines(settingsFajl);
                    string mapFajl = sorok[0];
                    string mentesiMappa = sorok[1];
                    int maxOra = int.Parse(sorok[2]);
                    maxOraErtek = maxOra;

                    Terkep = new MarsMap();
                    Terkep.LoadFromFile(mapFajl);

                    string teljesLogPath = Path.Combine(mentesiMappa, "rover_log.csv");
                    string teljesPathFile = Path.Combine(mentesiMappa, "rover_path.csv");

                    RoverVezerlo vezerlo = new RoverVezerlo(Terkep, maxOra, teljesLogPath, false);
                    vezerlo.SzimulacioInditasa();

                    BetoltLogEsUtvonal(teljesLogPath, teljesPathFile);
                    DénesRover = new Rover { Pozicio = Terkep.KezdoPont };

                    InicializalasMap();
                    EredmenyekElokeszitese(mentesiMappa);

                    StatText.Text = "Kész! Indítsd el a lejátszást.";
                    AutoPlayGomb.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hiba a betöltéskor: " + ex.Message);
                }
            }
        }

        private BitmapImage GetCachedImage(string fileName)
        {
            string path = $"pack://application:,,,/képek/{fileName}";
            if (!ImageCache.ContainsKey(path))
            {
                try { var img = new BitmapImage(new Uri(path)); img.Freeze(); ImageCache[path] = img; }
                catch { return null; }
            }
            return ImageCache[path];
        }

        private void InicializalasMap()
        {
            MapCells.Clear();
            for (int i = 0; i < 50; i++)
                for (int j = 0; j < 50; j++)
                    MapCells.Add(new MapCell
                    {
                        ImageSource = GetImageForSign(Terkep.Grid[i][j], i, j),
                        Coordinates = $"[{i};{j}]",
                        Type = Terkep.Grid[i][j],
                        HasVisited = false
                    });
            MapDisplay.ItemsSource = MapCells;
        }

        private ImageSource GetImageForSign(string sign, int x, int y)
        {
            if (DénesRover.Pozicio.X == x && DénesRover.Pozicio.Y == y)
            {
                return GetCachedImage("rover_egyenesben.png");
            }
            if (Terkep.KezdoPont.X == x && Terkep.KezdoPont.Y == y)
            {
                return GetCachedImage("Brit.png");
            }
            return sign switch
            {
                "Y" => GetCachedImage("ritkaarany_talajon.png"),
                "B" => GetCachedImage("vízjég_talajon.png"),
                "G" => GetCachedImage("ritkaasvany_talajon.png"),
                "#" => GetCachedImage("fal_talajon.png"),
                _ => GetCachedImage("talaj.png")
            };
        }

        private void FrissitCella(int x, int y)
        {
            int index = x * 50 + y;
            if (index >= 0 && index < MapCells.Count)
                MapCells[index].ImageSource = GetImageForSign(Terkep.Grid[x][y], x, y);
        }

        private async void AutoPlay_Click(object sender, RoutedEventArgs e)
        {
            if (aktualisLogIndex >= szimulaciosLog.Count)
            {
                ResetSzimulacio(); // Mindent alaphelyzetbe állítunk
                autoLejatszas = false;
            }
            else
            {
                autoLejatszas = !autoLejatszas;
            }

            if (autoLejatszas)
            {
                AutoPlayGomb.Content = "⏸ Szünet";
                AutoPlayGomb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E74C3C"));

                while (autoLejatszas && aktualisLogIndex < szimulaciosLog.Count)
                {
                    await EgyLepesMegtetele();

                    int dinamikusDelay = (int)(200 / speedSlider.Value);
                    await Task.Delay(dinamikusDelay);
                }

                if (aktualisLogIndex >= szimulaciosLog.Count)
                {
                    AutoPlayGomb.Content = "🔄 Újraindítás";
                    AutoPlayGomb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                    autoLejatszas = false;
                }
            }
            else
            {
                AutoPlayGomb.Content = "▶ Lejátszás";
                AutoPlayGomb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27AE60"));
            }
        }

        private void ResetSzimulacio()
        {
            aktualisLogIndex = 0;
            aktualisUtvonalIndex = 0;
            folyamatban = false;

            DénesRover.Pozicio = Terkep.KezdoPont;

            string settingsFajl = "last_settings.txt";
            if (File.Exists(settingsFajl))
            {
                string[] sorok = File.ReadAllLines(settingsFajl);
                Terkep.LoadFromFile(sorok[0]); // Újraolvassuk az eredeti CSV-t
                string mentesiMappa = sorok[1];

                akkuPts.Clear();
                asvanyPts.Add(new ObservablePoint(0, 0)); // Kezdőpont az ásványnak
                akkuPts.Add(new ObservablePoint(0, 100)); // Kezdőpont az akkunak

                sebessegVals[0] = 0; sebessegVals[1] = 0; sebessegVals[2] = 0;
                haladasVal[0] = 0;
                banyaszatVal[0] = 0;
                hazaVal[0] = 0;
                nBanyVals[0] = 0; nBanyVals[1] = 0;
                eBanyVals[0] = 0; eBanyVals[1] = 0;

                EredmenyekElokeszitese(mentesiMappa);

                InicializalasMap();
            }
        }

        private void FrissitAutoPlayGombAllapot()
        {
            if (aktualisLogIndex >= szimulaciosLog.Count)
            {
                AutoPlayGomb.Content = "🔄 Újraindítás";
                AutoPlayGomb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB"));
                autoLejatszas = false;
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            autoLejatszas = false;

            SetupWindow setupAblak = new SetupWindow();

            setupAblak.Show();

            this.Close();
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W && !autoLejatszas)
            {
                if (aktualisLogIndex < szimulaciosLog.Count)
                {
                    await EgyLepesMegtetele();
                    FrissitAutoPlayGombAllapot();
                }
            }
        }

        private async Task EgyLepesMegtetele()
        {
            if (folyamatban || aktualisLogIndex >= szimulaciosLog.Count) return;
            folyamatban = true;

            var sor = szimulaciosLog[aktualisLogIndex];
            int seb = int.Parse(sor[5]);
            string napszak = sor.Length > 9 ? sor[9].Trim() : "Nappal";

            // --- NAPSZAK VIZUÁLIS JELZÉSE ---
            if (napszak == "Nappal")
            {
                NapszakIndikator.Fill = Brushes.Yellow;
                NapszakSzoveg.Text = "☀️ NAPPAL";
                NapszakSzoveg.Foreground = Brushes.Yellow;
            }
            else
            {
                NapszakIndikator.Fill = Brushes.DarkBlue;
                NapszakSzoveg.Text = "🌙 ÉJJEL";
                NapszakSzoveg.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A9CCE3"));
            }

            if (sor[8] == "Bányászat")
            {
                string banyaszottTipus = Terkep.Grid[DénesRover.Pozicio.X][DénesRover.Pozicio.Y];

                if (banyaszottTipus == "B") { sikeresVals[0]++; maradtVals[0]--; }
                else if (banyaszottTipus == "Y") { sikeresVals[1]++; maradtVals[1]--; }
                else if (banyaszottTipus == "G" || banyaszottTipus == "#") { sikeresVals[2]++; maradtVals[2]--; }

                Terkep.Grid[DénesRover.Pozicio.X][DénesRover.Pozicio.Y] = ".";
                FrissitCella(DénesRover.Pozicio.X, DénesRover.Pozicio.Y);
            }
            else
            {
                for (int i = 0; i < seb && aktualisUtvonalIndex < utvonalPontok.Count; i++)
                {
                    Point regi = DénesRover.Pozicio;
                    int regiIndex = regi.X * 50 + regi.Y;
                    if (regiIndex >= 0 && regiIndex < MapCells.Count)
                        MapCells[regiIndex].HasVisited = true;

                    DénesRover.Pozicio = utvonalPontok[aktualisUtvonalIndex++];
                    FrissitCella(regi.X, regi.Y);
                    FrissitCella(DénesRover.Pozicio.X, DénesRover.Pozicio.Y);

                    // Dinamikus késleltetés a robot mozgásához (Alap: 500ms)
                    await Task.Delay((int)(500 / speedSlider.Value));
                }
            }

            double oraVal = 0;
            if (sor[1].Contains(":"))
            {
                var reszek = sor[1].Split(':');
                oraVal = double.Parse(reszek[0]) + (double.Parse(reszek[1]) / 60.0);
            }

            akkuPts.Add(new ObservablePoint(oraVal, double.Parse(sor[4], CultureInfo.InvariantCulture)));
            asvanyPts.Add(new ObservablePoint(oraVal, double.Parse(sor[7], CultureInfo.InvariantCulture)));

            if (seb >= 1 && seb <= 3) sebessegVals[seb - 1]++;

            string statusz = sor[8];

            if (statusz == "Bányászat")
            {
                banyaszatVal[0]++;
                if (napszak == "Nappal") nBanyVals[0]++; else eBanyVals[1]++;
            }
            else if (statusz.Contains("Haladás")) haladasVal[0]++;
            else hazaVal[0]++;

            if (StatText != null)
                StatText.Text = $"Idő: {sor[1]} / {maxOraErtek}:00 | Akku: {sor[4]}% | Státusz: {sor[8]} \nMegtett távolság: {sor[6]} blokk  | Ásványok: {sor[7]}db";

            aktualisLogIndex++;
            folyamatban = false;
        }

        private void BetoltLogEsUtvonal(string logPath, string pathFile)
        {
            if (File.Exists(logPath))
                szimulaciosLog = File.ReadAllLines(logPath).Skip(1).Select(s => s.Split(';')).ToList();

            if (File.Exists(pathFile))
                utvonalPontok = File.ReadAllLines(pathFile).Skip(1)
                    .Select(l => l.Split(';'))
                    .Select(p => new Point(int.Parse(p[1]), int.Parse(p[2]))).ToList();
        }

        private void EredmenyekElokeszitese(string mappa)
        {
            try
            {
                AkkuSeries.Clear();
                AkkuSeries.Add(new LineSeries { Title = "Akku %", Values = akkuPts, PointGeometry = null, Fill = Brushes.Transparent });

                AsvanySeries.Clear();
                AsvanySeries.Add(new LineSeries { Title = "Ásvány", Values = asvanyPts, PointGeometry = null });

                Func<ChartPoint, string> pieLabel = chartPoint => $"{chartPoint.Y} db";
                StatuszSeries.Clear();
                StatuszSeries.Add(new PieSeries { Title = "Haladás", Values = haladasVal, DataLabels = true, LabelPoint = pieLabel });
                StatuszSeries.Add(new PieSeries { Title = "Bányászat", Values = banyaszatVal, DataLabels = true, LabelPoint = pieLabel });
                StatuszSeries.Add(new PieSeries { Title = "Haza", Values = hazaVal, DataLabels = true, LabelPoint = pieLabel });

                SebessegSeries.Clear();
                SebessegSeries.Add(new ColumnSeries { Title = "Gyakoriság", Values = sebessegVals, DataLabels = true, LabelPoint = p => p.Y.ToString(), LabelsPosition = BarLabelPosition.Parallel, Foreground = Brushes.White });

                NapszakSeries.Clear();  
                NapszakSeries.Add(new StackedColumnSeries { Title = "Nappal", Values = nBanyVals, DataLabels = true, LabelPoint = p => p.Y > 0 ? p.Y.ToString() : ""});
                NapszakSeries.Add(new StackedColumnSeries { Title = "Éjjel", Values = eBanyVals, DataLabels = true, LabelPoint = p => p.Y > 0 ? p.Y.ToString() : ""});
                    
                string sumFajl = Path.Combine(mappa, "rover_summary.csv");
                if (File.Exists(sumFajl))
                {
                    var sumSorok = File.ReadAllLines(sumFajl);
                    if (sumSorok.Length > 1)
                    {
                        var sd = sumSorok[1].Split(';');

                        int oV = int.Parse(sd[4]);
                        int oA = int.Parse(sd[6]);
                        int oR = int.Parse(sd[8]);

                        sikeresVals.Clear(); sikeresVals.AddRange(new int[] { 0, 0, 0 });
                        maradtVals.Clear(); maradtVals.AddRange(new int[] { oV, oA, oR });

                        TeljesitmenySeries.Clear();
                        TeljesitmenySeries.Add(new RowSeries { Title = "Begyűjtött", Values = sikeresVals, DataLabels = true, LabelPoint = p => p.X.ToString(),LabelsPosition = BarLabelPosition.Parallel});
                        TeljesitmenySeries.Add(new RowSeries { Title = "Maradék", Values = maradtVals, DataLabels = true, LabelPoint = p => p.X.ToString(), LabelsPosition = BarLabelPosition.Parallel, Foreground = Brushes.White });
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Hiba a dashboard előkészítésekor: " + ex.Message); }
        }
    }
}
