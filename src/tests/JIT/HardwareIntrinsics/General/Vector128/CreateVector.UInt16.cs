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
        private static void CreateVectorUInt16()
        {
            var test = new VectorCreate__CreateVectorUInt16();

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

    public sealed unsafe class VectorCreate__CreateVectorUInt16
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector128<UInt16>>() / sizeof(UInt16);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            UInt16 lowerValue = TestLibrary.Generator.GetUInt16();
            Vector64<UInt16> lower = Vector64.Create(lowerValue);

            UInt16 upperValue = TestLibrary.Generator.GetUInt16();
            Vector64<UInt16> upper = Vector64.Create(upperValue);

            Vector128<UInt16> result = Vector128.Create(lower, upper);

            ValidateResult(result, lowerValue, upperValue);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            UInt16 lowerValue = TestLibrary.Generator.GetUInt16();
            Vector64<UInt16> lower = Vector64.Create(lowerValue);

            UInt16 upperValue = TestLibrary.Generator.GetUInt16();
            Vector64<UInt16> upper = Vector64.Create(upperValue);

            object result = typeof(Vector128)
                                .GetMethod(nameof(Vector128.Create), new Type[] { typeof(Vector64<UInt16>), typeof(Vector64<UInt16>) })
                                .Invoke(null, new object[] { lower, upper });

            ValidateResult((Vector128<UInt16>)(result), lowerValue, upperValue);
        }

        private void ValidateResult(Vector128<UInt16> result, UInt16 expectedLowerValue, UInt16 expectedUpperValue, [CallerMemberName] string method = "")
        {
            UInt16[] resultElements = new UInt16[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedLowerValue, expectedUpperValue, method);
        }

        private void ValidateResult(UInt16[] resultElements, UInt16 expectedLowerValue, UInt16 expectedUpperValue, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128.Create(UInt16): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   lower: {expectedLowerValue}");
                TestLibrary.TestFramework.LogInformation($"   upper: {expectedUpperValue}");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
