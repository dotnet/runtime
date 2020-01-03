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
                ["AddAcross.Vector64.Byte"] = AddAcross_Vector64_Byte,
                ["AddAcross.Vector64.SByte"] = AddAcross_Vector64_SByte,
                ["AddAcross.Vector64.UInt16"] = AddAcross_Vector64_UInt16,
                ["AddAcross.Vector64.Int16"] = AddAcross_Vector64_Int16,
                ["AddAcross.Vector128.Byte"] = AddAcross_Vector128_Byte,
                ["AddAcross.Vector128.SByte"] = AddAcross_Vector128_SByte,
                ["AddAcross.Vector128.UInt16"] = AddAcross_Vector128_UInt16,
                ["AddAcross.Vector128.Int16"] = AddAcross_Vector128_Int16,
                ["AddAcross.Vector128.UInt32"] = AddAcross_Vector128_UInt32,
                ["AddAcross.Vector128.Int32"] = AddAcross_Vector128_Int32,
                ["CompareEqual.Vector128.Double"] = CompareEqual_Vector128_Double,
                ["CompareEqual.Vector128.Int64"] = CompareEqual_Vector128_Int64,
                ["CompareEqual.Vector128.UInt64"] = CompareEqual_Vector128_UInt64,
                ["CompareGreaterThan.Vector128.Double"] = CompareGreaterThan_Vector128_Double,
                ["CompareGreaterThan.Vector128.Int64"] = CompareGreaterThan_Vector128_Int64,
                ["CompareGreaterThan.Vector128.UInt64"] = CompareGreaterThan_Vector128_UInt64,
                ["CompareGreaterThanOrEqual.Vector128.Double"] = CompareGreaterThanOrEqual_Vector128_Double,
                ["CompareGreaterThanOrEqual.Vector128.Int64"] = CompareGreaterThanOrEqual_Vector128_Int64,
                ["CompareGreaterThanOrEqual.Vector128.UInt64"] = CompareGreaterThanOrEqual_Vector128_UInt64,
                ["CompareLessThan.Vector128.Double"] = CompareLessThan_Vector128_Double,
                ["CompareLessThan.Vector128.Int64"] = CompareLessThan_Vector128_Int64,
                ["CompareLessThan.Vector128.UInt64"] = CompareLessThan_Vector128_UInt64,
                ["CompareLessThanOrEqual.Vector128.Double"] = CompareLessThanOrEqual_Vector128_Double,
                ["CompareLessThanOrEqual.Vector128.Int64"] = CompareLessThanOrEqual_Vector128_Int64,
                ["CompareLessThanOrEqual.Vector128.UInt64"] = CompareLessThanOrEqual_Vector128_UInt64,
                ["CompareTest.Vector128.Double"] = CompareTest_Vector128_Double,
                ["CompareTest.Vector128.Int64"] = CompareTest_Vector128_Int64,
                ["CompareTest.Vector128.UInt64"] = CompareTest_Vector128_UInt64,
                ["ReverseElementBits.Vector128.Byte"] = ReverseElementBits_Vector128_Byte,
                ["ReverseElementBits.Vector128.SByte"] = ReverseElementBits_Vector128_SByte,
                ["ReverseElementBits.Vector64.Byte"] = ReverseElementBits_Vector64_Byte,
                ["ReverseElementBits.Vector64.SByte"] = ReverseElementBits_Vector64_SByte,
            };
        }
    }
}
