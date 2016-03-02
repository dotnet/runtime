// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class GuidCtor3
{
    private const int c_MIN_RANGE   = 64;
    private const int c_MAX_RANGE   = 128;
    private const int c_DELTA_RANGE = 55;

    public static int Main()
    {
        GuidCtor3 ac = new GuidCtor3();

        TestLibrary.TestFramework.BeginTestCase("GuidCtor3");

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
        int    a;
        short  b;
        short  c;
        byte[] d;
        byte[] bAfter;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Guid.Ctor(int, short, short, byte[])");

        try
        {
            d = new byte[8];

            for(int i=0;i<d.Length; i++) d[i] = TestLibrary.Generator.GetByte(-55);
            a = TestLibrary.Generator.GetInt32(-55);
            b = TestLibrary.Generator.GetInt16(-55);
            c = TestLibrary.Generator.GetInt16(-55);

            g = new Guid(a, b, c, d);

            bAfter = g.ToByteArray();

            if ((byte)(a) != bAfter[0] || (byte)(a >> 8) != bAfter[1] || (byte)(a >> 16) != bAfter[2] || (byte)(a >> 24) != bAfter[3])
            {
                TestLibrary.TestFramework.LogError("000", "Guid byte[0,1,2,3] mismatch:");
                TestLibrary.TestFramework.LogError("001", "Expected: " + (byte)(a) + " " + (byte)(a >> 8) + " " + (byte)(a >> 16) + " " + (byte)(a >> 24));
                TestLibrary.TestFramework.LogError("002", "Actual  : " + bAfter[0] + " " + bAfter[1] + " " + bAfter[2] + " " + bAfter[3]);
                retVal = false;
            }

            if ((byte)(b) != bAfter[4] || (byte)(b >> 8) != bAfter[5])
            {
                TestLibrary.TestFramework.LogError("003", "Guid byte[4,5] mismatch:");
                TestLibrary.TestFramework.LogError("004", "Expected: " + (byte)(b) + " " + (byte)(b >> 8));
                TestLibrary.TestFramework.LogError("005", "Actual  : " + bAfter[4] + " " + bAfter[5]);
                retVal = false;
            }

            if ((byte)(c) != bAfter[6] || (byte)(c >> 8) != bAfter[7])
            {
                TestLibrary.TestFramework.LogError("006", "Guid byte[6,7] mismatch:");
                TestLibrary.TestFramework.LogError("007", "Expected: " + (byte)(c) + " " + (byte)(c >> 8));
                TestLibrary.TestFramework.LogError("008", "Actual  : " + bAfter[6] + " " + bAfter[7]);
                retVal = false;
            }

            for(int i=0;i<d.Length; i++)
            {
                if (d[i] != bAfter[i+8])
                {
                    TestLibrary.TestFramework.LogError("009", "Guid byte["+i+"] mismatch: Exepcted("+d[i]+") Actual("+bAfter[i+8]+")");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool   retVal = true;
        Guid   g;
        int    a;
        short  b;
        short  c;
        byte[] d;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Guid.Ctor(int, short, short, byte[]) pass in null");

        try
        {
            d = null;
            a = TestLibrary.Generator.GetInt32(-55);
            b = TestLibrary.Generator.GetInt16(-55);
            c = TestLibrary.Generator.GetInt16(-55);

            g = new Guid(a, b, c, d);

            TestLibrary.TestFramework.LogError("011", "Exception expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool   retVal = true;
        Guid   g;
        int    a;
        short  b;
        short  c;
        byte[] d;

        TestLibrary.TestFramework.BeginScenario("NegTest2: Guid.Ctor(int, short, short, byte[]) b.Length != 16");

        try
        {
            d = new byte[16];

            for(int i=0;i<d.Length; i++) d[i] = TestLibrary.Generator.GetByte(-55);
            a = TestLibrary.Generator.GetInt32(-55);
            b = TestLibrary.Generator.GetInt16(-55);
            c = TestLibrary.Generator.GetInt16(-55);

            g = new Guid(a, b, c, d );

            TestLibrary.TestFramework.LogError("013", "Exception expected");
            retVal = false;
        }
        catch (ArgumentException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
