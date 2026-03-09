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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;

namespace Vadász_Mars_Dénes
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<MapCell> MapCells { get; set; } = new ObservableCollection<MapCell>();
        private static Dictionary<string, BitmapImage> ImageCache = new Dictionary<string, BitmapImage>();

        public SeriesCollection AkkuSeries { get; set; } = new SeriesCollection();
        public SeriesCollection AsvanySeries { get; set; } = new SeriesCollection();
        public SeriesCollection StatuszSeries { get; set; } = new SeriesCollection();
        public SeriesCollection SebessegSeries { get; set; } = new SeriesCollection();

        private List<string[]> szimulaciosLog = new List<string[]>();
        private List<Point> utvonalPontok = new List<Point>();
        private int aktualisLogIndex = 0;
        private int aktualisUtvonalIndex = 0;
        private bool folyamatban = false;
        public MarsMap Terkep { get; set; }
        public Rover DénesRover { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Terkep = new MarsMap();
            try { Terkep.LoadFromFile("mars_map_50x50.csv"); } catch { }

            RoverVezerlo vezerlo = new RoverVezerlo(Terkep, 72, "rover_log.csv", false);
            vezerlo.SzimulacioInditasa();

            BetoltDashboardAdatok();
            BetoltUtvonal();

            DénesRover = new Rover { Pozicio = Terkep.KezdoPont };

            InicializalasMap();
            this.KeyDown += MainWindow_KeyDown;
        }

        private BitmapImage GetCachedImage(string fileName)
        {
            string path = $"pack://application:,,,/képek/{fileName}";
            if (!ImageCache.ContainsKey(path))
            {
                try
                {
                    var img = new BitmapImage(new Uri(path));
                    img.Freeze();
                    ImageCache[path] = img;
                }
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
                        HasVisited = false // Kezdetben senki sem járt sehol
                    });
            MapDisplay.ItemsSource = MapCells;
        }

        private ImageSource GetImageForSign(string sign, int x, int y)
        {
            if (DénesRover.Pozicio.X == x && DénesRover.Pozicio.Y == y)
                return GetCachedImage("Nyíl.jpg");

            return sign switch
            {
                "Y" => GetCachedImage("ritkaarany_talajon.png"),
                "B" => GetCachedImage("vízjég_talajon.png"),
                "G" => GetCachedImage("ritkaasvany_talajon.png"),
                "#" => GetCachedImage("Brit.png"),
                _ => GetCachedImage("talaj.png")
            };
        }

        private void FrissitCella(int x, int y)
        {
            int index = x * 50 + y;
            if (index >= 0 && index < MapCells.Count)
                MapCells[index].ImageSource = GetImageForSign(Terkep.Grid[x][y], x, y);
        }

        private async void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.W && !folyamatban && aktualisLogIndex < szimulaciosLog.Count)
            {
                folyamatban = true;
                var sor = szimulaciosLog[aktualisLogIndex];
                int seb = int.Parse(sor[5]);

                if (sor[8] == "Bányászat")
                {
                    Terkep.Grid[DénesRover.Pozicio.X][DénesRover.Pozicio.Y] = ".";
                    FrissitCella(DénesRover.Pozicio.X, DénesRover.Pozicio.Y);
                }
                else
                {
                    for (int i = 0; i < seb && aktualisUtvonalIndex < utvonalPontok.Count; i++)
                    {
                        Point regi = DénesRover.Pozicio;

                        // Nyomvonal rögzítése a régi ponton
                        int regiIndex = regi.X * 50 + regi.Y;
                        if (regiIndex >= 0 && regiIndex < MapCells.Count)
                            MapCells[regiIndex].HasVisited = true;

                        DénesRover.Pozicio = utvonalPontok[aktualisUtvonalIndex++];
                        FrissitCella(regi.X, regi.Y);
                        FrissitCella(DénesRover.Pozicio.X, DénesRover.Pozicio.Y);
                        await Task.Delay(200);
                    }
                }
                StatText.Text = $"Idő: {sor[1]} | Akku: {sor[4]}% | Státusz: {sor[8]} | Távolság:{sor[6]} | Ásványok:{sor[7]}";
                aktualisLogIndex++;
                folyamatban = false;
            }
        }

        private void BetoltDashboardAdatok()
        {
            if (!File.Exists("rover_log.csv")) return;
            var logSorok = File.ReadAllLines("rover_log.csv").Skip(1).Select(s => s.Split(';')).ToList();
            szimulaciosLog = logSorok;

            var akkuPts = new ChartValues<ObservablePoint>();
            var asvanyPts = new ChartValues<ObservablePoint>();
            int s1 = 0, s2 = 0, s3 = 0, hal = 0, bany = 0, haz = 0;

            foreach (var d in logSorok)
            {
                double ora = d[1].Contains(":") ? double.Parse(d[1].Split(':')[0]) + (double.Parse(d[1].Split(':')[1]) / 60.0) : 0;
                akkuPts.Add(new ObservablePoint(ora, double.Parse(d[4], CultureInfo.InvariantCulture)));
                asvanyPts.Add(new ObservablePoint(ora, double.Parse(d[7], CultureInfo.InvariantCulture)));

                int seb = int.Parse(d[5]);
                if (seb == 1) s1++; else if (seb == 2) s2++; else s3++;
                if (d[8] == "Bányászat") bany++; else if (d[8].Contains("Haladás")) hal++; else haz++;
            }

            AkkuSeries.Clear(); AkkuSeries.Add(new LineSeries { Values = akkuPts, PointGeometry = null });
            AsvanySeries.Clear(); AsvanySeries.Add(new LineSeries { Values = asvanyPts, PointGeometry = null });
            StatuszSeries.Clear();
            StatuszSeries.Add(new PieSeries { Title = "Haladás", Values = new ChartValues<int> { hal } });
            StatuszSeries.Add(new PieSeries { Title = "Bányászat", Values = new ChartValues<int> { bany } });
            StatuszSeries.Add(new PieSeries { Title = "Haza", Values = new ChartValues<int> { haz } });
            SebessegSeries.Clear();
            SebessegSeries.Add(new ColumnSeries { Values = new ChartValues<int> { s1, s2, s3 } });
        }

        private void BetoltUtvonal()
        {
            if (File.Exists("rover_path.csv"))
                utvonalPontok = File.ReadAllLines("rover_path.csv").Skip(1)
                    .Select(l => l.Split(';'))
                    .Select(p => new Point(int.Parse(p[1]), int.Parse(p[2]))).ToList();
        }

        private void StatisztikaGomb_Click(object sender, RoutedEventArgs e) => BetoltDashboardAdatok();
    }
}