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
                        if (registerNumber is >= 16 and <= 31)
                            return $"R{registerNumber}";
                        return ((Amd64.Registers)registerNumber).ToString();

                    case Machine.ArmThumb2:
                        return ((Arm.Registers)registerNumber).ToString();

                    case Machine.Arm64:
                        return ((Arm64.Registers)registerNumber).ToString();

                    case Machine.LoongArch64:
                        return ((LoongArch64.Registers)registerNumber).ToString();

                    case Machine.RiscV64:
                        return ((RiscV64.Registers)registerNumber).ToString();

                    case WasmMachine.Wasm32:
                        throw new NotImplementedException("No implementation for machine type Wasm32.");
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
        public GcSlotTable(NativeReader imageReader, Machine machine, GcInfoTypes gcInfoTypes, ref int bitOffset, int version)
        {
            _machine = machine;

            if (machine != WasmMachine.Wasm32 && imageReader.ReadBits(1, ref bitOffset) != 0)
            {
                NumRegisters = imageReader.DecodeVarLengthUnsigned(gcInfoTypes.NUM_REGISTERS_ENCBASE, ref bitOffset);
            }
            if (imageReader.ReadBits(1, ref bitOffset) != 0)
            {
                NumStackSlots = imageReader.DecodeVarLengthUnsigned(gcInfoTypes.NUM_STACK_SLOTS_ENCBASE, ref bitOffset);
                NumUntracked = imageReader.DecodeVarLengthUnsigned(gcInfoTypes.NUM_UNTRACKED_SLOTS_ENCBASE, ref bitOffset);
            }
            NumSlots = NumRegisters + NumStackSlots + NumUntracked;

            GcSlots = new List<GcSlot>();
            if (NumRegisters > 0)
            {
                DecodeRegisters(imageReader, gcInfoTypes, ref bitOffset, version);
            }
            if (NumStackSlots > 0)
            {
                DecodeStackSlots(imageReader, machine, gcInfoTypes, NumStackSlots, false, ref bitOffset, version);
            }
            if (NumUntracked > 0)
            {
                DecodeStackSlots(imageReader, machine, gcInfoTypes, NumUntracked, true, ref bitOffset, version);
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

        private static uint ReadRegisterValue(NativeReader imageReader, int encodingBase, int registerBits, ref int bitOffset)
        {
            uint chunk = (uint)imageReader.ReadBits(encodingBase + 1, ref bitOffset);
            uint value = chunk & ((1u << encodingBase) - 1);
            if ((chunk & (1u << encodingBase)) != 0)
            {
                if (registerBits <= encodingBase)
                    throw new BadImageFormatException("GC register encoding exceeds the target register width.");
                value |= (uint)imageReader.ReadBits(registerBits - encodingBase, ref bitOffset) << encodingBase;
            }

            return value;
        }

        private GcSlotFlags ReadSlotFlags(NativeReader imageReader, GcSlotFlags previousFlags, bool first, ref int bitOffset)
        {
            GcSlotFlags flagsXor = _machine == WasmMachine.Wasm32 ? GcSlotFlags.GC_SLOT_PINNED : 0;
            previousFlags ^= flagsXor;
            GcSlotFlags flags = first || previousFlags != 0
                ? (GcSlotFlags)imageReader.ReadBits(first || (previousFlags & GcSlotFlags.GC_SLOT_PINNED) != 0 ? 2 : 1, ref bitOffset)
                : 0;

            return flags ^ flagsXor;
        }

        private void DecodeRegisters(NativeReader imageReader, GcInfoTypes gcInfoTypes, ref int bitOffset, int version)
        {
            uint regNum = 0;
            GcSlotFlags flags = 0;
            for (int i = 0; i < NumRegisters; i++)
            {
                bool absolute = i == 0 || flags != 0;
                if (version >= 6)
                {
                    GcSlotFlags previousFlags = flags;
                    flags = ReadSlotFlags(imageReader, previousFlags, i == 0, ref bitOffset);
                    absolute = i == 0 || flags != previousFlags;
                }
                int encodingBase = absolute ? gcInfoTypes.REGISTER_ENCBASE : gcInfoTypes.REGISTER_DELTA_ENCBASE;
                uint value = version >= 6
                    ? ReadRegisterValue(imageReader, encodingBase, gcInfoTypes.REGISTER_BITS, ref bitOffset)
                    : imageReader.DecodeVarLengthUnsigned(encodingBase, ref bitOffset);
                regNum = absolute ? value : regNum + value + 1;
                if (version < 6 && absolute)
                    flags = (GcSlotFlags)imageReader.ReadBits(2, ref bitOffset);
                GcSlots.Add(new GcSlot(GcSlots.Count, (int)regNum, null, flags));
            }
        }

        private void DecodeStackSlots(NativeReader imageReader, Machine machine, GcInfoTypes gcInfoTypes, uint nSlots, bool isUntracked, ref int bitOffset, int version)
        {
            if (machine == WasmMachine.Wasm32 || version >= 6)
            {
                GcSlotFlags slotFlags = 0;
                GcStackSlotBase previousBase = GcStackSlotBase.GC_SP_REL;
                int offset = 0;
                for (uint i = 0; i < nSlots; i++)
                {
                    GcSlotFlags previousFlags = slotFlags;
                    slotFlags = ReadSlotFlags(imageReader, previousFlags, i == 0, ref bitOffset);
                    GcStackSlotBase slotBase = machine != WasmMachine.Wasm32 && imageReader.ReadBits(1, ref bitOffset) == 0
                        ? GcStackSlotBase.GC_SP_REL : GcStackSlotBase.GC_FRAMEREG_REL;
                    offset = i == 0 || slotFlags != previousFlags || slotBase != previousBase
                        ? imageReader.DecodeVarLengthSigned(gcInfoTypes.STACK_SLOT_ENCBASE, ref bitOffset)
                        : offset + (int)imageReader.DecodeVarLengthUnsigned(gcInfoTypes.STACK_SLOT_DELTA_ENCBASE, ref bitOffset);
                    int byteOffset = gcInfoTypes.DenormalizeStackSlot(offset);
                    GcSlots.Add(new GcSlot(GcSlots.Count, -1, new GcStackSlot(byteOffset, slotBase), slotFlags, isUntracked));
                    previousBase = slotBase;
                }
                return;
            }

            // We have stack slots left and more room to predecode
            GcStackSlotBase spBase = (GcStackSlotBase)imageReader.ReadBits(2, ref bitOffset);
            int normSpOffset = imageReader.DecodeVarLengthSigned(gcInfoTypes.STACK_SLOT_ENCBASE, ref bitOffset);
            int spOffset = gcInfoTypes.DenormalizeStackSlot(normSpOffset);
            GcSlotFlags flags = (GcSlotFlags)imageReader.ReadBits(2, ref bitOffset);
            GcSlots.Add(new GcSlot(GcSlots.Count, -1, new GcStackSlot(spOffset, spBase), flags, isUntracked));

            for (int i = 1; i < nSlots; i++)
            {
                spBase = (GcStackSlotBase)imageReader.ReadBits(2, ref bitOffset);
                if ((uint)flags != 0)
                {
                    normSpOffset = imageReader.DecodeVarLengthSigned(gcInfoTypes.STACK_SLOT_ENCBASE, ref bitOffset);
                    spOffset = gcInfoTypes.DenormalizeStackSlot(normSpOffset);
                    flags = (GcSlotFlags)imageReader.ReadBits(2, ref bitOffset);
                }
                else
                {
                    int normSpOffsetDelta = (int)imageReader.DecodeVarLengthUnsigned(gcInfoTypes.STACK_SLOT_DELTA_ENCBASE, ref bitOffset);
                    normSpOffset += normSpOffsetDelta;
                    spOffset = gcInfoTypes.DenormalizeStackSlot(normSpOffset);
                }
                GcSlots.Add(new GcSlot(GcSlots.Count, -1, new GcStackSlot(spOffset, spBase), flags, isUntracked));
            }
        }
    }
}
