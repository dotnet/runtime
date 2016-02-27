// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;


/// <summary>
/// System.Byte,Equals(System.Object)
/// </summary>
public class ByteEquals2
{
    public static int Main(string[] args)
    {
        ByteEquals2 equal2 = new ByteEquals2();
        TestLibrary.TestFramework.BeginTestCase("Testing System.Byte.Equals(System.Object)...");

        if (equal2.RunTests())
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
        retVal = NegTest1() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byte1 is less than byte2...");

        try
        {
            Byte byte1 = 20;
            object byte2 = (Byte)30;

            if (byte1.Equals(byte2))
            {
                TestLibrary.TestFramework.LogError("001", "The compare result is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byte1 is larger than byte2...");

        try
        {
            Byte byte1 = 30;
            object byte2 = (Byte)20;

            if (byte1.Equals(byte2))
            {
                TestLibrary.TestFramework.LogError("003", "The compare result is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify byte1 is equal to byte2...");

        try
        {
            Byte byte1 = 20;
            object byte2 = (Byte)20;

            if (!byte1.Equals(byte2))
            {
                TestLibrary.TestFramework.LogError("005", "The compare result is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Byte instance compare to itself...");

        try
        {
            Byte byte1 = 20;

            if (!byte1.Equals((object)byte1))
            {
                TestLibrary.TestFramework.LogError("007", "The result of compare is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify Byte instance compare to Int32 instance when they have the same values...");

        try
        {
            Byte byte1 = 20;
            Int32 int1 = 20;

            if (byte1.Equals((object)int1))
            {
                TestLibrary.TestFramework.LogError("009", "The compare result is wrong!");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
