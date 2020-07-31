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
                ["MultiplyRoundedDoublingByScalarSaturateHigh.Vector64.Int16"] = MultiplyRoundedDoublingByScalarSaturateHigh_Vector64_Int16,
                ["MultiplyRoundedDoublingByScalarSaturateHigh.Vector64.Int32"] = MultiplyRoundedDoublingByScalarSaturateHigh_Vector64_Int32,
                ["MultiplyRoundedDoublingByScalarSaturateHigh.Vector128.Int16"] = MultiplyRoundedDoublingByScalarSaturateHigh_Vector128_Int16,
                ["MultiplyRoundedDoublingByScalarSaturateHigh.Vector128.Int32"] = MultiplyRoundedDoublingByScalarSaturateHigh_Vector128_Int32,
            };
        }
    }
}
