// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// ctor(System.Int32,System.Int16,System.Int16,System.Byte,System.Byte,System.Byte,System.Byte,System.Byte,System.Byte,System.Byte,System.Byte)
/// </summary>
public class GuidCtor2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor with all parameter set to zero");

        try
        {
            Guid guid = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        int a = 0;
        short b = 0;
        short c = 0;
        byte d = 0;
        byte e = 0;
        byte f = 0;
        byte g = 0;
        byte h = 0;
        byte i = 0;
        byte j = 0;
        byte k = 0;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ctor with all parameter set to zero");

        try
        {
            a = TestLibrary.Generator.GetInt32(-55);
            b = TestLibrary.Generator.GetInt16(-55);
            c = TestLibrary.Generator.GetInt16(-55);
            d = TestLibrary.Generator.GetByte(-55);
            e = TestLibrary.Generator.GetByte(-55);
            f = TestLibrary.Generator.GetByte(-55);
            g = TestLibrary.Generator.GetByte(-55);
            h = TestLibrary.Generator.GetByte(-55);
            i = TestLibrary.Generator.GetByte(-55);
            j = TestLibrary.Generator.GetByte(-55);
            k = TestLibrary.Generator.GetByte(-55);

            Guid guid = new Guid(a, b, c, d, e, f, g, h, i, j, k);
        }
        catch (Exception ex)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + ex);
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] a = " + a + ", b = " + b + ", c = " + c + ", d = " + d + ", e = " + e + ", f = " + f + ", g = " + g + ", i = " + i + ", j = " + j + ", k = " + k);
            TestLibrary.TestFramework.LogInformation(ex.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GuidCtor2 test = new GuidCtor2();

        TestLibrary.TestFramework.BeginTestCase("GuidCtor2");

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
