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
                ["MaxNumberPairwise.Vector64.Single"] = MaxNumberPairwise_Vector64_Single,
                ["MaxNumberPairwise.Vector128.Double"] = MaxNumberPairwise_Vector128_Double,
                ["MaxNumberPairwise.Vector128.Single"] = MaxNumberPairwise_Vector128_Single,
            };
        }
    }
}
