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
                ["LoadAndReplicateToVector64.Byte"] = LoadAndReplicateToVector64_Byte,
                ["LoadAndReplicateToVector64.Int16"] = LoadAndReplicateToVector64_Int16,
                ["LoadAndReplicateToVector64.Int32"] = LoadAndReplicateToVector64_Int32,
                ["LoadAndReplicateToVector64.SByte"] = LoadAndReplicateToVector64_SByte,
                ["LoadAndReplicateToVector64.Single"] = LoadAndReplicateToVector64_Single,
                ["LoadAndReplicateToVector64.UInt16"] = LoadAndReplicateToVector64_UInt16,
                ["LoadAndReplicateToVector64.UInt32"] = LoadAndReplicateToVector64_UInt32,
            };
        }
    }
}
