// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayBinarySort1
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_NUM_LOOPS = 50;

    public static int Main()
    {
        ArrayBinarySort1 ac = new ArrayBinarySort1();

        TestLibrary.TestFramework.BeginTestCase("Array.Sort(Array)");

        if (ac.RunTests())
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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;	// jagged array

        return retVal;
    }

    public bool PosTest1()
    {
        bool   retVal = true;
        Array  array;
        int    length;
        double element;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array.Sort(Array) ");

        try
        {
            for(int j=0; j<c_NUM_LOOPS; j++)
            {
                // creat the array
                length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
                array  = Array.CreateInstance(typeof(Double), length);

                // fill the array
                for (int i=0; i<array.Length; i++)
                {
                    array.SetValue((object)TestLibrary.Generator.GetDouble(-55), i);
                }

                Array.Sort(array);

                // ensure that all the elements are sorted
                element = (double)array.GetValue(0);
                for(int i=0; i<array.Length; i++)
                {
                    if (element > (double)array.GetValue(i))
                    {
                        TestLibrary.TestFramework.LogError("000", "Unexpected value: Element (" + element + ") is greater than (" + (double)array.GetValue(i) + ")");
                        retVal = false;
                    }
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool  retVal = true;
        Array array;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.Sort(Array) null array");

        try
        {
            array = null;

            Array.Sort(array);

            TestLibrary.TestFramework.LogError("002", "Exception expected.");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool     retVal = true;
        Array    array;
        int      length;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Array.Sort(Array) jagged array");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(double), new int[2] {length, length/2});

            // fill the array
            for (int i=0; i<array.GetLength(0); i++)
            {
                for (int j=0; j<array.GetLength(1); j++)
                {
                    array.SetValue(TestLibrary.Generator.GetDouble(-55), new int[2] {i,j});
                }
            }

            Array.Sort(array);

            TestLibrary.TestFramework.LogError("004", "Exception expected");
            retVal = false;
        }
        catch (RankException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

}
