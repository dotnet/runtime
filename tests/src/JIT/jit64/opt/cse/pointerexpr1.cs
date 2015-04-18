// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//((a+ *b)+ *c)

//permutations for  ((a+ *b)+ *c)
//((a+ *b)+ *c)
//( *c+(a+ *b))
//(a+ *b)
//( *b+a)
//a
// *b
//( *b+a)
//(a+ *b)
// *c
//( *c+(a+ *b))
//((a+ *b)+ *c)
namespace CseTest
{
    using System;


    public class TestClass
    {
        static int Main()
        {
            int a = returna(false);
            int c = returnc(false);
            int b = returnb(false);
            unsafe
            {
                return m_Main(&a, &b, &c);
            }
        }
        static unsafe int m_Main(int* a, int* b, int* c)
        {
            int ret = 100;
            int v;
            v = ((*a + *b) + *c);
            if (v != 102)
            {
                Console.WriteLine("test1: for (( *a+ *b)+ *c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			for (int i = 0; i < 5; i++) {
				for (int j = 0; j < 6; j++) {
#endif
            v = (*c + (*a + *b));
            if (v != 102)
            {
                Console.WriteLine("test2: for ( *c+( *a+ *b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (*a + *b);
            if (v != 67)
            {
                Console.WriteLine("test3: for ( *a+ *b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
				}
				*a = returna(false);
			}
#endif

            v = (*b + *a);
            if (v != 67)
            {
                Console.WriteLine("test4: for ( *b+ *a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (*b + *a);
            if (v != 67)
            {
                Console.WriteLine("test5: for ( *b+ *a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (*a + *b);
            if (v != 67)
            {
                Console.WriteLine("test6: for ( *a+ *b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (*c + (*a + *b));
            if (v != 102)
            {
                Console.WriteLine("test7: for ( *c+( *a+ *b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((*a + *b) + *c);
            if (v != 102)
            {
                Console.WriteLine("test8: for (( *a+ *b)+ *c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            return ret;
        }

        private static int returnb(bool verbose)
        {
            int ans;
            try
            {
                ans = 34;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning  *b=34");
                }
            }
            return ans;
        }

        private static int returnc(bool verbose)
        {
            int ans;
            try
            {
                ans = 35;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning  *c=35");
                }
            }
            return ans;
        }

        private static int returna(bool verbose)
        {
            int ans;
            try
            {
                ans = 33;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning  *a=33");
                }
            }
            return ans;
        }
    }
}

