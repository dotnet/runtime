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
        private static void AllBitsSetSingle()
        {
            var test = new VectorAllBitsSet__AllBitsSetSingle();

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

    public sealed unsafe class VectorAllBitsSet__AllBitsSetSingle
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Single>>() / sizeof(Single);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Vector256<Single> result = Vector256<Single>.AllBitsSet;

            ValidateResult(result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            object result = typeof(Vector256<Single>)
                                .GetProperty(nameof(Vector256<Single>.AllBitsSet), new Type[] { })
                                .GetGetMethod()
                                .Invoke(null, new object[] { });

            ValidateResult((Vector256<Single>)(result));
        }

        private void ValidateResult(Vector256<Single> result, [CallerMemberName] string method = "")
        {
            Single[] resultElements = new Single[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Single, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, method);
        }

        private unsafe void ValidateResult(Single[] resultElements, [CallerMemberName] string method = "")
        {
            bool succeeded = true;
            for (var i = 0; i < ElementCount; i++)
            {
                if (!HasAllBitsSet(resultElements[i]))
                {
                    succeeded = false;
                    break;
                }
            }

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256.AllBitsSet(Single): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        private unsafe bool HasAllBitsSet(Single value)
        {
            for (int i = 0; i < sizeof(Single); i++)
            {
                if (((byte*)&value)[i] != 0xFF)
                    return false;
            }
            return true;
        }
    }
}
