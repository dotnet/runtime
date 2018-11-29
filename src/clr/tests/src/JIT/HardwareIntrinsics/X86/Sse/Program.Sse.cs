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
                ["CompareEqualOrderedScalar.Boolean"] = CompareEqualOrderedScalarBoolean,
                ["CompareEqualUnorderedScalar.Boolean"] = CompareEqualUnorderedScalarBoolean,
                ["CompareGreaterThan.Single"] = CompareGreaterThanSingle,
                ["CompareGreaterThanScalar.Single"] = CompareGreaterThanScalarSingle,
                ["CompareGreaterThanOrderedScalar.Boolean"] = CompareGreaterThanOrderedScalarBoolean,
                ["CompareGreaterThanUnorderedScalar.Boolean"] = CompareGreaterThanUnorderedScalarBoolean,
                ["CompareGreaterThanOrEqual.Single"] = CompareGreaterThanOrEqualSingle,
                ["CompareGreaterThanOrEqualScalar.Single"] = CompareGreaterThanOrEqualScalarSingle,
                ["CompareGreaterThanOrEqualOrderedScalar.Boolean"] = CompareGreaterThanOrEqualOrderedScalarBoolean,
                ["CompareGreaterThanOrEqualUnorderedScalar.Boolean"] = CompareGreaterThanOrEqualUnorderedScalarBoolean,
                ["CompareLessThan.Single"] = CompareLessThanSingle,
                ["CompareLessThanScalar.Single"] = CompareLessThanScalarSingle,
                ["CompareLessThanOrderedScalar.Boolean"] = CompareLessThanOrderedScalarBoolean,
                ["CompareLessThanUnorderedScalar.Boolean"] = CompareLessThanUnorderedScalarBoolean,
                ["CompareLessThanOrEqual.Single"] = CompareLessThanOrEqualSingle,
                ["CompareLessThanOrEqualScalar.Single"] = CompareLessThanOrEqualScalarSingle,
                ["CompareLessThanOrEqualOrderedScalar.Boolean"] = CompareLessThanOrEqualOrderedScalarBoolean,
                ["CompareLessThanOrEqualUnorderedScalar.Boolean"] = CompareLessThanOrEqualUnorderedScalarBoolean,
                ["CompareNotEqual.Single"] = CompareNotEqualSingle,
                ["CompareNotEqualScalar.Single"] = CompareNotEqualScalarSingle,
                ["CompareNotEqualOrderedScalar.Boolean"] = CompareNotEqualOrderedScalarBoolean,
                ["CompareNotEqualUnorderedScalar.Boolean"] = CompareNotEqualUnorderedScalarBoolean,
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
                ["ConvertScalarToVector128Single.Single"] = ConvertScalarToVector128SingleSingle,
                ["Divide.Single"] = DivideSingle,
                ["DivideScalar.Single"] = DivideScalarSingle,
                ["LoadVector128.Single"] = LoadVector128Single,
                ["LoadScalarVector128.Single"] = LoadScalarVector128Single,
                ["Max.Single"] = MaxSingle,
                ["MaxScalar.Single"] = MaxScalarSingle,
                ["Min.Single"] = MinSingle,
                ["MinScalar.Single"] = MinScalarSingle,
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
