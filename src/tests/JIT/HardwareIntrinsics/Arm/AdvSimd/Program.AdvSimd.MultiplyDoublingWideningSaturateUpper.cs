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
                ["MultiplyDoublingWideningSaturateUpper.Vector128.Int16"] = MultiplyDoublingWideningSaturateUpper_Vector128_Int16,
                ["MultiplyDoublingWideningSaturateUpper.Vector128.Int32"] = MultiplyDoublingWideningSaturateUpper_Vector128_Int32,
            };
        }
    }
}
