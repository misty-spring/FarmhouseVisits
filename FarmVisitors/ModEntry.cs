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
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += this.GameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChange;
            helper.Events.GameLoop.DayEnding += this.DayEnding;
            helper.Events.GameLoop.ReturnedToTitle += this.TitleReturn;
            helper.Events.Content.AssetRequested += Extras.AssetRequest;

            this.Config = this.Helper.ReadConfig<ModConfig>();

            ModHelper = this.Helper;
            ModMonitor = this.Monitor;
            ModVisitor = this.VisitorName;
            //VisitorData = VisitorData;

            if (Config.Debug is true)
            {
                helper.ConsoleCommands.Add("force_visit", helper.Translation.Get("CLI.force_visit"), this.ForceVisit);
                helper.ConsoleCommands.Add("print_all", helper.Translation.Get("CLI.print_all"), this.PrintAll);
                helper.ConsoleCommands.Add("vi_reload", helper.Translation.Get("CLI.reload"), this.Reload);
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
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Extras",
                text: Extras.ExtrasTL
            );

            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Extras"
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                getValue: () => this.Config.Blacklist,
                setValue: value => this.Config.Blacklist = value,
                name: Extras.BlacklistTL,
                tooltip: Extras.BlacklistTTP
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
            CleanTempData(this);

            if (!string.IsNullOrWhiteSpace(Config.Blacklist))
            {
                ParseBlacklist(this);
            }

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
                            if(BlacklistParsed is not null)
                            {
                                if (BlacklistParsed.Contains(npcn.Name))
                                {
                                    this.Monitor.Log($"{npcn.displayName} (internal name {npcn.Name}) is in the blacklist.", LogLevel.Info);
                                }
                                else
                                {
                                    NameAndLevel.Add(name, hearts);
                                }
                            }
                            else
                            {
                                NameAndLevel.Add(name, hearts);
                            }
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

            ReloadCustomschedules(this);
        }
        private void OnTimeChange(object sender, TimeChangedEventArgs e)
        {
            if (IsConfigValid == false || RepeatedByLV is null)
            {
                return;
            }

            if (e.NewTime > Config.StartingHours && e.NewTime < Config.EndingHours)
            {
                if (HasAnyVisitors is false && CounterToday < Config.MaxVisitsPerDay && SchedulesParsed is not null) //custom
                {
                    foreach (KeyValuePair<string, ScheduleData> pair in SchedulesParsed)
                    {
                        if (e.NewTime.Equals(pair.Value.From))
                        {
                            currentCustom.Add(pair.Key, pair.Value);

                            VisitorName = pair.Key;
                            ModVisitor = VisitorName;

                            NPC visit = Game1.getCharacterFromName(VisitorName);
                            VisitorData = new TempNPC(visit);

                            Actions.AddCustom(visit, farmHouse, currentCustom[VisitorName]);

                            DurationSoFar = 0;

                            HasAnyVisitors = true;
                            TimeOfArrival = e.NewTime;
                            ControllerTime = 0;
                            CustomVisiting = true;

                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"\nHasAnyVisitors set to true.\n{VisitorName} will begin visiting player.\nTimeOfArrival = {e.NewTime};\nControllerTime = {ControllerTime};");
                            }

                            break;
                        }
                    }
                } //custom

                if (HasAnyVisitors is false && CounterToday < Config.MaxVisitsPerDay) //random
                {
                    if (Random.Next(1, 101) <= Config.CustomChance && Game1.currentLocation == farmHouseAsLocation)
                    {
                        // choose a random character from a list
                        var RChoice = Random.Next(0, (RepeatedByLV.Count));
                        VisitorName = RepeatedByLV[RChoice];
                        ModVisitor = VisitorName;

                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"Random: {RChoice}; VisitorName= {VisitorName}; ModVisitor= {ModVisitor}");
                        }

                        if (!TodaysVisitors.Contains(VisitorName))
                        {
                            CustomVisiting = false;

                            //save values
                            NPC visit = Game1.getCharacterFromName(VisitorName);
                            VisitorData = new TempNPC(visit);

                            //add them to farmhouse
                            Actions.AddToFarmHouse(visit, farmHouse);
                            DurationSoFar = 0;

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
                            VisitorName = null;
                            ModVisitor = null;
                            this.Monitor.Log($"{VisitorName} has already visited the Farm today!");
                        }
                    }
                } //random
            }
            
            if (HasAnyVisitors == true && CustomVisiting == false)
            {
                NPC c = Game1.getCharacterFromName(VisitorName);
                if (Values.IsVisitor(c.Name))
                {
                    if (DurationSoFar >= MaxTimeStay)
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

                        VisitorData = null;

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

                        if (c.controller is not null && ControllerTime >= 1)
                        {
                            c.Halt();
                            c.controller = null;
                            ControllerTime = 0;
                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"ControllerTime = {ControllerTime}");
                            }
                        }
                        else if (e.NewTime > TimeOfArrival)
                        {
                            c.controller = new PathFindController(c, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random), Random.Next(0, 4));

                            if (Random.Next(0, 11) <= 5 && FurnitureList is not null)
                            {
                                c.setNewDialogue(string.Format(Values.TalkAboutFurniture(c), Values.GetRandomFurniture()), true, false);
                                if (Config.Verbose == true)
                                {
                                    this.Monitor.Log($"Adding dialogue for {c.Name}...");
                                }
                            }

                            ControllerTime++;
                            if (Config.Verbose == true)
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
                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"DurationSoFar = {DurationSoFar} ({DurationSoFar * 10} minutes).");
                        }
                    }
                }
                else
                {
                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"{c.Name} is not marked as visit.");
                    }
                }
            }

            if (HasAnyVisitors == true && CustomVisiting == true)
            {
                NPC c = Game1.getCharacterFromName(VisitorName);
                if (DurationSoFar >= MaxTimeStay || e.NewTime.Equals(currentCustom[VisitorName].To))
                {
                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"{c.Name} is retiring for the day.");
                    }

                    //if has custom dialogue: exit with custom. else normal
                    var exitd = currentCustom[VisitorName].ExitDialogue;
                    if(!string.IsNullOrWhiteSpace(exitd))
                    {
                        Actions.RetireCustom(c, e.NewTime, farmHouse, exitd);
                    }
                    else
                    {
                        Actions.Retire(c, e.NewTime, farmHouse);
                    }

                    HasAnyVisitors = false;
                    TodaysVisitors.Add(VisitorName);
                    DurationSoFar = 0;
                    ControllerTime = 0;
                    VisitorName = null;
                    ModVisitor = VisitorName;
                    CustomVisiting = false;
                    currentCustom.Clear();

                    VisitorData = null;

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

                    if (c.controller is not null && ControllerTime >= 1)
                    {
                        c.Halt();
                        c.controller = null;
                        ControllerTime = 0;
                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"ControllerTime = {ControllerTime}");
                        }
                    }
                    else if (e.NewTime > TimeOfArrival)
                    {
                        c.controller = new PathFindController(c, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random), Random.Next(0, 4));

                        if (currentCustom[VisitorName].Dialogues.Any<string>())
                        {
                            c.setNewDialogue(currentCustom[VisitorName].Dialogues[0], true, false);

                            this.Monitor.Log($"Adding custom dialogue for {c.Name}...");
                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"Custom dialogue: {currentCustom[VisitorName].Dialogues[0]}");
                            }

                            //remove this dialogue
                            currentCustom[VisitorName].Dialogues.RemoveAt(0);
                        }
                        else if (Random.Next(0, 11) <= 5 && FurnitureList is not null)
                        {
                            c.setNewDialogue(string.Format(Values.TalkAboutFurniture(c), Values.GetRandomFurniture()), true, false);
                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"Adding dialogue for {c.Name}...");
                            }
                        }

                        ControllerTime++;
                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"ControllerTime = {ControllerTime}");
                        }
                    }
                    else
                    {
                        this.Monitor.Log($"Time of arrival equals current time. NPC won't move around");
                    }

                    //to check that value isnt 0 AND that its a valid one. (doesnt need parsing since thats done at the start)
                    if(currentCustom[VisitorName].To < 610)
                    {
                        DurationSoFar++;
                    }

                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"DurationSoFar = {DurationSoFar} ({DurationSoFar * 10} minutes).");
                    }
                }
            }
        }
        private void DayEnding(object sender, DayEndingEventArgs e)
        {
            CleanTempData(this);

            ReloadCustomschedules(this);
            
            if (BlacklistRaw != Config.Blacklist && !string.IsNullOrWhiteSpace(Config.Blacklist))
            {
                ParseBlacklist(this);
            }
            else if(string.IsNullOrWhiteSpace(Config.Blacklist))
            {
                BlacklistRaw = null;
                if(BlacklistParsed is not null)
                {
                    BlacklistParsed.Clear();
                }
            }

            if (Config.Verbose == true)
            {
                this.Monitor.Log("Clearing today's visitor list, visitor count, and all other temp info...");
            }
        }
        private void TitleReturn(object sender, ReturnedToTitleEventArgs e)
        {
            ClearValues(this);

            if (Config.Verbose == true)
            {
                this.Monitor.Log($"Removing cached information: HasAnyVisitors= {HasAnyVisitors}, TimeOfArrival={TimeOfArrival}, CounterToday={CounterToday}, VisitorName={VisitorName}. TodaysVisitors, NameAndLevel, FurnitureList, and RepeatedByLV cleared.");
            }

            if(SchedulesParsed is not null)
            {
                SchedulesParsed.Clear();
            }
        }

        //methods used by mod
        private static void ParseBlacklist(ModEntry entry)
        {
            BlacklistRaw = entry.Config.Blacklist;
            var charsToRemove = new string[] { "-", ",", ".", ";", "\"", "\'", "/" };
            foreach (var c in charsToRemove)
            {
                BlacklistRaw = BlacklistRaw.Replace(c, string.Empty);
            }
            if(entry.Config.Verbose == true)
            {
                entry.Monitor.Log($"Raw blacklist: \n {BlacklistRaw} \nWill be parsed to list now.");
            }
            BlacklistParsed = BlacklistRaw.Split(' ').ToList();
        }
        private static void CleanTempData(ModEntry entry)
        {
            ModVisitor = null;
            VisitorData = null;
            entry.VisitorName = null;
            entry.CounterToday = 0;

            entry.CustomVisiting = false;
            entry.HasAnyVisitors = false;

            if (currentCustom is not null)
            {
                currentCustom.Clear();
            }

            if(entry.TodaysVisitors is not null)
            {
                entry.TodaysVisitors.Clear();
            }
            
            if(entry.MaxTimeStay != (entry.Config.Duration - 1))
            {
                entry.MaxTimeStay = (entry.Config.Duration - 1);
                entry.Monitor.Log($"MaxTimeStay = {entry.MaxTimeStay}; Config.Duration = {entry.Config.Duration};");
            }
            
            if (FurnitureList is not null)
            {
                FurnitureList.Clear();
                FurnitureList = Values.UpdateFurniture(Utility.getHomeOfFarmer(Game1.MasterPlayer));

                if (entry.Config.Verbose == true)
                {
                    entry.Monitor.Log($"Furniture list updated. Count: {FurnitureList.Count}");
                }
            }
        }
        private static void ClearValues(ModEntry modEntry)
        {
            if (!string.IsNullOrWhiteSpace(modEntry.Config.Blacklist))
            {
                ParseBlacklist(modEntry);
            }
            else
            {
                BlacklistRaw = null;
                if (BlacklistParsed is not null)
                {
                    BlacklistParsed.Clear();
                }
            }

            if (modEntry.RepeatedByLV is not null)
            {
                modEntry.RepeatedByLV.Clear();
            }
            if (modEntry.TodaysVisitors is not null)
            {
                modEntry.TodaysVisitors.Clear();
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
            if (modEntry.CustomVisiting is true)
            {
                modEntry.CustomVisiting = false;
            }
            if (currentCustom is not null)
            {
                currentCustom.Clear();
            }
            if (NameAndLevel is not null)
            {
                NameAndLevel.Clear();
            }
            if (FurnitureList is not null)
            {
                FurnitureList.Clear();
            }

            if (VisitorData is not null)
            {
                VisitorData = null;
            }
        }
        private static void ReloadCustomschedules(ModEntry entry)
        {
            if(SchedulesParsed is not null)
            {
                SchedulesParsed.Clear();
            }
            else
            {
                return;
            }

            var schedules = ModHelper.GameContent.Load<Dictionary<string, ScheduleData>>("mistyspring.farmhousevisits/Schedules");
            if (schedules is not null)
            {
                foreach(KeyValuePair<string, ScheduleData> pair in schedules)
                {
                    entry.Monitor.Log($"Checking {pair.Key}'s schedule...");
                    if(!Extras.IsScheduleValid(pair))
                    {
                        entry.Monitor.Log($"{pair.Key} schedule won't be added.", LogLevel.Error);
                    }
                    else
                    {
                        SchedulesParsed.Add(pair.Key, pair.Value);
                    }
                }
            }
        }


        //console commands
        private void Reload(string command, string[] arg2)
        {
            if (!string.IsNullOrWhiteSpace(Config.Blacklist))
            {
                ParseBlacklist(this);
            }

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

            foreach (string name in Game1.NPCGiftTastes.Keys)
            {
                if (Config.Debug == true)
                {
                    this.Monitor.Log($"Name: {name}", LogLevel.Trace);
                }
                if (!name.StartsWith("Universal_") && name is not null)
                {
                    int hearts = Game1.MasterPlayer.getFriendshipHeartLevelForNPC(name);
                    NPC npcn = Game1.getCharacterFromName(name);

                    if (npcn is not null)
                    {
                        if (!Values.IsMarriedToPlayer(npcn) && hearts is not 0) //npc isnt married and value isnt 0
                        {
                            if (BlacklistParsed.Contains(npcn.Name))
                            {
                                this.Monitor.Log($"{npcn.displayName} (internal name {npcn.Name}) is in the blacklist.", LogLevel.Info);
                            }
                            else
                            {
                                NameAndLevel.Add(name, hearts);
                            }
                        }
                        else
                        {
                            if (Config.Verbose == true)
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
            if (Config.Verbose == true)
            {
                string LongString = "ALL values:   ";
                foreach (string item in RepeatedByLV)
                {
                    LongString = $"{LongString}, {item}";
                }
                this.Monitor.Log(LongString);
            }

            if (FurnitureList is not null)
            {
                FurnitureList.Clear();
            }
            FurnitureList = Values.UpdateFurniture(farmHouse);
            if (Config.Verbose == true)
            {
                this.Monitor.Log($"Furniture count: {FurnitureList.Count}");

            }
        }
        private void ForceVisit(string command, string[] arg2)
        {
            if (Context.IsWorldReady)
            {
                if (Game1.MasterPlayer.currentLocation == farmHouse)
                {
                    var RChoice = Random.Next(0, (RepeatedByLV.Count));
                    VisitorName = RepeatedByLV[RChoice];
                    ModVisitor = VisitorName;
                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"VisitorName= {VisitorName}; ModVisitor= {ModVisitor}");
                    }

                    if (!TodaysVisitors.Contains(VisitorName))
                    {

                        //save values
                        NPC visit = Game1.getCharacterFromName(VisitorName);
                        VisitorData = new TempNPC(visit);

                        //add them to farmhouse
                        Actions.AddToFarmHouse(visit, farmHouse);
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
                this.Monitor.Log(Helper.Translation.Get("error.WorldNotReady"), LogLevel.Error);
            }
        }
        private void PrintAll(string command, string[] arg2)
        {
            this.Monitor.Log($"VisitorName = {VisitorName}; IsConfigValid = {IsConfigValid}; HasAnyVisitors = {HasAnyVisitors}; TimeOfArrival = {TimeOfArrival}; CounterToday = {CounterToday}; DurationSoFar = {DurationSoFar}; MaxTimeStay = {MaxTimeStay}; ControllerTime = {ControllerTime}");
        }

        //data
        private ModConfig Config;
        private List<string> RepeatedByLV = new();
        private List<string> TodaysVisitors = new();
        private static Random random;

        internal static IMonitor ModMonitor { get; private set; }
        internal static IModHelper ModHelper { get; private set; }
        internal static string ModVisitor { get; private set; }
        internal static TempNPC VisitorData { get; private set; }

        internal static Random Random
        {
            get
            {
                random ??= new Random(((int)Game1.uniqueIDForThisGame * 26) + (int)(Game1.stats.DaysPlayed * 36));
                return random;
            }
        }

        internal GameLocation farmHouseAsLocation;
        internal FarmHouse farmHouse { get; private set; }
        
        internal static string BlacklistRaw { get; private set; }
        internal string VisitorName { get; private set; }
        internal bool CustomVisiting { get; private set; }
        internal bool IsConfigValid { get; private set; }
        internal bool HasAnyVisitors { get; private set; }
        internal int TimeOfArrival { get; private set; }
        internal int CounterToday { get; private set; }
        internal int DurationSoFar { get; private set; }
        internal int MaxTimeStay { get; private set; }
        internal int ControllerTime { get; private set; }
        internal static Dictionary<string, int> NameAndLevel { get; private set; } = new();
        internal static Dictionary<string, string> NPCDisp { get; private set; }
        internal static List<Furniture> FurnitureList { get; private set; }
        internal static List<string> BlacklistParsed { get; private set; }
        internal static Dictionary<string, ScheduleData> SchedulesParsed { get; private set; }
        internal static Dictionary<string, ScheduleData> currentCustom { get; private set; }
    }
}
