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
                ["CompareLessThan.Vector128.Double"] = CompareLessThan_Vector128_Double,
                ["CompareLessThan.Vector128.Int64"] = CompareLessThan_Vector128_Int64,
                ["CompareLessThan.Vector128.UInt64"] = CompareLessThan_Vector128_UInt64,
            };
        }
    }
}
