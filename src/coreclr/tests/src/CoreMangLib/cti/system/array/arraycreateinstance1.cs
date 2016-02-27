// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Collections;
/// <summary>
/// CreateInstance Method (Type, Int32) 
/// </summary>
public class ArrayCreateInstance1
{
    const int c_MaxValue = 10;
    const int c_MinValue = 0;
    public static int Main()
    {
        ArrayCreateInstance1 ArrayCreateInstance1 = new ArrayCreateInstance1();

        TestLibrary.TestFramework.BeginTestCase("ArrayCreateInstance1");
        if (ArrayCreateInstance1.RunTests())
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
        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
       
        return retVal;
    }

    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: create a one-dimensional  Array with int type.");
        try
        {

            Array myArray = Array.CreateInstance(typeof(int), c_MaxValue);
            int[] myIntArray = new int[c_MaxValue];
            int generator = 0;
            for (int i = 0; i < c_MaxValue; i++)
            {
                generator = TestLibrary.Generator.GetInt32(-55);
                myArray.SetValue(generator, i);
                myIntArray[i] = generator;
            }
            int index = c_MinValue;
            for (IEnumerator itr = myArray.GetEnumerator(); itr.MoveNext(); )
            {
                object current = itr.Current;
                if (!current.Equals(myIntArray[index]))
                {
                    TestLibrary.TestFramework.LogError("001", "Create error.");
                    retVal = false;
                    break;
                }
                index++;
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: create a one-dimensional  Array with string type.");
        try
        {
            Array myArray = Array.CreateInstance(typeof(string), c_MaxValue);
            string[] myStringArray = new string[c_MaxValue];
            string generator = string.Empty;
            for (int i = 0; i < c_MaxValue; i++)
            {
                generator = TestLibrary.Generator.GetString(-55, true, c_MinValue, c_MaxValue);
                myArray.SetValue(generator, i);
                myStringArray[i] = generator;
            }
            int index = c_MinValue;
            for (IEnumerator itr = myArray.GetEnumerator(); itr.MoveNext(); )
            {
                object current = itr.Current;
                if (!current.Equals(myStringArray[index]))
                {
                    TestLibrary.TestFramework.LogError("003", "Create error.");
                    retVal = false;
                    break;
                }
                index++;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("004", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: create a one-dimensional  Array with user define type.");
       
        try
        {
            Array myArray = Array.CreateInstance(typeof(Temperature), c_MaxValue);
            Temperature[] myTemperatureArray = new Temperature[c_MaxValue];
            Temperature generator = null;
            for (int i = 0; i < c_MaxValue; i++)
            {
                generator = new Temperature();
                generator.Value = i * 4;
                myArray.SetValue(generator, i);
                myTemperatureArray[i] = generator;
            }
            int index = c_MinValue;
            for (IEnumerator itr = myArray.GetEnumerator(); itr.MoveNext(); )
            {
                object current = itr.Current;
                if (!current.Equals(myTemperatureArray[index]))
                {
                    TestLibrary.TestFramework.LogError("005", "Create error.");
                    retVal = false;
                    break;
                }
                index++;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("006", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: create a one-dimensional  Array with user define Generic type.");
        try
        {
            Array myArray = Array.CreateInstance(typeof(TestClass1<int>), c_MaxValue);
            TestClass1<int>[] myTemperatureArray = new TestClass1<int>[c_MaxValue];
            TestClass1<int> generator = null;
            for (int i = 0; i < c_MaxValue; i++)
            {
                generator = new TestClass1<int>();
                generator.Value = i * 4;
                myArray.SetValue(generator, i);
                myTemperatureArray[i] = generator;
            }
            int index = c_MinValue;
            for (IEnumerator itr = myArray.GetEnumerator(); itr.MoveNext(); )
            {
                object current = itr.Current;
                if (!current.Equals(myTemperatureArray[index]))
                {
                    TestLibrary.TestFramework.LogError("007", "Create error.");
                    retVal = false;
                    break;
                }
                index++;
            }

        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("008", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: elementType is a null reference .");

        try
        {
            Array myArray = Array.CreateInstance(null, c_MaxValue);
            TestLibrary.TestFramework.LogError("009", "elementType is a null reference should not be created.");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("010", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public  bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: elementType is not supported. Void is not supported.");
        try
        {
            Array myArray = Array.CreateInstance(typeof(void), c_MaxValue);
            TestLibrary.TestFramework.LogError("011", "Void should not be supported.");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("012", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public  bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: elementType is an open generic type.");

        try
        {
            Array myArray = Array.CreateInstance(typeof(TestClass1<>), c_MaxValue);
            TestLibrary.TestFramework.LogError("013", "elementType is an open generic type.");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("014", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: length is less than zero.");

        try
        {
            Array myArray = Array.CreateInstance(typeof(int), c_MinValue -1);
            TestLibrary.TestFramework.LogError("015", "length is less than zero.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
   
}
//create Temperature  for provding test method and test target.
public class Temperature : System.Collections.IComparer
{

    // The value holder
    protected int m_value;

    public int Value
    {
        get
        {
            return m_value;
        }
        set
        {
            m_value = value;
        }
    }
    #region IComparer Members

    public int Compare(object x, object y)
    {
        if (x is Temperature)
        {
            Temperature temp = (Temperature)x;

            return ((Temperature)y).m_value.CompareTo(temp.m_value);
        }

        throw new Exception("The method parameter x is not expected.");
    }

    #endregion
}

public  class  TestClass1<T> 
{
    // The value holder
    protected int m_value;

    public int Value
    {
        get
        {
            return m_value;
        }
        set
        {
            m_value = value;
        }
    }
}


