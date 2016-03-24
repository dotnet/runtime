// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayBinarySort2
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_NUM_LOOPS = 50;

    public static int Main()
    {
        ArrayBinarySort2 ac = new ArrayBinarySort2();

        TestLibrary.TestFramework.BeginTestCase("Array.Sort(Array, IComparer)");

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
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool   retVal = true;
        Array  array;
        int    length;
        double element;
        IComparer myc;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array.Sort(Array, IComparer) ");

        try
        {
            for(int j=0; j<c_NUM_LOOPS; j++)
            {
                myc = new MyComparer();

                // creat the array
                length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
                array  = Array.CreateInstance(typeof(Double), length);

                // fill the array
                for (int i=0; i<array.Length; i++)
                {
                    array.SetValue((object)TestLibrary.Generator.GetDouble(-55), i);
                }

                Array.Sort(array, myc);

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

    public bool PosTest2()
    {
        bool     retVal = true;
        Array    array;
        int      length;
        IComparer myc;
        double    element;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Array.Sort(Array, IComparer) null comparer");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(double), length);

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array.SetValue(TestLibrary.Generator.GetDouble(-55), i);
            }

            myc = null;
            Array.Sort(array, myc);

            // ensure that all the elements are sorted
            element = (double)array.GetValue(0);
            for(int i=0; i<array.Length; i++)
            {
                if (element > (double)array.GetValue(i))
                {
                    TestLibrary.TestFramework.LogError("002", "Unexpected value: Element (" + element + ") is greater than (" + (double)array.GetValue(i) + ")");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool  retVal = true;
        Array array;
        IComparer myc;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.Sort(Array, IComparer) null array");

        try
        {
            array = null;
            myc   = new MyComparer();

            Array.Sort(array, myc);

            TestLibrary.TestFramework.LogError("004", "Exception expected.");
            retVal = false;
        }
        catch (ArgumentNullException)
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

    public class MyComparer : IComparer
    {
        public int Compare(object obj1, object obj2)
        {

            if ((double)obj1 == (double)obj2) return 0;

            return ((double)obj1 < (double)obj2) ? -1 : 1;
        }
    }

}
