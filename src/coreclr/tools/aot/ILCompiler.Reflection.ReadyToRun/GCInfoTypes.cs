// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.Reflection.ReadyToRun
{
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
