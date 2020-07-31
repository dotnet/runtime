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
                ["MultiplyWideningLowerAndSubtract.Vector64.Byte"] = MultiplyWideningLowerAndSubtract_Vector64_Byte,
                ["MultiplyWideningLowerAndSubtract.Vector64.Int16"] = MultiplyWideningLowerAndSubtract_Vector64_Int16,
                ["MultiplyWideningLowerAndSubtract.Vector64.Int32"] = MultiplyWideningLowerAndSubtract_Vector64_Int32,
                ["MultiplyWideningLowerAndSubtract.Vector64.SByte"] = MultiplyWideningLowerAndSubtract_Vector64_SByte,
                ["MultiplyWideningLowerAndSubtract.Vector64.UInt16"] = MultiplyWideningLowerAndSubtract_Vector64_UInt16,
                ["MultiplyWideningLowerAndSubtract.Vector64.UInt32"] = MultiplyWideningLowerAndSubtract_Vector64_UInt32,
            };
        }
    }
}
