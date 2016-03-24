// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime;

/// <summary>
/// System.Runtime.CompilerServices.IndexerNameAttribute.Ctor()
/// </summary>
public class IndexerNameAttributeCtor
{
    private const int c_MIN_STRING_LEN = 8;
    private const int c_MAX_STRING_LEN = 256;

    public static int Main()
    {
        IndexerNameAttributeCtor atpaCtor = new IndexerNameAttributeCtor();
        TestLibrary.TestFramework.BeginTestCase("Testing for Method: System.Runtime.CompilerServices.IndexerNameAttribute.Ctor()...");

        if (atpaCtor.RunTests())
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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest1: Create a new IndexerNameAttribute instance ... ";
        const string c_TEST_ID = "P001";

        string c_PROPERTYNAME = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LEN, c_MAX_STRING_LEN);

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            System.Runtime.CompilerServices.IndexerNameAttribute atpa = new System.Runtime.CompilerServices.IndexerNameAttribute(c_PROPERTYNAME);
            if (atpa == null)
            {
                string errorDesc = "the IndexerNameAttribute ctor error occurred.)";
                errorDesc += "\n parameter is " + c_PROPERTYNAME;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest2: the parameter is empty ... ";
        const string c_TEST_ID = "P002";

        string c_PROPERTYNAME = "";

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            System.Runtime.CompilerServices.IndexerNameAttribute atpa = new System.Runtime.CompilerServices.IndexerNameAttribute(c_PROPERTYNAME);
            if (atpa == null)
            {
                string errorDesc = "the IndexerNameAttribute ctor error occurred.)";
                errorDesc += "\n parameter is empty";
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        const string c_TEST_DESC = "PosTest3: the parameter is a null reference ... ";
        const string c_TEST_ID = "P003";

        string c_PROPERTYNAME = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            System.Runtime.CompilerServices.IndexerNameAttribute atpa = new System.Runtime.CompilerServices.IndexerNameAttribute(c_PROPERTYNAME);
            if (atpa == null)
            {
                string errorDesc = "the IndexerNameAttribute ctor error occurred.)";
                errorDesc += "\n parameter is a null reference";
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }


        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "unexpected exception occurs :" + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
