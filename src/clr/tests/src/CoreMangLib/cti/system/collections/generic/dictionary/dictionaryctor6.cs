// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

/// <summary>
/// ctor(System.Int32,System.Collections.Generic.IEqualityComparer&lt;TKey&gt;)
/// </summary>

public class DictionaryCtor6
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
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify ctor(System.Int32,System.Collections.Generic.IEqualityComparer&lt;TKey&gt;) .");

        try
        {
            int i = TestLibrary.Generator.GetInt16(-55);
            Dictionary<string, string> dictionary = new Dictionary<string, string>(i, StringComparer.CurrentCultureIgnoreCase);

            if (null == dictionary)
            {
                TestLibrary.TestFramework.LogError("001.1", "Failed to instance a Dictionary type with ctor(System.Int32,System.Collections.Generic.IEqualityComparer&lt;TKey&gt;) .");
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException is not thrown when capacity is less than 0.");

        try
        {
            Dictionary<string, string> openWith = new Dictionary<string, string>(-1, StringComparer.CurrentCultureIgnoreCase);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentOutOfRangeException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException) { }
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
        DictionaryCtor6 test = new DictionaryCtor6();

        TestLibrary.TestFramework.BeginTestCase("DictionaryCtor6");

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
