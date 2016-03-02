// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayICollectionget_Count
{
    const int row_Length = 10;
    const int column_length = 10;
    const int three_dimensionlngth = 10;

    public static int Main(string[] args)
    {
        ArrayICollectionget_Count aICollectionCount = new ArrayICollectionget_Count();
        TestLibrary.TestFramework.BeginScenario("Testing Array.System.ICollection.get_Count...");

        if (aICollectionCount.RunTests())
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

        return retVal;
    }
    
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify get_Count interface can fetch correct value in one-dimensional array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_Length);
            ICollection myIList = (ICollection)myArray;
            if (myIList.Count != row_Length)
            {
                TestLibrary.TestFramework.LogError("001","The count is not equal to row_length in one-dimensional array!");
                return retVal;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify get_Count interface can fetch correct value in two-dimensional array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_Length,column_length);
            ICollection myIList = (ICollection)myArray;
            if (myIList.Count != row_Length*column_length)
            {
                TestLibrary.TestFramework.LogError("001", "The count is not equal to total length in two-dimensional array!");
                return retVal;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify get_Count interface can fetch correct value in multi-dimensional array...");

        try
        {
            int[] dimensionCount = { row_Length, column_length, three_dimensionlngth };
            Array myArray = Array.CreateInstance(typeof(object), dimensionCount);
            ICollection myIList = (ICollection)myArray;
            if (myIList.Count != row_Length * column_length * three_dimensionlngth)
            {
                TestLibrary.TestFramework.LogError("003", "The count is not equal to total length in multi-dimensional array... ");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception ouucrs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
