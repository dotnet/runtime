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
        public static void CreateElementUInt32()
        {
            var test = new VectorCreate__CreateElementUInt32();

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

    public sealed unsafe class VectorCreate__CreateElementUInt32
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<UInt32>>() / sizeof(UInt32);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            UInt32[] values = new UInt32[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetUInt32();
            }

            Vector256<UInt32> result = Vector256.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);

            ValidateResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Type[] operandTypes = new Type[ElementCount];
            UInt32[] values = new UInt32[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                operandTypes[i] = typeof(UInt32);
                values[i] = TestLibrary.Generator.GetUInt32();
            }

            object result = typeof(Vector256)
                                .GetMethod(nameof(Vector256.Create), operandTypes)
                                .Invoke(null, new object[] { values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7] });

            ValidateResult((Vector256<UInt32>)(result), values);
        }

        private void ValidateResult(Vector256<UInt32> result, UInt32[] expectedValues, [CallerMemberName] string method = "")
        {
            UInt32[] resultElements = new UInt32[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt32, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedValues, method);
        }

        private void ValidateResult(UInt32[] resultElements, UInt32[] expectedValues, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector256.Create(UInt32): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", expectedValues)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
