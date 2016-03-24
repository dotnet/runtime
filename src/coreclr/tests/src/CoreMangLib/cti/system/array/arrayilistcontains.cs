// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayIListContains
{
    public static int Main(string[] args)
    {
        ArrayIListContains aIListContains = new ArrayIListContains();
        TestLibrary.TestFramework.BeginScenario("Testing Array.System.Collections.IList.Contains...");

        if (aIListContains.RunTests())
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
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Contains referrence type...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), 10);
            for (int i = 0; i < myArray.Length; i++)
            {
                myArray.SetValue((object)(i + 1), i);
            }

            IList myIList = (IList)myArray;
            for (int i = 0; i < myIList.Count; i++)
            {
                if (!myIList.Contains((object)(i + 1)))
                {
                    TestLibrary.TestFramework.LogError("001", "Contains wrong object!");
                    retVal = false;
                }
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
        TestLibrary.TestFramework.BeginScenario("Verify Contains value type...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(int), 10);
            for (int i = 0; i < myArray.Length; i++)
            {
                myArray.SetValue(i + 1, i);
            }

            IList myIList = (IList)myArray;
            for (int i = 0; i < myIList.Count; i++)
            {
                if (!myIList.Contains(i + 1))
                {
                    TestLibrary.TestFramework.LogError("003", "Contains wrong object!");
                    retVal = false;
                    break;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify RankException is thrown when invole contain using multiple array...");

        try
        {
            object obj = new object();
            Array myMultiArray = Array.CreateInstance(typeof(object), 10, 10);
            IList myIList = (IList)myMultiArray;
            myIList.Contains(obj);

            TestLibrary.TestFramework.LogError("005","No exception occurs!");
            retVal = false;
        }
        catch (RankException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}

