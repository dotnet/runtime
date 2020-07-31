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
                ["ShiftRightLogicalAdd.Vector64.Byte.1"] = ShiftRightLogicalAdd_Vector64_Byte_1,
                ["ShiftRightLogicalAdd.Vector64.Int16.1"] = ShiftRightLogicalAdd_Vector64_Int16_1,
                ["ShiftRightLogicalAdd.Vector64.Int32.1"] = ShiftRightLogicalAdd_Vector64_Int32_1,
                ["ShiftRightLogicalAdd.Vector64.SByte.1"] = ShiftRightLogicalAdd_Vector64_SByte_1,
                ["ShiftRightLogicalAdd.Vector64.UInt16.1"] = ShiftRightLogicalAdd_Vector64_UInt16_1,
                ["ShiftRightLogicalAdd.Vector64.UInt32.1"] = ShiftRightLogicalAdd_Vector64_UInt32_1,
                ["ShiftRightLogicalAdd.Vector128.Byte.1"] = ShiftRightLogicalAdd_Vector128_Byte_1,
                ["ShiftRightLogicalAdd.Vector128.Int16.1"] = ShiftRightLogicalAdd_Vector128_Int16_1,
                ["ShiftRightLogicalAdd.Vector128.Int32.1"] = ShiftRightLogicalAdd_Vector128_Int32_1,
                ["ShiftRightLogicalAdd.Vector128.Int64.1"] = ShiftRightLogicalAdd_Vector128_Int64_1,
                ["ShiftRightLogicalAdd.Vector128.SByte.1"] = ShiftRightLogicalAdd_Vector128_SByte_1,
                ["ShiftRightLogicalAdd.Vector128.UInt16.1"] = ShiftRightLogicalAdd_Vector128_UInt16_1,
                ["ShiftRightLogicalAdd.Vector128.UInt32.1"] = ShiftRightLogicalAdd_Vector128_UInt32_1,
                ["ShiftRightLogicalAdd.Vector128.UInt64.1"] = ShiftRightLogicalAdd_Vector128_UInt64_1,
            };
        }
    }
}
