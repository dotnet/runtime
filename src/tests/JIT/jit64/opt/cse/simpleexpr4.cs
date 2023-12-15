// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
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
//(a*(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))))

//permutations for  (a*(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))))
//(a*(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))))
//((b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))*a)
//a
//(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))
//b
//(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))
//(((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))*c)
//c
//((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))
//(((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))+(d-e))
//(d-e)
//d
//e
//d
//(d-e)
//((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))
//(((f-a)+(a*(b-(c*(d-(e*f))))))+f)
//(f+((f-a)+(a*(b-(c*(d-(e*f)))))))
//((f-a)+(a*(b-(c*(d-(e*f))))))
//((a*(b-(c*(d-(e*f)))))+(f-a))
//(f-a)
//f
//a
//f
//(f-a)
//(a*(b-(c*(d-(e*f)))))
//((b-(c*(d-(e*f))))*a)
//a
//(b-(c*(d-(e*f))))
//b
//(c*(d-(e*f)))
//((d-(e*f))*c)
//c
//(d-(e*f))
//d
//(e*f)
//(f*e)
//e
//f
//(f*e)
//(e*f)
//d
//(d-(e*f))
//((d-(e*f))*c)
//(c*(d-(e*f)))
//b
//(b-(c*(d-(e*f))))
//((b-(c*(d-(e*f))))*a)
//(a*(b-(c*(d-(e*f)))))
//((a*(b-(c*(d-(e*f)))))+(f-a))
//((f-a)+(a*(b-(c*(d-(e*f))))))
//f
//((f-a)+((a*(b-(c*(d-(e*f)))))+f))
//((a*(b-(c*(d-(e*f)))))+((f-a)+f))
//((a*(b-(c*(d-(e*f)))))+f)
//(f+(a*(b-(c*(d-(e*f))))))
//(a*(b-(c*(d-(e*f)))))
//((b-(c*(d-(e*f))))*a)
//a
//(b-(c*(d-(e*f))))
//b
//(c*(d-(e*f)))
//((d-(e*f))*c)
//c
//(d-(e*f))
//d
//(e*f)
//(f*e)
//e
//f
//(f*e)
//(e*f)
//d
//(d-(e*f))
//((d-(e*f))*c)
//(c*(d-(e*f)))
//b
//(b-(c*(d-(e*f))))
//((b-(c*(d-(e*f))))*a)
//(a*(b-(c*(d-(e*f)))))
//f
//(f+(a*(b-(c*(d-(e*f))))))
//((a*(b-(c*(d-(e*f)))))+f)
//((a*(b-(c*(d-(e*f)))))+f)
//(f+(a*(b-(c*(d-(e*f))))))
//(a*(b-(c*(d-(e*f)))))
//((b-(c*(d-(e*f))))*a)
//a
//(b-(c*(d-(e*f))))
//b
//(c*(d-(e*f)))
//((d-(e*f))*c)
//c
//(d-(e*f))
//d
//(e*f)
//(f*e)
//e
//f
//(f*e)
//(e*f)
//d
//(d-(e*f))
//((d-(e*f))*c)
//(c*(d-(e*f)))
//b
//(b-(c*(d-(e*f))))
//((b-(c*(d-(e*f))))*a)
//(a*(b-(c*(d-(e*f)))))
//f
//(f+(a*(b-(c*(d-(e*f))))))
//((a*(b-(c*(d-(e*f)))))+f)
//(f+((f-a)+(a*(b-(c*(d-(e*f)))))))
//(((f-a)+(a*(b-(c*(d-(e*f))))))+f)
//(a*ab)
//(ab*a)
//a
//ab
//(ab*a)
//(a*ab)
//(((f-a)+(a*(b-(c*(d-(e*f))))))+f)
//((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))
//(((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))+(d-e))
//((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))
//(((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))*c)
//(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))
//b
//(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))
//((b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))*a)
//(a*(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))))
namespace CseTest
{
    using System;


    public class TestClass
    {

        [Fact]
        public static int TestEntryPoint()
        {
            int f = returnf(false);
            int a = returna(false);
            int c = returnc(false);
            int b = returnb(false);
            int ab = returnab(false);
            int abc = returnabc(false);
            int r = returnr(false);
            int ad = returnad(false);
            int e = returne(false);
            int d = returnd(false);
            int ret = 100;
            int v;
            v = (((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))) * (((abc + c) - (a - (ad * a))) + r));
            if (v != 90007775)
            {
                Console.WriteLine("test1: for (((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((((abc + c) - (a - (ad * a))) + r) * ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))));
            if (v != 90007775)
            {
                Console.WriteLine("test2: for ((((abc+c)-(a-(ad*a)))+r)*((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b)));
            if (v != 37025)
            {
                Console.WriteLine("test3: for ((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (b * ((c * c) - (c + d))));
            if (v != 39269)
            {
                Console.WriteLine("test4: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b * ((c * c) - (c + d))) + a);
            if (v != 39269)
            {
                Console.WriteLine("test5: for ((b*((c*c)-(c+d)))+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * ((c * c) - (c + d)));
            if (v != 39236)
            {
                Console.WriteLine("test6: for (b*((c*c)-(c+d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((c * c) - (c + d)) * b);
            if (v != 39236)
            {
                Console.WriteLine("test7: for (((c*c)-(c+d))*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((c * c) - (c + d));
            if (v != 1154)
            {
                Console.WriteLine("test8: for ((c*c)-(c+d))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 1225)
            {
                Console.WriteLine("test9: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 1225)
            {
                Console.WriteLine("test10: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 1225)
            {
                Console.WriteLine("test11: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 1225)
            {
                Console.WriteLine("test12: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + d);
            if (v != 71)
            {
                Console.WriteLine("test13: for (c+d)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d + c);
            if (v != 71)
            {
                Console.WriteLine("test14: for (d+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d + c);
            if (v != 71)
            {
                Console.WriteLine("test15: for (d+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + d);
            if (v != 71)
            {
                Console.WriteLine("test16: for (c+d)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 1225)
            {
                Console.WriteLine("test17: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((c * c) - (c + d));
            if (v != 1154)
            {
                Console.WriteLine("test18: for ((c*c)-(c+d))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((c * c) - (c + d)) * b);
            if (v != 39236)
            {
                Console.WriteLine("test19: for (((c*c)-(c+d))*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * ((c * c) - (c + d)));
            if (v != 39236)
            {
                Console.WriteLine("test20: for (b*((c*c)-(c+d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b * ((c * c) - (c + d))) + a);
            if (v != 39269)
            {
                Console.WriteLine("test21: for ((b*((c*c)-(c+d)))+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (b * ((c * c) - (c + d))));
            if (v != 39269)
            {
                Console.WriteLine("test22: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((a * a) + a) + (a * b));
            if (v != 2244)
            {
                Console.WriteLine("test23: for (((a*a)+a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + ((a * a) + a));
            if (v != 2244)
            {
                Console.WriteLine("test24: for ((a*b)+((a*a)+a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + a);
            if (v != 1122)
            {
                Console.WriteLine("test25: for ((a*a)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * a));
            if (v != 1122)
            {
                Console.WriteLine("test26: for (a+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1089)
            {
                Console.WriteLine("test27: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1089)
            {
                Console.WriteLine("test28: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1089)
            {
                Console.WriteLine("test29: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1089)
            {
                Console.WriteLine("test30: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * a));
            if (v != 1122)
            {
                Console.WriteLine("test31: for (a+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + a);
            if (v != 1122)
            {
                Console.WriteLine("test32: for ((a*a)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 1122)
            {
                Console.WriteLine("test33: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 1122)
            {
                Console.WriteLine("test34: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 1122)
            {
                Console.WriteLine("test35: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 1122)
            {
                Console.WriteLine("test36: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + (a + (a * b)));
            if (v != 2244)
            {
                Console.WriteLine("test37: for ((a*a)+(a+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + ((a * a) + (a * b)));
            if (v != 2244)
            {
                Console.WriteLine("test38: for (a+((a*a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * b));
            if (v != 1155)
            {
                Console.WriteLine("test39: for (a+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + a);
            if (v != 1155)
            {
                Console.WriteLine("test40: for ((a*b)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 1122)
            {
                Console.WriteLine("test41: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 1122)
            {
                Console.WriteLine("test42: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 1122)
            {
                Console.WriteLine("test43: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 1122)
            {
                Console.WriteLine("test44: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + a);
            if (v != 1155)
            {
                Console.WriteLine("test45: for ((a*b)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * b));
            if (v != 1155)
            {
                Console.WriteLine("test46: for (a+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * b));
            if (v != 1155)
            {
                Console.WriteLine("test47: for (a+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + a);
            if (v != 1155)
            {
                Console.WriteLine("test48: for ((a*b)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 1122)
            {
                Console.WriteLine("test49: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 1122)
            {
                Console.WriteLine("test50: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 1122)
            {
                Console.WriteLine("test51: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 1122)
            {
                Console.WriteLine("test52: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + a);
            if (v != 1155)
            {
                Console.WriteLine("test53: for ((a*b)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * b));
            if (v != 1155)
            {
                Console.WriteLine("test54: for (a+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + ((a * a) + a));
            if (v != 2244)
            {
                Console.WriteLine("test55: for ((a*b)+((a*a)+a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((a * a) + a) + (a * b));
            if (v != 2244)
            {
                Console.WriteLine("test56: for (((a*a)+a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (b * ((c * c) - (c + d))));
            if (v != 39269)
            {
                Console.WriteLine("test57: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b)));
            if (v != 37025)
            {
                Console.WriteLine("test58: for ((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((abc + c) - (a - (ad * a))) + r);
            if (v != 2431)
            {
                Console.WriteLine("test59: for (((abc+c)-(a-(ad*a)))+r)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (r + ((abc + c) - (a - (ad * a))));
            if (v != 2431)
            {
                Console.WriteLine("test60: for (r+((abc+c)-(a-(ad*a))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((abc + c) - (a - (ad * a)));
            if (v != 2381)
            {
                Console.WriteLine("test61: for ((abc+c)-(a-(ad*a)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (abc + c);
            if (v != 137)
            {
                Console.WriteLine("test62: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + abc);
            if (v != 137)
            {
                Console.WriteLine("test63: for (c+abc)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + abc);
            if (v != 137)
            {
                Console.WriteLine("test64: for (c+abc)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (abc + c);
            if (v != 137)
            {
                Console.WriteLine("test65: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a - (ad * a));
            if (v != -2244)
            {
                Console.WriteLine("test66: for (a-(ad*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ad * a);
            if (v != 2277)
            {
                Console.WriteLine("test67: for (ad*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * ad);
            if (v != 2277)
            {
                Console.WriteLine("test68: for (a*ad)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * ad);
            if (v != 2277)
            {
                Console.WriteLine("test69: for (a*ad)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ad * a);
            if (v != 2277)
            {
                Console.WriteLine("test70: for (ad*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a - (ad * a));
            if (v != -2244)
            {
                Console.WriteLine("test71: for (a-(ad*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (abc + c);
            if (v != 137)
            {
                Console.WriteLine("test72: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((abc + c) - (a - (ad * a)));
            if (v != 2381)
            {
                Console.WriteLine("test73: for ((abc+c)-(a-(ad*a)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (r + ((abc + c) - (a - (ad * a))));
            if (v != 2431)
            {
                Console.WriteLine("test74: for (r+((abc+c)-(a-(ad*a))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((abc + c) - (a - (ad * a))) + r);
            if (v != 2431)
            {
                Console.WriteLine("test75: for (((abc+c)-(a-(ad*a)))+r)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((((abc + c) - (a - (ad * a))) + r) * ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))));
            if (v != 90007775)
            {
                Console.WriteLine("test76: for ((((abc+c)-(a-(ad*a)))+r)*((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))) * (((abc + c) - (a - (ad * a))) + r));
            if (v != 90007775)
            {
                Console.WriteLine("test77: for (((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))))));
            if (v != -1826403843)
            {
                Console.WriteLine("test78: for (a*(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))))) * a);
            if (v != -1826403843)
            {
                Console.WriteLine("test79: for ((b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)))));
            if (v != -55345571)
            {
                Console.WriteLine("test80: for (b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))));
            if (v != 55345605)
            {
                Console.WriteLine("test81: for (c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))) * c);
            if (v != 55345605)
            {
                Console.WriteLine("test82: for (((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)));
            if (v != 1581303)
            {
                Console.WriteLine("test83: for ((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)) + (d - e));
            if (v != 1581303)
            {
                Console.WriteLine("test84: for (((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))+(d-e))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - e);
            if (v != -1)
            {
                Console.WriteLine("test85: for (d-e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - e);
            if (v != -1)
            {
                Console.WriteLine("test86: for (d-e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab));
            if (v != 1581304)
            {
                Console.WriteLine("test87: for ((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((f - a) + (a * (b - (c * (d - (e * f)))))) + f);
            if (v != 1583515)
            {
                Console.WriteLine("test88: for (((f-a)+(a*(b-(c*(d-(e*f))))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + ((f - a) + (a * (b - (c * (d - (e * f)))))));
            if (v != 1583515)
            {
                Console.WriteLine("test89: for (f+((f-a)+(a*(b-(c*(d-(e*f)))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((f - a) + (a * (b - (c * (d - (e * f))))));
            if (v != 1583477)
            {
                Console.WriteLine("test90: for ((f-a)+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + (f - a));
            if (v != 1583477)
            {
                Console.WriteLine("test91: for ((a*(b-(c*(d-(e*f)))))+(f-a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f - a);
            if (v != 5)
            {
                Console.WriteLine("test92: for (f-a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f - a);
            if (v != 5)
            {
                Console.WriteLine("test93: for (f-a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 1583472)
            {
                Console.WriteLine("test94: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 1583472)
            {
                Console.WriteLine("test95: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != 47984)
            {
                Console.WriteLine("test96: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != -47950)
            {
                Console.WriteLine("test97: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != -47950)
            {
                Console.WriteLine("test98: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != -1370)
            {
                Console.WriteLine("test99: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != 1406)
            {
                Console.WriteLine("test100: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != 1406)
            {
                Console.WriteLine("test101: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != 1406)
            {
                Console.WriteLine("test102: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != 1406)
            {
                Console.WriteLine("test103: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != -1370)
            {
                Console.WriteLine("test104: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != -47950)
            {
                Console.WriteLine("test105: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != -47950)
            {
                Console.WriteLine("test106: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != 47984)
            {
                Console.WriteLine("test107: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 1583472)
            {
                Console.WriteLine("test108: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 1583472)
            {
                Console.WriteLine("test109: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + (f - a));
            if (v != 1583477)
            {
                Console.WriteLine("test110: for ((a*(b-(c*(d-(e*f)))))+(f-a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((f - a) + (a * (b - (c * (d - (e * f))))));
            if (v != 1583477)
            {
                Console.WriteLine("test111: for ((f-a)+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((f - a) + ((a * (b - (c * (d - (e * f))))) + f));
            if (v != 1583515)
            {
                Console.WriteLine("test112: for ((f-a)+((a*(b-(c*(d-(e*f)))))+f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + ((f - a) + f));
            if (v != 1583515)
            {
                Console.WriteLine("test113: for ((a*(b-(c*(d-(e*f)))))+((f-a)+f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + f);
            if (v != 1583510)
            {
                Console.WriteLine("test114: for ((a*(b-(c*(d-(e*f)))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + (a * (b - (c * (d - (e * f))))));
            if (v != 1583510)
            {
                Console.WriteLine("test115: for (f+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 1583472)
            {
                Console.WriteLine("test116: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 1583472)
            {
                Console.WriteLine("test117: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != 47984)
            {
                Console.WriteLine("test118: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != -47950)
            {
                Console.WriteLine("test119: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != -47950)
            {
                Console.WriteLine("test120: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != -1370)
            {
                Console.WriteLine("test121: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != 1406)
            {
                Console.WriteLine("test122: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != 1406)
            {
                Console.WriteLine("test123: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != 1406)
            {
                Console.WriteLine("test124: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != 1406)
            {
                Console.WriteLine("test125: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != -1370)
            {
                Console.WriteLine("test126: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != -47950)
            {
                Console.WriteLine("test127: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != -47950)
            {
                Console.WriteLine("test128: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != 47984)
            {
                Console.WriteLine("test129: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 1583472)
            {
                Console.WriteLine("test130: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 1583472)
            {
                Console.WriteLine("test131: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + (a * (b - (c * (d - (e * f))))));
            if (v != 1583510)
            {
                Console.WriteLine("test132: for (f+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + f);
            if (v != 1583510)
            {
                Console.WriteLine("test133: for ((a*(b-(c*(d-(e*f)))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + f);
            if (v != 1583510)
            {
                Console.WriteLine("test134: for ((a*(b-(c*(d-(e*f)))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + (a * (b - (c * (d - (e * f))))));
            if (v != 1583510)
            {
                Console.WriteLine("test135: for (f+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 1583472)
            {
                Console.WriteLine("test136: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 1583472)
            {
                Console.WriteLine("test137: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != 47984)
            {
                Console.WriteLine("test138: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != -47950)
            {
                Console.WriteLine("test139: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != -47950)
            {
                Console.WriteLine("test140: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != -1370)
            {
                Console.WriteLine("test141: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != 1406)
            {
                Console.WriteLine("test142: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != 1406)
            {
                Console.WriteLine("test143: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != 1406)
            {
                Console.WriteLine("test144: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != 1406)
            {
                Console.WriteLine("test145: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != -1370)
            {
                Console.WriteLine("test146: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != -47950)
            {
                Console.WriteLine("test147: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != -47950)
            {
                Console.WriteLine("test148: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != 47984)
            {
                Console.WriteLine("test149: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 1583472)
            {
                Console.WriteLine("test150: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 1583472)
            {
                Console.WriteLine("test151: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + (a * (b - (c * (d - (e * f))))));
            if (v != 1583510)
            {
                Console.WriteLine("test152: for (f+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + f);
            if (v != 1583510)
            {
                Console.WriteLine("test153: for ((a*(b-(c*(d-(e*f)))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + ((f - a) + (a * (b - (c * (d - (e * f)))))));
            if (v != 1583515)
            {
                Console.WriteLine("test154: for (f+((f-a)+(a*(b-(c*(d-(e*f)))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((f - a) + (a * (b - (c * (d - (e * f)))))) + f);
            if (v != 1583515)
            {
                Console.WriteLine("test155: for (((f-a)+(a*(b-(c*(d-(e*f))))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * ab);
            if (v != 2211)
            {
                Console.WriteLine("test156: for (a*ab)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ab * a);
            if (v != 2211)
            {
                Console.WriteLine("test157: for (ab*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ab * a);
            if (v != 2211)
            {
                Console.WriteLine("test158: for (ab*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * ab);
            if (v != 2211)
            {
                Console.WriteLine("test159: for (a*ab)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((f - a) + (a * (b - (c * (d - (e * f)))))) + f);
            if (v != 1583515)
            {
                Console.WriteLine("test160: for (((f-a)+(a*(b-(c*(d-(e*f))))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab));
            if (v != 1581304)
            {
                Console.WriteLine("test161: for ((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)) + (d - e));
            if (v != 1581303)
            {
                Console.WriteLine("test162: for (((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))+(d-e))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)));
            if (v != 1581303)
            {
                Console.WriteLine("test163: for ((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))) * c);
            if (v != 55345605)
            {
                Console.WriteLine("test164: for (((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))));
            if (v != 55345605)
            {
                Console.WriteLine("test165: for (c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)))));
            if (v != -55345571)
            {
                Console.WriteLine("test166: for (b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))))) * a);
            if (v != -1826403843)
            {
                Console.WriteLine("test167: for ((b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))))));
            if (v != -1826403843)
            {
                Console.WriteLine("test168: for (a*(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            return ret;
        }

        private static int returnd(bool verbose)
        {
            int ans;
            try
            {
                ans = 36;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning d=36");
                }
            }
            return ans;
        }

        private static int returne(bool verbose)
        {
            int ans;
            try
            {
                ans = 37;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning e=37");
                }
            }
            return ans;
        }

        private static int returnad(bool verbose)
        {
            int ans;
            try
            {
                ans = 69;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning ad=69");
                }
            }
            return ans;
        }

        private static int returnr(bool verbose)
        {
            int ans;
            try
            {
                ans = 50;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning r=50");
                }
            }
            return ans;
        }

        private static int returnabc(bool verbose)
        {
            int ans;
            try
            {
                ans = 102;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning abc=102");
                }
            }
            return ans;
        }

        private static int returnab(bool verbose)
        {
            int ans;
            try
            {
                ans = 67;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning ab=67");
                }
            }
            return ans;
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
                    Console.WriteLine("returning b=34");
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
                    Console.WriteLine("returning c=35");
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
                    Console.WriteLine("returning a=33");
                }
            }
            return ans;
        }

        private static int returnf(bool verbose)
        {
            int ans;
            try
            {
                ans = 38;
            }
            finally
            {
                if (verbose)
                {
                    Console.WriteLine("returning f=38");
                }
            }
            return ans;
        }
    }
}

