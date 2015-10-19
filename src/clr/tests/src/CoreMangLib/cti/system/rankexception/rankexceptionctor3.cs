using System;

/// <summary>
/// RankException constructor(string, Exception) [v-yaduoj]
/// </summary>
public class RankExceptionCtor
{
    public static int Main()
    {
        RankExceptionCtor testObj = new RankExceptionCtor();

        TestLibrary.TestFramework.BeginTestCase("for RankException(string, Exception)");
        if (testObj.RunTests())
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

    #region Positive tests
    public bool PosTest1()
    {
        bool retVal = true;

        const string c_TEST_ID = "P001";
        string c_TEST_DESC = "PosTest1: initialize an instance of type RankException using an emtpy string message";
        string errorDesc;

        string message = string.Empty;
        Exception innerException = new ArgumentNullException();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            RankException e = new RankException(message, innerException);
            if (null == e || e.Message != message || e.InnerException != innerException)
            {
                errorDesc = "Failed to initialize an instance of type RankException.";
                errorDesc += "\nInput message is emtpy string";
                errorDesc += "\nInner exception is " + innerException;
                TestLibrary.TestFramework.LogError("001" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nInput message is emtpy string";
            errorDesc += "\nInner exception is " + innerException;
            TestLibrary.TestFramework.LogError("002" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        const string c_TEST_ID = "P002";
        string c_TEST_DESC = "PosTest2: initialize an instance of type RankException using a string containing special character";
        string errorDesc;

        string message = "Not supported exception occurs here \n\r\0\t\v";
        Exception innerException = new Exception();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            RankException e = new RankException(message, innerException);
            if (null == e || e.Message != message || e.InnerException != innerException)
            {
                errorDesc = "Failed to initialize an instance of type RankException.";
                errorDesc += "\nInput message is \"" + message + "\"";
                errorDesc += "\nInner exception is " + innerException;
                TestLibrary.TestFramework.LogError("003" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nInput message is \"" + message + "\"";
            errorDesc += "\nInner exception is " + innerException;
            TestLibrary.TestFramework.LogError("004" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        const string c_TEST_ID = "P003";
        string c_TEST_DESC = "PosTest3: initialize an instance of type RankException using a null reference";
        string errorDesc;

        string message = null;
        Exception innerException = new Exception();

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            RankException e = new RankException(message, innerException);
            if (null == e)
            {
                errorDesc = "Failed to initialize an instance of type RankException.";
                errorDesc += "\nInput message is a null reference.";
                errorDesc += "\nInner exception is " + innerException;
                TestLibrary.TestFramework.LogError("005" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nInput message is a null reference.";
            errorDesc += "\nInner exception is " + innerException;
            TestLibrary.TestFramework.LogError("006" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        const string c_TEST_ID = "P004";
        string c_TEST_DESC = "PosTest4: message is a string containing special character and inner exception is a null reference";
        string errorDesc;

        string message = "Not supported exception occurs here";
        Exception innerException = null;

        TestLibrary.TestFramework.BeginScenario(c_TEST_DESC);
        try
        {
            RankException e = new RankException(message, innerException);
            if (null == e || e.Message != message || e.InnerException != innerException)
            {
                errorDesc = "Failed to initialize an instance of type RankException.";
                errorDesc += "\nInput message is \"" + message + "\"";
                errorDesc += "\nInner exception is a null reference.";
                TestLibrary.TestFramework.LogError("007" + " TestId-" + c_TEST_ID, errorDesc);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            errorDesc = "Unexpected exception: " + e;
            errorDesc += "\nInput message is \"" + message + "\"";
            errorDesc += "\nInner exception is a null reference.";
            TestLibrary.TestFramework.LogError("008" + " TestId-" + c_TEST_ID, errorDesc);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
