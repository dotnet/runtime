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
                ["MultiplyDoublingWideningUpperAndSubtractSaturate.Vector128.Int16"] = MultiplyDoublingWideningUpperAndSubtractSaturate_Vector128_Int16,
                ["MultiplyDoublingWideningUpperAndSubtractSaturate.Vector128.Int32"] = MultiplyDoublingWideningUpperAndSubtractSaturate_Vector128_Int32,
            };
        }
    }
}
