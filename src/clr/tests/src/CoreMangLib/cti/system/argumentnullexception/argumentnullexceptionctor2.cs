using System;
using TestLibrary;

/// <summary>
/// System.ArgumentNullException.Ctor(String)
/// </summary>
public class ArgumentNullExceptionCtor2
{
    private const int c_MIN_STRING_LENGTH = 1;
    private const int c_MAX_STRING_LENGTH = 256;

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call ctor with a random string argument to construct a new instance");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            ArgumentNullException argumentNullException = new ArgumentNullException(randValue);
            if (argumentNullException == null)
            {
                TestLibrary.TestFramework.LogError("001", "argumentNullException is null");
                retVal = false;
            }

            string expectedWindows = "Value cannot be null.\r\nParameter name: " + randValue;
            string expectedMac = "Value cannot be null.\nParameter name: " + randValue;

            if (!argumentNullException.Message.Contains("[ArgumentNull_Generic]"))
            {
                if (!Utilities.IsWindows)
                {
                    if (argumentNullException.Message != expectedMac)
                    {
                        TestLibrary.TestFramework.LogError("002", "The result is not the value as expected");
                        TestLibrary.TestFramework.LogInformation("Expected: " + expectedMac + "; Actual: " + argumentNullException.Message);
                        retVal = false;
                    }
                }
                else if (argumentNullException.Message != expectedWindows)
                {
                    TestLibrary.TestFramework.LogError("002", "The result is not the value as expected");
                    TestLibrary.TestFramework.LogInformation("Expected: " + expectedWindows + "; Actual: " + argumentNullException.Message);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call ctor with a null argument to construct a new instance");

        try
        {
            string randValue = null;
            ArgumentNullException argumentNullException = new ArgumentNullException(randValue);
            
            if (argumentNullException == null)
            {
                TestLibrary.TestFramework.LogError("004", "argumentNullException is null");
                retVal = false;
            }
            string expected = "Value cannot be null.";
            if ((argumentNullException.Message != expected) &
                (!argumentNullException.Message.Contains("[ArgumentNull_Generic]")))
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
                TestLibrary.TestFramework.LogInformation("Expected: " + expected + "; Actual: " + argumentNullException.Message);
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call ctor with stringEmpty to construct a new instance");

        try
        {
            string randValue = String.Empty;
            ArgumentNullException argumentNullException = new ArgumentNullException(randValue);
            if (argumentNullException == null)
            {
                TestLibrary.TestFramework.LogError("007", "argumentNullException is null");
                retVal = false;
            }
            string expected = "Value cannot be null.";
            if ((argumentNullException.Message != expected) &
                (!argumentNullException.Message.Contains("[ArgumentNull_Generic]")))
            {
                TestLibrary.TestFramework.LogError("008", "The result is not the value as expected");
                TestLibrary.TestFramework.LogInformation("Expected: " + expected + "; Actual: " + argumentNullException.Message);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    #endregion
    #endregion

    public static int Main()
    {
        ArgumentNullExceptionCtor2 test = new ArgumentNullExceptionCtor2();

        TestLibrary.TestFramework.BeginTestCase("ArgumentNullExceptionCtor2");

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
