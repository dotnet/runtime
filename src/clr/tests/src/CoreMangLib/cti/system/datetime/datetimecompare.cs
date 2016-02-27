// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// Compare(System.DateTime,System.DateTime)
/// </summary>
public class DateTimeCompare
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Compare a datetime instance with itself");

        try
        {
            DateTime t1 = new DateTime(2006, 9, 21, 10, 54, 56, 888);

            if (DateTime.Compare(t1, t1) != 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "Compare a datetime instance with itself does not return 0");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Compare a datetime instance with a datetime instance less than it");

        try
        {
            DateTime t1 = new DateTime(2006, 9, 21, 10, 54, 56, 888);
            DateTime t2 = new DateTime(2006, 9, 21, 10, 54, 56, 887);

            if (DateTime.Compare(t1, t2) <= 0)
            {
                TestLibrary.TestFramework.LogError("002.1", "Compare a datetime instance with a datetime instance less than it does not greater than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 9, 21, 10, 54, 55, 888);

            if (DateTime.Compare(t1, t2) <= 0)
            {
                TestLibrary.TestFramework.LogError("002.2", "Compare a datetime instance with a datetime instance less than it does not greater than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 9, 21, 10, 53, 56, 888);

            if (DateTime.Compare(t1, t2) <= 0)
            {
                TestLibrary.TestFramework.LogError("002.3", "Compare a datetime instance with a datetime instance less than it does not greater than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 9, 21, 9, 54, 56, 888);

            if (DateTime.Compare(t1, t2) <= 0)
            {
                TestLibrary.TestFramework.LogError("002.4", "Compare a datetime instance with a datetime instance less than it does not greater than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 9, 20, 10, 54, 56, 888);

            if (DateTime.Compare(t1, t2) <= 0)
            {
                TestLibrary.TestFramework.LogError("002.5", "Compare a datetime instance with a datetime instance less than it does not greater than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 8, 21, 10, 54, 56, 888);

            if (DateTime.Compare(t1, t2) <= 0)
            {
                TestLibrary.TestFramework.LogError("002.6", "Compare a datetime instance with a datetime instance less than it does not greater than 0");
                retVal = false;
            }

            t2 = new DateTime(2005, 9, 21, 10, 54, 56, 888);

            if (DateTime.Compare(t1, t2) <= 0)
            {
                TestLibrary.TestFramework.LogError("002.7", "Compare a datetime instance with a datetime instance less than it does not greater than 0");
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Compare a datetime instance equal to it");

        try
        {
            DateTime t1 = new DateTime(2006, 9, 21, 10, 54, 56, 888);
            DateTime t2 = new DateTime(2006, 9, 21, 10, 54, 56, 888);

            if (DateTime.Compare(t1, t2) != 0)
            {
                TestLibrary.TestFramework.LogError("003.1", "Compare a datetime instance  equal to it does not return 0");
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Compare a datetime instance with a datetime instance greater than it");

        try
        {
            DateTime t1 = new DateTime(2006, 9, 21, 10, 54, 56, 888);
            DateTime t2 = new DateTime(2006, 9, 21, 10, 54, 56, 889);

            if (DateTime.Compare(t1, t2) >= 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "Compare a datetime instance with a datetime instance greater than it does not less than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 9, 21, 10, 54, 57, 888);

            if (DateTime.Compare(t1, t2) >= 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "Compare a datetime instance with a datetime instance greater than it does not less than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 9, 21, 10, 55, 56, 888);

            if (DateTime.Compare(t1, t2) >= 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "Compare a datetime instance with a datetime instance greater than it does not less than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 9, 21, 11, 54, 56, 888);

            if (DateTime.Compare(t1, t2) >= 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "Compare a datetime instance with a datetime instance greater than it does not less than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 9, 22, 10, 54, 56, 888);

            if (DateTime.Compare(t1, t2) >= 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "Compare a datetime instance with a datetime instance greater than it does not less than 0");
                retVal = false;
            }

            t2 = new DateTime(2006, 10, 21, 10, 54, 56, 888);

            if (DateTime.Compare(t1, t2) >= 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "Compare a datetime instance with a datetime instance greater than it does not less than 0");
                retVal = false;
            }

            t2 = new DateTime(2007, 9, 21, 10, 54, 56, 888);

            if (DateTime.Compare(t1, t2) >= 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "Compare a datetime instance with a datetime instance greater than it does not less than 0");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DateTimeCompare test = new DateTimeCompare();

        TestLibrary.TestFramework.BeginTestCase("DateTimeCompare");

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
}
