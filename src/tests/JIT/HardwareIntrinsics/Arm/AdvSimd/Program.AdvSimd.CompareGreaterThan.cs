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
                ["CompareGreaterThan.Vector64.Byte"] = CompareGreaterThan_Vector64_Byte,
                ["CompareGreaterThan.Vector64.Int16"] = CompareGreaterThan_Vector64_Int16,
                ["CompareGreaterThan.Vector64.Int32"] = CompareGreaterThan_Vector64_Int32,
                ["CompareGreaterThan.Vector64.SByte"] = CompareGreaterThan_Vector64_SByte,
                ["CompareGreaterThan.Vector64.Single"] = CompareGreaterThan_Vector64_Single,
                ["CompareGreaterThan.Vector64.UInt16"] = CompareGreaterThan_Vector64_UInt16,
                ["CompareGreaterThan.Vector64.UInt32"] = CompareGreaterThan_Vector64_UInt32,
                ["CompareGreaterThan.Vector128.Byte"] = CompareGreaterThan_Vector128_Byte,
                ["CompareGreaterThan.Vector128.Int16"] = CompareGreaterThan_Vector128_Int16,
                ["CompareGreaterThan.Vector128.Int32"] = CompareGreaterThan_Vector128_Int32,
                ["CompareGreaterThan.Vector128.SByte"] = CompareGreaterThan_Vector128_SByte,
                ["CompareGreaterThan.Vector128.Single"] = CompareGreaterThan_Vector128_Single,
                ["CompareGreaterThan.Vector128.UInt16"] = CompareGreaterThan_Vector128_UInt16,
                ["CompareGreaterThan.Vector128.UInt32"] = CompareGreaterThan_Vector128_UInt32,
            };
        }
    }
}
