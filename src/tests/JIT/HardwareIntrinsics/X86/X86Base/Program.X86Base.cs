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
                ["DivRem.Int32.Tuple3Op"] = DivRemInt32Tuple3Op,
                ["DivRem.UInt32.Tuple3Op"] = DivRemUInt32Tuple3Op,
                ["DivRem.nint.Tuple3Op"] = DivRemnintTuple3Op,
                ["DivRem.nuint.Tuple3Op"] = DivRemnuintTuple3Op,
            };
        }
    }
}
