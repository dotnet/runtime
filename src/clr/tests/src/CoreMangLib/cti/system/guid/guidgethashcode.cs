// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

/// <summary>
/// GetHashCode
/// </summary>
public class GuidGetHashCode
{
    #region Private Fields
    private const int c_SIZE_OF_ARRAY = 16;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: GetHashCode should return the same value for two equal instances");

        try
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            TestLibrary.Generator.GetBytes(-55, bytes);

            Guid guid1 = new Guid(bytes);
            Guid guid2 = new Guid(bytes);

            int hashCode1 = guid1.GetHashCode();
            int hashCode2 = guid2.GetHashCode();

            if (hashCode1 != hashCode2)
            {
                TestLibrary.TestFramework.LogError("001.1", "GetHashCode does not return the same value for two equal instances");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] hashCode1 = " + hashCode1 + ", hashCode2 = " + hashCode2);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Calling GetHashCode multiple times should return the same hash value");

        try
        {
            byte[] bytes = new byte[c_SIZE_OF_ARRAY];
            TestLibrary.Generator.GetBytes(-55, bytes);

            Guid guid = new Guid(bytes);

            int hashCode1 = guid.GetHashCode();
            int hashCode2 = guid.GetHashCode();

            if (hashCode1 != hashCode2)
            {
                TestLibrary.TestFramework.LogError("002.1", "Calling GetHashCode multiple times does not return the same hash value");
                TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLES] hashCode1 = " + hashCode1 + ", hashCode2 = " + hashCode2);
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        GuidGetHashCode test = new GuidGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("GuidGetHashCode");

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
