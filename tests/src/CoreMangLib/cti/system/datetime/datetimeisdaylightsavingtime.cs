using System;
using System.Runtime.InteropServices;
using System.Reflection;

[StructLayout(LayoutKind.Explicit)]
public struct SYSTEMTIME
{
    [FieldOffset(0)]
    public ushort wYear;
    [FieldOffset(2)]
    public ushort wMonth;
    [FieldOffset(4)]
    public ushort wDayOfWeek;
    [FieldOffset(6)]
    public ushort wDay;
    [FieldOffset(8)]
    public ushort wHour;
    [FieldOffset(10)]
    public ushort wMinute;
    [FieldOffset(12)]
    public ushort wSecond;
    [FieldOffset(14)]
    public ushort wMilliseconds;
}

[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
public struct TIME_ZONE_INFORMATION
{

    public int Bias;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]
    public string StandardName;
    public SYSTEMTIME StandardDate;
    public int StandardBias;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string DaylightName;
    public SYSTEMTIME DaylightDate;
    public int DaylightBias;
}

/// <summary>
/// IsDaylightSavingTime
/// </summary>
public class DateTimeIsDaylightSavingTime
{
    #region Private Fields
    private const uint TIME_ZONE_ID_INVALID = 0xFFFFFFFF;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal; ;
        retVal = PosTest2() && retVal; ;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: IsDaylightSavingTime should return correct value if corrent time is daylight time and current timezone supports daylight time when Kind is not Utc");

        try
        {
            DateTime t = new DateTime(2006, 9, 25, 3, 21, 0, 0, DateTimeKind.Unspecified);

            if (!VerificationHelper(t))
            {
                TestLibrary.TestFramework.LogError("001.1", "IsDaylightSavingTime returns wrong value if corrent time is daylight time and current timezone supports daylight time when Kind is not Utc");
                retVal = false;
            }

            t = new DateTime(2006, 9, 25, 3, 21, 0, 0, DateTimeKind.Local);

            if (!VerificationHelper(t))
            {
                TestLibrary.TestFramework.LogError("001.2", "IsDaylightSavingTime returns wrong value if corrent time is daylight time and current timezone supports daylight time when Kind is not Utc");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: IsDaylightSavingTime should return false if corrent time is daylight time and current timezone supports daylight time when Kind is Utc");

        try
        {
            DateTime t = new DateTime(2006, 9, 25, 3, 21, 0, 0, DateTimeKind.Utc);

            if (t.IsDaylightSavingTime())
            {
                TestLibrary.TestFramework.LogError("002.1", "IsDaylightSavingTime returns true if corrent time is daylight time and current timezone supports daylight time when Kind is Utc");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: IsDaylightSavingTime should return false if corrent time is daylight time and current timezone supports daylight time when Kind is not Utc when datetime is supposed not in daylight range");

        try
        {
            DateTime t = new DateTime(2006, 1, 25, 3, 21, 0, 0, DateTimeKind.Unspecified);

            if (!VerificationHelper(t))
            {
                TestLibrary.TestFramework.LogError("003.1", "IsDaylightSavingTime returns wrong value if corrent time is daylight time and current timezone supports daylight time when Kind is not Utc");
                retVal = false;
            }

            t = new DateTime(2006, 1, 25, 3, 21, 0, 0, DateTimeKind.Local);

            if (!VerificationHelper(t))
            {
                TestLibrary.TestFramework.LogError("003.2", "IsDaylightSavingTime returns wrong value if corrent time is daylight time and current timezone supports daylight time when Kind is not Utc");
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
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeIsDaylightSavingTime test = new DateTimeIsDaylightSavingTime();

        TestLibrary.TestFramework.BeginTestCase("DateTimeIsDaylightSavingTime");

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
    [DllImport("kernel32.dll")]
    private extern static uint GetTimeZoneInformation(ref TIME_ZONE_INFORMATION lpTimeZoneInformation);

    [DllImport("kernel32.dll")]
    private extern static int GetLastError();

    [System.Security.SecuritySafeCritical]
    private bool MacVerificationHelper(DateTime t)
    {
        IntPtr cfTimeZoneRef = CFDateTimeTools.CFTimeZoneCopySystem();
        bool osResult = CFDateTimeTools.CFTimeZoneIsDaylightSavingTime(cfTimeZoneRef, CFDateTimeTools.DateTimeToCFAbsoluteTime(t));
        bool clrResult = t.IsDaylightSavingTime();
        return osResult == clrResult;
    }

    [System.Security.SecuritySafeCritical]
    private bool VerificationHelper(DateTime t)
    {
	if (!TestLibrary.Utilities.IsWindows)
		return MacVerificationHelper(t);

        // else...
   

        TIME_ZONE_INFORMATION lpTimeZoneInformation = new TIME_ZONE_INFORMATION();

        uint callVal = GetTimeZoneInformation(ref lpTimeZoneInformation);
        if (TIME_ZONE_ID_INVALID == callVal)
        {
            throw new Exception("WINAPI set error = " + GetLastError());
        }

        bool expected = false;

        if (lpTimeZoneInformation.DaylightBias != 0)
        {
            // This timezone have Daylight setting set
            int daylightMonth = lpTimeZoneInformation.DaylightDate.wMonth;
            int standardMonth = lpTimeZoneInformation.StandardDate.wMonth;

            // Determine t.month is in day light time range
            // In this case, daylightMonth is 4, standardMonth is 10
            // so May is considered in day light time range, whereas Feruary is not
            if ((daylightMonth < standardMonth) &&
                ((t.Month - daylightMonth) > 0) &&
                ((t.Month - standardMonth) < 0))
            {
                expected = true;
            }

            // Determine t.month is in day light time range
            // In this case, daylightMonth is 10, standardMonth is 3
            // so May is not considered in day light time range, whereas Feruary is
            if ((daylightMonth > standardMonth) &&
                (((t.Month - daylightMonth) > 0) ^
                 ((t.Month - standardMonth) > 0) == false))
            {
                expected = true;
            }

            if (t.Month == lpTimeZoneInformation.DaylightDate.wMonth)
            {
                if (t.Day > lpTimeZoneInformation.DaylightDate.wDay)
                {
                    expected = true;
                }
            }

            if (t.Month == lpTimeZoneInformation.StandardDate.wMonth)
            {
                if (t.Day < lpTimeZoneInformation.StandardDate.wDay)
                {
                    expected = true;
                }
            }
        }

        bool actual = t.IsDaylightSavingTime();

        return expected == actual;
    }
    #endregion
}
