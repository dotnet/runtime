// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//System.Int32.Equals(System.Object)
public class Int32Equals2
{
    #region Public Methods
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: test two random Int32");

        try
        {
            Int32 o1;
            object o2 = o1 = TestLibrary.Generator.GetInt32(-55);
            if (!o1.Equals(o2))
            {
                TestLibrary.TestFramework.LogError("001", "Equal error , random number: " + o1);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: test 0 = 0");

        try
        {
            Int32 o1 = 0;
            object o2 = 0;
            if (!o1.Equals(o2))
            {
                TestLibrary.TestFramework.LogError("003", "Equal error 0 = 0  ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: test Int32.MaxValue");

        try
        {
            Int32 o1 = Int32.MaxValue;
            object o2 = Int32.MaxValue;
            if (!o1.Equals(o2))
            {
                TestLibrary.TestFramework.LogError("005", "Equal error Int32.MaxValue ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: test Int32.MinValue");

        try
        {
            Int32 o1 = Int32.MinValue;
            object o2 = Int32.MinValue;
            if (!o1.Equals(o2))
            {
                TestLibrary.TestFramework.LogError("007", "Equal error Int32.MinValue ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: test two different value");

        try
        {
            Int32 o1 = 0;
            object o2 = o1 + 1;

            if (o1.Equals(o2))
            {
                TestLibrary.TestFramework.LogError("009", "Equal two different value get a true, error");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest6: test other parameter ");

        try
        {
            double f1 = 3.00;
            Int32 i1 = 3;
            if (i1.Equals(f1))
            {
                TestLibrary.TestFramework.LogError("011", "different kind parameters is not equal");
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases

    #endregion
    #endregion

    public static int Main()
    {
        Int32Equals2 test = new Int32Equals2();

        TestLibrary.TestFramework.BeginTestCase("Int32Equals2");

        if (test.RunTests())
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
}

