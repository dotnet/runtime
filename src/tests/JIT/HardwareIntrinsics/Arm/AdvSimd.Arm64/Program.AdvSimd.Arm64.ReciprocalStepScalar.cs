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
                ["ReciprocalStepScalar.Vector64.Double"] = ReciprocalStepScalar_Vector64_Double,
                ["ReciprocalStepScalar.Vector64.Single"] = ReciprocalStepScalar_Vector64_Single,
            };
        }
    }
}
