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
                ["StorePairScalar.Vector64.Int32"] = StorePairScalar_Vector64_Int32,
                ["StorePairScalar.Vector64.Single"] = StorePairScalar_Vector64_Single,
                ["StorePairScalar.Vector64.UInt32"] = StorePairScalar_Vector64_UInt32,
            };
        }
    }
}
