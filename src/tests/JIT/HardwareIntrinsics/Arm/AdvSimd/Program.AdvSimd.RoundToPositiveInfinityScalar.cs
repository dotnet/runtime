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
                ["RoundToPositiveInfinityScalar.Vector64.Double"] = RoundToPositiveInfinityScalar_Vector64_Double,
                ["RoundToPositiveInfinityScalar.Vector64.Single"] = RoundToPositiveInfinityScalar_Vector64_Single,
            };
        }
    }
}
