using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using System;
using System.Collections.Generic;
using System.Linq;

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

                WarpHome(c);

                if(c.Schedule is null)
                {
                    c.InvalidateMasterSchedule();

                    var sched = c.getSchedule(Game1.dayOfMonth);
                    c.Schedule.Clear();
                    foreach (KeyValuePair<int, SchedulePathDescription> pair in sched)
                    {
                        c.Schedule.Add(pair.Key, pair.Value);
                    }
                }

                c.checkSchedule(currentTime);
                c.moveCharacterOnSchedulePath();
                c.warpToPathControllerDestination();
            }
            catch(Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error while returning NPC: {ex}", LogLevel.Error);
            }
        }

        private static void WarpHome(NPC c)
        {
            try
            {
                //get dict from cached one in ModEntry
                Dictionary<string, string> dictionary = ModEntry.NPCDisp;

                //make array with NPC info
                string[] array = dictionary[c.Name].Split('/');

                //check second-to-last position (which is the map they spawn in)
                int PositionToCheck = array.Length - 1;

                //make a new array with the info of that position, split by each space/" "
                string[] InitialMap = array[PositionToCheck].Split(' ');
                ModEntry.ModMonitor.Log($"{c.Name} initial map: {InitialMap[0]}");

                //get X, Y from the array (point will use these to warp)
                var x = Int16.Parse(InitialMap[1]);
                var y = Int16.Parse(InitialMap[2]);
                ModEntry.ModMonitor.Log($"{c.Name} position: {InitialMap[1]}, {InitialMap[2]}");

                //finally, warp NPC to location at x,y we grabbed before
                Game1.warpCharacter(c, c.DefaultMap, c.DefaultPosition);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error while warping back home: {ex}", LogLevel.Error);
            }
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
