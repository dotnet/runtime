// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace X
{
    //@BEGINRENAME; Verify this renames
    //@ENDRENAME; Verify this renames
    using System;

    public class Y
    {

        //extern("msvcrt.dll:printf") int printf(const char *fmt, ...);
        //UInt32 int _exception_code();

        /*
        public static int     filt(UInt32 a)
        {
            Console.WriteLine("Exception code = " + a);
            return  1;
        }
		
        public static int     filt0(UInt32 a)
        {
            Console.WriteLine("Exception code = " + a);
            return  0;
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
            UInt32 ec, ec1;

            ec = (UInt32)0;
            ec1 = (UInt32)0;

            try
            {
                try
                {
                    bomb();
                }
                //except(filt(ec = _exception_code()))
                catch (NullReferenceException e)
                {
                    ec = (UInt32)1;
                    Console.WriteLine("Caught the exception once, now throwing again.");
                    throw e;
                }

            }
            //except(filt(ec1 = _exception_code()))
            catch (NullReferenceException /*e1*/)
            {
                ec1 = (UInt32)2;
                Console.WriteLine("'Outer' catch handler");
                Console.WriteLine("Caught the exception [code1 = " + ec + "] [code2 = " + ec1 + "]");
            }
            //    printf("Caught the exception [code1 = %08X] [code2 = %08X]\n", ec, ec1);
            if ((ec != 0) && (ec1 != 0))
            {
                Console.WriteLine("Passed.");
                return 100;
            }
            Console.WriteLine("Failed.");
            return 1;
        }
    }
}
