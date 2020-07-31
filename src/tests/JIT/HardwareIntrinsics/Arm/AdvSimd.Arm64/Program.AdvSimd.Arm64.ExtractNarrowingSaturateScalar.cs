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
                ["ExtractNarrowingSaturateScalar.Vector64.Byte"] = ExtractNarrowingSaturateScalar_Vector64_Byte,
                ["ExtractNarrowingSaturateScalar.Vector64.Int16"] = ExtractNarrowingSaturateScalar_Vector64_Int16,
                ["ExtractNarrowingSaturateScalar.Vector64.Int32"] = ExtractNarrowingSaturateScalar_Vector64_Int32,
                ["ExtractNarrowingSaturateScalar.Vector64.SByte"] = ExtractNarrowingSaturateScalar_Vector64_SByte,
                ["ExtractNarrowingSaturateScalar.Vector64.UInt16"] = ExtractNarrowingSaturateScalar_Vector64_UInt16,
                ["ExtractNarrowingSaturateScalar.Vector64.UInt32"] = ExtractNarrowingSaturateScalar_Vector64_UInt32,
            };
        }
    }
}
