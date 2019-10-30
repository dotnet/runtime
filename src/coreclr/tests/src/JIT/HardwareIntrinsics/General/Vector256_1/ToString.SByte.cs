// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private unsafe static void ToStringSByte()
        {
            int size = Unsafe.SizeOf<Vector256<SByte>>() / sizeof(SByte);
            SByte[] values = new SByte[size];

            for (int i = 0; i < size; i++)
            {
                values[i] = TestLibrary.Generator.GetSByte();
            }
            
            Vector256<SByte> vector = Vector256.Create(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7], values[8], values[9], values[10], values[11], values[12], values[13], values[14], values[15], values[16], values[17], values[18], values[19], values[20], values[21], values[22], values[23], values[24], values[25], values[26], values[27], values[28], values[29], values[30], values[31]);
            string actual = vector.ToString();

            string expected = '<' + string.Join(", ", values.Select(x => x.ToString("G", System.Globalization.CultureInfo.InvariantCulture))) + '>';

            bool succeeded = string.Equals(expected, actual, StringComparison.Ordinal);

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector256SByteToString: Vector256<SByte>.ToString() returned an unexpected result.");
                TestLibrary.TestFramework.LogInformation($"Expected: {expected}");
                TestLibrary.TestFramework.LogInformation($"Actual: {actual}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }
}
