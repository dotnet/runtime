// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;
using TestLibrary;
using System.Globalization;

class StringBuilderAppend
{
    static int Main()
    {
        StringBuilderAppend test = new StringBuilderAppend();

        TestFramework.BeginTestCase("StringBuilder.Append");

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
        ret &= Test019();
        ret &= Test021();
        ret &= Test022();
        ret &= Test023();

        ret &= Test010();
        ret &= Test011();
        ret &= Test012();
        ret &= Test013();
        ret &= Test014();
        ret &= Test020();
        ret &= Test024();

        ret &= Test025();
        ret &= Test026();
        ret &= Test027();
        ret &= Test028();
        ret &= Test029();
        ret &= Test030();
        ret &= Test031();
        ret &= Test032();
        ret &= Test033();
        ret &= Test034();
        ret &= Test035();
        ret &= Test036();
        ret &= Test037();
        ret &= Test038();

        ret &= Test039();
        ret &= Test040();
        ret &= Test041();
        ret &= Test042();

        // Negative Tests
        ret &= Test006();
        ret &= Test007();
        ret &= Test008();
        ret &= Test009();

        ret &= Test015();
        ret &= Test016();
        ret &= Test017();
        ret &= Test018();

        return ret;
    }

    public bool Test001() { return PositiveTest("Testing", 1, 6, "esting", "00A");}
    public bool Test002() { return PositiveTest(new string('a', 5000), 0, 5000, new string('a',5000), "00B");}
    public bool Test003() { return PositiveTest("Testing", 1, 0, string.Empty, "00C");}
    public bool Test004() { return PositiveTest(null, 0, 0, string.Empty, "00D");}
    public bool Test005() { return PositiveTest(string.Empty, 0, 0, string.Empty, "00E");}

    public bool Test019() { return NegativeTest("Testing", -5, 0, typeof(ArgumentOutOfRangeException), "00J"); }
    public bool Test006() { return NegativeTest(null, 0, 1, typeof(ArgumentNullException), "00F"); }
    public bool Test007() { return NegativeTest("a", -1, 1, typeof(ArgumentOutOfRangeException), "00G"); }
    public bool Test008() { return NegativeTest("a", 0, -1, typeof(ArgumentOutOfRangeException), "00H"); }
    public bool Test009() { return NegativeTest("a", 0, 3, typeof(ArgumentOutOfRangeException), "00I"); }

    public bool Test010() { return PositiveTest2(new char[] {'T', 'e', 's', 't', 'i', 'n', 'g'}, 1, 6, "esting", "00A1"); }
    public bool Test011() { char[] chars = new char[5000]; for (int i = 0; i < 5000; i++) chars[i] = 'a';
                            return PositiveTest2(chars, 0, 5000, new string('a', 5000), "00B1"); }
    public bool Test012() { return PositiveTest2(new char[] {'T', 'e', 's', 't', 'i', 'n', 'g'}, 1, 0, string.Empty, "00C1"); }
    public bool Test013() { return PositiveTest2(null, 0, 0, string.Empty, "00D1"); }
    public bool Test014() { return PositiveTest2(new char[] { }, 0, 0, string.Empty, "00E1"); }

    public bool Test020() { return NegativeTest2(new char[] { 'T', 'e', 's', 't', 'i', 'n', 'g' }, -5, 0, typeof(ArgumentOutOfRangeException), "00J1"); }
    public bool Test015() { return NegativeTest2(null, 0, 1, typeof(ArgumentNullException), "00F1"); }
    public bool Test016() { return NegativeTest2(new char[] { 'T', 'e', 's', 't', 'i', 'n', 'g' }, - 1, 1, typeof(ArgumentOutOfRangeException), "00G1"); }
    public bool Test017() { return NegativeTest2(new char[] { 'T', 'e', 's', 't', 'i', 'n', 'g' }, 0, -1, typeof(ArgumentOutOfRangeException), "00H1"); }
    public bool Test018() { return NegativeTest2(new char[] { 'T' }, 0, 3, typeof(ArgumentOutOfRangeException), "00I1"); }

    public bool Test021() { return PositiveTest3('T', 6, "TTTTTT", "00A2"); }
    public bool Test022() { return PositiveTest3('a', 5000, new string('a', 5000), "00B2"); }
    public bool Test023() { return PositiveTest3('a', 0, string.Empty, "00C2"); }

    public bool Test024() { return NegativeTest3('a', -1, typeof(ArgumentOutOfRangeException), "00G2"); }

    public bool Test025() { return PositiveTest4<byte>(1, "00K"); }
    public bool Test026() { return PositiveTest4<sbyte>(-1, "00L"); }
    public bool Test027() { return PositiveTest4<bool>(true, "00M"); }
    public bool Test028() { return PositiveTest4<char>('t', "00N"); }
    public bool Test029() { return PositiveTest4<short>(short.MaxValue, "00O"); }
    public bool Test030() { return PositiveTest4<int>(int.MaxValue, "00P"); }
    public bool Test031() { return PositiveTest4<long>(long.MaxValue, "00Q"); }
    public bool Test032() { return PositiveTest4<float>(3.14f, "00R"); }
    public bool Test033() { return PositiveTest4<double>(3.1415927, "00S"); }
    public bool Test034() { return PositiveTest4<ushort>(17, "00T"); }
    public bool Test035() { return PositiveTest4<uint>(uint.MaxValue, "00U"); }
    public bool Test036() { return PositiveTest4<ulong>(ulong.MaxValue, "00V"); }
    public bool Test037() { return PositiveTest4<object>(null, "00W"); }
    public bool Test038() { return PositiveTest4<object>(new StringBuilder("Testing"), "00X"); }

    public bool Test039() { return PositiveTest5(new char[] { 'T', 'e', 's', 't' }, "Test", "00Y"); }
    public bool Test040() { char[] chars = new char[5000]; 
                            for (int i = 0; i < 5000; i++) chars[i] = 'a';
                            return PositiveTest5(chars, new string('a', 5000), "00Z"); }
    public bool Test041() { return PositiveTest5(new char[] {  }, String.Empty, "0AA"); }
    public bool Test042() { return PositiveTest5(null, String.Empty, "0AB"); }


    public bool PositiveTest(string str, int index, int count, string expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Append");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Append(str, index, count);
            string output = sb.ToString();
            expected = "Test" + expected;
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001", "Error in " + id + ", unexpected append result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool PositiveTest2(char[] str, int index, int count, string expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Append");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Append(str, index, count);
            string output = sb.ToString();
            expected = "Test" + expected;
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001a", "Error in " + id + ", unexpected append result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002a", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool PositiveTest3(char str, int count, string expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Append");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Append(str, count);
            string output = sb.ToString();
            expected = "Test" + expected;
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001b", "Error in " + id + ", unexpected append result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002b", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool PositiveTest4<T>(T str, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Append");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Append(str);
            string output = sb.ToString();
            string expected = ((str == null)?"Test":"Test" + str.ToString());
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001c", "Error in " + id + ", unexpected append result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002c", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool PositiveTest5(char[] str, string expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Append");
        try
        {
            StringBuilder sb = new StringBuilder("Test");
            sb.Append(str);
            string output = sb.ToString();
            expected = "Test" + expected;
            if (output != expected)
            {
                result = false;
                TestFramework.LogError("001d", "Error in " + id + ", unexpected append result. Actual string " + output + ", Expected: " + expected);
            }
        }
        catch (Exception exc)
        {
            result = false;
            TestFramework.LogError("002d", "Unexpected exception in " + id + ", excpetion: " + exc.ToString());
        }
        return result;
    }

    public bool NegativeTest(string str, int index, int count, Type expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Append");
        try
        {
            StringBuilder sb = new StringBuilder("Test");

            sb.Append(str, index, count);
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

    public bool NegativeTest2(char[] str, int index, int count, Type expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Append");
        try
        {
            StringBuilder sb = new StringBuilder("Test");

            sb.Append(str, index, count);
            string output = sb.ToString();
            result = false;
            TestFramework.LogError("003b", "Error in " + id + ", Expected exception not thrown. No exception. Actual string " + output + ", Expected: " + expected.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != expected)
            {
                result = false;
                TestFramework.LogError("004b", "Unexpected exception in " + id + ", expected type: " + expected.ToString() + ", Actual excpetion: " + exc.ToString());
            }
        }
        return result;
    }


    public bool NegativeTest3(char str, int count, Type expected, string id)
    {
        bool result = true;
        TestFramework.BeginScenario(id + ": Append");
        try
        {
            StringBuilder sb = new StringBuilder("Test");

            sb.Append(str, count);
            string output = sb.ToString();
            result = false;
            TestFramework.LogError("003c", "Error in " + id + ", Expected exception not thrown. No exception. Actual string " + output + ", Expected: " + expected.ToString());
        }
        catch (Exception exc)
        {
            if (exc.GetType() != expected)
            {
                result = false;
                TestFramework.LogError("004c", "Unexpected exception in " + id + ", expected type: " + expected.ToString() + ", Actual excpetion: " + exc.ToString());
            }
        }
        return result;
    }
}