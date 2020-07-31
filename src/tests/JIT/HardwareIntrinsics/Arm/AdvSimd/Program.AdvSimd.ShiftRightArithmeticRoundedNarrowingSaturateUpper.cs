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
                ["ShiftRightArithmeticRoundedNarrowingSaturateUpper.Vector128.Int16.1"] = ShiftRightArithmeticRoundedNarrowingSaturateUpper_Vector128_Int16_1,
                ["ShiftRightArithmeticRoundedNarrowingSaturateUpper.Vector128.Int32.1"] = ShiftRightArithmeticRoundedNarrowingSaturateUpper_Vector128_Int32_1,
                ["ShiftRightArithmeticRoundedNarrowingSaturateUpper.Vector128.SByte.1"] = ShiftRightArithmeticRoundedNarrowingSaturateUpper_Vector128_SByte_1,
            };
        }
    }
}
