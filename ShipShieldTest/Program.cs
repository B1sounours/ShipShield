using ShipShieldPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShipShieldPatch
{
    class Program
    {
        static void Main(string[] args)
        {
            ShipShield shipShield = new ShipShield();
            shipShield.Init();
        }
    }
}
