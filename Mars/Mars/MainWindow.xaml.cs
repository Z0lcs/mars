using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Drawing; // A Point típushoz
using Point = System.Drawing.Point;

namespace Vadász_Mars_Dénes
{
    public partial class MainWindow : Window
    {
        public MarsMap Terkep { get; set; }
        public Rover DénesRover { get; set; }
        public IdoKezelo Ido { get; set; }

        private double osszesTavolsag = 0;
        private bool szimulacioVége = false;

        public MainWindow()
        {
            InitializeComponent();

            // Inicializálás
            Terkep = new MarsMap();
            Terkep.LoadFromFile("mars_map_50x50.csv");

            DénesRover = new Rover();
            DénesRover.Pozicio = Terkep.KezdoPont;

            Ido = new IdoKezelo(24); // 72 fél óra = 36 óra szimuláció

            // Első megjelenítés
            FrissitMegjelenites();

            // Feliratkozás a gombnyomásra
            this.KeyDown += MainWindow_KeyDown;

            Point celpont = Terkep.KezdoPont;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && Ido.FuthatMegAProgram && !szimulacioVége)
            {
                LepesSzimulacio();
                FrissitMegjelenites();
            }
        }

        private void LepesSzimulacio()
        {
            bool elindultValaha = false;
            bool nappal = Ido.NappalVanE();
            string aktualisStatusz = "Várakozás";
            int tenylegesSebesseg = 0;
            bool banyaszik = false;
            Point celpont = Terkep.KezdoPont;

            var asvanyok = Terkep.Vizjeg.Concat(Terkep.RitkaArany).Concat(Terkep.RitkaAsvany).ToList();
            int tavBazisig = Math.Max(Math.Abs(DénesRover.Pozicio.X - Terkep.KezdoPont.X),
                                      Math.Abs(DénesRover.Pozicio.Y - Terkep.KezdoPont.Y));
            if (tavBazisig > 0) elindultValaha = true;

            double hatralevoIdo = (Ido.teljesIdo * 2) - Ido.ElteltFelOra;
            bool vészhelyzet = (tavBazisig > 0) && (tavBazisig >= (hatralevoIdo - 1.5));
            if (vészhelyzet)
            {
                celpont = Terkep.KezdoPont;
                aktualisStatusz = "VÉSZ-HAZATÉRÉS";
            }
            else if (asvanyok.Any(a => a.X == DénesRover.Pozicio.X && a.Y == DénesRover.Pozicio.Y))
            {
                banyaszik = true;
                aktualisStatusz = "Bányászat";
                Terkep.Grid[DénesRover.Pozicio.X][DénesRover.Pozicio.Y] = ".";
            }
            else if (asvanyok.Any())
            {
                celpont = asvanyok.OrderBy(a =>
                {
                    double tav = Math.Max(Math.Abs(a.X - DénesRover.Pozicio.X), Math.Abs(a.Y - DénesRover.Pozicio.Y));
                    int suruseg = asvanyok.Count(m => Math.Abs(m.X - a.X) <= 1 && Math.Abs(m.Y - a.Y) <= 1);
                    double ertek = (Terkep.RitkaArany.Contains(a) || Terkep.RitkaAsvany.Contains(a)) ? 5.0 : 0.0;
                    return tav - (suruseg * 0.725) - ertek;
                }).First();
                aktualisStatusz = "Haladás";
            }
            if (banyaszik)
            {
                DénesRover.OsszegyujtottAsvany++;
                Terkep.Vizjeg.RemoveAll(p => p.X == DénesRover.Pozicio.X && p.Y == DénesRover.Pozicio.Y);
                Terkep.RitkaArany.RemoveAll(p => p.X == DénesRover.Pozicio.X && p.Y == DénesRover.Pozicio.Y);
                Terkep.RitkaAsvany.RemoveAll(p => p.X == DénesRover.Pozicio.X && p.Y == DénesRover.Pozicio.Y);
            }
            else
            {
                int maxV = (vészhelyzet) ? ((DénesRover.Akkumulator > 55) ? 3 : 2) :
                                           ((nappal && DénesRover.Akkumulator > 35) ? 2 : 1);
                int tavCelpontig = Math.Max(Math.Abs(celpont.X - DénesRover.Pozicio.X), Math.Abs(celpont.Y - DénesRover.Pozicio.Y));
                tenylegesSebesseg = Math.Min(maxV, tavCelpontig);

                for (int s = 0; s < tenylegesSebesseg; s++)
                {
                    Point x = KeresLegjobbSzomszed(DénesRover.Pozicio, celpont, Terkep);
                    if (x != DénesRover.Pozicio)
                    {
                        DénesRover.Pozicio = x;
                        osszesTavolsag++;
                    }
                    if (Terkep.Vizjeg.Any(a => a == DénesRover.Pozicio) ||
                        Terkep.RitkaArany.Any(a => a == DénesRover.Pozicio) ||
                        Terkep.RitkaAsvany.Any(a => a == DénesRover.Pozicio)) break;
                }
            }
            double energiaValtozas = DénesRover.SzamolFogyasztas(tenylegesSebesseg, nappal, banyaszik);
            DénesRover.FrissitEnergia(energiaValtozas);
            Ido.Lepes();

            if (DénesRover.Akkumulator <= 0)
            {
                MessageBox.Show("Küldetés sikertelen! A rover nem ért hazaért.");
            }
            if (vészhelyzet && DénesRover.Pozicio == Terkep.KezdoPont && elindultValaha)
            {
                szimulacioVége = true;
                MessageBox.Show("Küldetés teljesítve! A rover hazaért.");
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
                        !terkep.Akadalyok.Any(a => a.X == vizsgalt.X && a.Y == vizsgalt.Y))
                    {
                        double tav = Math.Sqrt(Math.Pow(vizsgalt.X - cel.X, 2) + Math.Pow(vizsgalt.Y - cel.Y, 2));
                        if (tav < minTav) { minTav = tav; legjobb = vizsgalt; }
                    }
                }
            }
            return legjobb;
        }

        private void FrissitMegjelenites()
        {
            List<MapCell> cells = new List<MapCell>();

            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 50; j++)
                {
                    Point aktualisPont = new Point(i, j);
                    string jel = Terkep.Grid[i][j];

                    // Ha a rover itt van, felülbíráljuk a színt
                    if (DénesRover.Pozicio.X == i && DénesRover.Pozicio.Y == j)
                    {
                        cells.Add(new MapCell
                        {
                            Color = Brushes.White, // Fehér jelzi a Rovert
                            Type = "ROVER",
                            Coordinates = $"[{i};{j}]"
                        });
                    }
                    else
                    {
                        cells.Add(new MapCell
                        {
                            Color = GetColorBySign(jel),
                            Type = GetNameBySign(jel),
                            Coordinates = $"[{i};{j}]"
                        });
                    }
                }
            }

            MapDisplay.ItemsSource = cells;

            // Statisztika frissítése a UI-on
            StatText.Text = $"Idő: {Ido.ElteltFelOra * 0.5}ó ({(Ido.NappalVanE() ? "Nappal" : "Éjszaka")}) | " +
                           $"Akku: {DénesRover.Akkumulator:F1}% | " +
                           $"Ásvány: {DénesRover.OsszegyujtottAsvany} | " +
                           $"Táv: {osszesTavolsag} | " +
                           $"Poz: [{DénesRover.Pozicio.X};{DénesRover.Pozicio.Y}]";
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
            "#" => "Szikla/Akadály",
            "Y" => "Arany",
            "B" => "Vízjég",
            "G" => "Ásvány",
            "S" => "Start",
            _ => "Mars talaj"
        };
    }

}