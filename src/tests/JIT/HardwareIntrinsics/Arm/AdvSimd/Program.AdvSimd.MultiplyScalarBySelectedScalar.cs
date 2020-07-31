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
                ["MultiplyScalarBySelectedScalar.Vector64.Single.Vector64.Single.1"] = MultiplyScalarBySelectedScalar_Vector64_Single_Vector64_Single_1,
                ["MultiplyScalarBySelectedScalar.Vector64.Single.Vector128.Single.3"] = MultiplyScalarBySelectedScalar_Vector64_Single_Vector128_Single_3,
            };
        }
    }
}
