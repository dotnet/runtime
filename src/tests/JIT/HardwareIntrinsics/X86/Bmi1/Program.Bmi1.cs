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
                ["AndNot.UIntPtr"] = AndNotUIntPtr,
                ["ExtractLowestSetBit.UInt32"] = ExtractLowestSetBitUInt32,
                ["ExtractLowestSetBit.UIntPtr"] = ExtractLowestSetBitUIntPtr,
                ["GetMaskUpToLowestSetBit.UInt32"] = GetMaskUpToLowestSetBitUInt32,
                ["GetMaskUpToLowestSetBit.UIntPtr"] = GetMaskUpToLowestSetBitUIntPtr,
                ["ResetLowestSetBit.UInt32"] = ResetLowestSetBitUInt32,
                ["ResetLowestSetBit.UIntPtr"] = ResetLowestSetBitUIntPtr,
                ["TrailingZeroCount.UInt32"] = TrailingZeroCountUInt32,
                ["TrailingZeroCount.UIntPtr"] = TrailingZeroCountUIntPtr,
                ["BitFieldExtract.UInt32.3Op"] = BitFieldExtractUInt323Op,
                ["BitFieldExtract.UInt32"] = BitFieldExtractUInt32,
                ["BitFieldExtract.UIntPtr.3Op"] = BitFieldExtractUIntPtr3Op,
                ["BitFieldExtract.UIntPtr"] = BitFieldExtractUIntPtr,
            };
        }
    }
}
