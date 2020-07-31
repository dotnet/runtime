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
                ["PolynomialMultiply.Vector64.Byte"] = PolynomialMultiply_Vector64_Byte,
                ["PolynomialMultiply.Vector64.SByte"] = PolynomialMultiply_Vector64_SByte,
                ["PolynomialMultiply.Vector128.Byte"] = PolynomialMultiply_Vector128_Byte,
                ["PolynomialMultiply.Vector128.SByte"] = PolynomialMultiply_Vector128_SByte,
            };
        }
    }
}
