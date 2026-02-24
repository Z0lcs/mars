using System.Drawing;

namespace Vadász_Mars_Dénes
{
    public class MarsMap
    {
        public string[][] Grid { get; private set; } = new string[50][];
        public List<Point> Akadalyok { get; } = new();
        public List<Point> RitkaArany { get; } = new();
        public List<Point> RitkaAsvany { get; } = new();
        public List<Point> Vizjeg { get; } = new();
        public Point KezdoPont { get; private set; }

        public void LoadFromFile(string path)
        {
            string[] sorok = File.ReadAllLines(path);
            for (int i = 0; i < 50; i++)
            {
                string[] elemek = sorok[i].Split(',');
                Grid[i] = elemek;
                for (int j = 0; j < elemek.Length; j++)
                {
                    Point p = new Point(j, i); // X = oszlop, Y = sor
                    string ertek = elemek[j].Trim().ToUpper();
                    if (ertek == "S") KezdoPont = p;
                    else if (ertek == "#") Akadalyok.Add(p);
                    else if (ertek == "Y") RitkaArany.Add(p);
                    else if (ertek == "B") Vizjeg.Add(p);
                    else if (ertek == "G") RitkaAsvany.Add(p);
                }
            }
        }
    }
}