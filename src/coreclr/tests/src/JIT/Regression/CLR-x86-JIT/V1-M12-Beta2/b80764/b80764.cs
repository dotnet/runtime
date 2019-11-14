// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Runtime.InteropServices;


namespace JitTest
{
    internal class Test
    {
        private static unsafe void initbuf(byte* buf, int num)
        {
            for (int i = 0; i < 100; i++)
                buf[i] = (byte)i;
            Console.WriteLine("buffer " + num.ToString() + " allocated");
        }

        private static unsafe void ckbuf(byte* buf, int num)
        {
            if (buf != null)
            {
                for (int i = 0; i < 100; i++)
                {
                    if (buf[i] != (byte)i)
                    {
                        Console.WriteLine("buffer " + num.ToString() + " is garbage !!");
                        return;
                    }
                }
            }
            Console.WriteLine("buffer " + num.ToString() + " is OK");
        }

        private static unsafe int Main()
        {
            byte* buf1 = stackalloc byte[100], buf2 = null, buf3 = null;
            initbuf(buf1, 1);
            ckbuf(buf1, 1);
            try
            {
                Console.WriteLine("--- entered outer try ---");
                byte* TEMP1 = stackalloc byte[100];
                buf2 = TEMP1;
                initbuf(buf2, 2);
                ckbuf(buf1, 1);
                ckbuf(buf2, 2);
                try
                {
                    Console.WriteLine("--- entered inner try ---");
                    byte* TEMP2 = stackalloc byte[100];
                    buf3 = TEMP2;
                    initbuf(buf3, 3);
                    ckbuf(buf1, 1);
                    ckbuf(buf2, 2);
                    ckbuf(buf3, 3);
                    Console.WriteLine("--- throwing exception ---");
                    throw new Exception();
                }
                finally
                {
                    Console.WriteLine("--- finally ---");
                    ckbuf(buf1, 1);
                    ckbuf(buf2, 2);
                    ckbuf(buf3, 3);
                }
            }
            catch (Exception)
            {
                Console.WriteLine("--- catch ---");
                ckbuf(buf1, 1);
                ckbuf(buf2, 2);
                ckbuf(buf3, 3);
            }
            Console.WriteLine("--- after try-catch ---");
            ckbuf(buf1, 1);
            ckbuf(buf2, 2);
            ckbuf(buf3, 3);
            Console.WriteLine("=== TEST ENDED ===");
            return 100;
        }
    }
}
