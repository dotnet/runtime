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
        public static void GetAndWithLowerAndUpperByte()
        {
            var test = new VectorGetAndWithLowerAndUpper__GetAndWithLowerAndUpperByte();

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

    public sealed unsafe class VectorGetAndWithLowerAndUpper__GetAndWithLowerAndUpperByte
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Byte>>() / sizeof(Byte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Byte[] values = new Byte[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetByte();
            }

            Vector256<Byte> value = Vector256.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8], values[9], values[10], values[11], values[12], values[13], values[14], values[15], values[16], values[17], values[18], values[19], values[20], values[21], values[22], values[23], values[24], values[25], values[26], values[27], values[28], values[29], values[30], values[31]);

            Vector128<Byte> lowerResult = value.GetLower();
            Vector128<Byte> upperResult = value.GetUpper();
            ValidateGetResult(lowerResult, upperResult, values);

            Vector256<Byte> result = value.WithLower(upperResult);
            result = result.WithUpper(lowerResult);
            ValidateWithResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Byte[] values = new Byte[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetByte();
            }

            Vector256<Byte> value = Vector256.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8], values[9], values[10], values[11], values[12], values[13], values[14], values[15], values[16], values[17], values[18], values[19], values[20], values[21], values[22], values[23], values[24], values[25], values[26], values[27], values[28], values[29], values[30], values[31]);

            object lowerResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.GetLower))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            object upperResult = typeof(Vector256)
                                    .GetMethod(nameof(Vector256.GetUpper))
                                    .MakeGenericMethod(typeof(Byte))
                                    .Invoke(null, new object[] { value });
            ValidateGetResult((Vector128<Byte>)(lowerResult), (Vector128<Byte>)(upperResult), values);

            object result = typeof(Vector256)
                                .GetMethod(nameof(Vector256.WithLower))
                                .MakeGenericMethod(typeof(Byte))
                                .Invoke(null, new object[] { value, upperResult });
            result = typeof(Vector256)
                        .GetMethod(nameof(Vector256.WithUpper))
                        .MakeGenericMethod(typeof(Byte))
                        .Invoke(null, new object[] { result, lowerResult });
            ValidateWithResult((Vector256<Byte>)(result), values);
        }

        private void ValidateGetResult(Vector128<Byte> lowerResult, Vector128<Byte> upperResult, Byte[] values, [CallerMemberName] string method = "")
        {
            Byte[] lowerElements = new Byte[ElementCount / 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref lowerElements[0]), lowerResult);

            Byte[] upperElements = new Byte[ElementCount / 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref upperElements[0]), upperResult);

            ValidateGetResult(lowerElements, upperElements, values, method);
        }

        private void ValidateGetResult(Byte[] lowerResult, Byte[] upperResult, Byte[] values, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector256<Byte>.GetLower(): {method} failed:");
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
                TestLibrary.TestFramework.LogInformation($"Vector256<Byte>.GetUpper(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", upperResult)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        private void ValidateWithResult(Vector256<Byte> result, Byte[] values, [CallerMemberName] string method = "")
        {
            Byte[] resultElements = new Byte[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref resultElements[0]), result);
            ValidateWithResult(resultElements, values, method);
        }

        private void ValidateWithResult(Byte[] result, Byte[] values, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector256<Byte.WithLower(): {method} failed:");
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
                TestLibrary.TestFramework.LogInformation($"Vector256<Byte.WithUpper(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
