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
                ["MultiplyDoublingWideningLowerAndAddSaturate.Vector64.Int16"] = MultiplyDoublingWideningLowerAndAddSaturate_Vector64_Int16,
                ["MultiplyDoublingWideningLowerAndAddSaturate.Vector64.Int32"] = MultiplyDoublingWideningLowerAndAddSaturate_Vector64_Int32,
            };
        }
    }
}
