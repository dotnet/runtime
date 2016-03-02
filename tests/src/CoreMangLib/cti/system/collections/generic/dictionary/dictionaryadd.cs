// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// Add(TKey,TValue)
/// </summary>

public class DictionaryAdd
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method Add(TKey,TValue) .");

        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("txt", "notepad.exe");
            dictionary.Add("bmp", "paint.exe");
            dictionary.Add("dib", "paint.exe");
            dictionary.Add("rtf", "wordpad.exe");

            string actual = dictionary["txt"] + " " +
                            dictionary["bmp"] + " " +
                            dictionary["dib"] + " " +
                            dictionary["rtf"];
            string expected = "notepad.exe" + " " +
                              "paint.exe"   + " " +
                              "paint.exe"   + " " +
                              "wordpad.exe";

            if (dictionary["txt"] != "notepad.exe" ||
                dictionary["bmp"] != "paint.exe"   ||
                dictionary["dib"] != "paint.exe"   ||
                dictionary["rtf"] != "wordpad.exe")
            {
                TestLibrary.TestFramework.LogError("001.1", "Method Add(TKey,TValue) Err.");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown when key is null ref.");

        try
        {
            string str = null;
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add(str, "notepad.exe");

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

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentException is not thrown when An element with the same key already exists in the Dictionary .");

        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("txt", "notepad.exe");
            dictionary.Add("txt", "notepad.exe");

            TestLibrary.TestFramework.LogError("102.1", "ArgumentException is not thrown.");
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
        DictionaryAdd test = new DictionaryAdd();

        TestLibrary.TestFramework.BeginTestCase("DictionaryAdd");

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
