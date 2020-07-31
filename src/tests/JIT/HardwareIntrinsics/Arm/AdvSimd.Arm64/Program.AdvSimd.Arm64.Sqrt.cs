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
                ["Sqrt.Vector64.Single"] = Sqrt_Vector64_Single,
                ["Sqrt.Vector128.Double"] = Sqrt_Vector128_Double,
                ["Sqrt.Vector128.Single"] = Sqrt_Vector128_Single,
            };
        }
    }
}
