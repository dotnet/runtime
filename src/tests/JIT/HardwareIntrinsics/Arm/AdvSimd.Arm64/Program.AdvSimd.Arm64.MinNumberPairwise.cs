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
                ["MinNumberPairwise.Vector64.Single"] = MinNumberPairwise_Vector64_Single,
                ["MinNumberPairwise.Vector128.Double"] = MinNumberPairwise_Vector128_Double,
                ["MinNumberPairwise.Vector128.Single"] = MinNumberPairwise_Vector128_Single,
            };
        }
    }
}
