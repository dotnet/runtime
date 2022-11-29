// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using Xunit;

namespace JIT.HardwareIntrinsics.General._Vector128_1
{
    public static partial class Program
    {
        [Fact]
        public static void GetAndWithElementUInt167()
        {
            var test = new VectorGetAndWithElement__GetAndWithElementUInt167();

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

    public sealed unsafe class VectorGetAndWithElement__GetAndWithElementUInt167
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario(int imm = 7, bool expectedOutOfRangeException = false)
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            UInt16[] values = new UInt16[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetUInt16();
            }

            Vector128<UInt16> value = Vector128.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);

            bool succeeded = !expectedOutOfRangeException;

            try
            {
                UInt16 result = value.GetElement(imm);
                ValidateGetResult(result, values);
            }
            catch (ArgumentOutOfRangeException)
            {
                succeeded = expectedOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector128<UInt16.GetElement({imm}): {nameof(RunBasicScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }

            succeeded = !expectedOutOfRangeException;

            UInt16 insertedValue = TestLibrary.Generator.GetUInt16();

            try
            {
                Vector128<UInt16> result2 = value.WithElement(imm, insertedValue);
                ValidateWithResult(result2, values, insertedValue);
            }
            catch (ArgumentOutOfRangeException)
            {
                succeeded = expectedOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector128<UInt16.WithElement({imm}): {nameof(RunBasicScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        public void RunReflectionScenario(int imm = 7, bool expectedOutOfRangeException = false)
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            UInt16[] values = new UInt16[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetUInt16();
            }

            Vector128<UInt16> value = Vector128.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);

            bool succeeded = !expectedOutOfRangeException;

            try
            {
                object result = typeof(Vector128)
                                    .GetMethod(nameof(Vector128.GetElement))
                                    .MakeGenericMethod(typeof(UInt16))
                                    .Invoke(null, new object[] { value, imm });
                ValidateGetResult((UInt16)(result), values);
            }
            catch (TargetInvocationException e)
            {
                succeeded = expectedOutOfRangeException
                          && e.InnerException is ArgumentOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector128<UInt16.GetElement({imm}): {nameof(RunReflectionScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }

            succeeded = !expectedOutOfRangeException;

            UInt16 insertedValue = TestLibrary.Generator.GetUInt16();

            try
            {
                object result2 = typeof(Vector128)
                                    .GetMethod(nameof(Vector128.WithElement))
                                    .MakeGenericMethod(typeof(UInt16))
                                    .Invoke(null, new object[] { value, imm, insertedValue });
                ValidateWithResult((Vector128<UInt16>)(result2), values, insertedValue);
            }
            catch (TargetInvocationException e)
            {
                succeeded = expectedOutOfRangeException
                          && e.InnerException is ArgumentOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector128<UInt16.WithElement({imm}): {nameof(RunReflectionScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        public void RunArgumentOutOfRangeScenario()
        {
            RunBasicScenario(7 - ElementCount, expectedOutOfRangeException: true);
            RunBasicScenario(7 + ElementCount, expectedOutOfRangeException: true);

            RunReflectionScenario(7 - ElementCount, expectedOutOfRangeException: true);
            RunReflectionScenario(7 + ElementCount, expectedOutOfRangeException: true);
        }

        private void ValidateGetResult(UInt16 result, UInt16[] values, [CallerMemberName] string method = "")
        {
            if (result != values[7])
            {
                Succeeded = false;

                TestLibrary.TestFramework.LogInformation($"Vector128<UInt16.GetElement(7): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({result})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }

        private void ValidateWithResult(Vector128<UInt16> result, UInt16[] values, UInt16 insertedValue, [CallerMemberName] string method = "")
        {
            UInt16[] resultElements = new UInt16[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref resultElements[0]), result);
            ValidateWithResult(resultElements, values, insertedValue, method);
        }

        private void ValidateWithResult(UInt16[] result, UInt16[] values, UInt16 insertedValue, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < ElementCount; i++)
            {
                if ((i != 7) && (result[i] != values[i]))
                {
                    succeeded = false;
                    break;
                }
            }

            if (result[7] != insertedValue)
            {
                succeeded = false;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector128<UInt16.WithElement(7): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  insert: insertedValue");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
