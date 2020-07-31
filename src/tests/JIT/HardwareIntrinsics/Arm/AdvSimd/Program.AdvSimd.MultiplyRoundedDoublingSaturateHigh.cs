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
                ["MultiplyRoundedDoublingSaturateHigh.Vector64.Int16"] = MultiplyRoundedDoublingSaturateHigh_Vector64_Int16,
                ["MultiplyRoundedDoublingSaturateHigh.Vector64.Int32"] = MultiplyRoundedDoublingSaturateHigh_Vector64_Int32,
                ["MultiplyRoundedDoublingSaturateHigh.Vector128.Int16"] = MultiplyRoundedDoublingSaturateHigh_Vector128_Int16,
                ["MultiplyRoundedDoublingSaturateHigh.Vector128.Int32"] = MultiplyRoundedDoublingSaturateHigh_Vector128_Int32,
            };
        }
    }
}
