// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

//System.Array.GetLength(System.Int32)
public class ArrayGetLength
{
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
        retVal = NegTest2() && retVal;

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
            for (int i = 0; i < length; i++)
            {
                i1[i] = i;
            }
            if (i1.GetLength(0) != length)
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the two dimension array ");

        try
        {
            int[] length = { TestLibrary.Generator.GetInt16(-55), TestLibrary.Generator.GetByte() };
            string[,] s1 = new string[length[0], length[1]];
            int d1, d2;
            d1 = s1.GetLength(0);
            d2 = s1.GetLength(1);
            if ((d1 != length[0]) || (d2 != length[1]))
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected. ");
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Test the multiple dimension array ");

        try
        {
            int dimen = 5;
            int[] d = new int[5];// the return value array of GetLength methods,
            int[] length = new int[dimen];
            for (int y = 0; y < 5; y++)
            {
                do
                {
                    length[y] = TestLibrary.Generator.GetByte(-55) % 10;
                }
                while (length[y] == 0);
            }
            double[, , , ,] s1 = new double[length[0], length[1], length[2], length[3], length[4]];

            for (int i = 0; i < dimen; i++)
            {
                d[i] = s1.GetLength(i);
            }
            for (int i = 0; i < dimen; i++)
            {
                if (d[i] != length[i])
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
            int[] i1 = new int[TestLibrary.Generator.GetByte(-55)];
            int length = i1.GetLength(-1);
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
            int rank = TestLibrary.Generator.GetByte(-55);
            int[] i1 = new int[rank];
            int length = i1.GetLength(rank + 1);
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
    #endregion
    #endregion

    public static int Main()
    {
        ArrayGetLength test = new ArrayGetLength();

        TestLibrary.TestFramework.BeginTestCase("ArrayGetLength");

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
