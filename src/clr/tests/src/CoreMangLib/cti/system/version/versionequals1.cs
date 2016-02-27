// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// Equals(System.Object)
/// </summary>

public class VersionEquals1
{
    public static int Main()
    {
        VersionEquals1 test = new VersionEquals1();

        TestLibrary.TestFramework.BeginTestCase("VersionEquals1");

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

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Ensure Equals(System.Object) return true when the current Version object and obj are both Version objects and every component of the current Version object matches the corresponding component of obj.");

        try
        {
            Version TestVersion1 = new Version("2.3.4.5");
			Object TestObject1 = (Object)TestVersion1; 
			Version TestVersion2 = new Version("2.3.4.5");
            if (!TestVersion2.Equals(TestObject1))
            {
                TestLibrary.TestFramework.LogError("P01.1", "Equals(System.Object) failed when the current Version object and obj are both Version objects and every component of the current Version object matches the corresponding component of obj!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P01.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Ensure Equals(System.Object) return false when the current Version object and obj are both Version objects but not every component of the current Version object matches the corresponding component of obj.");

        try
        {
            Version TestVersion1 = new Version("2.3.4.5");
			Object TestObject1 = (Object)TestVersion1; 
			Version TestVersion2 = new Version("1.2.3.4");
			if (TestVersion2.Equals(TestObject1))
            {
                TestLibrary.TestFramework.LogError("P02.1", "Equals(System.Object) failed when the current Version object and obj are both Version objects but not every component of the current Version object matches the corresponding component of obj!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P02.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Ensure Equals(System.Object) return false when obj is not Version object.");

        try
        {
            String TestString1 = new String(new char[] { '2', '.', '3', '.', '4', '.', '8' });
            Version TestVersion2 = new Version("2.3.4.8");
            if (TestVersion2.Equals(TestString1))
            {
                TestLibrary.TestFramework.LogError("P03.1", "Equals(System.Object) failed when obj is not Version object!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P03.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Ensure Equals(System.Object) return false when obj is a null reference.");

        try
        {
            Version TestVersion2 = new Version("2.3.4.8");
            if (TestVersion2.Equals(null))
            {
                TestLibrary.TestFramework.LogError("P04.1", "Equals(System.Object) failed when obj is a null reference!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("P04.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
}
