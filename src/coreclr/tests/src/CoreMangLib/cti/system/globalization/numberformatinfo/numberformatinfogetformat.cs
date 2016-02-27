// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// GetFormat(System.Type)
/// </summary>

public class NumberFormatInfoGetFormat
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        
        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method GetFormat when arg is a type of NumberFormatInfo .");

        try
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Type formatType = typeof(NumberFormatInfo);
            object obj = nfi.GetFormat(formatType);

            bool testVerify = obj is NumberFormatInfo;

            if (testVerify != true)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method GetFormat .");
                retVal = false;
            }
    

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method GetFormat when arg is not a type of NumberFormatInfo .");

        try
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            Type formatType = typeof(object);

            if (nfi.GetFormat(formatType) != null)
            {
                TestLibrary.TestFramework.LogError("002.1", "Failed to instance a NumberFormatInfo type .");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    //public bool NegTest1()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion
    #endregion

    public static int Main()
    {
        NumberFormatInfoGetFormat test = new NumberFormatInfoGetFormat();

        TestLibrary.TestFramework.BeginTestCase("NumberFormatInfoGetFormat");

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
