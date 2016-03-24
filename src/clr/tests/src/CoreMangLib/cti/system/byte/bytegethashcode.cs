// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;


/// <summary>
/// System.Byte.GetHashCode
/// </summary>
public class ByteGetHashCode
{
    public static int Main(string[] args)
    {
        ByteGetHashCode hashCode = new ByteGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.GetHashCode()...");

        if (hashCode.RunTests())
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

        TestLibrary.TestFramework.LogInformation("[Negtive]");

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify hashCode of two Byte instance should be different...");

        try
        {
            Byte byte1;
            Byte byte2;
            do
            {
                byte1 = TestLibrary.Generator.GetByte(-55);
                byte2 = TestLibrary.Generator.GetByte(-55);
            }while(byte1 == byte2);

            if (byte1.GetHashCode() == byte2.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("001","The hashcode should be different!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexcepted exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify hashCode should be changed when value is modified...");

        try
        {
            Byte byte1 = TestLibrary.Generator.GetByte(-55);
            int hashcode1 = byte1.GetHashCode();
            //Don't generate a second random number, since the new one could have the same hashcode.
            byte1 = (byte)(byte1 + 1);
            int hashcode2 = byte1.GetHashCode();

            if (hashcode1 == hashcode2)
            {
                TestLibrary.TestFramework.LogError("003","The hashcode should not be equal!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify hashcode when two instance have the same reference...");

        try
        {
            Byte byte1 = TestLibrary.Generator.GetByte(-55);
            Byte byte2 = byte1;
            int hashcode1 = byte1.GetHashCode();
            int hashcode2 = byte2.GetHashCode();

            if (hashcode1 != hashcode2)
            {
                TestLibrary.TestFramework.LogError("005","The hashcode should be equal!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginTestCase("Verify hashcode when create Byte instance assigned to null...");

        try
        {
            Byte? byte1 = null;
            int hashcode = byte1.GetHashCode();

            if (hashcode != 0)
            {
                TestLibrary.TestFramework.LogError("007","The hashcode is not zero!");
                retVal = false;
            }
            
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
