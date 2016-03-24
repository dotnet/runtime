// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.Runtime.InteropServices;


[SecuritySafeCritical]
/// <summary>
/// AddrOfPinnedObject
/// </summary>
public class GCHandleAddrOfPinnedObject
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call AddrOfPinnedObject to get address of Pinned object");

        try
        {
            GCHandle handle = GCHandle.Alloc(TestLibrary.Generator.GetInt32(-55), GCHandleType.Pinned);

            IntPtr ptr = handle.AddrOfPinnedObject();

            if (ptr == IntPtr.Zero)
            {
                TestLibrary.TestFramework.LogError("001.1", "Call AddrOfPinnedObject to get address of Pinned object returns NULL");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: InvalidOperationException should be thrown when The handle is any type other than Pinned. ");

        try
        {
            GCHandle handle = GCHandle.Alloc(TestLibrary.Generator.GetInt32(-55), GCHandleType.Normal);

            IntPtr ptr = handle.AddrOfPinnedObject();

            TestLibrary.TestFramework.LogError("001.1", "InvalidOperationException is not thrown when The handle is any type other than Pinned. ");
            retVal = false;
        }
        catch (InvalidOperationException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GCHandleAddrOfPinnedObject test = new GCHandleAddrOfPinnedObject();

        TestLibrary.TestFramework.BeginTestCase("GCHandleAddrOfPinnedObject");

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
