// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["MultiplyAdd.Double"] = MultiplyAddDouble,
                ["MultiplyAdd.Single"] = MultiplyAddSingle,
                ["MultiplyAddNegated.Double"] = MultiplyAddNegatedDouble,
                ["MultiplyAddNegated.Single"] = MultiplyAddNegatedSingle,
                ["MultiplyAddSubtract.Double"] = MultiplyAddSubtractDouble,
                ["MultiplyAddSubtract.Single"] = MultiplyAddSubtractSingle,
                ["MultiplySubtract.Double"] = MultiplySubtractDouble,
                ["MultiplySubtract.Single"] = MultiplySubtractSingle,
                ["MultiplySubtractAdd.Double"] = MultiplySubtractAddDouble,
                ["MultiplySubtractAdd.Single"] = MultiplySubtractAddSingle,
                ["MultiplySubtractNegated.Double"] = MultiplySubtractNegatedDouble,
                ["MultiplySubtractNegated.Single"] = MultiplySubtractNegatedSingle,
            };
        }
    }
}
