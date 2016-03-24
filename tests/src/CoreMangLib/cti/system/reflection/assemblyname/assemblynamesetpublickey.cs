// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Reflection;

[assembly: SecurityCritical]

/// <summary>
/// System.Reflection.AssembelyName.SetPublicKey(byte[])
/// </summary>
public class AssemblyNameSetPublicKey
{
    #region Main Entry
    static public int Main()
    {
        AssemblyNameSetPublicKey test = new AssemblyNameSetPublicKey();

        TestLibrary.TestFramework.BeginTestCase("AssembelyName.SetPublicKey(byte[])");

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

    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        TestLibrary.TestFramework.BeginScenario("PosTest1: set then get and verify");
        bool retVal = true;
        try
        {
            byte[] key = new byte[TestLibrary.Generator.GetInt32(-55) % 80 + 20];

            AssemblyName an = new AssemblyName();
            an.SetPublicKey(key);

            if (an.GetPublicKey() != key)
            {
                TestLibrary.TestFramework.LogError("001.1", "expect AssemblyName.GetPublicKey() equals what it's been set");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: set a 0 length key then get and verify");
        bool retVal = true;
        try
        {
            byte[] key = new byte[0];

            AssemblyName an = new AssemblyName();
            an.SetPublicKey(key);

            if (an.GetPublicKey() != key)
            {
                TestLibrary.TestFramework.LogError("002.1", "expect AssemblyName.GetPublicKey() equals what it's been set");
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

    public bool PosTest3()
    {
        TestLibrary.TestFramework.BeginScenario("PosTest2: set a null key then get and verify");
        bool retVal = true;
        try
        {
            byte[] key = null;

            AssemblyName an = new AssemblyName();
            an.SetPublicKey(key);

            if (an.GetPublicKey() != key)
            {
                TestLibrary.TestFramework.LogError("003.1", "expect AssemblyName.GetPublicKey() equals what it's been set");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }

}
