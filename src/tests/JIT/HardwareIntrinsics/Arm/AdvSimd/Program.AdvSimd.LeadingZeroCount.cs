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
            TestList = new Dictionary<string, Action>()
            {
                ["LeadingZeroCount.Vector64.Byte"] = LeadingZeroCount_Vector64_Byte,
                ["LeadingZeroCount.Vector64.Int16"] = LeadingZeroCount_Vector64_Int16,
                ["LeadingZeroCount.Vector64.Int32"] = LeadingZeroCount_Vector64_Int32,
                ["LeadingZeroCount.Vector64.SByte"] = LeadingZeroCount_Vector64_SByte,
                ["LeadingZeroCount.Vector64.UInt16"] = LeadingZeroCount_Vector64_UInt16,
                ["LeadingZeroCount.Vector64.UInt32"] = LeadingZeroCount_Vector64_UInt32,
                ["LeadingZeroCount.Vector128.Byte"] = LeadingZeroCount_Vector128_Byte,
                ["LeadingZeroCount.Vector128.Int16"] = LeadingZeroCount_Vector128_Int16,
                ["LeadingZeroCount.Vector128.Int32"] = LeadingZeroCount_Vector128_Int32,
                ["LeadingZeroCount.Vector128.SByte"] = LeadingZeroCount_Vector128_SByte,
                ["LeadingZeroCount.Vector128.UInt16"] = LeadingZeroCount_Vector128_UInt16,
                ["LeadingZeroCount.Vector128.UInt32"] = LeadingZeroCount_Vector128_UInt32,
            };
        }
    }
}
