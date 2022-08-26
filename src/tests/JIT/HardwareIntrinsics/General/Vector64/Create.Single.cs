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

namespace JIT.HardwareIntrinsics.General._Vector64
{
    public static partial class Program
    {
        [Fact]
        public static void CreateSingle()
        {
            var test = new VectorCreate__CreateSingle();

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

    public sealed unsafe class VectorCreate__CreateSingle
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<Single>>() / sizeof(Single);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Single value = TestLibrary.Generator.GetSingle();
            Vector64<Single> result = Vector64.Create(value);

            ValidateResult(result, value);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            Single value = TestLibrary.Generator.GetSingle();
            object result = typeof(Vector64)
                                .GetMethod(nameof(Vector64.Create), new Type[] { typeof(Single) })
                                .Invoke(null, new object[] { value });

            ValidateResult((Vector64<Single>)(result), value);
        }

        private void ValidateResult(Vector64<Single> result, Single expectedValue, [CallerMemberName] string method = "")
        {
            Single[] resultElements = new Single[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, expectedValue, method);
        }

        private void ValidateResult(Single[] resultElements, Single expectedValue, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector64.Create(Single): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"   value: {expectedValue}");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
