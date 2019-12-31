// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Abs.Vector128.Double"] = Abs_Vector128_Double,
                ["Abs.Vector128.UInt64"] = Abs_Vector128_UInt64,
                ["AbsoluteCompareGreaterThan.Vector128.Double"] = AbsoluteCompareGreaterThan_Vector128_Double,
                ["AbsoluteCompareGreaterThanOrEqual.Vector128.Double"] = AbsoluteCompareGreaterThanOrEqual_Vector128_Double,
                ["AbsoluteCompareLessThan.Vector128.Double"] = AbsoluteCompareLessThan_Vector128_Double,
                ["AbsoluteCompareLessThanOrEqual.Vector128.Double"] = AbsoluteCompareLessThanOrEqual_Vector128_Double,
                ["ReverseElementBits.Vector128.Byte"] = ReverseElementBits_Vector128_Byte,
                ["ReverseElementBits.Vector128.SByte"] = ReverseElementBits_Vector128_SByte,
                ["ReverseElementBits.Vector64.Byte"] = ReverseElementBits_Vector64_Byte,
                ["ReverseElementBits.Vector64.SByte"] = ReverseElementBits_Vector64_SByte,
            };
        }
    }
}
