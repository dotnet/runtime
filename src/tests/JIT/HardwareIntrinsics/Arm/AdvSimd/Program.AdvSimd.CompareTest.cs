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
                ["CompareTest.Vector64.Byte"] = CompareTest_Vector64_Byte,
                ["CompareTest.Vector64.Int16"] = CompareTest_Vector64_Int16,
                ["CompareTest.Vector64.Int32"] = CompareTest_Vector64_Int32,
                ["CompareTest.Vector64.SByte"] = CompareTest_Vector64_SByte,
                ["CompareTest.Vector64.Single"] = CompareTest_Vector64_Single,
                ["CompareTest.Vector64.UInt16"] = CompareTest_Vector64_UInt16,
                ["CompareTest.Vector64.UInt32"] = CompareTest_Vector64_UInt32,
                ["CompareTest.Vector128.Byte"] = CompareTest_Vector128_Byte,
                ["CompareTest.Vector128.Int16"] = CompareTest_Vector128_Int16,
                ["CompareTest.Vector128.Int32"] = CompareTest_Vector128_Int32,
                ["CompareTest.Vector128.SByte"] = CompareTest_Vector128_SByte,
                ["CompareTest.Vector128.Single"] = CompareTest_Vector128_Single,
                ["CompareTest.Vector128.UInt16"] = CompareTest_Vector128_UInt16,
                ["CompareTest.Vector128.UInt32"] = CompareTest_Vector128_UInt32,
            };
        }
    }
}
