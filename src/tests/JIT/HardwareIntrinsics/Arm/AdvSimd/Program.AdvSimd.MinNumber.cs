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
                ["MinNumber.Vector64.Single"] = MinNumber_Vector64_Single,
                ["MinNumber.Vector128.Single"] = MinNumber_Vector128_Single,
            };
        }
    }
}
