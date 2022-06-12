using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using System;
using System.Collections.Generic;

namespace FarmVisitors
{
    internal class Actions
    {
        internal static void AddToFarmHouse(string visitor, FarmHouse farmHouse)
        {
            try
            {
                if (!Values.IsVisitor(visitor))
                {
                    ModEntry.ModMonitor.Log($"{visitor} is not a visitor!");
                    return;
                }

                NPC npcv = Game1.getCharacterFromName(visitor);

                npcv.ignoreScheduleToday = true;
                npcv.temporaryController = null;

                Game1.drawDialogue(npcv, Values.GetIntroDialogue(npcv));

                var position = farmHouse.getEntryLocation();
                position.Y--;
                npcv.faceDirection(0);
                Game1.warpCharacter(npcv, "FarmHouse", position);

                npcv.showTextAboveHead(string.Format(Values.GetTextOverHead(npcv),Game1.MasterPlayer.Name));

                npcv.CurrentDialogue.Push(new Dialogue(string.Format(Values.StringByPersonality(npcv), Values.GetSeasonalGifts()), npcv));
                //add but different for attempt
                npcv.setNewDialogue(string.Format(Values.StringByPersonality(npcv), Values.GetSeasonalGifts()), true, false);

                if (Game1.currentLocation == farmHouse)
                {
                    Game1.currentLocation.playSound("doorClose", NetAudio.SoundContext.NPC);
                }

                position.Y--;
                npcv.controller = new PathFindController(npcv, farmHouse, position, 0);
            }
            catch(Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error while adding to farmhouse: {ex}", LogLevel.Error);
            }

        }

        internal static void MoveAroundHouse(NPC c, FarmHouse farmHouse)
        {
            c.controller = new PathFindController(c, farmHouse, farmHouse.getRandomOpenPointInHouse(Game1.random), 2);
        }

        internal static void Retire(NPC c, int currentTime, FarmHouse farmHouse)
        {
            if (Game1.currentLocation == farmHouse)
            {
                try
                {
                    if(c.controller is not null)
                    {
                        c.Halt();
                        c.controller = null;
                    }
                    Game1.fadeScreenToBlack();
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log($"An error ocurred when pathing to entry: {ex}", LogLevel.Error);
                }
                finally
                {
                    Game1.drawDialogue(c, Values.GetRetireDialogue(c));

                    ReturnToNormal(c, currentTime);
                    Game1.currentLocation.playSound("doorClose", NetAudio.SoundContext.NPC);
                }
            }
            else
            {
                Game1.drawDialogueBox(string.Format(Values.GetNPCGone(), c.Name));
            }
        }
        internal static void ReturnToNormal(NPC c, int currentTime)
        {
            try
            {
                c.Halt();
                c.controller = null;

                c.ignoreScheduleToday = false;
                c.followSchedule = true;

                Game1.warpCharacter(c, "BusStop", new Point(2, 23));

                c.InvalidateMasterSchedule();

                c.checkSchedule(currentTime);

                if (c.Schedule is null)
                {
                    var sched = c.getSchedule(Game1.dayOfMonth);
                    c.Schedule.Clear();
                    foreach (KeyValuePair<int, SchedulePathDescription> pair in sched)
                    {
                        c.Schedule.Add(pair.Key, pair.Value);
                    }
                }

                c.moveCharacterOnSchedulePath();
                c.warpToPathControllerDestination();
            }
            catch(Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error while returning NPC: {ex}", LogLevel.Error);
            }
        }

        private static string ParseSchedule(NPC c)
        {
            var time = Game1.timeOfDay;
            var schedulename = c.dayScheduleName;
            var schedule = c.getMasterScheduleEntry(schedulename);
            string[] array = schedule.Split('/');
            foreach(string s in array)
            {
                string[] schedulepart = s.Split(' ');
                var scheduletime = Int16.Parse(schedulepart[0]);
                if(scheduletime > time)
                {
                    string scheduleloc = schedulepart[1];
                    return scheduleloc;
                }
            }
            return "Town";
        }

        internal static string TurnToString(List<string> list)
        {
            string result = "";

            foreach (string s in list)
            {
                if(s.Equals(list[0]))
                {
                    result = $"{s}";
                }
                else
                {
                    result = $"{result}, {s}";
                }
            }
            return result;
        }
    }
}
