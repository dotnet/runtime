// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor(System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32,System.Int32)
/// </summary>
public class DateTimeCtor6
{
    #region Private Fields
    private int m_ErrorNo = 0;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: We can call ctor to constructor a new DateTime instance by using valid value");

        try
        {
            retVal = retVal && VerifyDateTimeHelper(2006, 8, 28, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(2006, 8, 28, 12, 0, 0, 500);
            retVal = retVal && VerifyDateTimeHelper(2006, 8, 28, 12, 56, 56, 900);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: We can call ctor to constructor a new DateTime instance by using MAX/MIN values");

        try
        {
            retVal = retVal && VerifyDateTimeHelper(1, 1, 1, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(1, 1, 1, 23, 59, 59, 999);
            retVal = retVal && VerifyDateTimeHelper(1, 1, 1, 0, 0, 0, 0);
            retVal = retVal && VerifyDateTimeHelper(1, 1, 1, 0, 59, 59, 999);

            retVal = retVal && VerifyDateTimeHelper(9999, 12, 31, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(9999, 12, 31, 23, 59, 59, 999);
            retVal = retVal && VerifyDateTimeHelper(9999, 12, 31, 0, 0, 0, 999);
            retVal = retVal && VerifyDateTimeHelper(9999, 12, 31, 0, 59, 59, 0);

            retVal = retVal && VerifyDateTimeHelper(1, 12, 31, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(1, 12, 31, 23, 59, 59, 999);
            retVal = retVal && VerifyDateTimeHelper(1, 12, 31, 0, 0, 0, 999);
            retVal = retVal && VerifyDateTimeHelper(1, 12, 31, 0, 59, 59, 0);

            retVal = retVal && VerifyDateTimeHelper(9999, 1, 1, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(9999, 1, 1, 23, 59, 59, 999);
            retVal = retVal && VerifyDateTimeHelper(9999, 1, 1, 0, 0, 0, 0);
            retVal = retVal && VerifyDateTimeHelper(9999, 1, 1, 0, 59, 59, 999);

            retVal = retVal && VerifyDateTimeHelper(2000, 1, 31, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(2000, 1, 31, 23, 59, 59, 999);
            retVal = retVal && VerifyDateTimeHelper(2000, 1, 31, 0, 0, 0, 0);
            retVal = retVal && VerifyDateTimeHelper(2000, 1, 31, 0, 59, 59, 999);

            retVal = retVal && VerifyDateTimeHelper(2000, 2, 29, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(2000, 2, 29, 23, 59, 59, 999);
            retVal = retVal && VerifyDateTimeHelper(2000, 2, 29, 0, 0, 0, 999);
            retVal = retVal && VerifyDateTimeHelper(2000, 2, 29, 0, 59, 59, 0);

            retVal = retVal && VerifyDateTimeHelper(2001, 2, 28, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(2001, 2, 28, 23, 59, 59, 999);
            retVal = retVal && VerifyDateTimeHelper(2001, 2, 28, 0, 0, 0, 0);
            retVal = retVal && VerifyDateTimeHelper(2001, 2, 28, 0, 59, 59, 999);

            retVal = retVal && VerifyDateTimeHelper(2001, 4, 30, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(2001, 4, 30, 23, 59, 59, 999);
            retVal = retVal && VerifyDateTimeHelper(2001, 4, 30, 0, 0, 0, 999);
            retVal = retVal && VerifyDateTimeHelper(2001, 4, 30, 0, 59, 59, 0);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: We can call ctor to constructor a new DateTime instance by using correct day/month pair");

        try
        {
            retVal = retVal && VerifyDateTimeHelper(2000, 2, 29, 16, 7, 43, 500);
            retVal = retVal && VerifyDateTimeHelper(2006, 2, 28, 12, 0, 0, 0);
            retVal = retVal && VerifyDateTimeHelper(2006, 4, 30, 16, 7, 43, 100);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when year is less than 1 or greater than 9999.");

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(0, 1, 1, 1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when year is less than 1");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(10000, 1, 1, 1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when year is greater than 9999");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException should be thrown when month is less than 1 or greater than 12");

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 0, 1, 1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when month is less than 1");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 13, 1, 1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when month is greater than 12");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentOutOfRangeException should be thrown when day is less than 1 or greater than the number of days in month");

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 0, 1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when day is less than 1");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 32, 1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when day is greater than the number of days in month");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 2, 29, 1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when day is greater than the number of days in month");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 4, 31, 1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when day is greater than the number of days in month");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentOutOfRangeException should be thrown when hour is less than 0 or greater than 23");

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 1, -1, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when hour is less than 0");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 1, 24, 1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when hour is greater than 23");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: ArgumentOutOfRangeException should be thrown when minute is less than 0 or greater than 59");

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 1, 1, -1, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown minute year is less than 0");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("105.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 1, 1, 60, 1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when minute is greater than 59");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("105.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: ArgumentOutOfRangeException should be thrown when second is less than 0 or greater than 59");

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 1, 1, 1, -1, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when second is less than 0");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 1, 1, 1, 60, 1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when second is greater than 59");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest7: ArgumentOutOfRangeException should be thrown when millisecond is less than 0 or greater than 999");

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 1, 1, 1, 1, -1);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when millisecond is less than 0");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("107.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            m_ErrorNo++;
            DateTime value = new DateTime(2006, 1, 1, 1, 1, 1, 1000);

            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when millisecond is greater than 999");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("107.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeCtor6 test = new DateTimeCtor6();

        TestLibrary.TestFramework.BeginTestCase("DateTimeCtor6");

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

    #region Private Methods
    private bool VerifyDateTimeHelper(int desiredYear,
        int desiredMonth,
        int desiredDay,
        int desiredHour,
        int desiredMinute,
        int desiredSecond,
        int desiredMillisecond)
    {
        bool retVal = true;

        DateTime value = new DateTime(desiredYear, desiredMonth, desiredDay,
            desiredHour, desiredMinute, desiredSecond, desiredMillisecond);

        m_ErrorNo++;
        if ((desiredYear != value.Year) ||
            (desiredMonth != value.Month) ||
            (desiredDay != value.Day) ||
            (desiredHour != value.Hour) ||
            (desiredMinute != value.Minute) ||
            (desiredSecond != value.Second) ||
            (desiredMillisecond != value.Millisecond) )
        {
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Calling ctor constructors a wrong DateTime instance by using valid value");
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredYear = " + desiredYear.ToString() +
                                                                                             ", desiredMonth = " + desiredMonth.ToString() +
                                                                                             ", desiredDay = " + desiredDay.ToString() +
                                                                                             ", desiredHour = " + desiredHour.ToString() +
                                                                                             ", desiredMinute = " + desiredMinute.ToString() +
                                                                                             ", desiredSecond = " + desiredSecond.ToString() +
                                                                                             ", desiredMillisecond = " + desiredMillisecond.ToString() +
                                                                                             ", actualYear = " + value.Year.ToString() +
                                                                                             ", actualMonth = " + value.Month.ToString() +
                                                                                             ", actualDay = " + value.Day.ToString() +
                                                                                             ", actualHour = " + value.Hour.ToString() +
                                                                                             ", actualMinute = " + value.Minute.ToString() +
                                                                                             ", actualSecond = " + value.Second.ToString() +
                                                                                             ", actualMillisecond = " + value.Millisecond.ToString());
            retVal = false;
        }

        m_ErrorNo++;
        if (value.Kind != DateTimeKind.Unspecified)
        {
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Calling ctor constructors a wrong DateTime instance by using valid value");
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredKind = DateTimeKind.Unspecified" + ", actualKind = " + value.Kind.ToString());
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
