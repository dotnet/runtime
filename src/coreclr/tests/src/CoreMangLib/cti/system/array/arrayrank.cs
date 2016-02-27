// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayRank
{
    const int row_length = 100;
    const int column_length = 10;
    const int high_length = 10;

    public static int Main(string[] args)
    {
        ArrayRank aRank = new ArrayRank();
        TestLibrary.TestFramework.BeginScenario("Testing Array.Rank property...");

        if (aRank.RunTests())
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
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Rank property of one-dimensional array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(Int32), row_length);
            if (myArray.Rank != 1)
            {
                TestLibrary.TestFramework.LogError("001", "The Rank property is not correct!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpceted exception ouucrs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Rank property of two-dimensional array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_length, column_length);
            if (myArray.Rank != 2)
            {
                TestLibrary.TestFramework.LogError("003", "The Rank property is not correct!");
                retVal = true;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Rank property of multi-dimensional array...");

        try
        {
            int[] dimenArray = { row_length, column_length, high_length };
            Array myArray = Array.CreateInstance(typeof(object), dimenArray);

            if (myArray.Rank != 3)
            {
                TestLibrary.TestFramework.LogError("005", "The Rank property is not correct...");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    /// <summary>
    /// new added scenario
    /// </summary>
    /// <returns></returns>
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("check the rank of an empty array...");

        try
        {
            Array myArray = new int[0];

            if (myArray.Rank != 1)
            {
                TestLibrary.TestFramework.LogError("009", "The Rank property is not correct...");
                retVal = false;
            }

            myArray = new object[0,0];
            if (myArray.Rank != 2)
            {
                TestLibrary.TestFramework.LogError("010", "The Rank property is not correct...");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("011", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool RetVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Rank property of array with null reference...");

        try
        {
            Array myArray = null;
            int arrayRank = myArray.Rank;

            TestLibrary.TestFramework.LogError("007", "No exception occurs!");
            RetVal = true;
        }
        catch (NullReferenceException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            RetVal = false;
        }

        return RetVal;
    }

    public bool NegTest2()
    {
        bool RetVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Rank property of array with null reference...");

        try
        {
            Array myArray = null;
            int rank = myArray.Rank;

            TestLibrary.TestFramework.LogError("007", "No exception occurs!");
            RetVal = true;
        }
        catch (NullReferenceException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            RetVal = false;
        }

        return RetVal;
    }

}

