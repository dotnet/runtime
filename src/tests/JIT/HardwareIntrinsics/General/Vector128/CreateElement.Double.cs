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

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        private static void CreateElementDouble()
        {
            var test = new VectorCreate__CreateElementDouble();

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

    public sealed unsafe class VectorCreate__CreateElementDouble
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector128<Double>>() / sizeof(Double);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Double[] values = new Double[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetDouble();
            }

            Vector128<Double> result = Vector128.Create(values[0], values[1]);

            ValidateResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Type[] operandTypes = new Type[ElementCount];
            Double[] values = new Double[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                operandTypes[i] = typeof(Double);
                values[i] = TestLibrary.Generator.GetDouble();
            }

            object result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.Create), operandTypes)
                                .Invoke(null, new object[] { values[0], values[1] });

            ValidateResult((Vector128<Double>)(result), values);
        }

        private void ValidateResult(Vector128<Double> result, Double[] expectedValues, [CallerMemberName] string method = "")
        {
            Double[] resultElements = new Double[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Double, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedValues, method);
        }

        private void ValidateResult(Double[] resultElements, Double[] expectedValues, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (var i = 0; i < ElementCount; i++)
            {
                if (resultElements[i] != expectedValues[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector128.Create(Double): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", expectedValues)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
