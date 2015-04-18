// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

//(((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))

//permutations for  (((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))
//(((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))
//((((abc+c)-(a-(ad*a)))+r)*((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b))))
//((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))
//(a+(b*((c*c)-(c+d))))
//((b*((c*c)-(c+d)))+a)
//a
//(b*((c*c)-(c+d)))
//(((c*c)-(c+d))*b)
//b
//((c*c)-(c+d))
//(c*c)
//(c*c)
//c
//c
//(c*c)
//(c*c)
//(c+d)
//(d+c)
//c
//d
//(d+c)
//(c+d)
//(c*c)
//((c*c)-(c+d))
//(((c*c)-(c+d))*b)
//(b*((c*c)-(c+d)))
//((b*((c*c)-(c+d)))+a)
//(a+(b*((c*c)-(c+d))))
//(((a*a)+a)+(a*b))
//((a*b)+((a*a)+a))
//((a*a)+a)
//(a+(a*a))
//(a*a)
//(a*a)
//a
//a
//(a*a)
//(a*a)
//a
//(a+(a*a))
//((a*a)+a)
//(a*b)
//(b*a)
//a
//b
//(b*a)
//(a*b)
//((a*a)+(a+(a*b)))
//(a+((a*a)+(a*b)))
//(a+(a*b))
//((a*b)+a)
//a
//(a*b)
//(b*a)
//a
//b
//(b*a)
//(a*b)
//((a*b)+a)
//(a+(a*b))
//((a*a)+(a*b))
//((a*b)+(a*a))
//(a*a)
//(a*a)
//a
//a
//(a*a)
//(a*a)
//(a*b)
//(b*a)
//a
//b
//(b*a)
//(a*b)
//((a*b)+(a*a))
//((a*a)+(a*b))
//((a*b)+((a*a)+a))
//(((a*a)+a)+(a*b))
//(a+(b*((c*c)-(c+d))))
//((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))
//(((abc+c)-(a-(ad*a)))+r)
//(r+((abc+c)-(a-(ad*a))))
//((abc+c)-(a-(ad*a)))
//(abc+c)
//(c+abc)
//abc
//c
//(c+abc)
//(abc+c)
//(a-(ad*a))
//a
//(ad*a)
//(a*ad)
//ad
//a
//(a*ad)
//(ad*a)
//a
//(a-(ad*a))
//(abc+c)
//((abc+c)-(a-(ad*a)))
//r
//(r+((abc+c)-(a-(ad*a))))
//(((abc+c)-(a-(ad*a)))+r)
//((((abc+c)-(a-(ad*a)))+r)*((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b))))
//(((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))

namespace CseTest
{
    using System;


    public class Test_Main
    {

        static int Main()
        {
            int ret = 100;
            int d = return_int(false, 40);
            int ad = return_int(false, -31);
            int r = return_int(false, 15);
            int abc = return_int(false, -75);
            int b = return_int(false, 30);
            int c = return_int(false, 19);
            int a = return_int(false, -27);
            int v;
#if LOOP
			for (int i = 0; i < 5; i++) {
#endif
            v = (((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))) * (((abc + c) - (a - (ad * a))) + r));
            if (v != 7523043)
            {
                Console.WriteLine("test0: for (((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((abc + c) - (a - (ad * a))) + r) * ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))));
            if (v != 7523043)
            {
                Console.WriteLine("test1: for ((((abc+c)-(a-(ad*a)))+r)*((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b)));
            if (v != 9141)
            {
                Console.WriteLine("test2: for ((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
				ad = return_int(false,-31);
			}
#endif
            b = return_int(false, -65);
            r = return_int(false, 55);
            v = (a + (b * ((c * c) - (c + d))));
            if (v != -19657)
            {
                Console.WriteLine("test3: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((b * ((c * c) - (c + d))) + a);
            if (v != -19657)
            {
                Console.WriteLine("test4: for ((b*((c*c)-(c+d)))+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b * ((c * c) - (c + d)));
            if (v != -19630)
            {
                Console.WriteLine("test5: for (b*((c*c)-(c+d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((c * c) - (c + d)) * b);
            if (v != -19630)
            {
                Console.WriteLine("test6: for (((c*c)-(c+d))*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP		
			for (int i = 0; i < 10; i++) {
#endif
            v = ((c * c) - (c + d));
            if (v != 302)
            {
                Console.WriteLine("test7: for ((c*c)-(c+d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c * c);
            if (v != 361)
            {
                Console.WriteLine("test8: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c * c);
            if (v != 361)
            {
                Console.WriteLine("test9: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
				d = return_int(false, 40);
			}
#endif
            v = (c * c);
            if (v != 361)
            {
                Console.WriteLine("test10: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 361)
            {
                Console.WriteLine("test11: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + d);
            if (v != 59)
            {
                Console.WriteLine("test12: for (c+d)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d + c);
            if (v != 59)
            {
                Console.WriteLine("test13: for (d+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d + c);
            if (v != 59)
            {
                Console.WriteLine("test14: for (d+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + d);
            if (v != 59)
            {
                Console.WriteLine("test15: for (c+d)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 361)
            {
                Console.WriteLine("test16: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			for (int j = 0; j < 4; j++) {
				for (int i = 0; i < 3; i++) {
#endif
            v = ((c * c) - (c + d));
            if (v != 302)
            {
                Console.WriteLine("test17: for ((c*c)-(c+d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((c * c) - (c + d)) * b);
            if (v != -19630)
            {
                Console.WriteLine("test18: for (((c*c)-(c+d))*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
					d = return_int(false, 40);
				}
			}
#endif
            ad = return_int(false, 22);
            v = (b * ((c * c) - (c + d)));
            if (v != -19630)
            {
                Console.WriteLine("test19: for (b*((c*c)-(c+d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((b * ((c * c) - (c + d))) + a);
            if (v != -19657)
            {
                Console.WriteLine("test20: for ((b*((c*c)-(c+d)))+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            r = return_int(false, 67);
            v = (a + (b * ((c * c) - (c + d))));
            if (v != -19657)
            {
                Console.WriteLine("test21: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }


            v = (((a * a) + a) + (a * b));
            if (v != 2457)
            {
                Console.WriteLine("test22: for (((a*a)+a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            c = return_int(false, 64);
            v = ((a * b) + ((a * a) + a));
            if (v != 2457)
            {
                Console.WriteLine("test23: for ((a*b)+((a*a)+a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + a);
            if (v != 702)
            {
                Console.WriteLine("test24: for ((a*a)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * a));
            if (v != 702)
            {
                Console.WriteLine("test25: for (a+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 729)
            {
                Console.WriteLine("test26: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
			try {
#endif
#if LOOP
			for (int i = 0; i < 4; i++) {
#endif
            v = (a * a);
            if (v != 729)
            {
                Console.WriteLine("test27: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * a);
            if (v != 729)
            {
                Console.WriteLine("test28: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			}

			for (int j = 0; j < a; j++) {
			
				for (int i = 0; i < b; i++) {
#endif
            v = (a * a);
            if (v != 729)
            {
                Console.WriteLine("test29: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a + (a * a));
            if (v != 702)
            {
                Console.WriteLine("test30: for (a+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a * a) + a);
            if (v != 702)
            {
                Console.WriteLine("test31: for ((a*a)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            abc = return_int(false, -10);
            v = (a * b);
            if (v != 1755)
            {
                Console.WriteLine("test32: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP

				}
			}
#endif
#if TRY
			} finally {
#endif
            v = (b * a);
            if (v != 1755)
            {
                Console.WriteLine("test33: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b * a);
            if (v != 1755)
            {
                Console.WriteLine("test34: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
				try {
#endif
            r = return_int(false, 84);
            v = (a * b);
            if (v != 1755)
            {
                Console.WriteLine("test35: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a * a) + (a + (a * b)));
            if (v != 2457)
            {
                Console.WriteLine("test36: for ((a*a)+(a+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a + ((a * a) + (a * b)));
            if (v != 2457)
            {
                Console.WriteLine("test37: for (a+((a*a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a + (a * b));
            if (v != 1728)
            {
                Console.WriteLine("test38: for (a+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a * b) + a);
            if (v != 1728)
            {
                Console.WriteLine("test39: for ((a*b)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * b);
            if (v != 1755)
            {
                Console.WriteLine("test40: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
				} finally {
#endif
            v = (b * a);
            if (v != 1755)
            {
                Console.WriteLine("test41: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b * a);
            if (v != 1755)
            {
                Console.WriteLine("test42: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * b);
            if (v != 1755)
            {
                Console.WriteLine("test43: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a * b) + a);
            if (v != 1728)
            {
                Console.WriteLine("test44: for ((a*b)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            r = return_int(false, 77);
            r = return_int(false, 1);
            v = (a + (a * b));
            if (v != 1728)
            {
                Console.WriteLine("test45: for (a+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY
				}
#endif
            v = ((a * a) + (a * b));
            if (v != 2484)
            {
                Console.WriteLine("test46: for ((a*a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a * b) + (a * a));
            if (v != 2484)
            {
                Console.WriteLine("test47: for ((a*b)+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * a);
            if (v != 729)
            {
                Console.WriteLine("test48: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * a);
            if (v != 729)
            {
                Console.WriteLine("test49: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * a);
            if (v != 729)
            {
                Console.WriteLine("test50: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * a);
            if (v != 729)
            {
                Console.WriteLine("test51: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * b);
            if (v != 1755)
            {
                Console.WriteLine("test52: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b * a);
            if (v != 1755)
            {
                Console.WriteLine("test53: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (b * a);
            if (v != 1755)
            {
                Console.WriteLine("test54: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * b);
            if (v != 1755)
            {
                Console.WriteLine("test55: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a * b) + (a * a));
            if (v != 2484)
            {
                Console.WriteLine("test56: for ((a*b)+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a * a) + (a * b));
            if (v != 2484)
            {
                Console.WriteLine("test57: for ((a*a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a * b) + ((a * a) + a));
            if (v != 2457)
            {
                Console.WriteLine("test58: for ((a*b)+((a*a)+a))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            abc = return_int(false, 45);
            v = (((a * a) + a) + (a * b));
            if (v != 2457)
            {
                Console.WriteLine("test59: for (((a*a)+a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            a = return_int(false, 3);
            v = (a + (b * ((c * c) - (c + d))));
            if (v != -259477)
            {
                Console.WriteLine("test60: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b)));
            if (v != -259294)
            {
                Console.WriteLine("test61: for ((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((abc + c) - (a - (ad * a))) + r);
            if (v != 173)
            {
                Console.WriteLine("test62: for (((abc+c)-(a-(ad*a)))+r)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (r + ((abc + c) - (a - (ad * a))));
            if (v != 173)
            {
                Console.WriteLine("test63: for (r+((abc+c)-(a-(ad*a))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			if (v==173) {
#endif
            v = ((abc + c) - (a - (ad * a)));
            if (v != 172)
            {
                Console.WriteLine("test64: for ((abc+c)-(a-(ad*a)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (abc + c);
            if (v != 109)
            {
                Console.WriteLine("test65: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c + abc);
            if (v != 109)
            {
                Console.WriteLine("test66: for (c+abc)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (c + abc);
            if (v != 109)
            {
                Console.WriteLine("test67: for (c+abc)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (abc + c);
            if (v != 109)
            {
                Console.WriteLine("test68: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a - (ad * a));
            if (v != -63)
            {
                Console.WriteLine("test69: for (a-(ad*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (ad * a);
            if (v != 66)
            {
                Console.WriteLine("test70: for (ad*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * ad);
            if (v != 66)
            {
                Console.WriteLine("test71: for (a*ad)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a * ad);
            if (v != 66)
            {
                Console.WriteLine("test72: for (a*ad)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			}
#endif
            v = (ad * a);
            if (v != 66)
            {
                Console.WriteLine("test73: for (ad*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (a - (ad * a));
            if (v != -63)
            {
                Console.WriteLine("test74: for (a-(ad*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (abc + c);
            if (v != 109)
            {
                Console.WriteLine("test75: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((abc + c) - (a - (ad * a)));
            if (v != 172)
            {
                Console.WriteLine("test76: for ((abc+c)-(a-(ad*a)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (r + ((abc + c) - (a - (ad * a))));
            if (v != 173)
            {
                Console.WriteLine("test77: for (r+((abc+c)-(a-(ad*a))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((abc + c) - (a - (ad * a))) + r);
            if (v != 173)
            {
                Console.WriteLine("test78: for (((abc+c)-(a-(ad*a)))+r)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((abc + c) - (a - (ad * a))) + r) * ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))));
            if (v != -44857862)
            {
                Console.WriteLine("test79: for ((((abc+c)-(a-(ad*a)))+r)*((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))) * (((abc + c) - (a - (ad * a))) + r));
            if (v != -44857862)
            {
                Console.WriteLine("test80: for (((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))  failed actual value {0} ", v);
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

