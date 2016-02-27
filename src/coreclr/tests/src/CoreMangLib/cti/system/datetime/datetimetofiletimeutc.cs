// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// System.DateTime.ToFileTimeUtc()
/// </summary>
public class DateTimeToFileTimeUtc
{
    public static int Main()
    {
        DateTimeToFileTimeUtc dttftu = new DateTimeToFileTimeUtc();
        TestLibrary.TestFramework.BeginTestCase("DataTimeToFileTimeUtc");
        if (dttftu.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[PosTest]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        TestLibrary.TestFramework.LogInformation("[NegTest]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: The current dateTime equals 1601/1/1");
        try
        {
            DateTime date1 = new DateTime(1601, 1, 1).ToLocalTime().ToUniversalTime();
            long result = date1.ToFileTime();
            if (result != 0)
            {
                TestLibrary.TestFramework.LogError("001", "The ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: The current dateTime lager 1601/1/1");
        try
        {
            DateTime date1 = new DateTime(1999, 1, 1).ToLocalTime().ToUniversalTime();
            DateTime date2 = new DateTime(1601, 1, 1).ToLocalTime().ToUniversalTime();
            TimeSpan timeSpan = date1.Subtract(date2);
            long result = date1.ToFileTime();
            long expect = timeSpan.Days * 864000000000; //8640000000 = 24*3600*1000*1000*1000/100
            if (result != expect)
            {
                TestLibrary.TestFramework.LogError("003", "The ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: The current dateTime less than 1601/1/1 1");
        try
        {
            DateTime date1 = DateTime.MinValue.ToLocalTime().ToUniversalTime();
            long result = date1.ToFileTime();
            retVal = false;
            TestLibrary.TestFramework.LogError("N001", "The current dateTime is less than 1601/1/1 but not throw exception");
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest2: The current dateTime less than 1601/1/1 2");
        try
        {
            DateTime date1 = new DateTime(1600, 1, 1).ToLocalTime().ToUniversalTime();
            long result = date1.ToFileTime();
            retVal = false;
            TestLibrary.TestFramework.LogError("N003", "The current dateTime is less than 1601/1/1 but not throw exception");
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}