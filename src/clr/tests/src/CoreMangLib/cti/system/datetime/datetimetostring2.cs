// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;


/// <summary>
/// System.DateTime.ToString(System.IFormatProvider)
/// </summary>
public class DateTimeToString2
{
    public static int Main(string[] args)
    {
        DateTimeToString2 toString = new DateTimeToString2();
        TestLibrary.TestFramework.BeginTestCase("Testing System.DateTime.ToString(System.IFormatProvider)...");

        if (toString.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.IFormatProvider) when DateTimeFormatInfo is CurrentInfo...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29);
            string dateString = myDateTime.ToString(DateTimeFormatInfo.CurrentInfo);
            char[] splitors = { '/', ' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 6)
            {
                TestLibrary.TestFramework.LogError("001", "The component parts are not correct!");
                retVal = false;
            }
            else
            {
                if (parts[0] != "1978" && parts[1] != "08" && parts[2] != "29" && parts[3] != "00"
                    && parts[4] != "00" && parts[5] != "00")
                {
                    TestLibrary.TestFramework.LogError("002", "The content is not correct!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.IFormatProvider) when when DateTimeFormatInfo is InvariantInfo...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            DateTime myDateTime = new DateTime(1978, 08, 29, 01, 10, 10);
            string dateString = myDateTime.ToString(DateTimeFormatInfo.InvariantInfo);
            char[] splitors = { '/', ' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 6)
            {
                TestLibrary.TestFramework.LogError("001", "The component parts are not correct!");
                retVal = false;
            }
            else
            {
                if (parts[0] != "1978" && parts[1] != "08" && parts[2] != "29" && parts[3] != "01"
                    && parts[4] != "10" && parts[5] != "10")
                {
                    TestLibrary.TestFramework.LogError("002", "The content is not correct!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
