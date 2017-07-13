using System;
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
    class Parser
    {
        public Dictionary<string, string> Config = new Dictionary<string, string>();
        public Dictionary<string, List<string>> ItemQueries = new Dictionary<string, List<string>>();
        public Dictionary<string, Modifier> DSPModifiers = new Dictionary<string, Modifier>();
        public char[] AlphaCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz \"".ToCharArray(); 
        public Parser(string[] args)
        {
            for (var i = 0; i < args.Length; ++i)
            {
                string arg = "", val = "";
                try
                {
                    arg = args[i].Trim('-');
                    val = args[i + 1];
                    Config[arg] = val;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Invalid argument: --{0} {1}", arg, val);
                }
            }
            if (!Config.ContainsKey("in"))
            {
                Config["in"] = Environment.CurrentDirectory;
            }
            if (!Config.ContainsKey("dspMod"))
            {
                Config["dspMod"] = Environment.CurrentDirectory;
            }

            string dspModifierFile = Path.Combine(Path.GetFullPath(Config["dspMod"]), "modifier.h");
            string itemFile = Path.Combine(Path.GetFullPath(Config["in"]), "item_general.xml");
            string item2File = Path.Combine(Path.GetFullPath(Config["in"]), "item_general2.xml");
            string itemArmorFile = Path.Combine(Path.GetFullPath(Config["in"]), "item_armor.xml");
            string itemArmor2File = Path.Combine(Path.GetFullPath(Config["in"]), "item_armor2.xml");
            string itemWeaponFile = Path.Combine(Path.GetFullPath(Config["in"]), "item_weapon.xml");

            LoadDSPModifiers(dspModifierFile);
            File.WriteAllText("item_mods.sql", "");

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

        List<Modifier> GetModifiersFromDescription(string description)
        {
            // todo: split at each number + whitespace and filter out latents
            Dictionary<int, Modifier> mods = new Dictionary<int, Modifier>();
            foreach (var dspModifier in DSPModifiers)
            {
                Modifier mod = new Modifier();
                if (dspModifier.Value.ModifierNameRegex.Length > 0)
                {
                    foreach (var nameStrRegex in dspModifier.Value.ModifierNameRegex)
                    {
                        var nameRegex = new Regex(nameStrRegex);
                        var nameMatches = nameRegex.Matches(description);

                        if (nameMatches.Count > 0)
                        {
                            mod.ModifierName = nameMatches[0].Groups[1].Value;
                            break;
                        }
                    }
                }
                else
                {
                    int pos = description.IndexOf(dspModifier.Key);

                    if (pos != -1)
                    {
                        mod.ModifierName = description.Substring(pos, dspModifier.Key.Length);
                        mod.ModifierName = mod.ModifierName.Length > 1 ? mod.ModifierName : null;
                    }
                }

                /*
                // if (true || dspModifier.Value.ModifierValueRegex.Length > 0)
                {

                    //foreach (var valueStrRegex in dspModifier.Value.ModifierValueRegex)
                    {
                        var valueStrRegex = "((?:+|-|)\d+)";
                        var valueRegex = new Regex(mod.ModifierName + @"(?:\s+|)(?:\:|)" + valueStrRegex);
                        var valueMatches = valueRegex.Matches(description);

                        if (valueMatches.Count > 0)
                        {

                        }
                    }
                }
                //*/
                //else
                if (mod.ModifierName != null)
                {
                    mod.ModifierId = dspModifier.Value.ModifierId;
                    int defPos = description.IndexOf(mod.ModifierName) + mod.ModifierName.Length;
                    int defEndPos = description.IndexOfAny(AlphaCharacters, defPos);
                    defEndPos = defEndPos == -1 ? description.Length : defEndPos;
                    mod.ModifierValue = description.Substring(defPos, defEndPos - defPos).Replace("\r\n", "").Replace("?","").Replace(":","");
                    //Console.WriteLine(mod.ModifierName + " " + mod.ModifierValue);

                    mod.ModifierComment = $" -- {mod.ModifierName}: {mod.ModifierValue} ";

                    // todo: mod conversion
                    if (mod.ModifierValue.Length > 0)
                        mods.Add(mod.ModifierId, mod);
                }
            }
            if (mods.Count == 0)
                Console.WriteLine(description);
            return mods.Values.ToList();
        }

        bool LoadDSPModifiers(string fileName)
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
                            var parserNameRegex = new Regex(@".*(?:\s+|)\{(?:\s+|)parserNameMatch\:(?:\s+|)(.*)(?:\s+|)\} ", RegexOptions.CultureInvariant);
                            var parserValueRegex = new Regex(@".*(?:\s+|)\{(?:\s+|)parserValueMatch\:(?:\s+|)(.*)(?:\s+|)\} ", RegexOptions.CultureInvariant);
                            var modConversionRegex = new Regex(@".*(?:\s+|)\{(?:\s+|)modConversion\:(?:\s+|)(.*)(?:\s+|)\} ", RegexOptions.CultureInvariant); // todo: should be something like value/100 or something

                            mod.ModifierValueRegex = StringArrayFromMatchCollection(parserValueRegex.Matches(enumMatch.Groups[3].Value));
                            mod.ModifierNameRegex = StringArrayFromMatchCollection(parserNameRegex.Matches(enumMatch.Groups[3].Value));
                            mod.ModifierConversion = modConversionRegex.Match(enumMatch.Groups[3].Value)?.Groups[1].Value;
                        }

                        try
                        {
                            DSPModifiers.Add(mod.ModifierName, mod);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message + " " + enumMatch.Value);
                        }
                    }
                }
            }
            return false;
        }

        string[] StringArrayFromMatchCollection(MatchCollection matches, int fromGroupIndex = 1)
        {
            List<string> retStrs = new List<string>();

            foreach (Match match in matches)
            {
                for (var i = fromGroupIndex; i < match.Groups.Count; ++i)
                {
                    retStrs.Add(match.Groups[i].Value);
                }
            }

            return retStrs.ToArray();
        }

        bool ParseFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return false;
            }

            XmlDocument xml = new XmlDocument();
            xml.Load(fileName);
            Dictionary<ushort, Item> items = new Dictionary<ushort, Item>();

            foreach (XmlNode item in xml.GetElementsByTagName("thing"))
            {
                var Item = new Item();
                bool skip = false;
                foreach (XmlNode child in item.ChildNodes)
                {
                    string fieldName = child.Attributes.GetNamedItem("name").Value;

                    if (fieldName == "id")
                    {
                        Item.ItemId = ushort.Parse(child.InnerText);
                        if (skip = items.ContainsKey(Item.ItemId))
                            break;
                    }
                    else if (fieldName == "flags")
                        Item.Flags = UInt32.Parse(child.InnerText, NumberStyles.AllowHexSpecifier);
                    else if (fieldName == "valid-targets")
                        Item.ValidTargets = (ValidTargets)ulong.Parse(child.InnerText);
                    else if (fieldName == "name")
                        Item.Name = child.InnerText.Replace(' ', '_').ToLower().Replace("\'", "\\'");
                    else if (fieldName == "description")
                        Item.Modifiers = GetModifiersFromDescription(child.InnerText);
                    else if (fieldName == "level")
                        Item.Level = byte.Parse(child.InnerText);
                    else if (fieldName == "iLevel")
                        Item.ItemLevel = byte.Parse(child.InnerText);
                    else if (fieldName == "slots")
                        Item.Slots = (EquipSlots)ulong.Parse(child.InnerText);
                    else if (fieldName == "races")
                        Item.Races = (Races)UInt32.Parse(child.InnerText, NumberStyles.AllowHexSpecifier);
                    else if (fieldName == "jobs")
                        Item.Jobs = child.InnerText;
                    else if (fieldName == "superior-level")
                        Item.SuperiorLevel = ushort.Parse(child.InnerText);
                    else if (fieldName == "shield-size")
                        Item.ShieldSize = ushort.Parse(child.InnerText);
                    else if (fieldName == "max-charges")
                        Item.MaxCharges = ushort.Parse(child.InnerText);
                    else if (fieldName == "casting-time")
                        Item.CastingTime = UInt32.Parse(child.InnerText);
                    else if (fieldName == "use-delay")
                        Item.UseDelay = UInt32.Parse(child.InnerText);
                    else if (fieldName == "reuse-delay")
                        Item.ReuseDelay = UInt32.Parse(child.InnerText);


                }
                if (!skip)
                    items.Add(Item.ItemId, Item);
            }

            foreach (var Item in items.Values)
            {
                if (Item.Modifiers != null)
                {
                    List<string> lines = new List<string>();
                    foreach (var mod in Item.Modifiers)
                    {
                        var queryStr = $"INSERT INTO item_mods VALUES ({Item.ItemId}, {mod.ModifierId}, {mod.ModifierValue});";

                        if (mod.ModifierValue.Contains("%"))
                        {
                            mod.ModifierValue = mod.ModifierValue.Replace("%", "");
                            queryStr = "-- " + queryStr;
                        }
                        lines.Add(queryStr + mod.ModifierComment);
                    }
                    File.AppendAllLines("item_mods.sql", lines.ToArray());
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
