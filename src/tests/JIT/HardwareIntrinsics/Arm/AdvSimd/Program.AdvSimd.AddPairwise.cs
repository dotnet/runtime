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
                ["AddPairwise.Vector64.Byte"] = AddPairwise_Vector64_Byte,
                ["AddPairwise.Vector64.Int16"] = AddPairwise_Vector64_Int16,
                ["AddPairwise.Vector64.Int32"] = AddPairwise_Vector64_Int32,
                ["AddPairwise.Vector64.SByte"] = AddPairwise_Vector64_SByte,
                ["AddPairwise.Vector64.Single"] = AddPairwise_Vector64_Single,
                ["AddPairwise.Vector64.UInt16"] = AddPairwise_Vector64_UInt16,
                ["AddPairwise.Vector64.UInt32"] = AddPairwise_Vector64_UInt32,
            };
        }
    }
}
