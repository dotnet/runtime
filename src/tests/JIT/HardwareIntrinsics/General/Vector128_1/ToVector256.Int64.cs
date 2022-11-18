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

namespace JIT.HardwareIntrinsics.General._Vector128_1
{
    public static partial class Program
    {
        [Fact]
        public static void ToVector256Int64()
        {
            var test = new VectorExtend__ToVector256Int64();

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

    public sealed unsafe class VectorExtend__ToVector256Int64
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector128<Int64>>() / sizeof(Int64);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Int64[] values = new Int64[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt64();
            }

            Vector128<Int64> value = Vector128.Create(values[0], values[1]);

            Vector256<Int64> result = value.ToVector256();
            ValidateResult(result, values, isUnsafe: false);

            Vector256<Int64> unsafeResult = value.ToVector256Unsafe();
            ValidateResult(unsafeResult, values, isUnsafe: true);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Int64[] values = new Int64[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt64();
            }

            Vector128<Int64> value = Vector128.Create(values[0], values[1]);

            object result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.ToVector256))
                                .MakeGenericMethod(typeof(Int64))
                                .Invoke(null, new object[] { value });
            ValidateResult((Vector256<Int64>)(result), values, isUnsafe: false);

            object unsafeResult = typeof(Vector128)
                                    .GetMethod(nameof(Vector128.ToVector256))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<Int64>)(unsafeResult), values, isUnsafe: true);
        }

        private void ValidateResult(Vector256<Int64> result, Int64[] values, bool isUnsafe, [CallerMemberName] string method = "")
        {
            Int64[] resultElements = new Int64[ElementCount * 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref resultElements[0]), result);

            ValidateResult(resultElements, values, isUnsafe, method);
        }

        private void ValidateResult(Int64[] result, Int64[] values, bool isUnsafe, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < ElementCount; i++)
            {
                if (result[i] != values[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!isUnsafe)
            {
                for (int i = ElementCount; i < ElementCount * 2; i++)
                {
                    if (result[i] != 0)
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector128<Int64>.ToVector256{(isUnsafe ? "Unsafe" : "")}(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
