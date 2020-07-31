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
                ["ShiftArithmeticRounded.Vector64.Int16"] = ShiftArithmeticRounded_Vector64_Int16,
                ["ShiftArithmeticRounded.Vector64.Int32"] = ShiftArithmeticRounded_Vector64_Int32,
                ["ShiftArithmeticRounded.Vector64.SByte"] = ShiftArithmeticRounded_Vector64_SByte,
                ["ShiftArithmeticRounded.Vector128.Int16"] = ShiftArithmeticRounded_Vector128_Int16,
                ["ShiftArithmeticRounded.Vector128.Int32"] = ShiftArithmeticRounded_Vector128_Int32,
                ["ShiftArithmeticRounded.Vector128.Int64"] = ShiftArithmeticRounded_Vector128_Int64,
                ["ShiftArithmeticRounded.Vector128.SByte"] = ShiftArithmeticRounded_Vector128_SByte,
            };
        }
    }
}
