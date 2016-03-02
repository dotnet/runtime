// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.ICollection&lt;System.Collections.Generic.KeyValuePair&lt;TKey,TValue&gt;&gt;.CopyTo(System.Collections.Generic.KeyValuePair&lt;TKey,TValue&gt;[],System.Int32)
/// </summary>

public class DictionaryICollectionCopyTo
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
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method ICollectionCopyTo .");

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

            KeyValuePair<string, string>[] kvpArray = new KeyValuePair<string,string>[dictionary.Count];

            dictionary.CopyTo(kvpArray, 0);


            bool actual = (kvpArray[0].Equals(kvp1)) && (kvpArray[1].Equals(kvp2)) &&
                          (kvpArray[2].Equals(kvp3)) && (kvpArray[3].Equals(kvp4));
            bool expected = true;

            if (actual != expected)
            {
                TestLibrary.TestFramework.LogError("001.1", "Method ICollectionCopyTo Err.");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown when array is null ref.");

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

            KeyValuePair<string, string>[] kvpArray = null;
            dictionary.CopyTo(kvpArray, 0);

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

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException is not thrown .");

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

            KeyValuePair<string, string>[] kvpArray = new KeyValuePair<string, string>[dictionary.Count];
            dictionary.CopyTo(kvpArray, -1);

            TestLibrary.TestFramework.LogError("102.1", "ArgumentOutOfRangeException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: ArgumentException is not thrown .");

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

            KeyValuePair<string, string>[] kvpArray = new KeyValuePair<string, string>[dictionary.Count];
            dictionary.CopyTo(kvpArray, 4);

            TestLibrary.TestFramework.LogError("103.1", "ArgumentException is not thrown.");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: ArgumentException is not thrown .");

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

            KeyValuePair<string, string>[] kvpArray = new KeyValuePair<string, string>[dictionary.Count - 1];
            dictionary.CopyTo(kvpArray, 0);

            TestLibrary.TestFramework.LogError("104.1", "ArgumentException is not thrown.");
            retVal = false;
        }
        catch (ArgumentException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DictionaryICollectionCopyTo test = new DictionaryICollectionCopyTo();

        TestLibrary.TestFramework.BeginTestCase("DictionaryICollectionCopyTo");

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
