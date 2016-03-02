// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// System.Collections.IList.Item(Int32)
/// </summary>
public class IListItem
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Using item property to get the value in IList");

        try
        {
            byte[] byteValue = new byte[1000];
            TestLibrary.Generator.GetBytes(-55, byteValue);
            List<byte> arrayList = new List<byte>(byteValue);
            for (int i = 0; i < 1000; i++)
            {
                if ((byte)arrayList[i] != (byte)(byteValue as IList)[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected, i is" + i);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Using index to set the property ");

        try
        {
            int[] arrayInt = new int[1000];
            for (int i = 0; i < 1000; i++)
            {
                (arrayInt as IList)[i] = i;
            }
            for (int i = 0; i < 1000; i++)
            {
                if ((int)arrayInt[i] != i)
                {
                    TestLibrary.TestFramework.LogError("003", "The result is not the value as expected, i is" + i);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The index is invalid");

        try
        {
            byte[] byteValue = new byte[100];
            TestLibrary.Generator.GetBytes(-55, byteValue);
            byte by = (byte)(byteValue as IList)[-1];
            TestLibrary.TestFramework.LogError("101", "The IndexOutOfRangeException was not thrown as expected");
            retVal = false;
        }
        catch (IndexOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    #endregion
    #endregion

    public static int Main()
    {
        IListItem test = new IListItem();

        TestLibrary.TestFramework.BeginTestCase("IListItem");

        if (test.RunTests())
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
}
