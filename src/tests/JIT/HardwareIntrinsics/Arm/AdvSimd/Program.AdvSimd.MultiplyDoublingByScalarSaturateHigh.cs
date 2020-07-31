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
                ["MultiplyDoublingByScalarSaturateHigh.Vector64.Int16"] = MultiplyDoublingByScalarSaturateHigh_Vector64_Int16,
                ["MultiplyDoublingByScalarSaturateHigh.Vector64.Int32"] = MultiplyDoublingByScalarSaturateHigh_Vector64_Int32,
                ["MultiplyDoublingByScalarSaturateHigh.Vector128.Int16"] = MultiplyDoublingByScalarSaturateHigh_Vector128_Int16,
                ["MultiplyDoublingByScalarSaturateHigh.Vector128.Int32"] = MultiplyDoublingByScalarSaturateHigh_Vector128_Int32,
            };
        }
    }
}
