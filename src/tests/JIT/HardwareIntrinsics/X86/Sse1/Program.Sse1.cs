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
                ["Add.Single"] = AddSingle,
                ["AddScalar.Single"] = AddScalarSingle,
                ["And.Single"] = AndSingle,
                ["AndNot.Single"] = AndNotSingle,
                ["CompareEqual.Single"] = CompareEqualSingle,
                ["CompareScalarEqual.Single"] = CompareScalarEqualSingle,
                ["CompareScalarOrderedEqual.Boolean"] = CompareScalarOrderedEqualBoolean,
                ["CompareScalarUnorderedEqual.Boolean"] = CompareScalarUnorderedEqualBoolean,
                ["CompareGreaterThan.Single"] = CompareGreaterThanSingle,
                ["CompareScalarGreaterThan.Single"] = CompareScalarGreaterThanSingle,
                ["CompareScalarOrderedGreaterThan.Boolean"] = CompareScalarOrderedGreaterThanBoolean,
                ["CompareScalarUnorderedGreaterThan.Boolean"] = CompareScalarUnorderedGreaterThanBoolean,
                ["CompareGreaterThanOrEqual.Single"] = CompareGreaterThanOrEqualSingle,
                ["CompareScalarGreaterThanOrEqual.Single"] = CompareScalarGreaterThanOrEqualSingle,
                ["CompareScalarOrderedGreaterThanOrEqual.Boolean"] = CompareScalarOrderedGreaterThanOrEqualBoolean,
                ["CompareScalarUnorderedGreaterThanOrEqual.Boolean"] = CompareScalarUnorderedGreaterThanOrEqualBoolean,
                ["CompareLessThan.Single"] = CompareLessThanSingle,
                ["CompareScalarLessThan.Single"] = CompareScalarLessThanSingle,
                ["CompareScalarOrderedLessThan.Boolean"] = CompareScalarOrderedLessThanBoolean,
                ["CompareScalarUnorderedLessThan.Boolean"] = CompareScalarUnorderedLessThanBoolean,
                ["CompareLessThanOrEqual.Single"] = CompareLessThanOrEqualSingle,
                ["CompareScalarLessThanOrEqual.Single"] = CompareScalarLessThanOrEqualSingle,
                ["CompareScalarOrderedLessThanOrEqual.Boolean"] = CompareScalarOrderedLessThanOrEqualBoolean,
                ["CompareScalarUnorderedLessThanOrEqual.Boolean"] = CompareScalarUnorderedLessThanOrEqualBoolean,
                ["CompareNotEqual.Single"] = CompareNotEqualSingle,
                ["CompareScalarNotEqual.Single"] = CompareScalarNotEqualSingle,
                ["CompareScalarOrderedNotEqual.Boolean"] = CompareScalarOrderedNotEqualBoolean,
                ["CompareScalarUnorderedNotEqual.Boolean"] = CompareScalarUnorderedNotEqualBoolean,
                ["CompareNotGreaterThan.Single"] = CompareNotGreaterThanSingle,
                ["CompareScalarNotGreaterThan.Single"] = CompareScalarNotGreaterThanSingle,
                ["CompareNotGreaterThanOrEqual.Single"] = CompareNotGreaterThanOrEqualSingle,
                ["CompareScalarNotGreaterThanOrEqual.Single"] = CompareScalarNotGreaterThanOrEqualSingle,
                ["CompareNotLessThan.Single"] = CompareNotLessThanSingle,
                ["CompareScalarNotLessThan.Single"] = CompareScalarNotLessThanSingle,
                ["CompareNotLessThanOrEqual.Single"] = CompareNotLessThanOrEqualSingle,
                ["CompareScalarNotLessThanOrEqual.Single"] = CompareScalarNotLessThanOrEqualSingle,
                ["CompareOrdered.Single"] = CompareOrderedSingle,
                ["CompareScalarOrdered.Single"] = CompareScalarOrderedSingle,
                ["CompareUnordered.Single"] = CompareUnorderedSingle,
                ["CompareScalarUnordered.Single"] = CompareScalarUnorderedSingle,
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
