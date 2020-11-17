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
                ["LeadingZeroCount.Int32"] = LeadingZeroCount_Int32,
                ["LeadingZeroCount.UInt32"] = LeadingZeroCount_UInt32,
                ["ReverseElementBits.Int32"] = ReverseElementBits_Int32,
                ["ReverseElementBits.UInt32"] = ReverseElementBits_UInt32,
            };
        }
    }
}
