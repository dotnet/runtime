// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
//extern("msvcrt.dll:printf") int printf(const char *fmt, ...);
//unsigned int _exception_code();

namespace X
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class Y
    {
        /*
        int     filt(unsigned a)
        {
            Console.WriteLine("Exception code = " + a);
            return  1;
        }
        */

        internal static void bomb()
        {
            char[] p = null;
            p[0] = (char)0;
        }

        [Fact]
        public static int TestEntryPoint()
        {
            UInt32 ec;
            ec = (UInt32)0;
            Console.WriteLine("Starting up.");
            try
            {
                bomb();
            }
            //except(filt(ec = _exception_code()))
            catch (NullReferenceException)
            {
                ec = (UInt32)1;
                Console.WriteLine("Caught the exception [code = " + ec + "]");
            }

            if (ec == 0)
            {
                Console.WriteLine("Failed.");
                return 1;
            }

            Console.WriteLine("Passed.");
            return 100;
        }
    }
}
