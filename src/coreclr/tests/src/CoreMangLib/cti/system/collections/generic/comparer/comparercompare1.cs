// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

public enum TestEnum
{
    VALUE_0,
    VALUE_1
}

public class TestGenericComparer<T> : Comparer<T>
{
    public TestGenericComparer() : base() { }

    public override int Compare(T x, T y)
    {
        if (!(x is ValueType))
        {
            // reference type
            Object ox = x as Object;
            Object oy = y as Object;

            if (x == null) return (y == null) ? 0 : -1;
            if (y == null) return 1;
        }

        if (x is IComparable<T>)
        {
            IComparable<T> comparer = x as IComparable<T>;
            return comparer.CompareTo(y);
        }

        if (x is IComparable)
        {
            IComparable comparer = x as IComparable;
            return comparer.CompareTo(y);
        }

        throw new ArgumentException();
    }
}

public class TestClass : IComparable<TestClass>
{
    public int Value;

    public TestClass(int value)
    {
        Value = value;
    }

    public int CompareTo(TestClass other)
    {
        return this.Value - other.Value;
    }
}

public class TestClass1 : IComparable
{
    public int Value;

    public TestClass1(int value)
    {
        Value = value;
    }

    public int  CompareTo(object obj)
    {
        TestClass1 other = obj as TestClass1;
        if (other != null)
        {
            return Value - other.Value;
        }

        if (obj is int)
        {
            int i = (int)obj;
            return Value - i;
        }

        throw new ArgumentException("Must be instance of TestClass1 or Int32");
    }
}

/// <summary>
/// Compare(T,T)
/// </summary>
public class ComparerCompare1
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;
        retVal = PosTest4() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Call Compare to compare two value type instance");

        try
        {
            Comparer<ValueType> comparer = new TestGenericComparer<ValueType>();
            retVal = VerificationHelper<ValueType>(comparer, 1, 2, -1, "001.1") && retVal;
            retVal = VerificationHelper<ValueType>(comparer, 2, 1, 1, "001.2") && retVal;
            retVal = VerificationHelper<ValueType>(comparer, 1, 1, 0, "001.3") && retVal;
            retVal = VerificationHelper<ValueType>(comparer, 1.0, 2.0, -1, "001.4") && retVal;
            retVal = VerificationHelper<ValueType>(comparer, 1, (int)TestEnum.VALUE_0, 1, "001.5") && retVal;
            retVal = VerificationHelper<ValueType>(comparer, 1, (int)TestEnum.VALUE_1, 0, "001.6") && retVal;
            retVal = VerificationHelper<ValueType>(comparer, 'a', 'A', 32, "001.7") && retVal;
            retVal = VerificationHelper<ValueType>(comparer, 'a', 'a', 0, "001.8") && retVal;
            retVal = VerificationHelper<ValueType>(comparer, 'A', 'a', -32, "001.9") && retVal;

            Comparer<int> comparer1 = new TestGenericComparer<int>();
            retVal = VerificationHelper<int>(comparer1, 1, 2, -1, "001.10") && retVal;
            retVal = VerificationHelper<int>(comparer1, 2, 1, 1, "001.11") && retVal;
            retVal = VerificationHelper<int>(comparer1, 1, 1, 0, "001.12") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("001.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: Call Compare with one or both parameters are null reference");

        try
        {
            Comparer<TestClass> comparer = new TestGenericComparer<TestClass>();
            retVal = VerificationHelper<TestClass>(comparer, null, new TestClass(1), -1, "002.1") && retVal;
            retVal = VerificationHelper<TestClass>(comparer, new TestClass(1), null, 1, "002.2") && retVal;
            retVal = VerificationHelper<TestClass>(comparer, null, null, 0, "002.3") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: Call Compare when T implements IComparable<T>");

        try
        {
            Comparer<TestClass> comparer = new TestGenericComparer<TestClass>();
            retVal = VerificationHelper<TestClass>(comparer, new TestClass(0), new TestClass(1), -1, "003.1") && retVal;
            retVal = VerificationHelper<TestClass>(comparer, new TestClass(1), new TestClass(0), 1, "003.2") && retVal;
            retVal = VerificationHelper<TestClass>(comparer, new TestClass(1), new TestClass(1), 0, "003.3") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("003.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: Call Compare when T implements IComparable");

        try
        {
            Comparer<TestClass1> comparer = new TestGenericComparer<TestClass1>();
            retVal = VerificationHelper<TestClass1>(comparer, new TestClass1(0), new TestClass1(1), -1, "004.1") && retVal;
            retVal = VerificationHelper<TestClass1>(comparer, new TestClass1(1), new TestClass1(0), 1, "004.2") && retVal;
            retVal = VerificationHelper<TestClass1>(comparer, new TestClass1(1), new TestClass1(1), 0, "004.3") && retVal;

            Comparer<Object> comparer1 = new TestGenericComparer<Object>();
            retVal = VerificationHelper<Object>(comparer1, new TestClass1(0), 1, -1, "004.4") && retVal;
            retVal = VerificationHelper<Object>(comparer1, new TestClass1(1), 0, 1, "004.5") && retVal;
            retVal = VerificationHelper<Object>(comparer1, new TestClass1(1), 1, 0, "004.6") && retVal;
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: ArgumentException should be thrown when Type T does not implement either the System.IComparable generic interface or the System.IComparable interface.");

        try
        {
            TestGenericComparer<ComparerCompare1> comparer = new TestGenericComparer<ComparerCompare1>();
            comparer.Compare(new ComparerCompare1(), new ComparerCompare1());

            TestLibrary.TestFramework.LogError("101.1", "ArgumentException is not thrown when Type T does not implement either the System.IComparable generic interface or the System.IComparable interface.");
            retVal = false;
        }
        catch (ArgumentException)
        { 
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("101.0", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ComparerCompare1 test = new ComparerCompare1();

        TestLibrary.TestFramework.BeginTestCase("ComparerCompare1");

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

    #region Private Methods
    private bool VerificationHelper<T>(Comparer<T> comparer, T x, T y, int expected, string errorno)
    {
        bool retVal = true;

        int actual = comparer.Compare(x, y);
        if ( actual != expected )
        {
            TestLibrary.TestFramework.LogError(errorno, "Compare returns unexpected value");
            TestLibrary.TestFramework.LogInformation("WARNING [LOCAL VARIABLE] x = " + x + ", y = " + y + ", expected = " + expected + ", actual = " + actual);
            retVal = false;
        }

        return retVal;
    }
    #endregion
}
