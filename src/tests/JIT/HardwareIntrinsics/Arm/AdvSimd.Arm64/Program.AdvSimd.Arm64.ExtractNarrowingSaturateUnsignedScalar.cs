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
                ["ExtractNarrowingSaturateUnsignedScalar.Vector64.Byte"] = ExtractNarrowingSaturateUnsignedScalar_Vector64_Byte,
                ["ExtractNarrowingSaturateUnsignedScalar.Vector64.UInt16"] = ExtractNarrowingSaturateUnsignedScalar_Vector64_UInt16,
                ["ExtractNarrowingSaturateUnsignedScalar.Vector64.UInt32"] = ExtractNarrowingSaturateUnsignedScalar_Vector64_UInt32,
            };
        }
    }
}
