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
                ["MaxNumberPairwiseScalar.Vector64.Single"] = MaxNumberPairwiseScalar_Vector64_Single,
                ["MaxNumberPairwiseScalar.Vector128.Double"] = MaxNumberPairwiseScalar_Vector128_Double,
            };
        }
    }
}
