// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

///<summary>
///System.DllNotFoundException.Ctor()
///</summary>

public class DllNotFoundExceptionCtor
{

    public static int Main()
    {
        DllNotFoundExceptionCtor testObj = new DllNotFoundExceptionCtor();
        TestLibrary.TestFramework.BeginTestCase("for constructor of System.DllNotFoundException");
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
        return retVal;
    }


    #region Positive Test Logic
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1:Create a instance of DllNotFoundException");
        try
        {
            DllNotFoundException dbz = new DllNotFoundException();

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
}
