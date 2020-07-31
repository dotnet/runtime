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
                ["NegateSaturateScalar.Vector64.Int16"] = NegateSaturateScalar_Vector64_Int16,
                ["NegateSaturateScalar.Vector64.Int32"] = NegateSaturateScalar_Vector64_Int32,
                ["NegateSaturateScalar.Vector64.Int64"] = NegateSaturateScalar_Vector64_Int64,
                ["NegateSaturateScalar.Vector64.SByte"] = NegateSaturateScalar_Vector64_SByte,
            };
        }
    }
}
