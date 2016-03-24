// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System.Security;
using System;
using System.IO;

[SecuritySafeCritical]
public class ConsoleSetError
{
    #region Private Fields
    private const string c_TEST_TXT_FILE = "ConsoleSetError.txt";
    private const int c_MIN_STRING_LENGTH = 1;
    private const int c_MAX_STRING_LENGTH = 256;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;
        TextWriter saved = Console.Error;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call SetError to set a new standard output");

        try
        {
            string randValue = TestLibrary.Generator.GetString(-55, true, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

            using (TextWriter reader = new StreamWriter(new FileStream(c_TEST_TXT_FILE, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                Console.SetError(reader);

                Console.Error.WriteLine(randValue);
            }

            using (TextReader verifyReader = new StreamReader(new FileStream(c_TEST_TXT_FILE, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                string actual = verifyReader.ReadLine();

                if (actual != randValue)
                {
                    TestLibrary.TestFramework.LogError("001.0", "Call SetError failed to set a new standard output");
                    TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] actual = " + actual + ", randValue = " + randValue);
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }
        finally
        {
            Console.SetError(saved);
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentNullException should be thrown when newOut is a null reference");

        try
        {
            Console.SetError(null);

            TestLibrary.TestFramework.LogError("101.1", "ArgumentNullException is not thrown when newOut is a null reference");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ConsoleSetError test = new ConsoleSetError();

        TestLibrary.TestFramework.BeginTestCase("ConsoleSetError");

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
