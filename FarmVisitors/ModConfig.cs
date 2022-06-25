namespace FarmVisitors
{
    internal class ModConfig
    {
        public int CustomChance { get; set; } = 20;
        public int MaxVisitsPerDay { get; set; } = 3;
        public string Blacklist { get; set; } = "";
        public int StartingHours { get; set; } = 800;
        public int EndingHours { get; set; } = 2200;
        public int Duration { get; set; } = 6;
        public bool Verbose { get; set; } = true;
        public bool Debug { get; set; } = false;
	    public bool InLawComments { get; set; } = true;
    }
}
