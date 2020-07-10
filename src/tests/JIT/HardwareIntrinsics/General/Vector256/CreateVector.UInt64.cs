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
        private static void CreateVectorUInt64()
        {
            var test = new VectorCreate__CreateVectorUInt64();

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

    public sealed unsafe class VectorCreate__CreateVectorUInt64
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<UInt64>>() / sizeof(UInt64);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            UInt64 lowerValue = TestLibrary.Generator.GetUInt64();
            Vector128<UInt64> lower = Vector128.Create(lowerValue);

            UInt64 upperValue = TestLibrary.Generator.GetUInt64();
            Vector128<UInt64> upper = Vector128.Create(upperValue);

            Vector256<UInt64> result = Vector256.Create(lower, upper);

            ValidateResult(result, lowerValue, upperValue);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            UInt64 lowerValue = TestLibrary.Generator.GetUInt64();
            Vector128<UInt64> lower = Vector128.Create(lowerValue);

            UInt64 upperValue = TestLibrary.Generator.GetUInt64();
            Vector128<UInt64> upper = Vector128.Create(upperValue);

            object result = typeof(Vector256)
                                .GetMethod(nameof(Vector256.Create), new Type[] { typeof(Vector128<UInt64>), typeof(Vector128<UInt64>) })
                                .Invoke(null, new object[] { lower, upper });

            ValidateResult((Vector256<UInt64>)(result), lowerValue, upperValue);
        }

        private void ValidateResult(Vector256<UInt64> result, UInt64 expectedLowerValue, UInt64 expectedUpperValue, [CallerMemberName] string method = "")
        {
            UInt64[] resultElements = new UInt64[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedLowerValue, expectedUpperValue, method);
        }

        private void ValidateResult(UInt64[] resultElements, UInt64 expectedLowerValue, UInt64 expectedUpperValue, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector256.Create(UInt64): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   lower: {expectedLowerValue}");
                TestLibrary.TestFramework.LogInformation($"   upper: {expectedUpperValue}");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
