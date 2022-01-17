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
                ["AndNot.UInt32"] = AndNotUInt32,
                ["AndNot.nuint"] = AndNotnuint,
                ["ExtractLowestSetBit.UInt32"] = ExtractLowestSetBitUInt32,
                ["ExtractLowestSetBit.nuint"] = ExtractLowestSetBitnuint,
                ["GetMaskUpToLowestSetBit.UInt32"] = GetMaskUpToLowestSetBitUInt32,
                ["GetMaskUpToLowestSetBit.nuint"] = GetMaskUpToLowestSetBitnuint,
                ["ResetLowestSetBit.UInt32"] = ResetLowestSetBitUInt32,
                ["ResetLowestSetBit.nuint"] = ResetLowestSetBitnuint,
                ["TrailingZeroCount.UInt32"] = TrailingZeroCountUInt32,
                ["TrailingZeroCount.nuint"] = TrailingZeroCountnuint,
                ["BitFieldExtract.UInt32.3Op"] = BitFieldExtractUInt323Op,
                ["BitFieldExtract.UInt32"] = BitFieldExtractUInt32,
                ["BitFieldExtract.nuint.3Op"] = BitFieldExtractnuint3Op,
                ["BitFieldExtract.nuint"] = BitFieldExtractnuint,
            };
        }
    }
}
