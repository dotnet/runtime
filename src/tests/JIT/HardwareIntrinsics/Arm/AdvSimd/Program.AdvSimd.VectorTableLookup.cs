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
                ["VectorTableLookup.Vector64.Byte"] = VectorTableLookup_Vector64_Byte,
                ["VectorTableLookup.Vector64.SByte"] = VectorTableLookup_Vector64_SByte,
            };
        }
    }
}
