// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using Xunit;

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

public class Example
{
    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 1000; i++)
        {
            IntPtr fixedPtr1 = FixedClass.AddressOfFixedAge();

            // Garbage collection.
            GC.Collect(3, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();

            // Get addresses of static Age fields after garbage collection.
            IntPtr fixedPtr2 = FixedClass.AddressOfFixedAge();

            if (fixedPtr1 != fixedPtr2)
            {
                return -1;
            }
        }
        return 100;
    }
}
