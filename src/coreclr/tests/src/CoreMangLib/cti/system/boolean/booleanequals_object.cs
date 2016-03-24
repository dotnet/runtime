// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class BooleanEquals_Object
{

    public static int Main()
    {
        BooleanEquals_Object testCase = new BooleanEquals_Object();

        TestLibrary.TestFramework.BeginTestCase("Boolean.Equals(Object)");
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
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;


        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        try
        {
            Boolean b0 = false;
            Boolean b1 = false;
            if (!b0.Equals(b1 as Object))
            {
                TestLibrary.TestFramework.LogError("001", "expect false.Equals(false as object) == true");
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
            if (b0.Equals(b1 as Object))
            {
                TestLibrary.TestFramework.LogError("002", "expect false.Equals(true as object) == false");
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
            if (b0.Equals(b1 as Object))
            {
                TestLibrary.TestFramework.LogError("003", "expect true.Equals(false as object) == false");
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
            if (!b0.Equals(b1 as Object))
            {
                TestLibrary.TestFramework.LogError("004", "expect true.Equals(true as object) == true");
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

    public bool PosTest5()
    {
        bool retVal = true;
        try
        {
            Boolean b0 = false;
            if (b0.Equals(null))
            {
                TestLibrary.TestFramework.LogError("005", "expect false.Equals(null) == false");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        try
        {
            Boolean b0 = true;
            if (b0.Equals(null))
            {
                TestLibrary.TestFramework.LogError("006", "expect true.Equals(null) == false");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        try
        {
            Boolean b0 = true;
            if (b0.Equals(1))
            {
                TestLibrary.TestFramework.LogError("007", "expect true.Equals(1) == false");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest8()
    {
        bool retVal = true;
        try
        {
            if (false.Equals(0))
            {
                TestLibrary.TestFramework.LogError("008", "expect false.Equals(0) == false");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest9()
    {
        bool retVal = true;
        try
        {
            if (false.Equals(Single.NaN))
            {
                TestLibrary.TestFramework.LogError("009", "expect false.Equals(Single.NaN) == false");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
}
