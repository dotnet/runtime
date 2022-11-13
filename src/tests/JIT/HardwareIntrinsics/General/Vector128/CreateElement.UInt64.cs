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

namespace JIT.HardwareIntrinsics.General._Vector128
{
    public static partial class Program
    {
        [Fact]
        public static void CreateElementUInt64()
        {
            var test = new VectorCreate__CreateElementUInt64();

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

    public sealed unsafe class VectorCreate__CreateElementUInt64
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector128<UInt64>>() / sizeof(UInt64);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            UInt64[] values = new UInt64[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetUInt64();
            }

            Vector128<UInt64> result = Vector128.Create(values[0], values[1]);

            ValidateResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Type[] operandTypes = new Type[ElementCount];
            UInt64[] values = new UInt64[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                operandTypes[i] = typeof(UInt64);
                values[i] = TestLibrary.Generator.GetUInt64();
            }

            object result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.Create), operandTypes)
                                .Invoke(null, new object[] { values[0], values[1] });

            ValidateResult((Vector128<UInt64>)(result), values);
        }

        private void ValidateResult(Vector128<UInt64> result, UInt64[] expectedValues, [CallerMemberName] string method = "")
        {
            UInt64[] resultElements = new UInt64[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedValues, method);
        }

        private void ValidateResult(UInt64[] resultElements, UInt64[] expectedValues, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128.Create(UInt64): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", expectedValues)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
