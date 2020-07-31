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
                ["MinPairwise.Vector64.Byte"] = MinPairwise_Vector64_Byte,
                ["MinPairwise.Vector64.Int16"] = MinPairwise_Vector64_Int16,
                ["MinPairwise.Vector64.Int32"] = MinPairwise_Vector64_Int32,
                ["MinPairwise.Vector64.SByte"] = MinPairwise_Vector64_SByte,
                ["MinPairwise.Vector64.Single"] = MinPairwise_Vector64_Single,
                ["MinPairwise.Vector64.UInt16"] = MinPairwise_Vector64_UInt16,
                ["MinPairwise.Vector64.UInt32"] = MinPairwise_Vector64_UInt32,
            };
        }
    }
}
