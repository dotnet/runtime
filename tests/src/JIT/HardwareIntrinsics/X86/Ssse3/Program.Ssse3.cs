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
