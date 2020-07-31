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
                ["RoundToZeroScalar.Vector64.Double"] = RoundToZeroScalar_Vector64_Double,
                ["RoundToZeroScalar.Vector64.Single"] = RoundToZeroScalar_Vector64_Single,
            };
        }
    }
}
