// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
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
            s.a = return_int(false, -51);
            s.b = return_int(false, 86);
            s.c = return_int(false, 89);
            s.d = return_int(false, 56);

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


            int v1 = (((s.a + s.b) + s.c) + s.d);
            int v2 = (s.d + ((s.a + s.b) + s.c));
            int v3 = ((s.a + s.b) + s.c);
            int v4 = (s.c + (s.a + s.b));
            int v5 = (s.a + s.b);
            int v6 = (s.b + s.a);
            int v7 = (s.a + s.b);
            int v8 = (s.b + s.a);
            int v9 = (s.a + (s.b + s.c));
            int v10 = (s.b + (s.a + s.c));
            int v11 = (s.b + s.c);
            int v12 = (s.a + s.c);
            int v13 = ((s.a + s.b) + s.c);
            int v14 = (s.c + (s.a + s.b));
            int v15 = ((s.a + s.b) + (s.c + s.d));
            int v16 = (s.c + ((s.a + s.b) + s.d));
            int v17 = (s.c + s.d);
            int v18 = ((s.a + s.b) + s.d);
            int v19 = (s.a + (s.b + s.d));
            int v20 = (s.b + (s.a + s.d));
            int v21 = (s.b + s.d);
            int v22 = (s.a + s.d);
            int v23 = (((s.a + s.b) + s.c) + s.d);
            int v24 = (s.d + ((s.a + s.b) + s.c));
            if (v1 != 180)
            {
                Console.WriteLine("test0: for (((s.a+s.b)+s.c)+s.d)  failed actual value {0} ", v1);
                ret = ret + 1;
            }

            if (v2 != 180)
            {
                Console.WriteLine("test1: for (s.d+((s.a+s.b)+s.c))  failed actual value {0} ", v2);
                ret = ret + 1;
            }

            if (v3 != 124)
            {
                Console.WriteLine("test2: for ((s.a+s.b)+s.c)  failed actual value {0} ", v3);
                ret = ret + 1;
            }

            if (v4 != 124)
            {
                Console.WriteLine("test3: for (s.c+(s.a+s.b))  failed actual value {0} ", v4);
                ret = ret + 1;
            }

            if (v5 != 35)
            {
                Console.WriteLine("test4: for (s.a+s.b)  failed actual value {0} ", v5);
                ret = ret + 1;
            }

            if (v6 != 35)
            {
                Console.WriteLine("test5: for (s.b+s.a)  failed actual value {0} ", v6);
                ret = ret + 1;
            }

            if (v7 != 35)
            {
                Console.WriteLine("test6: for (s.a+s.b)  failed actual value {0} ", v7);
                ret = ret + 1;
            }

            if (v8 != 35)
            {
                Console.WriteLine("test7: for (s.b+s.a)  failed actual value {0} ", v8);
                ret = ret + 1;
            }

            if (v9 != 124)
            {
                Console.WriteLine("test8: for (s.a+(s.b+s.c))  failed actual value {0} ", v9);
                ret = ret + 1;
            }

            if (v10 != 124)
            {
                Console.WriteLine("test9: for (s.b+(s.a+s.c))  failed actual value {0} ", v10);
                ret = ret + 1;
            }

            if (v11 != 175)
            {
                Console.WriteLine("test10: for (s.b+s.c)  failed actual value {0} ", v11);
                ret = ret + 1;
            }

            if (v12 != 38)
            {
                Console.WriteLine("test11: for (s.a+s.c)  failed actual value {0} ", v12);
                ret = ret + 1;
            }

            if (v13 != 124)
            {
                Console.WriteLine("test12: for ((s.a+s.b)+s.c)  failed actual value {0} ", v13);
                ret = ret + 1;
            }

            if (v14 != 124)
            {
                Console.WriteLine("test13: for (s.c+(s.a+s.b))  failed actual value {0} ", v14);
                ret = ret + 1;
            }

            if (v15 != 180)
            {
                Console.WriteLine("test14: for ((s.a+s.b)+(s.c+s.d))  failed actual value {0} ", v15);
                ret = ret + 1;
            }

            if (v16 != 180)
            {
                Console.WriteLine("test15: for (s.c+((s.a+s.b)+s.d))  failed actual value {0} ", v16);
                ret = ret + 1;
            }

            if (v17 != 145)
            {
                Console.WriteLine("test16: for (s.c+s.d)  failed actual value {0} ", v17);
                ret = ret + 1;
            }

            if (v18 != 91)
            {
                Console.WriteLine("test17: for ((s.a+s.b)+s.d)  failed actual value {0} ", v18);
                ret = ret + 1;
            }

            if (v19 != 91)
            {
                Console.WriteLine("test18: for (s.a+(s.b+s.d))  failed actual value {0} ", v19);
                ret = ret + 1;
            }

            if (v20 != 91)
            {
                Console.WriteLine("test19: for (s.b+(s.a+s.d))  failed actual value {0} ", v20);
                ret = ret + 1;
            }

            if (v21 != 142)
            {
                Console.WriteLine("test20: for (s.b+s.d)  failed actual value {0} ", v21);
                ret = ret + 1;
            }

            if (v22 != 5)
            {
                Console.WriteLine("test21: for (s.a+s.d)  failed actual value {0} ", v22);
                ret = ret + 1;
            }

            if (v23 != 180)
            {
                Console.WriteLine("test22: for (((s.a+s.b)+s.c)+s.d)  failed actual value {0} ", v23);
                ret = ret + 1;
            }

            if (v24 != 180)
            {
                Console.WriteLine("test23: for (s.d+((s.a+s.b)+s.c))  failed actual value {0} ", v24);
                ret = ret + 1;
            }
#if LOOP                  
						}
					} while (ret == 1000);
#endif
#if TRY
				} finally {
				}
#endif
#if LOOP                  
			} while (ret== 1000);
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

        public volatile int a;

        public volatile int b;

        public volatile int c;

        public volatile int d;
    }
}

