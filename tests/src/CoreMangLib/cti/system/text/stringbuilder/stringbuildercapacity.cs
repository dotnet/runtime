// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using TestLibrary;
using System.Globalization;

class StringBuilderCapacity
{
    static int Main()
    {
        StringBuilderCapacity test = new StringBuilderCapacity();

        TestFramework.BeginTestCase("StringBuilder.Capacity");

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
        ret &= Test2();
        ret &= Test3();
        ret &= Test4();
        ret &= Test5();

        // Negative Tests
        ret &= Test1();
        ret &= Test6();
        ret &= Test7();

        return ret;
    }

    public bool Test1()
    {
        string id = "Scenario1";
        bool result = true;
        TestFramework.BeginScenario("Scenario 1: Setting Capacity to 0");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Capacity = 0;
            string output = sb.ToString();
            int cap = sb.Capacity;
            result = false;
            TestFramework.LogError("001", "Error in " + id + ", expected exception not thrown. Capacity: " + cap + ", string: " + output);

        }
        catch (Exception exc)
        {
            if (exc.GetType() != typeof(ArgumentOutOfRangeException))
            {
                result = false;
                TestFramework.LogError("003", "Unexpected exception in " + id + ", expected type: " + typeof(ArgumentOutOfRangeException).ToString() + ", Actual exception: " + exc.ToString());
            }
        }
        return result;
    }

    public bool Test2()
    {
        string id = "Scenario2";
        bool result = true;
        TestFramework.BeginScenario("Scenario 2: Setting capacity to current capacity");
        try
        {
            StringBuilder sb = new StringBuilder("Test", 4);
            sb.Capacity = 4;
            string output = sb.ToString();
            int cap = sb.Capacity;
            if (output != "Test")
            {
                result = false;
                TestFramework.LogError("004", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: Test");
            }
            if (cap != 4)
            {
                result = false;
                TestFramework.LogError("005", "Error in " + id + ", unexpected capacity. Actual capacity " + cap + ", Expected: 4");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("006", "Unexpected exception in " + id + ", exception: " + exc.ToString());
        }
        return result;
    }

    public bool Test3()
    {
        string id = "Scenario3";
        bool result = true;
        TestFramework.BeginScenario("Scenario 3: Setting capacity to > length < capacity");
        try
        {
            StringBuilder sb = new StringBuilder("Test", 10);
            sb.Capacity = 8;
            string output = sb.ToString();
            int capacity = sb.Capacity;
            if (output != "Test")
            {
                result = false;
                TestFramework.LogError("007", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: Test");
            }
            if (capacity != 8)
            {
                result = false;
                TestFramework.LogError("008", "Error in " + id + ", unexpected legnth. Actual capacity" + capacity + ", Expected: 8");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("009", "Unexpected exception in " + id + ", exception: " + exc.ToString());
        }
        return result;
    }

    public bool Test4()
    {
        string id = "Scenario4";
        bool result = true;
        TestFramework.BeginScenario("Scenario 4: Setting capacity to > capacity");
        try
        {
            StringBuilder sb = new StringBuilder("Test", 10);
            sb.Capacity = 12;
            string output = sb.ToString();
            int cap = sb.Capacity;
            if (output != "Test")
            {
                result = false;
                TestFramework.LogError("010", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: Test");
            }
            if (cap != 12)
            {
                result = false;
                TestFramework.LogError("011", "Error in " + id + ", unexpected legnth. Actual capacity " + cap + ", Expected: 12");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("012", "Unexpected exception in " + id + ", exception: " + exc.ToString());
        }
        return result;
    }

    public bool Test5()
    {
        string id = "Scenario5";
        bool result = true;
        TestFramework.BeginScenario("Scenario 5: Setting capacity to something very large");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Capacity = 10004;
            string output = sb.ToString();
            int capacity = sb.Capacity;
            if (output != "Test")
            {
                result = false;
                TestFramework.LogError("013", "Error in " + id + ", unexpected string. Actual string " + output + ", Expected: Test");
            }
            if (capacity != 10004)
            {
                result = false;
                TestFramework.LogError("014", "Error in " + id + ", unexpected legnth. Actual capacity " + capacity + ", Expected: 10004");
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("015", "Unexpected exception in " + id + ", exception: " + exc.ToString());
        }
        return result;
    }

    public bool Test6()
    {
        string id = "Scenario6";
        bool result = true;
        TestFramework.BeginScenario("Scenario 6: Setting Capacity to > max capacity");
        try
        {
            StringBuilder sb = new StringBuilder(4, 10);
            sb.Append("Test");

            sb.Capacity = 12;
            string output = sb.ToString();
            result = false;
            TestFramework.LogError("016", "Error in " + id + ", Expected exception not thrown. No exception. Actual string " + output + ", Expected: " + typeof(ArgumentOutOfRangeException).ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != typeof(ArgumentOutOfRangeException))
            {
                result = false;
                TestFramework.LogError("017", "Unexpected exception in " + id + ", expected type: " + typeof(ArgumentOutOfRangeException).ToString() + ", Actual exception: " + exc.ToString());
            }
        }
        return result;
    }

    public bool Test7()
    {
        string id = "Scenario7";
        bool result = true;
        TestFramework.BeginScenario("Scenario 7: Setting capacity to < 0");
        try
        {
            StringBuilder sb = new StringBuilder(4, 10);
            sb.Append("Test");

            sb.Capacity = -1;
            string output = sb.ToString();
            result = false;
            TestFramework.LogError("018", "Error in " + id + ", Expected exception not thrown. No exception. Actual string " + output + ", Expected: " + typeof(ArgumentOutOfRangeException).ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != typeof(ArgumentOutOfRangeException))
            {
                result = false;
                TestFramework.LogError("018", "Unexpected exception in " + id + ", expected type: " + typeof(ArgumentOutOfRangeException).ToString() + ", Actual exception: " + exc.ToString());
            }
        }
        return result;
    }
}