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

            public GcSlot(int registerNumber, GcStackSlot stack, GcSlotFlags flags)
            {
                RegisterNumber = registerNumber;
                StackSlot = stack;
                Flags = flags;
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

        public enum GcSlotFlags
        {
            GC_SLOT_BASE = 0x0,
            GC_SLOT_INTERIOR = 0x1,
            GC_SLOT_PINNED = 0x2,
            GC_SLOT_UNTRACKED = 0x4,

            // For internal use by the encoder/decoder
            GC_SLOT_IS_REGISTER = 0x8,
            GC_SLOT_IS_DELETED = 0x10,
        };

        public enum GcStackSlotBase
        {
            GC_CALLER_SP_REL = 0x0,
            GC_SP_REL = 0x1,
            GC_FRAMEREG_REL = 0x2,

            GC_SPBASE_FIRST = GC_CALLER_SP_REL,
            GC_SPBASE_LAST = GC_FRAMEREG_REL,
        };

        public class GcStackSlot
        {
            public int SpOffset { get; }
            public GcStackSlotBase Base { get; }
            public GcStackSlot(int spOffset, GcStackSlotBase stackSlotBase)
            {
                SpOffset = spOffset;
                Base = stackSlotBase;
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                string tab4 = new string(' ', 16);

                sb.AppendLine($"{tab4}SpOffset: {SpOffset}");
                sb.Append($"{tab4}Base: {Enum.GetName(typeof(GcStackSlotBase), Base)}");

                return sb.ToString();
            }
        };

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
                DecodeStackSlots(image, machine, gcInfoTypes, NumStackSlots, ref bitOffset);
            }
            if ((NumUntracked > 0) && (GcSlots.Count < gcInfoTypes.MAX_PREDECODED_SLOTS))
            {
                DecodeStackSlots(image, machine, gcInfoTypes, NumUntracked, ref bitOffset);
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

        private int DenormalizeStackSlot(Machine target, int x)
        {
            switch (target)
            {
                case Machine.Amd64:
                    return (x << 3);
                case Machine.Arm:
                    return (x << 2);
                case Machine.Arm64:
                    return (x << 3);
            }
            return x;
        }

        private void DecodeRegisters(byte[] image, GcInfoTypes gcInfoTypes, ref int bitOffset)
        {
            // We certainly predecode the first register
            uint regNum = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.REGISTER_ENCBASE, ref bitOffset);
            GcSlotFlags flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
            GcSlots.Add(new GcSlot((int)regNum, null, flags));

            for (int i = 1; i < NumRegisters && i < gcInfoTypes.MAX_PREDECODED_SLOTS; i++)
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

        private void DecodeStackSlots(byte[] image, Machine machine, GcInfoTypes gcInfoTypes, uint nSlots, ref int bitOffset)
        {
            // We have stack slots left and more room to predecode
            GcStackSlotBase spBase = (GcStackSlotBase)NativeReader.ReadBits(image, 2, ref bitOffset);
            int normSpOffset = NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.STACK_SLOT_ENCBASE, ref bitOffset);
            int spOffset = DenormalizeStackSlot(machine, normSpOffset);
            GcSlotFlags flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
            GcSlots.Add(new GcSlot(-1, new GcStackSlot(spOffset, spBase), flags));

            for (int i = 1; i < nSlots && GcSlots.Count < gcInfoTypes.MAX_PREDECODED_SLOTS; i++)
            {
                spBase = (GcStackSlotBase)NativeReader.ReadBits(image, 2, ref bitOffset);
                if ((uint)flags != 0)
                {
                    normSpOffset = NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.STACK_SLOT_ENCBASE, ref bitOffset);
                    spOffset = DenormalizeStackSlot(machine, normSpOffset);
                    flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
                }
                else
                {
                    int normSpOffsetDelta = NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.STACK_SLOT_DELTA_ENCBASE, ref bitOffset);
                    normSpOffset += normSpOffsetDelta;
                    spOffset = DenormalizeStackSlot(machine, normSpOffset);
                }
                GcSlots.Add(new GcSlot(-1, new GcStackSlot(spOffset, spBase), flags));
            }
        }
    }
}
