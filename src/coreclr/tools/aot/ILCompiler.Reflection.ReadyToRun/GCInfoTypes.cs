// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.PortableExecutable;
using System.Text;

namespace ILCompiler.Reflection.ReadyToRun
{
    /// <summary>
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcinfotypes.h">src\inc\gcinfotypes.h</a> infoHdrAdjustConstants
    /// </summary>
    enum InfoHdrAdjustConstants
    {
        // Constants
        SET_FRAMESIZE_MAX = 7,
        SET_ARGCOUNT_MAX = 8,
        SET_PROLOGSIZE_MAX = 16,
        SET_EPILOGSIZE_MAX = 10,
        SET_EPILOGCNT_MAX = 4,
        SET_UNTRACKED_MAX = 3,
        SET_RET_KIND_MAX = 4,
        ADJ_ENCODING_MAX = 0x7f,
        MORE_BYTES_TO_FOLLOW = 0x80
    };

    /// <summary>
    /// Enum to define codes that are used to incrementally adjust the InfoHdr structure.
    /// based on <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcinfotypes.h">src\inc\gcinfotypes.h</a> infoHdrAdjustConstants
    /// </summary>
    enum InfoHdrAdjust
    {
        SET_FRAMESIZE = 0,                                            // 0x00
        SET_ARGCOUNT = SET_FRAMESIZE + InfoHdrAdjustConstants.SET_FRAMESIZE_MAX + 1,      // 0x08
        SET_PROLOGSIZE = SET_ARGCOUNT + InfoHdrAdjustConstants.SET_ARGCOUNT_MAX + 1,      // 0x11
        SET_EPILOGSIZE = SET_PROLOGSIZE + InfoHdrAdjustConstants.SET_PROLOGSIZE_MAX + 1,      // 0x22
        SET_EPILOGCNT = SET_EPILOGSIZE + InfoHdrAdjustConstants.SET_EPILOGSIZE_MAX + 1,      // 0x2d
        SET_UNTRACKED = SET_EPILOGCNT + (InfoHdrAdjustConstants.SET_EPILOGCNT_MAX + 1) * 2, // 0x37

        FIRST_FLIP = SET_UNTRACKED + InfoHdrAdjustConstants.SET_UNTRACKED_MAX + 1,

        FLIP_EDI_SAVED = FIRST_FLIP, // 0x3b
        FLIP_ESI_SAVED,           // 0x3c
        FLIP_EBX_SAVED,           // 0x3d
        FLIP_EBP_SAVED,           // 0x3e
        FLIP_EBP_FRAME,           // 0x3f
        FLIP_INTERRUPTIBLE,       // 0x40
        FLIP_DOUBLE_ALIGN,        // 0x41
        FLIP_SECURITY,            // 0x42
        FLIP_HANDLERS,            // 0x43
        FLIP_LOCALLOC,            // 0x44
        FLIP_EDITnCONTINUE,       // 0x45
        FLIP_VAR_PTR_TABLE_SZ,    // 0x46 Flip whether a table-size exits after the header encoding
        FFFF_UNTRACKED_CNT,       // 0x47 There is a count (>SET_UNTRACKED_MAX) after the header encoding
        FLIP_VARARGS,             // 0x48
        FLIP_PROF_CALLBACKS,      // 0x49
        FLIP_HAS_GS_COOKIE,       // 0x4A - The offset of the GuardStack cookie follows after the header encoding
        FLIP_SYNC,                // 0x4B
        FLIP_HAS_GENERICS_CONTEXT,// 0x4C
        FLIP_GENERICS_CONTEXT_IS_METHODDESC,// 0x4D
        FLIP_REV_PINVOKE_FRAME,   // 0x4E
        NEXT_OPCODE,              // 0x4F -- see next Adjustment enumeration
        NEXT_FOUR_START = 0x50,
        NEXT_FOUR_FRAMESIZE = 0x50,
        NEXT_FOUR_ARGCOUNT = 0x60,
        NEXT_THREE_PROLOGSIZE = 0x70,
        NEXT_THREE_EPILOGSIZE = 0x78
    };

    /// <summary>
    /// based on macros defined in <a href="https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/gcinfotypes.h">src\inc\gcinfotypes.h</a>
    /// </summary>
    public class GcInfoTypes
    {
        private Machine _target;

        internal int SIZE_OF_RETURN_KIND_SLIM { get; } = 2;
        internal int SIZE_OF_RETURN_KIND_FAT { get; } = 2;
        internal int CODE_LENGTH_ENCBASE { get; } = 8;
        internal int NORM_PROLOG_SIZE_ENCBASE { get; } = 5;
        internal int NORM_EPILOG_SIZE_ENCBASE { get; } = 3;
        internal int SECURITY_OBJECT_STACK_SLOT_ENCBASE { get; } = 6;
        internal int GS_COOKIE_STACK_SLOT_ENCBASE { get; } = 6;
        internal int PSP_SYM_STACK_SLOT_ENCBASE { get; } = 6;
        internal int GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE { get; } = 6;
        internal int STACK_BASE_REGISTER_ENCBASE { get; } = 3;
        internal int SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE { get; } = 4;
        internal int REVERSE_PINVOKE_FRAME_ENCBASE { get; } = 6;
        internal int SIZE_OF_STACK_AREA_ENCBASE { get; } = 3;
        internal int NUM_SAFE_POINTS_ENCBASE { get; } = 3;
        internal int NUM_INTERRUPTIBLE_RANGES_ENCBASE { get; } = 1;
        internal int INTERRUPTIBLE_RANGE_DELTA1_ENCBASE { get; } = 6;
        internal int INTERRUPTIBLE_RANGE_DELTA2_ENCBASE { get; } = 6;

        internal int NUM_REGISTERS_ENCBASE { get; } = 2;
        internal int NUM_STACK_SLOTS_ENCBASE { get; } = 2;
        internal int NUM_UNTRACKED_SLOTS_ENCBASE { get; } = 1;
        internal int REGISTER_ENCBASE { get; } = 3;
        internal int REGISTER_DELTA_ENCBASE { get; } = 2;
        internal int STACK_SLOT_ENCBASE { get; } = 6;
        internal int STACK_SLOT_DELTA_ENCBASE { get; } = 4;
        internal int POINTER_SIZE_ENCBASE { get; } = 3;
        internal int NUM_NORM_CODE_OFFSETS_PER_CHUNK { get; } = 64;
        internal int LIVESTATE_RLE_RUN_ENCBASE { get; } = 2;
        internal int LIVESTATE_RLE_SKIP_ENCBASE { get; } = 4;
        internal int NUM_NORM_CODE_OFFSETS_PER_CHUNK_LOG2 { get; } = 6;

        internal GcInfoTypes(Machine machine)
        {
            _target = machine;

            switch (machine)
            {
                case Machine.Amd64:
                    SIZE_OF_RETURN_KIND_FAT = 4;
                    NUM_SAFE_POINTS_ENCBASE = 2;
                    break;
                case Machine.ArmThumb2:
                    CODE_LENGTH_ENCBASE = 7;
                    SECURITY_OBJECT_STACK_SLOT_ENCBASE = 5;
                    GS_COOKIE_STACK_SLOT_ENCBASE = 5;
                    PSP_SYM_STACK_SLOT_ENCBASE = 5;
                    GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE = 5;
                    STACK_BASE_REGISTER_ENCBASE = 1;
                    SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE = 3;
                    REVERSE_PINVOKE_FRAME_ENCBASE = 5;
                    NUM_INTERRUPTIBLE_RANGES_ENCBASE = 2;
                    INTERRUPTIBLE_RANGE_DELTA1_ENCBASE = 4;
                    NUM_STACK_SLOTS_ENCBASE = 3;
                    NUM_UNTRACKED_SLOTS_ENCBASE = 3;
                    REGISTER_ENCBASE = 2;
                    REGISTER_DELTA_ENCBASE = 1;
                    break;
                case Machine.Arm64:
                    SIZE_OF_RETURN_KIND_FAT = 4;
                    STACK_BASE_REGISTER_ENCBASE = 2;
                    NUM_REGISTERS_ENCBASE = 3;
                    break;
                case Machine.I386:
                    CODE_LENGTH_ENCBASE = 6;
                    NORM_PROLOG_SIZE_ENCBASE = 4;
                    SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE = 3;
                    SIZE_OF_STACK_AREA_ENCBASE = 6;
                    NUM_SAFE_POINTS_ENCBASE = 4;
                    INTERRUPTIBLE_RANGE_DELTA1_ENCBASE = 5;
                    INTERRUPTIBLE_RANGE_DELTA2_ENCBASE = 5;
                    NUM_REGISTERS_ENCBASE = 3;
                    NUM_STACK_SLOTS_ENCBASE = 5;
                    NUM_UNTRACKED_SLOTS_ENCBASE = 5;
                    REGISTER_DELTA_ENCBASE = 3;
                    break;
                case Machine.LoongArch64:
                    SIZE_OF_RETURN_KIND_FAT = 4;
                    STACK_BASE_REGISTER_ENCBASE = 2;
                    NUM_REGISTERS_ENCBASE = 3;
                    break;
            }
        }

        internal int DenormalizeCodeLength(int x)
        {
            switch (_target)
            {
                case Machine.ArmThumb2:
                    return (x << 1);
                case Machine.Arm64:
                case Machine.LoongArch64:
                    return (x << 2);
            }
            return x;
        }

        internal int DenormalizeStackSlot(int x)
        {
            switch (_target)
            {
                case Machine.Amd64:
                    return (x << 3);
                case Machine.ArmThumb2:
                    return (x << 2);
                case Machine.Arm64:
                case Machine.LoongArch64:
                    return (x << 3);
            }
            return x;
        }

        internal uint DenormalizeStackBaseRegister(uint x)
        {
            switch (_target)
            {
                case Machine.Amd64:
                    return (x ^ 5);
                case Machine.ArmThumb2:
                    return ((x ^ 7) + 4);
                case Machine.Arm64:
                    return (x ^ 29);
                case Machine.LoongArch64:
                    return ((x ^ 22) & 0x3);
            }
            return x;
        }

        internal uint DenormalizeSizeOfStackArea(uint x)
        {
            switch (_target)
            {
                case Machine.Amd64:
                    return (x << 3);
                case Machine.ArmThumb2:
                    return (x << 2);
                case Machine.Arm64:
                case Machine.LoongArch64:
                    return (x << 3);
            }
            return x;
        }

        internal static uint CeilOfLog2(int x)
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
    }

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

    [Flags]
    public enum GcSlotFlags
    {
        GC_SLOT_BASE = 0x0,
        GC_SLOT_INTERIOR = 0x1,
        GC_SLOT_PINNED = 0x2,
        GC_SLOT_UNTRACKED = 0x4,

        GC_SLOT_INVALID = -1
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
        public int SpOffset { get; set; }
        public GcStackSlotBase Base { get; set; }

        public GcStackSlot() { }

        public GcStackSlot(int spOffset, GcStackSlotBase stackSlotBase)
        {
            SpOffset = spOffset;
            Base = stackSlotBase;
        }

        public override string ToString()
        {
            return $@"{Enum.GetName(typeof(GcStackSlotBase), Base)}+0x{SpOffset:X2}";
        }
    };
}
