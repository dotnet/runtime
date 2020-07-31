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
                ["NegateScalar.Vector64.Double"] = NegateScalar_Vector64_Double,
                ["NegateScalar.Vector64.Single"] = NegateScalar_Vector64_Single,
            };
        }
    }
}
