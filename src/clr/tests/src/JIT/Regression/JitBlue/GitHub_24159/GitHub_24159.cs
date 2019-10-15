// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace GitHub_24159
{

    public struct Str1
    {
        public int i1;
        public int i2;
        public int i3;
        public int i4;
        public int i5;
    }

    public struct Str2
    {
        public int j1;
        public int j2;
        public int j3;
        public int j4;
        public int j5;
    }

    class Test
    {
        static int i;

        public static int Main()
        {
            i = 0;

            Str1 str1 = new Str1();

            if (i != 0)
            {
                str1 = new Str1();
            }
            else
            {
                str1.i2 = 7;
            }

            // This call reinterprets the struct.
            Str2 str2 = Unsafe.As<Str1, Str2>(ref str1);

            // The bug was that value numbering couldn't recognize
            // that this field has been updated on str1.
            int k = str2.j2;

            Console.WriteLine(k);

            return k + 93;

        }
    }
}
