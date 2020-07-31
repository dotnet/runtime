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
                ["ShiftRightArithmeticAdd.Vector64.Int16.1"] = ShiftRightArithmeticAdd_Vector64_Int16_1,
                ["ShiftRightArithmeticAdd.Vector64.Int32.1"] = ShiftRightArithmeticAdd_Vector64_Int32_1,
                ["ShiftRightArithmeticAdd.Vector64.SByte.1"] = ShiftRightArithmeticAdd_Vector64_SByte_1,
                ["ShiftRightArithmeticAdd.Vector128.Int16.1"] = ShiftRightArithmeticAdd_Vector128_Int16_1,
                ["ShiftRightArithmeticAdd.Vector128.Int32.1"] = ShiftRightArithmeticAdd_Vector128_Int32_1,
                ["ShiftRightArithmeticAdd.Vector128.Int64.1"] = ShiftRightArithmeticAdd_Vector128_Int64_1,
                ["ShiftRightArithmeticAdd.Vector128.SByte.1"] = ShiftRightArithmeticAdd_Vector128_SByte_1,
            };
        }
    }
}
