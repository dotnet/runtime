// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Serialization;

namespace R2RDump.x86
{
    public class GcSlotTable
    {
        public class GcSlot
        {
            [XmlAttribute("Index")]
            public int Index { get; set; }
            public string Register { get; set; }
            public int StackOffset { get; set; }
            public int LowBits { get; set; }
            public GcSlotFlags Flags { get; set; }

            public int BeginOffset { get; set; }
            public int EndOffset { get; set; }

            public GcSlot() { }

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

                if (Flags == GcSlotFlags.GC_SLOT_UNTRACKED)
                {
                    if (StackOffset < 0)
                    {
                        sb.AppendLine($"\t\t\t[{Register}-{-StackOffset}]");
                    }
                    else
                    {
                        sb.AppendLine($"\t\t\t[{Register}+{StackOffset}]");
                    }
                }
                else
                {
                    sb.AppendLine($"\t\t\tBeginOffset: {BeginOffset}");
                    sb.AppendLine($"\t\t\tEndOffset: {EndOffset}");
                    if (Register.Equals("BP"))
                    {
                        sb.AppendLine($"\t\t\t[{Register}-{-StackOffset}]");
                    }
                    else
                    {
                        sb.AppendLine($"\t\t\t[{Register}+{-StackOffset}]");
                    }
                }

                sb.AppendLine($"\t\t\tFlags: {Flags}");

                sb.Append($"\t\t\tLowBits: ");
                if (Flags == GcSlotFlags.GC_SLOT_UNTRACKED)
                {
                    if((LowBits & pinned_OFFSET_FLAG) != 0) sb.Append("pinned ");
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

        public GcSlotTable() { }

        public GcSlotTable(byte[] image, InfoHdrSmall header, ref int offset)
        {
            GcSlots = new List<GcSlot>();

            DecodeUntracked(image, header, ref offset);
            DecodeFrameVariableLifetimeTable(image, header, ref offset);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"\t\tGcSlots:");
            sb.AppendLine($"\t\t\t-------------------------");
            foreach (GcSlot slot in GcSlots)
            {
                sb.Append(slot.ToString());
                sb.AppendLine($"\t\t\t-------------------------");
            }

            return sb.ToString();
        }

        /// <summary>
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
        /// </summary>
        private void DecodeUntracked(byte[] image, InfoHdrSmall header, ref int offset)
        {
            uint calleeSavedRegs = 0;
            if (header.DoubleAlign)
            {
                calleeSavedRegs = 0;
                if (header.EdiSaved) calleeSavedRegs++;
                if (header.EsiSaved) calleeSavedRegs++;
                if (header.EbxSaved) calleeSavedRegs++;
            }

            uint count = header.UntrackedCnt;
            int lastStkOffs = 0;
            while (count-- > 0)
            {
                int stkOffsDelta;
                int lowBits;

                char reg = header.EbpFrame ? 'B' : 'S';

                stkOffsDelta = NativeReader.DecodeSignedGc(image, ref offset);
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
        /// based on <a href="https://github.com/dotnet/coreclr/blob/master/src/gcdump/i386/gcdumpx86.cpp">GCDump::DumpGCTable</a>
        /// </summary>
        private void DecodeFrameVariableLifetimeTable(byte[] image, InfoHdrSmall header, ref int offset)
        {
            uint count = header.VarPtrTableSize;
            uint curOffs = 0;
            while (count-- > 0)
            {
                uint varOffs = NativeReader.DecodeUnsignedGc(image, ref offset);
                uint begOffs = NativeReader.DecodeUDelta(image, ref offset, curOffs);
                uint endOffs = NativeReader.DecodeUDelta(image, ref offset, begOffs);


                uint lowBits = varOffs & 0x3;
                varOffs &= ~OFFSET_MASK;

                curOffs = begOffs;

                string reg = header.EbpFrame ? "BP" : "SP";
                GcSlots.Add(new GcSlot(GcSlots.Count, reg, (int)begOffs, (int)endOffs, (int)varOffs, (int)lowBits, GcSlotFlags.GC_SLOT_BASE));
            }
        }
    }
}
