// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// String.ToString()
/// </summary>
public class StringToString1
{
    private int c_MINI_STRING_LENGTH = 8;
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main()
    {
        StringToString1 sts1 = new StringToString1();
        TestLibrary.TestFramework.BeginTestCase("StringToString1");

        if (sts1.RunTests())
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
        return retVal;
    }
    public bool PosTest1()
    {
        bool retVal = true;
        string strA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest1: normal string ToString()");
        try
        {
            strA = string.Empty;
            ActualResult = strA.ToString();
            if (ActualResult != string.Empty || !(object.ReferenceEquals(strA, ActualResult)))
            {
                TestLibrary.TestFramework.LogError("001", "normal string ToString() ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        string strA;
        string ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest2: empty string ToString()");
        try
        {
            strA = TestLibrary.Generator.GetString(-55, false, c_MINI_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ActualResult = strA.ToString();
            char[] charStrA = strA.ToCharArray();
            char[] charActual = ActualResult.ToCharArray();
            for (int i = 0; strA.Length > i; i++)
            {
                if (charStrA[i] != charActual[i])
                {
                    TestLibrary.TestFramework.LogError("001", "normal string ToString() ActualResult is not the ExpectResult");
                    retVal = false;
                    break;
                }
            }
            if (!object.ReferenceEquals(strA, ActualResult))
            {
                TestLibrary.TestFramework.LogError("002", "normal string ToString() ActualResult is not ExpectResult");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }

}

