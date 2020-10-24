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
                ["FixedRotate.Vector64.UInt32"] = FixedRotate_Vector64_UInt32,
                ["HashUpdateChoose.Vector128.UInt32"] = HashUpdateChoose_Vector128_UInt32,
                ["HashUpdateMajority.Vector128.UInt32"] = HashUpdateMajority_Vector128_UInt32,
                ["HashUpdateParity.Vector128.UInt32"] = HashUpdateParity_Vector128_UInt32,
                ["ScheduleUpdate0.Vector128.UInt32"] = ScheduleUpdate0_Vector128_UInt32,
                ["ScheduleUpdate1.Vector128.UInt32"] = ScheduleUpdate1_Vector128_UInt32,
            };
        }
    }
}
