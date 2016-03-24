// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayGetValue1
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;
    private const int c_MIN_STRLEN = 1;
    private const int c_MAX_STRLEN = 1024;

    public static int Main()
    {
        ArrayGetValue1 ac = new ArrayGetValue1();

        TestLibrary.TestFramework.BeginTestCase("Array.GetValue(int[])");

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
        retVal = NegTest2() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool  retVal = true;
        Array array;
        int   length;
        int   element;
        int   index;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Array.GetValue(int[]) single dim");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);

            element = TestLibrary.Generator.GetInt32(-55);

            // fill the array
            for (int i=0; i<array.Length; i++)
            {
                array.SetValue((object)TestLibrary.Generator.GetInt32(-55), i);
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, index);

            if (element != (int)array.GetValue(new int[1] {index} ))
            {
                TestLibrary.TestFramework.LogError("000", "Unexpected value: Expected(" + element + ") Actual(" + (int)array.GetValue(new int[1] {index} ) + ")");
                retVal = false;
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
        bool  retVal = true;
        Array array;
        int   length;
        int   element;
        int   index;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Array.GetValue(int[]) multi dim");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), new int[2] {length, length});

            element = TestLibrary.Generator.GetInt32(-55);

            // fill the array
            for (int i=0; i<array.GetLength(0); i++)
            {
                array.SetValue((object)TestLibrary.Generator.GetInt32(-55), new int[2] {i,i});
            }

            // set the lucky index
            index = TestLibrary.Generator.GetInt32(-55) % length;

            // set the value
            array.SetValue( element, new int[2] {index,index} );

            if (element != (int)array.GetValue(new int[2] {index,index} ))
            {
                TestLibrary.TestFramework.LogError("002", "Unexpected value: Expected(" + element + ") Actual(" + (int)array.GetValue(new int[2] {index, index} ) + ")");
                retVal = false;
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
        int   length;
        int[] dims;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Array.GetValue(int[]) null dims");

        try
        {
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), length);
            dims   = null;

            array.GetValue(dims);

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

    public bool NegTest2()
    {
        bool  retVal = true;
        Array array;
        int   length;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Array.GetValue(int[]) wrong dim length");

        try
        {
            // creat the array
            length = (TestLibrary.Generator.GetInt32(-55) % (c_MAX_SIZE-c_MIN_SIZE)) + c_MIN_SIZE;
            array  = Array.CreateInstance(typeof(Int32), new int[2] {length, length});

            array.GetValue(new int[4] {0,0,0,0});

            TestLibrary.TestFramework.LogError("006", "Exception expected.");
            retVal = false;
        }
        catch (ArgumentException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
