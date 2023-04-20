// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
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
        [Fact]
        public static int TestEntryPoint()
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
#pragma warning disable 0168
            int v;
#pragma warning restore 0168
            int v1 = ((*a + *b) + *c);
            int v2 = (*c + (*a + *b));
            int v3 = (*a + *b);
            int v4 = (*b + *a);
            int v5 = (*b + *a);
            int v6 = (*a + *b);
            int v7 = (*c + (*a + *b));
            int v8 = ((*a + *b) + *c);
            if (v2 != 102)
            {
                Console.WriteLine("test2: for ( *c+( *a+ *b))  failed actual value {0} ", v2);
                ret = ret + 1;
            }
            if (v3 != 67)
            {
                Console.WriteLine("test3: for ( *a+ *b)  failed actual value {0} ", v3);
                ret = ret + 1;
            }
            if (v4 != 67)
            {
                Console.WriteLine("test4: for ( *b+ *a)  failed actual value {0} ", v4);
                ret = ret + 1;
            }
            if (v5 != 67)
            {
                Console.WriteLine("test5: for ( *b+ *a)  failed actual value {0} ", v5);
                ret = ret + 1;
            }
            if (v6 != 67)
            {
                Console.WriteLine("test6: for ( *a+ *b)  failed actual value {0} ", v6);
                ret = ret + 1;
            }
            if (v8 != 102)
            {
                Console.WriteLine("test8: for (( *a+ *b)+ *c)  failed actual value {0} ", v8);
                ret = ret + 1;
            }
            if (v7 != 102)
            {
                Console.WriteLine("test7: for ( *c+( *a+ *b))  failed actual value {0} ", v7);
                ret = ret + 1;
            }
            if (v1 != 102)
            {
                Console.WriteLine("test1: for (( *a+ *b)+ *c)  failed actual value {0} ", v1);
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

