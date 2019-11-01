// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["CarrylessMultiply.UInt64.0"] = CarrylessMultiplyUInt640,
                ["CarrylessMultiply.UInt64.1"] = CarrylessMultiplyUInt641,
                ["CarrylessMultiply.UInt64.16"] = CarrylessMultiplyUInt6416,
                ["CarrylessMultiply.UInt64.17"] = CarrylessMultiplyUInt6417,
                ["CarrylessMultiply.UInt64.129"] = CarrylessMultiplyUInt64129,
                ["CarrylessMultiply.Int64.0"] = CarrylessMultiplyInt640,
                ["CarrylessMultiply.Int64.1"] = CarrylessMultiplyInt641,
                ["CarrylessMultiply.Int64.16"] = CarrylessMultiplyInt6416,
                ["CarrylessMultiply.Int64.17"] = CarrylessMultiplyInt6417,
                ["CarrylessMultiply.Int64.129"] = CarrylessMultiplyInt64129,
            };
        }
    }
}
