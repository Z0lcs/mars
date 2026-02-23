using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            using StreamReader sr = new(path);
            int sor = 0;
            while (!sr.EndOfStream && sor < 50)
            {
                string[] parts = sr.ReadLine().Split(',');
                for (int oszlop = 0; oszlop < parts.Length; oszlop++)
                {
                    Point p = new Point(sor, oszlop);
                    switch (parts[oszlop])
                    {
                        case "#": Akadalyok.Add(p); break;
                        case "Y": RitkaArany.Add(p); break;
                        case "B": Vizjeg.Add(p); break;
                        case "G": RitkaAsvany.Add(p); break;
                        case "S": KezdoPont = p; break;
                    }
                }
                Grid[sor] = parts;
                sor++;
            }
        }
    }
}
