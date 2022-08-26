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

namespace JIT.HardwareIntrinsics.General._Vector64
{
    public static partial class Program
    {
        [Fact]
        public static void CreateElementSByte()
        {
            var test = new VectorCreate__CreateElementSByte();

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

    public sealed unsafe class VectorCreate__CreateElementSByte
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<SByte>>() / sizeof(SByte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            SByte[] values = new SByte[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetSByte();
            }

            Vector64<SByte> result = Vector64.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);

            ValidateResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Type[] operandTypes = new Type[ElementCount];
            SByte[] values = new SByte[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                operandTypes[i] = typeof(SByte);
                values[i] = TestLibrary.Generator.GetSByte();
            }

            object result = typeof(Vector64)
                                .GetMethod(nameof(Vector64.Create), operandTypes)
                                .Invoke(null, new object[] { values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7] });

            ValidateResult((Vector64<SByte>)(result), values);
        }

        private void ValidateResult(Vector64<SByte> result, SByte[] expectedValues, [CallerMemberName] string method = "")
        {
            SByte[] resultElements = new SByte[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedValues, method);
        }

        private void ValidateResult(SByte[] resultElements, SByte[] expectedValues, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector64.Create(SByte): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", expectedValues)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
