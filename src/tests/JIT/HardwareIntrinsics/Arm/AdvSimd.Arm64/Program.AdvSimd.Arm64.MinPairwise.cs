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
                ["MinPairwise.Vector128.Byte"] = MinPairwise_Vector128_Byte,
                ["MinPairwise.Vector128.Double"] = MinPairwise_Vector128_Double,
                ["MinPairwise.Vector128.Int16"] = MinPairwise_Vector128_Int16,
                ["MinPairwise.Vector128.Int32"] = MinPairwise_Vector128_Int32,
                ["MinPairwise.Vector128.SByte"] = MinPairwise_Vector128_SByte,
                ["MinPairwise.Vector128.Single"] = MinPairwise_Vector128_Single,
                ["MinPairwise.Vector128.UInt16"] = MinPairwise_Vector128_UInt16,
                ["MinPairwise.Vector128.UInt32"] = MinPairwise_Vector128_UInt32,
            };
        }
    }
}
