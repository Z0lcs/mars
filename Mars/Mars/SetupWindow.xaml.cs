using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Vadász_Mars_Dénes
{
    public partial class SetupWindow : Window
    {
        public string KivalasztottMapPath { get; private set; }
        public string KivalasztottLogMappa { get; private set; }
        public int MegadottMaxOra { get; private set; }

        private readonly string settingsFajl = "last_settings.txt";

        public SetupWindow()
        {
            InitializeComponent();

            BetoltElolozoBeallitasokat();
        }

        private void BetoltElolozoBeallitasokat()
        {
            if (File.Exists(settingsFajl))
            {
                try
                {
                    string[] sorok = File.ReadAllLines(settingsFajl);
                    if (sorok.Length >= 2)
                    {
                        MapPathTextBox.Text = sorok[0];
                        LogFolderPathTextBox.Text = sorok[1];

                        if (sorok.Length >= 3)
                        {
                            TimeTextBox.Text = sorok[2];
                        }
                    }
                }
                catch
                {
                }
            }
        }

        private void TallozasMap_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV fájlok (*.csv)|*.csv|Minden fájl (*.*)|*.*";
            openFileDialog.Title = "Válaszd ki a Mars térképet";

            if (openFileDialog.ShowDialog() == true)
            {
                MapPathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void TallozasMappa_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog folderDialog = new OpenFileDialog();
            folderDialog.ValidateNames = false;
            folderDialog.CheckFileExists = false;
            folderDialog.CheckPathExists = true;
            folderDialog.FileName = "Mappa Kiválasztása";
            folderDialog.Title = "Válaszd ki a logok mentési helyét";

            if (folderDialog.ShowDialog() == true)
            {
                string folderPath = System.IO.Path.GetDirectoryName(folderDialog.FileName);
                LogFolderPathTextBox.Text = folderPath;
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MapPathTextBox.Text) || !File.Exists(MapPathTextBox.Text))
            {
                MessageBox.Show("Kérlek válassz ki egy érvényes térkép fájlt!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(LogFolderPathTextBox.Text) || !Directory.Exists(LogFolderPathTextBox.Text))
            {
                MessageBox.Show("Kérlek válassz ki egy érvényes mentési mappát!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TimeTextBox.Text, out int orak) || orak < 24)
            {
                MessageBox.Show("Az időkorlátnak legalább 24 órának kell lennie!", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string[] mentendoAdatok = { MapPathTextBox.Text, LogFolderPathTextBox.Text, TimeTextBox.Text };
                File.WriteAllLines(settingsFajl, mentendoAdatok);
            }
            catch { }

            KivalasztottMapPath = MapPathTextBox.Text;
            KivalasztottLogMappa = LogFolderPathTextBox.Text;
            MegadottMaxOra = orak;

            // MODOSÍTÁS: MainWindow megnyitása és SetupWindow bezárása
            MainWindow foAblak = new MainWindow();
            foAblak.Show();

            this.Close();
        }
    }
}