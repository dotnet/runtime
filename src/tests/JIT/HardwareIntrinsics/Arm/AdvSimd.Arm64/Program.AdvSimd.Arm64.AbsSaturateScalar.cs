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
                ["AbsSaturateScalar.Vector64.Int16"] = AbsSaturateScalar_Vector64_Int16,
                ["AbsSaturateScalar.Vector64.Int32"] = AbsSaturateScalar_Vector64_Int32,
                ["AbsSaturateScalar.Vector64.Int64"] = AbsSaturateScalar_Vector64_Int64,
                ["AbsSaturateScalar.Vector64.SByte"] = AbsSaturateScalar_Vector64_SByte,
            };
        }
    }
}
