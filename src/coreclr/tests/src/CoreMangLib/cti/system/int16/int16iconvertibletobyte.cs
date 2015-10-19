using System;

//System.Int16.System.IConvertibleToByte
public class Int16IConvertibleToByte
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: Convert a random int16 to byte ");

        try
        {
            Byte by = TestLibrary.Generator.GetByte(-55);
            Int16 i1 = (Int16)by;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToByte(null) != by)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Convert ByteMaxValue to byte ");

        try
        {
            Int16 i1 = (Int16)Byte.MaxValue;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToByte(null) != Byte.MaxValue)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3:Convert zero to byte ");

        try
        {
            Int16 i1 = 0;
            IConvertible Icon1 = (IConvertible)i1;
            if (Icon1.ToByte(null) != 0)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not zero as expected");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Convert a negative byte int16 to Byte ");

        try
        {
            Int16 i1 = 0;
            while (i1 == 0)
            {
                i1 = (Int16)TestLibrary.Generator.GetByte(-55);
            }
            IConvertible Icon1 = (IConvertible)(-i1);
            Byte b1 = Icon1.ToByte(null);
            TestLibrary.TestFramework.LogError("101", "An overflowException was not thrown as expected");
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Convert int16 to Byte ");

        try
        {
            Int16 i1 = 0;
            while (i1 <= 255)
            {
                i1 = (Int16)TestLibrary.Generator.GetInt16(-55);
            }
            IConvertible Icon1 = (IConvertible)(i1);
            Byte b1 = Icon1.ToByte(null);
            TestLibrary.TestFramework.LogError("103", "An overflowException was not thrown as expected");
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
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
            Int16 i1 = 256;
            IConvertible Icon1 = (IConvertible)(i1);
            Byte b1 = Icon1.ToByte(null);
            TestLibrary.TestFramework.LogError("105", "An overflowException was not thrown as expected");
            retVal = false;
        }
        catch (System.OverflowException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        Int16IConvertibleToByte test = new Int16IConvertibleToByte();

        TestLibrary.TestFramework.BeginTestCase("Int16IConvertibleToByte");

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
