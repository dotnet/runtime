// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;


public class ArrayIListAdd
{
    public static int Main(string[] args)
    {
        ArrayIListAdd aIListAdd = new ArrayIListAdd();
        TestLibrary.TestFramework.BeginScenario("Testing Array.System.Collections.IList.Add...");

        if (aIListAdd.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("Verify add interface is not implement...");

        try
        {
            Array myObjArray = Array.CreateInstance(typeof(object), 10);
            IList myObjList = (IList)myObjArray;
            object addValue = (object)new Int32();
            myObjList.Add(addValue);

            TestLibrary.TestFramework.LogError("001","No exception occurs!");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }
        return retVal;
    }
}
