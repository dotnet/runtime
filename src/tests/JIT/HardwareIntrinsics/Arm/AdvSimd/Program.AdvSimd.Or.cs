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
                ["Or.Vector64.Byte"] = Or_Vector64_Byte,
                ["Or.Vector64.Double"] = Or_Vector64_Double,
                ["Or.Vector64.Int16"] = Or_Vector64_Int16,
                ["Or.Vector64.Int32"] = Or_Vector64_Int32,
                ["Or.Vector64.Int64"] = Or_Vector64_Int64,
                ["Or.Vector64.SByte"] = Or_Vector64_SByte,
                ["Or.Vector64.Single"] = Or_Vector64_Single,
                ["Or.Vector64.UInt16"] = Or_Vector64_UInt16,
                ["Or.Vector64.UInt32"] = Or_Vector64_UInt32,
                ["Or.Vector64.UInt64"] = Or_Vector64_UInt64,
                ["Or.Vector128.Byte"] = Or_Vector128_Byte,
                ["Or.Vector128.Double"] = Or_Vector128_Double,
                ["Or.Vector128.Int16"] = Or_Vector128_Int16,
                ["Or.Vector128.Int32"] = Or_Vector128_Int32,
                ["Or.Vector128.Int64"] = Or_Vector128_Int64,
                ["Or.Vector128.SByte"] = Or_Vector128_SByte,
                ["Or.Vector128.Single"] = Or_Vector128_Single,
                ["Or.Vector128.UInt16"] = Or_Vector128_UInt16,
                ["Or.Vector128.UInt32"] = Or_Vector128_UInt32,
                ["Or.Vector128.UInt64"] = Or_Vector128_UInt64,
            };
        }
    }
}
