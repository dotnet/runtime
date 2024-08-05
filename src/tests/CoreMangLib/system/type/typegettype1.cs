// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Xunit;


class MyTypeClass
{
    private int a;
    private int b;
    private int c;

    public MyTypeClass(int aa, int bb, int cc)
    {
        int zero = 0;
        a = aa;
        b = bb;
        try
        {
            c = cc / zero;
        }
        catch (Exception)
        {
        }
    }
}

public class TypeGetType1
{
    [Fact]
    public static int TestEntryPoint()
    {
        TypeGetType1 getType1 = new TypeGetType1();
        TestLibrary.TestFramework.BeginScenario("Testing System.Type.GetType()...");

        if (getType1.RunTests())
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
        //retVal = NegTest1() && retVal;
        //retVal = NegTest2() && retVal;
        //retVal = NegTest3() && retVal;
        //retVal = NegTest4() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify GetType method return correct system instance type...");

        try
        {
            Int32 intInstance = new Int32();
            Type instanceType = intInstance.GetType();

            if (instanceType.FullName != "System.Int32")
            {
                TestLibrary.TestFramework.LogError("001","Fetch the wrong Type of instance!");
                retVal = true;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify GetType method return correct customer instance type...");

        try
        {
            Type instanceType = new MyTypeClass(TestLibrary.Generator.GetInt32(-55),
                TestLibrary.Generator.GetInt32(-55), TestLibrary.Generator.GetInt32(-55)).GetType();

            if (instanceType.FullName != "MyTypeClass")
            {
                TestLibrary.TestFramework.LogError("001","fetch the wrong customer type!");
                return retVal;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002","Unexpected exception occurs: " + e);
            return retVal;
        }

        return retVal;
    }
}
