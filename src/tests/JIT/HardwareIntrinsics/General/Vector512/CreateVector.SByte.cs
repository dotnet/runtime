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

namespace JIT.HardwareIntrinsics.General._Vector512
{
    public static partial class Program
    {
        [Fact]
        public static void CreateVectorSByte()
        {
            var test = new VectorCreate__CreateVectorSByte();

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

    public sealed unsafe class VectorCreate__CreateVectorSByte
    {
        private static readonly int LargestVectorSize = 64;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector512<SByte>>() / sizeof(SByte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            SByte lowerValue = TestLibrary.Generator.GetSByte();
            Vector256<SByte> lower = Vector256.Create(lowerValue);

            SByte upperValue = TestLibrary.Generator.GetSByte();
            Vector256<SByte> upper = Vector256.Create(upperValue);

            Vector512<SByte> result = Vector512.Create(lower, upper);

            ValidateResult(result, lowerValue, upperValue);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            SByte lowerValue = TestLibrary.Generator.GetSByte();
            Vector256<SByte> lower = Vector256.Create(lowerValue);

            SByte upperValue = TestLibrary.Generator.GetSByte();
            Vector256<SByte> upper = Vector256.Create(upperValue);

            object result = typeof(Vector512)
                                .GetMethod(nameof(Vector512.Create), new Type[] { typeof(Vector256<SByte>), typeof(Vector256<SByte>) })
                                .Invoke(null, new object[] { lower, upper });

            ValidateResult((Vector512<SByte>)(result), lowerValue, upperValue);
        }

        private void ValidateResult(Vector512<SByte> result, SByte expectedLowerValue, SByte expectedUpperValue, [CallerMemberName] string method = "")
        {
            SByte[] resultElements = new SByte[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<SByte, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedLowerValue, expectedUpperValue, method);
        }

        private void ValidateResult(SByte[] resultElements, SByte expectedLowerValue, SByte expectedUpperValue, [CallerMemberName] string method = "")
        {
            bool succeeded = true;

            for (var i = 0; i < ElementCount / 2; i++)
            {
                if (resultElements[i] != expectedLowerValue)
                {
                    succeeded = false;
                    break;
                }
            }

            for (var i = ElementCount / 2; i < ElementCount; i++)
            {
                if (resultElements[i] != expectedUpperValue)
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector512.Create(SByte): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   lower: {expectedLowerValue}");
                TestLibrary.TestFramework.LogInformation($"   upper: {expectedUpperValue}");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
