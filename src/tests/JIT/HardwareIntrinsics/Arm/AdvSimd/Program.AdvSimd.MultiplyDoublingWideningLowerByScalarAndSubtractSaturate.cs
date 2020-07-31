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
                ["MultiplyDoublingWideningLowerByScalarAndSubtractSaturate.Vector64.Int16.Vector64.Int16"] = MultiplyDoublingWideningLowerByScalarAndSubtractSaturate_Vector64_Int16_Vector64_Int16,
                ["MultiplyDoublingWideningLowerByScalarAndSubtractSaturate.Vector64.Int32.Vector64.Int32"] = MultiplyDoublingWideningLowerByScalarAndSubtractSaturate_Vector64_Int32_Vector64_Int32,
            };
        }
    }
}
