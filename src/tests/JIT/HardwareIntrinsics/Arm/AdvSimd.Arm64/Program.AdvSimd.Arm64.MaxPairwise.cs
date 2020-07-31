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
                ["MaxPairwise.Vector128.Byte"] = MaxPairwise_Vector128_Byte,
                ["MaxPairwise.Vector128.Double"] = MaxPairwise_Vector128_Double,
                ["MaxPairwise.Vector128.Int16"] = MaxPairwise_Vector128_Int16,
                ["MaxPairwise.Vector128.Int32"] = MaxPairwise_Vector128_Int32,
                ["MaxPairwise.Vector128.SByte"] = MaxPairwise_Vector128_SByte,
                ["MaxPairwise.Vector128.Single"] = MaxPairwise_Vector128_Single,
                ["MaxPairwise.Vector128.UInt16"] = MaxPairwise_Vector128_UInt16,
                ["MaxPairwise.Vector128.UInt32"] = MaxPairwise_Vector128_UInt32,
            };
        }
    }
}
