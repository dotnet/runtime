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
                ["Subtract.Vector64.Byte"] = Subtract_Vector64_Byte,
                ["Subtract.Vector64.Int16"] = Subtract_Vector64_Int16,
                ["Subtract.Vector64.Int32"] = Subtract_Vector64_Int32,
                ["Subtract.Vector64.SByte"] = Subtract_Vector64_SByte,
                ["Subtract.Vector64.Single"] = Subtract_Vector64_Single,
                ["Subtract.Vector64.UInt16"] = Subtract_Vector64_UInt16,
                ["Subtract.Vector64.UInt32"] = Subtract_Vector64_UInt32,
                ["Subtract.Vector128.Byte"] = Subtract_Vector128_Byte,
                ["Subtract.Vector128.Int16"] = Subtract_Vector128_Int16,
                ["Subtract.Vector128.Int32"] = Subtract_Vector128_Int32,
                ["Subtract.Vector128.Int64"] = Subtract_Vector128_Int64,
                ["Subtract.Vector128.SByte"] = Subtract_Vector128_SByte,
                ["Subtract.Vector128.Single"] = Subtract_Vector128_Single,
                ["Subtract.Vector128.UInt16"] = Subtract_Vector128_UInt16,
                ["Subtract.Vector128.UInt32"] = Subtract_Vector128_UInt32,
                ["Subtract.Vector128.UInt64"] = Subtract_Vector128_UInt64,
            };
        }
    }
}
