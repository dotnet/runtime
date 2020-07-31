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
                ["MaxNumberScalar.Vector64.Double"] = MaxNumberScalar_Vector64_Double,
                ["MaxNumberScalar.Vector64.Single"] = MaxNumberScalar_Vector64_Single,
            };
        }
    }
}
