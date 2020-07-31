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
                ["FloorScalar.Vector64.Double"] = FloorScalar_Vector64_Double,
                ["FloorScalar.Vector64.Single"] = FloorScalar_Vector64_Single,
            };
        }
    }
}
