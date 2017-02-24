// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Atan2(System.Single, System.Single)
/// </summary>
public class MathFAtan2
{
    public static int Main(string[] args)
    {
        MathFAtan2 arctan2 = new MathFAtan2();
        TestLibrary.TestFramework.BeginTestCase("Testing System.MathF.Atan2(System.Single,System.Single)...");

        if (arctan2.RunTests())
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
        retVal = PosTest5() && retVal;
        retVal = PosTest6() && retVal;
        retVal = PosTest7() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the angle of arctan(x,y) when the point in quadrant one...");

        try
        {
            float x = TestLibrary.Generator.GetSingle(-55);
            while (x <= 0)
            {
                x = TestLibrary.Generator.GetSingle(-55);
            }
            float y = TestLibrary.Generator.GetSingle(-55);
            while (y <= 0)
            {
                y = TestLibrary.Generator.GetSingle(-55);
            }

            float angle = MathF.Atan2(y, x);
            if (angle > MathF.PI / 2 || angle < 0)
            {
                TestLibrary.TestFramework.LogError("001", "The angle should be between 0 and MathF.PI/2, actual: " +
                    angle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the angle of arctan(x,y) when the point in quadrant four...");

        try
        {
            float x = TestLibrary.Generator.GetSingle(-55);
            while (x <= 0)
            {
                x = TestLibrary.Generator.GetSingle(-55);
            }
            float y = TestLibrary.Generator.GetSingle(-55);
            while (y >= 0)
            {
                y = -TestLibrary.Generator.GetSingle(-55);
            }

            float angle = MathF.Atan2(y, x);
            if (angle > 0 || angle < -MathF.PI / 2)
            {
                TestLibrary.TestFramework.LogError("003", "The angle should be between 0 and -MathF.PI/2, actual: " +
                    angle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the angle of arctan(x,y) when the point in forward direction of X axis...");

        try
        {
            float x = TestLibrary.Generator.GetSingle(-55);
            while (x <= 0)
            {
                x = TestLibrary.Generator.GetSingle(-55);
            }

            float y = 0;
            float angle = MathF.Atan2(y, x);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, 0.0f))
            {
                TestLibrary.TestFramework.LogError("005", "The angle should be zero,actual: " + angle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest4: Verify the angle of arctan(x,y) when the point in forward direction of Y axis...");

        try
        {
            float x = 0;
            float y = TestLibrary.Generator.GetSingle(-55);
            while (y <= 0)
            {
                y = TestLibrary.Generator.GetSingle(-55);
            }
            float angle = MathF.Atan2(y, x);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, MathF.PI / 2))
            {
                TestLibrary.TestFramework.LogError("007", "The angle should be pi/2, actual:" + angle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest5: Verify the angle of arctan(x,y) when the point in negative direction of Y axis...");

        try
        {
            float x = 0;
            float y = TestLibrary.Generator.GetSingle(-55);
            while (y >= 0)
            {
                y = -TestLibrary.Generator.GetSingle(-55);
            }
            float angle = MathF.Atan2(y, x);
            if (!MathFTestLib.SingleIsWithinEpsilon(angle, -MathF.PI / 2))
            {
                TestLibrary.TestFramework.LogError("009", "The angle should be -pi/2, actual: " + angle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest6()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest6: Verify the angle of arctan(x,y) when the point in quadrant two...");

        try
        {
            float x = TestLibrary.Generator.GetSingle(-55);
            while (x >= 0)
            {
                x = -TestLibrary.Generator.GetSingle(-55);
            }
            float y = TestLibrary.Generator.GetSingle(-55);
            while (y <= 0)
            {
                y = TestLibrary.Generator.GetSingle(-55);
            }

            float angle = MathF.Atan2(y, x);
            if (angle < MathF.PI / 2 || angle > MathF.PI)
            {
                TestLibrary.TestFramework.LogError("011", "The angle should be between 0 and MathF.PI/2, actual: " + angle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest7()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest7: Verify the angle of arctan(x,y) when the point in quadrant three...");

        try
        {
            float x = TestLibrary.Generator.GetSingle(-55);
            while (x >= 0)
            {
                x = -TestLibrary.Generator.GetSingle(-55);
            }
            float y = TestLibrary.Generator.GetSingle(-55);
            while (y >= 0)
            {
                y = -TestLibrary.Generator.GetSingle(-55);
            }

            float angle = MathF.Atan2(y, x);
            if (angle > -MathF.PI / 2 || angle < -MathF.PI)
            {
                TestLibrary.TestFramework.LogError("013", "The angle should be between 0 and MathF.PI/2, actual: " +
                    angle.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
