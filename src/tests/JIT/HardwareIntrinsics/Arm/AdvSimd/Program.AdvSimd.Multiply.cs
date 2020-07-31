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
                ["Multiply.Vector64.Byte"] = Multiply_Vector64_Byte,
                ["Multiply.Vector64.Int16"] = Multiply_Vector64_Int16,
                ["Multiply.Vector64.Int32"] = Multiply_Vector64_Int32,
                ["Multiply.Vector64.SByte"] = Multiply_Vector64_SByte,
                ["Multiply.Vector64.Single"] = Multiply_Vector64_Single,
                ["Multiply.Vector64.UInt16"] = Multiply_Vector64_UInt16,
                ["Multiply.Vector64.UInt32"] = Multiply_Vector64_UInt32,
                ["Multiply.Vector128.Byte"] = Multiply_Vector128_Byte,
                ["Multiply.Vector128.Int16"] = Multiply_Vector128_Int16,
                ["Multiply.Vector128.Int32"] = Multiply_Vector128_Int32,
                ["Multiply.Vector128.SByte"] = Multiply_Vector128_SByte,
                ["Multiply.Vector128.Single"] = Multiply_Vector128_Single,
                ["Multiply.Vector128.UInt16"] = Multiply_Vector128_UInt16,
                ["Multiply.Vector128.UInt32"] = Multiply_Vector128_UInt32,
            };
        }
    }
}
