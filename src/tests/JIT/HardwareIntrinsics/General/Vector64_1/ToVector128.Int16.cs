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
        private static void ToVector128Int16()
        {
            var test = new VectorExtend__ToVector128Int16();

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

    public sealed unsafe class VectorExtend__ToVector128Int16
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<Int16>>() / sizeof(Int16);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Int16[] values = new Int16[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt16();
            }

            Vector64<Int16> value = Vector64.Create(values[0], values[1], values[2], values[3]);

            Vector128<Int16> result = value.ToVector128();
            ValidateResult(result, values, isUnsafe: false);

            Vector128<Int16> unsafeResult = value.ToVector128Unsafe();
            ValidateResult(unsafeResult, values, isUnsafe: true);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Int16[] values = new Int16[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt16();
            }

            Vector64<Int16> value = Vector64.Create(values[0], values[1], values[2], values[3]);

            object result = typeof(Vector64)
                                .GetMethod(nameof(Vector64.ToVector128))
                                .MakeGenericMethod(typeof(Int16))
                                .Invoke(null, new object[] { value });
            ValidateResult((Vector128<Int16>)(result), values, isUnsafe: false);

            object unsafeResult = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.ToVector128))
                                    .MakeGenericMethod(typeof(Int16))
                                    .Invoke(null, new object[] { value });
            ValidateResult((Vector128<Int16>)(unsafeResult), values, isUnsafe: true);
        }

        private void ValidateResult(Vector128<Int16> result, Int16[] values, bool isUnsafe, [CallerMemberName] string method = "")
        {
            Int16[] resultElements = new Int16[ElementCount * 2];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int16, byte>(ref resultElements[0]), result);

            ValidateResult(resultElements, values, isUnsafe, method);
        }

        private void ValidateResult(Int16[] result, Int16[] values, bool isUnsafe, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector64<Int16>.ToVector128{(isUnsafe ? "Unsafe" : "")}(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
