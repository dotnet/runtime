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
        private static void ToVector256SByte()
        {
            var test = new VectorExtend__ToVector256SByte();

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

    public sealed unsafe class VectorExtend__ToVector256SByte
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector128<SByte>>() / sizeof(SByte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            SByte[] values = new SByte[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetSByte();
            }

            Vector128<SByte> value = Vector128.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8], values[9], values[10], values[11], values[12], values[13], values[14], values[15]);

            Vector256<SByte> result = value.ToVector256();
            ValidateResult(result, values, isUnsafe: false);

            Vector256<SByte> unsafeResult = value.ToVector256Unsafe();
            ValidateResult(unsafeResult, values, isUnsafe: true);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            SByte[] values = new SByte[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetSByte();
            }

            Vector128<SByte> value = Vector128.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8], values[9], values[10], values[11], values[12], values[13], values[14], values[15]);

            object result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.ToVector256))
                                .MakeGenericMethod(typeof(SByte))
                                .Invoke(null, new object[] { value });
            ValidateResult((Vector256<SByte>)(result), values, isUnsafe: false);

            object unsafeResult = typeof(Vector128)
                                    .GetMethod(nameof(Vector128.ToVector256))
                                    .MakeGenericMethod(typeof(SByte))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector256<SByte>)(unsafeResult), values, isUnsafe: true);
        }

        private void ValidateResult(Vector256<SByte> result, SByte[] values, bool isUnsafe, [CallerMemberName] string method = "")
        {
            SByte[] resultElements = new SByte[ElementCount * 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref resultElements[0]), result);

            ValidateResult(resultElements, values, isUnsafe, method);
        }

        private void ValidateResult(SByte[] result, SByte[] values, bool isUnsafe, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128<SByte>.ToVector256{(isUnsafe ? "Unsafe" : "")}(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
