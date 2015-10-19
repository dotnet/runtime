using System;
using System.Globalization;
/// <summary>
/// System.DateTime.SpecifyKind(DateTime,DateTimeKind)
/// </summary>
public class DateTimeSpecifyKind
{
    public static int Main()
    {
        DateTimeSpecifyKind dtsk = new DateTimeSpecifyKind();
        TestLibrary.TestFramework.BeginTestCase("DataTimeSpecifyKind");
        if (dtsk.RunTests())
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

        if ((TimeZoneInfo.Local.StandardName != "Pacific Standard Time") &&
            (TimeZoneInfo.Local.StandardName != "PST")) // Mac
        {
            Console.WriteLine("Not running test because machine is not in Pacific time zone");
            return retVal;
        }

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Local;
        TestLibrary.TestFramework.BeginScenario("PosTest1:datetime is the minValue and the kind is Local");
        try
        {
            myTime = DateTime.MinValue;
            DateTime localTime = myTime.ToLocalTime();
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(localTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
            if (resultTime2 != myTime.AddHours(8) || resultTime1 != myTime)
            {
                TestLibrary.TestFramework.LogError("001", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
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
    public bool PosTest2()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Unspecified;
        TestLibrary.TestFramework.BeginScenario("PosTest2:datetime is the minValue and the kind is Unspecified");
        try
        {
            myTime = DateTime.MinValue;
            DateTime localTime = myTime.ToLocalTime();
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(localTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
            if (resultTime2 != myTime.AddHours(8) || resultTime1 != myTime)
            {
                TestLibrary.TestFramework.LogError("003", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Utc;
        TestLibrary.TestFramework.BeginScenario("PosTest3:datetime is the minValue and the kind is Utc");
        try
        {
            myTime = DateTime.MinValue;
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime LocalTime = myTime.ToLocalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(LocalTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
            if (resultTime2 != myTime.AddHours(8)|| resultTime1 != myTime)
            {
                TestLibrary.TestFramework.LogError("005", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Local;
        TestLibrary.TestFramework.BeginScenario("PosTest4:datetime is the maxValue and the kind is Local");
        try
        {
            myTime = DateTime.MaxValue;
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime LocalTime = myTime.ToLocalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(LocalTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
            if (resultTime2 != myTime || resultTime1 != myTime.AddHours(-8))
            {
                TestLibrary.TestFramework.LogError("007", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest5()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Unspecified;
        TestLibrary.TestFramework.BeginScenario("PosTest5:datetime is the maxValue and the kind is Unspecified");
        try
        {
            myTime = DateTime.MaxValue;
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime LocalTime = myTime.ToLocalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(LocalTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
            if (resultTime2 != myTime || resultTime1!= myTime.AddHours(-8))
            {
                TestLibrary.TestFramework.LogError("009", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest6()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Utc;
        TestLibrary.TestFramework.BeginScenario("PosTest6:datetime is the maxValue and the kind is Utc");
        try
        {
            myTime = DateTime.MaxValue;
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime LocalTime = myTime.ToLocalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(LocalTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
            if (resultTime2 != myTime || resultTime1 != myTime.AddHours(-8))
            {
                TestLibrary.TestFramework.LogError("011", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest7()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Local;
        TestLibrary.TestFramework.BeginScenario("PosTest7:datetime is Random and the kind is local");
        try
        {
            myTime = DateTime.Now;
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime LocalTime = myTime.ToLocalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(LocalTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
	    int offset = (DateTime.Now.IsDaylightSavingTime()?7:8);
            if (resultTime1 != myTime || resultTime2 != myTime.AddHours(offset))
            {
                TestLibrary.TestFramework.LogError("013", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest8()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Utc;
        TestLibrary.TestFramework.BeginScenario("PosTest8:datetime is Random and the kind is Utc");
        try
        {
            myTime = DateTime.Now;
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime LocalTime = myTime.ToLocalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(LocalTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
	    int offset = (DateTime.Now.IsDaylightSavingTime()?7:8);
            if (resultTime1 != myTime || resultTime2 != myTime.AddHours(offset))
            {
                TestLibrary.TestFramework.LogError("015", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest9()
    {
        bool retVal = true;
        DateTime myTime;
        DateTimeKind kind = DateTimeKind.Unspecified;
        TestLibrary.TestFramework.BeginScenario("PosTest9:datetime is Random and the kind is Uspecified");
        try
        {
            myTime = DateTime.Now;
            DateTime UniverTime = myTime.ToUniversalTime();
            DateTime LocalTime = myTime.ToLocalTime();
            DateTime resultTime1 = DateTime.SpecifyKind(LocalTime, kind);
            DateTime resultTime2 = DateTime.SpecifyKind(UniverTime, kind);
	    int offset = (DateTime.Now.IsDaylightSavingTime()?-7:-8);
            if (resultTime2.AddHours(offset) != myTime || resultTime1 != myTime)
            {
                TestLibrary.TestFramework.LogError("017", "The ActualResult is not the ExpectResult");
                TestLibrary.TestFramework.LogInformation("result1: " + resultTime1.ToString() + " result2: " + resultTime2.ToString() + " myTime: " + myTime.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
