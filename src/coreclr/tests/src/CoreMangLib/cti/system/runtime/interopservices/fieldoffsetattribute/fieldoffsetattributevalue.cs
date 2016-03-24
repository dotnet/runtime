// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// FieldOffsetAttribute.Value [v-minch]
/// </summary>
public class FieldOffsetAttributeValue
{
    public static int Main()
    {
        FieldOffsetAttributeValue test = new FieldOffsetAttributeValue();
        TestLibrary.TestFramework.BeginTestCase("FieldOffsetAttribute.Value");
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
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Verify the value of initialized instance of the FieldOffsetAttribute 1");
        try
        {
            int offset = Int32.MaxValue;
            FieldOffsetAttribute myFieldOffsetAttribute = new FieldOffsetAttribute(offset);
            if (myFieldOffsetAttribute.Value != offset)
            {
                TestLibrary.TestFramework.LogError("001", "the initialized FieldOffsetAttribute ExpectedValue is " + offset + " but the ActualValue is " + myFieldOffsetAttribute.Value.ToString());
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Verify the value of initialized instance of the FieldOffsetAttribute 2");
        try
        {
            int offset = 0;
            FieldOffsetAttribute myFieldOffsetAttribute = new FieldOffsetAttribute(offset);
            if (myFieldOffsetAttribute.Value != offset)
            {
                TestLibrary.TestFramework.LogError("003", "the initialized FieldOffsetAttribute ExpectedValue is " + offset + " but the ActualValue is " + myFieldOffsetAttribute.Value.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3:Verify the value of initialized instance of the FieldOffsetAttribute 3");
        try
        {
            int offset = TestLibrary.Generator.GetInt32(-55);
            FieldOffsetAttribute myFieldOffsetAttribute = new FieldOffsetAttribute(offset);
            if (myFieldOffsetAttribute.Value != offset)
            {
                TestLibrary.TestFramework.LogError("005", "the initialized FieldOffsetAttribute ExpectedValue is " + offset + " but the ActualValue is " + myFieldOffsetAttribute.Value.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4:Verify the value of initialized instance of the FieldOffsetAttribute 4");
        try
        {
            int offset = Int32.MinValue;
            FieldOffsetAttribute myFieldOffsetAttribute = new FieldOffsetAttribute(offset);
            if (myFieldOffsetAttribute.Value != offset)
            {
                TestLibrary.TestFramework.LogError("007", "the initialized FieldOffsetAttribute ExpectedValue is " + offset + " but the ActualValue is " + myFieldOffsetAttribute.Value.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion
}
