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
                ["ReciprocalSquareRootEstimate.Vector64.Single"] = ReciprocalSquareRootEstimate_Vector64_Single,
                ["ReciprocalSquareRootEstimate.Vector64.UInt32"] = ReciprocalSquareRootEstimate_Vector64_UInt32,
                ["ReciprocalSquareRootEstimate.Vector128.Single"] = ReciprocalSquareRootEstimate_Vector128_Single,
                ["ReciprocalSquareRootEstimate.Vector128.UInt32"] = ReciprocalSquareRootEstimate_Vector128_UInt32,
            };
        }
    }
}
