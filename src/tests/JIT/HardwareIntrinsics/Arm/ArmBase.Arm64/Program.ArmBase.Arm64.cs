// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["LeadingSignCount.Int32"] = LeadingSignCount_Int32,
                ["LeadingSignCount.Int64"] = LeadingSignCount_Int64,
                ["LeadingZeroCount.Int64"] = LeadingZeroCount_Int64,
                ["LeadingZeroCount.UInt64"] = LeadingZeroCount_UInt64,
                ["MultiplyHigh.Int64"] = MultiplyHigh_Int64,
                ["MultiplyHigh.UInt64"] = MultiplyHigh_UInt64,
                ["ReverseElementBits.Int64"] = ReverseElementBits_Int64,
                ["ReverseElementBits.UInt64"] = ReverseElementBits_UInt64,
            };
        }
    }
}
