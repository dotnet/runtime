// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class BooleanCompareTo_Boolean
{

    public static int Main()
    {
        BooleanCompareTo_Boolean testCase = new BooleanCompareTo_Boolean();

        TestLibrary.TestFramework.BeginTestCase("Boolean.CompareTo(Boolean)");
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
			Boolean b0 = false;
            Boolean b1 = false;
            if (b0.CompareTo(b1) != 0)
            {
                TestLibrary.TestFramework.LogError("001", "expect false.CompareTo(false) == 0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        try
        {
            Boolean b0 = false;
            Boolean b1 = true;
            if (b0.CompareTo(b1) >= 0)
            {
                TestLibrary.TestFramework.LogError("002", "expect false.CompareTo(true) < 0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        try
        {
            Boolean b0 = true;
            Boolean b1 = false;
            if (b0.CompareTo(b1) <= 0)
            {
                TestLibrary.TestFramework.LogError("003", "expect true.CompareTo(false) > 0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        try
        {
            Boolean b0 = true;
            Boolean b1 = true;
            if (b0.CompareTo(b1) != 0)
            {
                TestLibrary.TestFramework.LogError("004", "expect true.CompareTo(true) == 0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
}
