// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;

namespace structinreg
{
    public class Foo3
    {
        public int iFoo;
    }

    struct Test30
    {
        public int i1;
        public int i2;
    }

    struct Test31
    {
        public Foo3 foo1;
    }

    struct Test32
    {
        public int i1;
        public Foo3 foo1;
    }

    struct Test33
    {
        public Foo3 foo1;
        public Foo3 foo2;
    }

    struct Test34
    {
        public int i1;
        public int i2;
        public float f1;
    }

    struct Test35
    {  
        public Foo3 foo1;  
        public Foo3 foo2;  
    }

    class Program0
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        Test30 test1()
        {
            Console.WriteLine("From Program0:test1!");
            Test30 test1 = default(Test30);
            test1.i1 = 1;
            test1.i2 = 2;

            return test1;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        Test31 test2()
        {
            Console.WriteLine("From Program0:test2!");
            Test31 test2 = default(Test31);
            Foo3 foo = new Foo3();
            foo.iFoo = 3;
            test2.foo1 = foo;
            return test2;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        Test32 test3()
        {
            Console.WriteLine("From Program0:test3!");
            Test32 test3 = default(Test32);
            Foo3 foo = new Foo3();
            foo.iFoo = 4;
            test3.foo1 = foo;
            test3.i1 = 3;
            return test3;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        Test33 test4()
        {
            Console.WriteLine("From Program0:test4!");
            Test33 test4 = default(Test33);
            Foo3 foo1 = new Foo3();
            Foo3 foo2 = new Foo3();
            foo1.iFoo = 5;
            foo2.iFoo = 6;
            test4.foo1 = foo1;
            test4.foo2 = foo2;
            return test4;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        Test34 test5(Test34 t5)
        {
            float fRes = t5.i1 + t5.i2 + t5.f1;
            Console.WriteLine("From Test5 members: {0} {1} {2} {3}", t5.i1, t5.i2, t5.f1, fRes);
            Console.WriteLine("From Test5: Res {0}", fRes);
            if (fRes != 33.0)
            {
                throw new Exception("Failed inside test5 test!");
            }

            Test34 tst5 = default(Test34);
            tst5.i1 = 13;
            tst5.i2 = 14;
            tst5.f1 = 15;
            return tst5;
        }


        [MethodImplAttribute(MethodImplOptions.NoInlining)]  
        Test33 test7(Test33 t5)
        {    
            int fRes = t5.foo1.iFoo + t5.foo2.iFoo;    
            Console.WriteLine("From Test7 members: {0} {1}", t5.foo1.iFoo, t5.foo2.iFoo);    
            Console.WriteLine("From Test7: Res {0}", fRes);    
            if (fRes != 43) {    
                throw new Exception("Failed inside test6 test!");    
            }

            Test33 tst5 = default(Test33);

            unsafe
            {
                // Force the use of GS check.
                int* array = stackalloc int[2];
                array[0] = 28;
                array[1] = 29;

                tst5.foo1 = new Foo3();
                tst5.foo2 = new Foo3();
                tst5.foo1.iFoo = array[0];
                tst5.foo2.iFoo = array[1];
            }

            return tst5;    
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        Test35 test6(Test35 t5)
        {  
            int fRes = t5.foo1.iFoo + t5.foo2.iFoo;  
            Console.WriteLine("From Test6 members: {0} {1}", t5.foo1.iFoo, t5.foo2.iFoo);  
            Console.WriteLine("From Test6: Res {0}", fRes);  
            if (fRes != 43) {  
                throw new Exception("Failed inside test6 test!");  
            }  
  
            Test35 tst5 = default(Test35);  
            tst5.foo1 = new Foo3();  
            tst5.foo2 = new Foo3();  
            tst5.foo1.iFoo = 28;  
            tst5.foo2.iFoo = 29;  
            return tst5;  
        }  
 
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static int Main1()
        {
            Program0 p = new Program0();

            Test30 t1Res = p.test1();
            Console.WriteLine("test1 Result: {0}", t1Res.i1 + t1Res.i2);
            if ((t1Res.i1 + t1Res.i2) != 3)
            {
                throw new Exception("Failed test1 test!");
            }

            Test31 t2Res = p.test2();
            Console.WriteLine("test2 Result: {0}", t2Res.foo1.iFoo);
            if (t2Res.foo1.iFoo != 3)
            {
                throw new Exception("Failed test2 test!");
            }

            Test32 t3Res = p.test3();
            Console.WriteLine("test3 Result: {0}", t3Res.i1 + t3Res.foo1.iFoo);
            if ((t3Res.i1 + t3Res.foo1.iFoo) != 7) // Adding HashCodes should be != 0.
            {
                throw new Exception("Failed test3 test!");
            }

            Test33 t4Res = p.test4();
            Console.WriteLine("test4 Result: {0}", t4Res.foo1.iFoo + t4Res.foo2.iFoo);
            if ((t4Res.foo1.iFoo + t4Res.foo2.iFoo) != 11)
            {
                throw new Exception("Failed test4 test!");
            }

            Test34 test5 = default(Test34);
            test5.i1 = 10;
            test5.i2 = 11;
            test5.f1 = 12;

            Test34 t5Res = p.test5(test5);

            Console.WriteLine("test5 Result: {0}", t5Res.i1 + t5Res.i2 + t5Res.f1);
            if ((t5Res.i1 + t5Res.i2 + t5Res.f1) != 42.0)
            {
                throw new Exception("Failed test5 test!");
            }

            Test35 test6 = default(Test35);
            test6.foo1 = new Foo3();
            test6.foo2 = new Foo3();
            test6.foo1.iFoo = 21;
            test6.foo2.iFoo = 22;

            Test35 t6Res = p.test6(test6);

            Console.WriteLine("test6 Result: {0}", t6Res.foo1.iFoo + t6Res.foo2.iFoo);
            if ((t6Res.foo1.iFoo + t6Res.foo2.iFoo) != 57) {
                throw new Exception("Failed test6 test!");
            }

            Test33 test7 = default(Test33);
            test7.foo1 = new Foo3();
            test7.foo2 = new Foo3();
            test7.foo1.iFoo = 21;
            test7.foo2.iFoo = 22;

            Test33 t7Res = p.test7(test7);

            Console.WriteLine("test7Result: {0}", t7Res.foo1.iFoo + t7Res.foo2.iFoo);
            if ((t7Res.foo1.iFoo + t7Res.foo2.iFoo) != 57) {
                throw new Exception("Failed test7 test!");
            }

            return 100;
        }
    }
}
