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
                ["ConvertToDouble.Vector64.Single"] = ConvertToDouble_Vector64_Single,
                ["ConvertToDouble.Vector128.Int64"] = ConvertToDouble_Vector128_Int64,
                ["ConvertToDouble.Vector128.UInt64"] = ConvertToDouble_Vector128_UInt64,
            };
        }
    }
}
