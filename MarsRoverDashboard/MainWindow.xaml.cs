using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using LiveCharts.Defaults;
using System.Globalization;

namespace MarsRoverDashboard
{
    public partial class MainWindow : Window
    {
        public SeriesCollection AkkuSeries { get; set; }
        public SeriesCollection AsvanySeries { get; set; }
        public SeriesCollection StatuszSeries { get; set; }
        public SeriesCollection SebessegSeries { get; set; }
        public SeriesCollection NapszakSeries { get; set; }
        public SeriesCollection TeljesitmenySeries { get; set; }

        public string[] SebessegLabels { get; set; }
        public string[] TeljesitmenyLabels { get; set; }
        public Func<double, string> XFormatter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            XFormatter = val => val.ToString("F1");

            AkkuSeries = new SeriesCollection();
            AsvanySeries = new SeriesCollection();
            StatuszSeries = new SeriesCollection();
            SebessegSeries = new SeriesCollection();
            NapszakSeries = new SeriesCollection();
            TeljesitmenySeries = new SeriesCollection();

            SebessegLabels = new[] { "1-es", "2-es", "3-as" };
            TeljesitmenyLabels = new[] { "Vízjég", "Arany", "Ritka" };

            BetoltAdatok();
            this.DataContext = this;
        }

        private void BetoltAdatok()
        {
            try
            {
                if (!File.Exists("rover_log.csv")) return;

                var logSorok = File.ReadAllLines("rover_log.csv");
                var akkuPontok = new ChartValues<ObservablePoint>();
                var asvanyPontok = new ChartValues<ObservablePoint>();

                int s1 = 0, s2 = 0, s3 = 0;
                int nBany = 0, eBany = 0;
                int hal = 0, bany = 0, haz = 0;

                for (int i = 1; i < logSorok.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(logSorok[i])) continue;
                    var d = logSorok[i].Split(';');

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
                    else if (d[8] == "Haladás") hal++; else haz++;
                }

                AkkuSeries.Add(new LineSeries { Title = "Akku %", Values = akkuPontok, PointGeometry = null, Fill = Brushes.Transparent });
                AsvanySeries.Add(new LineSeries { Title = "Ásvány", Values = asvanyPontok, PointGeometry = null });

                Func<ChartPoint, string> pieLabel = chartPoint => $"{chartPoint.Y} db";
                StatuszSeries.Add(new PieSeries { Title = "Haladás", Values = new ChartValues<int> { hal }, DataLabels = true, LabelPoint = pieLabel });
                StatuszSeries.Add(new PieSeries { Title = "Bányászat", Values = new ChartValues<int> { bany }, DataLabels = true, LabelPoint = pieLabel });
                StatuszSeries.Add(new PieSeries { Title = "Haza", Values = new ChartValues<int> { haz }, DataLabels = true, LabelPoint = pieLabel });

                SebessegSeries.Add(new ColumnSeries { Title = "Gyakoriság", Values = new ChartValues<int> { s1, s2, s3 }, DataLabels = true, LabelPoint = p => p.Y.ToString() });

                NapszakSeries.Add(new StackedColumnSeries
                {
                    Title = "Nappal",
                    Values = new ChartValues<int> { nBany, 0 },
                    DataLabels = true,
                    LabelPoint = p => p.Y > 0 ? p.Y.ToString() : "",
                });
                NapszakSeries.Add(new StackedColumnSeries
                {
                    Title = "Éjjel",
                    Values = new ChartValues<int> { 0, eBany },
                    DataLabels = true,
                    LabelPoint = p => p.Y > 0 ? p.Y.ToString() : "",
                });

                if (File.Exists("rover_summary.csv"))
                {
                    var sumSorok = File.ReadAllLines("rover_summary.csv");
                    var sd = sumSorok[1].Split(';');
                    int gV = int.Parse(sd[3]), oV = int.Parse(sd[4]);
                    int gA = int.Parse(sd[5]), oA = int.Parse(sd[6]);
                    int gR = int.Parse(sd[7]), oR = int.Parse(sd[8]);

                    TeljesitmenySeries.Add(new RowSeries { Title = "Sikeres", Values = new ChartValues<int> { gV, gA, gR }, DataLabels = true, LabelPoint = p => p.X.ToString() });
                    TeljesitmenySeries.Add(new RowSeries { Title = "Veszteség", Values = new ChartValues<int> { oV - gV, oA - gA, oR - gR }, DataLabels = true, LabelPoint = p => p.X.ToString() });
                }
            }
            catch (Exception ex) { MessageBox.Show("Hiba: " + ex.Message); }
        }
    }
}