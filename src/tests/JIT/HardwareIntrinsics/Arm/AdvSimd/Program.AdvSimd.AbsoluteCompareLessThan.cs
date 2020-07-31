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
                ["AbsoluteCompareLessThan.Vector64.Single"] = AbsoluteCompareLessThan_Vector64_Single,
                ["AbsoluteCompareLessThan.Vector128.Single"] = AbsoluteCompareLessThan_Vector128_Single,
            };
        }
    }
}
