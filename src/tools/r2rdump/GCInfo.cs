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

        public GCInfo(byte[] image, int offset, Machine machine, ushort majorVersion)
        {
            int sizeOfReturnKindSlim = 2;
            int sizeOfReturnKindFat = 2;
            int codeLengthEncBase = 8;
            switch (machine)
            {
                case Machine.Amd64:
                    sizeOfReturnKindFat = 4;
                    break;
                case Machine.Arm:
                    codeLengthEncBase = 7;
                    break;
                case Machine.Arm64:
                    sizeOfReturnKindFat = 4;
                    break;
                case Machine.I386:
                    codeLengthEncBase = 6;
                    break;
            }

            GcInfoHeaderFlags headerFlags;
            Version = ReadyToRunVersionToGcInfoVersion(majorVersion);
            int bitOffset = 0;
            bool slimHeader = (NativeReader.ReadBits(image, 1, ref offset, ref bitOffset) == 0);

            if (slimHeader)
            {
                headerFlags = NativeReader.ReadBits(image, 1, ref offset, ref bitOffset) == 1 ? GcInfoHeaderFlags.GC_INFO_HAS_STACK_BASE_REGISTER : 0;
            }
            else
            {
                int numFlagBits = (int)((Version == 1) ? GcInfoHeaderFlags.GC_INFO_FLAGS_BIT_SIZE_VERSION_1 : GcInfoHeaderFlags.GC_INFO_FLAGS_BIT_SIZE);
                headerFlags = (GcInfoHeaderFlags)NativeReader.ReadBits(image, numFlagBits, ref offset, ref bitOffset);
            }

            bool hasReversePInvokeFrame = false;
            if (Version >= MIN_GCINFO_VERSION_WITH_REV_PINVOKE_FRAME) // IsReversePInvokeFrameAvailable
            {
                hasReversePInvokeFrame = (headerFlags & GcInfoHeaderFlags.GC_INFO_REVERSE_PINVOKE_FRAME) != 0;
            }

            if (Version >= MIN_GCINFO_VERSION_WITH_RETURN_KIND) // IsReturnKindAvailable
            {
                int returnKindBits = (slimHeader) ? sizeOfReturnKindSlim : sizeOfReturnKindFat;
                ReturnKind = (ReturnKinds)NativeReader.ReadBits(image, returnKindBits, ref offset, ref bitOffset);
            }

            CodeLength = DenormalizeCodeLength(machine, (int)NativeReader.DecodeVarLengthUnsigned(image, codeLengthEncBase, ref offset, ref bitOffset));
        }

        private int DenormalizeCodeLength (Machine target, int x)
        {
            if (target == Machine.Arm)
            {
                return (x << 1);
            }
            if (target == Machine.Arm64)
            {
                return (x << 2);
            }
            return x;
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
