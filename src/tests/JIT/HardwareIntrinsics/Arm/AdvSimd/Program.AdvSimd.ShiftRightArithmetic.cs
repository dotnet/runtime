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
                ["ShiftRightArithmetic.Vector64.Int16.1"] = ShiftRightArithmetic_Vector64_Int16_1,
                ["ShiftRightArithmetic.Vector64.Int32.1"] = ShiftRightArithmetic_Vector64_Int32_1,
                ["ShiftRightArithmetic.Vector64.SByte.1"] = ShiftRightArithmetic_Vector64_SByte_1,
                ["ShiftRightArithmetic.Vector128.Int16.1"] = ShiftRightArithmetic_Vector128_Int16_1,
                ["ShiftRightArithmetic.Vector128.Int32.1"] = ShiftRightArithmetic_Vector128_Int32_1,
                ["ShiftRightArithmetic.Vector128.Int64.1"] = ShiftRightArithmetic_Vector128_Int64_1,
                ["ShiftRightArithmetic.Vector128.SByte.1"] = ShiftRightArithmetic_Vector128_SByte_1,
            };
        }
    }
}
