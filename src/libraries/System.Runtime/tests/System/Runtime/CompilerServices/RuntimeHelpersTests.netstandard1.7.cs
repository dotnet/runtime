// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.CompilerServices.Tests
{
    public struct Age
    {
        public int years;
        public int months;
    }

    public class FixedClass
    {
        [FixedAddressValueType]
        public static Age FixedAge;

        public static unsafe IntPtr AddressOfFixedAge()
        {
            fixed (Age* pointer = &FixedAge)
            {
                return (IntPtr)pointer;
            }
        }
    }

    public static partial class RuntimeHelpersTests
    {
        [Fact]
        public static void FixedAddressValueTypeTest()
        {
            // Get addresses of static Age fields.
            IntPtr fixedPtr1 = FixedClass.AddressOfFixedAge();

            // Garbage collection.
            GC.Collect(3, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();

            // Get addresses of static Age fields after garbage collection.
            IntPtr fixedPtr2 = FixedClass.AddressOfFixedAge();

            Assert.Equal(fixedPtr1, fixedPtr2);
        }
    }
}
