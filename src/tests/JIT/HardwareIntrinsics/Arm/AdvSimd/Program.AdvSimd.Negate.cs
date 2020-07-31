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
                ["Negate.Vector64.Int16"] = Negate_Vector64_Int16,
                ["Negate.Vector64.Int32"] = Negate_Vector64_Int32,
                ["Negate.Vector64.SByte"] = Negate_Vector64_SByte,
                ["Negate.Vector64.Single"] = Negate_Vector64_Single,
                ["Negate.Vector128.Int16"] = Negate_Vector128_Int16,
                ["Negate.Vector128.Int32"] = Negate_Vector128_Int32,
                ["Negate.Vector128.SByte"] = Negate_Vector128_SByte,
                ["Negate.Vector128.Single"] = Negate_Vector128_Single,
            };
        }
    }
}
