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
                ["ConvertToInt32RoundAwayFromZero.Vector64.Single"] = ConvertToInt32RoundAwayFromZero_Vector64_Single,
                ["ConvertToInt32RoundAwayFromZero.Vector128.Single"] = ConvertToInt32RoundAwayFromZero_Vector128_Single,
            };
        }
    }
}
