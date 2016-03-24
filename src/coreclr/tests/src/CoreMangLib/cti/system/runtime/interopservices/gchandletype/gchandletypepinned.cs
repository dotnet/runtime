// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// GCHandleType.Pinned [v-minch]
/// </summary>
public class GCHandleTypePinned
{
    public static int Main()
    {
        GCHandleTypePinned test = new GCHandleTypePinned();
        TestLibrary.TestFramework.BeginTestCase("GCHandleType.Pinned");
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
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify the Pinned in the GCHandleType Enumerator");
        try
        {
            int myVal = (int)GCHandleType.Pinned;
            if (myVal != 3)
            {
                TestLibrary.TestFramework.LogError("001", "the Pinned in the GCHandleType ExpectResult is 3 but the ActualResult is  " + myVal);
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
    #endregion
}