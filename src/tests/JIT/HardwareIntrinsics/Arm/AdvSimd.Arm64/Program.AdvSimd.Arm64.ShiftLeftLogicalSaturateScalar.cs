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
                ["ShiftLeftLogicalSaturateScalar.Vector64.Byte.7"] = ShiftLeftLogicalSaturateScalar_Vector64_Byte_7,
                ["ShiftLeftLogicalSaturateScalar.Vector64.Int16.15"] = ShiftLeftLogicalSaturateScalar_Vector64_Int16_15,
                ["ShiftLeftLogicalSaturateScalar.Vector64.Int32.31"] = ShiftLeftLogicalSaturateScalar_Vector64_Int32_31,
                ["ShiftLeftLogicalSaturateScalar.Vector64.SByte.1"] = ShiftLeftLogicalSaturateScalar_Vector64_SByte_1,
                ["ShiftLeftLogicalSaturateScalar.Vector64.UInt16.1"] = ShiftLeftLogicalSaturateScalar_Vector64_UInt16_1,
                ["ShiftLeftLogicalSaturateScalar.Vector64.UInt32.1"] = ShiftLeftLogicalSaturateScalar_Vector64_UInt32_1,
            };
        }
    }
}
