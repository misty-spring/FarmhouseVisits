using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using System.Collections.Generic;

namespace FarmVisitors
{
    internal class Values
    {
        internal static bool IsMarriedToPlayer(NPC c)
        {
            /*npc replacers will cause conflicts (friendshipdata checks internal but it seems to fail somehow)
             * tried using c.displayName but to no avail
            */
            if (c is null)
            {
                return false;
            }
            else if (Game1.IsMultiplayer)
            {
                foreach(Farmer eachFarmer in Game1.getAllFarmers())
                {
                    if(!eachFarmer.friendshipData.ContainsKey(c.Name))
                    {
                        ModEntry.ModMonitor.Log($"{c.Name} is not in the dictionary.");
                        return true;
                    }
                    //used to have "eachFarmer.spouse != null" inside the if
                    if (eachFarmer.IsMainPlayer && eachFarmer.friendshipData[c.Name].IsMarried())
                    {
                        ModEntry.ModMonitor.Log($"{c.Name} is married!");
                        return true;
                    }
                }
                return false;
            }
            else
            {         
                Farmer player = Game1.MasterPlayer;
                if (!player.friendshipData.ContainsKey(c.Name))
                {
                    ModEntry.ModMonitor.Log($"{c.Name} is not in the dictionary.");
                    return true;
                }
                if (player.friendshipData[c.Name].IsMarried())
                {
                    ModEntry.ModMonitor.Log($"{c.Name} is married!");
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
            var r = Game1.random.Next(1, 4);

            if (instance.SocialAnxiety.Equals(1)) //shy?
            {
                return ModEntry.ModHelper.Translation.Get($"NPCWalkIn.Shy{r}");
            }
            else
            {
                return ModEntry.ModHelper.Translation.Get($"NPCWalkIn.Outgoing{r}");
            }
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

        internal static string TalkAboutFurniture(NPC c)
        {
            var r = Game1.random.Next(1, 4);

            if (c.SocialAnxiety.Equals(1)) //shy?
            {
                return ModEntry.ModHelper.Translation.Get($"NPCFurniture.Shy{r}");
            }
            else
            {
                return ModEntry.ModHelper.Translation.Get($"NPCFurniture.Outgoing{r}");
            }
        }
        internal static string GetRandomFurniture()
        {
            var list = ModEntry.FurnitureList;
            int amount = list.Count;

            //if there's no furniture, return null
            if(amount is 0)
            {
                return null;
            }

            //choose random index and return itsdisplayname
            var r = Game1.random.Next(0, (amount + 1));
            return list[r].DisplayName;
        }
        internal static List<Furniture> UpdateFurniture(FarmHouse farmHouse)
        {
            List<Furniture> templist = new();
            foreach (Furniture f in farmHouse.furniture)
            {
                templist.Add(f);
            }
            return templist;
        }
    }
}
