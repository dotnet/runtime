// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using TestLibrary;
using System.Globalization;

class StringBuilderLength
{
    static int Main()
    {
        StringBuilderLength test = new StringBuilderLength();

        TestFramework.BeginTestCase("StringBuilder.Length");

        if (test.RunTests())
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestFramework.EndTestCase();
            TestFramework.LogInformation("FAIL");
            return 0;
        }

    }

    public bool RunTests()
    {
        bool ret = true;

        // Positive Tests
        ret &= Test1();
        ret &= Test2();
        ret &= Test3();
        ret &= Test4();
        ret &= Test5();

        // Negative Tests
        ret &= Test6();
        ret &= Test7();

        return ret;
    }

    public bool Test1()
    {
        string id = "Scenario1";
        bool result = true;
        TestFramework.BeginScenario("Scenario 1: Setting Length to 0");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Length = 0;
            string output = sb.ToString();
            int length = sb.Length;
            if (output != string.Empty)
            {
                result = false;
                TestFramework.LogError("001", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: " + string.Empty);
            }
            if (length != 0)
            {
                result = false;
                TestFramework.LogError("002", "Error in " + id + ", unexpected legnth. Actual length " + length + ", Expected: 0");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("003", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool Test2()
    {
        string id = "Scenario2";
        bool result = true;
        TestFramework.BeginScenario("Scenario 2: Setting Length to current length");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Length = 4;
            string output = sb.ToString();
            int length = sb.Length;
            if (output != "Test")
            {
                result = false;
                TestFramework.LogError("004", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: Test");
            }
            if (length != 4)
            {
                result = false;
                TestFramework.LogError("005", "Error in " + id + ", unexpected legnth. Actual length " + length + ", Expected: 4");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("006", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool Test3()
    {
        string id = "Scenario3";
        bool result = true;
        TestFramework.BeginScenario("Scenario 3: Setting Length to > length < capacity");
        try
        {
            StringBuilder sb = new StringBuilder("Test", 10);
            sb.Length = 8;
            string output = sb.ToString();
            int length = sb.Length;
            if (output != "Test\0\0\0\0")
            {
                result = false;
                TestFramework.LogError("007", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: Test\0\0\0\0");
            }
            if (length != 8)
            {
                result = false;
                TestFramework.LogError("008", "Error in " + id + ", unexpected legnth. Actual length " + length + ", Expected: 8");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("009", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool Test4()
    {
        string id = "Scenario4";
        bool result = true;
        TestFramework.BeginScenario("Scenario 4: Setting Length to > capacity");
        try
        {
            StringBuilder sb = new StringBuilder("Test", 10);
            sb.Length = 12;
            string output = sb.ToString();
            int length = sb.Length;
            if (output != "Test\0\0\0\0\0\0\0\0")
            {
                result = false;
                TestFramework.LogError("010", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: Test\0\0\0\0\0\0\0\0");
            }
            if (length != 12)
            {
                result = false;
                TestFramework.LogError("011", "Error in " + id + ", unexpected legnth. Actual length " + length + ", Expected: 12");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("012", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool Test5()
    {
        string id = "Scenario5";
        bool result = true;
        TestFramework.BeginScenario("Scenario 5: Setting Length to something very large");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Length = 10004;
            string output = sb.ToString();
            int length = sb.Length;
            if (output != ("Test" + new string('\0',10000)))
            {
                result = false;
                TestFramework.LogError("013", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: Test");
            }
            if (length != 10004)
            {
                result = false;
                TestFramework.LogError("014", "Error in " + id + ", unexpected legnth. Actual length " + length + ", Expected: 10004");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("015", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool Test6()
    {
        string id = "Scenario6";
        bool result = true;
        TestFramework.BeginScenario("Scenario 6: Setting Length to > max capacity");
        try
        {
            StringBuilder sb = new StringBuilder(4, 10);
            sb.Append("Test");

            sb.Length = 12;
            string output = sb.ToString();
            result = false;
            TestFramework.LogError("016", "Error in " + id + ", Expected exception not thrown. No exception. Actual string " + output + ", Expected: " + typeof(ArgumentOutOfRangeException).ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != typeof(ArgumentOutOfRangeException))
            {
                result = false;
                TestFramework.LogError("017", "Unexpected exception in " + id + ", expected type: " + typeof(ArgumentOutOfRangeException).ToString() + ", Actual excpetion: " + exc.ToString());
            }
        }
        return result;
    }

    public bool Test7()
    {
        string id = "Scenario7";
        bool result = true;
        TestFramework.BeginScenario("Scenario 7: Setting Length to < 0 capacity");
        try
        {
            StringBuilder sb = new StringBuilder(4, 10);
            sb.Append("Test");

            sb.Length = -1;
            string output = sb.ToString();
            result = false;
            TestFramework.LogError("018", "Error in " + id + ", Expected exception not thrown. No exception. Actual string " + output + ", Expected: " + typeof(ArgumentOutOfRangeException).ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != typeof(ArgumentOutOfRangeException))
            {
                result = false;
                TestFramework.LogError("018", "Unexpected exception in " + id + ", expected type: " + typeof(ArgumentOutOfRangeException).ToString() + ", Actual excpetion: " + exc.ToString());
            }
        }
        return result;
    }
}