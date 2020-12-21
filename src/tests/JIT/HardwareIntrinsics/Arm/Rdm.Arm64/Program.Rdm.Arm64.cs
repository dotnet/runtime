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
            TestList = new Dictionary<string, Action>() {
                ["MultiplyRoundedDoublingAndAddSaturateHighScalar.Vector64.Int16"] = MultiplyRoundedDoublingAndAddSaturateHighScalar_Vector64_Int16,
                ["MultiplyRoundedDoublingAndAddSaturateHighScalar.Vector64.Int32"] = MultiplyRoundedDoublingAndAddSaturateHighScalar_Vector64_Int32,
                ["MultiplyRoundedDoublingAndSubtractSaturateHighScalar.Vector64.Int16"] = MultiplyRoundedDoublingAndSubtractSaturateHighScalar_Vector64_Int16,
                ["MultiplyRoundedDoublingAndSubtractSaturateHighScalar.Vector64.Int32"] = MultiplyRoundedDoublingAndSubtractSaturateHighScalar_Vector64_Int32,
                ["MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh.Vector64.Int16.Vector64.Int16.3"] = MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh_Vector64_Int16_Vector64_Int16_3,
                ["MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh.Vector64.Int16.Vector128.Int16.7"] = MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh_Vector64_Int16_Vector128_Int16_7,
                ["MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh.Vector64.Int32.Vector64.Int32.1"] = MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh_Vector64_Int32_Vector64_Int32_1,
                ["MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh.Vector64.Int32.Vector128.Int32.3"] = MultiplyRoundedDoublingScalarBySelectedScalarAndAddSaturateHigh_Vector64_Int32_Vector128_Int32_3,
                ["MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh.Vector64.Int16.Vector64.Int16.3"] = MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh_Vector64_Int16_Vector64_Int16_3,
                ["MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh.Vector64.Int16.Vector128.Int16.7"] = MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh_Vector64_Int16_Vector128_Int16_7,
                ["MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh.Vector64.Int32.Vector64.Int32.1"] = MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh_Vector64_Int32_Vector64_Int32_1,
                ["MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh.Vector64.Int32.Vector128.Int32.3"] = MultiplyRoundedDoublingScalarBySelectedScalarAndSubtractSaturateHigh_Vector64_Int32_Vector128_Int32_3,
            };
        }
    }
}
