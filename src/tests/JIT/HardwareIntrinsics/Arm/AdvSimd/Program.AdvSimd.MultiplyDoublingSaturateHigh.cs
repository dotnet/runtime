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
                ["MultiplyDoublingSaturateHigh.Vector64.Int16"] = MultiplyDoublingSaturateHigh_Vector64_Int16,
                ["MultiplyDoublingSaturateHigh.Vector64.Int32"] = MultiplyDoublingSaturateHigh_Vector64_Int32,
                ["MultiplyDoublingSaturateHigh.Vector128.Int16"] = MultiplyDoublingSaturateHigh_Vector128_Int16,
                ["MultiplyDoublingSaturateHigh.Vector128.Int32"] = MultiplyDoublingSaturateHigh_Vector128_Int32,
            };
        }
    }
}
