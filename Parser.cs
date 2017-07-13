using System;
using System.Collections.Generic;
using System.Data;
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
        public Dictionary<string, Modifier> DSPLatents = new Dictionary<string, Modifier>();
        public List<string> LatentSearchStrings = new List<string>();

        public char[] NonNumericCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz \"\'".ToCharArray(); 
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
            if (!Config.ContainsKey("dspSrc"))
            {
                Config["dspSrc"] = Environment.CurrentDirectory;
            }

            string dspModifierFile = Path.Combine(Path.GetFullPath(Config["dspSrc"]), "modifier.h");
            string dspLatentsFile = Path.Combine(Path.GetFullPath(Config["dspSrc"]), "latent_effect.h");
            string itemFile = Path.Combine(Path.GetFullPath(Config["in"]), "item_general.xml");
            string itemArmorFile = Path.Combine(Path.GetFullPath(Config["in"]), "item_armor.xml");
            string itemWeaponFile = Path.Combine(Path.GetFullPath(Config["in"]), "item_weapon.xml");

            LoadLatentSearchStrings();
            if (!LoadDSPModifiers(dspModifierFile))
                throw new Exception("Unable to load DarkStar Project modifier.h file!");
            if (!LoadDSPModifiers(dspLatentsFile, "enum LATENT", true) && Config.ContainsKey("latents"))
                throw new Exception("Unable to load DarkStar Project latent_effect.h file!");

            File.WriteAllText("item_mods.sql", "");
            File.WriteAllText("item_latents.sql", "");

            var itemGeneralItems = ParseFile(itemFile);
            var itemArmorItems = ParseFile(itemArmorFile);
            var itemWeaponItems = ParseFile(itemWeaponFile);

            // todo: seems the "2" files are just icons and shit
            // todo: output shit
            if (itemGeneralItems != null)
            {

            }

            if (itemArmorItems != null)
            {
                WriteItemLatentsAndModifiersQueries(itemArmorItems);
            }

            if (itemWeaponItems != null)
            {
                WriteItemLatentsAndModifiersQueries(itemWeaponItems);
            }
        }

        void LoadLatentSearchStrings()
        {
            if (File.Exists("../latent_strings.txt"))
            {
                var lines = File.ReadAllLines("../latent_strings.txt");

                foreach (var line in lines)
                {
                    if (line.Length > 0)
                    {
                        if (line[0] == '#')
                            continue;

                        LatentSearchStrings.Add(line);
                    }
                }
            }
        }

        List<Modifier> GetModifiersFromDescription(string description, Modifier mod = null, bool latents = false, string itemName = "")
        {
            // todo: split at each number + whitespace and filter out latents
            // todo: im retarded, need to substring one LatentSearchString to another and grab those mods separately..
            Dictionary<int, Modifier> mods = new Dictionary<int, Modifier>();
            var attributes = Regex.Split(description, @"(?:[\:]|)((?:[\+\- ]|)\d+)(?: |)", RegexOptions.CultureInvariant);

            var list = latents ? DSPLatents : DSPModifiers;

            foreach (var dspModifier in list)
            {
                mod = latents ? mod : new Modifier();
                if (dspModifier.Value.ModifierNameRegex.Length > 0)
                {
                    foreach (var nameStrRegex in dspModifier.Value.ModifierNameRegex)
                    {
                        var nameRegex = new Regex(nameStrRegex);
                        var nameMatches = nameRegex.Matches(description);

                        if (nameMatches.Count > 0)
                        {
                            if (!latents)
                                mod.ModifierName = nameMatches[0].Groups[1].Value;
                            else
                                mod.LatentEffectId = nameMatches[0].Groups[1].Value;

                            break;
                        }
                    }
                }
                else
                {
                    // case sensitive
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
                        var valueMatches = valueRegex.Matches(attribute);

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
                    int defEndPos = description.IndexOfAny(NonNumericCharacters, defPos);
                    defEndPos = defEndPos == -1 ? description.Length : defEndPos;
                    mod.ModifierValue = description.Substring(defPos, defEndPos - defPos).Replace("\r\n", "").Replace("?", "").Replace(":", "");

                    //Console.WriteLine(mod.ModifierName + " " + mod.ModifierValue);

                    if (itemName != "")
                        mod.ModifierComment = $" -- {itemName} {mod.ModifierName}: {mod.ModifierValue} ";
                    else
                        mod.ModifierComment = $" -- {mod.ModifierName}: {mod.ModifierValue} ";

                    for (var i = 0; i + 1 < attributes.Length; ++i)
                    {
                        var modStr = attributes[i];
                        var modVal = attributes[i + 1];

                        foreach (var latentStr in LatentSearchStrings)
                        {
                            if (modStr.Contains(latentStr) && modStr.Contains(mod.ModifierName) && modVal.Contains(mod.ModifierValue))
                            {
                                GetLatentsFromDescription(mod, modStr, latentStr);
                            }
                        }
                    }

                    // convert the value to dsp shit
                    if (dspModifier.Value.ModifierConversion != "")
                        mod.ModifierValue = Convert.ToDouble(new DataTable().Compute(dspModifier.Value.ModifierConversion.Replace("val", mod.ModifierValue), null)).ToString();

                    mod.ModifierComment.Replace(Environment.NewLine, "");
                    if (mod.ModifierValue.Length > 0 && !mods.ContainsKey(mod.ModifierId))
                    {
                        mods.Add(mod.ModifierId, mod);
                    }
                }
            }

            //if (mods.Count == 0)
            //    Console.WriteLine(description);
            return mods.Values.ToList();
        }

        bool GetLatentsFromDescription(Modifier mod, string latentStr, string searchStr)
        {
            mod.IsLatent = true;
            mod.ModifierComment += $"(search string: {latentStr.Replace(Environment.NewLine, "")})";
            latentStr = latentStr.Replace(searchStr, "");
            GetModifiersFromDescription(latentStr, mod, true);
            return true;
        }

        bool LoadDSPModifiers(string fileName, string enumStartStr = "", bool isLatent = false)
        {
            if (File.Exists(fileName))
            {
                var fileStr = File.ReadAllText(fileName);
                var defStartPos = fileStr.IndexOf(enumStartStr == "" ? "enum class Mod" : enumStartStr);

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
                            if (!isLatent)
                                DSPModifiers.Add(mod.ModifierName, mod);
                            else
                                DSPLatents.Add(mod.ModifierName, mod);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message + " " + enumMatch.Value);
                        }
                    }
                }
                return isLatent ? DSPLatents.Count != 0 : DSPModifiers.Count != 0;
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

        void WriteItemLatentsAndModifiersQueries(Dictionary<ushort, Item> items)
        {
            foreach (var item in items.Values)
            {
                if (item.Modifiers != null)
                {
                    List<string> modLines = new List<string>();
                    List<string> latentsLines = new List<string>();

                    modLines.Add("--------------------------" + Environment.NewLine + "-- " + item.Name + Environment.NewLine + "--------------------------");
                    latentsLines.Add("--------------------------" + Environment.NewLine + "-- " + item.Name + Environment.NewLine + "--------------------------");

                    foreach (var mod in item.Modifiers)
                    {
                        var queryStr = $"-- INSERT INTO item_latents VALUES ({item.ItemId}, {mod.ModifierId}, {mod.ModifierValue}, {mod.LatentEffectId}, ); " + mod.ModifierComment;

                        if (!mod.IsLatent)
                        {
                            if (mod.ModifierValue.Contains("%") || mod.ModifierValue.Contains("~"))
                                queryStr = "-- fuck: " + queryStr;

                            queryStr = $"INSERT INTO item_mods VALUES ({item.ItemId}, {mod.ModifierId}, {mod.ModifierValue}); " + mod.ModifierComment;
                            modLines.Add(queryStr);
                        }
                        else
                        {
                            if (mod.ModifierValue.Contains("%") || mod.ModifierValue.Contains("~"))
                                queryStr = "-- fuck: " + queryStr;

                            latentsLines.Add(queryStr);
                        }
                    }

                    if (modLines.Count > 1)
                    {
                        modLines.Add("");
                        File.AppendAllLines("item_mods.sql", modLines.ToArray());

                    }
                    if (latentsLines.Count > 1)
                    {
                        latentsLines.Add("");
                        File.AppendAllLines("item_latents.sql", latentsLines.ToArray());
                    }
                }
            }
        }

        public void WriteItemGeneralQueries(Dictionary<ushort, Item> items)
        {
            foreach (var item in items)
            {

            }
        }

        Dictionary<ushort, Item> ParseFile(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            XmlDocument xml = new XmlDocument();
            xml.Load(fileName);
            Dictionary<ushort, Item> items = new Dictionary<ushort, Item>();

            foreach (XmlNode thing in xml.GetElementsByTagName("thing"))
            {
                var item = new Item();
                bool skip = false;
                foreach (XmlNode child in thing.ChildNodes)
                {
                    string fieldName = child.Attributes.GetNamedItem("name").Value;

                    if (fieldName == "id")
                    {
                        item.ItemId = ushort.Parse(child.InnerText);
                        if (skip = items.ContainsKey(item.ItemId))
                            break;
                    }
                    else if (fieldName == "flags")
                        item.Flags = UInt32.Parse(child.InnerText, NumberStyles.AllowHexSpecifier);
                    else if (fieldName == "valid-targets")
                        item.ValidTargets = (ValidTargets)ulong.Parse(child.InnerText,NumberStyles.AllowHexSpecifier);
                    else if (fieldName == "name")
                        item.Name = child.InnerText.Replace(' ', '_').ToLower().Replace("\'", "\\'");
                    else if (fieldName == "description")
                        item.Modifiers = GetModifiersFromDescription(child.InnerText, null, false, item.Name);
                    else if (fieldName == "level")
                        item.Level = ushort.Parse(child.InnerText);
                    else if (fieldName == "iLevel")
                        item.ItemLevel = ushort.Parse(child.InnerText);
                    else if (fieldName == "slots")
                        item.Slots = (EquipSlots)ulong.Parse(child.InnerText);
                    else if (fieldName == "races")
                        item.Races = (Races)UInt32.Parse(child.InnerText, NumberStyles.AllowHexSpecifier);
                    else if (fieldName == "jobs")
                        item.Jobs = child.InnerText;
                    else if (fieldName == "superior-level")
                        item.SuperiorLevel = ushort.Parse(child.InnerText);
                    else if (fieldName == "shield-size")
                        item.ShieldSize = ushort.Parse(child.InnerText);
                    else if (fieldName == "max-charges")
                        item.MaxCharges = ushort.Parse(child.InnerText);
                    else if (fieldName == "casting-time")
                        item.CastingTime = UInt32.Parse(child.InnerText);
                    else if (fieldName == "use-delay")
                        item.UseDelay = UInt32.Parse(child.InnerText);
                    else if (fieldName == "reuse-delay")
                        item.ReuseDelay = UInt32.Parse(child.InnerText);
                }
                if (!skip)
                    items.Add(item.ItemId, item);
            }
            return items;
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
