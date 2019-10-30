// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            };
        }
    }
}
