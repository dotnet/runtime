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

namespace JIT.HardwareIntrinsics.General._Vector64_1
{
    public static partial class Program
    {
        [Fact]
        public static void ZeroUInt16()
        {
            var test = new VectorZero__ZeroUInt16();

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

    public sealed unsafe class VectorZero__ZeroUInt16
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<UInt16>>() / sizeof(UInt16);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Vector64<UInt16> result = Vector64<UInt16>.Zero;

            ValidateResult(result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            object result = typeof(Vector64<UInt16>)
                                .GetProperty(nameof(Vector64<UInt16>.Zero), new Type[] { })
                                .GetGetMethod()
                                .Invoke(null, new object[] { });

            ValidateResult((Vector64<UInt16>)(result));
        }

        private void ValidateResult(Vector64<UInt16> result, [CallerMemberName] string method = "")
        {
            UInt16[] resultElements = new UInt16[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt16, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, method);
        }

        private void ValidateResult(UInt16[] resultElements, [CallerMemberName] string method = "")
        {
            bool succeeded = true;
        
            for (var i = 0; i < ElementCount; i++)
            {
                if (resultElements[i] != 0)
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64.Zero(UInt16): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
