// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using Xunit;

class MyClass
{
    private int a;
    private int b;

    public MyClass(int aa,int bb)
    {
        a = aa;
        b = bb;
    }
}

public class TypeGetType2
{
    [Fact]
    public static int TestEntryPoint()
    {
        TypeGetType2 getType2 = new TypeGetType2();
        TestLibrary.TestFramework.BeginScenario("Testing System.Type.GetType(System.String)...");

        if (getType2.RunTests())
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
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        //retVal = NegTest4() && retVal;

        return retVal;
    }

    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify System type can be fetch correctly...");

        try
        {
            Type intType = Type.GetType("System.Int32");
            if (intType.FullName != "System.Int32")
            {
                TestLibrary.TestFramework.LogError("001","intType is not fetched correctly!");
                retVal = false;
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
        TestLibrary.TestFramework.BeginScenario("Verify customer type can be fetch correctly... ");

        try
        {
            Type myType = Type.GetType("MyClass");

            if (myType != null && myType.FullName != "MyClass")
            {
                TestLibrary.TestFramework.LogError("003","Fetch the wrong type of an instance!");
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

    public bool NegTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify typename is a null reference...");

        try
        {
            Type myType = Type.GetType(null);

            TestLibrary.TestFramework.LogError("005", "No exception occurs!");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception occurs: " + e);
            return retVal;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify type name is invalid...");

        try
        {
            Type myType = Type.GetType("\\888");
            if (myType != null)
            {
                TestLibrary.TestFramework.LogError("007","myType should be null!");
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

    public bool NegTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("Verify type name is empty string...");

        try
        {
            Type myType = Type.GetType("");

            if (myType != null)
            {
                TestLibrary.TestFramework.LogError("009","The type should be null");
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010","Unexpected exception occurs: " + e);
            retVal = false;
        }

        return retVal;
    }
}
