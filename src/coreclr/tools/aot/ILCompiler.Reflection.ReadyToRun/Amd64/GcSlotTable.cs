// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun.Amd64
{
    public class GcSlotTable
    {
        public class GcSlot : BaseGcSlot
        {
            public int Index { get; set; }
            public int RegisterNumber { get; set; }
            public GcStackSlot StackSlot { get; set; }
            public GcSlotFlags Flags { get; set; }

            public GcSlot() { }

            public GcSlot(int index, int registerNumber, GcStackSlot stack, GcSlotFlags flags, bool isUntracked = false)
            {
                Index = index;
                RegisterNumber = registerNumber;
                StackSlot = stack;
                Flags = flags;
                if (isUntracked)
                {
                    Flags |= GcSlotFlags.GC_SLOT_UNTRACKED;
                }
            }

            public override GcSlotFlags WriteTo(StringBuilder sb, Machine machine, GcSlotFlags prevFlags)
            {
                if (prevFlags != Flags)
                {
                    sb.Append(Flags.ToString());
                    sb.Append(' ');
                }

                if (StackSlot != null)
                {
                    sb.Append(StackSlot.ToString());
                }
                else
                {
                    sb.Append(GetRegisterName(RegisterNumber, machine));
                }

                return Flags;
            }

            private static string GetRegisterName(int registerNumber, Machine machine)
            {
                switch (machine)
                {
                    case Machine.I386:
                        return ((x86.Registers)registerNumber).ToString();

                    case Machine.Amd64:
                        return ((Amd64.Registers)registerNumber).ToString();

                    case Machine.ArmThumb2:
                        return ((Arm.Registers)registerNumber).ToString();

                    case Machine.Arm64:
                        return ((Arm64.Registers)registerNumber).ToString();

                    default:
                        throw new NotImplementedException(machine.ToString());
                }
            }

        }

        public uint NumRegisters { get; set; }
        public uint NumStackSlots { get; set; }
        public uint NumUntracked { get; set; }
        public uint NumSlots { get; set; }

        private Machine _machine;

        public uint NumTracked
        {
            get
            {
                Debug.Assert(NumSlots == GcSlots.Count);
                return NumSlots - NumUntracked;
            }
        }

        public List<GcSlot> GcSlots { get; set; }

        public GcSlotTable() { }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/vm/gcinfodecoder.cpp">GcSlotDecoder::DecodeSlotTable</a>
        /// </summary>
        public GcSlotTable(byte[] image, Machine machine, GcInfoTypes gcInfoTypes, ref int bitOffset)
        {
            _machine = machine;

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
            if (NumStackSlots > 0)
            {
                DecodeStackSlots(image, machine, gcInfoTypes, NumStackSlots, false, ref bitOffset);
            }
            if (NumUntracked > 0)
            {
                DecodeStackSlots(image, machine, gcInfoTypes, NumUntracked, true, ref bitOffset);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"        NumSlots({NumSlots}) = NumRegisters({NumRegisters}) + NumStackSlots({NumStackSlots}) + NumUntracked({NumUntracked})");
            sb.AppendLine($"        GcSlots:");
            foreach (GcSlot slot in GcSlots)
            {
                sb.Append(' ', 12);
                slot.WriteTo(sb, _machine, GcSlotFlags.GC_SLOT_INVALID);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private void DecodeRegisters(byte[] image, GcInfoTypes gcInfoTypes, ref int bitOffset)
        {
            // We certainly predecode the first register
            uint regNum = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.REGISTER_ENCBASE, ref bitOffset);
            GcSlotFlags flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
            GcSlots.Add(new GcSlot(GcSlots.Count, (int)regNum, null, flags));

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
                GcSlots.Add(new GcSlot(GcSlots.Count, (int)regNum, null, flags));
            }
        }

        private void DecodeStackSlots(byte[] image, Machine machine, GcInfoTypes gcInfoTypes, uint nSlots, bool isUntracked, ref int bitOffset)
        {
            // We have stack slots left and more room to predecode
            GcStackSlotBase spBase = (GcStackSlotBase)NativeReader.ReadBits(image, 2, ref bitOffset);
            int normSpOffset = NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.STACK_SLOT_ENCBASE, ref bitOffset);
            int spOffset = gcInfoTypes.DenormalizeStackSlot(normSpOffset);
            GcSlotFlags flags = (GcSlotFlags)NativeReader.ReadBits(image, 2, ref bitOffset);
            GcSlots.Add(new GcSlot(GcSlots.Count, -1, new GcStackSlot(spOffset, spBase), flags, isUntracked));

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
                GcSlots.Add(new GcSlot(GcSlots.Count, -1, new GcStackSlot(spOffset, spBase), flags, isUntracked));
            }
        }
    }
}
