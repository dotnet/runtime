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
        private unsafe static void ToStringInt16()
        {
            int size = Unsafe.SizeOf<Vector64<Int16>>() / sizeof(Int16);
            Int16[] values = new Int16[size];

            for (int i = 0; i < size; i++)
            {
                values[i] = TestLibrary.Generator.GetInt16();
            }
            
            Vector64<Int16> vector = Vector64.Create(values[0], values[1], values[2], values[3]);
            string actual = vector.ToString();

            string expected = '<' + string.Join(", ", values.Select(x => x.ToString("G", System.Globalization.CultureInfo.InvariantCulture))) + '>';

            bool succeeded = string.Equals(expected, actual, StringComparison.Ordinal);

            if (!succeeded)
            {
                TestLibrary.TestFramework.LogInformation($"Vector64Int16ToString: Vector64<Int16>.ToString() returned an unexpected result.");
                TestLibrary.TestFramework.LogInformation($"Expected: {expected}");
                TestLibrary.TestFramework.LogInformation($"Actual: {actual}");
                TestLibrary.TestFramework.LogInformation(string.Empty);

                throw new Exception("One or more scenarios did not complete as expected.");
            }
        }
    }
}
