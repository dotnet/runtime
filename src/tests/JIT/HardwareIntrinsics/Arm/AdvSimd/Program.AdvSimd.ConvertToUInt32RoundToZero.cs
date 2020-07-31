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
                ["ConvertToUInt32RoundToZero.Vector64.Single"] = ConvertToUInt32RoundToZero_Vector64_Single,
                ["ConvertToUInt32RoundToZero.Vector128.Single"] = ConvertToUInt32RoundToZero_Vector128_Single,
            };
        }
    }
}
