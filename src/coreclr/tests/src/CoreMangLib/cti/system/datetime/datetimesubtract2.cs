// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// DateTime.Subtract(TimeSpan)
/// </summary>
public class DateTimeSubtract2
{
    public static int Main()
    {
        DateTimeSubtract2 dtsub2 = new DateTimeSubtract2();
        TestLibrary.TestFramework.BeginTestCase("DataTimeSubtract2");
        if (dtsub2.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[NegTest]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        DateTime resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest1: The return dateTime is in the range of MinValue and MaxValue 1");
        try
        {
            DateTime date1 = new DateTime(2000, 1, 1).ToLocalTime();
            TimeSpan timeSpan = new TimeSpan(365, 0, 0, 0);
            resultTime = date1.Subtract(timeSpan);
            if (resultTime!= new DateTime(1999,1,1).ToLocalTime())
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
        DateTime resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest2: The return dateTime is in the range of MinValue and MaxValue 2");
        try
        {
            DateTime date1 = new DateTime(1999, 1, 1).ToLocalTime();
            TimeSpan timeSpan = new TimeSpan(-365, 0, 0, 0);
            resultTime = date1.Subtract(timeSpan);
            if (resultTime != new DateTime(2000, 1, 1).ToLocalTime())
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
        DateTime resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest3: The return dateTime is in the range of MinValue and MaxValue 3");
        try
        {
            DateTime date1 = new DateTime(this.GetInt32(1,9999),this.GetInt32(1,12),this.GetInt32(1,28)).ToLocalTime();
            TimeSpan timeSpan = new TimeSpan(0, 0, 0, 0);
            resultTime = date1.Subtract(timeSpan);
            if (resultTime != date1)
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
        DateTime resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest4: The return dateTime is in the range of MinValue and MaxValue 4");
        try
        {
            DateTime date1 = new DateTime(2001, 1, 1).ToLocalTime();
            TimeSpan timeSpan = new TimeSpan(366, 0, 0, 0);
            resultTime = date1.Subtract(timeSpan);
            if (resultTime != new DateTime(2000, 1, 1).ToLocalTime())
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
        DateTime resultTime;
        TestLibrary.TestFramework.BeginScenario("PosTest5: The return dateTime is in the range of MinValue and MaxValue 5");
        try
        {
            DateTime date1 = new DateTime(2000, 1, 1).ToLocalTime();
            TimeSpan timeSpan = new TimeSpan(-366, 0, 0, 0);
            resultTime = date1.Subtract(timeSpan);
            if (resultTime != new DateTime(2001, 1, 1).ToLocalTime())
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
    #endregion
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        DateTime resultTime;
        TestLibrary.TestFramework.BeginScenario("NegTest1: The return dateTime is out the range of MinValue and MaxValue 1");
        try
        {
            DateTime date1 = DateTime.MinValue.ToLocalTime();
            TimeSpan timeSpan = new TimeSpan(365, 0, 0, 0);
            resultTime = date1.Subtract(timeSpan);
            TestLibrary.TestFramework.LogError("N001", "The return datetime is less than MinValue but not throw exception");
            retVal = false;
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
        DateTime resultTime;
        TestLibrary.TestFramework.BeginScenario("NegTest2: The return dateTime is out the range of MinValue and MaxValue 1");
        try
        {
            DateTime date1 = DateTime.MaxValue.ToLocalTime();
            TimeSpan timeSpan = new TimeSpan(-365, 0, 0, 0);
            resultTime = date1.Subtract(timeSpan);
            retVal = false;
            TestLibrary.TestFramework.LogError("N003", "The return datetime is greater than MaxValue but not throw exception");
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
    #region HelpMethod
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
    #endregion
}
