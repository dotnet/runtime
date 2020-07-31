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
                ["LoadAndReplicateToVector128.Double"] = LoadAndReplicateToVector128_Double,
                ["LoadAndReplicateToVector128.Int64"] = LoadAndReplicateToVector128_Int64,
                ["LoadAndReplicateToVector128.UInt64"] = LoadAndReplicateToVector128_UInt64,
            };
        }
    }
}
