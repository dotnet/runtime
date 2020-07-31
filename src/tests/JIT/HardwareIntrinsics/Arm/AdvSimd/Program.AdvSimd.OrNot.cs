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
                ["OrNot.Vector64.Byte"] = OrNot_Vector64_Byte,
                ["OrNot.Vector64.Double"] = OrNot_Vector64_Double,
                ["OrNot.Vector64.Int16"] = OrNot_Vector64_Int16,
                ["OrNot.Vector64.Int32"] = OrNot_Vector64_Int32,
                ["OrNot.Vector64.Int64"] = OrNot_Vector64_Int64,
                ["OrNot.Vector64.SByte"] = OrNot_Vector64_SByte,
                ["OrNot.Vector64.Single"] = OrNot_Vector64_Single,
                ["OrNot.Vector64.UInt16"] = OrNot_Vector64_UInt16,
                ["OrNot.Vector64.UInt32"] = OrNot_Vector64_UInt32,
                ["OrNot.Vector64.UInt64"] = OrNot_Vector64_UInt64,
                ["OrNot.Vector128.Byte"] = OrNot_Vector128_Byte,
                ["OrNot.Vector128.Double"] = OrNot_Vector128_Double,
                ["OrNot.Vector128.Int16"] = OrNot_Vector128_Int16,
                ["OrNot.Vector128.Int32"] = OrNot_Vector128_Int32,
                ["OrNot.Vector128.Int64"] = OrNot_Vector128_Int64,
                ["OrNot.Vector128.SByte"] = OrNot_Vector128_SByte,
                ["OrNot.Vector128.Single"] = OrNot_Vector128_Single,
                ["OrNot.Vector128.UInt16"] = OrNot_Vector128_UInt16,
                ["OrNot.Vector128.UInt32"] = OrNot_Vector128_UInt32,
                ["OrNot.Vector128.UInt64"] = OrNot_Vector128_UInt64,
            };
        }
    }
}
