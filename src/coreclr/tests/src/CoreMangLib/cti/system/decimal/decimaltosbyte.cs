using System;
using System.Globalization;
/// <summary>
///System.IConvertible.ToSByte(System.IFormatProvider)
/// </summary>
public class DecimalToSByte
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;
        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check a  random Decimal.");

        try
        {
            Decimal i1 = new decimal(TestLibrary.Generator.GetSingle(-55));
            int expectValue = 0;
            if (i1 > 0.5m)
                expectValue = 1;
            else
                expectValue = 0;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            if (actualValue != expectValue)
            {
                TestLibrary.TestFramework.LogError("001.1", "ToSByte  should return " + expectValue);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Check a Decimal which is   sbyte.MaxValue and  sbyte.MinValue.");

        try
        {
            Decimal i1 = sbyte.MaxValue;
            CultureInfo myCulture =CultureInfo.InvariantCulture;
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            if (actualValue != sbyte.MaxValue)
            {
                TestLibrary.TestFramework.LogError("002.1", "ToSByte  return failed. ");
                retVal = false;
            }

            i1 = sbyte.MinValue;
            actualValue = ((IConvertible)i1).ToSByte(myCulture);
            if (actualValue != sbyte.MinValue)
            {
                TestLibrary.TestFramework.LogError("002.2", "ToSByte  return failed. ");
                retVal = false;
            }

        }

        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

  
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Check a Decimal which is  >SByte.MaxValue.");

        try
        {

            Decimal i1 = SByte.MaxValue + 1.0m;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            TestLibrary.TestFramework.LogError("101.1", "ToSByte  return failed. ");
            retVal = false;


        }
        catch (OverflowException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: Check a Decimal which is  <SByte.MinValue.");

        try
        {
            Decimal i1 = SByte.MinValue - 1.0m;
            CultureInfo myCulture = CultureInfo.InvariantCulture;
            sbyte actualValue = ((IConvertible)i1).ToSByte(myCulture);
            TestLibrary.TestFramework.LogError("102.1", "ToSByte  return failed. ");
            retVal = false;


        }
        catch (OverflowException)
        {

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102.2", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #endregion

    public static int Main()
    {
        DecimalToSByte test = new DecimalToSByte();

        TestLibrary.TestFramework.BeginTestCase("DecimalToSByte");

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
