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
                ["Abs.Vector64.Int16"] = Abs_Vector64_Int16,
                ["Abs.Vector64.Int32"] = Abs_Vector64_Int32,
                ["Abs.Vector64.SByte"] = Abs_Vector64_SByte,
                ["Abs.Vector64.Single"] = Abs_Vector64_Single,
                ["Abs.Vector128.Int16"] = Abs_Vector128_Int16,
                ["Abs.Vector128.Int32"] = Abs_Vector128_Int32,
                ["Abs.Vector128.SByte"] = Abs_Vector128_SByte,
                ["Abs.Vector128.Single"] = Abs_Vector128_Single,
            };
        }
    }
}
