// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Text;

/// <summary>
/// WebName
/// </summary>

public class EncodingWebName
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
        // retVal = NegTest1() && retVal;

        return retVal;
    }


    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify property WebName.");
        try
        {
            // UTF8
            if (!Encoding.UTF8.WebName.Equals("utf-8"))
            {
                TestLibrary.TestFramework.LogError("001", "UTF8 should have Webname utf-8; actual value: " +
                    Encoding.UTF8.WebName);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        try
        {
            // Unicode
            if (!Encoding.Unicode.WebName.Equals("utf-16"))
            {
                TestLibrary.TestFramework.LogError("003", "Unicode should have Webname utf-16; actual value: " +
                    Encoding.Unicode.WebName);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }


        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Round-trip WebNames.");
        try
        {
            // UTF8
            if (!Encoding.GetEncoding(Encoding.UTF8.WebName).Equals(Encoding.UTF8))
            {
                TestLibrary.TestFramework.LogError("005", "GetEncoding(UTF8.WebName) should return UTF8");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        try
        {
            // Unicode
            if (!Encoding.GetEncoding(Encoding.Unicode.WebName).Equals(Encoding.Unicode))
            {
                TestLibrary.TestFramework.LogError("007", "GetEncoding(UTF8.WebName) should return UTF8");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        try
        {
            // BigEndianUnicode
            if (!Encoding.GetEncoding(Encoding.BigEndianUnicode.WebName).Equals(Encoding.BigEndianUnicode))
            {
                TestLibrary.TestFramework.LogError("009", "GetEncoding(BigEndianUnicode.WebName) should return BigEndianUnicode");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Negative Test Cases
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
        EncodingWebName test = new EncodingWebName();

        TestLibrary.TestFramework.BeginTestCase("EncodingWebName");

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
