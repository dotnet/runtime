// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
//((s.a+s.b)+s.c)
//permutations for  ((s.a+s.b)+s.c)
//((s.a+s.b)+s.c)
//(s.c+(s.a+s.b))
//(s.a+s.b)
//(s.b+s.a)
//s.a
//s.b
//(s.b+s.a)
//(s.a+s.b)
//s.c
//(s.a+(s.b+s.c))
//(s.b+(s.a+s.c))
//(s.b+s.c)
//(s.c+s.b)
//s.b
//s.c
//(s.c+s.b)
//(s.b+s.c)
//(s.a+s.c)
//(s.c+s.a)
//s.a
//s.c
//(s.c+s.a)
//(s.a+s.c)
//(s.c+(s.a+s.b))
//((s.a+s.b)+s.c)
namespace CseTest
{
    using System;
    public class Test_Main
    {
        [Fact]
        public static int TestEntryPoint()
        {
            int ret = 100;
            class_s s = new class_s();
            s.a = return_int(false, -62);
            s.b = return_int(false, -4);
            s.c = return_int(false, 6);
            int v;
#if LOOP            
			do {
#endif
            v = ((s.a + s.b) + s.c);
            if (v != -60)
            {
                Console.WriteLine("test0: for ((s.a+s.b)+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c + (s.a + s.b));
            if (v != -60)
            {
                Console.WriteLine("test1: for (s.c+(s.a+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + s.b);
            if (v != -66)
            {
                Console.WriteLine("test2: for (s.a+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.a);
            if (v != -66)
            {
                Console.WriteLine("test3: for (s.b+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.a);
            if (v != -66)
            {
                Console.WriteLine("test4: for (s.b+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + s.b);
            if (v != -66)
            {
                Console.WriteLine("test5: for (s.a+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

#if LOOP
				do {
#endif
            v = (s.a + (s.b + s.c));
            if (v != -60)
            {
                Console.WriteLine("test6: for (s.a+(s.b+s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + (s.a + s.c));
#if TRY               
					try {
#endif
            if (v != -60)
            {
                Console.WriteLine("test7: for (s.b+(s.a+s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.c);
            if (v != 2)
            {
                Console.WriteLine("test8: for (s.b+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
					}
					finally { 
#endif
            v = (s.c + s.b);
#if TRY                  
					}
#endif

            if (v != 2)
            {
                Console.WriteLine("test9: for (s.c+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c + s.b);
            if (v != 2)
            {
                Console.WriteLine("test10: for (s.c+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.c);
            if (v != 2)
            {
                Console.WriteLine("test11: for (s.b+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + s.c);
            if (v != -56)
            {
                Console.WriteLine("test12: for (s.a+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c + s.a);
            if (v != -56)
            {
                Console.WriteLine("test13: for (s.c+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c + s.a);
            if (v != -56)
            {
                Console.WriteLine("test14: for (s.c+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + s.c);
            if (v != -56)
            {
                Console.WriteLine("test15: for (s.a+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c + (s.a + s.b));
            if (v != -60)
            {
                Console.WriteLine("test16: for (s.c+(s.a+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP               
				} while (v == 0);
#endif

            v = ((s.a + s.b) + s.c);
            if (v != -60)
            {
                Console.WriteLine("test17: for ((s.a+s.b)+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP            
			} while (v == 0);
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
    public class class_s
    {
        public int a;
        public int b;
        public int c;
    }
}

