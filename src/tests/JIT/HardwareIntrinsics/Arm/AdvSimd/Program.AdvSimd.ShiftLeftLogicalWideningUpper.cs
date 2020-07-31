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
                ["ShiftLeftLogicalWideningUpper.Vector128.Byte.1"] = ShiftLeftLogicalWideningUpper_Vector128_Byte_1,
                ["ShiftLeftLogicalWideningUpper.Vector128.Int16.1"] = ShiftLeftLogicalWideningUpper_Vector128_Int16_1,
                ["ShiftLeftLogicalWideningUpper.Vector128.Int32.1"] = ShiftLeftLogicalWideningUpper_Vector128_Int32_1,
                ["ShiftLeftLogicalWideningUpper.Vector128.SByte.1"] = ShiftLeftLogicalWideningUpper_Vector128_SByte_1,
                ["ShiftLeftLogicalWideningUpper.Vector128.UInt16.1"] = ShiftLeftLogicalWideningUpper_Vector128_UInt16_1,
                ["ShiftLeftLogicalWideningUpper.Vector128.UInt32.1"] = ShiftLeftLogicalWideningUpper_Vector128_UInt32_1,
            };
        }
    }
}
