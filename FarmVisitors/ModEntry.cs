﻿using System;
using System.Collections.Generic;
using System.Linq;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;

namespace FarmVisitors
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += this.GameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.SaveLoaded;

            helper.Events.GameLoop.DayStarted += this.DayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChange;
            helper.Events.GameLoop.DayEnding += this.DayEnding;

            helper.Events.Player.Warped += FarmOutside.PlayerWarp;

            helper.Events.GameLoop.ReturnedToTitle += this.TitleReturn;
            helper.Events.Content.AssetRequested += Extras.AssetRequest;

            this.Config = this.Helper.ReadConfig<ModConfig>();

            Log = this.Monitor.Log;
            TL = this.Helper.Translation;

            InLawDialogue = Config.InLawComments;
            ReplacerOn = Config.ReplacerCompat;
            ReceiveGift = Config.Gifts;
            Debug = Config.Debug;

            if (Config.Debug)
            {
                helper.ConsoleCommands.Add("print", "List the values requested.", Debugging.Print);
                helper.ConsoleCommands.Add("vi_reload", "Reload visitor info.", this.Reload);
            }
        }

        private void GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            if(Config.Debug)
            {
                this.Monitor.Log("Debug has been turned on. This will change configuration for testing purposes.", LogLevel.Warn);

                this.Monitor.Log("Chance set to 100 (% every 10 min)");
                Config.CustomChance = 100;
                this.Monitor.Log("Starting hour will be 600.");
                Config.StartingHours = 600;
            }

            var AllowedStringVals = new string[3]
            {
                "VanillaOnly",
                "VanillaAndMod",
                "None"
            };

            //add actions
            ActionList.Add(new Action(Proceed));
            ActionList.Add(new Action(CancelVisit));

            //clear values. better safe than sorry
            ClearValues();

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
            configMenu.SetTitleScreenOnlyForNextOptions(
                mod: this.ModManifest,
                titleScreenOnly: true
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
            configMenu.SetTitleScreenOnlyForNextOptions(
                mod: this.ModManifest,
                titleScreenOnly: false
            );
            //extra customization
            configMenu.AddPageLink(
                mod: this.ModManifest,
                pageId: "Extras",
                text: Extras.ExtrasTL
            );

            configMenu.AddPage(
                mod: this.ModManifest,
                pageId: "Extras"
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: Extras.VisitConfiguration,
                tooltip: null);

            configMenu.SetTitleScreenOnlyForNextOptions(
                mod: this.ModManifest,
                titleScreenOnly: true
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                getValue: () => this.Config.Blacklist,
                setValue: value => this.Config.Blacklist = value,
                name: Extras.BlacklistTL,
                tooltip: Extras.BlacklistTTP
            );
            configMenu.SetTitleScreenOnlyForNextOptions(
                mod: this.ModManifest,
                titleScreenOnly: false
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

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.WalkOnFarm.name"),
                tooltip: () => this.Helper.Translation.Get("config.WalkOnFarm.description"),
                getValue: () => this.Config.WalkOnFarm,
                setValue: value => this.Config.WalkOnFarm = value
            );

            //from here on, ALL config is title-only
            configMenu.SetTitleScreenOnlyForNextOptions(
                mod: this.ModManifest,
                titleScreenOnly: true
                );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.AskForConfirmation.name"),
                tooltip: () => this.Helper.Translation.Get("config.AskForConfirmation.description"),
                getValue: () => this.Config.NeedsConfirmation,
                setValue: value => this.Config.NeedsConfirmation = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.RejectionDialogues.name"),
                tooltip: () => this.Helper.Translation.Get("config.RejectionDialogues.description"),
                getValue: () => this.Config.RejectionDialogue,
                setValue: value => this.Config.RejectionDialogue = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.ReceiveGifts.name"),
                tooltip: () => this.Helper.Translation.Get("config.ReceiveGifts.description"),
                getValue: () => this.Config.Gifts,
                setValue: value => this.Config.Gifts = value
            );
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.InLawComments.name"),
                tooltip: () => this.Helper.Translation.Get("config.InLawComments.description"),
                getValue: () => this.Config.InLawComments,
                setValue: value => this.Config.InLawComments = value,
                allowedValues: AllowedStringVals,
                formatAllowedValue: value => this.Helper.Translation.Get($"config.InLawComments.values.{value}")
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.ReplacerCompat.name"),
                tooltip: () => this.Helper.Translation.Get("config.ReplacerCompat.description"),
                getValue: () => this.Config.ReplacerCompat,
                setValue: value => this.Config.ReplacerCompat = value
            );

            //developer config
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: Extras.DebugTL,
                tooltip: null
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.Verbose.name"),
                tooltip: () => this.Helper.Translation.Get("config.Verbose.description"),
                getValue: () => this.Config.Verbose,
                setValue: value => this.Config.Verbose = value
            );
            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => this.Helper.Translation.Get("config.Debug.name"),
                tooltip: () => this.Helper.Translation.Get("config.Debug.Explanation"),
                getValue: () => this.Config.Debug,
                setValue: value => this.Config.Debug = value
            );

            configMenu.SetTitleScreenOnlyForNextOptions(
                mod: this.ModManifest,
                titleScreenOnly: false
                );
        }
        private void SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            //update from config
            ReceiveGift = Config.Gifts;
            InLawDialogue = Config.InLawComments;
            ReplacerOn = Config.ReplacerCompat;
            CanFollow = Config.WalkOnFarm;
            Debug = Config.Debug;

            //get translations
            if (ResponseList?.Count is not 2)
            {
                ResponseList.Add(new Response("AllowedEntry", this.Helper.Translation.Get("Visit.Yes")));
                ResponseList.Add(new Response("RejectedEntry", this.Helper.Translation.Get("Visit.No")));
            }

            FirstLoadedDay = true;

            //clear ALL values and temp data on load. this makes sure there's no conflicts with savedata cache (e.g if player had returned to title)
            ClearValues();
            CleanTempData();

            //check config
            if (Config.StartingHours >= Config.EndingHours)
            {
                this.Monitor.Log("Starting hours can't happen after ending hours! To use the mod, fix this and reload savefile.", LogLevel.Error);
                IsConfigValid = false;
                return;
            }
            else
            {
                this.Monitor.Log("User config is valid.");
                IsConfigValid = true;
            }

            //get all possible visitors- which also checks blacklist and married NPCs, etc.
            GetAllVisitors();

            /* if allowed, get all inlaws. 
             * this doesn't need daily reloading. NPC dispositions don't vary 
             * (WON'T add compat for the very small % of mods with conditional disp., its an expensive action)*/
            if (Config.InLawComments is "VanillaAndMod")
            {
                this.Monitor.Log("Getting all in-laws...");
                var tempdict = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions");
                foreach (string name in NameAndLevel.Keys)
                {
                    var temp = Moddeds.GetInlawOf(tempdict, name);
                    if (temp is not null)
                    {
                        InLaws.Add(name, temp);
                    }
                }
            }
        }
        private void DayStarted(object sender, DayStartedEventArgs e)
        {
            //if faulty config, don't do anything + mark as unvisitable
            if (IsConfigValid == false)
            {
                this.Monitor.Log("Configuration isn't valid. Mod will not work.", LogLevel.Warn);
                CanBeVisited = false;
                return;
            }

            //every sunday, friendship data is reloaded.
            if (Game1.dayOfMonth % 7 == 0 && FirstLoadedDay == false)
            {
                this.Monitor.Log("Day is sunday and not first loaded day. Reloading data...");
                NameAndLevel?.Clear();
                RepeatedByLV?.Clear();

                GetAllVisitors();
            }

            /* if no friendship with anyone OR festival day:
             * make unvisitable
             * return (= dont check custom schedule)
             */
            isFestivalToday = Utility.isFestivalDay(Game1.dayOfMonth, Game1.currentSeason);
            var anyInLV = RepeatedByLV.Any();
            //log info
            this.Monitor.Log($"isFestivalToday = {isFestivalToday}; anyInLV = {anyInLV}");

            if (anyInLV == false || isFestivalToday)
            {
                CanBeVisited = false;
                return;
            }

            CanBeVisited = true;

            //update animal and crop list
            if(Game1.currentSeason == "winter")
            {
                Crops?.Clear();
                Animals?.Clear();
            }
            else
            {
                Crops = Values.GetCrops();
                Animals = Values.GetAnimals();
            }
        }
        private void OnTimeChange(object sender, TimeChangedEventArgs e)
        {
            if (CanBeVisited == false)
            {
                return;
            }
            
            //update "follow" config 
            if (CanFollow != Config.WalkOnFarm)
            {
                CanFollow = Config.WalkOnFarm;
            }

            if (e.NewTime > Config.StartingHours && e.NewTime < Config.EndingHours && CounterToday < Config.MaxVisitsPerDay)
            {
                if (HasAnyVisitors == false && HasAnySchedules) //custom
                {
                    foreach (KeyValuePair<string, ScheduleData> pair in SchedulesParsed)
                    {
                        NPC visit = Game1.getCharacterFromName(pair.Key);
                        if (e.NewTime.Equals(pair.Value.From) && Values.IsFree(visit, false))
                        {
                            VisitorData = new TempNPC(visit);

                            currentCustom.Add(pair.Key, pair.Value);

                            VisitorName = visit.Name;

                            DurationSoFar = 0;

                            HasAnyVisitors = true;
                            TimeOfArrival = e.NewTime;
                            ControllerTime = 0;
                            CustomVisiting = true;

                            if (Config.NeedsConfirmation)
                            {
                                AskToEnter();
                            }
                            else
                            {
                                //add them to farmhouse (last to avoid issues)
                                Actions.AddCustom(Game1.getCharacterFromName(VisitorName), farmHouse, currentCustom[VisitorName], false); 

                                if (Config.Verbose == true)
                                {
                                    this.Monitor.Log($"\nHasAnyVisitors set to true.\n{VisitorName} will begin visiting player.\nTimeOfArrival = {TimeOfArrival};\nControllerTime = {ControllerTime};", LogLevel.Debug);
                                }
                            }

                            break;
                        }
                    }
                } //custom
                if (HasAnyVisitors == false) //random
                {
                    if (Random.Next(1, 101) <= Config.CustomChance && Game1.currentLocation == farmHouseAsLocation)
                    {
                        ChooseRandom();
                    }
                } //random
            }

            if (HasAnyVisitors == true)
            {
                NPC c = Game1.getCharacterFromName(VisitorName);

                if (!(Game1.player.currentLocation.getCharacters().Contains(c)))
                {
                    if(Config.Debug)
                    {
                        this.Monitor.Log($"Character \"{c.Name}\" ({c.displayName}) is currently not in the map. This time change will not be considered for MaxTimeStay count. (Skipping...)", LogLevel.Debug);
                    }
                    return;
                }

                //in the future, add unique dialogue for when characters fall asleep in your house.
                var isVisitSleeping = c.isSleeping.Value;
                if (DurationSoFar >= MaxTimeStay || (CustomVisiting && e.NewTime.Equals(currentCustom[VisitorName].To)) || isVisitSleeping)
                {
                    this.Monitor.Log($"{c.Name} is retiring for the day.");

                    //if custom AND has custom dialogue: exit with custom. else normal
                    if (CustomVisiting == true)
                    {
                        var exitd = currentCustom[VisitorName].ExitDialogue;
                        if (!string.IsNullOrWhiteSpace(exitd))
                            Actions.RetireCustom(c, e.NewTime, farmHouse, exitd);
                        else
                            Actions.Retire(c, e.NewTime, farmHouse);

                        CustomVisiting = false;
                        currentCustom.Clear();
                    }
                    else
                    {
                        Actions.Retire(c, e.NewTime, farmHouse);
                    }

                    HasAnyVisitors = false;
                    CounterToday++;
                    TodaysVisitors.Add(VisitorName);
                    DurationSoFar = 0;
                    ControllerTime = 0;
                    VisitorName = null;

                    VisitorData = null;

                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"HasAnyVisitors = false, CounterToday = {CounterToday}, TodaysVisitors= {Actions.TurnToString(TodaysVisitors)}, DurationSoFar = {DurationSoFar}, ControllerTime = {ControllerTime}, VisitorName = {VisitorName}", LogLevel.Debug);
                    }
                    return;
                }
                else
                {
                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"{c.Name} will move around.", LogLevel.Debug);
                    }

                    if (e.NewTime.Equals(TimeOfArrival))
                    {
                        this.Monitor.Log($"Time of arrival equals current time. NPC won't move around", LogLevel.Debug);
                    }
                    else if (ControllerTime >= 1)
                    {
                        c.Halt();
                        c.controller = null;
                        ControllerTime = 0;
                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"ControllerTime = {ControllerTime}", LogLevel.Debug);
                        }
                    }
                    else
                    {
                        if(IsOutside)
                        {
                            FarmOutside.WalkAroundFarm(c.Name);
                            ControllerTime++;
                            DurationSoFar++;
                            this.Monitor.Log($"ControllerTime = {ControllerTime}, DurationSoFar = {DurationSoFar} ({DurationSoFar * 10} minutes).", LogLevel.Debug);

                            return;
                        }

                        c.controller = new PathFindController(c, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random), Random.Next(0, 4));

                        var AnyDialogue = currentCustom?[VisitorName]?.Dialogues.Any<string>();
                        bool hasCustomDialogue = AnyDialogue ?? false;

                        if (CustomVisiting == true && hasCustomDialogue == true)
                        {
                            c.setNewDialogue(currentCustom[VisitorName].Dialogues[0], true, false);

                            this.Monitor.Log($"Adding custom dialogue for {c.Name}...");
                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"Custom dialogue: {currentCustom[VisitorName].Dialogues[0]}", LogLevel.Debug);
                            }

                            //remove this dialogue from the queue
                            currentCustom[VisitorName].Dialogues.RemoveAt(0);
                        }
                        else if (Random.Next(0, 11) <= 5 && FurnitureList.Any())
                        {
                            c.setNewDialogue(
                                string.Format(
                                    Values.GetDialogueType(
                                        c, 
                                        DialogueType.Furniture), 
                                    Values.GetRandomObj
                                        (ItemType.Furniture)), 
                                true, 
                                false);

                            if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"Adding dialogue for {c.Name}...", LogLevel.Debug);
                            }
                        }

                        ControllerTime++;
                        if (Config.Verbose == true)
                        {
                            this.Monitor.Log($"ControllerTime = {ControllerTime}", LogLevel.Debug);
                        }
                    }

                    DurationSoFar++;
                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"DurationSoFar = {DurationSoFar} ({DurationSoFar * 10} minutes).", LogLevel.Debug);
                    }
                }
            }
        }
        private void DayEnding(object sender, DayEndingEventArgs e)
        {
            FirstLoadedDay = false;

            CleanTempData();

            SchedulesParsed?.Clear();

            if (Config.Verbose == true)
            {
                this.Monitor.Log("Clearing today's visitor list, visitor count, and all other temp info...", LogLevel.Debug);
            }
        }
        private void TitleReturn(object sender, ReturnedToTitleEventArgs e)
        {
            ClearValues();

            this.Monitor.Log($"Removing cached information: HasAnyVisitors= {HasAnyVisitors}, TimeOfArrival={TimeOfArrival}, CounterToday={CounterToday}, VisitorName={VisitorName}. TodaysVisitors, NameAndLevel, FurnitureList, and RepeatedByLV cleared.");
        }

        /*  methods used by mod  */
        private void ParseBlacklist()
        {
            this.Monitor.Log("Getting raw blacklist.");
            BlacklistRaw = Config.Blacklist;
            if (BlacklistRaw is null)
            {
                this.Monitor.Log("No characters in blacklist.");
            }

            var charsToRemove = new string[] { "-", ",", ".", ";", "\"", "\'", "/" };
            foreach (var c in charsToRemove)
            {
                BlacklistRaw = BlacklistRaw.Replace(c, string.Empty);
            }
            if (Config.Verbose == true)
            {
                Monitor.Log($"Raw blacklist: \n {BlacklistRaw} \nWill be parsed to list now.", LogLevel.Debug);
            }
            BlacklistParsed = BlacklistRaw.Split(' ').ToList();
        }
        private void CleanTempData()
        {
            CounterToday = 0;

            VisitorData = null;
            VisitorName = null;

            CustomVisiting = false;
            HasAnyVisitors = false;

            currentCustom?.Clear();
            TodaysVisitors?.Clear();

            if (MaxTimeStay != (Config.Duration - 1))
            {
                MaxTimeStay = (Config.Duration - 1);
                Monitor.Log($"MaxTimeStay = {MaxTimeStay}; Config.Duration = {Config.Duration};");
            }

            Animals?.Clear();
            Crops?.Clear();
            FurnitureList?.Clear();
            FurnitureList = Values.UpdateFurniture(Utility.getHomeOfFarmer(Game1.MasterPlayer));

            if (Config.Verbose == true)
            {
                Monitor.Log($"Furniture list updated. Count: {FurnitureList?.Count ?? 0}", LogLevel.Debug);
            }
        }
        private void ClearValues()
        {
            if (!string.IsNullOrWhiteSpace(Config.Blacklist))
            {
                ParseBlacklist();
            }
            else
            {
                BlacklistRaw = null;
                BlacklistParsed?.Clear();
            }

            TimeOfArrival = 0;
            CounterToday = 0;
            DurationSoFar = 0;
            ControllerTime = 0;

            HasAnyVisitors = false;
            CustomVisiting = false;

            VisitorName = null;
            VisitorData = null;

            InLaws.Clear();
            NameAndLevel?.Clear();
            RepeatedByLV?.Clear();
            TodaysVisitors?.Clear();
            FurnitureList?.Clear();
            currentCustom?.Clear();
            SchedulesParsed?.Clear();
        }
        private void ReloadCustomschedules()
        {
            this.Monitor.Log("Began reloading custom schedules.");

            SchedulesParsed?.Clear();

            var schedules = Game1.content.Load<Dictionary<string, ScheduleData>>("mistyspring.farmhousevisits/Schedules");

            if (schedules.Any())
            {
                foreach (KeyValuePair<string, ScheduleData> pair in schedules)
                {
                    this.Monitor.Log($"Checking {pair.Key}'s schedule...");
                    bool isPatchValid = Extras.IsScheduleValid(pair);

                    if (!isPatchValid)
                    {
                        this.Monitor.Log($"{pair.Key} schedule won't be added.", LogLevel.Error);
                    }
                    else
                    {
                        SchedulesParsed.Add(pair.Key, pair.Value); //NRE
                    }
                }
            }

            HasAnySchedules = SchedulesParsed?.Any() ?? false;

            this.Monitor.Log("Finished reloading custom schedules.");
        }
        
        //below methods: REQUIRED, dont touch UNLESS it's for bug-fixing
        private void ChooseRandom()
        {
            this.Monitor.Log("Getting random...");
            var RChoice = Random.Next(0, (RepeatedByLV.Count));

            VisitorName = RepeatedByLV[RChoice];
            this.Monitor.Log($"Random: {RChoice}; VisitorName= {VisitorName}");

            NPC visit = Game1.getCharacterFromName(VisitorName);

            if (Values.IsFree(visit, true))
            {
                //save values
                VisitorData = new TempNPC(visit);

                CustomVisiting = false;

                DurationSoFar = 0;

                HasAnyVisitors = true;
                TimeOfArrival = Game1.timeOfDay;
                ControllerTime = 0;


                if (Config.NeedsConfirmation)
                {
                    AskToEnter();
                }
                else
                {
                    //add them to farmhouse (last to avoid issues)
                    Actions.AddToFarmHouse(Game1.getCharacterFromName(VisitorName), farmHouse, false);

                    if (Config.Verbose == true)
                    {
                        this.Monitor.Log($"\nHasAnyVisitors set to true.\n{VisitorName} will begin visiting player.\nTimeOfArrival = {TimeOfArrival};\nControllerTime = {ControllerTime};", LogLevel.Debug);
                    }
                }
            }
            else
            {
                VisitorName = null;
            }
        }
        private void GetAllVisitors()
        {
            if (!IsConfigValid)
            {
                return;
            }

            this.Monitor.Log("Began obtaining all visitors.");
            if (!string.IsNullOrWhiteSpace(Config.Blacklist))
            {
                ParseBlacklist();
            }
            NPCNames = Game1.content.Load<Dictionary<string, string>>("Data\\NPCDispositions").Keys.ToList<string>();

            MaxTimeStay = (Config.Duration - 1);
            this.Monitor.Log($"MaxTimeStay = {MaxTimeStay}; Config.Duration = {Config.Duration};");

            farmHouseAsLocation = Utility.getHomeOfFarmer(Game1.MasterPlayer);
            farmHouse = Utility.getHomeOfFarmer(Game1.MasterPlayer);

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
                        var _IsDivorced = Values.IsDivorced(npcn);
                        var _IsMarried = Values.IsMarriedToPlayer(npcn);

                        if (_IsMarried == false && hearts is not 0)
                        {
                            if (BlacklistParsed is not null)
                            {
                                if (BlacklistParsed.Contains(npcn.Name))
                                {
                                    this.Monitor.Log($"{npcn.displayName} is in the blacklist.", LogLevel.Info);
                                }
                                else
                                {
                                    NameAndLevel.Add(name, hearts);
                                }
                            }
                            else if (_IsDivorced == true)
                            {
                                this.Monitor.Log($"{name} is Divorced.");
                            }
                            else
                            {
                                if (npcn.Name.Equals("Dwarf"))
                                {
                                    if (!Game1.MasterPlayer.canUnderstandDwarves)
                                        this.Monitor.Log("Player can't understand dwarves yet!");

                                    else
                                        NameAndLevel.Add(name, hearts);
                                }
                                else
                                    NameAndLevel.Add(name, hearts);
                            }
                        }
                        else
                        {
                            if (_IsMarried)
                            {
                                MarriedNPCs.Add(npcn.Name);
                                this.Monitor.Log($"Adding {npcn.displayName} (internal name {npcn.Name}) to married list...");
                            }

                            if (_IsDivorced)
                            {
                                this.Monitor.Log($"{name} is Divorced. They won't visit player");
                            }
                            else if (Config.Verbose == true)
                            {
                                this.Monitor.Log($"{name} won't be added to the visitor list.", LogLevel.Debug);
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

            FurnitureList?.Clear();
            FurnitureList = Values.UpdateFurniture(farmHouse);
            this.Monitor.Log($"Furniture count: {FurnitureList.Count}");

            this.Monitor.Log("Finished obtaining all visitors.");

            ReloadCustomschedules();
        }

        //if user wants confirmation for NPC to come in
        internal void AskToEnter()
        {
            //knock on door
            /*
            Game1.currentLocation.playSound("stoneStep", SoundContext.Default);
            System.Threading.Thread.Sleep(300);
            Game1.currentLocation.playSound("stoneStep", SoundContext.Default);
            System.Threading.Thread.Sleep(300);*/

            //get name, place in question string
            var displayName = Game1.getCharacterFromName(VisitorName).displayName;
            var formattedQuestion = string.Format(this.Helper.Translation.Get("Visit.AllowOrNot"), displayName);

            var EntryQuestion = new EntryQuestion(formattedQuestion, ResponseList, ActionList);

            //put it on the game
            Game1.activeClickableMenu = EntryQuestion;
        }
        internal void CancelVisit()
        {
            VisitorData = null;
            HasAnyVisitors = false;
            CustomVisiting = false;

            var visit = Game1.getCharacterFromName(VisitorName);

            if(Config.RejectionDialogue)
            {
                //Game1.drawDialogue(visit, Values.GetRejectionResponse(visit));
                Game1.drawDialogue(visit, Values.GetDialogueType(visit, DialogueType.Rejected));
            }

            TodaysVisitors.Add(VisitorName);
            VisitorName = null;
        }
        internal void Proceed()
        {
            if(CustomVisiting)
            {
                Actions.AddCustom(Game1.getCharacterFromName(VisitorName), farmHouse, currentCustom[VisitorName], true);
            }
            else
            {
                Actions.AddToFarmHouse(Game1.getCharacterFromName(VisitorName), farmHouse, true);
            }
        }

        /*  console commands  */
        private void Reload(string command, string[] arg2) => GetAllVisitors();
        /*public void SetFromCommand(NPC visit)
        {

            VisitorData = new TempNPC(visit);

            DurationSoFar = 0;

            HasAnyVisitors = true;
            TimeOfArrival = Game1.timeOfDay;
            ControllerTime = 0;

            this.Monitor.Log($"\nHasAnyVisitors set to true.\n{VisitorName} will begin visiting player.\nTimeOfArrival = {TimeOfArrival};\nControllerTime = {ControllerTime};", LogLevel.Info);
        }*/
        //(other commands moved to a new file)

        /*  data  */
        private ModConfig Config;
        private List<string> RepeatedByLV = new();
        private static Random random;
        private bool isFestivalToday;
        private bool CanBeVisited;

        internal static Action<string, LogLevel> Log { get; private set; }
        internal static ITranslationHelper TL { get; private set; }
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
        private bool FirstLoadedDay;

        internal FarmHouse farmHouse { get; private set; }

        //configurable stuff
        internal static string BlacklistRaw { get; private set; }
        internal bool CustomVisiting { get; private set; }
        internal bool IsConfigValid { get; private set; }
        internal bool HasAnyVisitors { get; private set; }
        internal bool HasAnySchedules { get; private set; }

        //for entry question
        internal static List<Response> ResponseList { get; private set; } = new();
        internal static List<Action> ActionList { get; private set; } = new();

        //visit specific
        internal int TimeOfArrival { get; private set; }
        internal int CounterToday { get; private set; }
        internal int DurationSoFar { get; private set; }
        internal int MaxTimeStay { get; private set; }
        internal int ControllerTime { get; private set; }

        //information
        internal static Dictionary<string, int> NameAndLevel { get; private set; } = new();
        internal static List<string> NPCNames { get; private set; } = new();
        internal static List<string> BlacklistParsed { get; private set; } = new();
        internal static Dictionary<string, ScheduleData> SchedulesParsed { get; private set; } = new();
        internal static Dictionary<string, ScheduleData> currentCustom { get; private set; } = new();
        //selectable
        internal static List<string> FurnitureList { get; private set; } = new();
        internal static List<string> Animals { get; private set; } = new();
        internal static Dictionary<int,string> Crops { get; private set; } = new();

        //public data
        public static Dictionary<string, List<string>> InLaws { get; private set; } = new();
        public static List<string> MarriedNPCs { get; private set; } = new();
        public static string InLawDialogue { get; private set; }
        public static bool ReplacerOn { get; private set; }
        public static bool ReceiveGift { get; private set; }
        public static bool CanFollow { get; private set; } = false;
        public static string VisitorName { get; private set; }
        public static List<string> TodaysVisitors { get; private set; } = new();
        public static bool IsOutside { get; internal set; }
        public static bool Debug { get; internal set; }
    }
}