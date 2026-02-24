using System.Threading.Channels;

namespace Vadász_Mars_Dénes
{
    
    public class IdoKezelo
    {
        public double teljesIdo { get; set; }
        public int ElteltFelOra { get; set; } = 0;
        public bool FuthatMegAProgram => (ElteltFelOra * 0.5) < teljesIdo;
        public IdoKezelo(double idotartam)
        {
            teljesIdo = idotartam;
        }
        public bool NappalVanE()
        {
            double aktualisOra = (ElteltFelOra * 0.5) % 24;
            return aktualisOra < 16;
        }
        public void Lepes()
        {
            ElteltFelOra++;
        }
        public string Statusz()
        {
            bool nappal = NappalVanE() ? true : false;
            return $"Eltelt idő: {ElteltFelOra * 0.5 + 0.5} óra | Nappal van e? : {nappal}";
        }
    }
}
