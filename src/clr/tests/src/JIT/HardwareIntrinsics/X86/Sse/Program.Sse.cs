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
                ["CompareEqual.Single"] = CompareEqualSingle,
                ["CompareEqualScalar.Single"] = CompareEqualScalarSingle,
                ["CompareGreaterThan.Single"] = CompareGreaterThanSingle,
                ["CompareGreaterThanScalar.Single"] = CompareGreaterThanScalarSingle,
                ["CompareGreaterThanOrEqual.Single"] = CompareGreaterThanOrEqualSingle,
                ["CompareGreaterThanOrEqualScalar.Single"] = CompareGreaterThanOrEqualScalarSingle,
                ["CompareLessThan.Single"] = CompareLessThanSingle,
                ["CompareLessThanScalar.Single"] = CompareLessThanScalarSingle,
                ["CompareLessThanOrEqual.Single"] = CompareLessThanOrEqualSingle,
                ["CompareLessThanOrEqualScalar.Single"] = CompareLessThanOrEqualScalarSingle,
                ["CompareNotEqual.Single"] = CompareNotEqualSingle,
                ["CompareNotEqualScalar.Single"] = CompareNotEqualScalarSingle,
                ["CompareNotGreaterThan.Single"] = CompareNotGreaterThanSingle,
                ["CompareNotGreaterThanScalar.Single"] = CompareNotGreaterThanScalarSingle,
                ["CompareNotGreaterThanOrEqual.Single"] = CompareNotGreaterThanOrEqualSingle,
                ["CompareNotGreaterThanOrEqualScalar.Single"] = CompareNotGreaterThanOrEqualScalarSingle,
                ["CompareNotLessThan.Single"] = CompareNotLessThanSingle,
                ["CompareNotLessThanScalar.Single"] = CompareNotLessThanScalarSingle,
                ["CompareNotLessThanOrEqual.Single"] = CompareNotLessThanOrEqualSingle,
                ["CompareNotLessThanOrEqualScalar.Single"] = CompareNotLessThanOrEqualScalarSingle,
                ["CompareOrdered.Single"] = CompareOrderedSingle,
                ["CompareOrderedScalar.Single"] = CompareOrderedScalarSingle,
                ["CompareUnordered.Single"] = CompareUnorderedSingle,
                ["CompareUnorderedScalar.Single"] = CompareUnorderedScalarSingle,
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
