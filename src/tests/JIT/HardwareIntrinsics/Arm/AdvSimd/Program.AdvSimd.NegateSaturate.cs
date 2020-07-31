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
                ["NegateSaturate.Vector64.Int16"] = NegateSaturate_Vector64_Int16,
                ["NegateSaturate.Vector64.Int32"] = NegateSaturate_Vector64_Int32,
                ["NegateSaturate.Vector64.SByte"] = NegateSaturate_Vector64_SByte,
                ["NegateSaturate.Vector128.Int16"] = NegateSaturate_Vector128_Int16,
                ["NegateSaturate.Vector128.Int32"] = NegateSaturate_Vector128_Int32,
                ["NegateSaturate.Vector128.SByte"] = NegateSaturate_Vector128_SByte,
            };
        }
    }
}
