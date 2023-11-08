// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.Collections;
using Xunit;

/// <summary>
/// Type.GetHashCode()
/// </summary>
public class TypeGetHashCode
{
    [Fact]
    public static int TestEntryPoint()
    {
        TypeGetHashCode tghc = new TypeGetHashCode();
        TestLibrary.TestFramework.BeginTestCase("TypeGetHashCode");
        if (tghc.RunTests())
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
        return retVal;
    }
    #region PositiveTest
    public bool PosTest1()
    {
        bool retVal = true;
        Type tpA;
        int ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest1: normal Type get hash code");
        try
        {
            tpA = typeof(testClass);
            ActualResult = tpA.GetHashCode();
            testClass tc = new testClass(ActualResult);
            if (ActualResult != tc.GetHashCode())
            {
                TestLibrary.TestFramework.LogError("001", "the ActualResult is not the ExpectResult");
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
        Type tpA;
        int ActualResult;

        TestLibrary.TestFramework.BeginScenario("PosTest2: the same type get hash code");
        try
        {
            Base test1 = new Base();
            Base test2 = new Base();
            tpA = test1.GetType();
            Type tpB = test2.GetType();
            ActualResult = tpA.GetHashCode();
            int ActualResult2 = tpB.GetHashCode();
            if (ActualResult != ActualResult2)
            {
                TestLibrary.TestFramework.LogError("003","the ActualResult is not the ExpectResult");
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
    #endregion
    #region ForTestClass
    public class testClass : Object
    {
        int hashCode;
        public testClass(int intA)
        {
            hashCode = intA;
        }
        public override int GetHashCode()
        {
            return hashCode;
        }
    }
    public class Base:Object { }
    #endregion

}

