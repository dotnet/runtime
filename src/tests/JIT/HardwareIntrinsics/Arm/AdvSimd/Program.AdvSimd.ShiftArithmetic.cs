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
                ["ShiftArithmetic.Vector64.Int16"] = ShiftArithmetic_Vector64_Int16,
                ["ShiftArithmetic.Vector64.Int32"] = ShiftArithmetic_Vector64_Int32,
                ["ShiftArithmetic.Vector64.SByte"] = ShiftArithmetic_Vector64_SByte,
                ["ShiftArithmetic.Vector128.Int16"] = ShiftArithmetic_Vector128_Int16,
                ["ShiftArithmetic.Vector128.Int32"] = ShiftArithmetic_Vector128_Int32,
                ["ShiftArithmetic.Vector128.Int64"] = ShiftArithmetic_Vector128_Int64,
                ["ShiftArithmetic.Vector128.SByte"] = ShiftArithmetic_Vector128_SByte,
            };
        }
    }
}
