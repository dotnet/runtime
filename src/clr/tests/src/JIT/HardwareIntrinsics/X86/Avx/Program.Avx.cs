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
                ["Add.Double"] = AddDouble,
                ["Add.Single"] = AddSingle,
                ["AddSubtract.Double"] = AddSubtractDouble,
                ["AddSubtract.Single"] = AddSubtractSingle,
                ["And.Double"] = AndDouble,
                ["And.Single"] = AndSingle,
                ["AndNot.Double"] = AndNotDouble,
                ["AndNot.Single"] = AndNotSingle,
                ["BlendVariable.Double"] = BlendVariableDouble,
                ["BlendVariable.Single"] = BlendVariableSingle,
                ["DuplicateEvenIndexed.Double"] = DuplicateEvenIndexedDouble,
                ["DuplicateEvenIndexed.Single"] = DuplicateEvenIndexedSingle,
                ["DuplicateOddIndexed.Single"] = DuplicateOddIndexedSingle,
                ["Divide.Double"] = DivideDouble,
                ["Divide.Single"] = DivideSingle,
                ["Max.Double"] = MaxDouble,
                ["Max.Single"] = MaxSingle,
                ["Min.Double"] = MinDouble,
                ["Min.Single"] = MinSingle,
                ["Multiply.Double"] = MultiplyDouble,
                ["Multiply.Single"] = MultiplySingle,
                ["Or.Double"] = OrDouble,
                ["Or.Single"] = OrSingle,
                ["Subtract.Double"] = SubtractDouble,
                ["Subtract.Single"] = SubtractSingle,
                ["Xor.Double"] = XorDouble,
                ["Xor.Single"] = XorSingle,
            };
        }
    }
}
