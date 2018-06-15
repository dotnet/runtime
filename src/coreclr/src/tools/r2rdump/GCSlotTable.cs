// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace R2RDump
{
    class GcSlotTable
    {
        public struct GcSlot
        {
            public int RegisterNumber { get; }
            public GcStackSlot StackSlot { get; }
            public GcSlotFlags Flags { get; }

            public GcSlot(int registerNumber, GcStackSlot stack, GcSlotFlags flags, bool isUntracked = false)
            {
                RegisterNumber = registerNumber;
                StackSlot = stack;
                if (isUntracked)
                {
                    Flags = GcSlotFlags.GC_SLOT_UNTRACKED;
                }
                else
                {
                    Flags = flags;
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                string tab3 = new string(' ', 12);

                if (StackSlot != null)
                {
                    sb.AppendLine($"{tab3}Stack:");
                    sb.AppendLine(StackSlot.ToString());
                }
                else
                {
                    sb.AppendLine($"{tab3}RegisterNumber: {RegisterNumber}");
                }
                sb.AppendLine($"{tab3}Flags: {Flags}");

                return sb.ToString();
            }
        }

        public uint NumRegisters { get; }
        public uint NumStackSlots { get; }
        public uint NumUntracked { get; }
        public uint NumSlots { get; }
        public List<GcSlot> GcSlots { get; }

        public GcSlotTable(byte[] image, Machine machine, GcInfoTypes gcInfoTypes, ref int bitOffset)
        {
            if (NativeReader.ReadBits(image, 1, ref bitOffset) != 0)
            {
                NumRegisters = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.NUM_REGISTERS_ENCBASE, ref bitOffset);
            }
            if (NativeReader.ReadBits(image, 1, ref bitOffset) != 0)
            {
                NumStackSlots = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.NUM_STACK_SLOTS_ENCBASE, ref bitOffset);
                NumUntracked = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.NUM_UNTRACKED_SLOTS_ENCBASE, ref bitOffset);
            }
            NumSlots = NumRegisters + NumStackSlots + NumUntracked;

            GcSlots = new List<GcSlot>();
            if (NumRegisters > 0)
            {
                DecodeRegisters(image, gcInfoTypes, ref bitOffset);
            }
            if ((NumStackSlots > 0) && (GcSlots.Count < gcInfoTypes.MAX_PREDECODED_SLOTS))
            {
                DecodeStackSlots(image, machine, gcInfoTypes, NumStackSlots, false, ref bitOffset);
            }
            if ((NumUntracked > 0) && (GcSlots.Count < gcInfoTypes.MAX_PREDECODED_SLOTS))
            {
                DecodeStackSlots(image, machine, gcInfoTypes, NumUntracked, true, ref bitOffset);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string tab2 = new string(' ', 8);
            string tab3 = new string(' ', 12);

            sb.AppendLine($"{tab2}NumSlots({NumSlots}) = NumRegisters({NumRegisters}) + NumStackSlots({NumStackSlots}) + NumUntracked({NumUntracked})");
            sb.AppendLine($"{tab2}GcSlots:");
            sb.AppendLine($"{tab3}-------------------------");
            foreach (GcSlot slot in GcSlots)
            {
                sb.Append(slot.ToString());
                sb.AppendLine($"{tab3}-------------------------");
            }

            return sb.ToString();
        }

        private void DecodeRegisters(byte[] image, GcInfoTypes gcInfoTypes, ref int bitOffset)
        {
            // We certainly predecode the first register
            uint regNum = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.REGISTER_ENCBASE, ref bitOffset);
            GcSlotFlags flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
            GcSlots.Add(new GcSlot((int)regNum, null, flags));

            for (int i = 1; i < NumRegisters; i++)
            {
                if ((uint)flags != 0)
                {
                    regNum = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.REGISTER_ENCBASE, ref bitOffset);
                    flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
                }
                else
                {
                    uint regDelta = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.REGISTER_DELTA_ENCBASE, ref bitOffset) + 1;
                    regNum += regDelta;
                }
                GcSlots.Add(new GcSlot((int)regNum, null, flags));
            }
        }

        private void DecodeStackSlots(byte[] image, Machine machine, GcInfoTypes gcInfoTypes, uint nSlots, bool isUntracked, ref int bitOffset)
        {
            // We have stack slots left and more room to predecode
            GcStackSlotBase spBase = (GcStackSlotBase)NativeReader.ReadBits(image, 2, ref bitOffset);
            int normSpOffset = NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.STACK_SLOT_ENCBASE, ref bitOffset);
            int spOffset = gcInfoTypes.DenormalizeStackSlot(normSpOffset);
            GcSlotFlags flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
            GcSlots.Add(new GcSlot(-1, new GcStackSlot(spOffset, spBase), flags, isUntracked));

            for (int i = 1; i < nSlots; i++)
            {
                spBase = (GcStackSlotBase)NativeReader.ReadBits(image, 2, ref bitOffset);
                if ((uint)flags != 0)
                {
                    normSpOffset = NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.STACK_SLOT_ENCBASE, ref bitOffset);
                    spOffset = gcInfoTypes.DenormalizeStackSlot(normSpOffset);
                    flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
                }
                else
                {
                    int normSpOffsetDelta = NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.STACK_SLOT_DELTA_ENCBASE, ref bitOffset);
                    normSpOffset += normSpOffsetDelta;
                    spOffset = gcInfoTypes.DenormalizeStackSlot(normSpOffset);
                }
                GcSlots.Add(new GcSlot(-1, new GcStackSlot(spOffset, spBase), flags, isUntracked));
            }
        }
    }
}
