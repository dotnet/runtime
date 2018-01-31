// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace JIT.HardwareIntrinsics.X86
{
    public static partial class Program
    {
        static Program()
        {
            TestList = new Dictionary<string, Action>() {
                ["Add.Single"] = AddSingle,
                ["AddScalar.Single"] = AddScalarSingle,
                ["And.Single"] = AndSingle,
                ["AndNot.Single"] = AndNotSingle,
                ["Divide.Single"] = DivideSingle,
                ["DivideScalar.Single"] = DivideScalarSingle,
                ["Multiply.Single"] = MultiplySingle,
                ["MultiplyScalar.Single"] = MultiplyScalarSingle,
                ["Or.Single"] = OrSingle,
                ["Subtract.Single"] = SubtractSingle,
                ["SubtractScalar.Single"] = SubtractScalarSingle,
                ["Xor.Single"] = XorSingle,
            };
        }
    }
}
