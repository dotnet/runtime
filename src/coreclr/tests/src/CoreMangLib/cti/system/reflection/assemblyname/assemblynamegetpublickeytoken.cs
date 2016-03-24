// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Reflection;

[assembly: SecurityCritical]

/// <summary>
/// System.Reflection.AssembelyName.GetPublicKeyToken()
/// </summary>
public class AssemblyNameGetPublicKeyToken
{
    #region Main Entry
    static public int Main()
    {
        AssemblyNameGetPublicKeyToken test = new AssemblyNameGetPublicKeyToken();

        TestLibrary.TestFramework.BeginTestCase("AssembelyName.GetPublicKeyToken");

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
    #endregion

    #region utility functions
    string Key2String(byte[] key)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder(key.Length * 2);
        for (int i = 0; i < key.Length; i++)
        {
            result.AppendFormat("{0:x2}", key[i]);
        }
        return result.ToString();
    }

    bool KeyEquals(byte[] k1, byte[] k2)
    {
        if (k1.Length != k2.Length)
            return false;
        for (int i = 0; i < k1.Length; i++)
        {
            if (k1[i] != k2[i])
                return false;
        }
        return true;
    }
    #endregion

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: get default PublicKeyToken");
        bool retVal = true;
        try
        {
            AssemblyName an = new AssemblyName();
            byte[] key = an.GetPublicKeyToken();
            if (key != null)
            {
                TestLibrary.TestFramework.LogError("001.1", "expect (new AssemblyName()).GetPublicKeyToken() == null");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        TestLibrary.TestFramework.BeginScenario("PosTest2: set then get and verify");
        bool retVal = true;
        try
        {
            byte[] key = new byte[TestLibrary.Generator.GetInt32(-55) % 255];

            AssemblyName an = new AssemblyName();
            an.SetPublicKeyToken(key);

            if (an.GetPublicKeyToken() != key)
            {
                TestLibrary.TestFramework.LogError("002.1", "expect AssemblyName.GetPublicKeyToken() equals what it's been set");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
}
