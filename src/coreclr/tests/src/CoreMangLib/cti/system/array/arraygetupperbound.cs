// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//System.Array.GetUpperBound(System.Int32)
public class ArrayGetUpperBound
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

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
            int length = GetInt(1, Byte.MaxValue);
            int[] i1 = new int[length];
            if (i1.GetUpperBound(0) != (length - 1))
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected. ");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the multiple dimension array ");

        try
        {
            int dimen = 3;
            int[] d = new int[dimen];// the return value array of Getlowerbound methods,
            int[] length = new int[dimen];
            for (int y = 0; y < 3; y++)
            {
                do
                {
                    length[y] = TestLibrary.Generator.GetByte() % 10;
                }
                while (length[y] == 0);
            }
            double[, ,] s1 = new double[length[0], length[1], length[2]];

            for (int i = 0; i < dimen; i++)
            {
                d[i] = s1.GetUpperBound(i);
            }
            for (int i = 0; i < dimen; i++)
            {
                if (d[i] != (length[i] - 1))
                {
                    TestLibrary.TestFramework.LogError("005", "The result is not the value as expected. ");
                    retVal = false;
                }

            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The dimension is less than zero");

        try
        {
            int[] i1 = new int[GetInt(1, Byte.MaxValue)];
            int bound = i1.GetUpperBound(-1);
            TestLibrary.TestFramework.LogError("101", "The IndexOutOfRangeException is not thrown as expected ");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The dimension is equal to the rank");

        try
        {
            int rank = GetInt(1, Byte.MaxValue);
            int[] i1 = new int[rank];
            int bound = i1.GetUpperBound(rank);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The dimension is greater than the rank");

        try
        {
            int rank = GetInt(1, Byte.MaxValue);
            int range = GetInt(1, Byte.MaxValue);
            int[] i1 = new int[rank];
            int bound = i1.GetUpperBound(rank + range);
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

    #region Help method for geting test data

    private Int32 GetInt(Int32 minValue, Int32 maxValue)
    {
        if (minValue == maxValue)
        {
            return minValue;
        }
        if (minValue < maxValue)
        {
            return minValue + TestLibrary.Generator.GetInt32() % (maxValue - minValue);
        }

        return minValue;
    }

    #endregion

    public static int Main()
    {
        ArrayGetUpperBound test = new ArrayGetUpperBound();

        TestLibrary.TestFramework.BeginTestCase("ArrayGetUpperBound");

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
