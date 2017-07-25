// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    public override Object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
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

    public override Object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
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
    private String _strTestTmp;

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

    public String ReduceCount(double n)
    {
        _iUniqueVal -= (int)n;
        return _iUniqueVal.ToString();
    }

    public String AddToString(String str)
    {
        _strTestTmp = String.Concat(_strTestTmp, str);
        return _strTestTmp;
    }

    public override String ToString()
    {
        String S = String.Format("My Custom Object has a value of {0}", _iUniqueVal);
        return S;
    }

    public String PublicFunction()
    {
        return "Inside Public Function";
    }

    private String PrivateFunction()
    {
        return "Inside Private Function";
    }

    protected String ProtectedFunction()
    {
        return "Inside Protected Function";
    }

    private String DefaultFunction()
    {
        return "Default Function";
    }

    // Return types tests
    public int MyValue()
    {
        return _iUniqueVal;
    }

    public double GetUnitialized()
    {
        return _dNotUsed;
    }

    public String GetNull()
    {
        return null;
    }

    // Basic Tests
    public String Fn1()
    {
        return "Test1";
    }

    public String Fn2()
    {
        return "Test2";
    }

    public String Fn3()
    {
        return "Test3";
    }

    //Output Tests
    public void ConsoleWrite()
    {
        _output.WriteLine("\r\r\n\n> Where did I see this");
    }

    public String MessMeUp()
    {
        return ">\" $tmp >;\'\t \n&";
    }

    public String MessMeUp2()
    {
        return "<xsl:variable name=\"tmp\"/>";
    }

    public String MessMeUp3()
    {
        return "</xsl:stylesheet>";
    }

    //Recursion Tests
    public String RecursionSample()
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
    public String OverloadType(String str)
    {
        return "String Overlaod";
    }

    public String OverloadType(int i)
    {
        return "Int Overlaod";
    }

    public String OverloadType(double d)
    {
        return "Double Overload";
    }

    //Overload by arg
    public String OverloadArgTest(string s1)
    {
        return "String";
    }

    public String OverloadArgTest(string s1, string s2)
    {
        return "String, String";
    }

    public String OverloadArgTest(string s1, double d, string s2)
    {
        return "String, Double, String";
    }

    public String OverloadArgTest(string s1, string s2, double d)
    {
        return "String, String, Double";
    }

    // Overload conversion tests
    public String IntArg(int i)
    {
        return "Int";
    }

    public String BoolArg(Boolean i)
    {
        return "Boolean";
    }

    // Arg Tests
    public String ArgBoolTest(Boolean bFlag)
    {
        if (bFlag)
            return "Statement is True";
        return "Statement is False";
    }

    public String ArgDoubleTest(double d)
    {
        String s = String.Format("Received a double with value {0}", Convert.ToString(d, NumberFormatInfo.InvariantInfo));
        return s;
    }

    public String ArgStringTest(String s)
    {
        String s1 = String.Format("Received a string with value: {0}", s);
        return s1;
    }

    //Return tests
    public String ReturnString()
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

    public Boolean ReturnBooleanTrue()
    {
        return true;
    }

    public Boolean ReturnBooleanFalse()
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
