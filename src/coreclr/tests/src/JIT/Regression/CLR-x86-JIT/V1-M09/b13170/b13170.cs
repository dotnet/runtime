// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

/**
 * A simple Com+ application.
 */
namespace DefaultNamespace
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class SafeCStep
    {
        /**
         * The main entry point for the application.
         *
         * @param args Array of parameters passed to the application
         * via the command line.
         */
        public static int Main(String[] args)
        {
            int i = 10;
            int j = i - 5;              // j = 5
            int sum = 0;

            sum = Add(i, j);            // sum = 15
            sum = Add(sum, i);          // sum = 25
            sum = Add(j, sum);          // sum = 30

            i = 10;
            j = sum / i;                // j = 3
            sum = Add(40, j);  // sum = 43

            x();
            return 100;
        }

        public static int Add(int a, int b)
        {
            int c = a + b;

            return c;
        }

        public static void x()
        {
            int foo;

            foo
               =
               1;

            foo =
               2;

            foo = 3;

            if (y() && z())
            {
                w();
                w();
                w();
            }

            if (y() && z())
            {
                w();
                w();
                w();
            }
        }

        public static bool y()
        {
            int a = 1;
            int b = 2;
            int c = 3;
            int d = 4;
            int e = 5;
            int f = a +
                    b +
                    c +
                    d +
                    e;

            return (true);
        }

        public static bool z()
        {
            for (int i = 0; i < 10; i++)
                w();

            return (true);
        }

        public static void w()
        {
            int a = 1;
            int b = 2;
            int c = 3;
            int d = 4;
            int e = 5;
            int dummy = a + b + c + d + e;
        }
    }
}
