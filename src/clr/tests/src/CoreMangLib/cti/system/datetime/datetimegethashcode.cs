// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// GetHashCode
/// </summary>
public class DateTimeGetHashCode
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call GetHashCode on a valid instance");

        try
        {
            DateTime t = new DateTime(2006, 9, 25, 14, 15, 59, 999);
            int hashCode1 = t.GetHashCode();
            int hashCode2 = t.GetHashCode();
            if (hashCode1 != hashCode2)
            {
                TestLibrary.TestFramework.LogError("001.1", "Call GetHashCode on a valid instance twice does not return the same hash code");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] hashCode1 = " + hashCode1 + ", hashCode2 = " + hashCode2);
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call GetHashCode on a boundary value");

        try
        {
            DateTime t = DateTime.MaxValue;
            int hashCode1 = t.GetHashCode();
            int hashCode2 = t.GetHashCode();
            if (hashCode1 != hashCode2)
            {
                TestLibrary.TestFramework.LogError("002.1", "Call GetHashCode on a valid instance twice does not return the same hash code");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] hashCode1 = " + hashCode1 + ", hashCode2 = " + hashCode2);
                retVal = false;
            }

            t = DateTime.MinValue;
            hashCode1 = t.GetHashCode();
            hashCode2 = t.GetHashCode();
            if (hashCode1 != hashCode2)
            {
                TestLibrary.TestFramework.LogError("002.2", "Call GetHashCode on a valid instance twice does not return the same hash code");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] hashCode1 = " + hashCode1 + ", hashCode2 = " + hashCode2);
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Hash code should be the same for the same date time");

        try
        {
            DateTime t1 = new DateTime(2006, 9, 25, 14, 15, 59, 999);
            DateTime t2 = new DateTime(2006, 9, 25, 14, 15, 59, 999);
            int hashCode1 = t1.GetHashCode();
            int hashCode2 = t2.GetHashCode();
            if (hashCode1 != hashCode2)
            {
                TestLibrary.TestFramework.LogError("003.1", "Call GetHashCode on a valid instance twice does not return the same hash code");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] hashCode1 = " + hashCode1 + ", hashCode2 = " + hashCode2);
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
        DateTimeGetHashCode test = new DateTimeGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("DateTimeGetHashCode");

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
