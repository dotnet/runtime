// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayIListClear
{
    public static int Main(string[] args)
    {
        ArrayIListClear aIListClear = new ArrayIListClear();
        TestLibrary.TestFramework.BeginScenario("Testing Array.System.Collections.IList.clear...");

        if (aIListClear.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Verify Int array is set zero value after clear action...");

        try
        {
            Array myObjArray = Array.CreateInstance(typeof(int), 10);
            for (int i = 0; i < myObjArray.Length; i++)
            {
                myObjArray.SetValue(i + 1, i);
            }

            IList myIList = (IList)myObjArray;
            myIList.Clear();
            foreach (int element in myIList)
            {
                if (element != 0)
                {
                    TestLibrary.TestFramework.LogError("003", "element is not removed yet!");
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

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify object array is set null value after clear action...");

        try
        {
            Array myObjArray = Array.CreateInstance(typeof(object), 10);
            for (int i = 0; i < myObjArray.Length; i++)
            {
                myObjArray.SetValue((object)(i + 1), i);
            }

            IList myIList = (IList)myObjArray;
            myIList.Clear();
            foreach (object element in myIList)
            {
                if (element != null)
                {
                    TestLibrary.TestFramework.LogError("001", "element is not removed yet!");
                    retVal = false;
                    break;
                }
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
        TestLibrary.TestFramework.BeginScenario("Verify bool array is set false value after clear action...");

        try
        {
            Array myObjArray = Array.CreateInstance(typeof(bool), 10);
            for (int i = 0; i < myObjArray.Length; i++)
            {
                myObjArray.SetValue(true, i);
            }

            IList myIList = (IList)myObjArray;
            myIList.Clear();
            foreach (bool element in myIList)
            {
                if (element)
                {
                    TestLibrary.TestFramework.LogError("005", "element is not removed yet!");
                    retVal = false;
                    break;
                }
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify ArgumentNullException is thrown when array is null...");

        try
        {
            Array myArray = null;
            IList myIList = (IList)myArray;

            myIList.Clear();
            TestLibrary.TestFramework.LogError("007","No exception occurs!");
            retVal = true;
        }
        catch (NullReferenceException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008","Unexpected exception occurs: " + e);
            retVal = false;
        }
        
        return retVal;
    }
}
