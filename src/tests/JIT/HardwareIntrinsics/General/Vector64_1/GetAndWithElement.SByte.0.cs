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

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        private static void GetAndWithElementSByte0()
        {
            var test = new VectorGetAndWithElement__GetAndWithElementSByte0();

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

    public sealed unsafe class VectorGetAndWithElement__GetAndWithElementSByte0
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<SByte>>() / sizeof(SByte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario(int imm = 0, bool expectedOutOfRangeException = false)
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            SByte[] values = new SByte[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetSByte();
            }

            Vector64<SByte> value = Vector64.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);

            bool succeeded = !expectedOutOfRangeException;

            try
            {
                SByte result = value.GetElement(imm);
                ValidateGetResult(result, values);
            }
            catch (ArgumentOutOfRangeException)
            {
                succeeded = expectedOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<SByte.GetElement({imm}): {nameof(RunBasicScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }

            succeeded = !expectedOutOfRangeException;

            SByte insertedValue = TestLibrary.Generator.GetSByte();

            try
            {
                Vector64<SByte> result2 = value.WithElement(imm, insertedValue);
                ValidateWithResult(result2, values, insertedValue);
            }
            catch (ArgumentOutOfRangeException)
            {
                succeeded = expectedOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<SByte.WithElement({imm}): {nameof(RunBasicScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        public void RunReflectionScenario(int imm = 0, bool expectedOutOfRangeException = false)
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            SByte[] values = new SByte[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetSByte();
            }

            Vector64<SByte> value = Vector64.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);

            bool succeeded = !expectedOutOfRangeException;

            try
            {
                object result = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.GetElement))
                                    .MakeGenericMethod(typeof(SByte))
                                    .Invoke(null, new object[] { value, imm });
                ValidateGetResult((SByte)(result), values);
            }
            catch (TargetInvocationException e)
            {
                succeeded = expectedOutOfRangeException
                          && e.InnerException is ArgumentOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<SByte.GetElement({imm}): {nameof(RunReflectionScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }

            succeeded = !expectedOutOfRangeException;

            SByte insertedValue = TestLibrary.Generator.GetSByte();

            try
            {
                object result2 = typeof(Vector64)
                                    .GetMethod(nameof(Vector64.WithElement))
                                    .MakeGenericMethod(typeof(SByte))
                                    .Invoke(null, new object[] { value, imm, insertedValue });
                ValidateWithResult((Vector64<SByte>)(result2), values, insertedValue);
            }
            catch (TargetInvocationException e)
            {
                succeeded = expectedOutOfRangeException
                          && e.InnerException is ArgumentOutOfRangeException;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<SByte.WithElement({imm}): {nameof(RunReflectionScenario)} failed to throw ArgumentOutOfRangeException.");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        public void RunArgumentOutOfRangeScenario()
        {
            RunBasicScenario(0 - ElementCount, expectedOutOfRangeException: true);
            RunBasicScenario(0 + ElementCount, expectedOutOfRangeException: true);

            RunReflectionScenario(0 - ElementCount, expectedOutOfRangeException: true);
            RunReflectionScenario(0 + ElementCount, expectedOutOfRangeException: true);
        }

        private void ValidateGetResult(SByte result, SByte[] values, [CallerMemberName] string method = "")
        {
            if (result != values[0])
            {
                Succeeded = false;

                TestLibrary.TestFramework.LogInformation($"Vector64<SByte.GetElement(0): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: ({result})");
                TestLibrary.TestFramework.LogInformation(string.Empty);
            }
        }

        private void ValidateWithResult(Vector64<SByte> result, SByte[] values, SByte insertedValue, [CallerMemberName] string method = "")
        {
            SByte[] resultElements = new SByte[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref resultElements[0]), result);
            ValidateWithResult(resultElements, values, insertedValue, method);
        }

        private void ValidateWithResult(SByte[] result, SByte[] values, SByte insertedValue, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (int i = 0; i < ElementCount; i++)
            {
                if ((i != 0) && (result[i] != values[i]))
                {
                    succeeded = false;
                    break;
                }
            }

            if (result[0] != insertedValue)
            {
                succeeded = false;
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<SByte.WithElement(0): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  insert: insertedValue");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", result)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
