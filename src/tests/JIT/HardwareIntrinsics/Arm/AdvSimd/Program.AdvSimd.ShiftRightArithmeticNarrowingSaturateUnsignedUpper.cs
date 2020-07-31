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
                ["ShiftRightArithmeticNarrowingSaturateUnsignedUpper.Vector128.Byte.1"] = ShiftRightArithmeticNarrowingSaturateUnsignedUpper_Vector128_Byte_1,
                ["ShiftRightArithmeticNarrowingSaturateUnsignedUpper.Vector128.UInt16.1"] = ShiftRightArithmeticNarrowingSaturateUnsignedUpper_Vector128_UInt16_1,
                ["ShiftRightArithmeticNarrowingSaturateUnsignedUpper.Vector128.UInt32.1"] = ShiftRightArithmeticNarrowingSaturateUnsignedUpper_Vector128_UInt32_1,
            };
        }
    }
}
