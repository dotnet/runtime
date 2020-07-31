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
                ["MaxPairwise.Vector64.Byte"] = MaxPairwise_Vector64_Byte,
                ["MaxPairwise.Vector64.Int16"] = MaxPairwise_Vector64_Int16,
                ["MaxPairwise.Vector64.Int32"] = MaxPairwise_Vector64_Int32,
                ["MaxPairwise.Vector64.SByte"] = MaxPairwise_Vector64_SByte,
                ["MaxPairwise.Vector64.Single"] = MaxPairwise_Vector64_Single,
                ["MaxPairwise.Vector64.UInt16"] = MaxPairwise_Vector64_UInt16,
                ["MaxPairwise.Vector64.UInt32"] = MaxPairwise_Vector64_UInt32,
            };
        }
    }
}
