// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections; 
using System.Collections.Generic;

/// <summary>
/// System.Collections.IDictionary.Contains(object)
/// </summary>
public class IDictionaryContains
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

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The IDictionary contains the key");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            Int32[] key = new Int32[1000];
            Byte[] value = new Byte[1000];
            for (int i = 0; i < 1000; i++)
            {
                key[i] = i;
            }
            TestLibrary.Generator.GetBytes(-55, value);
            for (int i = 0; i < 1000; i++)
            {
                iDictionary.Add(key[i], value[i]);
            }
            int keyValue = this.GetInt32(0, 1000);
            if (!iDictionary.Contains(keyValue))
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected ");
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
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The key is a string of white space");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            iDictionary.Add("  ", true);
            if (!iDictionary.Contains("  "))
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected ");
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
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: The key is a custom class");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            MyClass mc = new MyClass();
            iDictionary.Add(mc, true);
            if (!iDictionary.Contains(mc))
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected ");
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
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: The IDictionary did not contain the key");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            MyClass mc = new MyClass();
            iDictionary.Add(mc, true);
            if (iDictionary.Contains(new MyClass()))
            {
                TestLibrary.TestFramework.LogError("007", "The result is not the value as expected ");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The argument is a null reference");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            iDictionary.Contains(null);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        IDictionaryContains test = new IDictionaryContains();

        TestLibrary.TestFramework.BeginTestCase("IDictionaryContains");

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
    private Int32 GetInt32(Int32 minValue, Int32 maxValue)
    {
        try
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32(-55) % (maxValue - minValue);
            }
        }
        catch
        {
            throw;
        }

        return minValue;
    }
}
public class MyClass
{
}
