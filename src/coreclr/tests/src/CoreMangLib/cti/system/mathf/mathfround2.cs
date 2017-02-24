// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// System.MathF.Pow(System.Single, System.Int32)
/// </summary>
public class MathFRound2
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify Round(f, 0)");

        try
        {
            int i = 0;
            if (MathF.Round(3.4f, i) != 3 || MathF.Round(3.5f, i) != 4 || MathF.Round(3.6f, i) != 4)
            {
                TestLibrary.TestFramework.LogError("001.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify Round(f, 1)");

        try
        {
            if (MathF.Round(3.44f, 1) != 3.4f || MathF.Round(3.45f, 1) != 3.4f || MathF.Round(3.46f, 1) != 3.5f)
            {
                TestLibrary.TestFramework.LogError("002.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify Round(f, 6)");

        try
        {
            float f1 = 1234.56784f;
            float expectedResult1 = 1234.5678f;

            float f2 = 1234.56785f;
            float expectedResult2 = 1234.5679f;

            float f3 = 1234.56786f;
            float expectedResult3 = 1234.5679f;

            int i = 6;

            if (MathF.Round(f1, i).ToString() != expectedResult1.ToString()
             || MathF.Round(f2, i).ToString() != expectedResult2.ToString()
             || MathF.Round(f3, i).ToString() != expectedResult3.ToString())
            {
                TestLibrary.TestFramework.LogError("003.1", "Return value is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.2", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentOutOfRangeException is not thrown.");

        try
        {
            float f = MathF.Round(3.45f, -1);
            TestLibrary.TestFramework.LogError("101.1", " OverflowException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: ArgumentOutOfRangeException is not thrown.");

        try
        {
            float f = MathF.Round(12345.6789f, 8);
            TestLibrary.TestFramework.LogError("102.1", " OverflowException is not thrown.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        MathFRound2 test = new MathFRound2();

        TestLibrary.TestFramework.BeginTestCase("MathFRound4");

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
