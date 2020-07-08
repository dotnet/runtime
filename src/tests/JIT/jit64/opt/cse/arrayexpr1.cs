// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

//((a[0]+a[2])+a[1])
//permutations for  ((a[0]+a[2])+a[1])
//((a[0]+a[2])+a[1])
//(a[1]+(a[0]+a[2]))
//(a[0]+a[2])
//(a[2]+a[0])
//a[0]
//a[2]
//(a[2]+a[0])
//(a[0]+a[2])
//a[1]
//(a[0]+(a[2]+a[1]))
//(a[2]+(a[0]+a[1]))
//(a[2]+a[1])
//(a[1]+a[2])
//a[2]
//a[1]
//(a[1]+a[2])
//(a[2]+a[1])
//(a[0]+a[1])
//(a[1]+a[0])
//a[0]
//a[1]
//(a[1]+a[0])
//(a[0]+a[1])
//(a[1]+(a[0]+a[2]))
//((a[0]+a[2])+a[1])
namespace CseTest
{
    using System;

    public class Test_Main
    {
        static int Main()
        {
            int ret = 100;
            int[] a = new int[5];

            a[0] = return_int(false, -69);
            a[2] = return_int(false, 15);
            a[1] = return_int(false, -17);

            int v;

#if LOOP
            do {
#endif
            v = ((a[0] + a[2]) + a[1]);
            if (v != -71)
            {
                Console.WriteLine("test0: for ((a[0]+a[2])+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			for (int i = 0; i < 5; i++) {
#endif
            v = (a[1] + (a[0] + a[2]));
            if (v != -71)
            {
                Console.WriteLine("test1: for (a[1]+(a[0]+a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + a[2]);
            if (v != -54)
            {
                Console.WriteLine("test2: for (a[0]+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + a[0]);
            if (v != -54)
            {
                Console.WriteLine("test3: for (a[2]+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + a[0]);
            if (v != -54)
            {
                Console.WriteLine("test4: for (a[2]+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			      a[0] = return_int(false,-69);
			}
#endif
#if TRY
				try {
#endif
            a[0] = return_int(false, -27);
            v = (a[0] + a[2]);
            if (v != -12)
            {
                Console.WriteLine("test5: for (a[0]+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[0] + (a[2] + a[1]));
            if (v != -29)
            {
                Console.WriteLine("test6: for (a[0]+(a[2]+a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + (a[0] + a[1]));
            if (v != -29)
            {
                Console.WriteLine("test7: for (a[2]+(a[0]+a[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + a[1]);
            if (v != -2)
            {
                Console.WriteLine("test8: for (a[2]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[2]);
            if (v != -2)
            {
                Console.WriteLine("test9: for (a[1]+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
				} finally {
#endif

#if LOOP
			for (int j = 0; j < 5; j++) {
				for (int i = 0; i < 10; i++) {
#endif
            v = (a[1] + a[2]);

            if (v != -2)
            {
                Console.WriteLine(a[1] + " " + a[2]);
                Console.WriteLine("test10: for (a[1]+a[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[2] + a[1]);
            if (v != -2)
            {
                Console.WriteLine("test11: for (a[2]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a[2] = return_int(false, -105);
            v = (a[0] + a[1]);
            if (v != -44)
            {
                Console.WriteLine("test12: for (a[0]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + a[0]);
            if (v != -44)
            {
                Console.WriteLine("test13: for (a[1]+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
				a[2] = return_int(false, 15);
				}
				a[1] = return_int(false, -17);
			}
#endif
            a[2] = return_int(false, -105);

            v = (a[1] + a[0]);
            if (v != -44)
            {
                Console.WriteLine("test14: for (a[1]+a[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
				}
#endif
            v = (a[0] + a[1]);
            if (v != -44)
            {
                Console.WriteLine("test15: for (a[0]+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a[1] + (a[0] + a[2]));
            if (v != -149)
            {
                Console.WriteLine("test16: for (a[1]+(a[0]+a[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a[0] + a[2]) + a[1]);
            if (v != -149)
            {
                Console.WriteLine("test17: for ((a[0]+a[2])+a[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
         }   while (v==0);
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

