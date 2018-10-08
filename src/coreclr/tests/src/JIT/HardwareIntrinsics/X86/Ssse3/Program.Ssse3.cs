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
                ["Abs.Byte"] = AbsByte,
                ["Abs.UInt16"] = AbsUInt16,
                ["Abs.UInt32"] = AbsUInt32,
                ["AlignRight.Byte.0"] = AlignRightByte0,
                ["AlignRight.Byte.1"] = AlignRightByte1,
                ["AlignRight.SByte.0"] = AlignRightSByte0,
                ["AlignRight.SByte.1"] = AlignRightSByte1,
                ["AlignRight.Int16.0"] = AlignRightInt160,
                ["AlignRight.Int16.2"] = AlignRightInt162,
                ["AlignRight.UInt16.0"] = AlignRightUInt160,
                ["AlignRight.UInt16.2"] = AlignRightUInt162,
                ["AlignRight.Int32.0"] = AlignRightInt320,
                ["AlignRight.Int32.4"] = AlignRightInt324,
                ["AlignRight.UInt32.0"] = AlignRightUInt320,
                ["AlignRight.UInt32.4"] = AlignRightUInt324,
                ["AlignRight.Int64.0"] = AlignRightInt640,
                ["AlignRight.Int64.8"] = AlignRightInt648,
                ["AlignRight.UInt64.0"] = AlignRightUInt640,
                ["AlignRight.UInt64.8"] = AlignRightUInt648,
                ["HorizontalAdd.Int16"] = HorizontalAddInt16,
                ["HorizontalAdd.Int32"] = HorizontalAddInt32,
                ["HorizontalAddSaturate.Int16"] = HorizontalAddSaturateInt16,
                ["HorizontalSubtract.Int16"] = HorizontalSubtractInt16,
                ["HorizontalSubtract.Int32"] = HorizontalSubtractInt32,
                ["HorizontalSubtractSaturate.Int16"] = HorizontalSubtractSaturateInt16,
                ["MultiplyAddAdjacent.Int16"] = MultiplyAddAdjacentInt16,
                ["MultiplyHighRoundScale.Int16"] = MultiplyHighRoundScaleInt16,
                ["Shuffle.Byte"] = ShuffleByte,
                ["Shuffle.SByte"] = ShuffleSByte,
                ["Sign.SByte"] = SignSByte,
                ["Sign.Int16"] = SignInt16,
                ["Sign.Int32"] = SignInt32,
            };
        }
    }
}
