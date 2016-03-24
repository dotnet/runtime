// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;
/// <summary>
/// MarshalAsAttribute.ctor(Int16) [v-minch]
/// </summary>
public class MarshalAsAttributeCtor1
{
    public static int Main()
    {
        MarshalAsAttributeCtor1 test = new MarshalAsAttributeCtor1();
        TestLibrary.TestFramework.BeginTestCase("MarshalAsAttribute.ctor(Int16)");
        if (test.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest1: Initialize a instance of MarshalAsAttribute Class 1");
        try
        {
            short unmanagedType = Int16.MaxValue;
            MarshalAsAttribute myMarshalAsAttribute = new MarshalAsAttribute(unmanagedType);
            if (myMarshalAsAttribute == null || myMarshalAsAttribute.Value != (UnmanagedType)unmanagedType)
            {
                TestLibrary.TestFramework.LogError("001", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute.Value.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest2()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest2: Initialize a instance of MarshalAsAttribute Class 2");
        try
        {
            short unmanagedType = Int16.MinValue;
            MarshalAsAttribute myMarshalAsAttribute = new MarshalAsAttribute(unmanagedType);
            if (myMarshalAsAttribute == null || myMarshalAsAttribute.Value != (UnmanagedType)unmanagedType)
            {
                TestLibrary.TestFramework.LogError("003", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute.Value.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    public bool PosTest3()
    {
        bool retVal = true;
        TestLibrary.TestFramework.BeginScenario("PosTest3: Initialize a instance of MarshalAsAttribute Class 3");
        try
        {
            short unmanagedType = TestLibrary.Generator.GetInt16(-55);
            MarshalAsAttribute myMarshalAsAttribute = new MarshalAsAttribute(unmanagedType);
            if (myMarshalAsAttribute == null || myMarshalAsAttribute.Value != (UnmanagedType)unmanagedType)
            {
                TestLibrary.TestFramework.LogError("005", "the intance should not be null and its value ExpectResult is " + ((UnmanagedType)unmanagedType).ToString() + " but the ActualResult is " + myMarshalAsAttribute.Value.ToString());
                retVal = false;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;
    }
    #endregion

}
