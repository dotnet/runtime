// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Test1()
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
            return k + 93;

        }

        public static Str2 Cast(Str1 s1)
        {
            return Unsafe.As<Str1, Str2>(ref s1);
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Test2()
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

            // The code protecting from the bug in Test1 was supposed to put
            // `lvOverlappingFields` on the source `LclVar` of reinterpritation copy,
            // but if we do the cast using another call and a return buffer it is incorrectly 
            // set the flag on the destination `LclVar`, so if the source is copy propagated 
            // instead of the destination the result node loses the flag.
            Str2 str2 = Cast(str1);

            Str2 str3 = str2;

            int k = str3.j2;
            return k + 93;

        }


        public static int Main()
        {
            bool passed = true;
            int r1 = Test1();
            if (r1 != 100)
            {
                Console.WriteLine("Test1 failed");
                passed = false;
            }

            int r2 = Test2();
            if (r2 != 100)
            {
                Console.WriteLine("Test2 failed");
                passed = false;
            }

            if (passed)
            {
                return 100;
            }
            return 101;

        }

    }
}
