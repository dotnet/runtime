// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayLength 
{
    const int row_length = 100;
    const int column_length = 10;
    const int high_length = 10;

    public static int Main(string[] args)
    {
        ArrayLength aLength = new ArrayLength();  
        TestLibrary.TestFramework.BeginScenario("Testing Array.Length property...");

        if (aLength.RunTests())
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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Length property of one-dimensional array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(Int32), row_length);
            if (myArray.Length!=row_length)
            {
                TestLibrary.TestFramework.LogError("001", "The Length property is not correct!");
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
        TestLibrary.TestFramework.BeginScenario("Verify Length property of two-dimensional array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object),row_length,column_length);
            if (myArray.Length!=row_length*column_length)
            {
                TestLibrary.TestFramework.LogError("003", "The Length property is not correct!");
                retVal = true;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()  
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Length property of multi-dimensional array...");

        try
        {
            int[] dimenArray = { row_length,column_length,high_length};
            Array myArray = Array.CreateInstance(typeof(object),dimenArray);

            if (myArray.Length!=row_length*column_length*high_length)
            {
                TestLibrary.TestFramework.LogError("005", "The Length property is not correct...");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1() 
    {
        bool RetVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Length property of array with null reference...");

        try
        {
            Array myArray = null;
            if (myArray.Length != 0)
            {
                TestLibrary.TestFramework.LogError("007", "No exception occurs!");
                RetVal = true;
            }
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

