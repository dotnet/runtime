// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// to test System.Boolean.Parse(string)
/// </summary>
public class BooleanParse
{
    // not using Boolean.TrueString/FalseString in order to not test circularly
    const string cTRUE = "True";
    const string cFALSE = "False";

    public static int Main()
    {
        BooleanParse testCase = new BooleanParse();

        TestLibrary.TestFramework.BeginTestCase("Boolean.Parse");
        if (testCase.RunTests())
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
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;

        return retVal;
    }



    public bool PosTest1()
    {
        return PosTest("001", cFALSE, false);
    }

    public bool PosTest2()
    {
        return PosTest("002", cTRUE, true);
    }

    public bool PosTest3()
    {
        return PosTest("003", "\t"+cTRUE, true);
    }
    public bool PosTest4()
    {
        return PosTest("004", cFALSE+"  ", false);
    }

    public bool PosTest5()
    {
        return PosTest("005", " "+cFALSE+"\t ", false);
    }

    public bool PosTest6()
    {
        return PosTest("006", "fAlse", false);
    }

    public bool PosTest7()
    {
        return PosTest("007", "tRUE", true);
    }

    bool PosTest(string id, string str, bool expect)
    {
        bool retVal = true;
        try
        {
            if (Boolean.Parse(str) != expect)
            {
                TestLibrary.TestFramework.LogError(id, String.Format("expect Boolean.Parse(\"{0}\") == {1}",str, expect));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(id, "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }


    public bool NegTest1()
    {
        bool retVal = false;
        try
        {
            bool v = Boolean.Parse(null);
        }
        catch (ArgumentNullException)
        {
            // that exception is what's expected
            retVal = true;
            return retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
            return retVal;
        }

        TestLibrary.TestFramework.LogError("001", "expect a FormatException on Boolean.Parse(null)");
        return retVal;
    }

    public bool NegTest2()
    {
        return NegTest("002", "0");
    }
    public bool NegTest3()
    {
        return NegTest("003", "1");
    }
    public bool NegTest4()
    {
        return NegTest("004", "Yes");
    }
    public bool NegTest5()
    {
        return NegTest("005", String.Empty);
    }
    public bool NegTest6()
    {
        return NegTest("006", cTRUE + cFALSE);
    }

    public bool NegTest7()
    {
        return NegTest("007", cFALSE +" "+ cTRUE);
    }

    bool NegTest(string id, string str)
    {
        bool retVal = false;
        try
        {
            bool v = Boolean.Parse(str);
        }
        catch ( FormatException )
        {
            // that exception is what's expected
            retVal = true;
            return retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError(id, "Unexpected exception: " + e);
            retVal = false;
            return retVal;
        }

        TestLibrary.TestFramework.LogError(id, String.Format("expect a FormatException on Boolean.Parse(\"{0}\")", str));
        return retVal;
    }
}
