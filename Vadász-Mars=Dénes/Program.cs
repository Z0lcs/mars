namespace Vadász_Mars_Dénes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"C:\mars\mars_map_50x50.csv";
            string[][] map = new string[50][];
            StreamReader sr = new(path);

            List<string> akadaly = new List<string>();
            List<string> ritkaArany = new List<string>();
            List<string> ritkaAsvany = new List<string>();
            List<string> vizjeg = new List<string>();
            string kezdoPont = "";

            int sorszam = 0;
            while (!sr.EndOfStream && sorszam < 50)
            {
                string line = sr.ReadLine();
                string[] parts = line.Split(',');

                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == "#")
                    {
                        akadaly.Add($"{sorszam},{i}");
                    }
                    else if (parts[i] =="Y")
                    {
                        ritkaArany.Add($"{sorszam},{i}");
                    }
                    else if (parts[i] == "B")
                    {
                        vizjeg.Add($"{sorszam},{i}");
                    }
                    else if (parts[i] == "G")
                    {
                        ritkaAsvany.Add($"{sorszam},{i}");
                    }
                    else if (parts[i] == "S")
                    {
                        kezdoPont += $"{sorszam},{i}";
                    }
                    //Console.Write(parts[i] + " ");
                }
                map[sorszam] = parts;
                sorszam++;
                //Console.WriteLine();
            }
            sr.Close();

        }
    }
}
