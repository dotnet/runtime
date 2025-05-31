// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

public class GcSlotTable
{
    [Flags]
    public enum GcSlotFlags
    {
        GC_SLOT_BASE = 0x0,
        GC_SLOT_INTERIOR = 0x1,
        GC_SLOT_PINNED = 0x2,
        GC_SLOT_UNTRACKED = 0x4,

        GC_SLOT_INVALID = -1
    };

    public class GcSlot
    {
        public int Index { get; set; }
        public string Register { get; set; }
        public int StackOffset { get; set; }
        public int LowBits { get; set; }
        public GcSlotFlags Flags { get; set; }

        public int BeginOffset { get; set; }
        public int EndOffset { get; set; }

        public GcSlot(int index, string reg, int stkOffs, int lowBits, GcSlotFlags flags)
        {
            Index = index;
            Register = reg;
            StackOffset = stkOffs;
            LowBits = lowBits;
            Flags = flags;

            BeginOffset = -1;
            EndOffset = -1;
        }

        public GcSlot(int index, string reg, int beginOffs, int endOffs, int varOffs, int lowBits, GcSlotFlags flags)
        {
            Index = index;
            Register = $"E{reg}P";
            StackOffset = varOffs;
            LowBits = lowBits;
            Flags = flags;

            BeginOffset = beginOffs;
            EndOffset = endOffs;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            if ((Flags & GcSlotFlags.GC_SLOT_UNTRACKED) != 0)
            {
                if (StackOffset < 0)
                {
                    sb.AppendLine($"            [{Register}-{-StackOffset}]");
                }
                else
                {
                    sb.AppendLine($"            [{Register}+{StackOffset}]");
                }
            }
            else
            {
                sb.AppendLine($"            BeginOffset: {BeginOffset}");
                sb.AppendLine($"            EndOffset: {EndOffset}");
                if (Register.Equals("BP"))
                {
                    sb.AppendLine($"            [{Register}-{-StackOffset}]");
                }
                else
                {
                    sb.AppendLine($"            [{Register}+{-StackOffset}]");
                }
            }

            sb.AppendLine($"            Flags: {Flags}");

            sb.Append($"            LowBits: ");
            if ((Flags & GcSlotFlags.GC_SLOT_UNTRACKED) != 0)
            {
                if ((LowBits & pinned_OFFSET_FLAG) != 0) sb.Append("pinned ");
                if ((LowBits & byref_OFFSET_FLAG) != 0) sb.Append("byref ");
            }
            sb.AppendLine();

            return sb.ToString();
        }
    }

    private const uint OFFSET_MASK = 0x3;
    private const uint byref_OFFSET_FLAG = 0x1;  // the offset is an interior ptr
    private const uint pinned_OFFSET_FLAG = 0x2;  // the offset is a pinned ptr

    public List<GcSlot> GcSlots { get; set; }


    public GcSlotTable(Target target, InfoHdr header, ref TargetPointer offset)
    {
        GcSlots = new List<GcSlot>();

        DecodeUntracked(target, header, ref offset);
        DecodeFrameVariableLifetimeTable(target, header, ref offset);
    }

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"        GcSlots:");
        sb.AppendLine($"            -------------------------");
        foreach (GcSlot slot in GcSlots)
        {
            sb.Append(slot.ToString());
            sb.AppendLine($"            -------------------------");
        }

        return sb.ToString();
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
    /// </summary>
    private void DecodeUntracked(Target target, InfoHdr header, ref TargetPointer offset)
    {
        uint calleeSavedRegs = 0;
        if (header.DoubleAlign)
        {
            calleeSavedRegs = 0;
            if (header.EdiSaved) calleeSavedRegs++;
            if (header.EsiSaved) calleeSavedRegs++;
            if (header.EbxSaved) calleeSavedRegs++;
        }

        uint count = header.UntrackedCount;
        int lastStkOffs = 0;
        while (count-- > 0)
        {
            int stkOffsDelta;
            int lowBits;

            char reg = header.EbpFrame ? 'B' : 'S';

            stkOffsDelta = target.GCDecodeSigned(ref offset);
            int stkOffs = lastStkOffs - stkOffsDelta;
            lastStkOffs = stkOffs;

            lowBits = (int)OFFSET_MASK & stkOffs;
            stkOffs = (int)((uint)stkOffs & ~OFFSET_MASK);

            if (header.DoubleAlign &&
                    (uint)stkOffs >= sizeof(int) * (header.FrameSize + calleeSavedRegs))
            {
                reg = 'B';
                stkOffs -= sizeof(int) * (int)(header.FrameSize + calleeSavedRegs);
            }

            GcSlots.Add(new GcSlot(GcSlots.Count, $"E{reg}P", stkOffs, lowBits, GcSlotFlags.GC_SLOT_UNTRACKED));
        }
    }

    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
    /// </summary>
    private void DecodeFrameVariableLifetimeTable(Target target, InfoHdr header, ref TargetPointer offset)
    {
        uint count = header.VarPtrTableSize;
        uint curOffs = 0;
        while (count-- > 0)
        {
            uint varOffs = target.GCDecodeUnsigned(ref offset);
            uint begOffs = target.GCDecodeUDelta(ref offset, curOffs);
            uint endOffs = target.GCDecodeUDelta(ref offset, begOffs);


            uint lowBits = varOffs & 0x3;
            varOffs &= ~OFFSET_MASK;

            curOffs = begOffs;

            string reg = header.EbpFrame ? "BP" : "SP";
            GcSlots.Add(new GcSlot(GcSlots.Count, reg, (int)begOffs, (int)endOffs, (int)varOffs, (int)lowBits, GcSlotFlags.GC_SLOT_BASE));
        }
    }
}
