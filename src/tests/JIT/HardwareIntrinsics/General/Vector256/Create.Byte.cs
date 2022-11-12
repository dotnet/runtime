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

namespace JIT.HardwareIntrinsics.General._Vector256
{
    public static partial class Program
    {
        [Fact]
        public static void CreateByte()
        {
            var test = new VectorCreate__CreateByte();

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

    public sealed unsafe class VectorCreate__CreateByte
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Byte>>() / sizeof(Byte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Byte value = TestLibrary.Generator.GetByte();
            Vector256<Byte> result = Vector256.Create(value);

            ValidateResult(result, value);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Byte value = TestLibrary.Generator.GetByte();
            object result = typeof(Vector256)
                                .GetMethod(nameof(Vector256.Create), new Type[] { typeof(Byte) })
                                .Invoke(null, new object[] { value });

            ValidateResult((Vector256<Byte>)(result), value);
        }

        private void ValidateResult(Vector256<Byte> result, Byte expectedValue, [CallerMemberName] string method = "")
        {
            Byte[] resultElements = new Byte[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedValue, method);
        }

        private void ValidateResult(Byte[] resultElements, Byte expectedValue, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            if (resultElements[0] != expectedValue)
            {
                succeeded = false;
            }
            else
            {
                for (var i = 1; i < ElementCount; i++)
                {
                    if (resultElements[i] != expectedValue)
                    {
                        succeeded = false;
                        break;
                    }
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256.Create(Byte): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: {expectedValue}");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
