using System;

/// <summary>
/// DoubleIConvertibleToByte
/// </summary>

public class DoubleIConvertibleToByte
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        // Add your scenario description here
        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random Double( <= 0.5 ) to byte");

        try
        {
            Double i1;
            do
                i1 = (Double)TestLibrary.Generator.GetDouble(-55);
            while (i1 > 0.5D);

            IConvertible Icon1 = (IConvertible)i1;

            if (Icon1.ToByte(null) != 0)
            {
                TestLibrary.TestFramework.LogError("001.1", "The result is not the value as expected");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert a random Double( > 0.5 ) to byte");

        try
        {
            Double i1;
            do
                i1 = (Double)TestLibrary.Generator.GetDouble(-55);
            while (i1 <= 0.5D);

            IConvertible Icon1 = (IConvertible)i1;

            if (Icon1.ToByte(null) != 1)
            {
                TestLibrary.TestFramework.LogError("002.1", "The result is not the value as expected");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.1", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Convert ByteMaxValue");

        try
        {
            Double i1 = (Double)Byte.MaxValue;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToByte(null) != i1)
            {
                TestLibrary.TestFramework.LogError("003.1", "The result is not the value as expected");
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

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Convert zero to byte ");

        try
        {
            Double i1 = 0;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToByte(null) != 0)
            {
                TestLibrary.TestFramework.LogError("004.1", "The result is not zero as expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert a negative byte Double to Byte ");

        try
        {
            Double i1 = 0;
            while (i1 == 0)
                i1 = (Double)TestLibrary.Generator.GetByte(-55);

            IConvertible Icon1 = (IConvertible)(-i1);
            Byte b1 = Icon1.ToByte(null);
            TestLibrary.TestFramework.LogError("101.1", "An overflowException was not thrown as expected");
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Convert Double to Byte ");

        try
        {
            Int32 i1 = 0;
            while (i1 <= 255)
                i1 = (Int32)TestLibrary.Generator.GetInt32(-55);

            IConvertible Icon1 = (IConvertible)((Double)i1);
            Byte b1 = Icon1.ToByte(null);
            TestLibrary.TestFramework.LogError("102.1", "An overflowException was not thrown as expected");
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: Check the border value ");

        try
        {
            Double i1 = 256D;
            IConvertible Icon1 = (IConvertible)(i1);
            Byte b1 = Icon1.ToByte(null);
            TestLibrary.TestFramework.LogError("103.1", "An overflowException was not thrown as expected");
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("103.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        DoubleIConvertibleToByte test = new DoubleIConvertibleToByte();

        TestLibrary.TestFramework.BeginTestCase("DoubleIConvertibleToByte");

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
