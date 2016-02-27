// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

///<summary>
///System.DivideByZeroException.Ctor()
///</summary>

public class DivideByZeroExceptionCtor
{

    public static int Main()
    {
        DivideByZeroExceptionCtor testObj = new DivideByZeroExceptionCtor();
        TestLibrary.TestFramework.BeginTestCase("for constructor of System.DivideByZeroException");
        if (testObj.RunTests())
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
        retVal = PosTest1() && retVal;
        retVal = NegTest1() && retVal;
        return retVal;
    }


    #region Positive Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Create a instance of DivideByZeroException");
        try
        {
            DivideByZeroException dbz = new DivideByZeroException();

            if (dbz == null)
            {
                TestLibrary.TestFramework.LogError("001", "ExpectedValue(not null) !=ActualValue(null)");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

    #region Negtive Test Logic
    public bool NegTest1()
    {
        bool retVal = true;

        int tempInt = TestLibrary.Generator.GetInt32(-55);
        int divider = 0;

        TestLibrary.TestFramework.BeginScenario("NegTest1:Try to throw a DivideByZeroException");
        try
        {
            int temp = tempInt / divider;
            TestLibrary.TestFramework.LogError("003", "No DivideByZeroExcepthion thrown out expected.");
            retVal = false;
        }
        catch (DivideByZeroException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }

    #endregion
}
