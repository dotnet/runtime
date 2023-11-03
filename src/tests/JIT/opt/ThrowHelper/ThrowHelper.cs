// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

class TestException : Exception
{
    int x;
    string y;

    public TestException() {}
    public TestException(int _x) { x = _x; }
    public TestException(string _y) { y = _y; }
}

public class TestCases
{
    static void Throw() => throw new TestException();
    static void Throw(int x) => throw new TestException(x);
    static void Throw(string y) => throw new TestException(y);

    static int MayThrow(int x) 
    {
        if (x > 0) 
        {
            throw new TestException(x);
        }
        return x;
    }

    static int ReturnThrow() => throw new TestException();

    public static int OneThrowHelper(int x)
    {
        if (x > 0) 
        {
            Throw();
        }

        return x;
    }

    internal static void OneThrowHelperTail(int x)
    {
        if (x > 0) 
        {
            Throw();
            return;
        }
    }

    public static int OneMayThrowHelper(int x)
    {
        if (x > 0) 
        {
            MayThrow(x);
        }

        return x;
    }

    public static int OneMayThrowHelperTail(int x)
    {
        if (x > 0) 
        {
            MayThrow(x);
            return 0;
        }

        return x;
    }

    public static int OneReturnThrowHelper(int x)
    {
        if (x > 0) 
        {
            return ReturnThrow();
        }

        return x;
    }

    public static int OneReturnThrowHelperTail(int x)
    {
        if (x > 0) 
        {
            return ReturnThrow();
        }

        return x;
    }

    public static int TwoIdenticalThrowHelpers_If(int x)
    {
        if (x == 0) 
        {
            Throw();
        }
        
        if (x == 1) 
        {
            Throw();
        }

        return x;
    }

    internal static void TwoIdenticalThrowHelpers_IfOneTail(int x)
    {
        if (x == 0) 
        {
            Throw();
            return;
        }
        
        if (x == 1) 
        {
            Throw();
        }
    }

    internal static void TwoIdenticalThrowHelpers_IfTwoTail(int x)
    {
        if (x == 0) 
        {
            Throw();
            return;
        }
        
        if (x == 1) 
        {
            Throw();
            return;
        }
    }

    internal static int ThreeIdenticalThrowHelpers_If(int x)
    {
        if (x == 0) 
        {
            Throw();
        }
        
        if (x == 1) 
        {
            Throw();
        }

        if (x == 2) 
        {
            Throw();
        }

        return x;
    }

    internal static void ThreeIdenticalThrowHelpers_IfOneTail(int x)
    {
        if (x == 0) 
        {
            Throw();
            return;
        }
        
        if (x == 1) 
        {
            Throw();
        }

        if (x == 2) 
        {
            Throw();
        }
    }

    internal static void ThreeIdenticalThrowHelpers_IfTwoTail(int x)
    {
        if (x == 0) 
        {
            Throw();
            return;
        }
        
        if (x == 1) 
        {
            Throw();
            return;
        }

        if (x == 2) 
        {
            Throw();
        }
    }

    internal static void ThreeIdenticalThrowHelpers_IfThreeTail(int x)
    {
        if (x == 0) 
        {
            Throw();
            return;
        }
        
        if (x == 1) 
        {
            Throw();
            return;
        }

        if (x == 2) 
        {
            Throw();
            return;
        }
    }

    public static int TwoIdenticalThrowHelpers_Goto(int x)
    {
        if (x == 0) 
        {
            goto L1;
        }

        if (x == 1) 
        { 
            goto L2;
        }

        return x;
 L1:
        Throw();
 L2:
        Throw();
        
        return x;
    }

    internal static void TwoIdenticalThrowHelpers_GotoOneTail(int x)
    {
        if (x == 0) 
        {
            goto L1;
        }

        if (x == 1) 
        { 
            goto L2;
        }

        return;

 L1:
        Throw();
 L2:
        Throw();
    }

    internal static void TwoIdenticalThrowHelpers_GotoTwoTail(int x)
    {
        if (x == 0) 
        {
            goto L1;
        }

        if (x == 1) 
        { 
            goto L2;
        }

        return;
 L1:
        Throw();
        return;

 L2:
        Throw();
    }

    public static int TwoIdenticalThrowHelpers_Switch(int x)
    {
        switch (x)
        {
            case 0: 
            {
                Throw();
            }
            break;
        
            case 1:
            {
                Throw();
            }
            break;
        }

        return x;
    }

    internal static void TwoIdenticalThrowHelpers_SwitchOneTail(int x)
    {
        switch (x)
        {
            case 0: 
            {
                Throw();
                return;
            }
        
            case 1:
            {
                Throw();
            }
            break;
        }
    }

    internal static void TwoIdenticalThrowHelpers_SwitchTwoTail(int x)
    {
        switch (x)
        {
            case 0: 
            {
                Throw();
                return;
            }
        
            case 1:
            {
                Throw();
                return;
            }
        }
    }

    public static int TwoIdenticalThrowHelpers_SwitchGoto(int x)
    {
        switch (x)
        {
            case 0: 
            {
                goto L1;
            }
        
            case 1:
            {
                goto L2;
            }
        }

        return x;

 L1:
        Throw();
 L2:
        Throw();
        
        return x;
    }

    internal static void TwoIdenticalThrowHelpers_SwitchGotoOneTail(int x)
    {
        switch (x)
        {
            case 0: 
            {
                goto L1;
            }
        
            case 1:
            {
                goto L2;
            }
        }

        return;

 L1:
        Throw();
 L2:
        Throw();
    }

    internal static void TwoIdenticalThrowHelpers_SwitchGotoTwoTail(int x)
    {
        switch (x)
        {
            case 0: 
            {
                goto L1;
            }
        
            case 1:
            {
                goto L2;
            }
        }

        return;

 L1:
        Throw();        
        return;
 L2:
        Throw();
    }

    public static int TwoDifferentThrowHelpers(int x)
    {
        if (x == 0) 
        {
            Throw();
        }

        if (x == 1) 
        {
            Throw(1);
        }

        return x;
    }

    public static int TwoIdenticalThrowHelpersDifferentArgs(int x)
    {
        if (x == 0) 
        {
            Throw(0);
        }

        if (x == 1) 
        {
            Throw(1);
        }

        return x;
    }

    public static int TwoIdenticalThrowHelpersSameArgTrees(int x, int[] y)
    {
        if (x == 0)
        {
            Throw(y[0]);
        }
        else if (x == 1)
        {
            Throw(y[0]);
        }

        return x;
    }

    public static int TwoIdenticalThrowHelpersDifferentArgTrees(int x, int[] y)
    {
        if (x == 0)
        {
            Throw(y[0]);
        }
        else if (x == 1)
        {
            Throw(y[1]);
        }

        return x;
    }


    static int testNumber = 0;
    static bool failed = false;

    static void Try(Func<int, int> f)
    {
        testNumber++;

        if (f(-1) != -1)
        {
            Console.WriteLine($"Test {testNumber} failed\n");
            failed = true;
        }
    }

    static void Try(Func<int, int[], int> f)
    {
        testNumber++;

        int[] y = new int[0];
        if (f(-1, y) != -1)
        {
            Console.WriteLine($"Test {testNumber} failed\n");
            failed = true;
        }
    }

    static void Try(Action<int> f)
    {
        testNumber++;

        try 
        {
            f(-1);
        }
        catch (TestException)
        {
            Console.WriteLine($"Test {testNumber} failed\n");
            failed = true;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Try(OneThrowHelper);
        Try(OneThrowHelperTail);
        Try(OneMayThrowHelper);
        Try(OneMayThrowHelperTail);
        Try(OneReturnThrowHelper);
        Try(OneReturnThrowHelperTail);
        Try(TwoIdenticalThrowHelpers_If);
        Try(TwoIdenticalThrowHelpers_IfOneTail);
        Try(TwoIdenticalThrowHelpers_IfTwoTail);
        Try(TwoIdenticalThrowHelpers_Goto);
        Try(TwoIdenticalThrowHelpers_GotoOneTail);
        Try(TwoIdenticalThrowHelpers_GotoTwoTail);
        Try(TwoIdenticalThrowHelpers_Switch);
        Try(TwoIdenticalThrowHelpers_SwitchOneTail);
        Try(TwoIdenticalThrowHelpers_SwitchTwoTail);
        Try(TwoIdenticalThrowHelpers_SwitchGoto);
        Try(TwoIdenticalThrowHelpers_SwitchGotoOneTail);
        Try(TwoIdenticalThrowHelpers_SwitchGotoTwoTail);
        Try(TwoDifferentThrowHelpers);
        Try(TwoIdenticalThrowHelpersDifferentArgs);
        Try(ThreeIdenticalThrowHelpers_If);
        Try(ThreeIdenticalThrowHelpers_IfOneTail);
        Try(ThreeIdenticalThrowHelpers_IfTwoTail);
        Try(ThreeIdenticalThrowHelpers_IfThreeTail);
        Try(TwoIdenticalThrowHelpersSameArgTrees);
        Try(TwoIdenticalThrowHelpersDifferentArgTrees);

        Console.WriteLine(failed ? "" : $"All {testNumber} tests passed");
        return failed ? -1 : 100;
    }
}
