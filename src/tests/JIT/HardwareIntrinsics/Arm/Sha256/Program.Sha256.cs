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
                ["HashUpdate1.Vector128.UInt32"] = HashUpdate1_Vector128_UInt32,
                ["HashUpdate2.Vector128.UInt32"] = HashUpdate2_Vector128_UInt32,
                ["ScheduleUpdate0.Vector128.UInt32"] = ScheduleUpdate0_Vector128_UInt32,
                ["ScheduleUpdate1.Vector128.UInt32"] = ScheduleUpdate1_Vector128_UInt32,
            };
        }
    }
}
