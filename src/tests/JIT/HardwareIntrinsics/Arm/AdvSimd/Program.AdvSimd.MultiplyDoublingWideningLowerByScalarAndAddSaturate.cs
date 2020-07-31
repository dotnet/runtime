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
                ["MultiplyDoublingWideningLowerByScalarAndAddSaturate.Vector64.Int16.Vector64.Int16"] = MultiplyDoublingWideningLowerByScalarAndAddSaturate_Vector64_Int16_Vector64_Int16,
                ["MultiplyDoublingWideningLowerByScalarAndAddSaturate.Vector64.Int32.Vector64.Int32"] = MultiplyDoublingWideningLowerByScalarAndAddSaturate_Vector64_Int32_Vector64_Int32,
            };
        }
    }
}
