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
                ["ShiftLogicalRoundedSaturate.Vector64.Byte"] = ShiftLogicalRoundedSaturate_Vector64_Byte,
                ["ShiftLogicalRoundedSaturate.Vector64.Int16"] = ShiftLogicalRoundedSaturate_Vector64_Int16,
                ["ShiftLogicalRoundedSaturate.Vector64.Int32"] = ShiftLogicalRoundedSaturate_Vector64_Int32,
                ["ShiftLogicalRoundedSaturate.Vector64.SByte"] = ShiftLogicalRoundedSaturate_Vector64_SByte,
                ["ShiftLogicalRoundedSaturate.Vector64.UInt16"] = ShiftLogicalRoundedSaturate_Vector64_UInt16,
                ["ShiftLogicalRoundedSaturate.Vector64.UInt32"] = ShiftLogicalRoundedSaturate_Vector64_UInt32,
                ["ShiftLogicalRoundedSaturate.Vector128.Byte"] = ShiftLogicalRoundedSaturate_Vector128_Byte,
                ["ShiftLogicalRoundedSaturate.Vector128.Int16"] = ShiftLogicalRoundedSaturate_Vector128_Int16,
                ["ShiftLogicalRoundedSaturate.Vector128.Int32"] = ShiftLogicalRoundedSaturate_Vector128_Int32,
                ["ShiftLogicalRoundedSaturate.Vector128.Int64"] = ShiftLogicalRoundedSaturate_Vector128_Int64,
                ["ShiftLogicalRoundedSaturate.Vector128.SByte"] = ShiftLogicalRoundedSaturate_Vector128_SByte,
                ["ShiftLogicalRoundedSaturate.Vector128.UInt16"] = ShiftLogicalRoundedSaturate_Vector128_UInt16,
                ["ShiftLogicalRoundedSaturate.Vector128.UInt32"] = ShiftLogicalRoundedSaturate_Vector128_UInt32,
                ["ShiftLogicalRoundedSaturate.Vector128.UInt64"] = ShiftLogicalRoundedSaturate_Vector128_UInt64,
            };
        }
    }
}
