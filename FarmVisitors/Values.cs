using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;

namespace FarmVisitors
{
    internal class Values
    {
        //internal static Dictionary<string, string> StringsFromCS = ModEntry.ModHelper.GameContent.Load<Dictionary<string, string>>("Strings\\StringsFromCSFiles");
        internal static bool IsMarriedToPlayer(NPC c)
        {
            if(Game1.IsMultiplayer)
            {
                foreach(Farmer eachFarmer in Game1.getAllFarmers())
                {
                    //used to have "eachFarmer.spouse != null" inside the if
                    if (eachFarmer.IsMainPlayer && eachFarmer.friendshipData[c.Name].IsMarried())
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                Farmer player = Game1.MasterPlayer;
                if (player.friendshipData[c.Name].IsMarried())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal static bool IsVisitor(string c)
        {
            if (c.Equals(ModEntry.ModVisitor))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static string GetIntroDialogue(NPC npcv)
        {
            var r = Game1.random.Next(1,4);

            if(npcv.SocialAnxiety.Equals(0))
            {
                return ModEntry.ModHelper.Translation.Get($"NPCIntroduce.Outgoing{r}");
            }
            else
            {
                return ModEntry.ModHelper.Translation.Get($"NPCIntroduce.Shy{r}");
            }
        }

        internal static string StringByPersonality(NPC instance)
        {
            var r = Game1.random.Next(1, 4);

            if (instance.SocialAnxiety.Equals(1)) //shy?
            {
                return ModEntry.ModHelper.Translation.Get($"NPCGreet.Shy{r}");
            }
            else
            {
                return ModEntry.ModHelper.Translation.Get($"NPCGreet.Outgoing{r}");
            }
        }

        internal static string GetNPCGone()
        {
            return ModEntry.ModHelper.Translation.Get("NPCGoneWhileOutside");
        }

        internal static string GetRetireDialogue(NPC c)
        {
            if(c.SocialAnxiety.Equals(0))
            {
                return ModEntry.ModHelper.Translation.Get("NPCRetiring.Outgoing");
            }
            else
            {
                return ModEntry.ModHelper.Translation.Get("NPCRetiring.Shy");
            }
        }

        internal static string GetTextOverHead(NPC instance)
        {
            //Dictionary<string, string> StringsFromCS = ModEntry.ModHelper.GameContent.Load<Dictionary<string, string>>("Strings\\StringsFromCSFiles");

            var r = Game1.random.Next(1, 4);

            if (instance.SocialAnxiety.Equals(1)) //shy?
            {
                //return StringsFromCS.GetValueOrDefault("NPC.cs.4071");
                return ModEntry.ModHelper.Translation.Get($"NPCWalkIn.Shy{r}");
            }
            else
            {
                //return StringsFromCS.GetValueOrDefault("NPC.cs.4068");
                return ModEntry.ModHelper.Translation.Get($"NPCWalkIn.Outgoing{r}");
            }
        }

        internal static Point GetHouseSize()
        {
            Point point = new();

            foreach (Farmer eachFarmer in Game1.getAllFarmers())
            {
                if (eachFarmer.IsMainPlayer)
                {
                    int level = eachFarmer.HouseUpgradeLevel;
                    if(level is 0)
                    {
                        point = new Point(3, 10);
                    }
                    if(level is 1)
                    {
                        point = new Point(9, 10);
                    }
                    else
                    {
                        point = new Point(12, 19);
                    }
                }
            }
            return point;
        }

        internal static string GetSeasonalGifts()
        {
            string defaultgifts = "419 246 423 216 247 688 176 180 436 174 182 184 424 186 438"; //Vinegar, Wheat Flour, Rice, Bread, Oil, Warp Totem: Farm, small Egg (2 types),Large Egg (2 types), Goat Milk, Milk, Cheese, Large Milk, L. Goat Milk

            if (Game1.IsSpring)
            {
                return $"{defaultgifts} 400 252 222 190 610 715"; //Strawberry, Rhubarb, Rhubarb Pie, Cauliflower, Fruit Salad, Lobster
            }
            else if (Game1.IsSummer)
            {
                return $"{defaultgifts} 636 256 258 270 690 260"; //Peach, Tomato, Blueberry, Corn, Warp Totem: Beach, Hot Pepper
            }
            else if (Game1.IsFall)
            {
                return $"{defaultgifts} 276 284 282 395 689 613"; //Pumpkin, Beet, Cranberries, Coffee, Warp Totem: Mountains, Apple
            }
            else if (Game1.IsWinter)
            {
                return $"{defaultgifts} 414 144 147 178 787 440"; //Crystal Fruit, Pike, Herring, Hay, Battery Pack, Wool
            }
            else
            {
                return defaultgifts;
            }
        }
    }
}
