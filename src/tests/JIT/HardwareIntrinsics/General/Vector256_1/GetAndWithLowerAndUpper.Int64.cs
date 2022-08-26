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

namespace JIT.HardwareIntrinsics.General._Vector256_1
{
    public static partial class Program
    {
        [Fact]
        public static void GetAndWithLowerAndUpperInt64()
        {
            var test = new VectorGetAndWithLowerAndUpper__GetAndWithLowerAndUpperInt64();

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

    public sealed unsafe class VectorGetAndWithLowerAndUpper__GetAndWithLowerAndUpperInt64
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Int64>>() / sizeof(Int64);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Int64[] values = new Int64[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt64();
            }

            Vector256<Int64> value = Vector256.Create(values[0], values[1], values[2], values[3]);

            Vector128<Int64> lowerResult = value.GetLower();
            Vector128<Int64> upperResult = value.GetUpper();
            ValidateGetResult(lowerResult, upperResult, values);

            Vector256<Int64> result = value.WithLower(upperResult);
            result = result.WithUpper(lowerResult);
            ValidateWithResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Int64[] values = new Int64[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt64();
            }

            Vector256<Int64> value = Vector256.Create(values[0], values[1], values[2], values[3]);

            object lowerResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.GetLower))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            object upperResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.GetUpper))
                                    .MakeGenericMethod(typeof(Int64))
                                    .Invoke(null, new object[] { value });
            ValidateGetResult((Vector128<Int64>)(lowerResult), (Vector128<Int64>)(upperResult), values);

            object result = typeof(Vector256)
                                .GetMethod(nameof(Vector256.WithLower))
                                .MakeGenericMethod(typeof(Int64))
                                .Invoke(null, new object[] { value, upperResult });
            result = typeof(Vector256)
                        .GetMethod(nameof(Vector256.WithUpper))
                        .MakeGenericMethod(typeof(Int64))
                        .Invoke(null, new object[] { result, lowerResult });
            ValidateWithResult((Vector256<Int64>)(result), values);
        }

        private void ValidateGetResult(Vector128<Int64> lowerResult, Vector128<Int64> upperResult, Int64[] values, [CallerMemberName] string method = "")
        {
            Int64[] lowerElements = new Int64[ElementCount / 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref lowerElements[0]), lowerResult);

            Int64[] upperElements = new Int64[ElementCount / 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref upperElements[0]), upperResult);

            ValidateGetResult(lowerElements, upperElements, values, method);
        }

        private void ValidateGetResult(Int64[] lowerResult, Int64[] upperResult, Int64[] values, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < ElementCount / 2; i++)
            {
                if (lowerResult[i] != values[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256<Int64>.GetLower(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", lowerResult)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }

            succeeded = true;

            for (int i = ElementCount / 2; i < ElementCount; i++)
            {
                if (upperResult[i - (ElementCount / 2)] != values[i])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256<Int64>.GetUpper(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", upperResult)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        private void ValidateWithResult(Vector256<Int64> result, Int64[] values, [CallerMemberName] string method = "")
        {
            Int64[] resultElements = new Int64[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int64, byte>(ref resultElements[0]), result);
            ValidateWithResult(resultElements, values, method);
        }

        private void ValidateWithResult(Int64[] result, Int64[] values, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < ElementCount / 2; i++)
            {
                if (result[i] != values[i + (ElementCount / 2)])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256<Int64.WithLower(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }

            succeeded = true;

            for (int i = ElementCount / 2; i < ElementCount; i++)
            {
                if (result[i] != values[i - (ElementCount / 2)])
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256<Int64.WithUpper(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
