// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ArrayIListRemoveAt
{
    public static int Main(string[] args)
    {
        ArrayIListRemoveAt aIListRemove = new ArrayIListRemoveAt();
        TestLibrary.TestFramework.BeginScenario("Testing Array,System.IList.RemoveAt...");

        if (aIListRemove.RunTests())
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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify System.IList.RemoveAt interface is not implement...");

        try
        {
            Array myObjArray = Array.CreateInstance(typeof(object), 10);
            for (int i = 0; i < myObjArray.Length; i++)
            {
                myObjArray.SetValue((object)(i + 1), i);
            }
            IList myObjList = (IList)myObjArray;

            for (int j = 0; j < myObjArray.Length; j++)
            {
                myObjList.RemoveAt(j);
            }

            TestLibrary.TestFramework.LogError("001", "No exception occurs!");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }
        return retVal;
    }
}
