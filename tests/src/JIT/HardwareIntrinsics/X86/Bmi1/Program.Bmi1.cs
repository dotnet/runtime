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
                ["ExtractLowestSetBit.UInt32"] = ExtractLowestSetBitUInt32,
                ["GetMaskUpToLowestSetBit.UInt32"] = GetMaskUpToLowestSetBitUInt32,
                ["ResetLowestSetBit.UInt32"] = ResetLowestSetBitUInt32,
                ["TrailingZeroCount.UInt32"] = TrailingZeroCountUInt32,
            };
        }
    }
}
