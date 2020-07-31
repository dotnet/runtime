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
                ["ShiftRightArithmeticNarrowingSaturateScalar.Vector64.Int16.16"] = ShiftRightArithmeticNarrowingSaturateScalar_Vector64_Int16_16,
                ["ShiftRightArithmeticNarrowingSaturateScalar.Vector64.Int32.32"] = ShiftRightArithmeticNarrowingSaturateScalar_Vector64_Int32_32,
                ["ShiftRightArithmeticNarrowingSaturateScalar.Vector64.SByte.8"] = ShiftRightArithmeticNarrowingSaturateScalar_Vector64_SByte_8,
            };
        }
    }
}
