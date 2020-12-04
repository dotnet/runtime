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
        private static void GetAndWithLowerAndUpperInt32()
        {
            var test = new VectorGetAndWithLowerAndUpper__GetAndWithLowerAndUpperInt32();

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

    public sealed unsafe class VectorGetAndWithLowerAndUpper__GetAndWithLowerAndUpperInt32
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector128<Int32>>() / sizeof(Int32);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Int32[] values = new Int32[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt32();
            }

            Vector128<Int32> value = Vector128.Create(values[0], values[1], values[2], values[3]);

            Vector64<Int32> lowerResult = value.GetLower();
            Vector64<Int32> upperResult = value.GetUpper();
            ValidateGetResult(lowerResult, upperResult, values);

            Vector128<Int32> result = value.WithLower(upperResult);
            result = result.WithUpper(lowerResult);
            ValidateWithResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Int32[] values = new Int32[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt32();
            }

            Vector128<Int32> value = Vector128.Create(values[0], values[1], values[2], values[3]);

            object lowerResult = typeof(Vector128)
                                    .GetMethod(nameof(Vector128.GetLower))
                                    .MakeGenericMethod(typeof(Int32))
                                    .Invoke(null, new object[] { value });
            object upperResult = typeof(Vector128)
                                    .GetMethod(nameof(Vector128.GetUpper))
                                    .MakeGenericMethod(typeof(Int32))
                                    .Invoke(null, new object[] { value });
            ValidateGetResult((Vector64<Int32>)(lowerResult), (Vector64<Int32>)(upperResult), values);

            object result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.WithLower))
                                .MakeGenericMethod(typeof(Int32))
                                .Invoke(null, new object[] { value, upperResult });
            result = typeof(Vector128)
                        .GetMethod(nameof(Vector128.WithUpper))
                        .MakeGenericMethod(typeof(Int32))
                        .Invoke(null, new object[] { result, lowerResult });
            ValidateWithResult((Vector128<Int32>)(result), values);
        }

        private void ValidateGetResult(Vector64<Int32> lowerResult, Vector64<Int32> upperResult, Int32[] values, [CallerMemberName] string method = "")
        {
            Int32[] lowerElements = new Int32[ElementCount / 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref lowerElements[0]), lowerResult);

            Int32[] upperElements = new Int32[ElementCount / 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref upperElements[0]), upperResult);

            ValidateGetResult(lowerElements, upperElements, values, method);
        }

        private void ValidateGetResult(Int32[] lowerResult, Int32[] upperResult, Int32[] values, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128<Int32>.GetLower(): {method} failed:");
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
                TestLibrary.TestFramework.LogInformation($"Vector128<Int32>.GetUpper(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", upperResult)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        private void ValidateWithResult(Vector128<Int32> result, Int32[] values, [CallerMemberName] string method = "")
        {
            Int32[] resultElements = new Int32[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref resultElements[0]), result);
            ValidateWithResult(resultElements, values, method);
        }

        private void ValidateWithResult(Int32[] result, Int32[] values, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128<Int32.WithLower(): {method} failed:");
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
                TestLibrary.TestFramework.LogInformation($"Vector128<Int32.WithUpper(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
