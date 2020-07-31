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
                ["Abs.Vector128.Double"] = Abs_Vector128_Double,
                ["Abs.Vector128.Int64"] = Abs_Vector128_Int64,
            };
        }
    }
}
