using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FarmVisitors
{
    internal class ModConfig
    {
        public int CustomChance { get; set; } = 10;
        public int MaxVisitsPerDay { get; set; } = 3;
        public int StartingHours { get; set; } = 800;
        public int EndingHours { get; set; } = 2200;
        public int Duration { get; set; } = 5;
        public bool Verbose { get; set; } = false;
    }
}
