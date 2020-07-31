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
                ["ShiftRightArithmeticNarrowingSaturateUnsignedLower.Vector64.Byte.1"] = ShiftRightArithmeticNarrowingSaturateUnsignedLower_Vector64_Byte_1,
                ["ShiftRightArithmeticNarrowingSaturateUnsignedLower.Vector64.UInt16.1"] = ShiftRightArithmeticNarrowingSaturateUnsignedLower_Vector64_UInt16_1,
                ["ShiftRightArithmeticNarrowingSaturateUnsignedLower.Vector64.UInt32.1"] = ShiftRightArithmeticNarrowingSaturateUnsignedLower_Vector64_UInt32_1,
            };
        }
    }
}
