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
                ["InsertScalar.Vector128.Double.1"] = InsertScalar_Vector128_Double_1,
                ["InsertScalar.Vector128.Int64.1"] = InsertScalar_Vector128_Int64_1,
                ["InsertScalar.Vector128.UInt64.1"] = InsertScalar_Vector128_UInt64_1,
            };
        }
    }
}
