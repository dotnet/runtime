// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// Convert.ToUInt32(DateTime)
/// </summary>
public class ConvertToUInt326
{
    public static int Main()
    {
        ConvertToUInt326 convertToUInt326 = new ConvertToUInt326();

        TestLibrary.TestFramework.BeginTestCase("ConvertToUInt326");
        if (convertToUInt326.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        return retVal;
    }
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert to UInt32 from DateTime");
        try
        {
            DateTime dtVal = DateTime.Now;
            uint uintVal = Convert.ToUInt32(dtVal);
            TestLibrary.TestFramework.LogError("N001", "Convert to UInt32 from DateTime but not throw exception");
            retVal = false;
        }
        catch (InvalidCastException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
