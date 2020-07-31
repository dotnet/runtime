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
                ["LeadingSignCount.Vector64.Int16"] = LeadingSignCount_Vector64_Int16,
                ["LeadingSignCount.Vector64.Int32"] = LeadingSignCount_Vector64_Int32,
                ["LeadingSignCount.Vector64.SByte"] = LeadingSignCount_Vector64_SByte,
                ["LeadingSignCount.Vector128.Int16"] = LeadingSignCount_Vector128_Int16,
                ["LeadingSignCount.Vector128.Int32"] = LeadingSignCount_Vector128_Int32,
                ["LeadingSignCount.Vector128.SByte"] = LeadingSignCount_Vector128_SByte,
            };
        }
    }
}
