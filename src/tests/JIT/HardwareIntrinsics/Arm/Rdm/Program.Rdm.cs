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
                ["MultiplyRoundedDoublingAndAddSaturateHigh.Vector64.Int16"] = MultiplyRoundedDoublingAndAddSaturateHigh_Vector64_Int16,
                ["MultiplyRoundedDoublingAndAddSaturateHigh.Vector64.Int32"] = MultiplyRoundedDoublingAndAddSaturateHigh_Vector64_Int32,
                ["MultiplyRoundedDoublingAndAddSaturateHigh.Vector128.Int16"] = MultiplyRoundedDoublingAndAddSaturateHigh_Vector128_Int16,
                ["MultiplyRoundedDoublingAndAddSaturateHigh.Vector128.Int32"] = MultiplyRoundedDoublingAndAddSaturateHigh_Vector128_Int32,
                ["MultiplyRoundedDoublingAndSubtractSaturateHigh.Vector64.Int16"] = MultiplyRoundedDoublingAndSubtractSaturateHigh_Vector64_Int16,
                ["MultiplyRoundedDoublingAndSubtractSaturateHigh.Vector64.Int32"] = MultiplyRoundedDoublingAndSubtractSaturateHigh_Vector64_Int32,
                ["MultiplyRoundedDoublingAndSubtractSaturateHigh.Vector128.Int16"] = MultiplyRoundedDoublingAndSubtractSaturateHigh_Vector128_Int16,
                ["MultiplyRoundedDoublingAndSubtractSaturateHigh.Vector128.Int32"] = MultiplyRoundedDoublingAndSubtractSaturateHigh_Vector128_Int32,
                ["MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh.Vector64.Int16.Vector64.Int16.3"] = MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh_Vector64_Int16_Vector64_Int16_3,
                ["MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh.Vector64.Int16.Vector128.Int16.7"] = MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh_Vector64_Int16_Vector128_Int16_7,
                ["MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh.Vector64.Int32.Vector64.Int32.1"] = MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh_Vector64_Int32_Vector64_Int32_1,
                ["MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh.Vector64.Int32.Vector128.Int32.3"] = MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh_Vector64_Int32_Vector128_Int32_3,
                ["MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh.Vector128.Int16.Vector64.Int16.3"] = MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh_Vector128_Int16_Vector64_Int16_3,
                ["MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh.Vector128.Int16.Vector128.Int16.7"] = MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh_Vector128_Int16_Vector128_Int16_7,
                ["MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh.Vector128.Int32.Vector64.Int32.1"] = MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh_Vector128_Int32_Vector64_Int32_1,
                ["MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh.Vector128.Int32.Vector128.Int32.3"] = MultiplyRoundedDoublingBySelectedScalarAndAddSaturateHigh_Vector128_Int32_Vector128_Int32_3,
                ["MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh.Vector64.Int16.Vector64.Int16.3"] = MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh_Vector64_Int16_Vector64_Int16_3,
                ["MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh.Vector64.Int16.Vector128.Int16.7"] = MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh_Vector64_Int16_Vector128_Int16_7,
                ["MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh.Vector64.Int32.Vector64.Int32.1"] = MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh_Vector64_Int32_Vector64_Int32_1,
                ["MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh.Vector64.Int32.Vector128.Int32.3"] = MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh_Vector64_Int32_Vector128_Int32_3,
                ["MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh.Vector128.Int16.Vector64.Int16.3"] = MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh_Vector128_Int16_Vector64_Int16_3,
                ["MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh.Vector128.Int16.Vector128.Int16.7"] = MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh_Vector128_Int16_Vector128_Int16_7,
                ["MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh.Vector128.Int32.Vector64.Int32.1"] = MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh_Vector128_Int32_Vector64_Int32_1,
                ["MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh.Vector128.Int32.Vector128.Int32.3"] = MultiplyRoundedDoublingBySelectedScalarAndSubtractSaturateHigh_Vector128_Int32_Vector128_Int32_3,
            };
        }
    }
}
