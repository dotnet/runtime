// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//System.Array.GetLowerBound(System.Int32)
public class ArrayGetLowerBound
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
            int length = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[length];
            if (i1.GetLowerBound(0) != 0)
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
                    length[y] = TestLibrary.Generator.GetByte(-55) % 10;
                }
                while (length[y] == 0);
            }
            double[, ,] s1 = new double[length[0], length[1], length[2]];

            for (int i = 0; i < dimen; i++)
            {
                d[i] = s1.GetLowerBound(i);
            }
            for (int i = 0; i < dimen; i++)
            {
                if (d[i] != 0)
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

    #region Negative Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The dimension is less than zero");

        try
        {
            int[] i1 = new int[TestLibrary.Generator.GetByte(-55) + 1];
            int bound = i1.GetLowerBound(-1);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: The dimension is greater than the rank");

        try
        {
            int rank = (TestLibrary.Generator.GetByte(-55) % 12) + 1;
            Array theArray = GetArrayOfRank(rank, 2);
            int bound = theArray.GetLowerBound(theArray.Rank + 1);
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: The dimension is equal to the rank");

        try
        {
            int rank = (TestLibrary.Generator.GetByte(-55) % 12) + 1;
            Array theArray = GetArrayOfRank(rank, 2);
            int bound = theArray.GetLowerBound(theArray.Rank);
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

    // Return an (integer) System.Array with specified rank, each dimension of size dimSize
    // ex. GetArrayOfRank(4, 3) is equivalent to new int[3,3,3,3]
    private Array GetArrayOfRank(int rank, int dimSize)
    {
        int[] sizeArray = new int[rank];
        for (int j = 0; j < rank; j++)
            sizeArray[j] = dimSize;
        return Array.CreateInstance(typeof(int), sizeArray);
    }
    #endregion

    public static int Main()
    {
        ArrayGetLowerBound test = new ArrayGetLowerBound();

        TestLibrary.TestFramework.BeginTestCase("ArrayGetLowerBound");

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
