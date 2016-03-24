// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// System.Collections.Generic.IEnumerable&lt;System.Collections.Generic.KeyValuePair&lt;TKey,TValue&gt;&gt;.GetEnumerator
/// </summary>

public class DictionaryIEnumerableGetEnumerator
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
        // retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify method IEnumerableGetEnumerator.");

        try
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            KeyValuePair<string, string> kvp1 = new KeyValuePair<String, String>("txt", "notepad.exe");
            KeyValuePair<string, string> kvp2 = new KeyValuePair<String, String>("bmp", "paint.exe");
            KeyValuePair<string, string> kvp3 = new KeyValuePair<String, String>("dib", "paint.exe");
            KeyValuePair<string, string> kvp4 = new KeyValuePair<String, String>("rtf", "wordpad.exe");

            dictionary.Add(kvp1.Key,kvp1.Value);
            dictionary.Add(kvp2.Key,kvp2.Value);
            dictionary.Add(kvp3.Key,kvp3.Value);
            dictionary.Add(kvp4.Key,kvp4.Value);

            if (!(dictionary is IEnumerable<KeyValuePair<string, string>>))
            {
                TestLibrary.TestFramework.LogError("001.1", "The dictionary is not a instance of IEnumerable.");
                retVal = false;
            }
            else
            {

                string testStr = null;
                foreach (string s in dictionary.Values)
                    testStr += s;

                if ( !(testStr.Contains(kvp1.Value) &&
                       testStr.Contains(kvp2.Value) &&
                       testStr.Contains(kvp3.Value) &&
                       testStr.Contains(kvp4.Value)) )
                {
                    TestLibrary.TestFramework.LogError("001.2", "Method IEnumerableGetEnumerator Err.");
                    retVal = false;
                }
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
    //public bool NegTest1()
    //{
    //    bool retVal = true;

    //    TestLibrary.TestFramework.BeginScenario("NegTest1: ");

    //    try
    //    {
    //          //
    //          // Add your test logic here
    //          //
    //    }
    //    catch (Exception e)
    //    {
    //        TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
    //        TestLibrary.TestFramework.LogInformation(e.StackTrace);
    //        retVal = false;
    //    }

    //    return retVal;
    //}
    #endregion
    #endregion

    public static int Main()
    {
        DictionaryIEnumerableGetEnumerator test = new DictionaryIEnumerableGetEnumerator();

        TestLibrary.TestFramework.BeginTestCase("DictionaryIEnumerableGetEnumerator");

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
