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
                ["DuplicateSelectedScalarToVector128.V128.Double.1"] = DuplicateSelectedScalarToVector128_V128_Double_1,
                ["DuplicateSelectedScalarToVector128.V128.Int64.1"] = DuplicateSelectedScalarToVector128_V128_Int64_1,
                ["DuplicateSelectedScalarToVector128.V128.UInt64.1"] = DuplicateSelectedScalarToVector128_V128_UInt64_1,
            };
        }
    }
}
