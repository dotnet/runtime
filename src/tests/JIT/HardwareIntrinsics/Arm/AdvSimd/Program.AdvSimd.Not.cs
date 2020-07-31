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
                ["Not.Vector64.Byte"] = Not_Vector64_Byte,
                ["Not.Vector64.Double"] = Not_Vector64_Double,
                ["Not.Vector64.Int16"] = Not_Vector64_Int16,
                ["Not.Vector64.Int32"] = Not_Vector64_Int32,
                ["Not.Vector64.Int64"] = Not_Vector64_Int64,
                ["Not.Vector64.SByte"] = Not_Vector64_SByte,
                ["Not.Vector64.Single"] = Not_Vector64_Single,
                ["Not.Vector64.UInt16"] = Not_Vector64_UInt16,
                ["Not.Vector64.UInt32"] = Not_Vector64_UInt32,
                ["Not.Vector64.UInt64"] = Not_Vector64_UInt64,
                ["Not.Vector128.Byte"] = Not_Vector128_Byte,
                ["Not.Vector128.Double"] = Not_Vector128_Double,
                ["Not.Vector128.Int16"] = Not_Vector128_Int16,
                ["Not.Vector128.Int32"] = Not_Vector128_Int32,
                ["Not.Vector128.Int64"] = Not_Vector128_Int64,
                ["Not.Vector128.SByte"] = Not_Vector128_SByte,
                ["Not.Vector128.Single"] = Not_Vector128_Single,
                ["Not.Vector128.UInt16"] = Not_Vector128_UInt16,
                ["Not.Vector128.UInt32"] = Not_Vector128_UInt32,
                ["Not.Vector128.UInt64"] = Not_Vector128_UInt64,
            };
        }
    }
}
