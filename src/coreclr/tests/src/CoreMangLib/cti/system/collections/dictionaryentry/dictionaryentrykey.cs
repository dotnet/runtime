// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class DictionaryEntryKey
{
    private const int c_MIN_STRING_LEN = 64;
    private const int c_MAX_STRING_LEN = 2048;

    public static int Main()
    {
        DictionaryEntryKey dek = new DictionaryEntryKey();

        TestLibrary.TestFramework.BeginTestCase("DictionaryEntryKey");

        if (dek.RunTests())
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
	int             key;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Get and set integral key");

        try
        {
            de = new DictionaryEntry(new object(), new object());

            key    = TestLibrary.Generator.GetInt32(-55);
            de.Key = key;
            if (key != (int)de.Key)
            {
                TestLibrary.TestFramework.LogError("001", "Failed to retrieve an integral value from Key: Expected(" + key + ") != Actual(" + (int)de.Key + ")");
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
	string          key;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Get and set string key");

        try
        {
            de = new DictionaryEntry(new object(), new object());

            key = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            de.Key = key;
            if (!key.Equals(de.Key))
            {
                TestLibrary.TestFramework.LogError("003", "Failed to retrieve an integral value from Key: Expected(" + key + ") != Actual(" + (string)de.Key + ")");
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

        TestLibrary.TestFramework.BeginScenario("PosTest5: Get and set a null key");

        try
        {
            de = new DictionaryEntry(new object(), new object());

            de.Key = null;
            if (null != de.Key)
            {
                TestLibrary.TestFramework.LogError("005", "Failed to retrieve an integral value from value: Expected(null) != Actual(" + de.Key +")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

}
