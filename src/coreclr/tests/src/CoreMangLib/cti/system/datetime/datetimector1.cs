// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor(System.Int32,System.Int32,System.Int32)
/// </summary>
public class DateTimeCtor1
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

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: We can call ctor(System.Int32,System.Int32,System.Int32) to constructor a new DateTime instance by using valid value");

        try
        {
            retVal = retVal && VerifyDateTimeHelper(2006, 8, 28);
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: We can call ctor(System.Int32,System.Int32,System.Int32) to constructor a new DateTime instance by using MAX/MIN values");

        try
        {
            retVal = retVal && VerifyDateTimeHelper(1, 1, 1);
            retVal = retVal && VerifyDateTimeHelper(9999, 12, 31);
            retVal = retVal && VerifyDateTimeHelper(1, 12, 31);
            retVal = retVal && VerifyDateTimeHelper(9999, 1, 1);
            retVal = retVal && VerifyDateTimeHelper(2000, 1, 31);
            retVal = retVal && VerifyDateTimeHelper(2000, 2, 29);
            retVal = retVal && VerifyDateTimeHelper(2001, 2, 28);
            retVal = retVal && VerifyDateTimeHelper(2001, 4, 30);
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: We can call ctor(System.Int32,System.Int32,System.Int32) to constructor a new DateTime instance by using correct day/month pair");

        try
        {
            retVal = retVal && VerifyDateTimeHelper(2000, 2, 29);
            retVal = retVal && VerifyDateTimeHelper(2006, 2, 28);
            retVal = retVal && VerifyDateTimeHelper(2006, 4, 30);
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException should be thrown when year is less than 1 or greater than 9999.");

        try
        {
            DateTime value = new DateTime(0, 1, 1);

            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when year is less than 1");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            DateTime value = new DateTime(10000, 1, 1);

            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when year is greater than 9999");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
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
            DateTime value = new DateTime(2006, 0, 1);

            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when month is less than 1");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            DateTime value = new DateTime(2006, 13, 1);

            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when month is greater than 12");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
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
            DateTime value = new DateTime(2006, 1, 0);

            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when day is less than 1");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            DateTime value = new DateTime(2006, 1, 32);

            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when day is greater than the number of days in month");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            DateTime value = new DateTime(2006, 2, 29);

            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when day is greater than the number of days in month");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        try
        {
            DateTime value = new DateTime(2006, 4, 31);

            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "ArgumentOutOfRangeException is not thrown when day is greater than the number of days in month");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            m_ErrorNo++;
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeCtor1 test = new DateTimeCtor1();

        TestLibrary.TestFramework.BeginTestCase("DateTimeCtor1");

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
    private bool VerifyDateTimeHelper(int desiredYear, int desiredMonth, int desiredDay)
    {
        bool retVal = true;

        DateTime value = new DateTime(desiredYear, desiredMonth, desiredDay);
        m_ErrorNo++;
        if ((desiredYear != value.Year) || (desiredMonth != value.Month) || (desiredDay != value.Day))
        {
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "We can call ctor(System.Int32,System.Int32,System.Int32) to constructor a wrong DateTime instance by using valid value");
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredYear = " + desiredYear.ToString() + ", desiredMonth = " + desiredMonth.ToString() + ", desiredDay = " + desiredDay.ToString() + ", actualYear = " + value.Year.ToString() + ", actualMonth = " + value.Month.ToString() + ", actualDay = " + value.Day.ToString());
            retVal = false;
        }

        m_ErrorNo++;
        if ((value.Hour != 0) || (value.Minute != 0) || (value.Second != 0))
        {
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "We can call ctor(System.Int32,System.Int32,System.Int32) to constructor a wrong DateTime instance by using valid value");
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredHour = 0, desiredMinute = 0, desiredSecond = 0, actualHour = " + value.Hour.ToString() + ", actualMinute = " + value.Minute.ToString() + ", actualSecond = " + value.Second.ToString());
            retVal = false;
        }

        m_ErrorNo++;
        if (value.Kind != DateTimeKind.Unspecified)
        {
            TestLibrary.TestFramework.LogError(m_ErrorNo.ToString(), "We can call ctor(System.Int32,System.Int32,System.Int32) to constructor a wrong DateTime instance by using valid value");
            TestLibrary.TestFramework.LogInformation("[LOCAL VARIABLES] desiredKind = DateTimeKind.Unspecified" + ", actualKind = " + value.Kind.ToString());
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
