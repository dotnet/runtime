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
                ["MultiplyDoublingWideningUpperByScalarAndAddSaturate.Vector128.Int16.Vector64.Int16"] = MultiplyDoublingWideningUpperByScalarAndAddSaturate_Vector128_Int16_Vector64_Int16,
                ["MultiplyDoublingWideningUpperByScalarAndAddSaturate.Vector128.Int32.Vector64.Int32"] = MultiplyDoublingWideningUpperByScalarAndAddSaturate_Vector128_Int32_Vector64_Int32,
            };
        }
    }
}
