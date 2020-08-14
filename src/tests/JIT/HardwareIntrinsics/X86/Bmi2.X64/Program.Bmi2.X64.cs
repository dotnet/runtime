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
                ["ParallelBitDeposit.UInt64"] = ParallelBitDepositUInt64,
                ["ParallelBitExtract.UInt64"] = ParallelBitExtractUInt64,
                ["ZeroHighBits.UInt64"] = ZeroHighBitsUInt64,
                ["MultiplyNoFlags.UInt64"] = MultiplyNoFlagsUInt64,
                ["MultiplyNoFlags.UInt64.BinRes"] = MultiplyNoFlagsUInt64BinRes,
            };
        }
    }
}
