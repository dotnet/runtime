// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class DictionaryEntryCtor
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        DictionaryEntryCtor dec = new DictionaryEntryCtor();

        TestLibrary.TestFramework.BeginTestCase("DictionaryEntryCtor");

        if (dec.RunTests())
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
        bool            retVal = true;
        DictionaryEntry de;

        TestLibrary.TestFramework.BeginScenario("PosTest1: null key");

        try
        {
            de = new DictionaryEntry(null, new object());

            if (null != de.Key)
            {
                TestLibrary.TestFramework.LogError("001", "Key is not null as expected: Actual(" + de.Key + ")");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: null value");

        try
        {
            de = new DictionaryEntry(new object(), null);

            if (null != de.Value)
            {
                TestLibrary.TestFramework.LogError("003", "Value is not null as expected: Actual(" + de.Value + ")");
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
        int             key;

        TestLibrary.TestFramework.BeginScenario("PosTest3: boxed integral value as key");

        try
        {
            key = TestLibrary.Generator.GetInt32(-55);
            de  = new DictionaryEntry((object)key, new object());

            if (key != (int)de.Key)
            {
                TestLibrary.TestFramework.LogError("005", "Key is not " + key + " as expected: Actual(" + de.Key + ")");
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

    public bool PosTest4()
    {
        bool            retVal = true;
        DictionaryEntry de;
        string          key;

        TestLibrary.TestFramework.BeginScenario("PosTest4: boxed string value as key");

        try
        {
            key = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            de  = new DictionaryEntry(key, new object());

            if (!key.Equals(de.Key))
            {
                TestLibrary.TestFramework.LogError("007", "Key is not " + key + " as expected: Actual(" + de.Key + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool            retVal = true;
        DictionaryEntry de;
        int             value;

        TestLibrary.TestFramework.BeginScenario("PosTest5: boxed integral value as value");

        try
        {
            value = TestLibrary.Generator.GetInt32(-55);
            de  = new DictionaryEntry(new object(), (object)value);

            if (value != (int)de.Value)
            {
                TestLibrary.TestFramework.LogError("009", "Value is not " + value + " as expected: Actual(" + de.Value + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool            retVal = true;
        DictionaryEntry de;
        string          value;

        TestLibrary.TestFramework.BeginScenario("PosTest6: boxed string value as value");

        try
        {
            value = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);
            de  = new DictionaryEntry(new object(), (object)value);

            if (!value.Equals(de.Value))
            {
                TestLibrary.TestFramework.LogError("011", "Value is not " + value + " as expected: Actual(" + de.Value + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012" ,"Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
