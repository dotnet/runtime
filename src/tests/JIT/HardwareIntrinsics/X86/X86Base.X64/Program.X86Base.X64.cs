// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["DivRem.Int64.Tuple3Op"] = DivRemInt64Tuple3Op,
                ["DivRem.UInt64.Tuple3Op"] = DivRemUInt64Tuple3Op,
            };
        }
    }
}
