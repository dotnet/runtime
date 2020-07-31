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
                ["ShiftLogicalSaturateScalar.Vector64.Int64"] = ShiftLogicalSaturateScalar_Vector64_Int64,
                ["ShiftLogicalSaturateScalar.Vector64.UInt64"] = ShiftLogicalSaturateScalar_Vector64_UInt64,
            };
        }
    }
}
