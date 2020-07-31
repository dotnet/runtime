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
                ["MinNumberScalar.Vector64.Double"] = MinNumberScalar_Vector64_Double,
                ["MinNumberScalar.Vector64.Single"] = MinNumberScalar_Vector64_Single,
            };
        }
    }
}
