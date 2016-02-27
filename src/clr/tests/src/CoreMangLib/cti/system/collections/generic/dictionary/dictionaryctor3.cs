// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// ctor(System.Collections.Generic.IDictionary&lt;TKey,TValue&gt;,System.Collections.Generic.IEqualityComparer&lt;TKey&gt;)
/// </summary>

public class DictionaryCtor3
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify ctor(System.Collections.Generic.IDictionary&lt;TKey,TValue&gt;,System.Collections.Generic.IEqualityComparer&lt;TKey&gt;) .");

        try
        {
            Dictionary<string, string> openWith = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            Dictionary<string, string> dictionary = new Dictionary<string, string>(openWith, StringComparer.CurrentCultureIgnoreCase);

            if (null == dictionary)
            {
                TestLibrary.TestFramework.LogError("001.1", "Failed to instance a Dictionary type with ctor(System.Collections.Generic.IDictionary&lt;TKey,TValue&gt;)");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException is not thrown when ref of dictionary is null .");

        try
        {
            Dictionary<string, string> openWith = null;

            Dictionary<string, string> dictionary = new Dictionary<string, string>(openWith, StringComparer.CurrentCultureIgnoreCase);

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

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentException is not thrown when dictionary contains duplicate keys.");

        try
        {
            Dictionary<string, string> openWith = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            openWith.Add("duplicate key", "value");
            openWith.Add("duplicate key", "value");

            Dictionary<string, string> dictionary = new Dictionary<string, string>(openWith, StringComparer.CurrentCultureIgnoreCase);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentException is not thrown.");
            retVal = false;
        }
        catch (ArgumentException) { }
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
        DictionaryCtor3 test = new DictionaryCtor3();

        TestLibrary.TestFramework.BeginTestCase("DictionaryCtor3");

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
