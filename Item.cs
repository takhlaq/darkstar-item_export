using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace darkstar_item_export
{

    class Item
    {
        public ushort ItemId;
        public string Name;
        public ulong Flags;
        public byte StackSize;
        public List<Modifier> Modifiers;
        public ValidTargets ValidTargets;
        public ushort Level;
        public ushort ItemLevel;
        public string Jobs;
        public Races Races;
        public EquipSlots Slots;
        public ushort SuperiorLevel;
        public ushort MaxCharges;
        public ushort ShieldSize;
        public ulong CastingTime;
        public ulong UseDelay;
        public ulong ReuseDelay;
    }
}
