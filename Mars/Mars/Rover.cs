using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vadász_Mars_Dénes
{
    public class Rover
    {
        public Point Pozicio { get; set; }
        public double Akkumulator { get; set; } = 100;
        public int OsszegyujtottAsvany { get; set; } = 0;

        public double SzamolFogyasztas(int sebesség, bool nappal, bool banyaszik = false)
        {
            double energiaFogyasztas = 0;

            if (banyaszik)
            {
                energiaFogyasztas = 2;
            }
            else if (sebesség == 0)
            {
                energiaFogyasztas = 1;
            }
            else
            {
                energiaFogyasztas = 2 * Math.Pow(sebesség, 2);
            }

            double toltes = nappal ? 10 : 0;

            return toltes - energiaFogyasztas;
        }
        public void FrissitEnergia(double energiaValtozas)
        {
            Akkumulator += energiaValtozas;

            if (Akkumulator > 100)
            {
                Akkumulator = 100;
            }

            if (Akkumulator < 0)
            {
                Akkumulator = 0;
            }
        }
    }
}
