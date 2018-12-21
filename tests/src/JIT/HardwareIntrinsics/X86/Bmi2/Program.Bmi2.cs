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
                ["ParallelBitDeposit.UInt32"] = ParallelBitDepositUInt32,
                ["ParallelBitExtract.UInt32"] = ParallelBitExtractUInt32,
                ["ZeroHighBits.UInt32"] = ZeroHighBitsUInt32,
                ["MultiplyNoFlags.UInt32"] = MultiplyNoFlagsUInt32,
                ["MultiplyNoFlags.UInt32.BinRes"] = MultiplyNoFlagsUInt32BinRes,
            };
        }
    }
}
