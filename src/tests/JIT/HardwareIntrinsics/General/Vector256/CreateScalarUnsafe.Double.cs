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

namespace JIT.HardwareIntrinsics.General._Vector256
{
    public static partial class Program
    {
        [Fact]
        public static void CreateScalarUnsafeDouble()
        {
            var test = new VectorCreate__CreateScalarUnsafeDouble();

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

    public sealed unsafe class VectorCreate__CreateScalarUnsafeDouble
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Double>>() / sizeof(Double);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Double value = TestLibrary.Generator.GetDouble();
            Vector256<Double> result = Vector256.CreateScalarUnsafe(value);

            ValidateResult(result, value);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Double value = TestLibrary.Generator.GetDouble();
            object result = typeof(Vector256)
                                .GetMethod(nameof(Vector256.CreateScalarUnsafe), new Type[] { typeof(Double) })
                                .Invoke(null, new object[] { value });

            ValidateResult((Vector256<Double>)(result), value);
        }

        private void ValidateResult(Vector256<Double> result, Double expectedValue, [CallerMemberName] string method = "")
        {
            Double[] resultElements = new Double[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedValue, method);
        }

        private void ValidateResult(Double[] resultElements, Double expectedValue, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (resultElements[0] != expectedValue)
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < ElementCount; i++)
                {
                    if (false /* value is uninitialized */)
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256.CreateScalarUnsafe(Double): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: {expectedValue}");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
