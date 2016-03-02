// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Threading;


/// <summary>
/// System.DateTime.Ticks
/// </summary>
public class DateTimeTicks
{
    public static int Main(string[] args)
    {
        DateTimeTicks ticks = new DateTimeTicks();
        TestLibrary.TestFramework.BeginTestCase("Testing System.DateTime.Ticks property...");

        if (ticks.RunTests())
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
        //retVal = PosTest3() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify ticks when DateTime instance is midnight, January 1, 0001...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new System.Globalization.CultureInfo("");
            DateTime myDateTime = new DateTime(0001,01,01,00,00,00);
            long ticks = myDateTime.Ticks;

            if (ticks != 0)
            {
                TestLibrary.TestFramework.LogError("001", "The ticks is not zero when the DateTime instance is midnight, January 1, 0001...");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify ticks when DateTime instance is larger than midnight, January 1, 0001");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new System.Globalization.CultureInfo("");
            DateTime myDateTime = new DateTime(1978,08,29,03,29,22);
            long ticks = myDateTime.Ticks;

            if (ticks <= 0)
            {
                TestLibrary.TestFramework.LogError("003","The ticks is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}

