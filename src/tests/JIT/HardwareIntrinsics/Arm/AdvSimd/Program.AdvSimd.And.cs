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
                ["And.Vector64.Byte"] = And_Vector64_Byte,
                ["And.Vector64.Double"] = And_Vector64_Double,
                ["And.Vector64.Int16"] = And_Vector64_Int16,
                ["And.Vector64.Int32"] = And_Vector64_Int32,
                ["And.Vector64.Int64"] = And_Vector64_Int64,
                ["And.Vector64.SByte"] = And_Vector64_SByte,
                ["And.Vector64.Single"] = And_Vector64_Single,
                ["And.Vector64.UInt16"] = And_Vector64_UInt16,
                ["And.Vector64.UInt32"] = And_Vector64_UInt32,
                ["And.Vector64.UInt64"] = And_Vector64_UInt64,
                ["And.Vector128.Byte"] = And_Vector128_Byte,
                ["And.Vector128.Double"] = And_Vector128_Double,
                ["And.Vector128.Int16"] = And_Vector128_Int16,
                ["And.Vector128.Int32"] = And_Vector128_Int32,
                ["And.Vector128.Int64"] = And_Vector128_Int64,
                ["And.Vector128.SByte"] = And_Vector128_SByte,
                ["And.Vector128.Single"] = And_Vector128_Single,
                ["And.Vector128.UInt16"] = And_Vector128_UInt16,
                ["And.Vector128.UInt32"] = And_Vector128_UInt32,
                ["And.Vector128.UInt64"] = And_Vector128_UInt64,
            };
        }
    }
}
