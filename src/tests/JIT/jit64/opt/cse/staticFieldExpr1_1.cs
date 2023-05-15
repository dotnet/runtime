// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
//(((class_s.a+class_s.b)+class_s.c)+class_s.d)
//permutations for  (((class_s.a+class_s.b)+class_s.c)+class_s.d)
//(((class_s.a+class_s.b)+class_s.c)+class_s.d)
//(class_s.d+((class_s.a+class_s.b)+class_s.c))
//((class_s.a+class_s.b)+class_s.c)
//(class_s.c+(class_s.a+class_s.b))
//(class_s.a+class_s.b)
//(class_s.b+class_s.a)
//(class_s.a+class_s.b)
//(class_s.b+class_s.a)
//(class_s.a+(class_s.b+class_s.c))
//(class_s.b+(class_s.a+class_s.c))
//(class_s.b+class_s.c)
//(class_s.a+class_s.c)
//((class_s.a+class_s.b)+class_s.c)
//(class_s.c+(class_s.a+class_s.b))
//((class_s.a+class_s.b)+(class_s.c+class_s.d))
//(class_s.c+((class_s.a+class_s.b)+class_s.d))
//(class_s.c+class_s.d)
//((class_s.a+class_s.b)+class_s.d)
//(class_s.a+(class_s.b+class_s.d))
//(class_s.b+(class_s.a+class_s.d))
//(class_s.b+class_s.d)
//(class_s.a+class_s.d)
//(((class_s.a+class_s.b)+class_s.c)+class_s.d)
//(class_s.d+((class_s.a+class_s.b)+class_s.c))
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

            class_s.a = return_int(false, 32);
            class_s.b = return_int(false, 77);
            class_s.c = return_int(false, 7);
            class_s.d = return_int(false, 33);

            int v = 0;

#if LOOP         
			
			do {
#endif
#if TRY
				try {
#endif
#if LOOP  
					do {
						for (int i = 0; i < 10; i++) {
#endif

            v = (((class_s.a + class_s.b) + class_s.c) + class_s.d);
            if (v != 149)
            {
                Console.WriteLine("test0: for (((class_s.a+class_s.b)+class_s.c)+class_s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.d + ((class_s.a + class_s.b) + class_s.c));
            if (v != 149)
            {
                Console.WriteLine("test1: for (class_s.d+((class_s.a+class_s.b)+class_s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((class_s.a + class_s.b) + class_s.c);
            if (v != 116)
            {
                Console.WriteLine("test2: for ((class_s.a+class_s.b)+class_s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.c + (class_s.a + class_s.b));
            if (v != 116)
            {
                Console.WriteLine("test3: for (class_s.c+(class_s.a+class_s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.a + class_s.b);
            if (v != 109)
            {
                Console.WriteLine("test4: for (class_s.a+class_s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.b + class_s.a);
            if (v != 109)
            {
                Console.WriteLine("test5: for (class_s.b+class_s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.a + class_s.b);
            if (v != 109)
            {
                Console.WriteLine("test6: for (class_s.a+class_s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.b + class_s.a);
            if (v != 109)
            {
                Console.WriteLine("test7: for (class_s.b+class_s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.a + (class_s.b + class_s.c));
            if (v != 116)
            {
                Console.WriteLine("test8: for (class_s.a+(class_s.b+class_s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.b + (class_s.a + class_s.c));
            if (v != 116)
            {
                Console.WriteLine("test9: for (class_s.b+(class_s.a+class_s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.b + class_s.c);
            if (v != 84)
            {
                Console.WriteLine("test10: for (class_s.b+class_s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.a + class_s.c);
            if (v != 39)
            {
                Console.WriteLine("test11: for (class_s.a+class_s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((class_s.a + class_s.b) + class_s.c);
            if (v != 116)
            {
                Console.WriteLine("test12: for ((class_s.a+class_s.b)+class_s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.c + (class_s.a + class_s.b));
            if (v != 116)
            {
                Console.WriteLine("test13: for (class_s.c+(class_s.a+class_s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((class_s.a + class_s.b) + (class_s.c + class_s.d));
            if (v != 149)
            {
                Console.WriteLine("test14: for ((class_s.a+class_s.b)+(class_s.c+class_s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.c + ((class_s.a + class_s.b) + class_s.d));
            if (v != 149)
            {
                Console.WriteLine("test15: for (class_s.c+((class_s.a+class_s.b)+class_s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.c + class_s.d);
            if (v != 40)
            {
                Console.WriteLine("test16: for (class_s.c+class_s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((class_s.a + class_s.b) + class_s.d);
            if (v != 142)
            {
                Console.WriteLine("test17: for ((class_s.a+class_s.b)+class_s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.a + (class_s.b + class_s.d));
            if (v != 142)
            {
                Console.WriteLine("test18: for (class_s.a+(class_s.b+class_s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.b + (class_s.a + class_s.d));
            if (v != 142)
            {
                Console.WriteLine("test19: for (class_s.b+(class_s.a+class_s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.b + class_s.d);
            if (v != 110)
            {
                Console.WriteLine("test20: for (class_s.b+class_s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.a + class_s.d);
            if (v != 65)
            {
                Console.WriteLine("test21: for (class_s.a+class_s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((class_s.a + class_s.b) + class_s.c) + class_s.d);
            if (v != 149)
            {
                Console.WriteLine("test22: for (((class_s.a+class_s.b)+class_s.c)+class_s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (class_s.d + ((class_s.a + class_s.b) + class_s.c));
            if (v != 149)
            {
                Console.WriteLine("test23: for (class_s.d+((class_s.a+class_s.b)+class_s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            class_s.d = return_int(false, 33);

#if LOOP                  
						}
					} while (v == 0);
#endif
#if TRY
				} finally {
				}
#endif
#if LOOP                  
			} while (v== 0);
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
        public static int a;
        public static int b;
        public static int c;
        public static int d;
    }
}

