// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class DictionaryEntryValue
{
    private const int c_MIN_STRING_LEN = 64;
    private const int c_MAX_STRING_LEN = 2048;

    public static int Main()
    {
        DictionaryEntryValue dev = new DictionaryEntryValue();

        TestLibrary.TestFramework.BeginTestCase("DictionaryEntryValue");

        if (dev.RunTests())
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

        return retVal;
    }

    public bool PosTest1()
    {
        bool            retVal = true;
        DictionaryEntry de;
        int             value;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Get and set integral value");

        try
        {
            de = new DictionaryEntry(new object(), new object());

            value = TestLibrary.Generator.GetInt32(-55);
            de.Value = value;
            if (value != (int)de.Value)
            {
                TestLibrary.TestFramework.LogError("001", "Failed to retrieve an integral value from Value: Expected(" + value + ") != Actual(" + (int)de.Value + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool            retVal = true;
        DictionaryEntry de;
        string          value;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Get and set string value");

        try
        {
            de = new DictionaryEntry(new object(), new object());

            value = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            de.Value = value;
            if (!value.Equals(de.Value))
            {
                TestLibrary.TestFramework.LogError("003", "Failed to retrieve an integral value from value: Expected(" + value + ") != Actual(" + de.Value + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool            retVal = true;
        DictionaryEntry de;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Get and set a null value");

        try
        {
            de = new DictionaryEntry(new object(), new object());

            de.Value = null;
            if (null != de.Value)
            {
                TestLibrary.TestFramework.LogError("005", "Failed to retrieve an integral value from value: Expected(null) != Actual(" + de.Value + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
