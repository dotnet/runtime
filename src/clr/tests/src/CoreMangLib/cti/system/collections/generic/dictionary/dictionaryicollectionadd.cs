// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.ICollection&lt;System.Collections.Generic.KeyValuePair&lt;TKey,TValue&gt;&gt;.Add(System.Collections.Generic.KeyValuePair&lt;TKey,TValue&gt;)
/// </summary>

public class DictionaryICollectionAdd
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method ICollectionAdd .");

        try
        {
            ICollection<KeyValuePair<String, String>> dictionary = new Dictionary<String, String>();

            KeyValuePair<string, string> kvp1 = new KeyValuePair<String, String>("txt", "notepad.exe");
            KeyValuePair<string, string> kvp2 = new KeyValuePair<String, String>("bmp", "paint.exe");
            KeyValuePair<string, string> kvp3 = new KeyValuePair<String, String>("dib", "paint.exe");
            KeyValuePair<string, string> kvp4 = new KeyValuePair<String, String>("rtf", "wordpad.exe");

            dictionary.Add(kvp1);
            dictionary.Add(kvp2);
            dictionary.Add(kvp3);
            dictionary.Add(kvp4);

            if ( !(dictionary.Contains(kvp1) &&
                   dictionary.Contains(kvp2) &&
                   dictionary.Contains(kvp3) &&
                   dictionary.Contains(kvp4))   )
            {
                TestLibrary.TestFramework.LogError("001.1", "Method ICollectionAdd Err.");
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
            ICollection<KeyValuePair<String, String>> dictionary = new Dictionary<String, String>();

            string str1 = null;
            KeyValuePair<string, string> kvp1 = new KeyValuePair<String, String>(str1, "notepad.exe");
            dictionary.Add(kvp1);

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
            ICollection<KeyValuePair<String, String>> dictionary = new Dictionary<String, String>();

            KeyValuePair<string, string> kvp1 = new KeyValuePair<String, String>("txt", "notepad.exe");
            dictionary.Add(kvp1);
            dictionary.Add(kvp1);

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
        DictionaryICollectionAdd test = new DictionaryICollectionAdd();

        TestLibrary.TestFramework.BeginTestCase("DictionaryICollectionAdd");

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
