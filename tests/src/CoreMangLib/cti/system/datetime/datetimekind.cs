// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;


/// <summary>
/// System.DateTime.Kind
/// </summary>
public class DateTimeKind
{
    public static int Main(string[] args)
    {
        DateTimeKind kind = new DateTimeKind();
        TestLibrary.TestFramework.BeginScenario("Testing System.DateTime.Kind property...");

        if (kind.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Check Kind property when create an instance using Utc...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 03, 00, 00, System.DateTimeKind.Utc);

            if (myDateTime.Kind != System.DateTimeKind.Utc)
            {
                TestLibrary.TestFramework.LogError("001", "The kind is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Check Kind property when create an instance using local...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 03, 00, 00, System.DateTimeKind.Local);

            if (myDateTime.Kind != System.DateTimeKind.Local)
            {
                TestLibrary.TestFramework.LogError("003", "The kind is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Check Kind property when create an instance using Unspecified...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 03, 00, 00, System.DateTimeKind.Unspecified);

            if (myDateTime.Kind != System.DateTimeKind.Unspecified)
            {
                TestLibrary.TestFramework.LogError("005", "The kind is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Check toUniversalTime is equal to original when create an instance using Utc...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 03, 00, 00, System.DateTimeKind.Utc);
            DateTime toUniversal = myDateTime.ToUniversalTime();

            if (myDateTime != toUniversal)
            {
                TestLibrary.TestFramework.LogError("007", "The two instances are not equal!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Check toLocalTime is equal to original when create an instance using local...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 03, 00, 00, System.DateTimeKind.Local);
            DateTime toLocal = myDateTime.ToLocalTime();

            if (myDateTime != toLocal)
            {
                TestLibrary.TestFramework.LogError("009", "The kind is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Check an instance created by Unspecified, then compare to local and universal...");

        try
        {
            if (TimeZoneInfo.Local.BaseUtcOffset == TimeSpan.Zero) // any TZ has same alignment with UTC
            {
                // if we are on UTC zone, then the following test wil not make sense because all date conversion will produce the same original date value
                return retVal;
            }

            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 03, 00, 00, System.DateTimeKind.Unspecified);

            DateTime toLocal = myDateTime.ToLocalTime();
            DateTime toUniversal = myDateTime.ToUniversalTime();

            if (myDateTime == toLocal)
            {
                string errorMessage = String.Format("The Unspecified myDateTime is regard as local by default!\nTZ: '{0}'\nmyDateTime: '{1}'\ntoLocal: '{2}'", TimeZoneInfo.Local.DisplayName, myDateTime, toLocal);
                TestLibrary.TestFramework.LogError("011", errorMessage);
                retVal = false;
            }
            else if (myDateTime == toUniversal)
            {
                string errorMessage = String.Format("Unexpected exception occurs!\nTZ: '{0}'\nmyDateTime: '{1}'\ntoUniversal: '{2}'", TimeZoneInfo.Local.DisplayName, myDateTime, toUniversal);
                TestLibrary.TestFramework.LogError("012", errorMessage);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
