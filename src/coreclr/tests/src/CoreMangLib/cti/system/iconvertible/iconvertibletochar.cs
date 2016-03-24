// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class IConvertibleToChar
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        //retVal = PosTest1() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    //public bool PosTest1()
    //{
    //    bool retVal = true;

    //    // Add your scenario description here
    //    TestLibrary.TestFramework.BeginScenario("PosTest1: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Verify method ToChar of interface IConvertible when value is true.");

        try
        {
            char v = (true as IConvertible).ToChar(null);
            TestLibrary.TestFramework.LogError("101.1",
                String.Format("expected a InvalidCastException on (true as IConvertible).ToChar(null)) but got {0}", v));
            retVal = false;
        }
        catch (InvalidCastException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Verify method ToChar of interface IConvertible when value is false.");

        try
        {
            char v = (false as IConvertible).ToChar(null);
            TestLibrary.TestFramework.LogError("102.1",
                String.Format("expected a InvalidCastException on (false as IConvertible).ToChar(null)) but got {0}", v));
            retVal = false;
        }
        catch (InvalidCastException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        IConvertibleToChar test = new IConvertibleToChar();

        TestLibrary.TestFramework.BeginTestCase("IConvertibleToChar");

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
