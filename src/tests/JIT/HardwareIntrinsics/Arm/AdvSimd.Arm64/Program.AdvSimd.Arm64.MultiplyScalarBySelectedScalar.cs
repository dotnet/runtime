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
                ["MultiplyScalarBySelectedScalar.Vector64.Double.Vector128.Double.1"] = MultiplyScalarBySelectedScalar_Vector64_Double_Vector128_Double_1,
            };
        }
    }
}
