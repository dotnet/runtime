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
                ["AddPairwise.Vector128.Byte"] = AddPairwise_Vector128_Byte,
                ["AddPairwise.Vector128.Double"] = AddPairwise_Vector128_Double,
                ["AddPairwise.Vector128.Int16"] = AddPairwise_Vector128_Int16,
                ["AddPairwise.Vector128.Int32"] = AddPairwise_Vector128_Int32,
                ["AddPairwise.Vector128.Int64"] = AddPairwise_Vector128_Int64,
                ["AddPairwise.Vector128.SByte"] = AddPairwise_Vector128_SByte,
                ["AddPairwise.Vector128.Single"] = AddPairwise_Vector128_Single,
                ["AddPairwise.Vector128.UInt16"] = AddPairwise_Vector128_UInt16,
                ["AddPairwise.Vector128.UInt32"] = AddPairwise_Vector128_UInt32,
                ["AddPairwise.Vector128.UInt64"] = AddPairwise_Vector128_UInt64,
            };
        }
    }
}
