// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class GuidCtor1
{
    public static int Main()
    {
        GuidCtor1 ac = new GuidCtor1();

        TestLibrary.TestFramework.BeginTestCase("GuidCtor1");

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

        TestLibrary.TestFramework.LogInformation("");
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool   retVal = true;
        Guid   g;
        byte[] b;
        byte[] bAfter;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Guid.Ctor(byte[] b)");

        try
        {
            b = new byte[16];

            for(int i=0;i<b.Length; i++) b[i] = TestLibrary.Generator.GetByte(-55);

            g = new Guid(b);

            bAfter = g.ToByteArray();

            for(int i=0;i<b.Length; i++)
            {
                if (b[i] != bAfter[i])
                {
                    TestLibrary.TestFramework.LogError("000", "Guid byte["+i+"] mismatch: Exepcted("+b[i]+") Actual("+bAfter[i]+")");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool   retVal = true;
        Guid   g;
        byte[] b;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Guid.Ctor(byte[] b) pass in null");

        try
        {
            b = null;

            g = new Guid(b);

            TestLibrary.TestFramework.LogError("002", "Exception expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool   retVal = true;
        Guid   g;
        byte[] b;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Guid.Ctor(byte[] b) b.Length != 16");

        try
        {
            b = new byte[19];

            for(int i=0;i<b.Length; i++) b[i] = TestLibrary.Generator.GetByte(-55);

            g = new Guid(b);

            TestLibrary.TestFramework.LogError("004", "Exception expected");
            retVal = false;
        }
        catch (ArgumentException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
