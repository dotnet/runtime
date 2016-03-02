// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;

/// <summary>
/// System.DateTime.ToString(System.String)
/// </summary>
public class DateTimeToString3
{
    public static int Main(string[] args)
    {
        DateTimeToString3 myDateTime = new DateTimeToString3();
        TestLibrary.TestFramework.BeginTestCase("Testing System.DateTime.ToString(System.String)...");

        if (myDateTime.RunTests())
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
        retVal = PosTest7() && retVal;
        retVal = PosTest8() && retVal;
        retVal = PosTest9() && retVal;
        retVal = PosTest10() && retVal;
        retVal = PosTest11() && retVal;
        retVal = PosTest12() && retVal;
        retVal = PosTest13() && retVal;
        retVal = PosTest14() && retVal;
        retVal = PosTest15() && retVal;
        retVal = PosTest16() && retVal;
        retVal = PosTest17() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as M/d/yyyy hh:mm:ss tt...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"M/d/yyyy hh:mm:ss tt";
            DateTime myDateTime = new DateTime(1978, 08, 29);
            string dateString = myDateTime.ToString(format);
            char[] splitors = { '/', ' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 7)
            {
                TestLibrary.TestFramework.LogError("001", "The component parts are not correct!");
                retVal = false;
            }
            else
            {
                if (parts[0] != "1978" && parts[1] != "08" && parts[2] != "29" && parts[3] != "00"
                    && parts[4] != "00" && parts[5] != "00" && parts[6]!="AM")
                {
                    TestLibrary.TestFramework.LogError("002", "The content is not correct!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as M-d-yyyy hh:mm:ss tt...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"M-d-yyyy hh:mm:ss tt";
            DateTime myDateTime = new DateTime(1978, 08, 29);
            string dateString = myDateTime.ToString(format);
            char[] splitors = { '-', ' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 7)
            {
                TestLibrary.TestFramework.LogError("004", "The component parts are not correct!");
                retVal = false;
            }
            else
            {
                if (parts[0] != "1978" && parts[1] != "08" && parts[2] != "29" && parts[3] != "00"
                    && parts[4] != "00" && parts[5] != "00" && parts[6]!="AM")
                {
                    TestLibrary.TestFramework.LogError("005", "The content is not correct!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as M-d-yyyy hh:mm:ss...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"M-d-yyyy hh:mm:ss";
            DateTime myDateTime = new DateTime(1978, 08, 29);
            string dateString = myDateTime.ToString(format);
            char[] splitors = { '-', ' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 6)
            {
                TestLibrary.TestFramework.LogError("007", "The component parts are not correct!");
                retVal = false;
            }
            else
            {
                if (parts[0] != "1978" && parts[1] != "08" && parts[2] != "29" && parts[3] != "00"
                    && parts[4] != "00" && parts[5] != "00")
                {
                    TestLibrary.TestFramework.LogError("008", "The content is not correct!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as d...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"d";
            DateTime myDateTime = new DateTime(1978, 08, 29);
            string dateString = myDateTime.ToString(format);
            char[] splitors = { '/'};
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 3)
            {
                TestLibrary.TestFramework.LogError("010", "The component parts are not correct!");
                retVal = false;
            }
            else
            {
                if (parts[0] != "29" && parts[1] != "08" && parts[2] != "1978")
                {
                    TestLibrary.TestFramework.LogError("011", "The content is not correct!");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as D...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"D";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { ' ' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 4)
            {
                TestLibrary.TestFramework.LogError("013", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as f...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"f";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { ' ',':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 6)
            {
                TestLibrary.TestFramework.LogError("015", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as F...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"F";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { ' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 7)
            {
                TestLibrary.TestFramework.LogError("017", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest8() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as g...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"g";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { '/',' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 5)
            {
                TestLibrary.TestFramework.LogError("019", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest9() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as G...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"G";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { '/', ' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 6)
            {
                TestLibrary.TestFramework.LogError("021", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("022", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest10() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as m...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"m";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = {' '};
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 2)
            {
                TestLibrary.TestFramework.LogError("023", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("024", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest11() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as r...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"r";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { ',',' ',':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 9)
            {
                TestLibrary.TestFramework.LogError("025", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("026", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest12()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as s...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"s";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { '-', 'T', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 6)
            {
                TestLibrary.TestFramework.LogError("027", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("028", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest13()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as t...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"t";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = {':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 2)
            {
                TestLibrary.TestFramework.LogError("029", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("030", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest14()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as T...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"T";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 3)
            {
                TestLibrary.TestFramework.LogError("031", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("032", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest15() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as u...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"u";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = {'-',' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 6)
            {
                TestLibrary.TestFramework.LogError("033", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("034", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest16() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as U...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"U";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = {' ', ':' };
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 7)
            {
                TestLibrary.TestFramework.LogError("035", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("036", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest17() 
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing DateTime.ToString(System.String) using format as y...");

        try
        {
            TestLibrary.Utilities.CurrentCulture = new CultureInfo("");
            string format = @"y";
            DateTime myDateTime = DateTime.Now;
            string dateString = myDateTime.ToString(format);
            char[] splitors = { ' '};
            string[] parts = dateString.Split(splitors);

            if (parts.Length != 2)
            {
                TestLibrary.TestFramework.LogError("037", "The component parts are not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("038", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }


}
