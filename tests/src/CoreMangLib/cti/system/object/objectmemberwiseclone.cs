// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public struct TestStruct
{
    public int Int32;
    public long Long;
    public double Double;
    public float Float;
}

public class ObjectMemberwiseClone
{
    #region Private Fields
    private string m_String = null;
    private int m_Int32 = 0;
    private TestStruct m_TestStruct;

    private const int c_MIN_STRING_LENGTH = 0;
    private const int c_MAX_STRING_LENGHT = 1024;
    #endregion

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

        TestLibrary.TestFramework.BeginScenario("PosTest1: MemberwiseClone do a bit-by-bit copy of a value type field");

        try
        {
            ObjectMemberwiseClone omc1 = new ObjectMemberwiseClone();
            omc1.m_Int32 = TestLibrary.Generator.GetInt32(-55);

            ObjectMemberwiseClone omc2 = (ObjectMemberwiseClone)omc1.MemberwiseClone();
            if (omc2.m_Int32 != omc1.m_Int32)
            {
                TestLibrary.TestFramework.LogError("001", "MemberwiseClone does not do a bit-by-bit copy of a value type field");
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: MemberwiseClone copies reference for a reference type field");

        try
        {
            ObjectMemberwiseClone omc1 = new ObjectMemberwiseClone();
            omc1.m_String = TestLibrary.Generator.GetString(-55, 
                false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGHT);

            ObjectMemberwiseClone omc2 = (ObjectMemberwiseClone)omc1.MemberwiseClone();
            if (!Object.ReferenceEquals(omc1.m_String, omc2.m_String))
            {
                TestLibrary.TestFramework.LogError("003", "MemberwiseClone does not copy reference for a reference type field");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: MemberwiseClone do a bit-by-bit copy of a struct type field");

        try
        {
            ObjectMemberwiseClone omc1 = new ObjectMemberwiseClone();
            omc1.m_TestStruct.Int32 = TestLibrary.Generator.GetInt32(-55);
            omc1.m_TestStruct.Long = TestLibrary.Generator.GetInt64(-55);
            omc1.m_TestStruct.Double = TestLibrary.Generator.GetDouble(-55);
            omc1.m_TestStruct.Float = TestLibrary.Generator.GetSingle(-55);

            ObjectMemberwiseClone omc2 = (ObjectMemberwiseClone)omc1.MemberwiseClone();
            if ((omc2.m_TestStruct.Int32 != omc1.m_TestStruct.Int32) ||
                 (omc2.m_TestStruct.Long != omc1.m_TestStruct.Long) ||
                 (omc2.m_TestStruct.Double != omc1.m_TestStruct.Double) ||
                 (omc2.m_TestStruct.Float != omc1.m_TestStruct.Float))
            {
                TestLibrary.TestFramework.LogError("005", "MemberwiseClone does not do a bit-by-bit copy of a value type field");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ObjectMemberwiseClone test = new ObjectMemberwiseClone();

        TestLibrary.TestFramework.BeginTestCase("ObjectMemberwiseClone");

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
