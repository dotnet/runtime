// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// Remove(TKey)
/// </summary>

public class DictionaryRemove
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        //
        // TODO: Add your negative test cases here
        //
        // TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method Remove when specified key is not exist .");

        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            bool actual = dictionary.Remove("txt");
            bool expected = false;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method Remove Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify method Remove when specified key existed.");

        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("txt", "notepad.exe");
            dictionary.Add("bmp", "paint.exe");
            dictionary.Add("dib", "paint.exe");
            dictionary.Add("rtf", "wordpad.exe");

            int count = dictionary.Count;

            bool actual = dictionary.Remove("txt");
            bool expected = true;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method Remove Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", expected = " + expected);
                retVal = false;
            }

            if (dictionary.Count != count - 1)
            {
                TestLibrary.TestFramework.LogError("001.2", "Method Remvoe Err.");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + dictionary.Count + ", expected = " + (count-1));
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.3", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown when key is null ref .");

        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string str = null;
            dictionary.Remove(str);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown.");
            retVal = false;
        }
        catch (ArgumentNullException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DictionaryRemove test = new DictionaryRemove();

        TestLibrary.TestFramework.BeginTestCase("DictionaryRemove");

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
