// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayIListget_item
{
    const int row_length = 10;
    const int colomn_length = 10;

    public static int Main(string[] args)
    {
        ArrayIListget_item aILIstGetItem = new ArrayIListget_item();
        TestLibrary.TestFramework.BeginScenario("Testing Array.System.IList.get_item...");

        if (aILIstGetItem.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Verify set_item method can get the correct object element in the range...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_length);
            object[] myObjArray1 = new object[myArray.Length];
            for (int i = 0; i < myArray.Length; i++)
            {
                myObjArray1[i] = (object)(i + 1);
            }
            IList myIList = (IList)myArray;

            for (int j = 0; j < myIList.Count; j++)
            {
                myIList[j] = myObjArray1[j];
                if (Convert.ToInt32(myObjArray1[j]) != Convert.ToInt32(myIList[j]))
                {
                    TestLibrary.TestFramework.LogError("002", "The value get from myIList is not equal to original set");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify set element before IList instance first index...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_length);
            for (int i = 0; i < myArray.Length; i++)
            {
                myArray.SetValue((object)(i + 1), i);
            }
            IList myIList = (IList)myArray;
            myIList[-1] = (object)10;

            TestLibrary.TestFramework.LogError("004", "No exception occurs!");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify set element after IList instance last index...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_length);
            for (int i = 0; i < myArray.Length; i++)
            {
                myArray.SetValue((object)(i + 1), i);
            }
            IList myIList = (IList)myArray;
            object myIListElement = myIList[myIList.Count];

            TestLibrary.TestFramework.LogError("006", "No exception occurs!");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("007", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Testing ArgumentException is thrown when set item from multidimensional array...");

        try
        {
            Array myArray = Array.CreateInstance(typeof(object), row_length, colomn_length);
            object[] objArray = new object[row_length * colomn_length];
            
            for (int i = 0; i < row_length; i++)
            {
                for (int j = 0; j < colomn_length; j++)
                {
                    myArray.SetValue((object)((i + 1) * (j + 1)), i, j);
                }
            }
            IList myIList = (IList)myArray;

            for (int j = 0; j < myIList.Count; j++)
            {
                objArray[j] = myIList[j];
            }

            TestLibrary.TestFramework.LogError("008", "No exception occurs!");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexcepted exception ouucrs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
