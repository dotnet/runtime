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
        private static void ToScalarUInt32()
        {
            var test = new VectorToScalar__ToScalarUInt32();

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

    public sealed unsafe class VectorToScalar__ToScalarUInt32
    {
        private static readonly int LargestVectorSize = 8;

        private static readonly int ElementCount = Unsafe.SizeOf<Vector64<UInt32>>() / sizeof(UInt32);

        public bool Succeeded { get; set; } = true;

        public void RunBasicScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunBasicScenario));

            UInt32[] values = new UInt32[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetUInt32();
            }

            Vector64<UInt32> value = Vector64.Create(values[0], values[1]);

            UInt32 result = value.ToScalar();
            ValidateResult(result, values);
        }

        public void RunReflectionScenario()
        {
            TestLibrary.TestFramework.BeginScenario(nameof(RunReflectionScenario));

            UInt32[] values = new UInt32[ElementCount];

            for (int i = 0; i < ElementCount; i++)
            {
                values[i] = TestLibrary.Generator.GetUInt32();
            }

            Vector64<UInt32> value = Vector64.Create(values[0], values[1]);

            object result = typeof(Vector64)
                                .GetMethod(nameof(Vector64.ToScalar))
                                .MakeGenericMethod(typeof(UInt32))
                                .Invoke(null, new object[] { value });

            ValidateResult((UInt32)(result), values);
        }

        private void ValidateResult(UInt32 result, UInt32[] values, [CallerMemberName] string method = "")
        {
            if (result != values[0])
            {
                TestLibrary.TestFramework.LogInformation($"Vector64<UInt32>.ToScalar(): {method} failed:");
                TestLibrary.TestFramework.LogInformation($"  values: ({string.Join(", ", values)})");
                TestLibrary.TestFramework.LogInformation($"  result: {result}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                Succeeded = false;
            }
        }
    }
}
