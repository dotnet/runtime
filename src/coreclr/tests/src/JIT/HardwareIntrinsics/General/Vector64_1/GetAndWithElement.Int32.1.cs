// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\General\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        private static void GetAndWithElementInt321()
        {
            var test = new VectorGetAndWithElement__GetAndWithElementInt321();

            // Validates basic functionality works
            test.RunBasicScenario();

            // Validates calling via reflection works
            test.RunReflectionScenario();

            // Validates that invalid indices throws ArgumentOutOfRangeException
            test.RunArgumentOutOfRangeScenario();

            if (!test.Succeeded)
            {
                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }

    public sealed unsafe class VectorGetAndWithElement__GetAndWithElementInt321
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<Int32>>() / sizeof(Int32);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario(int imm = 1, bool expectedOutOfRangeException = false)
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Int32[] values = new Int32[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt32();
            }

            Vector64<Int32> value = Vector64.Create(values[0], values[1]);

            bool succeeded = !expectedOutOfRangeException;

            try
            {
                Int32 result = value.GetElement(imm);
                ValidateGetResult(result, values);
            }
            catch (ArgumentOutOfRangeException)
            {
                succeeded = expectedOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<Int32.GetElement({imm}): {nameof(RunBasicScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }

            succeeded = !expectedOutOfRangeException;

            Int32 insertedValue = TestLibrary.Generator.GetInt32();

            try
            {
                Vector64<Int32> result2 = value.WithElement(imm, insertedValue);
                ValidateWithResult(result2, values, insertedValue);
            }
            catch (ArgumentOutOfRangeException)
            {
                succeeded = expectedOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<Int32.WithElement({imm}): {nameof(RunBasicScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        public void RunReflectionScenario(int imm = 1, bool expectedOutOfRangeException = false)
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Int32[] values = new Int32[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetInt32();
            }

            Vector64<Int32> value = Vector64.Create(values[0], values[1]);

            bool succeeded = !expectedOutOfRangeException;

            try
            {
                object result = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.GetElement))
                                    .MakeGenericMethod(typeof(Int32))
                                    .Invoke(null, new object[] { value, imm });
                ValidateGetResult((Int32)(result), values);
            }
            catch (TargetInvocationException e)
            {
                succeeded = expectedOutOfRangeException
                          && e.InnerException is ArgumentOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<Int32.GetElement({imm}): {nameof(RunReflectionScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }

            succeeded = !expectedOutOfRangeException;

            Int32 insertedValue = TestLibrary.Generator.GetInt32();

            try
            {
                object result2 = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.WithElement))
                                    .MakeGenericMethod(typeof(Int32))
                                    .Invoke(null, new object[] { value, imm, insertedValue });
                ValidateWithResult((Vector64<Int32>)(result2), values, insertedValue);
            }
            catch (TargetInvocationException e)
            {
                succeeded = expectedOutOfRangeException
                          && e.InnerException is ArgumentOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<Int32.WithElement({imm}): {nameof(RunReflectionScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        public void RunArgumentOutOfRangeScenario()
        {
            RunBasicScenario(1 - ElementCount, expectedOutOfRangeException: true);
            RunBasicScenario(1 + ElementCount, expectedOutOfRangeException: true);

            RunReflectionScenario(1 - ElementCount, expectedOutOfRangeException: true);
            RunReflectionScenario(1 + ElementCount, expectedOutOfRangeException: true);
        }

        private void ValidateGetResult(Int32 result, Int32[] values, [CallerMemberName] string method = "")
        {
            if (result != values[1])
            {
                Succeeded = false;

                TestLibrary.TestFramework.LogInformation($"Vector64<Int32.GetElement(1): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({result})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }

        private void ValidateWithResult(Vector64<Int32> result, Int32[] values, Int32 insertedValue, [CallerMemberName] string method = "")
        {
            Int32[] resultElements = new Int32[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Int32, byte>(ref resultElements[0]), result);
            ValidateWithResult(resultElements, values, insertedValue, method);
        }

        private void ValidateWithResult(Int32[] result, Int32[] values, Int32 insertedValue, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < ElementCount; i++)
            {
                if ((i != 1) && (result[i] != values[i]))
                {
                    succeeded = false;
                    break;
                }
            }

            if (result[1] != insertedValue)
            {
                succeeded = false;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<Int32.WithElement(1): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  insert: insertedValue");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
