// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Test_b07483
{
    private int _t = 0;
    private int _f = 0;

    [Fact]
    public static int TestEntryPoint()
    {
        Test_b07483 test = new Test_b07483();
        return (test.Run());
    }

    public int Run()
    {
        bool bFail = false;
        try
        {
            DoAnd();
            if ((_t != 0) || (_f != 0))
            {
                Console.WriteLine("Failure in BitAnd Tests");
                _t = _f = 0;
                bFail = true;
            }

            DoOr();
            if ((_t != 0) || (_f != 0))
            {
                Console.WriteLine("Failure in BitOr Tests");
                _t = _f = 0;
                bFail = true;
            }

            DoXor();
            if ((_t != 0) || (_f != 0))
            {
                Console.WriteLine("Failure in BitXor Tests");
                _t = _f = 0;
                bFail = true;
            }

            if (bFail)
                return (1);

            return (100);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception: {0}", e);
            return (1);
        }
    }

    private void DoAnd()
    {
        // bit and tests....
        Console.WriteLine("Testing And...");
        if (true & IsTrue())
            _t--;
        else
            throw new Exception("Bad Logic");

        if (false & IsTrue())
            throw new Exception("Bad Logic");
        else
            _t--;

        if (true & IsFalse())
            throw new Exception("Bad Logic");
        else
            _f--;

        if (false & IsFalse())
            throw new Exception("Bad Logic");
        else
            _f--;
    }

    private void DoOr()
    {
        // bit or tests....
        Console.WriteLine("Testing Or...");
        if (true | IsTrue())
            _t--;
        else
            throw new Exception("Bad Logic");

        if (false | IsTrue())
            _t--;
        else
            throw new Exception("Bad Logic");

        if (true | IsFalse())
            _f--;
        else
            throw new Exception("Bad Logic");

        if (false | IsFalse())
            throw new Exception("Bad Logic");
        else
            _f--;
    }

    private void DoXor()
    {
        // bit xor tests....
        Console.WriteLine("Testing Xor...");
        if (true ^ IsTrue())
            throw new Exception("Bad Logic");
        else
            _t--;

        if (false ^ IsTrue())
            _t--;
        else
            throw new Exception("Bad Logic");

        if (true ^ IsFalse())
            _f--;
        else
            throw new Exception("Bad Logic");

        if (false ^ IsFalse())
            throw new Exception("Bad Logic");
        else
            _f--;
    }

    public bool IsTrue() { Console.WriteLine("\tIsTrue() called"); _t++; return (true); }
    public bool IsFalse() { Console.WriteLine("\tIsFalse() called"); _f++; return (false); }
}
