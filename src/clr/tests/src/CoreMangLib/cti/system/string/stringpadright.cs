using System;
using System.Threading;
using System.Globalization;
using TestLibrary;

class StringPadRight
{
    static int Main()
    {
        StringPadRight test = new StringPadRight();

        TestFramework.BeginTestCase("String.PadRight");

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
        ret &= Test6();
        ret &= Test7();
        ret &= Test8();

        return ret;
    }

    public bool Test1() { return PositiveTest("TestString", 15, "TestString     ", "00A"); }
    public bool Test2() { return PositiveTest("", 5000, new string(' ', 5000), "00B"); }
    public bool Test3() { return PositiveTest("TestString", 10, "TestString", "00B1"); }
    public bool Test4() { return PositiveTest("TestString", 6, "TestString", "00B2"); }
    public bool Test5() { return PositiveTest2("TestString", 15, '*', "TestString*****", "00C"); }
    public bool Test6() { return PositiveTest2("", 5000, '*', new string('*', 5000), "00D"); }
    public bool Test7() { return PositiveTest2("TestString", 100, '\0', "TestString" + new string('\0', 90), "00E"); }
    public bool Test8() { return PositiveTest2("TestString", 100, '\u0400', "TestString" + new string('\u0400', 90), "00F"); }


    public bool PositiveTest(string str1, int totalWidth, string expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Padding " + str1 + " to " + totalWidth);
        try
        {
            string output = str1.PadRight(totalWidth);
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001", "Error in " + id + ", unexpected padding result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool PositiveTest2(string str1, int totalWidth, char padder, string expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Padding " + str1 + " to " + totalWidth);
        try
        {
            string output = str1.PadRight(totalWidth, padder);
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001", "Error in " + id + ", unexpected padding result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

}