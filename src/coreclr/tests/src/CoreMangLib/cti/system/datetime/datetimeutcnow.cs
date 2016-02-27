// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Threading;

public class DateTimeUtcNow
{
    public static int Main(string[] args)
    {
        DateTimeUtcNow utcNow = new DateTimeUtcNow();
        TestLibrary.TestFramework.BeginTestCase("Testing System.DateTime.UtcNow property...");

        if (utcNow.RunTests())
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
        TestLibrary.TestFramework.BeginTestCase("Verify DateTime.UtcNow is Universal kind...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new System.Globalization.CultureInfo("");
            DateTime myDateTime = DateTime.UtcNow;
            Type typeOfNow = myDateTime.GetType();
            DateTime toUniversal = myDateTime.ToUniversalTime();

            if (myDateTime != toUniversal)
            {
                TestLibrary.TestFramework.LogError("001", "The kind of UtcNow property is not local!");
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
        TestLibrary.TestFramework.BeginTestCase("Verify the type of UtcNow property is DateTime...");

        try
        {
            DateTime myDateTime = DateTime.UtcNow;
            Type typeOfNow = myDateTime.GetType();

            if (typeOfNow.ToString() != "System.DateTime")
            {
                TestLibrary.TestFramework.LogError("003", "The type of Now property is wrong!");
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
}
