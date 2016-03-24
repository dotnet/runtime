// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Reflection;

[assembly: SecurityCritical]

/// <summary>
/// System.Reflection.AssembelyName.Version
/// </summary>
public class AssemblyNameVersion
{
    #region Main Entry
    static public int Main()
    {
        AssemblyNameVersion test = new AssemblyNameVersion();

        TestLibrary.TestFramework.BeginTestCase("AssembelyName.Version");

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
        TestLibrary.TestFramework.BeginScenario("PosTest1: check version which set in ctor");
        bool retVal = true;
        try
        {
            Version ver = new Version(1,0,0,1);
            AssemblyName an = new AssemblyName("aa, Version="+ver.ToString());

            if (!an.Version.Equals(ver))
            {
                TestLibrary.TestFramework.LogError("001.1", "expect Version is " + ver.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: version in FullName");
        bool retVal = true;
        try
        {
            AssemblyName an = new AssemblyName("aa");
            Version ver = new Version("10.100.1000.10000");

            an.Version = ver;
            if (!an.FullName.Contains("Version="+ver.ToString()))
            {
                TestLibrary.TestFramework.LogError("002.1", "expect an.FullName.Contains " + ver.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest3: version set in ctor then set as null");
        bool retVal = true;
        try
        {
            Version ver = new Version("10.100.1000.10000");
            AssemblyName an = new AssemblyName("aa, Version="+ver.ToString());

            if (!an.Version.Equals(ver))
            {
                TestLibrary.TestFramework.LogError("003.1", "expect Version is " + ver.ToString());
                retVal = false;
            }

            an.Version = null;
            if (an.Version != null)
            {
                TestLibrary.TestFramework.LogError("003.2", "expect Version is " + ver.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.3", "Unexpected exception: " + e);
            retVal = false;
        }
        return retVal;
    }
}