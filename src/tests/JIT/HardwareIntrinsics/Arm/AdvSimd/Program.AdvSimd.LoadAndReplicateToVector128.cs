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
                ["LoadAndReplicateToVector128.Byte"] = LoadAndReplicateToVector128_Byte,
                ["LoadAndReplicateToVector128.Int16"] = LoadAndReplicateToVector128_Int16,
                ["LoadAndReplicateToVector128.Int32"] = LoadAndReplicateToVector128_Int32,
                ["LoadAndReplicateToVector128.SByte"] = LoadAndReplicateToVector128_SByte,
                ["LoadAndReplicateToVector128.Single"] = LoadAndReplicateToVector128_Single,
                ["LoadAndReplicateToVector128.UInt16"] = LoadAndReplicateToVector128_UInt16,
                ["LoadAndReplicateToVector128.UInt32"] = LoadAndReplicateToVector128_UInt32,
            };
        }
    }
}
