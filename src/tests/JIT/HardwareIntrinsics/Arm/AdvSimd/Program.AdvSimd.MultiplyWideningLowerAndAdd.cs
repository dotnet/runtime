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
                ["MultiplyWideningLowerAndAdd.Vector64.Byte"] = MultiplyWideningLowerAndAdd_Vector64_Byte,
                ["MultiplyWideningLowerAndAdd.Vector64.Int16"] = MultiplyWideningLowerAndAdd_Vector64_Int16,
                ["MultiplyWideningLowerAndAdd.Vector64.Int32"] = MultiplyWideningLowerAndAdd_Vector64_Int32,
                ["MultiplyWideningLowerAndAdd.Vector64.SByte"] = MultiplyWideningLowerAndAdd_Vector64_SByte,
                ["MultiplyWideningLowerAndAdd.Vector64.UInt16"] = MultiplyWideningLowerAndAdd_Vector64_UInt16,
                ["MultiplyWideningLowerAndAdd.Vector64.UInt32"] = MultiplyWideningLowerAndAdd_Vector64_UInt32,
            };
        }
    }
}
