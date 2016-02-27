// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayIListIndexOF
{
    const int minLength = 1;
    const int maxLength = 10;
    string myValue = TestLibrary.Generator.GetString(-55, false, minLength, maxLength);

    public static int Main(string[] args)
    {
        ArrayIListIndexOF aIListIndexOf = new ArrayIListIndexOF();
        TestLibrary.TestFramework.BeginScenario("Testing Array.System.Collections.IList.IndexOf...");

        if (aIListIndexOf.RunTests())
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
        retVal = NegTest2() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Indexof method return correct when multiple same objects in array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(String), 10);
            
            for (int i = 0; i < myValue.Length; i++)
            {
                myArray.SetValue(myValue, i);
            }
            IList myIList = (IList)myArray;
            if (myIList.IndexOf(myValue) != 0)
            {
                TestLibrary.TestFramework.LogError("001","The index if not the first place the value appears!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify NullReferrenceException is thrown when array is null...");

        try
        {
            Array myArray = null;
            IList myIList = (IList)myArray;
            myIList.IndexOf(myValue);

            TestLibrary.TestFramework.LogError("001","No exception ouucrs!");
            retVal = false;
        }
        catch (NullReferenceException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify RankException is thrown when array is multidimensional...");

        try
        {
            Array myMultiArray = Array.CreateInstance(typeof(string), 10, 10);
            IList myIList = (IList)myMultiArray;
            myIList.IndexOf(myValue);

            TestLibrary.TestFramework.LogError("003","No exception occurs!");
            retVal = false;
        }
        catch (RankException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}

