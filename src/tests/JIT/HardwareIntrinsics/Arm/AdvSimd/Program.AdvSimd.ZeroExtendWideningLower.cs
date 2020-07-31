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
                ["ZeroExtendWideningLower.Vector64.Byte"] = ZeroExtendWideningLower_Vector64_Byte,
                ["ZeroExtendWideningLower.Vector64.Int16"] = ZeroExtendWideningLower_Vector64_Int16,
                ["ZeroExtendWideningLower.Vector64.Int32"] = ZeroExtendWideningLower_Vector64_Int32,
                ["ZeroExtendWideningLower.Vector64.SByte"] = ZeroExtendWideningLower_Vector64_SByte,
                ["ZeroExtendWideningLower.Vector64.UInt16"] = ZeroExtendWideningLower_Vector64_UInt16,
                ["ZeroExtendWideningLower.Vector64.UInt32"] = ZeroExtendWideningLower_Vector64_UInt32,
            };
        }
    }
}
