using System.Threading.Channels;

namespace Vadász_Mars_Dénes
{

   public class IdoKezelo
{
    public int ElteltPerc { get; private set; } = 0;
    public double MaxOra { get; set; }

    public IdoKezelo(double idotartamOra) { MaxOra = idotartamOra; }

    public bool FuthatMegAProgram => (ElteltPerc / 60.0) < MaxOra;

    public void IdoUgras(int perc) { ElteltPerc += perc; }

        public bool NappalVanE()
        {
            double aktualisOra = (ElteltPerc / 60.0) % 24;
            return aktualisOra < 16;
        }

    public string Statusz() 
    {
        double ora = ElteltPerc / 60.0;
        return $"Idő: {ora:F1} óra | {(NappalVanE() ? "Nappal" : "Éjszaka")}";
    }
}
}
