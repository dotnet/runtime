// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

/// <summary>
/// ReadOnly(System.Globalization.DateTimeFormatInfo)
/// </summary>
public class DateTimeFormatInfoReadOnly
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ReadOnly on a writable DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = new DateTimeFormatInfo();
            DateTimeFormatInfo actual = DateTimeFormatInfo.ReadOnly(info);

            if (!actual.IsReadOnly)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling ReadOnly on a writable DateTimeFormatInfo instance does not make the instance read only");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ReadOnly on a read only DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.InvariantInfo;
            DateTimeFormatInfo actual = DateTimeFormatInfo.ReadOnly(info);

            if (!actual.IsReadOnly)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling ReadOnly on a read only DateTimeFormatInfo instance does not make the instance read only");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when dtfi is a null reference");

        try
        {
            DateTimeFormatInfo.ReadOnly(null);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when dtfi is a null reference");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoReadOnly test = new DateTimeFormatInfoReadOnly();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoReadOnly");

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
