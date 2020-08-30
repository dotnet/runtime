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
        private static void ZeroUInt64()
        {
            var test = new VectorZero__ZeroUInt64();

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

    public sealed unsafe class VectorZero__ZeroUInt64
    {
        private static readonly int LargestVectorSize = 16;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector128<UInt64>>() / sizeof(UInt64);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Vector128<UInt64> result = Vector128<UInt64>.Zero;

            ValidateResult(result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            object result = typeof(Vector128<UInt64>)
                                .GetProperty(nameof(Vector128<UInt64>.Zero), new Type[] { })
                                .GetGetMethod()
                                .Invoke(null, new object[] { });

            ValidateResult((Vector128<UInt64>)(result));
        }

        private void ValidateResult(Vector128<UInt64> result, [CallerMemberName] string method = "")
        {
            UInt64[] resultElements = new UInt64[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<UInt64, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, method);
        }

        private void ValidateResult(UInt64[] resultElements, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector128.Zero(UInt64): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
