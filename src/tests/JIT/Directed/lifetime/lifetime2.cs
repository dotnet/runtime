// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// Testing JIT handling and GC reporting of liveness of GC variable

using System;
using System.Collections.Generic;
using Xunit;

public class Test_lifetime2
{
    public static int aExists;
    public static int bExists;
    private abstract class A
    {
    }
    private class B : A
    {
        public B()
        {
            aExists++;
        }
        ~B()
        {
            aExists--;
            Console.WriteLine("~B");
        }

        public void F()
        {
            Console.WriteLine("B.F");
        }
    }
    private class C : B
    {
        public C()
        {
            bExists++;
        }
        ~C()
        {
            bExists--;
            Console.WriteLine("~C");
        }

        public void G()
        {
            Console.WriteLine("C.G");
        }
    }
    private static int f1()
    {
        B a = new B();
        a.F();

        Console.WriteLine();
        Console.WriteLine("testcase f1-1");
        if (aExists != 1)
        {
            Console.WriteLine("f1-1 failed");
            return -1;
        }

        GC.KeepAlive(a);
        a = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Console.WriteLine();
        Console.WriteLine("testcase f1-2");
        if (aExists != 0)
        {
            Console.WriteLine("f1-2 failed");
            return -1;
        }

        C b = new C();
        b.G();

        Console.WriteLine();
        Console.WriteLine("testcase f1-3");
        if ((aExists != 1) || (bExists != 1))
        {
            Console.WriteLine("f1-3 failed");
            return -1;
        }

        GC.KeepAlive(b);
        b = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        Console.WriteLine();
        Console.WriteLine("testcase f1-4");
        if ((aExists != 0) || (bExists != 0))
        {
            Console.WriteLine("f1-4 failed");
            return -1;
        }
        return 100;
    }
    private static int f2()
    {
        B a = new B();
        {
            C b = new C();
            b.G();
            b = null;
        }
        GC.Collect();
        GC.WaitForPendingFinalizers();

        a.F();
        Console.WriteLine();
        Console.WriteLine("testcase f2-1");
        if ((aExists != 1) || (bExists != 0))
        {
            Console.WriteLine("f2-1 failed");
            return -1;
        }

        GC.KeepAlive(a);
        a = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine();
        Console.WriteLine("testcase f2-2");
        if (aExists != 0)
        {
            Console.WriteLine("f2-2 failed");
            return -1;
        }
        return 100;
    }
    private static int f3()
    {
        C b = new C();
        b = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine();
        Console.WriteLine("testcase f3");
        if (aExists != 0)
        {
            Console.WriteLine("f3 failed");
            return -1;
        }
        b = null;
        return 100;
    }
    private static int f4()
    {
        B a = new B();
        a.F();
        C b = new C();
        b.G();

        Console.WriteLine();
        Console.WriteLine("testcase f4");
        if ((aExists != 2) || (bExists != 1))
        {
            Console.WriteLine("f4 failed");
            return -1;
        }

        GC.KeepAlive(a);
        GC.KeepAlive(b);
        return 100;
    }

    private static int f5()
    {
        Console.WriteLine();
        Console.WriteLine("testcase f5");
        if ((aExists != 0) || (bExists != 0))
        {
            Console.WriteLine("f5 failed");
            return -1;
        }
        return 100;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (f1() != 100)
            return -1;
        CleanGC();
        if (f2() != 100)
            return -1;
        CleanGC();
        if (f3() != 100)
            return -1;
        CleanGC();
        if (f4() != 100)
            return -1;
        CleanGC();
        if (f5() != 100)
            return -1;
        CleanGC();

        Console.WriteLine("PASSED");
        return 100;
    }

    private static void CleanGC()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
