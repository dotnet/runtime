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
                ["CompareEqual.Vector64.Byte"] = CompareEqual_Vector64_Byte,
                ["CompareEqual.Vector64.Int16"] = CompareEqual_Vector64_Int16,
                ["CompareEqual.Vector64.Int32"] = CompareEqual_Vector64_Int32,
                ["CompareEqual.Vector64.SByte"] = CompareEqual_Vector64_SByte,
                ["CompareEqual.Vector64.Single"] = CompareEqual_Vector64_Single,
                ["CompareEqual.Vector64.UInt16"] = CompareEqual_Vector64_UInt16,
                ["CompareEqual.Vector64.UInt32"] = CompareEqual_Vector64_UInt32,
                ["CompareEqual.Vector128.Byte"] = CompareEqual_Vector128_Byte,
                ["CompareEqual.Vector128.Int16"] = CompareEqual_Vector128_Int16,
                ["CompareEqual.Vector128.Int32"] = CompareEqual_Vector128_Int32,
                ["CompareEqual.Vector128.SByte"] = CompareEqual_Vector128_SByte,
                ["CompareEqual.Vector128.Single"] = CompareEqual_Vector128_Single,
                ["CompareEqual.Vector128.UInt16"] = CompareEqual_Vector128_UInt16,
                ["CompareEqual.Vector128.UInt32"] = CompareEqual_Vector128_UInt32,
            };
        }
    }
}
