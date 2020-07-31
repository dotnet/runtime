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
                ["FusedMultiplySubtractScalar.Vector64.Double"] = FusedMultiplySubtractScalar_Vector64_Double,
                ["FusedMultiplySubtractScalar.Vector64.Single"] = FusedMultiplySubtractScalar_Vector64_Single,
            };
        }
    }
}
