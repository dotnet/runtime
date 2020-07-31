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
                ["ConvertToSingle.Vector64.Int32"] = ConvertToSingle_Vector64_Int32,
                ["ConvertToSingle.Vector64.UInt32"] = ConvertToSingle_Vector64_UInt32,
                ["ConvertToSingle.Vector128.Int32"] = ConvertToSingle_Vector128_Int32,
                ["ConvertToSingle.Vector128.UInt32"] = ConvertToSingle_Vector128_UInt32,
            };
        }
    }
}
