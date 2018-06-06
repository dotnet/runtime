// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;

namespace R2RDump
{
    class GcInfo
    {
        public enum GcInfoHeaderFlags
        {
            GC_INFO_IS_VARARG = 0x1,
            GC_INFO_HAS_SECURITY_OBJECT = 0x2,
            GC_INFO_HAS_GS_COOKIE = 0x4,
            GC_INFO_HAS_PSP_SYM = 0x8,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK = 0x30,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE = 0x00,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_MT = 0x10,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_MD = 0x20,
            GC_INFO_HAS_GENERICS_INST_CONTEXT_THIS = 0x30,
            GC_INFO_HAS_STACK_BASE_REGISTER = 0x40,
            GC_INFO_WANTS_REPORT_ONLY_LEAF = 0x80, // GC_INFO_HAS_TAILCALLS = 0x80, for ARM and ARM64
            GC_INFO_HAS_EDIT_AND_CONTINUE_PRESERVED_SLOTS = 0x100,
            GC_INFO_REVERSE_PINVOKE_FRAME = 0x200,

            GC_INFO_FLAGS_BIT_SIZE_VERSION_1 = 9,
            GC_INFO_FLAGS_BIT_SIZE = 10,
        };

        public enum ReturnKinds
        {
            RT_Scalar = 0,
            RT_Object = 1,
            RT_ByRef = 2,
            RT_Unset = 3,       // Encoding 3 means RT_Float on X86
            RT_Scalar_Obj = RT_Object << 2 | RT_Scalar,
            RT_Scalar_ByRef = RT_ByRef << 2 | RT_Scalar,

            RT_Obj_Obj = RT_Object << 2 | RT_Object,
            RT_Obj_ByRef = RT_ByRef << 2 | RT_Object,

            RT_ByRef_Obj = RT_Object << 2 | RT_ByRef,
            RT_ByRef_ByRef = RT_ByRef << 2 | RT_ByRef,

            RT_Illegal = 0xFF
        };

        public struct InterruptibleRange
        {
            public uint StartOffset { get; }
            public uint StopOffset { get; }
            public InterruptibleRange(uint start, uint stop)
            {
                StartOffset = start;
                StopOffset = stop;
            }
        }

        private const int GCINFO_VERSION = 2;
        private const int MIN_GCINFO_VERSION_WITH_RETURN_KIND = 2;
        private const int MIN_GCINFO_VERSION_WITH_REV_PINVOKE_FRAME = 2;

        public int Version { get; }
        public int CodeLength { get; }
        public ReturnKinds ReturnKind { get; }
        public uint ValidRangeStart { get; }
        public uint ValidRangeEnd { get; }
        public int SecurityObjectStackSlot { get; }
        public int GSCookieStackSlot { get; }
        public int PSPSymStackSlot { get; }
        public int GenericsInstContextStackSlot { get; }
        public uint StackBaseRegister { get; }
        public uint SizeOfEditAndContinuePreservedArea { get; }
        public int ReversePInvokeFrameStackSlot { get; }
        public uint SizeOfStackOutgoingAndScratchArea { get; }
        public uint NumSafePoints { get; }
        public uint NumInterruptibleRanges { get; }
        public IEnumerable<uint> SafePointOffsets { get; }
        public IEnumerable<InterruptibleRange> InterruptibleRanges { get; }
        public GcSlotTable SlotTable { get; }
        public int Size { get; }

        public GcInfo(byte[] image, int offset, Machine machine, ushort majorVersion)
        {
            GcInfoTypes gcInfoTypes = new GcInfoTypes(machine);

            SecurityObjectStackSlot = -1;
            GSCookieStackSlot = -1;
            PSPSymStackSlot = -1;
            SecurityObjectStackSlot = -1;
            GenericsInstContextStackSlot = -1;
            StackBaseRegister = 0xffffffff;
            SizeOfEditAndContinuePreservedArea = 0xffffffff;
            ReversePInvokeFrameStackSlot = -1;

            GcInfoHeaderFlags headerFlags;
            Version = ReadyToRunVersionToGcInfoVersion(majorVersion);
            int bitOffset = offset * 8;
            int startBitOffset = bitOffset;
            bool slimHeader = (NativeReader.ReadBits(image, 1, ref bitOffset) == 0);

            if (slimHeader)
            {
                headerFlags = NativeReader.ReadBits(image, 1,ref bitOffset) == 1 ? GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER : 0;
            }
            else
            {
                int numFlagBits = (int)((Version == 1) ? GcInfoHeaderFlags.GC_INFO_FLAGS_BIT_SIZE_VERSION_1 : GcInfoHeaderFlags.GC_INFO_FLAGS_BIT_SIZE);
                headerFlags = (GcInfoHeaderFlags)NativeReader.ReadBits(image, numFlagBits, ref bitOffset);
            }

            bool hasReversePInvokeFrame = false;
            if (Version >= MIN_GCINFO_VERSION_WITH_REV_PINVOKE_FRAME) // IsReversePInvokeFrameAvailable
            {
                hasReversePInvokeFrame = (headerFlags & GcInfoHeaderFlags.GC_INFO_REVERSE_PINVOKE_FRAME) != 0;
            }

            if (Version >= MIN_GCINFO_VERSION_WITH_RETURN_KIND) // IsReturnKindAvailable
            {
                int returnKindBits = (slimHeader) ? gcInfoTypes.SIZE_OF_RETURN_KIND_SLIM : gcInfoTypes.SIZE_OF_RETURN_KIND_FAT;
                ReturnKind = (ReturnKinds)NativeReader.ReadBits(image, returnKindBits, ref bitOffset);
            }

            CodeLength = DenormalizeCodeLength(machine, (int)NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.CODE_LENGTH_ENCBASE, ref bitOffset));

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE) != 0)
            {
                // Decode prolog/epilog information
                uint normPrologSize = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.NORM_PROLOG_SIZE_ENCBASE, ref bitOffset) + 1;
                uint normEpilogSize = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.NORM_PROLOG_SIZE_ENCBASE, ref bitOffset);

                ValidRangeStart = normPrologSize;
                ValidRangeEnd = (uint)CodeLength - normEpilogSize;
            }
            else if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_SECURITY_OBJECT) != 0 || (headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE)
            {
                // Decode prolog information
                ValidRangeStart = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.NORM_PROLOG_SIZE_ENCBASE, ref bitOffset) + 1;
                // satisfy asserts that assume m_GSCookieValidRangeStart != 0 ==> m_GSCookieValidRangeStart < m_GSCookieValidRangeEnd
                ValidRangeEnd = ValidRangeStart + 1;
            }

            // Decode the offset to the security object.
            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_SECURITY_OBJECT) != 0)
            {
                SecurityObjectStackSlot = DenormalizeStackSlot(machine, NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.SECURITY_OBJECT_STACK_SLOT_ENCBASE, ref bitOffset));
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE) != 0)
            {
                GSCookieStackSlot = DenormalizeStackSlot(machine, NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.GS_COOKIE_STACK_SLOT_ENCBASE, ref bitOffset));
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_PSP_SYM) != 0)
            {
                PSPSymStackSlot = DenormalizeStackSlot(machine, NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.PSP_SYM_STACK_SLOT_ENCBASE, ref bitOffset));
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE)
            {
                GenericsInstContextStackSlot = DenormalizeStackSlot(machine, NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE, ref bitOffset));
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER) != 0)
            {
                if (slimHeader)
                {
                    StackBaseRegister = DenormalizeStackBaseRegister(machine, 0);
                }
                else
                {
                    StackBaseRegister = DenormalizeStackBaseRegister(machine, NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.STACK_BASE_REGISTER_ENCBASE, ref bitOffset));
                }
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_EDIT_AND_CONTINUE_PRESERVED_SLOTS) != 0)
            {
                SizeOfEditAndContinuePreservedArea = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE, ref bitOffset);
            }

            if (hasReversePInvokeFrame)
            {
                ReversePInvokeFrameStackSlot = NativeReader.DecodeVarLengthSigned(image, gcInfoTypes.REVERSE_PINVOKE_FRAME_ENCBASE, ref bitOffset);
            }

            // FIXED_STACK_PARAMETER_SCRATCH_AREA
            if (slimHeader)
            {
                SizeOfStackOutgoingAndScratchArea = 0;
            }
            else
            {
                SizeOfStackOutgoingAndScratchArea = DenormalizeSizeOfStackArea(machine, NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.SIZE_OF_STACK_AREA_ENCBASE, ref bitOffset));
            }

            // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
            NumSafePoints = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.NUM_SAFE_POINTS_ENCBASE, ref bitOffset);

            if (slimHeader)
            {
                NumInterruptibleRanges = 0;
            }
            else
            {
                NumInterruptibleRanges = NativeReader.DecodeVarLengthUnsigned(image, gcInfoTypes.NUM_INTERRUPTIBLE_RANGES_ENCBASE, ref bitOffset);
            }

            // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
            SafePointOffsets = EnumerateSafePoints(image, ref bitOffset);

            uint numBitsPerOffset = CeilOfLog2(CodeLength);
            bitOffset += (int)(NumSafePoints * numBitsPerOffset);

            InterruptibleRanges = EnumerateInterruptibleRanges(image, gcInfoTypes.INTERRUPTIBLE_RANGE_DELTA1_ENCBASE, gcInfoTypes.INTERRUPTIBLE_RANGE_DELTA2_ENCBASE, ref bitOffset);

            SlotTable = new GcSlotTable(image, machine, gcInfoTypes, ref bitOffset);

            Size = (int)Math.Ceiling((bitOffset - startBitOffset) / 8.0);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            string tab = "    ";

            sb.AppendLine($"{tab}Version: {Version}");
            sb.AppendLine($"{tab}CodeLength: {CodeLength}");
            sb.AppendLine($"{tab}ReturnKind: {Enum.GetName(typeof(ReturnKinds), ReturnKind)}");
            sb.AppendLine($"{tab}ValidRangeStart: {ValidRangeStart}");
            sb.AppendLine($"{tab}ValidRangeEnd: {ValidRangeEnd}");
            if (SecurityObjectStackSlot != -1)
                sb.AppendLine($"{tab}SecurityObjectStackSlot: {SecurityObjectStackSlot}");
            if (GSCookieStackSlot != -1)
                sb.AppendLine($"{tab}GSCookieStackSlot: {GSCookieStackSlot}");
            if (PSPSymStackSlot != -1)
                sb.AppendLine($"{tab}PSPSymStackSlot: {PSPSymStackSlot}");
            if (GenericsInstContextStackSlot != -1)
                sb.AppendLine($"{tab}GenericsInstContextStackSlot: {GenericsInstContextStackSlot}");
            if (StackBaseRegister != 0xffffffff)
                sb.AppendLine($"{tab}StackBaseRegister: {StackBaseRegister}");
            if (SizeOfEditAndContinuePreservedArea != 0xffffffff)
                sb.AppendLine($"{tab}SizeOfEditAndContinuePreservedArea: {SizeOfEditAndContinuePreservedArea}");
            if (ReversePInvokeFrameStackSlot != -1)
                sb.AppendLine($"{tab}ReversePInvokeFrameStackSlot: {ReversePInvokeFrameStackSlot}");
            sb.AppendLine($"{tab}SizeOfStackOutgoingAndScratchArea: {SizeOfStackOutgoingAndScratchArea}");
            sb.AppendLine($"{tab}NumSafePoints: {NumSafePoints}");
            sb.AppendLine($"{tab}NumInterruptibleRanges: {NumInterruptibleRanges}");
            sb.AppendLine($"{tab}SafePointOffsets:");
            foreach (uint offset in SafePointOffsets)
            {
                sb.AppendLine($"{tab}{tab}{offset}");
            }
            sb.AppendLine($"{tab}InterruptibleRanges:");
            foreach (InterruptibleRange range in InterruptibleRanges)
            {
                sb.AppendLine($"{tab}{tab}start:{range.StartOffset}, end:{range.StopOffset}");
            }
            sb.AppendLine($"{tab}SlotTable:");
            sb.Append(SlotTable.ToString());

            return sb.ToString();
        }

        private int DenormalizeCodeLength(Machine target, int x)
        {
            switch (target)
            {
                case Machine.Arm:
                    return (x << 1);
                case Machine.Arm64:
                    return (x << 2);
            }
            return x;
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

        private uint DenormalizeStackBaseRegister(Machine target, uint x)
        {
            switch (target)
            {
                case Machine.Amd64:
                    return (x ^ 5);
                case Machine.Arm:
                    return ((x ^ 7) + 4);
                case Machine.Arm64:
                    return (x ^ 29);
            }
            return x;
        }

        private uint DenormalizeSizeOfStackArea(Machine target, uint x)
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

        private uint CeilOfLog2(int x)
        {
            if (x == 0)
                return 0;
            uint result = (uint)((x & (x - 1)) != 0 ? 1 : 0);
            while (x != 1)
            {
                result++;
                x >>= 1;
            }
            return result;
        }

        private IEnumerable<uint> EnumerateSafePoints(byte[] image, ref int bitOffset)
        {
            List<uint> safePoints = new List<uint>();
            uint numBitsPerOffset = CeilOfLog2(CodeLength);
            for (int i = 0; i < NumSafePoints; i++)
            {
                uint normOffset = (uint)NativeReader.ReadBits(image, (int)numBitsPerOffset, ref bitOffset);
                safePoints.Add(normOffset);
            }
            return safePoints;
        }

        private IEnumerable<InterruptibleRange> EnumerateInterruptibleRanges(byte[] image, int interruptibleRangeDelta1EncBase, int interruptibleRangeDelta2EncBase, ref int bitOffset)
        {
            List<InterruptibleRange> ranges = new List<InterruptibleRange>();
            uint lastinterruptibleRangeStopOffset = 0;

            for (uint i = 0; i < NumInterruptibleRanges; i++)
            {
                uint normStartDelta = NativeReader.DecodeVarLengthUnsigned(image, interruptibleRangeDelta1EncBase, ref bitOffset);
                uint normStopDelta = NativeReader.DecodeVarLengthUnsigned(image, interruptibleRangeDelta2EncBase, ref bitOffset) + 1;

                uint rangeStartOffset = lastinterruptibleRangeStopOffset + normStartDelta;
                uint rangeStopOffset = rangeStartOffset + normStopDelta;
                ranges.Add(new InterruptibleRange(rangeStartOffset, rangeStopOffset));

                lastinterruptibleRangeStopOffset = rangeStopOffset;
            }
            return ranges;
        }

        /// <summary>
        /// GcInfo version is 1 up to ReadyTorun version 1.x. 
        /// GcInfo version is current from  ReadyToRun version 2.0
        /// </summary>
        static int ReadyToRunVersionToGcInfoVersion(int readyToRunMajorVersion)
        {
            return (readyToRunMajorVersion == 1) ? 1 : GCINFO_VERSION;
        }
    }
}
