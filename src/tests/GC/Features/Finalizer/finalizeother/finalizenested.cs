// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests Nested Finalize()

using System;
using System.Threading;
using System.Runtime.CompilerServices;

public class Test_finalizenested {

    public class D
    {
        ~D()
        {
            Console.WriteLine("In Finalize() of D");
            Thread.Sleep(1000);
        }
    }

    public class C
    {
        public D d;

        public C()
        {
            d = new D();
        }

        ~C()
        {
            Console.WriteLine("In Finalize() of C");
            d=null;
            Thread.Sleep(1000);
        }
    }

    public class B
    {
        public C c;

        public B()
        {
            c = new C();
        }

        ~B()
        {
            Console.WriteLine("In Finalize() of B");
            c=null;
            Thread.Sleep(1000);
        }
    }

    public class A
    {
        public B b;

        public A()
        {
            b = new B();
        }

        ~A()
        {
            Console.WriteLine("In Finalize() of A");
            b=null;
            Thread.Sleep(1000);
        }
    }

    public class Dummy {

        public A a;
        public static bool visited;

        public Dummy()
        {
            a = new A();
        }

        ~Dummy()
        {
            Console.WriteLine("In Finalize() of Dummy");
            a=null;
            visited=true;
        }
    }

    public class CreateObj
    {
// disabling unused variable warning
#pragma warning disable 0414
        Dummy obj;
#pragma warning restore 0414

        public CreateObj()
        {
            obj=new Dummy();
        }

        public void RunTest()
        {
            obj=null;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void AllocAndDealloc() 
    {
        CreateObj temp = new CreateObj();
        temp.RunTest();
    }

    public static int Main() 
    {
        AllocAndDealloc();

        GC.Collect();
        GC.WaitForPendingFinalizers();  // makes sure Finalize() is called.
        GC.Collect();

        if (Dummy.visited)
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
        Console.WriteLine("Test Failed");
        return 1;

    }
}
