// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//extern("msvcrt.dll:printf") int printf(const char *fmt, ...);
//unsigned int _exception_code();

namespace X
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    class Y
    {
        /*
        int     filt(unsigned a)
        {
            Console.WriteLine("Exception code = " + a);
            return  1;
        }
        */

        public static void bomb()
        {
            char[] p = null;
            p[0] = (char)0;
        }

        public static int Main(String[] args)
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
