// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class TypeGetArrayRank
{
    #region Private Variables
    private const int c_DEFAULT_ARRAY_DIMENSION = 2;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        int size = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Get rank of an one dimension array");

        try
        {
            // Do not make a large array
            size = TestLibrary.Generator.GetByte(-55);

            int[] array = new int[size];
            for (int i = 0; i < size; ++i)
            {
                array[i] = TestLibrary.Generator.GetInt32(-55);
            }

            Type type = array.GetType();
            int rank = type.GetArrayRank();
            if (rank != 1)
            {
                TestLibrary.TestFramework.LogError("001", "Get rank of an one dimension array returns " + rank.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e + "; with size = " + size);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        int size = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Get rank of an multiple dimensions array");

        try
        {
            // Do not make a large array
            size = TestLibrary.Generator.GetByte(-55);

            int[,] array = new int[c_DEFAULT_ARRAY_DIMENSION, size];
            for (int i = 0; i < c_DEFAULT_ARRAY_DIMENSION; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    array[i, j] = TestLibrary.Generator.GetInt32(-55);
                }
            }

            Type type = array.GetType();
            int rank = type.GetArrayRank();
            if (rank != c_DEFAULT_ARRAY_DIMENSION)
            {
                TestLibrary.TestFramework.LogError("003", "Get rank of an multiple dimensions array returns " + rank.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e + "; with size = " + size);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        int size = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Get rank of an multiple dimensions jagged array");

        try
        {
            // Do not make a large array
            size = TestLibrary.Generator.GetByte(-55);

            int[][] array = new int[size][];

            for (int i = 0; i < size; ++i)
            {
                int subArrayLength = TestLibrary.Generator.GetByte(-55);
                int[] subArray = new int[subArrayLength];

                for (int j = 0; j < subArrayLength; ++j)
                {
                    subArray[j] = TestLibrary.Generator.GetInt32(-55);
                }

                array[i] = subArray;
            }

            Type type = array.GetType();
            int rank = type.GetArrayRank();
            if (rank != 1)
            {
                TestLibrary.TestFramework.LogError("005", "Get rank of an multiple dimensions jagged array returns " + rank.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Check ArgumentException will be thrown");

        try
        {
            Type t1 = typeof(Exception);

            int returnObject = t1.GetArrayRank();
            TestLibrary.TestFramework.LogError("101", "ArgumentException is not thrown when calling typeof(Exception).GetArrayRank()");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        TypeGetArrayRank test = new TypeGetArrayRank();

        TestLibrary.TestFramework.BeginTestCase("TypeGetArrayRank");

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
