// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// String.ToCharArray()
/// </summary>
public class StringToCharArray
{
    private int c_MAX_STRING_LENGTH = 256;

    public static int Main()
    {
        StringToCharArray stca = new StringToCharArray();
        TestLibrary.TestFramework.BeginTestCase("StringToCharArray");

        if (stca.RunTests())
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
    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        string strA;
        char[]ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest1: empty string ToCharArray");
        try
        {
            strA = string.Empty;
            ActualResult = strA.ToCharArray();
            char[] Expect = new char[0];
            if (ActualResult.ToString()!= Expect.ToString() || ActualResult.Length !=0)
            {
                TestLibrary.TestFramework.LogError("001", "empty string ToCharArray ActualResult is not the ExpectResult");
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        string strA = "";
        char[] ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest2 : normal string ToCharArray");
        try
        {
            char[] Expect = new char[256];
            for (int i = 0; i < c_MAX_STRING_LENGTH; i++)
            {
                char charA = TestLibrary.Generator.GetChar(-55);
                Expect[i] = charA;
                strA += charA.ToString();
            }
            ActualResult = strA.ToCharArray();
            for (int j = 0; j < ActualResult.Length; j++)
            {
                if (ActualResult[j] != Expect[j])
                {
                    TestLibrary.TestFramework.LogError("001", "normal string ToCharArray ActualResult is not ExpectResult");
                    retVal = false;
                    break;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
    
}

