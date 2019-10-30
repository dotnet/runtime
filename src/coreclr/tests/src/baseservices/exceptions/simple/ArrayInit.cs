// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using TestLibrary;

public class ArrayInit {

	public static int Main(string[] args)
	{

        ArrayInit ai = new ArrayInit();
        TestFramework.BeginTestCase("Exception thrown in default ctor of a valuetype during Array.Initialize");
        if (ai.RunTests())
            return 100;
        else
            return 0;
	}

    public bool RunTests()
    {
        bool retVal = true;
        retVal &= PosTest1();
        retVal &= PosTest2();
        TestFramework.LogInformation(retVal ? "PASS" : "FAIL");
        return retVal;
    }

    public bool PosTest1()
    {
        TestFramework.BeginScenario("PosTest1: Initialize on vector of value-type, where default ctor throws");
        bool retVal = true;
        try
        {
            VT[] vecVT = new VT[5];
            vecVT.Initialize();
            TestFramework.LogError("001", "Expected exception to be thrown from Initialize");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestFramework.LogError("002", "Unexpected exception: " + e.ToString());
            retVal = false;
        }
        return retVal;
    }

    public bool PosTest2()
    {
        TestFramework.BeginScenario("PosTest2: Initialize on multi-dimensional array of value-type, where default ctor throws");
        bool retVal = true;
        try
        {
            VT[,] arrVT = new VT[5,10];
            arrVT.Initialize();
            TestFramework.LogError("001", "Expected exception to be thrown from Initialize");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestFramework.LogError("002", "Unexpected exception: " + e.ToString());
            retVal = false;
        }
        return retVal;
    }
}