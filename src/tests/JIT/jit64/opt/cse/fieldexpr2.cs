// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
//((((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))-(((s.b*s.b)+s.g)-((s.c+s.b)*s.k)))
//permutations for  ((((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))-(((s.b*s.b)+s.g)-((s.c+s.b)*s.k)))
//((((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))-(((s.b*s.b)+s.g)-((s.c+s.b)*s.k)))
//(((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))
//(((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k)))+((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h)))))
//((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))
//(((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))
//((s.e+((s.a+(s.b*s.c))-(s.c*s.d)))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))
//((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//(((s.a+(s.b*s.c))-(s.c*s.d))*s.e)
//s.e
//((s.a+(s.b*s.c))-(s.c*s.d))
//(s.a+(s.b*s.c))
//((s.b*s.c)+s.a)
//s.a
//(s.b*s.c)
//(s.c*s.b)
//s.b
//s.c
//(s.c*s.b)
//(s.b*s.c)
//((s.b*s.c)+s.a)
//(s.a+(s.b*s.c))
//(s.c*s.d)
//(s.d*s.c)
//s.c
//s.d
//(s.d*s.c)
//(s.c*s.d)
//(s.a+(s.b*s.c))
//((s.a+(s.b*s.c))-(s.c*s.d))
//(((s.a+(s.b*s.c))-(s.c*s.d))*s.e)
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//((s.f+(s.e*s.f))-(s.g*s.h))
//(s.f+(s.e*s.f))
//((s.e*s.f)+s.f)
//s.f
//(s.e*s.f)
//(s.f*s.e)
//s.e
//s.f
//(s.f*s.e)
//(s.e*s.f)
//((s.e*s.f)+s.f)
//(s.f+(s.e*s.f))
//(s.g*s.h)
//(s.h*s.g)
//s.g
//s.h
//(s.h*s.g)
//(s.g*s.h)
//(s.f+(s.e*s.f))
//((s.f+(s.e*s.f))-(s.g*s.h))
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))
//(s.e+((s.a+(s.b*s.c))-(s.c*s.d)))
//(((s.a+(s.b*s.c))-(s.c*s.d))+s.e)
//s.e
//((s.a+(s.b*s.c))-(s.c*s.d))
//(s.a+(s.b*s.c))
//((s.b*s.c)+s.a)
//s.a
//(s.b*s.c)
//(s.c*s.b)
//s.b
//s.c
//(s.c*s.b)
//(s.b*s.c)
//((s.b*s.c)+s.a)
//(s.a+(s.b*s.c))
//(s.c*s.d)
//(s.d*s.c)
//s.c
//s.d
//(s.d*s.c)
//(s.c*s.d)
//(s.a+(s.b*s.c))
//((s.a+(s.b*s.c))-(s.c*s.d))
//(((s.a+(s.b*s.c))-(s.c*s.d))+s.e)
//(s.e+((s.a+(s.b*s.c))-(s.c*s.d)))
//(s.e+(((s.a+(s.b*s.c))-(s.c*s.d))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))))
//(((s.a+(s.b*s.c))-(s.c*s.d))+(s.e+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))))
//(((s.a+(s.b*s.c))-(s.c*s.d))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))
//(((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+((s.a+(s.b*s.c))-(s.c*s.d)))
//((s.a+(s.b*s.c))-(s.c*s.d))
//(s.a+(s.b*s.c))
//((s.b*s.c)+s.a)
//s.a
//(s.b*s.c)
//(s.c*s.b)
//s.b
//s.c
//(s.c*s.b)
//(s.b*s.c)
//((s.b*s.c)+s.a)
//(s.a+(s.b*s.c))
//(s.c*s.d)
//(s.d*s.c)
//s.c
//s.d
//(s.d*s.c)
//(s.c*s.d)
//(s.a+(s.b*s.c))
//((s.a+(s.b*s.c))-(s.c*s.d))
//((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//(((s.a+(s.b*s.c))-(s.c*s.d))*s.e)
//s.e
//((s.a+(s.b*s.c))-(s.c*s.d))
//(s.a+(s.b*s.c))
//((s.b*s.c)+s.a)
//s.a
//(s.b*s.c)
//(s.c*s.b)
//s.b
//s.c
//(s.c*s.b)
//(s.b*s.c)
//((s.b*s.c)+s.a)
//(s.a+(s.b*s.c))
//(s.c*s.d)
//(s.d*s.c)
//s.c
//s.d
//(s.d*s.c)
//(s.c*s.d)
//(s.a+(s.b*s.c))
//((s.a+(s.b*s.c))-(s.c*s.d))
//(((s.a+(s.b*s.c))-(s.c*s.d))*s.e)
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//((s.f+(s.e*s.f))-(s.g*s.h))
//(s.f+(s.e*s.f))
//((s.e*s.f)+s.f)
//s.f
//(s.e*s.f)
//(s.f*s.e)
//s.e
//s.f
//(s.f*s.e)
//(s.e*s.f)
//((s.e*s.f)+s.f)
//(s.f+(s.e*s.f))
//(s.g*s.h)
//(s.h*s.g)
//s.g
//s.h
//(s.h*s.g)
//(s.g*s.h)
//(s.f+(s.e*s.f))
//((s.f+(s.e*s.f))-(s.g*s.h))
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))
//(((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+((s.a+(s.b*s.c))-(s.c*s.d)))
//(((s.a+(s.b*s.c))-(s.c*s.d))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))
//(s.e+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))
//(((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+s.e)
//s.e
//((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//(((s.a+(s.b*s.c))-(s.c*s.d))*s.e)
//s.e
//((s.a+(s.b*s.c))-(s.c*s.d))
//(s.a+(s.b*s.c))
//((s.b*s.c)+s.a)
//s.a
//(s.b*s.c)
//(s.c*s.b)
//s.b
//s.c
//(s.c*s.b)
//(s.b*s.c)
//((s.b*s.c)+s.a)
//(s.a+(s.b*s.c))
//(s.c*s.d)
//(s.d*s.c)
//s.c
//s.d
//(s.d*s.c)
//(s.c*s.d)
//(s.a+(s.b*s.c))
//((s.a+(s.b*s.c))-(s.c*s.d))
//(((s.a+(s.b*s.c))-(s.c*s.d))*s.e)
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//((s.f+(s.e*s.f))-(s.g*s.h))
//(s.f+(s.e*s.f))
//((s.e*s.f)+s.f)
//s.f
//(s.e*s.f)
//(s.f*s.e)
//s.e
//s.f
//(s.f*s.e)
//(s.e*s.f)
//((s.e*s.f)+s.f)
//(s.f+(s.e*s.f))
//(s.g*s.h)
//(s.h*s.g)
//s.g
//s.h
//(s.h*s.g)
//(s.g*s.h)
//(s.f+(s.e*s.f))
//((s.f+(s.e*s.f))-(s.g*s.h))
//(s.e*((s.a+(s.b*s.c))-(s.c*s.d)))
//((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))
//(((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+s.e)
//(s.e+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))
//((s.e+((s.a+(s.b*s.c))-(s.c*s.d)))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))
//(((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))
//(s.g+((s.f+(s.e*s.f))-(s.g*s.h)))
//(((s.f+(s.e*s.f))-(s.g*s.h))+s.g)
//s.g
//((s.f+(s.e*s.f))-(s.g*s.h))
//(s.f+(s.e*s.f))
//((s.e*s.f)+s.f)
//s.f
//(s.e*s.f)
//(s.f*s.e)
//s.e
//s.f
//(s.f*s.e)
//(s.e*s.f)
//((s.e*s.f)+s.f)
//(s.f+(s.e*s.f))
//(s.g*s.h)
//(s.h*s.g)
//s.g
//s.h
//(s.h*s.g)
//(s.g*s.h)
//(s.f+(s.e*s.f))
//((s.f+(s.e*s.f))-(s.g*s.h))
//(((s.f+(s.e*s.f))-(s.g*s.h))+s.g)
//(s.g+((s.f+(s.e*s.f))-(s.g*s.h)))
//(((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))
//((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))
//((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k)))
//((((s.a+s.b)+s.g)-((s.c+s.b)*s.k))*(s.a+((s.h+(s.f+s.g))-(s.p*s.q))))
//(s.a+((s.h+(s.f+s.g))-(s.p*s.q)))
//(((s.h+(s.f+s.g))-(s.p*s.q))+s.a)
//s.a
//((s.h+(s.f+s.g))-(s.p*s.q))
//(s.h+(s.f+s.g))
//((s.f+s.g)+s.h)
//s.h
//(s.f+s.g)
//(s.g+s.f)
//s.f
//s.g
//(s.g+s.f)
//(s.f+s.g)
//(s.f+(s.g+s.h))
//(s.g+(s.f+s.h))
//(s.g+s.h)
//(s.h+s.g)
//s.g
//s.h
//(s.h+s.g)
//(s.g+s.h)
//(s.f+s.h)
//(s.h+s.f)
//s.f
//s.h
//(s.h+s.f)
//(s.f+s.h)
//((s.f+s.g)+s.h)
//(s.h+(s.f+s.g))
//(s.p*s.q)
//(s.q*s.p)
//s.p
//s.q
//(s.q*s.p)
//(s.p*s.q)
//(s.h+(s.f+s.g))
//((s.h+(s.f+s.g))-(s.p*s.q))
//(((s.h+(s.f+s.g))-(s.p*s.q))+s.a)
//(s.a+((s.h+(s.f+s.g))-(s.p*s.q)))
//(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))
//((s.a+s.b)+s.g)
//(s.g+(s.a+s.b))
//(s.a+s.b)
//(s.b+s.a)
//s.a
//s.b
//(s.b+s.a)
//(s.a+s.b)
//s.g
//(s.a+(s.b+s.g))
//(s.b+(s.a+s.g))
//(s.b+s.g)
//(s.g+s.b)
//s.b
//s.g
//(s.g+s.b)
//(s.b+s.g)
//(s.a+s.g)
//(s.g+s.a)
//s.a
//s.g
//(s.g+s.a)
//(s.a+s.g)
//(s.g+(s.a+s.b))
//((s.a+s.b)+s.g)
//((s.c+s.b)*s.k)
//(s.k*(s.c+s.b))
//(s.c+s.b)
//(s.b+s.c)
//s.c
//s.b
//(s.b+s.c)
//(s.c+s.b)
//s.k
//(s.k*(s.c+s.b))
//((s.c+s.b)*s.k)
//((s.a+s.b)+s.g)
//(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))
//((((s.a+s.b)+s.g)-((s.c+s.b)*s.k))*(s.a+((s.h+(s.f+s.g))-(s.p*s.q))))
//((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k)))
//(((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k)))+((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h)))))
//(((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))
//(((s.b*s.b)+s.g)-((s.c+s.b)*s.k))
//((s.b*s.b)+s.g)
//(s.g+(s.b*s.b))
//(s.b*s.b)
//(s.b*s.b)
//s.b
//s.b
//(s.b*s.b)
//(s.b*s.b)
//s.g
//(s.g+(s.b*s.b))
//((s.b*s.b)+s.g)
//((s.c+s.b)*s.k)
//(s.k*(s.c+s.b))
//(s.c+s.b)
//(s.b+s.c)
//s.c
//s.b
//(s.b+s.c)
//(s.c+s.b)
//s.k
//(s.k*(s.c+s.b))
//((s.c+s.b)*s.k)
//((s.b*s.b)+s.g)
//(((s.b*s.b)+s.g)-((s.c+s.b)*s.k))
//(((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))
//((((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))-(((s.b*s.b)+s.g)-((s.c+s.b)*s.k)))
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

            s.e = return_int(false, 47);
            s.a = return_int(false, 16);
            s.b = return_int(false, -39);
            s.c = return_int(false, 27);
            s.d = return_int(false, 61);
            s.f = return_int(false, 32);
            s.g = return_int(false, 4);
            s.h = return_int(false, 99);
            s.p = return_int(false, 122);
            s.q = return_int(false, -14);
            s.k = return_int(false, 124);

            int v;

#if LOOP
			do {
#endif
            v = ((((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)))) + ((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k)))) - (((s.b * s.b) + s.g) - ((s.c + s.b) * s.k)));
            if (v != 2596789)
            {
                Console.WriteLine("test0: for ((((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))-(((s.b*s.b)+s.g)-((s.c+s.b)*s.k)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)))) + ((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k))));
            if (v != 2599802)
            {
                Console.WriteLine("test1: for (((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k))) + ((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)))));
            if (v != 2599802)
            {
                Console.WriteLine("test2: for (((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k)))+((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.d = return_int(false, 33);
            v = ((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h))));
            if (v != -94781)
            {
                Console.WriteLine("test3: for ((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d))));
            if (v != -93637)
            {
                Console.WriteLine("test4: for (((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e + ((s.a + (s.b * s.c)) - (s.c * s.d))) + ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))));
            if (v != -93637)
            {
                Console.WriteLine("test5: for ((s.e+((s.a+(s.b*s.c))-(s.c*s.d)))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h)));
            if (v != -91756)
            {
                Console.WriteLine("test6: for ((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -90616)
            {
                Console.WriteLine("test7: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) * s.e);
            if (v != -90616)
            {
                Console.WriteLine("test8: for (((s.a+(s.b*s.c))-(s.c*s.d))*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -1928)
            {
                Console.WriteLine("test9: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

#if LOOP
				do {
#endif
            v = (s.a + (s.b * s.c));
            if (v != -1037)
            {
                Console.WriteLine("test10: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -1037)
            {
                Console.WriteLine("test11: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -1053)
            {
                Console.WriteLine("test12: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -1053)
            {
                Console.WriteLine("test13: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -1053)
            {
                Console.WriteLine("test14: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -1053)
            {
                Console.WriteLine("test15: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -1037)
            {
                Console.WriteLine("test16: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -1037)
            {
                Console.WriteLine("test17: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 891)
            {
                Console.WriteLine("test18: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 891)
            {
                Console.WriteLine("test19: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 891)
            {
                Console.WriteLine("test20: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 891)
            {
                Console.WriteLine("test21: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -1037)
            {
                Console.WriteLine("test22: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
				} while (v == 0);
#endif

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -1928)
            {
                Console.WriteLine("test23: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) * s.e);
            if (v != -90616)
            {
                Console.WriteLine("test24: for (((s.a+(s.b*s.c))-(s.c*s.d))*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -90616)
            {
                Console.WriteLine("test25: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + (s.e * s.f)) - (s.g * s.h));
            if (v != 1140)
            {
                Console.WriteLine("test26: for ((s.f+(s.e*s.f))-(s.g*s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

#if LOOP
			do {
#endif

            v = (s.f + (s.e * s.f));
            if (v != 1536)
            {
                Console.WriteLine("test27: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * s.f) + s.f);
            if (v != 1536)
            {
                Console.WriteLine("test28: for ((s.e*s.f)+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * s.f);
            if (v != 1504)
            {
                Console.WriteLine("test29: for (s.e*s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f * s.e);
            if (v != 1504)
            {
                Console.WriteLine("test30: for (s.f*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f * s.e);
            if (v != 1504)
            {
                Console.WriteLine("test31: for (s.f*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * s.f);
            if (v != 1504)
            {
                Console.WriteLine("test32: for (s.e*s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * s.f) + s.f);
            if (v != 1536)
            {
                Console.WriteLine("test33: for ((s.e*s.f)+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 1536)
            {
                Console.WriteLine("test34: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g * s.h);
            if (v != 396)
            {
                Console.WriteLine("test35: for (s.g*s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

#if LOOP
			do {
#endif

            v = (s.h * s.g);
            if (v != 396)
            {
                Console.WriteLine("test36: for (s.h*s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h * s.g);
            if (v != 396)
            {
                Console.WriteLine("test37: for (s.h*s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g * s.h);
            if (v != 396)
            {
                Console.WriteLine("test38: for (s.g*s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 1536)
            {
                Console.WriteLine("test39: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + (s.e * s.f)) - (s.g * s.h));
            if (v != 1140)
            {
                Console.WriteLine("test40: for ((s.f+(s.e*s.f))-(s.g*s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -90616)
            {
                Console.WriteLine("test41: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h)));
            if (v != -91756)
            {
                Console.WriteLine("test42: for ((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -1881)
            {
                Console.WriteLine("test43: for (s.e+((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) + s.e);
            if (v != -1881)
            {
                Console.WriteLine("test44: for (((s.a+(s.b*s.c))-(s.c*s.d))+s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -1928)
            {
                Console.WriteLine("test45: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

#if LOOP
         }   while (v==0);
#endif
            v = (s.a + (s.b * s.c));
            if (v != -1037)
            {
                Console.WriteLine("test46: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

#if LOOP
         }   while (v==0);
#endif
            v = ((s.b * s.c) + s.a);
            if (v != -1037)
            {
                Console.WriteLine("test47: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.a = return_int(false, 11);
            v = (s.b * s.c);
            if (v != -1053)
            {
                Console.WriteLine("test48: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -1053)
            {
                Console.WriteLine("test49: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -1053)
            {
                Console.WriteLine("test50: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -1053)
            {
                Console.WriteLine("test51: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -1042)
            {
                Console.WriteLine("test52: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

#if TRY
				try {
#endif
            v = (s.a + (s.b * s.c));
            if (v != -1042)
            {
                Console.WriteLine("test53: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 891)
            {
                Console.WriteLine("test54: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 891)
            {
                Console.WriteLine("test55: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 891)
            {
                Console.WriteLine("test56: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 891)
            {
                Console.WriteLine("test57: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if TRY            
					throw new Exception("Test exception");
				}
				catch (System.Exception) {
					Console.WriteLine("In catch");
#endif
            s.q = return_int(false, 33);

#if TRY            
				}
				
#endif
            v = (s.a + (s.b * s.c));
            if (v != -1042)
            {
                Console.WriteLine("test58: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -1933)
            {
                Console.WriteLine("test59: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) + s.e);
            if (v != -1886)
            {
                Console.WriteLine("test60: for (((s.a+(s.b*s.c))-(s.c*s.d))+s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -1886)
            {
                Console.WriteLine("test61: for (s.e+((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e + (((s.a + (s.b * s.c)) - (s.c * s.d)) + ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h)))));
            if (v != -93877)
            {
                Console.WriteLine("test62: for (s.e+(((s.a+(s.b*s.c))-(s.c*s.d))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) + (s.e + ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h)))));
            if (v != -93877)
            {
                Console.WriteLine("test63: for (((s.a+(s.b*s.c))-(s.c*s.d))+(s.e+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) + ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))));
            if (v != -93924)
            {
                Console.WriteLine("test64: for (((s.a+(s.b*s.c))-(s.c*s.d))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -93924)
            {
                Console.WriteLine("test65: for (((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -1933)
            {
                Console.WriteLine("test66: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -1042)
            {
                Console.WriteLine("test67: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -1042)
            {
                Console.WriteLine("test68: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -1053)
            {
                Console.WriteLine("test69: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -1053)
            {
                Console.WriteLine("test70: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -1053)
            {
                Console.WriteLine("test71: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -1053)
            {
                Console.WriteLine("test72: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -1042)
            {
                Console.WriteLine("test73: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -1042)
            {
                Console.WriteLine("test74: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 891)
            {
                Console.WriteLine("test75: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 891)
            {
                Console.WriteLine("test76: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 891)
            {
                Console.WriteLine("test77: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 891)
            {
                Console.WriteLine("test78: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.k = return_int(false, -3);
            v = (s.a + (s.b * s.c));
            if (v != -1042)
            {
                Console.WriteLine("test79: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -1933)
            {
                Console.WriteLine("test80: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h)));
            if (v != -91991)
            {
                Console.WriteLine("test81: for ((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -90851)
            {
                Console.WriteLine("test82: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) * s.e);
            if (v != -90851)
            {
                Console.WriteLine("test83: for (((s.a+(s.b*s.c))-(s.c*s.d))*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -1933)
            {
                Console.WriteLine("test84: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.f = return_int(false, 42);
            v = (s.a + (s.b * s.c));
            if (v != -1042)
            {
                Console.WriteLine("test85: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -1042)
            {
                Console.WriteLine("test86: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -1053)
            {
                Console.WriteLine("test87: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -1053)
            {
                Console.WriteLine("test88: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -1053)
            {
                Console.WriteLine("test89: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -1053)
            {
                Console.WriteLine("test90: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -1042)
            {
                Console.WriteLine("test91: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -1042)
            {
                Console.WriteLine("test92: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 891)
            {
                Console.WriteLine("test93: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 891)
            {
                Console.WriteLine("test94: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.f = return_int(false, 58);
            v = (s.d * s.c);
            if (v != 891)
            {
                Console.WriteLine("test95: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 891)
            {
                Console.WriteLine("test96: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -1042)
            {
                Console.WriteLine("test97: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -1933)
            {
                Console.WriteLine("test98: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) * s.e);
            if (v != -90851)
            {
                Console.WriteLine("test99: for (((s.a+(s.b*s.c))-(s.c*s.d))*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -90851)
            {
                Console.WriteLine("test100: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + (s.e * s.f)) - (s.g * s.h));
            if (v != 2388)
            {
                Console.WriteLine("test101: for ((s.f+(s.e*s.f))-(s.g*s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 2784)
            {
                Console.WriteLine("test102: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * s.f) + s.f);
            if (v != 2784)
            {
                Console.WriteLine("test103: for ((s.e*s.f)+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * s.f);
            if (v != 2726)
            {
                Console.WriteLine("test104: for (s.e*s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f * s.e);
            if (v != 2726)
            {
                Console.WriteLine("test105: for (s.f*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f * s.e);
            if (v != 2726)
            {
                Console.WriteLine("test106: for (s.f*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * s.f);
            if (v != 2726)
            {
                Console.WriteLine("test107: for (s.e*s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * s.f) + s.f);
            if (v != 2784)
            {
                Console.WriteLine("test108: for ((s.e*s.f)+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 2784)
            {
                Console.WriteLine("test109: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g * s.h);
            if (v != 396)
            {
                Console.WriteLine("test110: for (s.g*s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h * s.g);
            if (v != 396)
            {
                Console.WriteLine("test111: for (s.h*s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h * s.g);
            if (v != 396)
            {
                Console.WriteLine("test112: for (s.h*s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g * s.h);
            if (v != 396)
            {
                Console.WriteLine("test113: for (s.g*s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 2784)
            {
                Console.WriteLine("test114: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + (s.e * s.f)) - (s.g * s.h));
            if (v != 2388)
            {
                Console.WriteLine("test115: for ((s.f+(s.e*s.f))-(s.g*s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -90851)
            {
                Console.WriteLine("test116: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h)));
            if (v != -93239)
            {
                Console.WriteLine("test117: for ((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -95172)
            {
                Console.WriteLine("test118: for (((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) + ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))));
            if (v != -95172)
            {
                Console.WriteLine("test119: for (((s.a+(s.b*s.c))-(s.c*s.d))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.p = return_int(false, 85);
            v = (s.e + ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))));
            if (v != -93192)
            {
                Console.WriteLine("test120: for (s.e+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + s.e);
            if (v != -93192)
            {
                Console.WriteLine("test121: for (((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h)));
            if (v != -93239)
            {
                Console.WriteLine("test122: for ((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -90851)
            {
                Console.WriteLine("test123: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) * s.e);
            if (v != -90851)
            {
                Console.WriteLine("test124: for (((s.a+(s.b*s.c))-(s.c*s.d))*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.c = return_int(false, 95);
            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -6829)
            {
                Console.WriteLine("test125: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -3694)
            {
                Console.WriteLine("test126: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -3694)
            {
                Console.WriteLine("test127: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -3705)
            {
                Console.WriteLine("test128: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -3705)
            {
                Console.WriteLine("test129: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.b);
            if (v != -3705)
            {
                Console.WriteLine("test130: for (s.c*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.c);
            if (v != -3705)
            {
                Console.WriteLine("test131: for (s.b*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.c) + s.a);
            if (v != -3694)
            {
                Console.WriteLine("test132: for ((s.b*s.c)+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -3694)
            {
                Console.WriteLine("test133: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 3135)
            {
                Console.WriteLine("test134: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 3135)
            {
                Console.WriteLine("test135: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.d * s.c);
            if (v != 3135)
            {
                Console.WriteLine("test136: for (s.d*s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c * s.d);
            if (v != 3135)
            {
                Console.WriteLine("test137: for (s.c*s.d)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b * s.c));
            if (v != -3694)
            {
                Console.WriteLine("test138: for (s.a+(s.b*s.c))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + (s.b * s.c)) - (s.c * s.d));
            if (v != -6829)
            {
                Console.WriteLine("test139: for ((s.a+(s.b*s.c))-(s.c*s.d))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + (s.b * s.c)) - (s.c * s.d)) * s.e);
            if (v != -320963)
            {
                Console.WriteLine("test140: for (((s.a+(s.b*s.c))-(s.c*s.d))*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -320963)
            {
                Console.WriteLine("test141: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + (s.e * s.f)) - (s.g * s.h));
            if (v != 2388)
            {
                Console.WriteLine("test142: for ((s.f+(s.e*s.f))-(s.g*s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.q = return_int(false, 53);
            v = (s.f + (s.e * s.f));
            if (v != 2784)
            {
                Console.WriteLine("test143: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * s.f) + s.f);
            if (v != 2784)
            {
                Console.WriteLine("test144: for ((s.e*s.f)+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * s.f);
            if (v != 2726)
            {
                Console.WriteLine("test145: for (s.e*s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.f = return_int(false, 21);
            v = (s.f * s.e);
            if (v != 987)
            {
                Console.WriteLine("test146: for (s.f*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f * s.e);
            if (v != 987)
            {
                Console.WriteLine("test147: for (s.f*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * s.f);
            if (v != 987)
            {
                Console.WriteLine("test148: for (s.e*s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * s.f) + s.f);
            if (v != 1008)
            {
                Console.WriteLine("test149: for ((s.e*s.f)+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 1008)
            {
                Console.WriteLine("test150: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g * s.h);
            if (v != 396)
            {
                Console.WriteLine("test151: for (s.g*s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h * s.g);
            if (v != 396)
            {
                Console.WriteLine("test152: for (s.h*s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h * s.g);
            if (v != 396)
            {
                Console.WriteLine("test153: for (s.h*s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g * s.h);
            if (v != 396)
            {
                Console.WriteLine("test154: for (s.g*s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 1008)
            {
                Console.WriteLine("test155: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + (s.e * s.f)) - (s.g * s.h));
            if (v != 612)
            {
                Console.WriteLine("test156: for ((s.f+(s.e*s.f))-(s.g*s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * ((s.a + (s.b * s.c)) - (s.c * s.d)));
            if (v != -320963)
            {
                Console.WriteLine("test157: for (s.e*((s.a+(s.b*s.c))-(s.c*s.d)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h)));
            if (v != -321575)
            {
                Console.WriteLine("test158: for ((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + s.e);
            if (v != -321528)
            {
                Console.WriteLine("test159: for (((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e + ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))));
            if (v != -321528)
            {
                Console.WriteLine("test160: for (s.e+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e + ((s.a + (s.b * s.c)) - (s.c * s.d))) + ((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))));
            if (v != -328357)
            {
                Console.WriteLine("test161: for ((s.e+((s.a+(s.b*s.c))-(s.c*s.d)))+((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d))));
            if (v != -328357)
            {
                Console.WriteLine("test162: for (((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)));
            if (v != 616)
            {
                Console.WriteLine("test163: for (s.g+((s.f+(s.e*s.f))-(s.g*s.h)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.f + (s.e * s.f)) - (s.g * s.h)) + s.g);
            if (v != 616)
            {
                Console.WriteLine("test164: for (((s.f+(s.e*s.f))-(s.g*s.h))+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + (s.e * s.f)) - (s.g * s.h));
            if (v != 612)
            {
                Console.WriteLine("test165: for ((s.f+(s.e*s.f))-(s.g*s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 1008)
            {
                Console.WriteLine("test166: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * s.f) + s.f);
            if (v != 1008)
            {
                Console.WriteLine("test167: for ((s.e*s.f)+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.e * s.f);
            if (v != 987)
            {
                Console.WriteLine("test168: for (s.e*s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f * s.e);
            if (v != 987)
            {
                Console.WriteLine("test169: for (s.f*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f * s.e);
            if (v != 987)
            {
                Console.WriteLine("test170: for (s.f*s.e)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.a = return_int(false, 56);
            v = (s.e * s.f);
            if (v != 987)
            {
                Console.WriteLine("test171: for (s.e*s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.e * s.f) + s.f);
            if (v != 1008)
            {
                Console.WriteLine("test172: for ((s.e*s.f)+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.e * s.f));
            if (v != 1008)
            {
                Console.WriteLine("test173: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g * s.h);
            if (v != 396)
            {
                Console.WriteLine("test174: for (s.g*s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h * s.g);
            if (v != 396)
            {
                Console.WriteLine("test175: for (s.h*s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h * s.g);
            if (v != 396)
            {
                Console.WriteLine("test176: for (s.h*s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g * s.h);
            if (v != 396)
            {
                Console.WriteLine("test177: for (s.g*s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.a = return_int(false, 95);
            v = (s.f + (s.e * s.f));
            if (v != 1008)
            {
                Console.WriteLine("test178: for (s.f+(s.e*s.f))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + (s.e * s.f)) - (s.g * s.h));
            if (v != 612)
            {
                Console.WriteLine("test179: for ((s.f+(s.e*s.f))-(s.g*s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.f + (s.e * s.f)) - (s.g * s.h)) + s.g);
            if (v != 616)
            {
                Console.WriteLine("test180: for (((s.f+(s.e*s.f))-(s.g*s.h))+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)));
            if (v != 616)
            {
                Console.WriteLine("test181: for (s.g+((s.f+(s.e*s.f))-(s.g*s.h)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d))));
            if (v != -324325)
            {
                Console.WriteLine("test182: for (((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h))));
            if (v != -324941)
            {
                Console.WriteLine("test183: for ((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k)));
            if (v != -977208)
            {
                Console.WriteLine("test184: for ((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((s.a + s.b) + s.g) - ((s.c + s.b) * s.k)) * (s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))));
            if (v != -977208)
            {
                Console.WriteLine("test185: for ((((s.a+s.b)+s.g)-((s.c+s.b)*s.k))*(s.a+((s.h+(s.f+s.g))-(s.p*s.q))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.b = return_int(false, 19);
            s.d = return_int(false, -10);
            v = (s.a + ((s.h + (s.f + s.g)) - (s.p * s.q)));
            if (v != -4286)
            {
                Console.WriteLine("test186: for (s.a+((s.h+(s.f+s.g))-(s.p*s.q)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.h + (s.f + s.g)) - (s.p * s.q)) + s.a);
            if (v != -4286)
            {
                Console.WriteLine("test187: for (((s.h+(s.f+s.g))-(s.p*s.q))+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.h + (s.f + s.g)) - (s.p * s.q));
            if (v != -4381)
            {
                Console.WriteLine("test188: for ((s.h+(s.f+s.g))-(s.p*s.q))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h + (s.f + s.g));
            if (v != 124)
            {
                Console.WriteLine("test189: for (s.h+(s.f+s.g))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + s.g) + s.h);
            if (v != 124)
            {
                Console.WriteLine("test190: for ((s.f+s.g)+s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + s.g);
            if (v != 25)
            {
                Console.WriteLine("test191: for (s.f+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + s.f);
            if (v != 25)
            {
                Console.WriteLine("test192: for (s.g+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + s.f);
            if (v != 25)
            {
                Console.WriteLine("test193: for (s.g+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + s.g);
            if (v != 25)
            {
                Console.WriteLine("test194: for (s.f+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + (s.g + s.h));
            if (v != 124)
            {
                Console.WriteLine("test195: for (s.f+(s.g+s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + (s.f + s.h));
            if (v != 124)
            {
                Console.WriteLine("test196: for (s.g+(s.f+s.h))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + s.h);
            if (v != 103)
            {
                Console.WriteLine("test197: for (s.g+s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h + s.g);
            if (v != 103)
            {
                Console.WriteLine("test198: for (s.h+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h + s.g);
            if (v != 103)
            {
                Console.WriteLine("test199: for (s.h+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + s.h);
            if (v != 103)
            {
                Console.WriteLine("test200: for (s.g+s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + s.h);
            if (v != 120)
            {
                Console.WriteLine("test201: for (s.f+s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h + s.f);
            if (v != 120)
            {
                Console.WriteLine("test202: for (s.h+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h + s.f);
            if (v != 120)
            {
                Console.WriteLine("test203: for (s.h+s.f)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.f + s.h);
            if (v != 120)
            {
                Console.WriteLine("test204: for (s.f+s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.f + s.g) + s.h);
            if (v != 124)
            {
                Console.WriteLine("test205: for ((s.f+s.g)+s.h)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h + (s.f + s.g));
            if (v != 124)
            {
                Console.WriteLine("test206: for (s.h+(s.f+s.g))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.p * s.q);
            if (v != 4505)
            {
                Console.WriteLine("test207: for (s.p*s.q)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.q * s.p);
            if (v != 4505)
            {
                Console.WriteLine("test208: for (s.q*s.p)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.q * s.p);
            if (v != 4505)
            {
                Console.WriteLine("test209: for (s.q*s.p)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.p * s.q);
            if (v != 4505)
            {
                Console.WriteLine("test210: for (s.p*s.q)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.h + (s.f + s.g));
            if (v != 124)
            {
                Console.WriteLine("test211: for (s.h+(s.f+s.g))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.h + (s.f + s.g)) - (s.p * s.q));
            if (v != -4381)
            {
                Console.WriteLine("test212: for ((s.h+(s.f+s.g))-(s.p*s.q))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.h + (s.f + s.g)) - (s.p * s.q)) + s.a);
            if (v != -4286)
            {
                Console.WriteLine("test213: for (((s.h+(s.f+s.g))-(s.p*s.q))+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + ((s.h + (s.f + s.g)) - (s.p * s.q)));
            if (v != -4286)
            {
                Console.WriteLine("test214: for (s.a+((s.h+(s.f+s.g))-(s.p*s.q)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k));
            if (v != 460)
            {
                Console.WriteLine("test215: for (((s.a+s.b)+s.g)-((s.c+s.b)*s.k))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.c = return_int(false, -33);
            v = ((s.a + s.b) + s.g);
            if (v != 118)
            {
                Console.WriteLine("test216: for ((s.a+s.b)+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + (s.a + s.b));
            if (v != 118)
            {
                Console.WriteLine("test217: for (s.g+(s.a+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + s.b);
            if (v != 114)
            {
                Console.WriteLine("test218: for (s.a+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.a);
            if (v != 114)
            {
                Console.WriteLine("test219: for (s.b+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.a);
            if (v != 114)
            {
                Console.WriteLine("test220: for (s.b+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + s.b);
            if (v != 114)
            {
                Console.WriteLine("test221: for (s.a+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.a + (s.b + s.g));
            if (v != 118)
            {
                Console.WriteLine("test222: for (s.a+(s.b+s.g))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + (s.a + s.g));
            if (v != 118)
            {
                Console.WriteLine("test223: for (s.b+(s.a+s.g))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.g);
            if (v != 23)
            {
                Console.WriteLine("test224: for (s.b+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + s.b);
            if (v != 23)
            {
                Console.WriteLine("test225: for (s.g+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + s.b);
            if (v != 23)
            {
                Console.WriteLine("test226: for (s.g+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.g);
            if (v != 23)
            {
                Console.WriteLine("test227: for (s.b+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.p = return_int(false, 13);
            s.g = return_int(false, 69);
            v = (s.a + s.g);
            if (v != 164)
            {
                Console.WriteLine("test228: for (s.a+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + s.a);
            if (v != 164)
            {
                Console.WriteLine("test229: for (s.g+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + s.a);
            if (v != 164)
            {
                Console.WriteLine("test230: for (s.g+s.a)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.h = return_int(false, 130);
            v = (s.a + s.g);
            if (v != 164)
            {
                Console.WriteLine("test231: for (s.a+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + (s.a + s.b));
            if (v != 183)
            {
                Console.WriteLine("test232: for (s.g+(s.a+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + s.b) + s.g);
            if (v != 183)
            {
                Console.WriteLine("test233: for ((s.a+s.b)+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.c + s.b) * s.k);
            if (v != 42)
            {
                Console.WriteLine("test234: for ((s.c+s.b)*s.k)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.k * (s.c + s.b));
            if (v != 42)
            {
                Console.WriteLine("test235: for (s.k*(s.c+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.p = return_int(false, 72);
            s.h = return_int(false, -13);
            v = (s.c + s.b);
            if (v != -14)
            {
                Console.WriteLine("test236: for (s.c+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.b = return_int(false, 2);
            v = (s.b + s.c);
            if (v != -31)
            {
                Console.WriteLine("test237: for (s.b+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.c);
            if (v != -31)
            {
                Console.WriteLine("test238: for (s.b+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c + s.b);
            if (v != -31)
            {
                Console.WriteLine("test239: for (s.c+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.k * (s.c + s.b));
            if (v != 93)
            {
                Console.WriteLine("test240: for (s.k*(s.c+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.c + s.b) * s.k);
            if (v != 93)
            {
                Console.WriteLine("test241: for ((s.c+s.b)*s.k)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + s.b) + s.g);
            if (v != 166)
            {
                Console.WriteLine("test242: for ((s.a+s.b)+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k));
            if (v != 73)
            {
                Console.WriteLine("test243: for (((s.a+s.b)+s.g)-((s.c+s.b)*s.k))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((s.a + s.b) + s.g) - ((s.c + s.b) * s.k)) * (s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))));
            if (v != -266012)
            {
                Console.WriteLine("test244: for ((((s.a+s.b)+s.g)-((s.c+s.b)*s.k))*(s.a+((s.h+(s.f+s.g))-(s.p*s.q))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k)));
            if (v != -266012)
            {
                Console.WriteLine("test245: for ((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k)))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k))) + ((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)))));
            if (v != -284292)
            {
                Console.WriteLine("test246: for (((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k)))+((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h)))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)))) + ((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k))));
            if (v != -284292)
            {
                Console.WriteLine("test247: for (((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.b * s.b) + s.g) - ((s.c + s.b) * s.k));
            if (v != -20)
            {
                Console.WriteLine("test248: for (((s.b*s.b)+s.g)-((s.c+s.b)*s.k))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.b) + s.g);
            if (v != 73)
            {
                Console.WriteLine("test249: for ((s.b*s.b)+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + (s.b * s.b));
            if (v != 73)
            {
                Console.WriteLine("test250: for (s.g+(s.b*s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.b);
            if (v != 4)
            {
                Console.WriteLine("test251: for (s.b*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.b);
            if (v != 4)
            {
                Console.WriteLine("test252: for (s.b*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.b);
            if (v != 4)
            {
                Console.WriteLine("test253: for (s.b*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b * s.b);
            if (v != 4)
            {
                Console.WriteLine("test254: for (s.b*s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.g + (s.b * s.b));
            if (v != 73)
            {
                Console.WriteLine("test255: for (s.g+(s.b*s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.b) + s.g);
            if (v != 73)
            {
                Console.WriteLine("test256: for ((s.b*s.b)+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.c + s.b) * s.k);
            if (v != 93)
            {
                Console.WriteLine("test257: for ((s.c+s.b)*s.k)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.k = return_int(false, 125);
            v = (s.k * (s.c + s.b));
            if (v != -3875)
            {
                Console.WriteLine("test258: for (s.k*(s.c+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c + s.b);
            if (v != -31)
            {
                Console.WriteLine("test259: for (s.c+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.c);
            if (v != -31)
            {
                Console.WriteLine("test260: for (s.b+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.b + s.c);
            if (v != -31)
            {
                Console.WriteLine("test261: for (s.b+s.c)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.c + s.b);
            if (v != -31)
            {
                Console.WriteLine("test262: for (s.c+s.b)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (s.k * (s.c + s.b));
            if (v != -3875)
            {
                Console.WriteLine("test263: for (s.k*(s.c+s.b))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.c + s.b) * s.k);
            if (v != -3875)
            {
                Console.WriteLine("test264: for ((s.c+s.b)*s.k)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((s.b * s.b) + s.g);
            if (v != 73)
            {
                Console.WriteLine("test265: for ((s.b*s.b)+s.g)  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = (((s.b * s.b) + s.g) - ((s.c + s.b) * s.k));
            if (v != 3948)
            {
                Console.WriteLine("test266: for (((s.b*s.b)+s.g)-((s.c+s.b)*s.k))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            s.a = return_int(false, 105);
            v = (((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)))) + ((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k))));
            if (v != -14739134)
            {
                Console.WriteLine("test267: for (((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))  failed actual value {0} ", v);
                ret = ret + 1;
            }

            v = ((((((s.e * ((s.a + (s.b * s.c)) - (s.c * s.d))) - ((s.f + (s.e * s.f)) - (s.g * s.h))) + (s.e + ((s.a + (s.b * s.c)) - (s.c * s.d)))) - (s.g + ((s.f + (s.e * s.f)) - (s.g * s.h)))) + ((s.a + ((s.h + (s.f + s.g)) - (s.p * s.q))) * (((s.a + s.b) + s.g) - ((s.c + s.b) * s.k)))) - (((s.b * s.b) + s.g) - ((s.c + s.b) * s.k)));
            if (v != -14743082)
            {
                Console.WriteLine("test268: for ((((((s.e*((s.a+(s.b*s.c))-(s.c*s.d)))-((s.f+(s.e*s.f))-(s.g*s.h)))+(s.e+((s.a+(s.b*s.c))-(s.c*s.d))))-(s.g+((s.f+(s.e*s.f))-(s.g*s.h))))+((s.a+((s.h+(s.f+s.g))-(s.p*s.q)))*(((s.a+s.b)+s.g)-((s.c+s.b)*s.k))))-(((s.b*s.b)+s.g)-((s.c+s.b)*s.k)))  failed actual value {0} ", v);
                ret = ret + 1;
            }
#if LOOP
			} while (v == 0);
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
        public int e;
        public int a;
        public int b;
        public int c;
        public int d;
        public int f;
        public int g;
        public int h;
        public int p;
        public int q;
        public int k;
    }
}

