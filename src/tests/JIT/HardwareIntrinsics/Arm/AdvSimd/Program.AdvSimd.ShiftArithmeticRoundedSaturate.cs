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
                ["ShiftArithmeticRoundedSaturate.Vector64.Int16"] = ShiftArithmeticRoundedSaturate_Vector64_Int16,
                ["ShiftArithmeticRoundedSaturate.Vector64.Int32"] = ShiftArithmeticRoundedSaturate_Vector64_Int32,
                ["ShiftArithmeticRoundedSaturate.Vector64.SByte"] = ShiftArithmeticRoundedSaturate_Vector64_SByte,
                ["ShiftArithmeticRoundedSaturate.Vector128.Int16"] = ShiftArithmeticRoundedSaturate_Vector128_Int16,
                ["ShiftArithmeticRoundedSaturate.Vector128.Int32"] = ShiftArithmeticRoundedSaturate_Vector128_Int32,
                ["ShiftArithmeticRoundedSaturate.Vector128.Int64"] = ShiftArithmeticRoundedSaturate_Vector128_Int64,
                ["ShiftArithmeticRoundedSaturate.Vector128.SByte"] = ShiftArithmeticRoundedSaturate_Vector128_SByte,
            };
        }
    }
}
