// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.Arm
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["DotProduct.Vector64.Int32"] = DotProduct_Vector64_Int32,
                ["DotProduct.Vector64.UInt32"] = DotProduct_Vector64_UInt32,
                ["DotProduct.Vector128.Int32"] = DotProduct_Vector128_Int32,
                ["DotProduct.Vector128.UInt32"] = DotProduct_Vector128_UInt32,
            };
        }
    }
}
