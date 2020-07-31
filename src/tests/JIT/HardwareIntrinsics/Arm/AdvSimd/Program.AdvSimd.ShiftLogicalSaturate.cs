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
                ["ShiftLogicalSaturate.Vector64.Byte"] = ShiftLogicalSaturate_Vector64_Byte,
                ["ShiftLogicalSaturate.Vector64.Int16"] = ShiftLogicalSaturate_Vector64_Int16,
                ["ShiftLogicalSaturate.Vector64.Int32"] = ShiftLogicalSaturate_Vector64_Int32,
                ["ShiftLogicalSaturate.Vector64.SByte"] = ShiftLogicalSaturate_Vector64_SByte,
                ["ShiftLogicalSaturate.Vector64.UInt16"] = ShiftLogicalSaturate_Vector64_UInt16,
                ["ShiftLogicalSaturate.Vector64.UInt32"] = ShiftLogicalSaturate_Vector64_UInt32,
                ["ShiftLogicalSaturate.Vector128.Byte"] = ShiftLogicalSaturate_Vector128_Byte,
                ["ShiftLogicalSaturate.Vector128.Int16"] = ShiftLogicalSaturate_Vector128_Int16,
                ["ShiftLogicalSaturate.Vector128.Int32"] = ShiftLogicalSaturate_Vector128_Int32,
                ["ShiftLogicalSaturate.Vector128.Int64"] = ShiftLogicalSaturate_Vector128_Int64,
                ["ShiftLogicalSaturate.Vector128.SByte"] = ShiftLogicalSaturate_Vector128_SByte,
                ["ShiftLogicalSaturate.Vector128.UInt16"] = ShiftLogicalSaturate_Vector128_UInt16,
                ["ShiftLogicalSaturate.Vector128.UInt32"] = ShiftLogicalSaturate_Vector128_UInt32,
                ["ShiftLogicalSaturate.Vector128.UInt64"] = ShiftLogicalSaturate_Vector128_UInt64,
            };
        }
    }
}
