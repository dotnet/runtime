// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// testing the JIT handling and GC reporting of "liveness" of GC variable

using System;

internal class Test
{
    private class A
    {
        public A()
        {
            Console.WriteLine("A");
            _iMember = 123;
            Test.aExists = true;
        }
        ~A()
        {
            Console.WriteLine("~A");
            Test.aExists = false;
        }
        public bool F()
        {
            Console.WriteLine("A.F(): iMember = {0}", _iMember);
            return true;
        }
        private volatile int _iMember;
    }

    public static volatile bool aExists = false;

    public static int f1()
    {
        A a = new A();
        a.F();

        // Testcase 1
        Console.WriteLine();
        Console.WriteLine("Testcase 1");
        if (!Test.aExists)
        {
            Console.WriteLine("Testcase 1 FAILED");
            return -1;
        }
        a.F();
        a = null;
        return 100;
    }

    public static int f2()
    {
        A a = new A();
        a.F();

        // Testcase 3
        Console.WriteLine();
        Console.WriteLine("Testcase 3");
        if (!Test.aExists)
        {
            Console.WriteLine("Testcase 3 FAILED");
            return -1;
        }
        GC.KeepAlive(a);
        return 100;
    }


    public static int f3()
    {
        A a = new A();
        a.F();
        a = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();

        A b = new A();
        a = b;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Console.WriteLine();
        Console.WriteLine("Testcase 5");
        if (!Test.aExists)
        {
            Console.WriteLine("Testcase 5 FAILED");
            return -1;
        }
        GC.KeepAlive(b);
        // Testcase 6
        Console.WriteLine();
        Console.WriteLine("Testcase 6");
        if (b == null)
        {
            Console.WriteLine("Testcase 6 FAILED");
            return -1;
        }

        b = null;

        return 100;
    }


    private static int Main()
    {
        if (f1() != 100) return -1;
        CleanGC();

        // Testcase 2
        Console.WriteLine();
        Console.WriteLine("Testcase 2");
        // here JIT should know a is not live anymore
        if (Test.aExists)
        {
            Console.WriteLine("Testcase 2 FAILED");
            return -1;
        }

        if (f2() != 100) return -1;
        CleanGC();

        // here JIT should know object a is not live anymore        
        // Testcase 4
        Console.WriteLine();
        Console.WriteLine("Testcase 4");
        if (Test.aExists)
        {
            Console.WriteLine("Testcase 4 FAILED");
            return -1;
        }

        if (f3() != 100) return -1;
        CleanGC();

        // here JIT should know object a is not live anymore        
        // Testcase 7
        Console.WriteLine();
        Console.WriteLine("Testcase 7");
        if (Test.aExists)
        {
            Console.WriteLine("Testcase 7 FAILED");
            return -1;
        }

        CleanGC();



        Console.WriteLine("Test SUCCESS");
        return 100;
    }

    private static void CleanGC()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
