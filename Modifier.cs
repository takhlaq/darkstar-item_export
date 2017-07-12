using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace darkstar_item_export
{
    internal struct Modifier
    {
        public int ModifierId { get; set; }
        public string ModifierName { get; set; }
        public string ModifierValue { get; set; }
        public MatchCollection ModifierValueRegex { get; set; }
        public MatchCollection ModifierNameRegex { get; set; }
        public Match ModifierConversion { get; set; }
        public string ErrorString { get; set; }
    }
}
