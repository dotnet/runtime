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
//(s.b+s.c)
//(s.c+s.b)
//s.b
//s.c
//(s.c+s.b)
//(s.b+s.c)
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

            s.a = returns_a(false);
            s.b = returns_b(false);
            s.c = returns_c(false);
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
            //int v;

#if LOOP         
			do {
            do {
#endif
#if TRY
			try{
#endif
#if LOOP  
					do {
#endif
            v1 = ((s.a + s.b) + s.c);
            v2 = (s.c + (s.a + s.b));
            v3 = (s.a + s.b);
            v4 = (s.b + s.a);
            v5 = (s.b + s.a);
            v6 = (s.a + s.b);
            v7 = (s.a + (s.b + s.c));
            v8 = (s.b + (s.a + s.c));
            v9 = (s.b + s.c);
            v10 = (s.c + s.b);
            v11 = (s.c + s.b);
            v12 = (s.b + s.c);
            v13 = (s.b + s.c);
            v14 = (s.c + s.b);
            v15 = (s.c + s.b);
            v16 = (s.b + s.c);
            v17 = (s.c + (s.a + s.b));
#if LOOP                  
					} while (v17 == 0);
#endif
#if TRY
			} finally {
#endif

            v18 = ((s.a + s.b) + s.c);

            if (v1 != 348)
            {
                Console.WriteLine("test1: for ((s.a+s.b)+s.c)  failed actual value {0} ", v1);
                ret = ret + 1;
            }

            if (v2 != 348)
            {
                Console.WriteLine("test2: for (s.c+(s.a+s.b))  failed actual value {0} ", v2);
                ret = ret + 1;
            }

            if (v3 != 231)
            {
                Console.WriteLine("test3: for (s.a+s.b)  failed actual value {0} ", v3);
                ret = ret + 1;
            }

#if TRY
					try {
#endif
            if (v4 != 231)
            {
                Console.WriteLine("test4: for (s.b+s.a)  failed actual value {0} ", v4);
                ret = ret + 1;
            }

            if (v5 != 231)
            {
                Console.WriteLine("test5: for (s.b+s.a)  failed actual value {0} ", v5);
                ret = ret + 1;
            }

            if (v6 != 231)
            {
                Console.WriteLine("test6: for (s.a+s.b)  failed actual value {0} ", v6);
                ret = ret + 1;
            }
#if TRY                  
					} finally {
					}
#endif
            if (v7 != 348)
            {
                Console.WriteLine("test7: for (s.a+(s.b+s.c))  failed actual value {0} ", v7);
                ret = ret + 1;
            }

            if (v8 != 348)
            {
                Console.WriteLine("test8: for (s.b+(s.a+s.c))  failed actual value {0} ", v8);
                ret = ret + 1;
            }

            if (v9 != 233)
            {
                Console.WriteLine("test9: for (s.b+s.c)  failed actual value {0} ", v9);
                ret = ret + 1;
            }

            if (v10 != 233)
            {
                Console.WriteLine("test10: for (s.c+s.b)  failed actual value {0} ", v10);
                ret = ret + 1;
            }

            if (v11 != 233)
            {
                Console.WriteLine("test11: for (s.c+s.b)  failed actual value {0} ", v11);
                ret = ret + 1;
            }

            if (v12 != 233)
            {
                Console.WriteLine("test12: for (s.b+s.c)  failed actual value {0} ", v12);
                ret = ret + 1;
            }

            if (v13 != 233)
            {
                Console.WriteLine("test13: for (s.b+s.c)  failed actual value {0} ", v13);
                ret = ret + 1;
            }

            if (v14 != 233)
            {
                Console.WriteLine("test14: for (s.c+s.b)  failed actual value {0} ", v14);
                ret = ret + 1;
            }

            if (v15 != 233)
            {
                Console.WriteLine("test15: for (s.c+s.b)  failed actual value {0} ", v15);
                ret = ret + 1;
            }

            if (v16 != 233)
            {
                Console.WriteLine("test16: for (s.b+s.c)  failed actual value {0} ", v16);
                ret = ret + 1;
            }

            if (v17 != 348)
            {
                Console.WriteLine("test17: for (s.c+(s.a+s.b))  failed actual value {0} ", v17);
                ret = ret + 1;
            }

            if (v18 != 348)
            {
                Console.WriteLine("test18: for ((s.a+s.b)+s.c)  failed actual value {0} ", v18);
                ret = ret + 1;
            }
#if TRY
			} 
#endif

#if LOOP               
				} while (v18 == 0);
			} while (v17 == 0);
#endif

            Console.WriteLine(ret);
            return ret;
        }


        private static int returns_a(bool verbose)
        {
            int ans;
            try
            {
                ans = 115;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning s_a=115");
                }
            }
            return ans;
        }

        private static int returns_b(bool verbose)
        {
            int ans;
            try
            {
                ans = 116;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning s_b=116");
                }
            }
            return ans;
        }

        private static int returns_c(bool verbose)
        {
            int ans;
            try
            {
                ans = 117;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning s_c=117");
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

