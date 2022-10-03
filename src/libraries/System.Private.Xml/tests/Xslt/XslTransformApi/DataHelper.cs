// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;
using System;
using System.Globalization;
using System.Xml;

public class CustomUrlResolver : XmlUrlResolver
{
    private ITestOutputHelper _output;
    public CustomUrlResolver(ITestOutputHelper output)
    {
        _output = output;
    }

    public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        _output.WriteLine("Getting {0}", absoluteUri);
        return base.GetEntity(absoluteUri, role, ofObjectToReturn);
    }
}

public class CustomNullResolver : XmlUrlResolver
{
    private ITestOutputHelper _output;
    public CustomNullResolver(ITestOutputHelper output)
    {
        _output = output;
    }

    public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
    {
        _output.WriteLine("Getting {0}", absoluteUri);
        return null;
    }
}

// These two classes are for bug 78587 repro
public class Id
{
    private string _id;

    public Id(string id)
    {
        _id = id;
    }

    public string GetId()
    {
        return _id;
    }
}

public class Capitalizer
{
    public string Capitalize(string str)
    {
        return str.ToUpper();
    }
}

public class MyObject
{
    private int _iUniqueVal;
    private double _dNotUsed;
    private string _strTestTmp;

    private ITestOutputHelper _output;

    // State Tests
    public MyObject(int n, ITestOutputHelper output)
    {
        _iUniqueVal = n;
        _output = output;
    }

    public MyObject(double n, ITestOutputHelper output)
    {
        _dNotUsed = n;
        _output = output;
    }

    public void DecreaseCounter()
    {
        _iUniqueVal--;
    }

    public string ReduceCount(double n)
    {
        _iUniqueVal -= (int)n;
        return _iUniqueVal.ToString();
    }

    public string AddToString(string str)
    {
        _strTestTmp = string.Concat(_strTestTmp, str);
        return _strTestTmp;
    }

    public override string ToString()
    {
        string S = string.Format("My Custom Object has a value of {0}", _iUniqueVal);
        return S;
    }

    public string PublicFunction()
    {
        return "Inside Public Function";
    }

    private string PrivateFunction()
    {
        return "Inside Private Function";
    }

    protected string ProtectedFunction()
    {
        return "Inside Protected Function";
    }

    private string DefaultFunction()
    {
        return "Default Function";
    }

    // Return types tests
    public int MyValue()
    {
        return _iUniqueVal;
    }

    public double GetUninitialized()
    {
        return _dNotUsed;
    }

    public string GetNull()
    {
        return null;
    }

    // Basic Tests
    public string Fn1()
    {
        return "Test1";
    }

    public string Fn2()
    {
        return "Test2";
    }

    public string Fn3()
    {
        return "Test3";
    }

    //Output Tests
    public void ConsoleWrite()
    {
        _output.WriteLine("\r\r\n\n> Where did I see this");
    }

    public string MessMeUp()
    {
        return ">\" $tmp >;\'\t \n&";
    }

    public string MessMeUp2()
    {
        return "<xsl:variable name=\"tmp\"/>";
    }

    public string MessMeUp3()
    {
        return "</xsl:stylesheet>";
    }

    //Recursion Tests
    public string RecursionSample()
    {
        return (Factorial(5)).ToString();
    }

    public int Factorial(int n)
    {
        if (n < 1)
            return 1;
        return (n * Factorial(n - 1));
    }

    //Overload by type
    public string OverloadType(string str)
    {
        return "String Overload";
    }

    public string OverloadType(int i)
    {
        return "Int Overload";
    }

    public string OverloadType(double d)
    {
        return "Double Overload";
    }

    //Overload by arg
    public string OverloadArgTest(string s1)
    {
        return "String";
    }

    public string OverloadArgTest(string s1, string s2)
    {
        return "String, String";
    }

    public string OverloadArgTest(string s1, double d, string s2)
    {
        return "String, Double, String";
    }

    public string OverloadArgTest(string s1, string s2, double d)
    {
        return "String, String, Double";
    }

    // Overload conversion tests
    public string IntArg(int i)
    {
        return "Int";
    }

    public string BoolArg(bool i)
    {
        return "Boolean";
    }

    // Arg Tests
    public string ArgBoolTest(bool bFlag)
    {
        if (bFlag)
            return "Statement is True";
        return "Statement is False";
    }

    public string ArgDoubleTest(double d)
    {
        string s = string.Format("Received a double with value {0}", Convert.ToString(d, NumberFormatInfo.InvariantInfo));
        return s;
    }

    public string ArgStringTest(string s)
    {
        string s1 = string.Format("Received a string with value: {0}", s);
        return s1;
    }

    //Return tests
    public string ReturnString()
    {
        return "Hello world";
    }

    public int ReturnInt()
    {
        return 10;
    }

    public double ReturnDouble()
    {
        return 022.4127600;
    }

    public bool ReturnBooleanTrue()
    {
        return true;
    }

    public bool ReturnBooleanFalse()
    {
        return false;
    }

    public MyObject ReturnOther()
    {
        return this;
    }

    public void DoNothing()
    {
    }
}
