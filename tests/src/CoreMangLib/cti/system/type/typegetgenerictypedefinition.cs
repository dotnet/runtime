// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

/// <summary>
///Type.GetGenericTypeDefinition()
/// </summary>
public class TypeGetGenericTypeDefinition
{
    public static int Main()
    {
        TypeGetGenericTypeDefinition tggtd = new TypeGetGenericTypeDefinition();
        TestLibrary.TestFramework.BeginTestCase("TypeGetGenericTypeDefinition");
        if (tggtd.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negtive]");
        retVal = NegTest1() && retVal;

        return retVal;
    }
    #region PositiveTesting
    public bool PosTest1()
    {
        bool retVal = true;
        Type tpA;
        Type ActualResult;
        TestLibrary.TestFramework.BeginScenario("PosTest1: the current type's definition has one generic param");
        try
        {
            tpA = typeof(Derived2<Derived>);
            ActualResult = tpA.GetGenericTypeDefinition();
            if(ActualResult != typeof(Derived2<>))
            {
                TestLibrary.TestFramework.LogError("001","the ActualResult is not the ExpectResult");
                retVal = false;
            }
        }
        catch(Exception e)
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
        Type tpB;
        Type ActualResult1;
        Type ActualResult2;
        TestLibrary.TestFramework.BeginScenario("PosTest2: the current type has the same type argument");
        try
        {
            tpA = typeof(Derived2<int>);
            tpB = typeof(Derived2<string>);
            ActualResult1 = tpA.GetGenericTypeDefinition();
            ActualResult2 = tpB.GetGenericTypeDefinition();
            if (ActualResult1 != typeof(Derived2<>) || ActualResult2 != ActualResult1)
            {
                TestLibrary.TestFramework.LogError("003", "the ActualResult is not the ExpectResult");
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
    #region NegativeTest
    public bool NegTest1()
    {
        bool retVal = true;
        Type tpA;
        Type ActualResult;
        TestLibrary.TestFramework.BeginScenario("NegTest1: the current object is not generic type");

        try
        {
            tpA = typeof(Derived);
            ActualResult = tpA.GetGenericTypeDefinition();
            retVal = false;
        }
        catch (InvalidOperationException) { }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("N001", "Unexpect exception:" + e);
            retVal = false;
        }
        return retVal;

    }
    #endregion
    #region UsingForTesting
    public class Base<T, U> { }
    public class Derived : Base<string, int> { }
    public class Derived2<V> : Base<bool, V> { }
    public class G<T> { }
    #endregion
}

