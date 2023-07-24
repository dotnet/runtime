// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
//((a+b)+c)

//permutations for  ((a+b)+c)
//((a+b)+c)
//(c+(a+b))
//(a+b)
//(b+a)
//a
//b
//(b+a)
//(a+b)
//c
//(a+(b+c))
//(b+(a+c))
//(b+c)
//(c+b)
//b
//c
//(c+b)
//(b+c)
//(a+c)
//(c+a)
//a
//c
//(c+a)
//(a+c)
//(c+(a+b))
//((a+b)+c)
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
//((ar[0]+ar[1])+ar[2])

//permutations for  ((ar[0]+ar[1])+ar[2])
//((ar[0]+ar[1])+ar[2])
//(ar[2]+(ar[0]+ar[1]))
//(ar[0]+ar[1])
//(ar[1]+ar[0])
//ar[0]
//ar[1]
//(ar[1]+ar[0])
//(ar[0]+ar[1])
//ar[2]
//(ar[0]+(ar[1]+ar[2]))
//(ar[1]+(ar[0]+ar[2]))
//(ar[1]+ar[2])
//(ar[2]+ar[1])
//ar[1]
//ar[2]
//(ar[2]+ar[1])
//(ar[1]+ar[2])
//(ar[0]+ar[2])
//(ar[2]+ar[0])
//ar[0]
//ar[2]
//(ar[2]+ar[0])
//(ar[0]+ar[2])
//(ar[2]+(ar[0]+ar[1]))
//((ar[0]+ar[1])+ar[2])
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
//((f-a)+f)
//(f+(f-a))
//(f-a)
//f
//a
//f
//(f-a)
//f
//(f+(f-a))
//((f-a)+f)
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


    public class Test_Main
    {

        [Fact]
        public static int TestEntryPoint()
        {
            int ret = 100;
            int d = return_int(false, -17);
            int e = return_int(false, -9);
            int ad = return_int(false, 30);
            int[] ar = new int[5];
            ar[0] = return_int(false, -67);
            ar[1] = return_int(false, -72);
            ar[2] = return_int(false, 9);
            class_s s = new class_s();
            s.a = return_int(false, 57);
            s.b = return_int(false, 35);
            s.c = return_int(false, 47);
            int abc = return_int(false, 10);
            int r = return_int(false, 34);
            int ab = return_int(false, 78);
            int b = return_int(false, -19);
            int c = return_int(false, -31);
            int a = return_int(false, -42);
            int f = return_int(false, -8);
            int v;
            v = ((a + b) + c);
            if (v != -92)
            {
                Console.WriteLine("test0: for ((a+b)+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + (a + b));
            if (v != -92)
            {
                Console.WriteLine("test1: for (c+(a+b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + b);
            if (v != -61)
            {
                Console.WriteLine("test2: for (a+b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b + a);
            if (v != -61)
            {
                Console.WriteLine("test3: for (b+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b + a);
            if (v != -61)
            {
                Console.WriteLine("test4: for (b+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + b);
            if (v != -61)
            {
                Console.WriteLine("test5: for (a+b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (b + c));
            if (v != -92)
            {
                Console.WriteLine("test6: for (a+(b+c))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b + (a + c));
            if (v != -92)
            {
                Console.WriteLine("test7: for (b+(a+c))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            s.c = return_int(false, -6);
            v = (b + c);
            if (v != -50)
            {
                Console.WriteLine("test8: for (b+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            e = return_int(false, 54);
            v = (c + b);
            if (v != -50)
            {
                Console.WriteLine("test9: for (c+b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + b);
            if (v != -50)
            {
                Console.WriteLine("test10: for (c+b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b + c);
            if (v != -50)
            {
                Console.WriteLine("test11: for (b+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + c);
            if (v != -73)
            {
                Console.WriteLine("test12: for (a+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + a);
            if (v != -73)
            {
                Console.WriteLine("test13: for (c+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + a);
            if (v != -73)
            {
                Console.WriteLine("test14: for (c+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + c);
            if (v != -73)
            {
                Console.WriteLine("test15: for (a+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + (a + b));
            if (v != -92)
            {
                Console.WriteLine("test16: for (c+(a+b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a + b) + c);
            if (v != -92)
            {
                Console.WriteLine("test17: for ((a+b)+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((s.a + s.b) + s.c);
            if (v != 86)
            {
                Console.WriteLine("test18: for ((s.a+s.b)+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.c + (s.a + s.b));
            if (v != 86)
            {
                Console.WriteLine("test19: for (s.c+(s.a+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.a + s.b);
            if (v != 92)
            {
                Console.WriteLine("test20: for (s.a+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.b + s.a);
            if (v != 92)
            {
                Console.WriteLine("test21: for (s.b+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.b + s.a);
            if (v != 92)
            {
                Console.WriteLine("test22: for (s.b+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.a + s.b);
            if (v != 92)
            {
                Console.WriteLine("test23: for (s.a+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.a + (s.b + s.c));
            if (v != 86)
            {
                Console.WriteLine("test24: for (s.a+(s.b+s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.b + (s.a + s.c));
            if (v != 86)
            {
                Console.WriteLine("test25: for (s.b+(s.a+s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.b + s.c);
            if (v != 29)
            {
                Console.WriteLine("test26: for (s.b+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.c + s.b);
            if (v != 29)
            {
                Console.WriteLine("test27: for (s.c+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.c + s.b);
            if (v != 29)
            {
                Console.WriteLine("test28: for (s.c+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.b + s.c);
            if (v != 29)
            {
                Console.WriteLine("test29: for (s.b+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.a + s.c);
            if (v != 51)
            {
                Console.WriteLine("test30: for (s.a+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.c + s.a);
            if (v != 51)
            {
                Console.WriteLine("test31: for (s.c+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.c + s.a);
            if (v != 51)
            {
                Console.WriteLine("test32: for (s.c+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.a + s.c);
            if (v != 51)
            {
                Console.WriteLine("test33: for (s.a+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (s.c + (s.a + s.b));
            if (v != 86)
            {
                Console.WriteLine("test34: for (s.c+(s.a+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((s.a + s.b) + s.c);
            if (v != 86)
            {
                Console.WriteLine("test35: for ((s.a+s.b)+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((ar[0] + ar[1]) + ar[2]);
            if (v != -130)
            {
                Console.WriteLine("test36: for ((ar[0]+ar[1])+ar[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[2] + (ar[0] + ar[1]));
            if (v != -130)
            {
                Console.WriteLine("test37: for (ar[2]+(ar[0]+ar[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[0] + ar[1]);
            if (v != -139)
            {
                Console.WriteLine("test38: for (ar[0]+ar[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[1] + ar[0]);
            if (v != -139)
            {
                Console.WriteLine("test39: for (ar[1]+ar[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            s.c = return_int(false, -4);
            v = (ar[1] + ar[0]);
            if (v != -139)
            {
                Console.WriteLine("test40: for (ar[1]+ar[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[0] + ar[1]);
            if (v != -139)
            {
                Console.WriteLine("test41: for (ar[0]+ar[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[0] + (ar[1] + ar[2]));
            if (v != -130)
            {
                Console.WriteLine("test42: for (ar[0]+(ar[1]+ar[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[1] + (ar[0] + ar[2]));
            if (v != -130)
            {
                Console.WriteLine("test43: for (ar[1]+(ar[0]+ar[2]))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[1] + ar[2]);
            if (v != -63)
            {
                Console.WriteLine("test44: for (ar[1]+ar[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[2] + ar[1]);
            if (v != -63)
            {
                Console.WriteLine("test45: for (ar[2]+ar[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[2] + ar[1]);
            if (v != -63)
            {
                Console.WriteLine("test46: for (ar[2]+ar[1])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[1] + ar[2]);
            if (v != -63)
            {
                Console.WriteLine("test47: for (ar[1]+ar[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[0] + ar[2]);
            if (v != -58)
            {
                Console.WriteLine("test48: for (ar[0]+ar[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            s.b = return_int(false, 33);
            v = (ar[2] + ar[0]);
            if (v != -58)
            {
                Console.WriteLine("test49: for (ar[2]+ar[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[2] + ar[0]);
            if (v != -58)
            {
                Console.WriteLine("test50: for (ar[2]+ar[0])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[0] + ar[2]);
            if (v != -58)
            {
                Console.WriteLine("test51: for (ar[0]+ar[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ar[2] + (ar[0] + ar[1]));
            if (v != -130)
            {
                Console.WriteLine("test52: for (ar[2]+(ar[0]+ar[1]))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((ar[0] + ar[1]) + ar[2]);
            if (v != -130)
            {
                Console.WriteLine("test53: for ((ar[0]+ar[1])+ar[2])  failed actual value {0} ", v);
                ret = ret + 1;
            }
            ad = return_int(false, 12);
            v = (((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))) * (((abc + c) - (a - (ad * a))) + r));
            if (v != 9758117)
            {
                Console.WriteLine("test54: for (((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((((abc + c) - (a - (ad * a))) + r) * ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))));
            if (v != 9758117)
            {
                Console.WriteLine("test55: for ((((abc+c)-(a-(ad*a)))+r)*((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b)));
            if (v != -21733)
            {
                Console.WriteLine("test56: for ((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (b * ((c * c) - (c + d))));
            if (v != -19213)
            {
                Console.WriteLine("test57: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b * ((c * c) - (c + d))) + a);
            if (v != -19213)
            {
                Console.WriteLine("test58: for ((b*((c*c)-(c+d)))+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * ((c * c) - (c + d)));
            if (v != -19171)
            {
                Console.WriteLine("test59: for (b*((c*c)-(c+d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((c * c) - (c + d)) * b);
            if (v != -19171)
            {
                Console.WriteLine("test60: for (((c*c)-(c+d))*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((c * c) - (c + d));
            if (v != 1009)
            {
                Console.WriteLine("test61: for ((c*c)-(c+d))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 961)
            {
                Console.WriteLine("test62: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 961)
            {
                Console.WriteLine("test63: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 961)
            {
                Console.WriteLine("test64: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 961)
            {
                Console.WriteLine("test65: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + d);
            if (v != -48)
            {
                Console.WriteLine("test66: for (c+d)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d + c);
            if (v != -48)
            {
                Console.WriteLine("test67: for (d+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d + c);
            if (v != -48)
            {
                Console.WriteLine("test68: for (d+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + d);
            if (v != -48)
            {
                Console.WriteLine("test69: for (c+d)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * c);
            if (v != 961)
            {
                Console.WriteLine("test70: for (c*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((c * c) - (c + d));
            if (v != 1009)
            {
                Console.WriteLine("test71: for ((c*c)-(c+d))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((c * c) - (c + d)) * b);
            if (v != -19171)
            {
                Console.WriteLine("test72: for (((c*c)-(c+d))*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * ((c * c) - (c + d)));
            if (v != -19171)
            {
                Console.WriteLine("test73: for (b*((c*c)-(c+d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b * ((c * c) - (c + d))) + a);
            if (v != -19213)
            {
                Console.WriteLine("test74: for ((b*((c*c)-(c+d)))+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (b * ((c * c) - (c + d))));
            if (v != -19213)
            {
                Console.WriteLine("test75: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((a * a) + a) + (a * b));
            if (v != 2520)
            {
                Console.WriteLine("test76: for (((a*a)+a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + ((a * a) + a));
            if (v != 2520)
            {
                Console.WriteLine("test77: for ((a*b)+((a*a)+a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + a);
            if (v != 1722)
            {
                Console.WriteLine("test78: for ((a*a)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * a));
            if (v != 1722)
            {
                Console.WriteLine("test79: for (a+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            s.c = return_int(false, 12);
            ad = return_int(false, 23);
            v = (a * a);
            if (v != 1764)
            {
                Console.WriteLine("test80: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1764)
            {
                Console.WriteLine("test81: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1764)
            {
                Console.WriteLine("test82: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1764)
            {
                Console.WriteLine("test83: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * a));
            if (v != 1722)
            {
                Console.WriteLine("test84: for (a+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + a);
            if (v != 1722)
            {
                Console.WriteLine("test85: for ((a*a)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 798)
            {
                Console.WriteLine("test86: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 798)
            {
                Console.WriteLine("test87: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 798)
            {
                Console.WriteLine("test88: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 798)
            {
                Console.WriteLine("test89: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + (a + (a * b)));
            if (v != 2520)
            {
                Console.WriteLine("test90: for ((a*a)+(a+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + ((a * a) + (a * b)));
            if (v != 2520)
            {
                Console.WriteLine("test91: for (a+((a*a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * b));
            if (v != 756)
            {
                Console.WriteLine("test92: for (a+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + a);
            if (v != 756)
            {
                Console.WriteLine("test93: for ((a*b)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 798)
            {
                Console.WriteLine("test94: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 798)
            {
                Console.WriteLine("test95: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 798)
            {
                Console.WriteLine("test96: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 798)
            {
                Console.WriteLine("test97: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            e = return_int(false, 11);
            v = ((a * b) + a);
            if (v != 756)
            {
                Console.WriteLine("test98: for ((a*b)+a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (a * b));
            if (v != 756)
            {
                Console.WriteLine("test99: for (a+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + (a * b));
            if (v != 2562)
            {
                Console.WriteLine("test100: for ((a*a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + (a * a));
            if (v != 2562)
            {
                Console.WriteLine("test101: for ((a*b)+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1764)
            {
                Console.WriteLine("test102: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1764)
            {
                Console.WriteLine("test103: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1764)
            {
                Console.WriteLine("test104: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * a);
            if (v != 1764)
            {
                Console.WriteLine("test105: for (a*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 798)
            {
                Console.WriteLine("test106: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            s.c = return_int(false, 8);
            v = (b * a);
            if (v != 798)
            {
                Console.WriteLine("test107: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b * a);
            if (v != 798)
            {
                Console.WriteLine("test108: for (b*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * b);
            if (v != 798)
            {
                Console.WriteLine("test109: for (a*b)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + (a * a));
            if (v != 2562)
            {
                Console.WriteLine("test110: for ((a*b)+(a*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * a) + (a * b));
            if (v != 2562)
            {
                Console.WriteLine("test111: for ((a*a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * b) + ((a * a) + a));
            if (v != 2520)
            {
                Console.WriteLine("test112: for ((a*b)+((a*a)+a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((a * a) + a) + (a * b));
            if (v != 2520)
            {
                Console.WriteLine("test113: for (((a*a)+a)+(a*b))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a + (b * ((c * c) - (c + d))));
            if (v != -19213)
            {
                Console.WriteLine("test114: for (a+(b*((c*c)-(c+d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            c = return_int(false, 32);
            v = ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b)));
            if (v != -21733)
            {
                Console.WriteLine("test115: for ((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((abc + c) - (a - (ad * a))) + r);
            if (v != -848)
            {
                Console.WriteLine("test116: for (((abc+c)-(a-(ad*a)))+r)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            s.c = return_int(false, 99);
            v = (r + ((abc + c) - (a - (ad * a))));
            if (v != -848)
            {
                Console.WriteLine("test117: for (r+((abc+c)-(a-(ad*a))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            ar[0] = return_int(false, -13);
            s.c = return_int(false, 47);
            v = ((abc + c) - (a - (ad * a)));
            if (v != -882)
            {
                Console.WriteLine("test118: for ((abc+c)-(a-(ad*a)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (abc + c);
            if (v != 42)
            {
                Console.WriteLine("test119: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + abc);
            if (v != 42)
            {
                Console.WriteLine("test120: for (c+abc)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c + abc);
            if (v != 42)
            {
                Console.WriteLine("test121: for (c+abc)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (abc + c);
            if (v != 42)
            {
                Console.WriteLine("test122: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a - (ad * a));
            if (v != 924)
            {
                Console.WriteLine("test123: for (a-(ad*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ad * a);
            if (v != -966)
            {
                Console.WriteLine("test124: for (ad*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * ad);
            if (v != -966)
            {
                Console.WriteLine("test125: for (a*ad)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * ad);
            if (v != -966)
            {
                Console.WriteLine("test126: for (a*ad)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ad * a);
            if (v != -966)
            {
                Console.WriteLine("test127: for (ad*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a - (ad * a));
            if (v != 924)
            {
                Console.WriteLine("test128: for (a-(ad*a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (abc + c);
            if (v != 42)
            {
                Console.WriteLine("test129: for (abc+c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((abc + c) - (a - (ad * a)));
            if (v != -882)
            {
                Console.WriteLine("test130: for ((abc+c)-(a-(ad*a)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (r + ((abc + c) - (a - (ad * a))));
            if (v != -848)
            {
                Console.WriteLine("test131: for (r+((abc+c)-(a-(ad*a))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((abc + c) - (a - (ad * a))) + r);
            if (v != -848)
            {
                Console.WriteLine("test132: for (((abc+c)-(a-(ad*a)))+r)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((((abc + c) - (a - (ad * a))) + r) * ((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))));
            if (v != 18429584)
            {
                Console.WriteLine("test133: for ((((abc+c)-(a-(ad*a)))+r)*((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((a + (b * ((c * c) - (c + d)))) - (((a * a) + a) + (a * b))) * (((abc + c) - (a - (ad * a))) + r));
            if (v != 18429584)
            {
                Console.WriteLine("test134: for (((a+(b*((c*c)-(c+d))))-(((a*a)+a)+(a*b)))*(((abc+c)-(a-(ad*a)))+r))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))))));
            if (v != 133723422)
            {
                Console.WriteLine("test135: for (a*(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))))) * a);
            if (v != 133723422)
            {
                Console.WriteLine("test136: for ((b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)))));
            if (v != -3183891)
            {
                Console.WriteLine("test137: for (b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))));
            if (v != 3183872)
            {
                Console.WriteLine("test138: for (c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))) * c);
            if (v != 3183872)
            {
                Console.WriteLine("test139: for (((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)));
            if (v != 99496)
            {
                Console.WriteLine("test140: for ((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)) + (d - e));
            if (v != 99496)
            {
                Console.WriteLine("test141: for (((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))+(d-e))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - e);
            if (v != -28)
            {
                Console.WriteLine("test142: for (d-e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - e);
            if (v != -28)
            {
                Console.WriteLine("test143: for (d-e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab));
            if (v != 99524)
            {
                Console.WriteLine("test144: for ((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((f - a) + (a * (b - (c * (d - (e * f)))))) + f);
            if (v != 96248)
            {
                Console.WriteLine("test145: for (((f-a)+(a*(b-(c*(d-(e*f))))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + ((f - a) + (a * (b - (c * (d - (e * f)))))));
            if (v != 96248)
            {
                Console.WriteLine("test146: for (f+((f-a)+(a*(b-(c*(d-(e*f)))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((f - a) + (a * (b - (c * (d - (e * f))))));
            if (v != 96256)
            {
                Console.WriteLine("test147: for ((f-a)+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + (f - a));
            if (v != 96256)
            {
                Console.WriteLine("test148: for ((a*(b-(c*(d-(e*f)))))+(f-a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f - a);
            if (v != 34)
            {
                Console.WriteLine("test149: for (f-a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f - a);
            if (v != 34)
            {
                Console.WriteLine("test150: for (f-a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 96222)
            {
                Console.WriteLine("test151: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 96222)
            {
                Console.WriteLine("test152: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != -2291)
            {
                Console.WriteLine("test153: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != 2272)
            {
                Console.WriteLine("test154: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != 2272)
            {
                Console.WriteLine("test155: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != 71)
            {
                Console.WriteLine("test156: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != -88)
            {
                Console.WriteLine("test157: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != -88)
            {
                Console.WriteLine("test158: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != -88)
            {
                Console.WriteLine("test159: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != -88)
            {
                Console.WriteLine("test160: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != 71)
            {
                Console.WriteLine("test161: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != 2272)
            {
                Console.WriteLine("test162: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != 2272)
            {
                Console.WriteLine("test163: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != -2291)
            {
                Console.WriteLine("test164: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            s.b = return_int(false, -51);
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 96222)
            {
                Console.WriteLine("test165: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 96222)
            {
                Console.WriteLine("test166: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + (f - a));
            if (v != 96256)
            {
                Console.WriteLine("test167: for ((a*(b-(c*(d-(e*f)))))+(f-a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((f - a) + (a * (b - (c * (d - (e * f))))));
            if (v != 96256)
            {
                Console.WriteLine("test168: for ((f-a)+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((f - a) + ((a * (b - (c * (d - (e * f))))) + f));
            if (v != 96248)
            {
                Console.WriteLine("test169: for ((f-a)+((a*(b-(c*(d-(e*f)))))+f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + ((f - a) + f));
            if (v != 96248)
            {
                Console.WriteLine("test170: for ((a*(b-(c*(d-(e*f)))))+((f-a)+f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + f);
            if (v != 96214)
            {
                Console.WriteLine("test171: for ((a*(b-(c*(d-(e*f)))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + (a * (b - (c * (d - (e * f))))));
            if (v != 96214)
            {
                Console.WriteLine("test172: for (f+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 96222)
            {
                Console.WriteLine("test173: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 96222)
            {
                Console.WriteLine("test174: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * (d - (e * f))));
            if (v != -2291)
            {
                Console.WriteLine("test175: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            r = return_int(false, -10);
            v = (c * (d - (e * f)));
            if (v != 2272)
            {
                Console.WriteLine("test176: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - (e * f)) * c);
            if (v != 2272)
            {
                Console.WriteLine("test177: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != 71)
            {
                Console.WriteLine("test178: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != -88)
            {
                Console.WriteLine("test179: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != -88)
            {
                Console.WriteLine("test180: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f * e);
            if (v != -88)
            {
                Console.WriteLine("test181: for (f*e)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (e * f);
            if (v != -88)
            {
                Console.WriteLine("test182: for (e*f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (d - (e * f));
            if (v != 71)
            {
                Console.WriteLine("test183: for (d-(e*f))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            abc = return_int(false, 68);
            v = ((d - (e * f)) * c);
            if (v != 2272)
            {
                Console.WriteLine("test184: for ((d-(e*f))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * (d - (e * f)));
            if (v != 2272)
            {
                Console.WriteLine("test185: for (c*(d-(e*f)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            ar[0] = return_int(false, -46);
            r = return_int(false, 65);
            v = (b - (c * (d - (e * f))));
            if (v != -2291)
            {
                Console.WriteLine("test186: for (b-(c*(d-(e*f))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * (d - (e * f)))) * a);
            if (v != 96222)
            {
                Console.WriteLine("test187: for ((b-(c*(d-(e*f))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * (d - (e * f)))));
            if (v != 96222)
            {
                Console.WriteLine("test188: for (a*(b-(c*(d-(e*f)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + (a * (b - (c * (d - (e * f))))));
            if (v != 96214)
            {
                Console.WriteLine("test189: for (f+(a*(b-(c*(d-(e*f))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((a * (b - (c * (d - (e * f))))) + f);
            if (v != 96214)
            {
                Console.WriteLine("test190: for ((a*(b-(c*(d-(e*f)))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((f - a) + f);
            if (v != 26)
            {
                Console.WriteLine("test191: for ((f-a)+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            f = return_int(false, -56);
            v = (f + (f - a));
            if (v != -70)
            {
                Console.WriteLine("test192: for (f+(f-a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f - a);
            if (v != -14)
            {
                Console.WriteLine("test193: for (f-a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f - a);
            if (v != -14)
            {
                Console.WriteLine("test194: for (f-a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + (f - a));
            if (v != -70)
            {
                Console.WriteLine("test195: for (f+(f-a))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((f - a) + f);
            if (v != -70)
            {
                Console.WriteLine("test196: for ((f-a)+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (f + ((f - a) + (a * (b - (c * (d - (e * f)))))));
            if (v != 805784)
            {
                Console.WriteLine("test197: for (f+((f-a)+(a*(b-(c*(d-(e*f)))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((f - a) + (a * (b - (c * (d - (e * f)))))) + f);
            if (v != 805784)
            {
                Console.WriteLine("test198: for (((f-a)+(a*(b-(c*(d-(e*f))))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            s.c = return_int(false, 2);
            v = (a * ab);
            if (v != -3276)
            {
                Console.WriteLine("test199: for (a*ab)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ab * a);
            if (v != -3276)
            {
                Console.WriteLine("test200: for (ab*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (ab * a);
            if (v != -3276)
            {
                Console.WriteLine("test201: for (ab*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * ab);
            if (v != -3276)
            {
                Console.WriteLine("test202: for (a*ab)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((f - a) + (a * (b - (c * (d - (e * f)))))) + f);
            if (v != 805784)
            {
                Console.WriteLine("test203: for (((f-a)+(a*(b-(c*(d-(e*f))))))+f)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            ad = return_int(false, 16);
            v = ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab));
            if (v != 809060)
            {
                Console.WriteLine("test204: for ((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)) + (d - e));
            if (v != 809032)
            {
                Console.WriteLine("test205: for (((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))+(d-e))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)));
            if (v != 809032)
            {
                Console.WriteLine("test206: for ((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))) * c);
            if (v != 25889024)
            {
                Console.WriteLine("test207: for (((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))*c)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))));
            if (v != 25889024)
            {
                Console.WriteLine("test208: for (c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab)))));
            if (v != -25889043)
            {
                Console.WriteLine("test209: for (b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = ((b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))))) * a);
            if (v != 1087339806)
            {
                Console.WriteLine("test210: for ((b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab)))))*a)  failed actual value {0} ", v);
                ret = ret + 1;
            }
            v = (a * (b - (c * ((d - e) + ((((f - a) + (a * (b - (c * (d - (e * f)))))) + f) - (a * ab))))));
            if (v != 1087339806)
            {
                Console.WriteLine("test211: for (a*(b-(c*((d-e)+((((f-a)+(a*(b-(c*(d-(e*f))))))+f)-(a*ab))))))  failed actual value {0} ", v);
                ret = ret + 1;
            }
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

