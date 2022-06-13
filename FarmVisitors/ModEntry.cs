using System;
using System.Collections.Generic;
using System.Linq;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace FarmVisitors
{
    public class ModEntry : Mod
    {
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += this.GameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChange;
            helper.Events.GameLoop.DayEnding += this.DayEnding;
            helper.Events.GameLoop.ReturnedToTitle += this.TitleReturn;

            this.Config = this.Helper.ReadConfig<ModConfig>();

            ModHelper = this.Helper;
            ModMonitor = this.Monitor;
            ModVisitor = this.VisitorName;

            if (Config.Debug is true)
            {
                helper.ConsoleCommands.Add("force_visit", helper.Translation.Get("CLI.force_visit"), this.ForceVisit);
                helper.ConsoleCommands.Add("print_all", helper.Translation.Get("CLI.print_all"), this.PrintAll);
            }
        }
        
        private void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            //check values and clear them if needed
            ClearValues(this);

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;
            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // basic config options
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.CustomChance.name"),
                tooltip: () => this.Helper.Translation.Get("config.CustomChance.description"),
                getValue: () => this.Config.CustomChance,
                setValue: value => this.Config.CustomChance = value,
                min: 0,
                max: 100,
                interval: 1
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.MaxVisitsPerDay.name"),
                tooltip: () => this.Helper.Translation.Get("config.MaxVisitsPerDay.description"),
                getValue: () => this.Config.MaxVisitsPerDay,
                setValue: value => this.Config.MaxVisitsPerDay = value,
                min: 0,
                max: 24,
                interval: 1
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.StartingHours.name"),
                tooltip: () => this.Helper.Translation.Get("config.StartingHours.description"),
                getValue: () => this.Config.StartingHours,
                setValue: value => this.Config.StartingHours = value,
                min: 600,
                max: 2400,
                interval: 100
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.EndingHours.name"),
                tooltip: () => this.Helper.Translation.Get("config.EndingHours.description"),
                getValue: () => this.Config.EndingHours,
                setValue: value => this.Config.EndingHours = value,
                min: 600,
                max: 2400,
                interval: 100
            );
            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.VisitDuration.name"),
                tooltip: () => this.Helper.Translation.Get("config.VisitDuration.description"),
                getValue: () => this.Config.Duration,
                setValue: value => this.Config.Duration = value,
                min: 1,
                max: 20,
                interval: 1
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.Verbose.name"),
                tooltip: () => this.Helper.Translation.Get("config.Verbose.description"),
                getValue: () => this.Config.Verbose,
                setValue: value => this.Config.Verbose = value
            );
        }
        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            /*for testing friendshipdata
            Farmer player = Game1.MasterPlayer;
            var fdata = player.friendshipData;
            this.Helper.Data.WriteJsonFile("FriendshipData.json", fdata);*/

            NPCDisp = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");

            MaxTimeStay = (Config.Duration - 1);
            if (Config.Verbose == true)
            {
                this.Monitor.Log($"MaxTimeStay = {MaxTimeStay}; Config.Duration = {Config.Duration};");
            }

            farmHouseAsLocation = Utility.getHomeOfFarmer(Game1.MasterPlayer);
            farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);

            if (Config.StartingHours >= Config.EndingHours)
            {
                this.Monitor.Log("Starting hours can't happen after ending hours! To use the mod, fix this and reload savefile.", LogLevel.Error);
                IsConfigValid = false;
                return;
            }
            else
            {
                IsConfigValid = true;
            }

            foreach(string name in Game1.NPCGiftTastes.Keys)
            {
                if(Config.Debug == true)
                {
                    this.Monitor.Log($"Name: {name}", LogLevel.Trace);
                }
                if (!name.StartsWith("Universal_") && name is not null)
                {
                    int hearts = Game1.MasterPlayer.getFriendshipHeartLevelForNPC(name);
                    NPC npcn = Game1.getCharacterFromName(name);

                    if(npcn is not null)
                    {
                        if (!Values.IsMarriedToPlayer(npcn) && hearts is not 0) //npc isnt married and value isnt 0
                        {
                            NameAndLevel.Add(name, hearts);
                        }
                        else
                        {
                            if(Config.Verbose == true)
                            {
                                this.Monitor.Log($"{name} won't be added to the list.");
                            }
                        }
                    }
                    else
                    {
                        this.Monitor.Log($"{name} is not an existing NPC!");
                    }
                    
                }
            }
            string call = "\n Name   | Hearts\n--------------------";
            foreach (KeyValuePair<string, int> pair in NameAndLevel)
            {
                call += $"\n   {pair.Key}   {pair.Value}";

                List<string> tempdict = Enumerable.Repeat(pair.Key, pair.Value).ToList();
                RepeatedByLV.AddRange(tempdict);
            }
            this.Monitor.Log(call);
            if(Config.Verbose == true)
            {
                string LongString = "ALL values:   ";
                foreach (string item in RepeatedByLV)
                {
                    LongString = $"{LongString}, {item}";
                }
                this.Monitor.Log(LongString);
            }

            if(FurnitureList is not null)
            {
                FurnitureList.Clear();
            }
            FurnitureList = Values.UpdateFurniture(farmHouse);
            if (Config.Verbose == true)
            {
                this.Monitor.Log($"Furniture count: {FurnitureList.Count}");
            }
        }
        private void OnTimeChange(object sender, TimeChangedEventArgs e)
        {
            if (IsConfigValid == false || RepeatedByLV.Count is 0)
            {
                return;
            }

            if(HasAnyVisitors is false && CounterToday < Config.MaxVisitsPerDay && (e.NewTime > Config.StartingHours && e.NewTime < Config.EndingHours))
            {
                if (Random.Next(1, 101) <= Config.CustomChance && Game1.currentLocation == farmHouseAsLocation)
                {
                    // choose a random character from a list
                    var RChoice = Random.Next(0,(RepeatedByLV.Count + 1));
                    VisitorName = RepeatedByLV[RChoice];
                    ModVisitor = VisitorName;
                    if(Config.Verbose == true)
                    {
                        this.Monitor.Log($"Random: {RChoice}; VisitorName= {VisitorName}; ModVisitor= {ModVisitor}");
                    }

                    if(!TodaysVisitors.Contains(VisitorName))
                    {
                        //add them to farmhouse
                        Actions.AddToFarmHouse(VisitorName, farmHouse);
                        DurationSoFar = 0;
                        //(added to method) Game1.warpCharacter(Game1.getCharacterFromName(VisitorName), "FarmHouse", houseSize);

                        HasAnyVisitors = true;
                        TimeOfArrival = e.NewTime;
                        ControllerTime = 0;

                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"\nHasAnyVisitors set to true.\n{VisitorName} will begin visiting player.\nTimeOfArrival = {e.NewTime};\nControllerTime = {ControllerTime};");
                        }
                    }
                    else
                    {
                        this.Monitor.Log($"{VisitorName} has already visited the Farm today!");
                    }
                }
            }
            if(HasAnyVisitors == true)
            {
                foreach (NPC c in farmHouseAsLocation.characters)
                {
                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"Checking {c.Name}...");
                    }
                    if(Values.IsVisitor(c.Name))
                    {
                        if(DurationSoFar >= MaxTimeStay)
                        {
                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"{c.Name} is retiring for the day.");
                            }
                            Actions.Retire(c, e.NewTime, farmHouse);
                            HasAnyVisitors = false;
                            CounterToday++;
                            TodaysVisitors.Add(VisitorName);
                            DurationSoFar = 0;
                            ControllerTime = 0;
                            VisitorName = null;
                            ModVisitor = VisitorName;

                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"HasAnyVisitors = false, CounterToday = {CounterToday}, TodaysVisitors= {Actions.TurnToString(TodaysVisitors)}, DurationSoFar = {DurationSoFar}, ControllerTime = {ControllerTime}, VisitorName = {VisitorName}, ModVisitor = {ModVisitor}");
                            }
                            return;
                        }
                        else
                        {
                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"{c.Name} will move around the house now.");
                            }

                            if(c.controller is not null && ControllerTime >=1)
                            {
                                c.Halt();
                                c.controller = null;
                                ControllerTime = 0;
                                if(Config.Verbose == true)
                                {
                                    this.Monitor.Log($"ControllerTime = {ControllerTime}");
                                }
                            }
                            else if(e.NewTime > TimeOfArrival)
                            {
                                c.controller = new PathFindController(c, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random), Random.Next(0, 4));
                                
                                if(Random.Next(0, 11) <= 5 && FurnitureList.Count is not 0)
                                {
                                    c.setNewDialogue(string.Format(Values.TalkAboutFurniture(c), Values.GetRandomFurniture()), true, false);
                                    if (Config.Verbose == true)
                                    {
                                        this.Monitor.Log($"Adding dialogue for {c.Name}...");
                                    }
                                }
                                
                                ControllerTime++;
                                if(Config.Verbose == true)
                                {
                                    this.Monitor.Log($"ControllerTime = {ControllerTime}");
                                }
                            }
                            else
                            {
                                if (Config.Verbose == true)
                                {
                                    this.Monitor.Log($"Time of arrival equals current time. NPC won't move around");
                                }
                            }

                            DurationSoFar++;
                            if(Config.Verbose == true)
                            {
                                this.Monitor.Log($"DurationSoFar = {DurationSoFar} ({DurationSoFar * 10} minutes).");
                            }
                        }
                    }
                    else
                    {
                        if(Config.Verbose == true)
                        {
                            this.Monitor.Log($"{c.Name} is not marked as visit.");
                        }
                    }
                }
            }
        }
        private void DayEnding(object sender, DayEndingEventArgs e)
        {
            TodaysVisitors.Clear();
            CounterToday = 0;
            if(MaxTimeStay != (Config.Duration - 1))
            {
                MaxTimeStay = (Config.Duration - 1);
                this.Monitor.Log($"MaxTimeStay = {MaxTimeStay}; Config.Duration = {Config.Duration};");
            }

            if (Config.Verbose == true)
            {
                this.Monitor.Log("Clearing today's visitor list...");
                this.Monitor.Log("Clearing today's visitor count...");
                
            }

            if (FurnitureList is not null)
            {
                FurnitureList.Clear();
            }
            FurnitureList = Values.UpdateFurniture(farmHouse);
            if (Config.Verbose == true)
            {
                this.Monitor.Log($"Furniture list updated. Count: {FurnitureList.Count}");
            }
        }
        private void TitleReturn(object sender, ReturnedToTitleEventArgs e)
        {
            ClearValues(this);

            if (Config.Verbose == true)
            {
                this.Monitor.Log($"Removing cached information: HasAnyVisitors= {HasAnyVisitors}, TimeOfArrival={TimeOfArrival}, CounterToday={CounterToday}, VisitorName={VisitorName}. TodaysVisitors, NameAndLevel, FurnitureList, and RepeatedByLV cleared.");
            }
        }

        private static void ClearValues(ModEntry modEntry)
        {
            if (modEntry.RepeatedByLV.Count is not 0)
            {
                modEntry.RepeatedByLV.Clear();
            }
            if (modEntry.TodaysVisitors.Count is not 0)
            {
                modEntry.TodaysVisitors.Clear();
            }
            if (modEntry.NameAndLevel.Count is not 0)
            {
                modEntry.NameAndLevel.Clear();
            }
            if (modEntry.HasAnyVisitors == true)
            {
                modEntry.HasAnyVisitors = false;
            }
            if (modEntry.TimeOfArrival is not 0)
            {
                modEntry.TimeOfArrival = 0;
            }
            if (modEntry.CounterToday is not 0)
            {
                modEntry.CounterToday = 0;
            }
            if (modEntry.VisitorName is not null)
            {
                modEntry.VisitorName = null;
            }
            if (modEntry.DurationSoFar is not 0)
            {
                modEntry.DurationSoFar = 0;
            }
            if (modEntry.ControllerTime is not 0)
            {
                modEntry.ControllerTime = 0;
            }
            if (FurnitureList is not null)
            {
                FurnitureList.Clear();
            }
        }

        private void ForceVisit(string command, string[] arg2)
        {
            if (Context.IsWorldReady)
            {
                if (!string.IsNullOrWhiteSpace(arg2[0]) && NPCDisp.Keys.Contains(arg2[0]))
                {
                    if (Game1.MasterPlayer.currentLocation == farmHouse)
                    {
                        VisitorName = arg2[0];
                        ModVisitor = arg2[0];
                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"VisitorName= {VisitorName}; ModVisitor= {ModVisitor}");
                        }

                        if (!TodaysVisitors.Contains(VisitorName))
                        {
                            Actions.AddToFarmHouse(VisitorName, farmHouse);
                            DurationSoFar = 0;

                            HasAnyVisitors = true;
                            TimeOfArrival = Game1.timeOfDay;
                            ControllerTime = 0;

                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"\nHasAnyVisitors set to true.\n{VisitorName} will begin visiting player.\nTimeOfArrival = {TimeOfArrival};\nControllerTime = {ControllerTime};");
                            }
                        }
                        else
                        {
                            this.Monitor.Log($"{VisitorName} has already visited the Farm today!");
                        }
                    }
                    else
                    {
                        this.Monitor.Log(Helper.Translation.Get("error.NotInFarmhouse"), LogLevel.Error);
                    }
                }
                else if (string.IsNullOrWhiteSpace(arg2[0]))
                {
                    if (Game1.MasterPlayer.currentLocation == farmHouse)
                    {
                        var RChoice = Random.Next(0, (RepeatedByLV.Count + 1));
                        VisitorName = RepeatedByLV[RChoice];
                        ModVisitor = VisitorName;
                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"VisitorName= {VisitorName}; ModVisitor= {ModVisitor}");
                        }

                        if (!TodaysVisitors.Contains(VisitorName))
                        {
                            Actions.AddToFarmHouse(VisitorName, farmHouse);
                            DurationSoFar = 0;

                            HasAnyVisitors = true;
                            TimeOfArrival = Game1.timeOfDay;
                            ControllerTime = 0;

                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"\nHasAnyVisitors set to true.\n{VisitorName} will begin visiting player.\nTimeOfArrival = {TimeOfArrival};\nControllerTime = {ControllerTime};");
                            }
                        }
                        else
                        {
                            this.Monitor.Log($"{VisitorName} has already visited the Farm today!");
                        }
                    }
                    else
                    {
                        this.Monitor.Log(Helper.Translation.Get("error.NotInFarmhouse"), LogLevel.Error);
                    }
                }
                else
                {
                    this.Monitor.Log(Helper.Translation.Get("error.InvalidValue"), LogLevel.Error);
                }
            }
            else
            {
                this.Monitor.Log(Helper.Translation.Get("error.WorldNotReady"), LogLevel.Error);
            }
        }
        private void PrintAll(string command, string[] arg2)
        {
            this.Monitor.Log($"VisitorName = {VisitorName}; IsConfigValid = {IsConfigValid}; HasAnyVisitors = {HasAnyVisitors}; TimeOfArrival = {TimeOfArrival}; CounterToday = {CounterToday}; DurationSoFar = {DurationSoFar}; MaxTimeStay = {MaxTimeStay}; ControllerTime = {ControllerTime}");
        }

        private ModConfig Config;
        private List<string> RepeatedByLV = new();
        private List<string> TodaysVisitors = new();
        private Dictionary<string, int> NameAndLevel = new();
        private static Random random;

        internal static IMonitor ModMonitor { get; private set; }
        internal static IModHelper ModHelper { get; private set; }
        internal static string ModVisitor { get; private set; }

        internal static Random Random
        {
            get
            {
                random ??= new Random(((int)Game1.uniqueIDForThisGame * 26) + (int)(Game1.stats.DaysPlayed * 36));
                return random;
            }
        }

        internal GameLocation farmHouseAsLocation;
        internal FarmHouse farmHouse;
        
        internal string VisitorName { get; private set; }
        internal bool IsConfigValid { get; private set; }
        internal bool HasAnyVisitors { get; private set; }
        internal int TimeOfArrival { get; private set; }
        internal int CounterToday { get; private set; }
        internal int DurationSoFar { get; private set; }
        internal int MaxTimeStay { get; private set; }
        internal int ControllerTime { get; private set; }
        internal static Dictionary<string, string> NPCDisp { get; private set; }
        internal static List<Furniture> FurnitureList { get; private set; }
    }
}
