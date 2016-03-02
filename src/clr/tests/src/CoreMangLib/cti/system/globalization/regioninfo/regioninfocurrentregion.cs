// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
/// <summary>
/// RegionInfo.CurrentRegion
/// </summary>
public class RegionInfoCurrentRegion
{
    public static int Main()
    {
        RegionInfoCurrentRegion regInfoCurrentRegion = new RegionInfoCurrentRegion();
        TestLibrary.TestFramework.BeginTestCase("RegionInfoCurrentRegion");
        if (regInfoCurrentRegion.RunTests())
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
        TestLibrary.TestFramework.BeginScenario("PosTest1:Return the CurrentRegion property in RegionInfo class");
        try
        {
            RegionInfo regionInfo = RegionInfo.CurrentRegion;

            if (!regionInfo.Equals(new RegionInfo(TestLibrary.Utilities.CurrentCulture.Name))) 
            {
				TestLibrary.TestFramework.LogError("001", "the ExpectResult is RegionInfo about " + TestLibrary.Utilities.CurrentCulture.Name + " but the ActualResult is " + regionInfo.ToString());
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