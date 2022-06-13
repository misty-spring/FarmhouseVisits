namespace FarmVisitors
{
    internal class ModConfig
    {
        public int CustomChance { get; set; } = 20;
        public int MaxVisitsPerDay { get; set; } = 3;
        public int StartingHours { get; set; } = 800;
        public int EndingHours { get; set; } = 2200;
        public int Duration { get; set; } = 6;
        public bool Verbose { get; set; } = false;
        public bool Debug { get; set; } = false;
    }
}
