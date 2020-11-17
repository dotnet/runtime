// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["AndNot.UInt64"] = AndNotUInt64,
                ["ExtractLowestSetBit.UInt64"] = ExtractLowestSetBitUInt64,
                ["GetMaskUpToLowestSetBit.UInt64"] = GetMaskUpToLowestSetBitUInt64,
                ["ResetLowestSetBit.UInt64"] = ResetLowestSetBitUInt64,
                ["TrailingZeroCount.UInt64"] = TrailingZeroCountUInt64,
                ["BitFieldExtract.UInt64.3Op"] = BitFieldExtractUInt643Op,
                ["BitFieldExtract.UInt64"] = BitFieldExtractUInt64,
            };
        }
    }
}
