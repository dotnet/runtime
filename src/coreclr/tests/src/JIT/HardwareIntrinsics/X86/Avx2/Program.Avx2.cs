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
                ["Add.Byte"] = AddByte,
                ["Add.Int16"] = AddInt16,
                ["Add.Int32"] = AddInt32,
                ["Add.Int64"] = AddInt64,
                ["Add.SByte"] = AddSByte,
                ["Add.UInt16"] = AddUInt16,
                ["Add.UInt32"] = AddUInt32,
                ["Add.UInt64"] = AddUInt64,
                ["And.Byte"] = AndByte,
                ["And.Int16"] = AndInt16,
                ["And.Int32"] = AndInt32,
                ["And.Int64"] = AndInt64,
                ["And.SByte"] = AndSByte,
                ["And.UInt16"] = AndUInt16,
                ["And.UInt32"] = AndUInt32,
                ["And.UInt64"] = AndUInt64,
                ["AndNot.Byte"] = AndNotByte,
                ["AndNot.Int16"] = AndNotInt16,
                ["AndNot.Int32"] = AndNotInt32,
                ["AndNot.Int64"] = AndNotInt64,
                ["AndNot.SByte"] = AndNotSByte,
                ["AndNot.UInt16"] = AndNotUInt16,
                ["AndNot.UInt32"] = AndNotUInt32,
                ["AndNot.UInt64"] = AndNotUInt64,
                ["Average.Byte"] = AverageByte,
                ["Average.UInt16"] = AverageUInt16,
                ["CompareEqual.Byte"] = CompareEqualByte,
                ["CompareEqual.Int16"] = CompareEqualInt16,
                ["CompareEqual.Int32"] = CompareEqualInt32,
                ["CompareEqual.Int64"] = CompareEqualInt64,
                ["CompareEqual.SByte"] = CompareEqualSByte,
                ["CompareEqual.UInt16"] = CompareEqualUInt16,
                ["CompareEqual.UInt32"] = CompareEqualUInt32,
                ["CompareEqual.UInt64"] = CompareEqualUInt64,
                ["CompareGreaterThan.Int16"] = CompareGreaterThanInt16,
                ["CompareGreaterThan.Int32"] = CompareGreaterThanInt32,
                ["CompareGreaterThan.Int64"] = CompareGreaterThanInt64,
                ["CompareGreaterThan.SByte"] = CompareGreaterThanSByte,
                ["Or.Byte"] = OrByte,
                ["Or.Int16"] = OrInt16,
                ["Or.Int32"] = OrInt32,
                ["Or.Int64"] = OrInt64,
                ["Or.SByte"] = OrSByte,
                ["Or.UInt16"] = OrUInt16,
                ["Or.UInt32"] = OrUInt32,
                ["Or.UInt64"] = OrUInt64,
                ["Subtract.Byte"] = SubtractByte,
                ["Subtract.Int16"] = SubtractInt16,
                ["Subtract.Int32"] = SubtractInt32,
                ["Subtract.Int64"] = SubtractInt64,
                ["Subtract.SByte"] = SubtractSByte,
                ["Subtract.UInt16"] = SubtractUInt16,
                ["Subtract.UInt32"] = SubtractUInt32,
                ["Subtract.UInt64"] = SubtractUInt64,
                ["Xor.Byte"] = XorByte,
                ["Xor.Int16"] = XorInt16,
                ["Xor.Int32"] = XorInt32,
                ["Xor.Int64"] = XorInt64,
                ["Xor.SByte"] = XorSByte,
                ["Xor.UInt16"] = XorUInt16,
                ["Xor.UInt32"] = XorUInt32,
                ["Xor.UInt64"] = XorUInt64,
            };
        }
    }
}
