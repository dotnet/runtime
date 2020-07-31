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
                ["CompareGreaterThanOrEqual.Vector64.Byte"] = CompareGreaterThanOrEqual_Vector64_Byte,
                ["CompareGreaterThanOrEqual.Vector64.Int16"] = CompareGreaterThanOrEqual_Vector64_Int16,
                ["CompareGreaterThanOrEqual.Vector64.Int32"] = CompareGreaterThanOrEqual_Vector64_Int32,
                ["CompareGreaterThanOrEqual.Vector64.SByte"] = CompareGreaterThanOrEqual_Vector64_SByte,
                ["CompareGreaterThanOrEqual.Vector64.Single"] = CompareGreaterThanOrEqual_Vector64_Single,
                ["CompareGreaterThanOrEqual.Vector64.UInt16"] = CompareGreaterThanOrEqual_Vector64_UInt16,
                ["CompareGreaterThanOrEqual.Vector64.UInt32"] = CompareGreaterThanOrEqual_Vector64_UInt32,
                ["CompareGreaterThanOrEqual.Vector128.Byte"] = CompareGreaterThanOrEqual_Vector128_Byte,
                ["CompareGreaterThanOrEqual.Vector128.Int16"] = CompareGreaterThanOrEqual_Vector128_Int16,
                ["CompareGreaterThanOrEqual.Vector128.Int32"] = CompareGreaterThanOrEqual_Vector128_Int32,
                ["CompareGreaterThanOrEqual.Vector128.SByte"] = CompareGreaterThanOrEqual_Vector128_SByte,
                ["CompareGreaterThanOrEqual.Vector128.Single"] = CompareGreaterThanOrEqual_Vector128_Single,
                ["CompareGreaterThanOrEqual.Vector128.UInt16"] = CompareGreaterThanOrEqual_Vector128_UInt16,
                ["CompareGreaterThanOrEqual.Vector128.UInt32"] = CompareGreaterThanOrEqual_Vector128_UInt32,
            };
        }
    }
}
