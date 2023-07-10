// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
//((a+b)+c)

//permutations for  ((a+b)+c)
//((a+b)+c)
//(c+(a+b))
//(a+b)
//(b+a)
//a
//b
//(b+a)
//(a+b)
//c
//(a+(b+c))
//(b+(a+c))
//(b+c)
//(c+b)
//b
//c
//(c+b)
//(b+c)
//(a+c)
//(c+a)
//a
//c
//(c+a)
//(a+c)
//(c+(a+b))
//((a+b)+c)
namespace CseTest
{
    using System;


    public class Test_Main
    {

        [Fact]
        public static int TestEntryPoint()
        {
            int ret = 100;
            int b = return_int(false, 6);
            int c = return_int(false, -40);
            int a = return_int(false, -27);
            int v;
#if LOOP
			 for (int j = 0; j < 6; j++) {
				 for (int i = 0; i < 5; i++) {
#endif
            v = ((a + b) + c);
            if (v != -61)
            {
                Console.WriteLine("test0: for ((a+b)+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c + (a + b));
            if (v != -61)
            {
                Console.WriteLine("test1: for (c+(a+b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a + b);
            if (v != -21)
            {
                Console.WriteLine("test2: for (a+b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b + a);
            if (v != -21)
            {
                Console.WriteLine("test3: for (b+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
#if KILL1
				 c = return_int(false, -40);
#endif
					 }
#if KILL2
				 a = return_int(false, -27);
#endif
				 }
#endif
#if TRY 
					 try {
#endif
            v = (b + a);
            if (v != -21)
            {
                Console.WriteLine("test4: for (b+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a + b);
            if (v != -21)
            {
                Console.WriteLine("test5: for (a+b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a + (b + c));
            if (v != -61)
            {
                Console.WriteLine("test6: for (a+(b+c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b + (a + c));
            if (v != -61)
            {
                Console.WriteLine("test7: for (b+(a+c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b + c);
            if (v != -34)
            {
                Console.WriteLine("test8: for (b+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c + b);
            if (v != -34)
            {
                Console.WriteLine("test9: for (c+b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c + b);
            if (v != -34)
            {
                Console.WriteLine("test10: for (c+b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b + c);
            if (v != -34)
            {
                Console.WriteLine("test11: for (b+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a + c);
            if (v != -67)
            {
                Console.WriteLine("test12: for (a+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c + a);
            if (v != -67)
            {
                Console.WriteLine("test13: for (c+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c + a);
            if (v != -67)
            {
                Console.WriteLine("test14: for (c+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            c = return_int(false, -71);
#if LOOP
			for (int i = 0; i < 4; i++) {
#endif
            v = (a + c);
            if (v != -98)
            {
                Console.WriteLine("test15: for (a+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP

			}
#endif
#if TRY
					 } finally {
#endif
            v = (c + (a + b));
            if (v != -92)
            {
                Console.WriteLine("test16: for (c+(a+b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a + b) + c);
            if (v != -92)
            {
                Console.WriteLine("test17: for ((a+b)+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY 
					 }
#endif
            Console.WriteLine(ret);
            return ret;

        }

        private static int return_int(bool verbose, int input)
        {
            int ans;
            try
            {
                ans = input;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning  : ans");
                }
            }
            return ans;
        }
    }
}

