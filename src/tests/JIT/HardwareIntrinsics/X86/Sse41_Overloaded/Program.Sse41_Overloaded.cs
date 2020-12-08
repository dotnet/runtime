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
                ["RoundCurrentDirectionScalar.Double"] = RoundCurrentDirectionScalarDouble,
                ["RoundCurrentDirectionScalar.Single"] = RoundCurrentDirectionScalarSingle,
                ["RoundToNearestIntegerScalar.Double"] = RoundToNearestIntegerScalarDouble,
                ["RoundToNearestIntegerScalar.Single"] = RoundToNearestIntegerScalarSingle,
                ["RoundToNegativeInfinityScalar.Double"] = RoundToNegativeInfinityScalarDouble,
                ["RoundToNegativeInfinityScalar.Single"] = RoundToNegativeInfinityScalarSingle,
                ["RoundToPositiveInfinityScalar.Double"] = RoundToPositiveInfinityScalarDouble,
                ["RoundToPositiveInfinityScalar.Single"] = RoundToPositiveInfinityScalarSingle,
            };
        }
    }
}
