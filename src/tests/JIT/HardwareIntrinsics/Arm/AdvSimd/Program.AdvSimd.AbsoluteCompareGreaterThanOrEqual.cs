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
                ["AbsoluteCompareGreaterThanOrEqual.Vector64.Single"] = AbsoluteCompareGreaterThanOrEqual_Vector64_Single,
                ["AbsoluteCompareGreaterThanOrEqual.Vector128.Single"] = AbsoluteCompareGreaterThanOrEqual_Vector128_Single,
            };
        }
    }
}
