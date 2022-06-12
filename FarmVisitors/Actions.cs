using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using StardewValley.Objects;
using System;

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
                Game1.drawDialogue(c, Values.GetRetireDialogue(c));
                var position = farmHouse.getEntryLocation();
                try
                {
                    c.controller = new PathFindController(c, farmHouse, position, 2);
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log($"An error ocurred when pathing to entry: {ex}", LogLevel.Error);
                }
                finally
                {
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
            c.ignoreScheduleToday = false;
            c.followSchedule = true;
            c.checkSchedule(currentTime);

            c.moveCharacterOnSchedulePath();
            c.warpToPathControllerDestination();
        }
    }
}