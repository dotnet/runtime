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
                ["FusedMultiplySubtract.Vector64.Single"] = FusedMultiplySubtract_Vector64_Single,
                ["FusedMultiplySubtract.Vector128.Single"] = FusedMultiplySubtract_Vector128_Single,
            };
        }
    }
}
