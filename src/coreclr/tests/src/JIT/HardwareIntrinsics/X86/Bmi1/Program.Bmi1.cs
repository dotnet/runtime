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
                ["AndNot.UInt32"] = AndNotUInt32,
                ["AndNot.UInt64"] = AndNotUInt64,
                ["ExtractLowestSetBit.UInt32"] = ExtractLowestSetBitUInt32,
                ["ExtractLowestSetBit.UInt64"] = ExtractLowestSetBitUInt64,
                ["GetMaskUpToLowestSetBit.UInt32"] = GetMaskUpToLowestSetBitUInt32,
                ["GetMaskUpToLowestSetBit.UInt64"] = GetMaskUpToLowestSetBitUInt64,
                ["ResetLowestSetBit.UInt32"] = ResetLowestSetBitUInt32,
                ["ResetLowestSetBit.UInt64"] = ResetLowestSetBitUInt64,
                ["TrailingZeroCount.UInt32"] = TrailingZeroCountUInt32,
                ["TrailingZeroCount.UInt64"] = TrailingZeroCountUInt64,
            };
        }
    }
}
