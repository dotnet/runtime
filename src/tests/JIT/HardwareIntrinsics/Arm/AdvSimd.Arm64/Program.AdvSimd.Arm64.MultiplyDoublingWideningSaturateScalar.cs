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
                ["MultiplyDoublingWideningSaturateScalar.Vector64.Int16"] = MultiplyDoublingWideningSaturateScalar_Vector64_Int16,
                ["MultiplyDoublingWideningSaturateScalar.Vector64.Int32"] = MultiplyDoublingWideningSaturateScalar_Vector64_Int32,
            };
        }
    }
}
