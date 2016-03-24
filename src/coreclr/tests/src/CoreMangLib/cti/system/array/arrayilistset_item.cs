// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayIListset_item
{
    const int row_length = 10;
    const int column_length = 10;

    public static int Main(string[] args)
    {
        ArrayIListset_item aIListItem = new ArrayIListset_item();
        TestLibrary.TestFramework.BeginScenario("Testing Array.System.Collections.IList.set_item...");

        if (aIListItem.RunTests())
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
        retVal = NegTest3() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify get_item method can get the correct object element in the range...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_length);
            object[] myObjArray1 = new object[myArray.Length];
            for (int i = 0; i < myArray.Length; i++)
            {
                myObjArray1[i] = (object)(i + 1);
                myArray.SetValue((object)(i + 1), i);
            }
            IList myIList = (IList)myArray;
            object[] myObjArray2 = new object[myIList.Count];
            if (myObjArray1.Length != myObjArray2.Length)
            {
                TestLibrary.TestFramework.LogError("001", "The length is not equal!");
                retVal = false;
            }
            else
            {
                for (int j = 0; j < myIList.Count; j++)
                {
                    myObjArray2[j] = myIList[j];
                    if (Convert.ToInt32(myObjArray1[j]) != Convert.ToInt32(myObjArray2[j]))
                    {
                        TestLibrary.TestFramework.LogError("002", "The value get from myIList is not equal to original set");
                        retVal = false;
                    }
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

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify get element before IList instance first index...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_length);
            for (int i = 0; i < myArray.Length; i++)
            {
                myArray.SetValue((object)(i + 1), i);
            }
            IList myIList = (IList)myArray;
            object myIListElement = myIList[-1];

            TestLibrary.TestFramework.LogError("003", "No exception occurs!");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify get element after IList instance last index...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_length);
            for (int i = 0; i < myArray.Length; i++)
            {
                myArray.SetValue((object)(i + 1), i);
            }
            IList myIList = (IList)myArray;
            object myIListElement = myIList[myIList.Count];

            TestLibrary.TestFramework.LogError("005", "No exception occurs!");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing ArgumentException is thrown when get item from multidimensional array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(Int32), row_length, column_length);
            object[] objArray = new object[row_length * column_length];
            for (int i = 0; i < myArray.Length; i++)
            {
                myArray.SetValue(i + 1, i);
            }
            IList myIList = (IList)myArray;

            for (int j = 0; j < myIList.Count; j++)
            {
                objArray[j] = myIList[j];
            }

            TestLibrary.TestFramework.LogError("007", "No exception occurs!");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexcepted exception ouucrs: " + e);
            retVal = false;
        }

        return retVal;
    }
}

