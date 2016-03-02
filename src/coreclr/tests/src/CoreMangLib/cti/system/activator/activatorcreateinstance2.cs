// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public class ActivatorCreateInstance2
{
    public ActivatorCreateInstance2() {}
    public ActivatorCreateInstance2(string str, int i) {}

    public static int Main()
    {
        ActivatorCreateInstance2 ac = new ActivatorCreateInstance2();

        TestLibrary.TestFramework.BeginTestCase("ActivatorCreateInstance2");

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

    public bool PosTest1()
    {
        bool   retVal = true;
        object obj;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Activator.CreateInstance(Type, null)");

        try
        {
            obj = Activator.CreateInstance(this.GetType(), null);

            if (null == obj)
            {
                TestLibrary.TestFramework.LogError("001", "Returned object is null: Obj=" + obj);
                retVal = false;
            }
            if (null == obj || obj.GetType() != this.GetType())
            {
                TestLibrary.TestFramework.LogError("002", "Type of object does not match: Expected(" + this.GetType() + ") Actual(" + obj.GetType() + ")");
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
        object obj;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Activator.CreateInstance(Type, object[])");

        try
        {
            obj = Activator.CreateInstance(this.GetType(), new object[2] { "Text", (object)1} );

            if (null == obj)
            {
                TestLibrary.TestFramework.LogError("004", "Returned object is null: Obj=" + obj);
                retVal = false;
            }
            if (null == obj || obj.GetType() != this.GetType())
            {
                TestLibrary.TestFramework.LogError("005", "Type of object does not match: Expected(" + this.GetType() + ") Actual(" + obj.GetType() + ")");
                retVal = false;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest1()
    {
        bool   retVal = true;
        object obj;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Activator.CreateInstance(Type, object[]), constructor not found");

        try
        {
            obj = Activator.CreateInstance(this.GetType(), new object[1] { "Text"} );

            TestLibrary.TestFramework.LogError("007", "Exception expected");
            retVal = false;
        }
        catch (MissingMethodException)
        {
            // expected
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
}
