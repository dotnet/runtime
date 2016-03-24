// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

public class ConvertToBase64String2
{
    private const int c_MIN_SIZE   = 64;
    private const int c_MAX_SIZE   = 1024;

    public static int Main()
    {
        ConvertToBase64String2 ac = new ConvertToBase64String2();

        TestLibrary.TestFramework.BeginTestCase("ToBase64String(byte[])");

        if (ac.RunTests())
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

        TestLibrary.TestFramework.LogInformation("");

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }
                                          //id inArray exception
    public bool NegTest1() { return NegTest(1, null,   typeof(System.ArgumentNullException)); }

    public bool PosTest1()
    {
        bool   retVal = true;
        byte[] array;
        string str;

        TestLibrary.TestFramework.BeginScenario("PosTest1: ToBase64String(byte[])");

        try
        {
            array    = new byte[ (TestLibrary.Generator.GetInt32(-55) % c_MAX_SIZE) + c_MIN_SIZE ];

            // fill the array
            TestLibrary.Generator.GetBytes(-55, array);
            
            str = Convert.ToBase64String(array);
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("000", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool   retVal = true;
        byte[] array;
        string str;

        TestLibrary.TestFramework.BeginScenario("PosTest2: ToBase64String(byte[]) zero length");

        try
        {
            array = new byte[ 0 ];
            str   = Convert.ToBase64String(array);

            if (String.Empty != str)
            {
                TestLibrary.TestFramework.LogError("001", "In array length mismatch: Expected(String.Empty) Actual(" + str + ")");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest(int id, byte[] inArray, Type exception)
    {
        bool            retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest"+id+": ToBase64String(byte[]) (ex:"+exception+")");

        try
        {
            Convert.ToBase64String(inArray);

            TestLibrary.TestFramework.LogError("003", "Exception expected");
            retVal = false;
        }
        catch (Exception e)
        {
            if (exception != e.GetType())
            {
                TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
                retVal = false;
            }
        }

        return retVal;
    }
}
