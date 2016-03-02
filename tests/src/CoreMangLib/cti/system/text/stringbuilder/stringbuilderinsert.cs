// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using TestLibrary;
using System.Globalization;

class StringBuilderInsert
{
    static int Main()
    {
        StringBuilderInsert test = new StringBuilderInsert();

        TestFramework.BeginTestCase("StringBuilder.Insert");

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
        ret &= Test001();
        ret &= Test002();
        ret &= Test003();
        ret &= Test004();
        ret &= Test005();

        ret &= Test012();
        ret &= Test013();
//        ret &= Test014();
        
        // Negative Tests
        ret &= Test006();
        ret &= Test007();
        ret &= Test008();
        ret &= Test009();
        ret &= Test010();
        ret &= Test011();

        return ret;
    }

    public bool Test001() { return PositiveTest(2, new char[] { 'a', 'b', 'c', 'd', 'e' }, 1, 2, "Tebcst", "00A"); }
    public bool Test002() { return PositiveTest(2, null, 0, 0, "Test", "00B"); }
    public bool Test003() { char[] chars = new char[10000]; for (int i = 0; i < 10000; i++) chars[i] = 'a';
                            return PositiveTest(4, chars, 0, 10000, "Test" + new string('a', 10000), "00C"); }
    public bool Test004() { return PositiveTest(0, new char[] { 'a', 'b', 'c', 'd', 'e' }, 0, 1, "aTest", "00D"); }
    public bool Test005() { return PositiveTest(1, new char[] { 'a', 'b', 'c', 'd', 'e' }, 3, 0, "Test", "00E"); }

    public bool Test006() { return NegativeTest(-1, new char[] { 'a', 'b', 'c', 'd', 'e' }, 1, 2, typeof(ArgumentOutOfRangeException), "00F"); }
    public bool Test007() { return NegativeTest(5, new char[] { 'a', 'b', 'c', 'd', 'e' }, 1, 2, typeof(ArgumentOutOfRangeException), "00G"); }
    public bool Test008() { return NegativeTest(0, new char[] { 'a', 'b', 'c', 'd', 'e' }, -1, 2, typeof(ArgumentOutOfRangeException), "00H"); }
    public bool Test009() { return NegativeTest(0, new char[] { 'a', 'b', 'c', 'd', 'e' }, 1, -1, typeof(ArgumentOutOfRangeException), "00I"); }
    public bool Test010() { return NegativeTest(0, new char[] { 'a', 'b', 'c', 'd', 'e' }, 4, 3, typeof(ArgumentOutOfRangeException), "00J"); }
    public bool Test011() { return NegativeTest(0, null, 0, 1, typeof(ArgumentNullException), "00K"); }

    public bool Test012() { return PositiveTest2(2, new char[] { 'a', 'b', 'c', 'd', 'e' }, "Teabcdest", "00A1"); }
    public bool Test013() { return PositiveTest2(2, null, "Test", "00B1"); }

//    public bool Test014() { return PositiveTest3(2, 't', "Tetst", "00A2"); }

    public bool PositiveTest(int index, char[] chars, int startIndex, int count, string expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Insert");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Insert(index, chars, startIndex, count);
            string output = sb.ToString();
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001", "Error in " + id + ", unexpected insert result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool PositiveTest2(int index, char[] chars, string expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Insert");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Insert(index, chars);
            string output = sb.ToString();
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001a", "Error in " + id + ", unexpected insert result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002a", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    //public bool PositiveTest3(int index, char chars, string expected, string id)
    //{
    //    bool result = true;
    //    TestFramework.BeginScenario(id + ": Insert");
    //    try
    //    {
    //        StringBuilder sb = new StringBuilder("Test");
    //        sb.Insert(index, chars);
    //        string output = sb.ToString();
    //        if (output != expected)
    //        {
    //            result = false;
    //            TestFramework.LogError("001b", "Error in " + id + ", unexpected insert result. Actual string " + output + ", Expected: " + expected);
    //        }
    //    }
    //    catch (Exception exc)
    //    {
    //        result = false;
    //        TestFramework.LogError("002b", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
    //    }
    //    return result;
    //}

    public bool NegativeTest(int index, char[] chars, int startIndex, int count, Type expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Insert");
        try
        {
            StringBuilder sb = new StringBuilder("Test");

            sb.Insert(index, chars, startIndex, count);
            string output = sb.ToString();
            result = false;
            TestFramework.LogError("003", "Error in " + id + ", Expected exception not thrown. No exception. Actual string " + output + ", Expected: " + expected.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != expected)
            {
                result = false;
                TestFramework.LogError("004", "Unexpected exception in " + id + ", expected type: " + expected.ToString() + ", Actual excpetion: " + exc.ToString());
            }
        }
        return result;
    }
}