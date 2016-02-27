// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.Array.GetValue(System.Int32)
/// </summary>
public class ArrayGetValue1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Test the one dimension array ");

        try
        {
            int[] i1 = new int[1000];
            for (int i = 0; i < 1000; i++) // set the value in every element of the array
            {
                i1[i] = i;
            }
            for (int i = 0; i < 1000; i++)
            {
                if (i1[i] != (int)i1.GetValue(i))
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected. ");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The dimension is greater than one");

        try
        {
            int[,] i1 = new int[6, 8];
            object o1 = i1.GetValue(3);
            TestLibrary.TestFramework.LogError("101", "The ArgumentException is not thrown as expected ");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The index is less than zero");

        try
        {
            int rank = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[rank];
            object i2 = i1.GetValue(-1);
            TestLibrary.TestFramework.LogError("103", "The IndexOutOfRangeException is not thrown as expected ");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: The index is greater than the upper bound");

        try
        {
            int rank = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[rank];
            object i2 = i1.GetValue(rank + 1);
            TestLibrary.TestFramework.LogError("105", "The IndexOutOfRangeException is not thrown as expected ");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ArrayGetValue1 test = new ArrayGetValue1();

        TestLibrary.TestFramework.BeginTestCase("ArrayGetValue1");

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
