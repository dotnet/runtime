// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public struct Empty
{
}

public class ObjectGetHashCode
{
    #region Private Variables
    private string m_String = null;
    private int m_Int32 = 0;
    #endregion

    #region Private Constaints
    private const int c_MIN_STRING_LENGTH = 1;
    private const int c_MAX_STRING_LENGHT = 1024;
    #endregion

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Hash code for two objects with same type and same value should be equal");

        try
        {
            ObjectGetHashCode obj1 = new ObjectGetHashCode();
            ObjectGetHashCode obj2 = new ObjectGetHashCode();

            if (obj1.GetHashCode() == obj2.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("001", "Hash code for two objects with same type and same value are not equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Hash function must return exactly the same value regardless of any changes that are made to the object");

        try
        {
            // Hack: Implement of ObjectGetHashCode.GetHashCode is inheritted from Object
            String str1 = TestLibrary.Generator.GetString(-55, 
                false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGHT);
            ObjectGetHashCode oghc = new ObjectGetHashCode();
            oghc.m_String = str1; 
            int hashCode1 = oghc.GetHashCode();

            String str2 = TestLibrary.Generator.GetString(-55, 
                false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGHT);

            if (str1.Length == str2.Length)
            {
                str2 += TestLibrary.Generator.GetChar(-55);
            }

            oghc.m_String = str2;
            int hashCode2 = oghc.GetHashCode();

            if (hashCode1 != hashCode2)
            {
                TestLibrary.TestFramework.LogError("003", "Hash function does not return exactly the same value regardless of any changes that are made to the object");
                retVal = false;
            }

            // Change value type field should not change the hash code
            oghc.m_Int32 = TestLibrary.Generator.GetInt32(-55);
            hashCode2 = oghc.GetHashCode();

            if (hashCode1 != hashCode2)
            {
                TestLibrary.TestFramework.LogError("004", "Hash function does not return exactly the same value regardless of any changes that are made to the object");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: Get hash code for an empty struct");

        try
        {
            Empty test = new Empty();
            Empty test1 = new Empty();

            if (test.GetHashCode() != test1.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("101", "Hash code for two Empty struct instances does not equal");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ObjectGetHashCode test = new ObjectGetHashCode();

        TestLibrary.TestFramework.BeginTestCase("ObjectGetHashCode");

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
