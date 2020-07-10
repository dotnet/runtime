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
        private static void AllBitsSetByte()
        {
            var test = new VectorAllBitsSet__AllBitsSetByte();

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

    public sealed unsafe class VectorAllBitsSet__AllBitsSetByte
    {
        private static readonly int LargestVectorSize = 32;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector256<Byte>>() / sizeof(Byte);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            Vector256<Byte> result = Vector256<Byte>.AllBitsSet;

            ValidateResult(result);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            object result = typeof(Vector256<Byte>)
                                .GetProperty(nameof(Vector256<Byte>.AllBitsSet), new Type[] { })
                                .GetGetMethod()
                                .Invoke(null, new object[] { });

            ValidateResult((Vector256<Byte>)(result));
        }

        private void ValidateResult(Vector256<Byte> result, [CallerMemberName] string method = "")
        {
            Byte[] resultElements = new Byte[ElementCount];
            Unsafe.WriteUnaligned(ref Unsafe.As<Byte, byte>(ref resultElements[0]), result);
            ValidateResult(resultElements, method);
        }

        private unsafe void ValidateResult(Byte[] resultElements, [CallerMemberName] string method = "")
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
                TestLibrary.TestFramework.LogInformation($"Vector256.AllBitsSet(Byte): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  result: ({string.Join(", ", resultElements)})");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }

        private unsafe bool HasAllBitsSet(Byte value)
        {
            for (int i = 0; i < sizeof(Byte); i++)
            {
                if (((byte*)&value)[i] != 0xFF)
                    return false;
            }
            return true;
        }
    }
}
