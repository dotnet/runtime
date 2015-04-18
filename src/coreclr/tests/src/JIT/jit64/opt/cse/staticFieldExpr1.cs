// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

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

        static int Main()
        {
            int ret = 100;
            class_s s = new class_s();
            class_s.a = return_int(false, -51);
            class_s.b = return_int(false, 86);
            class_s.c = return_int(false, 89);
            class_s.d = return_int(false, 56);
            int v1 = 0;
            int v2 = 0;
            int v3 = 0;
            int v4 = 0;
            int v5 = 0;
            int v6 = 0;
            int v7 = 0;
            int v8 = 0;
            int v9 = 0;
            int v10 = 0;
            int v11 = 0;
            int v12 = 0;
            int v13 = 0;
            int v14 = 0;
            int v15 = 0;
            int v16 = 0;
            int v17 = 0;
            int v18 = 0;
#if LOOP         
			do {
            do {
#endif
#if TRY
			try{
#endif
#if LOOP  
					do {
			for (int i=0;i<10;i++){
#endif

            v1 = (((class_s.a + class_s.b) + class_s.c) + class_s.d);
            v2 = (class_s.d + ((class_s.a + class_s.b) + class_s.c));
            v3 = ((class_s.a + class_s.b) + class_s.c);
            v4 = (class_s.c + (class_s.a + class_s.b));
            v5 = (class_s.a + class_s.b);
            v6 = (class_s.b + class_s.a);
            v7 = (class_s.a + class_s.b);
            v8 = (class_s.b + class_s.a);
            v9 = (class_s.a + (class_s.b + class_s.c));
            v10 = (class_s.b + (class_s.a + class_s.c));
            v11 = (class_s.b + class_s.c);
            v12 = (class_s.a + class_s.c);
            v13 = ((class_s.a + class_s.b) + class_s.c);
            v14 = (class_s.c + (class_s.a + class_s.b));
            v15 = ((class_s.a + class_s.b) + (class_s.c + class_s.d));
            v16 = (class_s.c + ((class_s.a + class_s.b) + class_s.d));
            v17 = (class_s.c + class_s.d);
            v18 = ((class_s.a + class_s.b) + class_s.d);

#if LOOP                  
			}
					} while (v17 == 0);
#endif
#if TRY
			} finally {
			}
#endif
            int v19 = (class_s.a + (class_s.b + class_s.d));
            int v20 = (class_s.b + (class_s.a + class_s.d));
            int v21 = (class_s.b + class_s.d);
            int v22 = (class_s.a + class_s.d);
            int v23 = (((class_s.a + class_s.b) + class_s.c) + class_s.d);
            int v24 = (class_s.d + ((class_s.a + class_s.b) + class_s.c));
            if (v1 != 180)
            {
                Console.WriteLine("test0: for (((class_s.a+class_s.b)+class_s.c)+class_s.d)  failed actual value {0} ", v1);
                ret = ret + 1;
            }

            if (v2 != 180)
            {
                Console.WriteLine("test1: for (class_s.d+((class_s.a+class_s.b)+class_s.c))  failed actual value {0} ", v2);
                ret = ret + 1;
            }

            if (v3 != 124)
            {
                Console.WriteLine("test2: for ((class_s.a+class_s.b)+class_s.c)  failed actual value {0} ", v3);
                ret = ret + 1;
            }

            if (v4 != 124)
            {
                Console.WriteLine("test3: for (class_s.c+(class_s.a+class_s.b))  failed actual value {0} ", v4);
                ret = ret + 1;
            }

            if (v5 != 35)
            {
                Console.WriteLine("test4: for (class_s.a+class_s.b)  failed actual value {0} ", v5);
                ret = ret + 1;
            }

            if (v6 != 35)
            {
                Console.WriteLine("test5: for (class_s.b+class_s.a)  failed actual value {0} ", v6);
                ret = ret + 1;
            }

            if (v7 != 35)
            {
                Console.WriteLine("test6: for (class_s.a+class_s.b)  failed actual value {0} ", v7);
                ret = ret + 1;
            }

            if (v8 != 35)
            {
                Console.WriteLine("test7: for (class_s.b+class_s.a)  failed actual value {0} ", v8);
                ret = ret + 1;
            }

            if (v9 != 124)
            {
                Console.WriteLine("test8: for (class_s.a+(class_s.b+class_s.c))  failed actual value {0} ", v9);
                ret = ret + 1;
            }

            if (v10 != 124)
            {
                Console.WriteLine("test9: for (class_s.b+(class_s.a+class_s.c))  failed actual value {0} ", v10);
                ret = ret + 1;
            }

            if (v11 != 175)
            {
                Console.WriteLine("test10: for (class_s.b+class_s.c)  failed actual value {0} ", v11);
                ret = ret + 1;
            }

            if (v12 != 38)
            {
                Console.WriteLine("test11: for (class_s.a+class_s.c)  failed actual value {0} ", v12);
                ret = ret + 1;
            }

            if (v13 != 124)
            {
                Console.WriteLine("test12: for ((class_s.a+class_s.b)+class_s.c)  failed actual value {0} ", v13);
                ret = ret + 1;
            }

            if (v14 != 124)
            {
                Console.WriteLine("test13: for (class_s.c+(class_s.a+class_s.b))  failed actual value {0} ", v14);
                ret = ret + 1;
            }

            if (v15 != 180)
            {
                Console.WriteLine("test14: for ((class_s.a+class_s.b)+(class_s.c+class_s.d))  failed actual value {0} ", v15);
                ret = ret + 1;
            }

            if (v16 != 180)
            {
                Console.WriteLine("test15: for (class_s.c+((class_s.a+class_s.b)+class_s.d))  failed actual value {0} ", v16);
                ret = ret + 1;
            }

            if (v17 != 145)
            {
                Console.WriteLine("test16: for (class_s.c+class_s.d)  failed actual value {0} ", v17);
                ret = ret + 1;
            }

            if (v18 != 91)
            {
                Console.WriteLine("test17: for ((class_s.a+class_s.b)+class_s.d)  failed actual value {0} ", v18);
                ret = ret + 1;
            }

            if (v19 != 91)
            {
                Console.WriteLine("test18: for (class_s.a+(class_s.b+class_s.d))  failed actual value {0} ", v19);
                ret = ret + 1;
            }

            if (v20 != 91)
            {
                Console.WriteLine("test19: for (class_s.b+(class_s.a+class_s.d))  failed actual value {0} ", v20);
                ret = ret + 1;
            }

            if (v21 != 142)
            {
                Console.WriteLine("test20: for (class_s.b+class_s.d)  failed actual value {0} ", v21);
                ret = ret + 1;
            }

            if (v22 != 5)
            {
                Console.WriteLine("test21: for (class_s.a+class_s.d)  failed actual value {0} ", v22);
                ret = ret + 1;
            }

            if (v23 != 180)
            {
                Console.WriteLine("test22: for (((class_s.a+class_s.b)+class_s.c)+class_s.d)  failed actual value {0} ", v23);
                ret = ret + 1;
            }

            if (v24 != 180)
            {
                Console.WriteLine("test23: for (class_s.d+((class_s.a+class_s.b)+class_s.c))  failed actual value {0} ", v24);
                ret = ret + 1;
            }

#if LOOP               
				} while (v18 == 0);
			} while (v17 == 0);
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

