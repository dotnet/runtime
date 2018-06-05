// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.PortableExecutable;

namespace R2RDump
{
    class GCInfo
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

        public GCInfo(byte[] image, int offset, Machine machine, ushort majorVersion)
        {
            int sizeOfReturnKindSlim = 2;
            int sizeOfReturnKindFat = 2;
            int codeLengthEncBase = 8;
            int normPrologSizeEncBase = 5;
            int securityObjectStackSlotEncBase = 6;
            int gsCookieStackSlotEncBase = 6;
            int pspSymStackSlotEncBase = 6;
            int genericsInstContextStackSlotEncBase = 6;
            int stackBaseRegisterEncBase = 3;
            int sizeOfEditAndContinuePreservedAreaEncBase = 4;
            int reversePinvokeFrameEncBase = 6;
            int sizeOfStackAreaEncBase = 3;
            int numSafePointsEncBase = 3;
            int numInterruptibleRangesEncBase = 1;
            switch (machine)
            {
                case Machine.Amd64:
                    sizeOfReturnKindFat = 4;
                    numSafePointsEncBase = 2;
                    break;
                case Machine.Arm:
                    codeLengthEncBase = 7;
                    securityObjectStackSlotEncBase = 5;
                    gsCookieStackSlotEncBase = 5;
                    pspSymStackSlotEncBase = 5;
                    genericsInstContextStackSlotEncBase = 5;
                    stackBaseRegisterEncBase = 1;
                    sizeOfEditAndContinuePreservedAreaEncBase = 3;
                    reversePinvokeFrameEncBase = 5;
                    numInterruptibleRangesEncBase = 2;
                    break;
                case Machine.Arm64:
                    sizeOfReturnKindFat = 4;
                    stackBaseRegisterEncBase = 2;
                    break;
                case Machine.I386:
                    codeLengthEncBase = 6;
                    normPrologSizeEncBase = 4;
                    sizeOfEditAndContinuePreservedAreaEncBase = 3;
                    sizeOfStackAreaEncBase = 6;
                    numSafePointsEncBase = 4;
                    break;
            }

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
                int returnKindBits = (slimHeader) ? sizeOfReturnKindSlim : sizeOfReturnKindFat;
                ReturnKind = (ReturnKinds)NativeReader.ReadBits(image, returnKindBits, ref bitOffset);
            }

            CodeLength = DenormalizeCodeLength(machine, (int)NativeReader.DecodeVarLengthUnsigned(image, codeLengthEncBase, ref bitOffset));

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE) != 0)
            {
                // Decode prolog/epilog information
                uint normPrologSize = NativeReader.DecodeVarLengthUnsigned(image, normPrologSizeEncBase, ref bitOffset) + 1;
                uint normEpilogSize = NativeReader.DecodeVarLengthUnsigned(image, normPrologSizeEncBase, ref bitOffset);

                ValidRangeStart = normPrologSize;
                ValidRangeEnd = (uint)CodeLength - normEpilogSize;
            }
            else if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_SECURITY_OBJECT) != 0 || (headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE)
            {
                // Decode prolog information
                ValidRangeStart = NativeReader.DecodeVarLengthUnsigned(image, normPrologSizeEncBase, ref bitOffset) + 1;
                // satisfy asserts that assume m_GSCookieValidRangeStart != 0 ==> m_GSCookieValidRangeStart < m_GSCookieValidRangeEnd
                ValidRangeEnd = ValidRangeStart + 1;
            }

            // Decode the offset to the security object.
            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_SECURITY_OBJECT) != 0)
            {
                SecurityObjectStackSlot = DenormalizeStackSlot(machine, NativeReader.DecodeVarLengthSigned(image, securityObjectStackSlotEncBase, ref bitOffset));
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GS_COOKIE) != 0)
            {
                GSCookieStackSlot = DenormalizeStackSlot(machine, NativeReader.DecodeVarLengthSigned(image, gsCookieStackSlotEncBase, ref bitOffset));
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_PSP_SYM) != 0)
            {
                PSPSymStackSlot = DenormalizeStackSlot(machine, NativeReader.DecodeVarLengthSigned(image, pspSymStackSlotEncBase, ref bitOffset));
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_MASK) != GcInfoHeaderFlags.GC_INFO_HAS_GENERICS_INST_CONTEXT_NONE)
            {
                GenericsInstContextStackSlot = DenormalizeStackSlot(machine, NativeReader.DecodeVarLengthSigned(image, genericsInstContextStackSlotEncBase, ref bitOffset));
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER) != 0)
            {
                if (slimHeader)
                {
                    StackBaseRegister = DenormalizeStackBaseRegister(machine, 0);
                }
                else
                {
                    StackBaseRegister = DenormalizeStackBaseRegister(machine, NativeReader.DecodeVarLengthUnsigned(image, stackBaseRegisterEncBase, ref bitOffset));
                }
            }

            if ((headerFlags & GcInfoHeaderFlags.GC_INFO_HAS_EDIT_AND_CONTINUE_PRESERVED_SLOTS) != 0)
            {
                SizeOfEditAndContinuePreservedArea = NativeReader.DecodeVarLengthUnsigned(image, sizeOfEditAndContinuePreservedAreaEncBase, ref bitOffset);
            }

            if (hasReversePInvokeFrame)
            {
                ReversePInvokeFrameStackSlot = NativeReader.DecodeVarLengthSigned(image, reversePinvokeFrameEncBase, ref bitOffset);
            }

            // FIXED_STACK_PARAMETER_SCRATCH_AREA
            if (slimHeader)
            {
                SizeOfStackOutgoingAndScratchArea = 0;
            }
            else
            {
                SizeOfStackOutgoingAndScratchArea = DenormalizeSizeOfStackArea(machine, NativeReader.DecodeVarLengthUnsigned(image, sizeOfStackAreaEncBase, ref bitOffset));
            }

            // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
            NumSafePoints = NativeReader.DecodeVarLengthUnsigned(image, numSafePointsEncBase, ref bitOffset);

            if (slimHeader)
            {
                NumInterruptibleRanges = 0;
            }
            else
            {
                NumInterruptibleRanges = NativeReader.DecodeVarLengthUnsigned(image, numInterruptibleRangesEncBase, ref bitOffset);
            }

            // PARTIALLY_INTERRUPTIBLE_GC_SUPPORTED
            /*if (NumSafePoints != 0)
            {
                SafePointIndex = FindSafePoint(image, machine, InstructionOffset, ref bitOffset);
            }
            else
            {
                SafePointIndex = 0;
            }

            uint numBitsPerOffset = CeilOfLog2(CodeLength);
            bitOffset += (int)(NumSafePoints* numBitsPerOffset);
            


            EnumerateInterruptibleRanges(&SetIsInterruptibleCB, this);*/

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
            uint result = (uint)((x & (x - 1)) != 0 ? 1 : 0);
            while (x != 1)
            {
                result++;
                x >>= 1;
            }
            return result;
        }


        private uint FindSafePoint(byte[] image, Machine target, uint breakOffset, ref int currentPos)
        {
            if (NumSafePoints == 0)
                return 0;

            int savedPos = currentPos;
            uint numBitsPerOffset = CeilOfLog2(CodeLength);
            uint result = NumSafePoints;

            // Safepoints are encoded with a -1 adjustment
            // but normalizing them masks off the low order bit
            // Thus only bother looking if the address is odd
            if ((target != Machine.Arm && target != Machine.Arm64) || (breakOffset & 1) != 0)
            {
                int low = 0;
                int high = (int)NumSafePoints;

                while (low < high)
                {
                    int mid = (low + high) / 2;
                    currentPos = (int)(savedPos + mid * numBitsPerOffset);
                    uint normOffset = (uint)NativeReader.ReadBits(image, (int)numBitsPerOffset, ref currentPos);
                    if (normOffset == breakOffset)
                    {
                        result = (uint)mid;
                        break;
                    }

                    if (normOffset < breakOffset)
                        low = mid + 1;
                    else
                        high = mid;
                }
            }

            currentPos = (int)(savedPos + NumSafePoints * numBitsPerOffset);
            return result;
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
