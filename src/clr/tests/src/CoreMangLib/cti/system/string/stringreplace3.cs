// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// String.Replace(String, String)
/// Replaces all occurrences of a specified Unicode character in this instance 
/// with another specified Unicode character. 
/// </summary>
public class StringReplace3
{
    public static int Main()
    {
        int result =  Test1() && Test2() && Test3() && Test4() && Test5() && Test6() && Test7() ? 100 : -1;

        return result; 
    }

    private static bool Test1()
    {
        string strExpected = "Test HEY with HEYAND HEY";
        string strActual = "Test %token% with %TOKEN%AND %tokeN%".Replace("%token%", "HEY", StringComparison.InvariantCultureIgnoreCase);

        return strExpected == strActual;
    }

    private static bool Test2()
    {
        string strExpected = "Test HEY with HEYAND Test!";
        string strActual = "Test %token% with %TOKEN%AND Test!".Replace("%token%", "HEY", StringComparison.InvariantCultureIgnoreCase);

        return strExpected == strActual;
    }

    private static bool Test3()
    {
        string strExpected = "HEYTest HEY with HEYAND Test!";
        string strActual = "%TOKEN%Test %token% with %TOKEN%AND Test!".Replace("%token%", "HEY", StringComparison.InvariantCultureIgnoreCase);

        return strExpected == strActual;
    }

    private static bool Test4()
    {
        string strExpected = "HEYHEYTest HEY with HEYAND Test!";
        string strActual = "%TOKEN%%token%Test %token% with %TOKEN%AND Test!".Replace("%token%", "HEY", StringComparison.InvariantCultureIgnoreCase);

        return strExpected == strActual;
    }

    private static bool Test5()
    {
        string strExpected = "dof";
        string strActual = "d\u00e9f".Replace("e\u0301", "o", StringComparison.InvariantCultureIgnoreCase);

        return strExpected == strActual;
    }

    private static bool Test6()
    {
        string strExpected = "dof";
        string strActual = "de\u0301f".Replace("\u00e9", "o", StringComparison.InvariantCultureIgnoreCase);

        return strExpected == strActual;
    }

    private static bool Test7()
    {
        string strExpected = "dfo";
        string strActual = "de\u0301fo".Replace("\u00e9", "", StringComparison.InvariantCultureIgnoreCase);

        return strExpected == strActual;
    }
}
