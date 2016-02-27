// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class GuidEquals1
{
    public static int Main()
    {
        GuidEquals1 ac = new GuidEquals1();

        TestLibrary.TestFramework.BeginTestCase("GuidEquals1");

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
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;
        retVal = PosTest5() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool   retVal = true;
        Guid   g1;
        Guid   g2;
        int    a;
        short  b;
        short  c;
        byte[] d;
        bool   compare;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Guid.Equals(object) always equals");

        try
        {
            d = new byte[8];
            for(int i=0;i<d.Length; i++) d[i] = TestLibrary.Generator.GetByte(-55);
            a = TestLibrary.Generator.GetInt32(-55);
            b = TestLibrary.Generator.GetInt16(-55);
            c = TestLibrary.Generator.GetInt16(-55);
            g1 = new Guid(a, b, c, d);

            // equals
            g2 = new Guid(a, b, c, d);

            compare = g1.Equals((object)g2);

            if (!compare)
            {
                TestLibrary.TestFramework.LogError("000", "Guid1: " + g1);
                TestLibrary.TestFramework.LogError("001", "Guid2: " + g2);
                TestLibrary.TestFramework.LogError("002", "Compare failed");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool   retVal = true;
        Guid   g;
        byte[] b;
        bool   compare;
        object obj;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Guid.Equals(object) object is null");

        try
        {
            b = new byte[16];
            for(int i=0;i<b.Length; i++) b[i] = TestLibrary.Generator.GetByte(-55);
            g = new Guid(b);

            obj     = null;
            compare = g.Equals(obj);

            if (compare)
            {
                TestLibrary.TestFramework.LogError("004", "Compare succeed unexpectedly");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("005", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool   retVal = true;
        Guid   g1;
        Guid   g2;
        int    a;
        short  b;
        short  c;
        byte[] d;
        bool   compare;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Guid.Equals(object) always less than");

        try
        {
            for (int i=0; i<11; i++)
            {
                d = new byte[8];
                for(int j=0;j<d.Length; j++) d[j] = (byte)(Math.Abs(TestLibrary.Generator.GetByte(-55) - 1) + 1);
                a = Math.Abs(TestLibrary.Generator.GetInt32(-55) - 1) + 1;
                b = (short)(Math.Abs(TestLibrary.Generator.GetInt16(-55) - 1) + 1);
                c = (short)(Math.Abs(TestLibrary.Generator.GetInt16(-55) - 1) + 1);
                g1 = new Guid(a, b, c, d);

                // less than
                switch (i)
                {
                case 0:
                    g2 = new Guid(a-1, b, c, d);
                    break;
                case 1:
                    g2 = new Guid(a, (short)(b-1), c, d);
                    break;
                case 2:
                    g2 = new Guid(a, b, (short)(c-1), d);
                    break;
                default:
                    d[i-3] = (byte)(d[i-3] - 1);
                    g2 = new Guid(a, b, c, d);
                    break;
                }

                compare = g1.Equals((object)g2);

                if (compare)
                {
                    TestLibrary.TestFramework.LogError("006", "Guid1: " + g1);
                    TestLibrary.TestFramework.LogError("007", "Guid2: " + g2);
                    TestLibrary.TestFramework.LogError("008", "Compare succeed unexpectedly");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("009", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool   retVal = true;
        Guid   g1;
        Guid   g2;
        int    a;
        short  b;
        short  c;
        byte[] d;
        bool   compare;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Guid.Equals(object) always greater than");

        try
        {
            for (int i=0; i<11; i++)
            {
                d = new byte[8];
                for(int j=0;j<d.Length; j++) d[j] = (byte)(Math.Abs(TestLibrary.Generator.GetByte(-55) - 1));
                a = Math.Abs(TestLibrary.Generator.GetInt32(-55) - 1);
                b = (short)(Math.Abs(TestLibrary.Generator.GetInt16(-55) - 1));
                c = (short)(Math.Abs(TestLibrary.Generator.GetInt16(-55) - 1));
                g1 = new Guid(a, b, c, d);

                // less than
                switch (i)
                {
                case 0:
                    g2 = new Guid(a+1, b, c, d);
                    break;
                case 1:
                    g2 = new Guid(a, (short)(b+1), c, d);
                    break;
                case 2:
                    g2 = new Guid(a, b, (short)(c+1), d);
                    break;
                default:
                    d[i-3] = (byte)(d[i-3] + 1);
                    g2 = new Guid(a, b, c, d);
                    break;
                }

                compare = g1.Equals((object)g2);

                if (compare)
                {
                    TestLibrary.TestFramework.LogError("010", "Guid1: " + g1);
                    TestLibrary.TestFramework.LogError("011", "Guid2: " + g2);
                    TestLibrary.TestFramework.LogError("012", "Compare succeed unexpectedly");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("013", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest5()
    {
        bool   retVal = true;
        Guid   g;
        byte[] b;
        bool   compare;

        TestLibrary.TestFramework.BeginScenario("PosTest5: Guid.Equals(object) object is not a Guid");

        try
        {
            b = new byte[16];
            for(int i=0;i<b.Length; i++) b[i] = TestLibrary.Generator.GetByte(-55);
            g = new Guid(b);

            compare = g.Equals((object)this);

            if (compare)
            {
                TestLibrary.TestFramework.LogError("014", "Compare succeed unexpectedly");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("015", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}


