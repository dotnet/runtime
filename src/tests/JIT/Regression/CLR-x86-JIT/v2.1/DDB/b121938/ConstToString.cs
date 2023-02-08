// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class ConstToString
{
    static int IntConstToString()
    {
        int iret = 100;
        string s = (10).ToString() + "." + (20).ToString();
        if (s != "10.20")
        {
            Console.WriteLine("FAIL: IntConstToString");
            iret = 666;
        }
        else
        {
            Console.WriteLine("IntConstToString ok");
        }
        return iret;
    }
    static int FloatConstToString()
    {
        int iret = 100;
        string s = (10F).ToString() + "." + (20F).ToString();
        if (s != "10.20")
        {
            Console.WriteLine("FAIL: FloatConstToString");
            iret = 666;
        }
        else
        {
            Console.WriteLine("FloatConstToString ok");
        }
        return iret;
    }
    static int StringConstToString()
    {
        int iret = 100;
        string s = ("ABC").ToString() + "." + ("DEF").ToString();
        if (s != "ABC.DEF")
        {
            Console.WriteLine("FAIL: StringConstToString");
            iret = 666;
        }
        else
        {
            Console.WriteLine("StringConstToString ok");
        }
        return iret;
    }
    static int BoolConstToString()
    {
        int iret = 100;
        string s = (true).ToString() + "." + (false).ToString();
        if (s != "True.False")
        {
            Console.WriteLine("FAIL: BoolConstToString");
            iret = 666;
        }
        else
        {
            Console.WriteLine("BoolConstToString ok");
        }
        return iret;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        int iret = 100;
        if (IntConstToString() != 100)
            iret = 666;
        if (FloatConstToString() != 100)
            iret = 666;
        if (StringConstToString() != 100)
            iret = 666;
        if (BoolConstToString() != 100)
            iret = 666;
        if (iret == 100)
        {
            Console.WriteLine("PASS");
        }
        return iret;
    }


}

