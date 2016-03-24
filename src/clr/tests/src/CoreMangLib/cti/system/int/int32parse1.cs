// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//system.Int32.Parse(system.string)
public class Int32Parse1
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;


        TestLibrary.TestFramework.BeginScenario("PosTest1: normal test basic function of the method ");

        try
        {
            string s1 = Int32.MaxValue.ToString();
            if (Int32.MaxValue != Int32.Parse(s1))
            {
                TestLibrary.TestFramework.LogError("001", "the result is not the int32.maxvalue as expected ");
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

    public bool PosTest2()
    {
        bool retVal = true;


        TestLibrary.TestFramework.BeginScenario("PosTest2: normal test about int32.minvalue ");

        try
        {
            string s1 = Int32.MinValue.ToString();
            if (Int32.MinValue != Int32.Parse(s1))
            {
                TestLibrary.TestFramework.LogError("003", "the result is not the int32.minvalue as expected ");
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: test the parameter of white space in both of the beginning and end of the string");

        try
        {
            string s1 = " 12125684 ";
            Int32 i1 = Int32.Parse(s1);
            if (i1 != 12125684)
            {
                TestLibrary.TestFramework.LogError("005", "the result is not the int32 as expected ");
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: test the parameter (-0)");

        try
        {
            string s1 = "-0";
            Int32 i1 = Int32.Parse(s1);
            if (i1 != 0)
            {
                TestLibrary.TestFramework.LogError("007", "the result is not the 0 as expected ");
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
    #endregion

    #region Negative Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: null reference parameter ");

        try
        {
            string s1 = null;
            Int32 i1 = Int32.Parse(s1);
            TestLibrary.TestFramework.LogError("101", "the Method did not throw a ArgumentNullException");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }

        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: test FormatException1 ");

        string s1 = "-321-677";
        try
        {
            Int32 i1 = Int32.Parse(s1);
            TestLibrary.TestFramework.LogError("103", "the Method did not throw a FormatException,patameter is: " + s1);
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: test FormatException2, parameter:43dce6a");

        try
        {
            string s1 = "43dce6a";
            Int32 i1 = Int32.Parse(s1);
            TestLibrary.TestFramework.LogError("105", "the Method did not throw a FormatException,patameter is: " + s1);
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: test FormatException3 by setting the parameter white space");

        try
        {
            string s1 = "  ";
            Int32 i1 = Int32.Parse(s1);
            TestLibrary.TestFramework.LogError("107", "the Method did not throw a FormatException,patameter is: white space");
            retVal = false;
        }
        catch (FormatException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: test OverFlowException");

        try
        {
            string s1 = "2147483648";
            Int32 i1 = Int32.Parse(s1);
            TestLibrary.TestFramework.LogError("109", "the Method did not throw an OverflowException ,patameter is: Int32.MaxValue+1");
            retVal = false;
        }
        catch (OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int32Parse1 test = new Int32Parse1();

        TestLibrary.TestFramework.BeginTestCase("Int32Parse");

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
