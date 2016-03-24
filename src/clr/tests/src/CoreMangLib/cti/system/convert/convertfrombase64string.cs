// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using TestLibrary;

public class ConvertFromBase64String
{
    private const int c_MIN_SIZE = 64;
    private const int c_MAX_SIZE = 1024;

    public static int Main()
    {
        ConvertFromBase64String ac = new ConvertFromBase64String();

        TestLibrary.TestFramework.BeginTestCase("FromBase64String(string)");

        if (ac.RunTests())
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("PASS");
            return 100;
        }
        else
        {
            TestLibrary.TestFramework.EndTestCase();
            TestLibrary.TestFramework.LogInformation("FAIL");
            return 0;
        }
    }

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }
    //id inArray        length offsetIn exception
    public bool PosTest1() { return PosTest("TEST", new byte[] { 76, 68, 147 }, "00A"); }
    public bool PosTest2()
    {
        Random numGen = new Random(-55);
        bool ret = true;
        for (int i = 0; i < 1000; i++)
        {
            int byteLen = numGen.Next(c_MIN_SIZE, c_MAX_SIZE);
            byte[] expected = new byte[byteLen];
            Generator.GetBytes(-55, expected);
            string testStr = Convert.ToBase64String(expected);
            ret &= PosTest(testStr, expected, "00B");
        }
        return ret;
    }
    public bool PosTest3() { return PosTest("", new byte[0], "00C"); }
    public bool PosTest4() { return PosTest("   ", new byte[0], "00D"); }
    public bool PosTest5() { return PosTest(" T E   ST", new byte[] { 76, 68, 147 }, "00E"); }

    public bool NegTest1() { return NegTest(null, typeof(ArgumentNullException), "00F"); }
    public bool NegTest2() { return NegTest("Tes", typeof(FormatException), "00G"); }
    public bool NegTest3() { return NegTest("Tes\u00C0", typeof(FormatException), "00H"); }

    public bool PosTest(string input, byte[] expected, string id)
    {
        bool retVal = true;
        TestFramework.BeginScenario("\nPosTest " + id + ": FromBase64String");
        try
        {
            byte[] output = Convert.FromBase64String(input);
            if (!Utilities.CompareBytes(output, expected))
            {
            	TestFramework.LogInformation("Input string: " + input);
                TestFramework.LogError("001", "Conversion not correct. Expect: " + Utilities.ByteArrayToString(expected) + ", Actual: " + Utilities.ByteArrayToString(output));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e.ToString());
            retVal = false;
        }

        return retVal;
    }


    public bool NegTest(string input, Type exception, string id)
    {
        bool retVal = true;
        TestFramework.BeginScenario("NegTest " + id + ": FromBase64String()");
        try
        {
            byte[] output = Convert.FromBase64String(input);
            TestFramework.LogError("003", "Expected exception not thrown!");
            retVal = false;
        }
        catch (Exception e)
        {
            if (e.GetType() != exception)
            {
                TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e.ToString());
                retVal = false;
            }
        }

        return retVal;
    }
}

