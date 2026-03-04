using System;

namespace Vadász_Mars_Dénes
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MarsMap terkep = new MarsMap();
            try { terkep.LoadFromFile("mars_map_50x50.csv"); }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba a térkép betöltésekor: {ex.Message}");
                Console.ReadLine();
                return;
            }

            RoverVezerlo vezerlo = new RoverVezerlo(terkep, 72, "rover_log.csv");

            vezerlo.SzimulacioInditasa();
        }
    }
}