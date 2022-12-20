// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_test1_cs
{
    public class Base
    {
        public double m;
        public Base() { m = 1.0; }

        public virtual Base[] Clone(int numOfCopies)
        {
            Base[] arr = new Base[numOfCopies];
            for (int L = 0; L < numOfCopies; L++)
                arr[L] = new Base();
            return arr;
        }
    }

    public class Derived : Base
    {
        public Derived() { m = 2.0; }

        public override Base[] Clone(int numOfCopies)
        {
            Derived[] arr = new Derived[numOfCopies];
            for (int L = 0; L < numOfCopies; L++)
                arr[L] = new Derived();
            return arr;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            Base bas = new Derived();
            bas = bas.Clone(11)[10];
            if (bas.m != 2.0)
            {
                Console.WriteLine("FAILED");
                return 1;
            }
            Derived derived = (Derived)bas;
            bas = derived.Clone(11)[10];
            if (bas.m != 2.0)
            {
                Console.WriteLine("FAILED");
                return 1;
            }
            Console.WriteLine("PASSED");
            return 100;
        }
    }
}
