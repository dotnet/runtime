// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>()
            {
                ["ShiftLogicalRounded.Vector64.Byte"] = ShiftLogicalRounded_Vector64_Byte,
                ["ShiftLogicalRounded.Vector64.Int16"] = ShiftLogicalRounded_Vector64_Int16,
                ["ShiftLogicalRounded.Vector64.Int32"] = ShiftLogicalRounded_Vector64_Int32,
                ["ShiftLogicalRounded.Vector64.SByte"] = ShiftLogicalRounded_Vector64_SByte,
                ["ShiftLogicalRounded.Vector64.UInt16"] = ShiftLogicalRounded_Vector64_UInt16,
                ["ShiftLogicalRounded.Vector64.UInt32"] = ShiftLogicalRounded_Vector64_UInt32,
                ["ShiftLogicalRounded.Vector128.Byte"] = ShiftLogicalRounded_Vector128_Byte,
                ["ShiftLogicalRounded.Vector128.Int16"] = ShiftLogicalRounded_Vector128_Int16,
                ["ShiftLogicalRounded.Vector128.Int32"] = ShiftLogicalRounded_Vector128_Int32,
                ["ShiftLogicalRounded.Vector128.Int64"] = ShiftLogicalRounded_Vector128_Int64,
                ["ShiftLogicalRounded.Vector128.SByte"] = ShiftLogicalRounded_Vector128_SByte,
                ["ShiftLogicalRounded.Vector128.UInt16"] = ShiftLogicalRounded_Vector128_UInt16,
                ["ShiftLogicalRounded.Vector128.UInt32"] = ShiftLogicalRounded_Vector128_UInt32,
                ["ShiftLogicalRounded.Vector128.UInt64"] = ShiftLogicalRounded_Vector128_UInt64,
            };
        }
    }
}
