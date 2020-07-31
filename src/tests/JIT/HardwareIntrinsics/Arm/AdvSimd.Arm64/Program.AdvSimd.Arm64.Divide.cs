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
                ["Divide.Vector64.Single"] = Divide_Vector64_Single,
                ["Divide.Vector128.Double"] = Divide_Vector128_Double,
                ["Divide.Vector128.Single"] = Divide_Vector128_Single,
            };
        }
    }
}
