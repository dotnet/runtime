// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Collections.ICollection.Count
/// </summary>
public class ICollectionCount
{
    static Random m_rand = new Random(-55);
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        int count = m_rand.Next(1, 1000);
        Array array = new byte[count];

        TestLibrary.TestFramework.BeginScenario("PosTest1: Using Array which implemented the Count property in ICollection ");

        try
        {
            if (((ICollection)array).Count != count)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                retVal = false;
            }
           
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        Array  array = new byte[0];

        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the count of ICollection is zero  ");

        try
        {
            if (((ICollection)array).Count != 0)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion


    #endregion

    public static int Main()
    {
        ICollectionCount test = new ICollectionCount();

        TestLibrary.TestFramework.BeginTestCase("Test for Property:System.Collections.ICollection.Count");

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

