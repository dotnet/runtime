// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
/// <summary>
/// System.InvalidCastException.ctor(String,Exception) [v-minch]
/// </summary>
public class InvalidCastExceptionctor3
{
    public static int Main()
    {
        InvalidCastExceptionctor3 invalidCastExceptionctor3 = new InvalidCastExceptionctor3();
        TestLibrary.TestFramework.BeginTestCase("InvalidCastExceptionctor3");
        if (invalidCastExceptionctor3.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1:Initialize the InvalidCastException instance with message and InnerException 1");
        try
        {
            string message = "HelloWorld";
            ArgumentException innerException = new ArgumentException();
            InvalidCastException myException = new InvalidCastException(message, innerException);
            if (myException == null || myException.Message != message || !myException.InnerException.Equals(innerException))
            {
                TestLibrary.TestFramework.LogError("001", "the InvalidCastException with message and innerException instance creating failed");
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
        TestLibrary.TestFramework.BeginScenario("PosTest2:Initialize the InvalidCastException instance with message and InnerException 2");
        try
        {
            string message = null;
            ArgumentException innerException = new ArgumentException();
            InvalidCastException myException = new InvalidCastException(message, innerException);
            if (myException == null || myException.Message == null || !myException.InnerException.Equals(innerException))
            {
                TestLibrary.TestFramework.LogError("003", "Initialize the InvalidCastException instance with null message not succeed");
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
        TestLibrary.TestFramework.BeginScenario("PosTest3:Initialize the InvalidCastException instance with message and InnerException 3");
        try
        {
            string message = null;
            InvalidCastException myException = new InvalidCastException(message, null);
            if (myException == null || myException.Message == null || myException.InnerException != null)
            {
                TestLibrary.TestFramework.LogError("005", "Initialize the InvalidCastException instance with null message and null InnerException not succeed");
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
    #endregion
}