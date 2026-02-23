namespace Vadász_Mars_Dénes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MarsMap terkep = new MarsMap();
            string path = "mars_map_50x50.csv"; 
            terkep.LoadFromFile(path);

            Rover rover = new Rover();
            rover.Pozicio = terkep.KezdoPont;

            Console.WriteLine($"A rover elindult a ({rover.Pozicio.X},{rover.Pozicio.Y}) pontról.");
            Console.WriteLine($"Találtunk {terkep.RitkaArany.Count+ terkep.RitkaAsvany.Count+ terkep.Vizjeg.Count} ásványt és {terkep.Akadalyok.Count} akadályt.");

        }
    }
}
