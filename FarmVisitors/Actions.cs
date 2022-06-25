﻿using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FarmVisitors
{
    internal class Actions
    {
        /* regular visit *
         * the one used by non-scheduled NPCs */
        internal static void AddToFarmHouse(NPC visitor, FarmHouse farmHouse)
        {
            try
            {
                if (!Values.IsVisitor(visitor.Name))
                {
                    ModEntry.ModMonitor.Log($"{visitor.displayName} is not a visitor!");
                    return;
                }

                NPC npcv = visitor;

                bool isanimating = npcv.doingEndOfRouteAnimation.Value;

                if(isanimating == true || (npcv.doingEndOfRouteAnimation is not null && npcv.doingEndOfRouteAnimation.Value is true))
                {
                    RemoveAnimation(npcv);
                }
                if(npcv.CurrentDialogue.Any())
                {
                    npcv.CurrentDialogue.Clear();
                }

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

                if(ModEntry.RelativeComments == true && Game1.MasterPlayer.isMarried())
                {
                    InLawActions(visitor);
                }
            }
            catch(Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error while adding to farmhouse: {ex}", LogLevel.Error);
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

                if (c.DefaultMap.Equals(ModEntry.VisitorData.CurrentLocation))
                {
                    Point PositionFixed = new((int)(c.DefaultPosition.X / 64), (int)(c.DefaultPosition.Y / 64));

                    Game1.warpCharacter(c, c.DefaultMap, PositionFixed);
                }
                else
                {
                    Game1.warpCharacter(c, ModEntry.VisitorData.CurrentLocation, ModEntry.VisitorData.Position);
                }

                c.Sprite = ModEntry.VisitorData.Sprite;
                c.endOfRouteMessage.Value = ModEntry.VisitorData.AnimationMessage;

                if (c.Schedule is null)
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

                //change facing direction
                c.FacingDirection = ModEntry.VisitorData.Facing;
                c.facingDirection.Value = ModEntry.VisitorData.Facing;
                c.faceDirection(ModEntry.VisitorData.Facing);

                if (c.CurrentDialogue.Any() || c.CurrentDialogue is not null)
                {
                    c.CurrentDialogue.Clear();
                    c.resetCurrentDialogue();

                    c.update(Game1.currentGameTime, c.currentLocation);

                    int heartlvl = Game1.MasterPlayer.friendshipData[c.displayName].Points / 250;
                    c.checkForNewCurrentDialogue(heartlvl);

                    c.CurrentDialogue = ModEntry.VisitorData.DialoguePreVisit;
                }

                c.performTenMinuteUpdate(Game1.timeOfDay, c.currentLocation);
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error while returning NPC: {ex}", LogLevel.Error);
            }
        }

        private static void RemoveAnimation(NPC npcv)
        {
            try
            {
                ModEntry.ModMonitor.Log($"Stopping animation for {npcv.displayName} and resizing...");

                npcv.Sprite.StopAnimation();
                npcv.Sprite.SpriteWidth = 16;
                npcv.Sprite.SpriteHeight = 32;
                npcv.reloadSprite();

                npcv.Sprite.CurrentFrame = 0;
                npcv.faceDirection(0);

                if(npcv.endOfRouteMessage.Value is not null)
                {
                    npcv.endOfRouteMessage.Value.Remove(0);
                    npcv.endOfRouteMessage.Value = null;
                }
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Error while stopping {npcv.displayName} animation: {ex}", LogLevel.Error);
            }
        }

        internal static void Retire(NPC c, int currentTime, FarmHouse farmHouse)
        {
            /*!Game1.player.currentLocation.name.Value.StartsWith("Cellar")
            
            string currentloc = Game1.currentLocation.Name;
            string cellar = farmHouse.GetCellarName();
            !currentloc.Equals(cellar)*/

            if (Game1.currentLocation == farmHouse && Game1.currentLocation.Name.StartsWith("FarmHouse"))
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
                try
                {
                    if (c.controller is not null)
                    {
                        c.Halt();
                        c.controller = null;
                    }
                    ReturnToNormal(c, currentTime);

                    Game1.drawObjectDialogue(string.Format(Values.GetNPCGone(), c.displayName));
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log($"An error ocurred when pathing to entry: {ex}", LogLevel.Error);
                }
            }
        }
        
        /* extra *
         * if in-law, will have dialogue about it */
        private static void InLawActions(NPC visitor)
        {
            if (Moddeds.IsVanillaInLaw(visitor.Name))
            {
                if (Vanillas.InLawOfSpouse(visitor.Name) is true)
                {
                    visitor.setNewDialogue(Vanillas.GetInLawDialogue(visitor.Name));

                    if (Game1.MasterPlayer.getChildrenCount() > 0)
                    {
                        visitor.setNewDialogue(Vanillas.AskAboutKids(Game1.MasterPlayer));
                    }
                }
            }
            else
            {
                var nameof = Moddeds.GetRelativeName(visitor.Name);
                if (nameof is not null)
                {
                    //use the prev-checked name to format string
                    string formatted = string.Format(Moddeds.GetDialogueRaw(), nameof);
                    visitor.setNewDialogue(formatted);

                    if (Game1.MasterPlayer.getChildrenCount() > 0)
                    {
                        visitor.setNewDialogue(Vanillas.AskAboutKids(Game1.MasterPlayer));
                    }
                }
            }
        }

        /* customized visits *
         * ones set by user via ContentPatcher*/
        internal static void AddCustom(NPC c, FarmHouse farmHouse, ScheduleData data)
        {
            try
            {
                if (!Values.IsVisitor(c.Name))
                {
                    ModEntry.ModMonitor.Log($"{c.displayName} is not a visitor!");
                    return;
                }

                NPC npcv = c;

                bool isanimating = npcv.doingEndOfRouteAnimation.Value; // || npcv.goingToDoEndOfRouteAnimation.Value;
                if(isanimating == true || (npcv.doingEndOfRouteAnimation is not null && npcv.doingEndOfRouteAnimation.Value is true))
                {
                    RemoveAnimation(npcv);
                }
                if (npcv.CurrentDialogue.Any())
                {
                    npcv.CurrentDialogue.Clear();
                }

                npcv.ignoreScheduleToday = true;
                npcv.temporaryController = null;

                if(!string.IsNullOrWhiteSpace(data.EntryQuestion))
                {
                    Game1.drawDialogue(npcv, data.EntryQuestion);
                }
                else
                {
                    Game1.drawDialogue(npcv, Values.GetIntroDialogue(npcv));
                }

                var position = farmHouse.getEntryLocation();
                position.Y--;
                npcv.faceDirection(0);
                Game1.warpCharacter(npcv, "FarmHouse", position);

                if (!string.IsNullOrWhiteSpace(data.EntryBubble))
                {
                    npcv.showTextAboveHead(string.Format(data.EntryBubble, Game1.MasterPlayer.Name));
                }
                else
                {
                    npcv.showTextAboveHead(string.Format(Values.GetTextOverHead(npcv), Game1.MasterPlayer.Name));
                }

                if(!string.IsNullOrWhiteSpace(data.EntryDialogue))
                {
                    npcv.setNewDialogue(string.Format(Values.StringByPersonality(npcv), Values.GetSeasonalGifts()), true, false);
                }
                else
                {
                    npcv.setNewDialogue(string.Format(Values.StringByPersonality(npcv), Values.GetSeasonalGifts()), true, false);
                }

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
        internal static void RetireCustom(NPC c, int currentTime, FarmHouse farmHouse, string text)
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
                    Game1.drawDialogue(c, text);

                    ReturnToNormal(c, currentTime);
                    Game1.currentLocation.playSound("doorClose", NetAudio.SoundContext.NPC);
                }
            }
            else
            {
                try
                {
                    if (c.controller is not null)
                    {
                        c.Halt();
                        c.controller = null;
                    }
                    ReturnToNormal(c, currentTime);
                    
                    Game1.drawObjectDialogue(string.Format(Values.GetNPCGone(), c.displayName));
                }
                catch (Exception ex)
                {
                    ModEntry.ModMonitor.Log($"An error ocurred when pathing to entry: {ex}", LogLevel.Error);
                }
            }
        }

        //turn list to string
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