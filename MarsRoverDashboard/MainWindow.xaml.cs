using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;

namespace MarsRoverDashboard
{
    public partial class MainWindow : Window
    {
        // Az összes diagram property-je
        public SeriesCollection AkkuSeries { get; set; }
        public SeriesCollection AsvanySeries { get; set; }
        public SeriesCollection StatuszSeries { get; set; }
        public SeriesCollection SebessegSeries { get; set; }
        public SeriesCollection NapszakSeries { get; set; }
        public SeriesCollection TeljesitmenySeries { get; set; }

        public Func<double, string> XFormatter { get; set; }
        public string[] SebessegLabels { get; set; }
        public string[] NapszakLabels { get; set; }
        public string[] TeljesitmenyLabels { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // Kollekciók inicializálása
            AkkuSeries = new SeriesCollection();
            AsvanySeries = new SeriesCollection();
            StatuszSeries = new SeriesCollection();
            SebessegSeries = new SeriesCollection();
            NapszakSeries = new SeriesCollection();
            TeljesitmenySeries = new SeriesCollection();

            XFormatter = val => val.ToString("F1") + "h";
            SebessegLabels = new[] { "1-es Seb", "2-es Seb", "3-as Seb" };
            NapszakLabels = new[] { "Nappal", "Éjszaka" };
            TeljesitmenyLabels = new[] { "Siker vs Kudarc" };

            // CSV fájl betöltése (legyen a bin/Debug mappában, ahonnan fut a program!)
            string logPath = "rover_log.csv";

            BetoltEsAggregal(logPath);

            // Adatkötés aktiválása
            DataContext = this;
        }

        private void BetoltEsAggregal(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("Nem található a rover_log.csv fájl! Másold be a futtatható .exe mellé.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var akkuPontok = new ChartValues<ObservablePoint>();
            var asvanyPontok = new ChartValues<ObservablePoint>();

            int haladasDb = 0, banyaszatDb = 0, veszDb = 0;
            int seb1 = 0, seb2 = 0, seb3 = 0;
            int asvanyNappal = 0, asvanyEjszaka = 0;

            string[] sorok = File.ReadAllLines(path);
            int kibanyaszottVegleges = 0;

            for (int i = 1; i < sorok.Length; i++)
            {
                var adatok = sorok[i].Split(';');
                if (adatok.Length < 9) continue;

                // --- 1. Idő (X tengely) ---
                double idoOra = 0;
                string idoSzoveg = adatok[1];
                if (idoSzoveg.Contains(":"))
                {
                    var reszek = idoSzoveg.Split(':');
                    idoOra = int.Parse(reszek[0]) + (int.Parse(reszek[1]) / 60.0);
                }
                else
                {
                    double.TryParse(idoSzoveg.Replace('.', ','), out idoOra);
                }

                // --- Vonaldiagram adatok ---
                double akku = double.Parse(adatok[4]);
                double asvany = double.Parse(adatok[7]);
                kibanyaszottVegleges = (int)asvany; // Ezt a végén fogjuk felhasználni

                akkuPontok.Add(new ObservablePoint(idoOra, akku));
                asvanyPontok.Add(new ObservablePoint(idoOra, asvany));

                // --- Státusz Számláló (Kördiagram) ---
                string statusz = adatok[8];
                if (statusz == "Haladás") haladasDb++;
                else if (statusz == "Bányászat") banyaszatDb++;
                else if (statusz.Contains("VÉSZ")) veszDb++;

                // --- Sebesség Számláló (Oszlop) ---
                int seb = int.Parse(adatok[5]);
                if (seb == 1) seb1++;
                else if (seb == 2) seb2++;
                else if (seb == 3) seb3++;

                // --- Napszak Számláló (Rakott Oszlop) ---
                string napszak = adatok[9].Trim();
                if (statusz == "Bányászat")
                {
                    if (napszak == "Nappal") asvanyNappal++;
                    else if (napszak == "Ejszaka" || napszak == "Éjszaka") asvanyEjszaka++;
                }
            }

            // --- 1. Akku Trend ---
            AkkuSeries.Add(new LineSeries { Title = "Akku", Values = akkuPontok, PointGeometrySize = 0, Stroke = Brushes.Crimson, Fill = Brushes.Transparent, LineSmoothness = 0.3 });

            // --- 2. Ásvány Trend ---
            AsvanySeries.Add(new LineSeries { Title = "Ásvány", Values = asvanyPontok, PointGeometrySize = 0, Stroke = Brushes.DodgerBlue, Fill = new SolidColorBrush(Color.FromArgb(90, 30, 144, 255)), LineSmoothness = 0 });

            // --- 3. Státusz (Kör) ---
            StatuszSeries.Add(new PieSeries { Title = "Haladás", Values = new ChartValues<int> { haladasDb }, DataLabels = true, Fill = Brushes.Gray });
            StatuszSeries.Add(new PieSeries { Title = "Bányászat", Values = new ChartValues<int> { banyaszatDb }, DataLabels = true, Fill = Brushes.Cyan });
            StatuszSeries.Add(new PieSeries { Title = "Hazatérés", Values = new ChartValues<int> { veszDb }, DataLabels = true, Fill = Brushes.Magenta });

            // --- 4. Sebesség (Oszlop) ---
            SebessegSeries.Add(new ColumnSeries { Title = "Alkalom", Values = new ChartValues<int> { seb1, seb2, seb3 }, Fill = Brushes.Orange, DataLabels = true });

            // --- 5. Nappal/Éjszaka (Rakott Oszlop) ---
            NapszakSeries.Add(new StackedColumnSeries { Title = "Nappali Bányászat", Values = new ChartValues<int> { asvanyNappal, 0 }, DataLabels = true, Fill = Brushes.Gold });
            NapszakSeries.Add(new StackedColumnSeries { Title = "Éjszakai Bányászat", Values = new ChartValues<int> { 0, asvanyEjszaka }, DataLabels = true, Fill = Brushes.DarkBlue });

            // --- 6. Küldetés Teljesítése (Sárga/Szürke) ---
            int osszesAsvany = 67;
            int kimaradt = Math.Max(0, osszesAsvany - kibanyaszottVegleges);

            TeljesitmenySeries.Add(new StackedColumnSeries { Title = "Kibányászott", Values = new ChartValues<int> { kibanyaszottVegleges }, DataLabels = true, Fill = Brushes.Gold });
            TeljesitmenySeries.Add(new StackedColumnSeries { Title = "Hátrahagyott", Values = new ChartValues<int> { kimaradt }, DataLabels = true, Fill = Brushes.LightGray });
        }
    }
}