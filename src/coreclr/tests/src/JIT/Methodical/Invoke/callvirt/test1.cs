// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Test
{
    internal class Base
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

    internal class Derived : Base
    {
        public Derived() { m = 2.0; }

        public override Base[] Clone(int numOfCopies)
        {
            Derived[] arr = new Derived[numOfCopies];
            for (int L = 0; L < numOfCopies; L++)
                arr[L] = new Derived();
            return arr;
        }

        private static int Main()
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
