// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.IDictionary.Add(System.Object,System.Object)
/// </summary>

public class DictionaryIDictionaryAdd
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
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method IDictionaryAdd .");

        try
        {
            IDictionary<string, string> dictionary = new Dictionary<string, string>();

            dictionary.Add("txt", "notepad.exe");
            dictionary.Add("bmp", "paint.exe");
            dictionary.Add("dib", "paint.exe");
            dictionary.Add("rtf", "wordpad.exe");

            ICollection<string> items = dictionary.Keys;

            string testStr = null;
            foreach (string s in items)
                testStr += s;

            bool actual = testStr.Contains("txt") && testStr.Contains("bmp") &&
                          testStr.Contains("dib") && testStr.Contains("rtf");

            if (actual == false)
            {
                TestLibrary.TestFramework.LogError("001.1", "Property IDictionaryKeys Err .");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown when key is a null ref.");

        try
        {
            string key = null;
            string value = "notepad.exe";

            IDictionary<string, string> dictionary = new Dictionary<string, string>();

            dictionary.Add(key, value);

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

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentNullException is not thrown when A value with the same key already exists in the Dictionary.");

        try
        {
            string key = "txt";
            string value = "notepad.exe";

            IDictionary<string, string> dictionary = new Dictionary<string, string>();

            dictionary.Add(key, value);
            dictionary.Add(key, value);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentNullException is not thrown.");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DictionaryIDictionaryAdd test = new DictionaryIDictionaryAdd();

        TestLibrary.TestFramework.BeginTestCase("DictionaryIDictionaryAdd");

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
