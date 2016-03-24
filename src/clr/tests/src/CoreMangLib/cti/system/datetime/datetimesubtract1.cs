// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
﻿using System;
using System.Globalization;
/// <summary>
/// DateTime.Subtract(DateTime)
/// </summary>
public class DateTimeSubtract1
{
    public static int Main()
    {
        DateTimeSubtract1 dtsub1 = new DateTimeSubtract1();
        TestLibrary.TestFramework.BeginTestCase("DataTimeSubtract1");
        if (dtsub1.RunTests())
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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        //TestLibrary.TestFramework.LogInformation("[NegTest]");
        //retVal = NegTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TimeSpan resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest1: The TimeSpan is in the range of MinValue and MaxValue 1");
        try
        {
            DateTime date1 = new DateTime(1,1,1);
            DateTime date2 = DateTime.MinValue.AddYears(1).AddMonths(1).AddDays(1);
            resultTime = date2.Subtract(date1);
            if (resultTime.Days != 397)
            {
                TestLibrary.TestFramework.LogError("001", "Expected: 397 days, actual: "+resultTime.Days);
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
        TimeSpan resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest2: The TimeSpan is in the range of MinValue and MaxValue 2");
        try
        {
            DateTime date1 = new DateTime(1999, 1, 1).ToLocalTime();
            DateTime date2 = new DateTime(2000, 1, 1).ToLocalTime();
            resultTime = date2.Subtract(date1);
            if (resultTime.Days != 365)
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
    public bool PosTest3()
    {
        bool retVal = true;
        TimeSpan resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest3: The TimeSpan is in the range of MinValue and MaxValue 3");
        try
        {
            DateTime date1 = new DateTime(2000, 1, 1).ToLocalTime();
            DateTime date2 = new DateTime(2001, 1, 1).ToLocalTime();
            resultTime = date2.Subtract(date1);
            if (resultTime.Days != 366)
            {
                TestLibrary.TestFramework.LogError("005", "The ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TimeSpan resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest4: The TimeSpan is in the range of MinValue and MaxValue 4");
        try
        {
            DateTime date1 = new DateTime(2001, 1, 1).ToLocalTime();
            DateTime date2 = new DateTime(2000, 1, 1).ToLocalTime();
            resultTime = date2.Subtract(date1);
            if (resultTime.Days != -366)
            {
                TestLibrary.TestFramework.LogError("007", "The ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        TimeSpan resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest5: The TimeSpan is in the range of MinValue and MaxValue 5");
        try
        {
            DateTime date1 = new DateTime(2000, 1, 1).ToLocalTime();
            DateTime date2 = new DateTime(1999, 1, 1).ToLocalTime();
            resultTime = date2.Subtract(date1);
            if (resultTime.Days != -365)
            {
                TestLibrary.TestFramework.LogError("009", "The ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        TimeSpan resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest6: The TimeSpan is in the range of MinValue and MaxValue 6");
        try
        {
            DateTime date1 = new DateTime(2000, 1, 1).ToLocalTime();
            DateTime date2 = new DateTime(2000, 1, 1).ToLocalTime();
            resultTime = date2.Subtract(date1);
            if (resultTime.Days != 0)
            {
                TestLibrary.TestFramework.LogError("011", "The ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    #region NegativeTest
    //public bool NegTest1()
    //{
    //    bool retVal = true;
    //    TimeSpan resultTime;
    //    TestLibrary.TestFramework.BeginScenario("NegTest1: The TimeSpan is less than DateTime MinValue");
    //    try
    //    {
    //        DateTime date1 = DateTime.MinValue.ToLocalTime();
    //        DateTime date2 = DateTime.MaxValue.ToLocalTime();
    //        resultTime = date1.Subtract(date2);
    //        retVal = false;
    //        TestLibrary.TestFramework.LogError("N001", "the TimeSpan is less DateTime MinValue but not throw exception");
    //    }
    //    catch (ArgumentOutOfRangeException) { }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
    //        retVal = false;
    //    }
    //    return retVal;
    //}
    #endregion
}
