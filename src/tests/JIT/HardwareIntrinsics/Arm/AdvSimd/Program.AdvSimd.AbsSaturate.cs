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
                ["AbsSaturate.Vector64.Int16"] = AbsSaturate_Vector64_Int16,
                ["AbsSaturate.Vector64.Int32"] = AbsSaturate_Vector64_Int32,
                ["AbsSaturate.Vector64.SByte"] = AbsSaturate_Vector64_SByte,
                ["AbsSaturate.Vector128.Int16"] = AbsSaturate_Vector128_Int16,
                ["AbsSaturate.Vector128.Int32"] = AbsSaturate_Vector128_Int32,
                ["AbsSaturate.Vector128.SByte"] = AbsSaturate_Vector128_SByte,
            };
        }
    }
}
