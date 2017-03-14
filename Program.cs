using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace darkstar_item_export
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, string> config = new Dictionary<string, string>();
            Dictionary<string, List<string>> itemQueries = new Dictionary<string, List<string>>();
            Dictionary<string, Modifier> dspModifiers = new Dictionary<string, Modifier>();

            for (var i = 0; i < args.Length; ++i)
            {
                string arg = "", val = "";
                try
                {
                    arg = args[i];
                    val = args[i + 1];
                    config[arg] = val;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Invalid argument: --{0} {1}", arg, val);
                }
            }
            if (!config.ContainsKey("in"))
            {
                config["in"] = Environment.CurrentDirectory;
            }
            string itemFile = Path.Combine(Path.GetFullPath(config["in"]), "item_general.xml");
            string item2File = Path.Combine(Path.GetFullPath(config["in"]), "item_general2.xml");
            string itemArmorFile = Path.Combine(Path.GetFullPath(config["in"]), "item_armor.xml");
            string itermArmor2File = Path.Combine(Path.GetFullPath(config["in"]), "item_armor2.xml");
            string itemWeaponFile = Path.Combine(Path.GetFullPath(config["in"]), "item_weapon.xml");

            if (!ParseFile(itemFile))
            {
                
            }
            if (!ParseFile(item2File))
            {
                
            }
            if (!ParseFile(itemArmorFile))
            {
                
            }
            if (!ParseFile(itemWeaponFile))
            {
                
            }
        }

        static Dictionary<string, Modifier> LoadDarkstarModifiers(string fileName)
        {
            Dictionary<string, Modifier> modifiers = new Dictionary<string, Modifier>();
            if (!File.Exists(fileName))
            {
                Console.WriteLine("Unable to find modifiers.h! {0}", fileName);
                return modifiers;
            }

            var fileLines = File.ReadAllLines(fileName);
            bool read = false;
            foreach (var line in fileLines)
            {
                if (line.Contains("enum class Mod"))
                    read = true;
                else if (line.Contains("};"))
                    read = false;

                if (read && line.ToList()[0] != '{')
                {

                }
            }

            return modifiers;
        }

 
        static List<Modifier> GetModifiersFromDescription(string description)
        {
            List<Modifier> mods = new List<Modifier>();

            return mods;
        }

        static bool ParseFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return false;
            }

            XmlDocument xml = new XmlDocument();
            xml.Load(fileName);

            foreach (XmlNode item in xml.GetElementsByTagName("thing"))
            {
                var Item = new Item();
                foreach (XmlNode child in item.ChildNodes)
                {
                    string fieldName = child.Attributes.GetNamedItem("name").Value;
                    if (fieldName == "id")
                    {
                        Item.ItemId = ushort.Parse(child.InnerText);
                    }
                    else if (fieldName == "flags")
                    {
                        Item.Flags = UInt32.Parse(child.InnerText, NumberStyles.AllowHexSpecifier);
                    }
                    else if (fieldName == "valid-targets")
                    {
                        Item.ValidTargets = (ValidTargets)ulong.Parse(child.InnerText);
                    }
                    else if (fieldName == "name")
                    {
                        Item.Name = child.InnerText.Replace(' ', '_').ToLower().Replace("\'", "\\'");
                    }
                    else if (fieldName == "description")
                    {
                        Item.Modifiers = GetModifiersFromDescription(child.InnerText);
                    }
                    else if (fieldName == "level")
                    {
                        Item.Level = byte.Parse(child.InnerText);
                    }
                    else if (fieldName == "iLevel")
                    {
                        Item.ItemLevel = byte.Parse(child.InnerText);
                    }
                    else if (fieldName == "slots")
                    {
                        Item.Slots = (EquipSlots)ulong.Parse(child.InnerText);
                    }
                    else if (fieldName == "races")
                    {
                        Item.Races = (Races)UInt32.Parse(child.InnerText, NumberStyles.AllowHexSpecifier);
                    }
                    else if (fieldName == "jobs")
                    {
                        Item.Jobs = child.InnerText;
                    }
                    else if (fieldName == "superior-level")
                    {
                        Item.SuperiorLevel = ushort.Parse(child.InnerText);
                    }
                    else if (fieldName == "shield-size")
                    {
                        Item.ShieldSize = ushort.Parse(child.InnerText);
                    }
                    else if (fieldName == "max-charges")
                    {
                        Item.MaxCharges = ushort.Parse(child.InnerText);
                    }
                    else if (fieldName == "casting-time")
                    {
                        Item.CastingTime = UInt32.Parse(child.InnerText);
                    }
                    else if (fieldName == "use-delay")
                    {
                        Item.UseDelay = UInt32.Parse(child.InnerText);
                    }
                    else if (fieldName == "reuse-delay")
                    {
                        Item.ReuseDelay = UInt32.Parse(child.InnerText);
                    }
                }

            }

            return false;
        }
    }
    #region defines

    internal enum ValidTargets
    {

    }

    internal enum EquipSlots
    {
        Main,
        Sub,
        Ranged,
        Ammo,
        Head,
        Body,
        Hands,
        Legs,
        Feet,
        Neck,
        Waist,
        LeftEar,
        RightEar,
        LeftRing,
        RightRing,
        Back
    }

    internal enum Jobs
    {
        None,
        Warrior,
        Monk,
        WhiteMage,
        RedMage,
        Thief,
        Paladin,
        DarkKnight,
        Beastmaster,
        Bard,
        Ranger,
        Samurai,
        Ninja,
        Dragoon,
        Summoner,
        BlueMage,
        Corsair,
        Puppetmaster,
        Dancer,
        Scholar,
        Geomancer,
        RuneFencer
    }

    internal enum Races
    {
        FemaleHume,
        MaleHume,
        FemaleElvaan, // ew
        MaleElvaan,   // double ew
        FemaleTarutaru,
        MaleTarutaru,
        WhasfsMum, // Galka
    }
    #endregion
}
