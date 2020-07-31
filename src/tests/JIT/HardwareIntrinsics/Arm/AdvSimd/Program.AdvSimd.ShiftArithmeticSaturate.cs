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
                ["ShiftArithmeticSaturate.Vector64.Int16"] = ShiftArithmeticSaturate_Vector64_Int16,
                ["ShiftArithmeticSaturate.Vector64.Int32"] = ShiftArithmeticSaturate_Vector64_Int32,
                ["ShiftArithmeticSaturate.Vector64.SByte"] = ShiftArithmeticSaturate_Vector64_SByte,
                ["ShiftArithmeticSaturate.Vector128.Int16"] = ShiftArithmeticSaturate_Vector128_Int16,
                ["ShiftArithmeticSaturate.Vector128.Int32"] = ShiftArithmeticSaturate_Vector128_Int32,
                ["ShiftArithmeticSaturate.Vector128.Int64"] = ShiftArithmeticSaturate_Vector128_Int64,
                ["ShiftArithmeticSaturate.Vector128.SByte"] = ShiftArithmeticSaturate_Vector128_SByte,
            };
        }
    }
}
