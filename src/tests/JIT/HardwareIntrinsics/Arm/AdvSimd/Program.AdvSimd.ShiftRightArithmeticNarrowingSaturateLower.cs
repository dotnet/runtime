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
                ["ShiftRightArithmeticNarrowingSaturateLower.Vector64.Int16.1"] = ShiftRightArithmeticNarrowingSaturateLower_Vector64_Int16_1,
                ["ShiftRightArithmeticNarrowingSaturateLower.Vector64.Int32.1"] = ShiftRightArithmeticNarrowingSaturateLower_Vector64_Int32_1,
                ["ShiftRightArithmeticNarrowingSaturateLower.Vector64.SByte.1"] = ShiftRightArithmeticNarrowingSaturateLower_Vector64_SByte_1,
            };
        }
    }
}
