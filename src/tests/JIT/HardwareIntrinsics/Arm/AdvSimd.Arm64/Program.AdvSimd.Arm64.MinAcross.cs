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
                ["MinAcross.Vector64.Byte"] = MinAcross_Vector64_Byte,
                ["MinAcross.Vector64.Int16"] = MinAcross_Vector64_Int16,
                ["MinAcross.Vector64.SByte"] = MinAcross_Vector64_SByte,
                ["MinAcross.Vector64.UInt16"] = MinAcross_Vector64_UInt16,
                ["MinAcross.Vector128.Byte"] = MinAcross_Vector128_Byte,
                ["MinAcross.Vector128.Int16"] = MinAcross_Vector128_Int16,
                ["MinAcross.Vector128.Int32"] = MinAcross_Vector128_Int32,
                ["MinAcross.Vector128.SByte"] = MinAcross_Vector128_SByte,
                ["MinAcross.Vector128.Single"] = MinAcross_Vector128_Single,
                ["MinAcross.Vector128.UInt16"] = MinAcross_Vector128_UInt16,
                ["MinAcross.Vector128.UInt32"] = MinAcross_Vector128_UInt32,
            };
        }
    }
}
