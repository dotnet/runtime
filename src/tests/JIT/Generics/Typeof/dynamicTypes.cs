// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

/* The following are the checks being made, where x is a user defined type as described below. The test does all of this pretty much as a grid cross section, so there are redundant cast cases also covered here.

1.	if(y.GetType() == typeof(x)) do something;
2.	if(y is x) do something;
3.	x z = y as x;
                if(z != null) do something;
4.	if(y.GetType() == typeof(x)) static cast y as x;
5.	if(y is x) static cast y as x;
6.	x z = y as x;
                if(z != null) static cast y as x;

where x can take the following values (whenever applicable):

                x is X< X<double> >
                x is X< X<string> >
                x is X< X<int> >
                x is X<double>
                x is X<string>
                x is X<int>
                x is A
                x is B
                x is C
                x is D
                x is CS
                x is DS

where the above classes have the following relationships:

class X<T>
class A : X<int>
class B : X<string>
class C : A
class D : B
sealed class CS : A
sealed class DS : B
 */

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

class X<T>
{
        public static int x_count = 1;
        public virtual void incCount()
        {
/*                switch(typeof(T))
                {
                case int: 
                        x_count*= 2;
                        break;
                case string: 
                        x_count*= 3;
                        break;
                case double:
                        x_count*= 5;
                        break;
                case void:
                        x_count*= 7;
                        break;
                case X<T>:
                        x_count*= 11;
                        break;
			    case X<X<T>>:
                        x_count*= 13;
                        break;
                }
*/
                if(typeof(T) == typeof(int))
                        x_count*= 2;
                else if(typeof(T) == typeof(string))
                        x_count*= 3;
                else if(typeof(T) == typeof(double))
                        x_count*= 5;
                else if(typeof(T) == typeof(void))
                        x_count*= 7;
                else if(typeof(T) == typeof(X<int>) || typeof(T) == typeof(X<double>))
                        x_count*= 11;
                else if(typeof(T) == typeof(X<string>) || typeof(T) == null)
                        x_count*= 13;
                else
                        Console.WriteLine("ERROR: Unknown type {0}", typeof(T));
        }
}

class A : X<int>
{
        public static int a_count = 1;
        public override void incCount() { a_count*= 17;}
}

class B : X<string>
{
        public static int b_count = 1;
        public override void incCount() { b_count*= 19;}
}

class C : A
{
        public static int c_count = 1;
        public override void incCount() { c_count*= 23;}
}

class D : B
{
        public static int d_count = 1;
        public override void incCount() { d_count*= 31;}
}

sealed class CS : A
{
        public static int cs_count = 1;
        public override void incCount() { cs_count*= 37;}
}

sealed class DS : B
{
        public static int ds_count = 1;
        public override void incCount() { ds_count*= 41;}
}

public class mainMethod
{
        public static bool failed = false;
        internal static void checkGetType<T>(X<T> x)
        {
                if(x.GetType() == typeof(DS)) (new DS()).incCount();
                if(x.GetType() == typeof(CS)) (new CS()).incCount();
                if(x.GetType() == typeof(D)) (new D()).incCount();
                if(x.GetType() == typeof(C)) (new C()).incCount();
                if(x.GetType() == typeof(B)) (new B()).incCount();
                if(x.GetType() == typeof(A)) (new A()).incCount();
                if(x.GetType() == typeof(X<int>)) (new X<int>()).incCount();
                if(x.GetType() == typeof(X<string>)) (new X<string>()).incCount();
                if(x.GetType() == typeof(X<double>)) (new X<double>()).incCount();
                if(x.GetType() == typeof(X< X<int> >)) (new X< X<int> >()).incCount();
                if(x.GetType() == typeof(X< X<string> >)) (new X< X<string> >()).incCount();
                if(x.GetType() == typeof(X< X<double> >)) (new X< X<double> >()).incCount();
                if(x.GetType() == null) (new X<string>()).incCount();
        }

        internal static void checkIs<T>(X<T> x)
        {
                //start from X<T>
                //if(x is null ) (new X< X<string> >()).incCount();
                if(x is X< X<double> >) (new X< X<double> >()).incCount();
                if(x is X< X<string> >) (new X< X<string> >()).incCount();
                if(x is X< X<int> >) (new X< X<int> >()).incCount();
                if(x is X<double>) (new X<double>()).incCount();
                if(x is X<string>) (new X<string>()).incCount();
                if(x is X<int>) (new X<int>()).incCount();
                if(x is A) (new A()).incCount();
                if(x is B) (new B()).incCount();
                if(x is C) (new C()).incCount();
                if(x is D) (new D()).incCount();
                if(x is CS) (new CS()).incCount();
                if(x is DS) (new DS()).incCount();
        }
        internal static void checkAs<T>(X<T> x)
        {
                X< X<double> > x6 = x as X< X<double> >;
                if(x6 != null) (new X< X<double> >()).incCount();
                X< X<string> > x5 = x as X< X<string> >;
                if(x5 != null) (new X< X<string> >()).incCount();
                X< X<int> > x4 = x as X< X<int> >;
                if(x4 != null) (new X< X<int> >()).incCount();
                X<double> x3 = x as X<double>;
                if(x3 != null) (new X<double>()).incCount();
                X<string> x2 = x as X<string>;
                if(x2 != null) (new X<string>()).incCount();
                X<int> x1 = x as X<int>;
                if(x1 != null) (new X<int>()).incCount();
                A a = x as A;
                if(a != null) (new A()).incCount();
                B b = x as B;
                if(b != null) (new B()).incCount();
                C c = x as C;
                if(c != null) (new C()).incCount();
                D d = x as D;
                if(d != null) (new D()).incCount();
                CS cs = x as CS;
                if(cs != null) (new CS()).incCount();
                DS ds = x as DS;
                if(ds != null) (new DS()).incCount();

                /*
                //start from X<T>
                B b = x as B;
                if(b != null) (new B()).incCount();
                checkCount(ref B.b_count, 31, "AS Failed for D");
                Console.WriteLine(D.b_count);
                */
        }
        internal static void checkGetTypeStringCast(X<string> x)
        {
                if(x.GetType() == typeof(DS)) ((DS)x).incCount();
                if(x.GetType() == typeof(D)) ((D)x).incCount();
                if(x.GetType() == typeof(B)) ((B)x).incCount();
                //if(x.GetType() == typeof(X<int>)) ((X<int>)x).incCount();
                if(x.GetType() == typeof(X<string>)) ((X<string>)x).incCount();
                //if(x.GetType() == typeof(X<double>)) ((X<double>)x).incCount();
                //if(x.GetType() == typeof(X< X<int> >)) ((X< X<int> >)x).incCount();
                //if(x.GetType() == typeof(X< X<string> >)) ((X< X<string> >)x).incCount();
                //if(x.GetType() == typeof(X< X<double> >)) ((X< X<double> >)x).incCount();
                if(x.GetType() == null) ((X<string>)x).incCount();
                //if(x.GetType() == typeof(DS)) ((DS)x).incCount();
        }        
        internal static void checkGetTypeIntCast(X<int> x)
        {
                if(x.GetType() == typeof(CS)) ((CS)x).incCount();
                if(x.GetType() == typeof(C)) ((C)x).incCount();
                if(x.GetType() == typeof(A)) ((A)x).incCount();
                if(x.GetType() == typeof(X<int>)) ((X<int>)x).incCount();
                //if(x.GetType() == typeof(X<string>)) ((X<string>)x).incCount();
                //if(x.GetType() == typeof(X<double>)) ((X<double>)x).incCount();
                //if(x.GetType() == typeof(X< X<int> >)) ((X< X<int> >)x).incCount();
                //if(x.GetType() == typeof(X< X<string> >)) ((X< X<string> >)x).incCount();
                //if(x.GetType() == typeof(X< X<double> >)) ((X< X<double> >)x).incCount();
                if(x.GetType() == null) ((X<int>)x).incCount();

                //if(x.GetType() == typeof(CS)) ((CS)x).incCount();
        }        
        internal static void checkIsStringCast(X<string> x)
        {
                //if(x is null ) ((X< X<string> >)x).incCount();
                //if(x is X< X<double> >) ((X< X<double> >)x).incCount();
                //if(x is X< X<string> >) ((X< X<string> >)x).incCount();
                //if(x is X< X<int> >) ((X< X<int> >)x).incCount();
                //if(x is X<double>) ((X<double>)x).incCount();
                if(x is X<string>) ((X<string>)x).incCount();
                //if(x is X<int>) ((X<int>)x).incCount();
                //if(x is A) ((A)x).incCount();
                if(x is B) ((B)x).incCount();
                //if(x is C) ((C)x).incCount();
                if(x is D) ((D)x).incCount();
                //if(x is CS) ((CS)x).incCount();
                if(x is DS) ((DS)x).incCount();

                //if(x is DS) ((DS)x).incCount();
        }
        internal static void checkIsIntCast(X<int> x)
        {
                //if(x is null ) ((X< X<string> >)x).incCount();
                //if(x is X< X<double> >) ((X< X<double> >)x).incCount();
                //if(x is X< X<string> >) ((X< X<string> >)x).incCount();
                //if(x is X< X<int> >) ((X< X<int> >)x).incCount();
                //if(x is X<double>) ((X<double>)x).incCount();
                //if(x is X<string>) ((X<string>)x).incCount();
                if(x is X<int>) ((X<int>)x).incCount();
                if(x is A) ((A)x).incCount();
                //if(x is B) ((B)x).incCount();
                if(x is C) ((C)x).incCount();
                //if(x is D) ((D)x).incCount();
                if(x is CS) ((CS)x).incCount();
                //if(x is DS) ((DS)x).incCount();

                //if(x is CS) ((CS)x).incCount();
        }
        internal static void checkAsStringCast(X<string> x)
        {
                //X< X<double> > x6 = x as X< X<double> >;
                //if(x6 != null) ((X< X<double> >)x).incCount();
                //X< X<string> > x5 = x as X< X<string> >;
                //if(x5 != null) ((X< X<string> >)x).incCount();
                //X< X<int> > x4 = x as X< X<int> >;
                //if(x4 != null) ((X< X<int> >)x).incCount();
                //X<double> x3 = x as X<double>;
                //if(x3 != null) ((X<double>)x).incCount();
                X<string> x2 = x as X<string>;
                if(x2 != null) ((X<string>)x).incCount();
                //X<int> x1 = x as X<int>;
                //if(x1 != null) ((X<int>)x).incCount();
                //A a = x as A;
                //if(a != null) ((A)x).incCount();
                B b = x as B;
                if(b != null) ((B)x).incCount();
                //C c = x as C;
                //if(c != null) ((C)x).incCount();
                D d = x as D;
                if(d != null) ((D)x).incCount();
                //CS cs = x as CS;
                //if(cs != null) ((CS)x).incCount();
                DS ds = x as DS;
                if(ds != null) ((DS)x).incCount();

                /* //start from X<T>
                B b = x as B;
                if(b != null) ((B)x).incCount();
                checkCount(ref B.b_count, 31, "AS Failed for D");
                */
        }
        internal static void checkAsIntCast(X<int> x)
        {
                //X< X<double> > x6 = x as X< X<double> >;
                //if(x6 != null) ((X< X<double> >)x).incCount();
                //X< X<string> > x5 = x as X< X<string> >;
                //if(x5 != null) ((X< X<string> >)x).incCount();
                //X< X<int> > x4 = x as X< X<int> >;
                //if(x4 != null) ((X< X<int> >)x).incCount();
                //X<double> x3 = x as X<double>;
                //if(x3 != null) ((X<double>)x).incCount();
                //X<string> x2 = x as X<string>;
                //if(x2 != null) ((X<string>)x).incCount();
                X<int> x1 = x as X<int>;
                if(x1 != null) ((X<int>)x).incCount();
                A a = x as A;
                if(a != null) ((A)x).incCount();
                //B b = x as B;
                //if(b != null) ((B)x).incCount();
                C c = x as C;
                if(c != null) ((C)x).incCount();
                //D d = x as D;
                //if(d != null) ((D)x).incCount();
                CS cs = x as CS;
                if(cs != null) ((CS)x).incCount();
                //DS ds = x as DS;
                //if(ds != null) ((DS)x).incCount();

                /*
                //start from X<T>
                A a = x as A;
                if(a != null) ((A)x).incCount();
                checkCount(ref A.b_count, 31, "AS Failed for D");
                */
        }

        public static void checkCount(ref int actual, int expected, string message)
        {
                if(actual != expected)
                {
                        Console.WriteLine("FAILED: {0}", message);
                        failed = true;
                }
                actual = 1;
        }

        public static void checkAllCounts(ref int x_actual, int ds, int cs, int d, int c, int b, int a, int x, string dsm, string csm, string dm, string cm, string bm, string am, string xm)
        {
                /*
                printCount(ref DS.ds_count, ds, dsm);
                printCount(ref CS.cs_count, cs, csm);
                printCount(ref D.d_count, d, dm);
                printCount(ref C.c_count, c, cm);
                printCount(ref B.b_count, b, bm);
                printCount(ref A.a_count, a, am);
                printCount(ref x_actual, x, xm);
                */
                checkCount(ref DS.ds_count, ds, dsm);
                checkCount(ref CS.cs_count, cs, csm);
                checkCount(ref D.d_count, d, dm);
                checkCount(ref C.c_count, c, cm);
                checkCount(ref B.b_count, b, bm);
                checkCount(ref A.a_count, a, am);
                checkCount(ref x_actual, x, xm);
        }

        public static void printCount(ref int actual, int expected, string message)
        {
               //Console.WriteLine("Result: {0} {1}", message, actual);
               Console.Write("{0}, ", actual);
               actual = 1;
        }

        public static void callCheckGetType()
        {
                int i = 0;
                checkGetType(new X< X<double> >());
                checkAllCounts(ref X< X<double> >.x_count, 1, 1, 1, 1, 1, 1, 11, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new X< X<string> >());
                checkAllCounts(ref X< X<string> >.x_count, 1, 1, 1, 1, 1, 1, 13, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new X< X<int> >());
                checkAllCounts(ref X< X<int> >.x_count, 1, 1, 1, 1, 1, 1, 11, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new X<double>());
                checkAllCounts(ref X<double>.x_count, 1, 1, 1, 1, 1, 1, 5, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new X<string>());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 1, 1, 3, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new X<int>());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new A());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 17, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new B());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 19, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new C());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 23, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new D());
                checkAllCounts(ref X<string>.x_count, 1, 1, 31, 1, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new CS());
                checkAllCounts(ref X<int>.x_count, 1, 37, 1, 1, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
                checkGetType(new DS());
                checkAllCounts(ref X<string>.x_count, 41, 1, 1, 1, 1, 1, 1, "DS Count after GetType and typeof", "CS Count after GetType and typeof", "D Count after GetType and typeof", "C Count after GetType and typeof", "B Count after GetType and typeof", "A Count after GetType and typeof", "X Count after GetType and typeof");
                Console.WriteLine("-----------{0}", i++);
        }

        public static void callCheckIs()
        {
                int i = 0;
                checkIs(new X< X<double> >());
                checkAllCounts(ref X< X<double> >.x_count, 1, 1, 1, 1, 1, 1, 11, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new X< X<string> >());
                checkAllCounts(ref X< X<string> >.x_count, 1, 1, 1, 1, 1, 1, 13, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new X< X<int> >());
                checkAllCounts(ref X< X<int> >.x_count, 1, 1, 1, 1, 1, 1, 11, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new X<double>());
                checkAllCounts(ref X<double>.x_count, 1, 1, 1, 1, 1, 1, 5, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new X<string>());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 1, 1, 3, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new X<int>());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new A());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 17, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new B());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 19, 1, 3, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new C());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 23, 1, 17, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new D());
                checkAllCounts(ref X<string>.x_count, 1, 1, 31, 1, 19, 1, 3, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new CS());
                checkAllCounts(ref X<int>.x_count, 1, 37, 1, 1, 1, 17, 2, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
                checkIs(new DS());
                checkAllCounts(ref X<string>.x_count, 41, 1, 1, 1, 19, 1, 3, "DS Count after checking is", "CS Count after checking is", "D Count after checking is", "C Count after checking is", "B Count after checking is", "A Count after checking is", "X Count after checking is");
                Console.WriteLine("-----------{0}", i++);
        }

        public static void callCheckAs()
        {
                int i = 0;
                checkAs(new X< X<double> >());
                checkAllCounts(ref X< X<double> >.x_count, 1, 1, 1, 1, 1, 1, 11, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new X< X<string> >());
                checkAllCounts(ref X< X<string> >.x_count, 1, 1, 1, 1, 1, 1, 13, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new X< X<int> >());
                checkAllCounts(ref X< X<int> >.x_count, 1, 1, 1, 1, 1, 1, 11, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new X<double>());
                checkAllCounts(ref X<double>.x_count, 1, 1, 1, 1, 1, 1, 5, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new X<string>());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 1, 1, 3, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new X<int>());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new A());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 17, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new B());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 19, 1, 3, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new C());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 23, 1, 17, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new D());
                checkAllCounts(ref X<string>.x_count, 1, 1, 31, 1, 19, 1, 3, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new CS());
                checkAllCounts(ref X<int>.x_count, 1, 37, 1, 1, 1, 17, 2, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
                checkAs(new DS());
                checkAllCounts(ref X<string>.x_count, 41, 1, 1, 1, 19, 1, 3, "DS Count after checking as", "CS Count after checking as", "D Count after checking as", "C Count after checking as", "B Count after checking as", "A Count after checking as", "X Count after checking as");
                Console.WriteLine("-----------{0}", i++);
        }

        public static void callCheckGetTypeStringCast()
        {
                int i = 0;
                //checkGetTypeStringCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeStringCast(new X< X<double> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeStringCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeStringCast(new X< X<int> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeStringCast(new X<double>());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                checkGetTypeStringCast(new X<string>());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 1, 1, 3, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                Console.WriteLine("-----------{0}", i++);
                //checkGetTypeStringCast(new X<int>());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeStringCast(new A());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                checkGetTypeStringCast(new B());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 19, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                Console.WriteLine("-----------{0}", i++);
                //checkGetTypeStringCast(new C());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                checkGetTypeStringCast(new D());
                checkAllCounts(ref X<string>.x_count, 1, 1, 31, 1, 1, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                Console.WriteLine("-----------{0}", i++);
                //checkGetTypeStringCast(new CS());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                //Console.WriteLine("-----------{0}", i++);
                checkGetTypeStringCast(new DS());
                checkAllCounts(ref X<string>.x_count, 41, 1, 1, 1, 1, 1, 1, "DS Count after GetType and typeof string cast", "CS Count after GetType and typeof string cast", "D Count after GetType and typeof string cast", "C Count after GetType and typeof string cast", "B Count after GetType and typeof string cast", "A Count after GetType and typeof string cast", "X Count after GetType and typeof string cast");
                Console.WriteLine("-----------{0}", i++);
        }

        public static void callCheckGetTypeIntCast()
        {
                int i = 0;
                //checkGetTypeIntCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeIntCast(new X< X<double> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeIntCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeIntCast(new X< X<int> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeIntCast(new X<double>());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkGetTypeIntCast(new X<string>());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkGetTypeIntCast(new X<int>());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                Console.WriteLine("-----------{0}", i++);
                checkGetTypeIntCast(new A());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 17, 1, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkGetTypeIntCast(new B());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkGetTypeIntCast(new C());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 23, 1, 1, 1, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkGetTypeIntCast(new D());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkGetTypeIntCast(new CS());
                checkAllCounts(ref X<int>.x_count, 1, 37, 1, 1, 1, 1, 1, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkGetTypeIntCast(new DS());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after GetType and typeof int cast", "CS Count after GetType and typeof int cast", "D Count after GetType and typeof int cast", "C Count after GetType and typeof int cast", "B Count after GetType and typeof int cast", "A Count after GetType and typeof int cast", "X Count after GetType and typeof int cast");
                //Console.WriteLine("-----------{0}", i++);
        }

        public static void callCheckIsStringCast()
        {
                int i = 0;
                //checkIsStringCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsStringCast(new X< X<double> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsStringCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsStringCast(new X< X<int> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsStringCast(new X<double>());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                checkIsStringCast(new X<string>());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 1, 1, 3, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                Console.WriteLine("-----------{0}", i++);
                //checkIsStringCast(new X<int>());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsStringCast(new A());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                checkIsStringCast(new B());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 361, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                Console.WriteLine("-----------{0}", i++);
                //checkIsStringCast(new C());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                checkIsStringCast(new D());
                checkAllCounts(ref X<string>.x_count, 1, 1, 29791, 1, 1, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                Console.WriteLine("-----------{0}", i++);
                //checkIsStringCast(new CS());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                //Console.WriteLine("-----------{0}", i++);
                checkIsStringCast(new DS());
                checkAllCounts(ref X<string>.x_count, 68921, 1, 1, 1, 1, 1, 1, "DS Count after check is string cast ", "CS Count after check is string cast ", "D Count after check is string cast ", "C Count after check is string cast ", "B Count after check is string cast ", "A Count after check is string cast ", "X Count after check is string cast ");
                Console.WriteLine("-----------{0}", i++);
        }

        public static void callCheckIsIntCast()
        {
                int i = 0;
                //checkIsIntCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsIntCast(new X< X<double> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsIntCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsIntCast(new X< X<int> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsIntCast(new X<double>());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkIsIntCast(new X<string>());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkIsIntCast(new X<int>());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                Console.WriteLine("-----------{0}", i++);
                checkIsIntCast(new A());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 289, 1, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkIsIntCast(new B());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkIsIntCast(new C());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 12167, 1, 1, 1, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkIsIntCast(new D());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkIsIntCast(new CS());
                checkAllCounts(ref X<int>.x_count, 1, 50653, 1, 1, 1, 1, 1, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkIsIntCast(new DS());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check is int cast", "CS Count after check is int cast", "D Count after check is int cast", "C Count after check is int cast", "B Count after check is int cast", "A Count after check is int cast", "X Count after check is int cast");
                //Console.WriteLine("-----------{0}", i++);
        }

        public static void callCheckAsStringCast()
        {
                int i = 0;
                //checkAsStringCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsStringCast(new X< X<double> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsStringCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsStringCast(new X< X<int> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsStringCast(new X<double>());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                checkAsStringCast(new X<string>());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 1, 1, 3, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                Console.WriteLine("-----------{0}", i++);
                //checkAsStringCast(new X<int>());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsStringCast(new A());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                checkAsStringCast(new B());
                checkAllCounts(ref X<string>.x_count, 1, 1, 1, 1, 361, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                Console.WriteLine("-----------{0}", i++);
                //checkAsStringCast(new C());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                checkAsStringCast(new D());
                checkAllCounts(ref X<string>.x_count, 1, 1, 29791, 1, 1, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                Console.WriteLine("-----------{0}", i++);
                //checkAsStringCast(new CS());
                //checkAllCounts(ref X<int>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                //Console.WriteLine("-----------{0}", i++);
                checkAsStringCast(new DS());
                checkAllCounts(ref X<string>.x_count, 68921, 1, 1, 1, 1, 1, 1, "DS Count after check as string cast ", "CS Count after check as string cast ", "D Count after check as string cast ", "C Count after check as string cast ", "B Count after check as string cast ", "A Count after check as string cast ", "X Count after check as string cast ");
                Console.WriteLine("-----------{0}", i++);
        }

        public static void callCheckAsIntCast()
        {
                int i = 0;
                //checkAsIntCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsIntCast(new X< X<double> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsIntCast(new X< X<string> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsIntCast(new X< X<int> >());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsIntCast(new X<double>());
                //checkAllCounts(0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
                //checkAsIntCast(new X<string>());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkAsIntCast(new X<int>());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 1, 2, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                Console.WriteLine("-----------{0}", i++);
                checkAsIntCast(new A());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 1, 1, 289, 1, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkAsIntCast(new B());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkAsIntCast(new C());
                checkAllCounts(ref X<int>.x_count, 1, 1, 1, 12167, 1, 1, 1, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkAsIntCast(new D());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
                checkAsIntCast(new CS());
                checkAllCounts(ref X<int>.x_count, 1, 50653, 1, 1, 1, 1, 1, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                Console.WriteLine("-----------{0}", i++);
                //checkAsIntCast(new DS());
                //checkAllCounts(ref X<string>.x_count, 0, 0, 0, 0, 0, 0, 0, "DS Count after check as int cast", "CS Count after check as int cast", "D Count after check as int cast", "C Count after check as int cast", "B Count after check as int cast", "A Count after check as int cast", "X Count after check as int cast");
                //Console.WriteLine("-----------{0}", i++);
        }

        [Fact]
        public static int TestEntryPoint()
        {
                callCheckGetType();
                callCheckIs();
                callCheckAs();
                callCheckGetTypeStringCast();
                callCheckGetTypeIntCast();
                callCheckIsStringCast();
                callCheckIsIntCast();
                callCheckAsStringCast();
                callCheckAsIntCast();
                if(failed) return 101; else return 100;
                /*
                CallX<int> x = new A();
                X< X<double> > y = new X< X<double> >();
                x.incCount();
                y.incCount();
                Console.WriteLine(X< X<double> >.x_count);
                //checkGetType(new D());
                //checkIs(new D());
                checkAs(new D());
                */
        }
}
