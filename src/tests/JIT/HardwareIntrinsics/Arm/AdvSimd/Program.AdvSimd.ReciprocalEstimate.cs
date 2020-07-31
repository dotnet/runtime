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
                ["ReciprocalEstimate.Vector64.Single"] = ReciprocalEstimate_Vector64_Single,
                ["ReciprocalEstimate.Vector64.UInt32"] = ReciprocalEstimate_Vector64_UInt32,
                ["ReciprocalEstimate.Vector128.Single"] = ReciprocalEstimate_Vector128_Single,
                ["ReciprocalEstimate.Vector128.UInt32"] = ReciprocalEstimate_Vector128_UInt32,
            };
        }
    }
}
