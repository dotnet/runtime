// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/******************************************************************************
 * This file is auto-generated from a template file by the GenerateTests.csx  *
 * script in tests\src\JIT\HardwareIntrinsics\General\Shared. In order to make    *
 * changes, please update the corresponding template and run according to the *
 * directions listed in the file.                                             *
 ******************************************************************************/

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace JIT.HardwareIntrinsics.General
{
    public static partial class Program
    {
        private unsafe static void ToStringUInt32()
        {
            int size = Unsafe.SizeOf<Vector256<UInt32>>() / sizeof(UInt32);
            UInt32[] values = new UInt32[size];

            for (int i = 0; i < size; i++)
            {
                values[i] = TestLibrary.Generator.GetUInt32();
            }
            
            Vector256<UInt32> vector = Vector256.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            string actual = vector.ToString();

            string expected = '<' + string.Join(", ", values.Select(x => x.ToString("G", System.Globalization.CultureInfo.InvariantCulture))) + '>';

            bool succeeded = string.Equals(expected, actual, StringComparison.Ordinal);

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256UInt32ToString: Vector256<UInt32>.ToString() returned an unexpected result.");
                TestLibrary.TestFramework.LogInformation($"Expected: {expected}");
                TestLibrary.TestFramework.LogInformation($"Actual: {actual}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }
}
