using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace Vadász_Mars_Dénes
{
    public partial class MainWindow : Window
    {
        public MarsMap Terkep { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            BetoltEsMegjelenit();
        }

        private void BetoltEsMegjelenit()
        {
            Terkep = new MarsMap();
            string path = "mars_map_50x50.csv";

            try
            {
                Terkep.LoadFromFile(path);

                List<MapCell> cells = new List<MapCell>();
                string kordinata = "";
                // Bejárjuk a 50x50-es rácsot
                for (int i = 0; i < 50; i++)
                {
                    for (int j = 0; j < 50; j++)
                    {
                        string jel = Terkep.Grid[i][j];
                        kordinata = $"[{i};{j}]";
                        cells.Add(new MapCell
                        {
                            Color = GetColorBySign(jel),
                            Type = GetNameBySign(jel),
                            Coordinates = $"[{i};{j}]"
                        });
                    }
                }

                // Átadjuk az adatokat a felületnek
                MapDisplay.ItemsSource = cells;

                // Statisztika frissítése
                StatText.Text = $"Arany (Y): {Terkep.RitkaArany.Count} | " +
                               $"Ásvány (G): {Terkep.RitkaAsvany.Count} | " +
                               $"Vízjég (B): {Terkep.Vizjeg.Count} | " +
                               $"Akadály (#): {Terkep.Akadalyok.Count}" +
                               $"Koordináta: {kordinata}"
                               ;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba a betöltés során: " + ex.Message);
            }
        }

        // Színválasztó logika a karakterek alapján
        private Brush GetColorBySign(string sign)
        {
            return sign switch
            {
                "#" => Brushes.DarkGray,          // Akadály
                "Y" => Brushes.Gold,           // Ritka Arany
                "B" => Brushes.DeepSkyBlue,    // Vízjég
                "G" => Brushes.LimeGreen,      // Ritka Ásvány
                "S" => Brushes.Red,            // Rover kezdőpontja
                _ => new SolidColorBrush(Color.FromRgb(210, 105, 30)) // "Csokibarna" a Mars talajnak
            };
        }

        private string GetNameBySign(string sign) => sign switch
        {
            "#" => "Szikla/Akadály",
            "Y" => "Arany",
            "B" => "Vízjég",
            "G" => "Ásvány",
            "S" => "Start (Dénes Rover)",
            _ => "Mars talaj"
        };
    }
}