// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;

public class TestIFormatProviderClass : IFormatProvider
{
    public object GetFormat(Type formatType)
    {
        return this;
    }
}

public class TestIFormatProviderClass2 : IFormatProvider
{
    public object GetFormat(Type formatType)
    {
        return new DateTimeFormatInfo();
    }
}

/// <summary>
/// GetInstance(System.IFormatProvider)
/// </summary>
public class DateTimeFormatInfoGetInstance
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetInstance to get an DateTimeFormatInfo instance when provider is an CultureInfo instance");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(new CultureInfo("en-us"));

            if (info == null)
            {
                TestLibrary.TestFramework.LogError("001.1", "Calling GetInstance to get an DateTimeFormatInfo instance when provider is an CultureInfo instance returns null reference");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call GetInstance to get an DateTimeFormatInfo instance when provider is null reference");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(null);

            if (info != DateTimeFormatInfo.CurrentInfo)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling GetInstance to get an DateTimeFormatInfo instance when provider is null reference does not return CurrentInfo");
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call GetInstance to get an DateTimeFormatInfo instance when provider is a DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(new DateTimeFormatInfo());

            if (info == null)
            {
                TestLibrary.TestFramework.LogError("003.1", "Calling GetInstance to get an DateTimeFormatInfo instance when provider is a DateTimeFormatInfo instance returns null reference");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call GetInstance to get an DateTimeFormatInfo instance when provider.GetFormat method supports a DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(new TestIFormatProviderClass2());

            if (info == null)
            {
                TestLibrary.TestFramework.LogError("004.1", "Calling GetInstance to get an DateTimeFormatInfo instance when provider.GetFormat method supports a DateTimeFormatInfo instance returns null reference");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Call GetInstance to get an DateTimeFormatInfo instance when provider.GetFormat method does not support a DateTimeFormatInfo instance");

        try
        {
            DateTimeFormatInfo info = DateTimeFormatInfo.GetInstance(new TestIFormatProviderClass());

            if (info != DateTimeFormatInfo.CurrentInfo)
            {
                TestLibrary.TestFramework.LogError("005.1", "Calling GetInstance to get an DateTimeFormatInfo instance when provider.GetFormat method does not support a DateTimeFormatInfo instance does not return CurrentInfo");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeFormatInfoGetInstance test = new DateTimeFormatInfoGetInstance();

        TestLibrary.TestFramework.BeginTestCase("DateTimeFormatInfoGetInstance");

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
