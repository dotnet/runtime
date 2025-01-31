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
                ["Permute2x128.Int32.2"] = Permute2x128Int322,
                ["Permute2x128.UInt32.2"] = Permute2x128UInt322,
                ["Permute2x128.Int64.2"] = Permute2x128Int642,
                ["Permute2x128.UInt64.2"] = Permute2x128UInt642,
            };
        }
    }
}
