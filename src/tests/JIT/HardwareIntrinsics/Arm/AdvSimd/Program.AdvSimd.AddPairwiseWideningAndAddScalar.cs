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
                ["AddPairwiseWideningAndAddScalar.Vector64.Int32"] = AddPairwiseWideningAndAddScalar_Vector64_Int32,
                ["AddPairwiseWideningAndAddScalar.Vector64.UInt32"] = AddPairwiseWideningAndAddScalar_Vector64_UInt32,
            };
        }
    }
}
