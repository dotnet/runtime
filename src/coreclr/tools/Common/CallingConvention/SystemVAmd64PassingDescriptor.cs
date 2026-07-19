// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// System V AMD64 ABI struct passing classification types.
// Extracted from JitInterface/CorInfoTypes.cs for standalone use.
// See ABI spec: https://software.intel.com/sites/default/files/article/402129/mpx-linux64-abi.pdf

namespace Internal.JitInterface
{
    public enum SystemVClassificationType : byte
    {
        SystemVClassificationTypeUnknown            = 0,
        SystemVClassificationTypeStruct             = 1,
        SystemVClassificationTypeNoClass            = 2,
        SystemVClassificationTypeMemory             = 3,
        SystemVClassificationTypeInteger            = 4,
        SystemVClassificationTypeIntegerReference   = 5,
        SystemVClassificationTypeIntegerByRef       = 6,
        SystemVClassificationTypeSSE                = 7,
    };

    public struct SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR
    {
        public const int CLR_SYSTEMV_MAX_EIGHTBYTES_COUNT_TO_PASS_IN_REGISTERS = 2;
        public const int CLR_SYSTEMV_MAX_STRUCT_BYTES_TO_PASS_IN_REGISTERS = 16;

        public const int SYSTEMV_EIGHT_BYTE_SIZE_IN_BYTES = 8;
        public const int SYSTEMV_MAX_NUM_FIELDS_IN_REGISTER_PASSED_STRUCT = 16;

        public byte _passedInRegisters;
        public bool passedInRegisters { get { return _passedInRegisters != 0; } set { _passedInRegisters = value ? (byte)1 : (byte)0; } }

        public byte eightByteCount;

        public SystemVClassificationType eightByteClassifications0;
        public SystemVClassificationType eightByteClassifications1;

        public byte eightByteSizes0;
        public byte eightByteSizes1;

        public byte eightByteOffsets0;
        public byte eightByteOffsets1;
    };
}
