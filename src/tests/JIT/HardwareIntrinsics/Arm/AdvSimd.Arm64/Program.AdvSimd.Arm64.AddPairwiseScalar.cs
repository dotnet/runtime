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
                ["AddPairwiseScalar.Vector64.Single"] = AddPairwiseScalar_Vector64_Single,
                ["AddPairwiseScalar.Vector128.Double"] = AddPairwiseScalar_Vector128_Double,
                ["AddPairwiseScalar.Vector128.Int64"] = AddPairwiseScalar_Vector128_Int64,
                ["AddPairwiseScalar.Vector128.UInt64"] = AddPairwiseScalar_Vector128_UInt64,
            };
        }
    }
}
