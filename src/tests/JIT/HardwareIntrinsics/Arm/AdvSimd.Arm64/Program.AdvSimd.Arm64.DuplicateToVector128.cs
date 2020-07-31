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
                ["DuplicateToVector128.Double"] = DuplicateToVector128_Double,
                ["DuplicateToVector128.Double.31"] = DuplicateToVector128_Double_31,
                ["DuplicateToVector128.Int64"] = DuplicateToVector128_Int64,
                ["DuplicateToVector128.Int64.31"] = DuplicateToVector128_Int64_31,
                ["DuplicateToVector128.UInt64"] = DuplicateToVector128_UInt64,
                ["DuplicateToVector128.UInt64.31"] = DuplicateToVector128_UInt64_31,
            };
        }
    }
}
