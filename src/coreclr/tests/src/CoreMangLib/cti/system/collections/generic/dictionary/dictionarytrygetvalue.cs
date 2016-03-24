// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// TryGetValue(TKey,TValue@)
/// </summary>

public class DictionaryTryGetValue
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method TryGetValue when the specified key existed .");

        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("txt", "notepad.exe");
            dictionary.Add("bmp", "paint.exe");
            dictionary.Add("dib", "paint.exe");
            dictionary.Add("rtf", "wordpad.exe");

            string value = "";
            if (dictionary.TryGetValue("txt", out value) != true)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method TryGetValue Err .");
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

    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown.");

        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("txt", "notepad.exe");
            dictionary.Add("bmp", "paint.exe");
            dictionary.Add("dib", "paint.exe");
            dictionary.Add("rtf", "wordpad.exe");

            string key = null;
            string value = "";
            bool b = dictionary.TryGetValue(key, out value);

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
        DictionaryTryGetValue test = new DictionaryTryGetValue();

        TestLibrary.TestFramework.BeginTestCase("DictionaryTryGetValue");

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
