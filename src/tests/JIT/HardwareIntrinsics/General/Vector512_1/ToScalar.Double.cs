// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\General\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

namespace JIT.HardwareIntrinsics.General._Vector512_1
{
    public static partial class Program
    {
        [Fact]
        public static void ToScalarDouble()
        {
            var test = new VectorToScalar__ToScalarDouble();

            // Validates basic functionality works
            test.RunBasicScenario();

            // Validates calling via reflection works
            test.RunReflectionScenario();

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class VectorToScalar__ToScalarDouble
    {
        private static readonly int LargestVectorSize = 64;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector512<Double>>() / sizeof(Double);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Double[] values = new Double[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetDouble();
            }

            Vector512<Double> value = Vector512.Create(values[0], values[1], values[2], values[3]);

            Double result = value.ToScalar();
            ValidateResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Double[] values = new Double[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetDouble();
            }

            Vector512<Double> value = Vector512.Create(values[0], values[1], values[2], values[3]);

            object result = typeof(Vector512)
                                .GetMethod(nameof(Vector512.ToScalar))
                                .MakeGenericMethod(typeof(Double))
                                .Invoke(null, new object[] { value });

            ValidateResult((Double)(result), values);
        }

        private void ValidateResult(Double result, Double[] values, [CallerMemberName] string method = "")
        {
            if (result != values[0])
            {
                TestLibrary.TestFramework.LogInformation($"Vector512<Double>.ToScalar(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  values: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
