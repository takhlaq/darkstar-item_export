using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                    arg = args[i].Trim('-');
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
            if (!config.ContainsKey("dspMod"))
            {
                config["dspMod"] = Environment.CurrentDirectory;
            }

            string dspModifierFile = Path.Combine(Path.GetFullPath(config["dspMod"]), "modifier.h");
            string itemFile = Path.Combine(Path.GetFullPath(config["in"]), "item_general.xml");
            string item2File = Path.Combine(Path.GetFullPath(config["in"]), "item_general2.xml");
            string itemArmorFile = Path.Combine(Path.GetFullPath(config["in"]), "item_armor.xml");
            string itermArmor2File = Path.Combine(Path.GetFullPath(config["in"]), "item_armor2.xml");
            string itemWeaponFile = Path.Combine(Path.GetFullPath(config["in"]), "item_weapon.xml");

            LoadDSPModifiers(dspModifierFile);

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
 
        static List<Modifier> GetModifiersFromDescription(string description)
        {
            List<Modifier> mods = new List<Modifier>();

            return mods;
        }

        static bool LoadDSPModifiers(string fileName)
        {
            if (File.Exists(fileName))
            {
                var fileStr = File.ReadAllText(fileName);
                var defStartPos = fileStr.IndexOf(@"enum class Mod");

                if (defStartPos >= 0)
                {
                    var defEndPos = fileStr.IndexOf(@"};", defStartPos);
                    var enumStr = fileStr.Substring(defStartPos, defEndPos - defStartPos);
                    var enumStrStart = enumStr.IndexOf("{", defStartPos, 1);

                    var regex = new Regex(@"\s+(\w+)\s+=(?:\s+|)(\d+),(?:\s+|)(?:(.*)\n|)", RegexOptions.CultureInvariant);

                    var enumMatches = regex.Matches(enumStr);

                    foreach (Match enumMatch in enumMatches)
                    {
                        Modifier mod = new Modifier();
                        mod.ModifierName = enumMatch.Groups[1].Value;
                        mod.ModifierId = Convert.ToInt32(enumMatch.Groups[2].Value);
                        
                        // find description str matches
                        if (enumMatch.Groups.Count > 3)
                        {
                            var parserNameRegex = new Regex(@".*(?:\s+|)\{(?:\s+|)parserNameMatch\:(?:\s+|)(.*)(?:\s+|)\}");
                            var parserValueRegex = new Regex(@".*(?:\s+|)\{(?:\s+|)parserValueMatch\:(?:\s+|)(.*)(?:\s+|)\}");
                            var modConversionRegex = new Regex(@".*(?:\s+|)\{(?:\s+|)modConversion\:(?:\s+|)(.*)(?:\s+|)\}"); // todo: should be something like value/100 or something

                            mod.ModifierValueRegex = parserValueRegex.Matches(enumMatch.Groups[3].Value);
                            mod.ModifierNameRegex = parserNameRegex.Matches(enumMatch.Groups[3].Value);
                            mod.ModifierConversion = modConversionRegex.Match(enumMatch.Groups[3].Value);
                        }
                    }
                }
            }
            return false;
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
