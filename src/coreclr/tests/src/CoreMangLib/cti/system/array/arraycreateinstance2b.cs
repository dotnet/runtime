// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Globalization;
using System.Collections;
/// <summary>
/// CreateInstance Method (Type, Int32[]) 
/// </summary>
public class ArrayCreateInstance2
{
    const int c_MaxValue = 10;
    const int c_MinValue = 0;
    public static int Main()
    {
        ArrayCreateInstance2 ArrayCreateInstance2 = new ArrayCreateInstance2();

        TestLibrary.TestFramework.BeginTestCase("ArrayCreateInstance2");
        if (ArrayCreateInstance2.RunTests())
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
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        return retVal;
    }

    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: create a multidimensional   Array with int type.");
        try
        {
            int[] parameter ={ c_MaxValue, c_MaxValue };
            Array myArray = Array.CreateInstance(typeof(int), parameter);
            int[,] myIntArray = new int[c_MaxValue, c_MaxValue];
            int generator = 0;
            for (int i = 0; i < c_MaxValue; i++)
            {
                for (int j = 0; j < c_MaxValue;j++)
                {
                    generator = TestLibrary.Generator.GetInt32(-55);
                    myArray.SetValue(generator, i, j);
                    myIntArray[i,j] = generator;
                }
            }
            if (!ToAssureReturnValue(myArray, myIntArray))
            {
                TestLibrary.TestFramework.LogError("001", "Create error.");
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: create a multidimensional  Array with string type.");
        try
        {
            int[] parameter ={ c_MaxValue, c_MaxValue };
            Array myArray = Array.CreateInstance(typeof(string), parameter);
            string[,] myStringArray = new string[c_MaxValue, c_MaxValue];
            string generator = string.Empty;
            for (int i = 0; i < c_MaxValue; i++)
            {
                for (int j = 0; j < c_MaxValue; j++)
                {
                    generator = TestLibrary.Generator.GetString(-55, true, c_MinValue, c_MaxValue);
                    myArray.SetValue(generator, i, j);
                    myStringArray[i,j] = generator;
                }
            }
            if (!ToAssureReturnValue(myArray, myStringArray))
            {
                TestLibrary.TestFramework.LogError("003", "Create error.");
                retVal = false;
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: create a multidimensional  Array with user define type.");
       
        try
        {
            int[] parameter ={ c_MaxValue, c_MaxValue };
            Array myArray = Array.CreateInstance(typeof(Temperature), parameter);
            Temperature[,] myTemperatureArray = new Temperature[c_MaxValue, c_MaxValue];
            Temperature generator = null;
            for (int i = 0; i < c_MaxValue; i++)
            {
                for (int j = 0; j < c_MaxValue; j++)
                {
                    generator = new Temperature();
                    generator.Value = i * 4 + j;
                    myArray.SetValue(generator, i, j);
                    myTemperatureArray[i,j] = generator;
                }
            }
            if (!ToAssureReturnValue(myArray, myTemperatureArray))
            {
                TestLibrary.TestFramework.LogError("005", "Create error.");
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
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool PosTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest4: create a multidimensional  Array with user define Generic type.");
        try
        {
            int[] parameter ={ c_MaxValue, c_MaxValue };
            Array myArray = Array.CreateInstance(typeof(TestClass1<int>), parameter);
            TestClass1<int>[,] myTemperatureArray = new TestClass1<int>[c_MaxValue, c_MaxValue];
            TestClass1<int> generator = null;
            for (int i = 0; i < c_MaxValue; i++)
            {
                for (int j = 0; j < c_MaxValue; j++)
                {
                    generator = new TestClass1<int>();
                    generator.Value = i * 4+j;
                    myArray.SetValue(generator, i, j);
                    myTemperatureArray[i,j] = generator;
                }
            }
            if (!ToAssureReturnValue(myArray, myTemperatureArray))
            {
                TestLibrary.TestFramework.LogError("007", "Create error.");
                retVal = false;
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
            int[] parameter ={ c_MaxValue, c_MaxValue };
            Array myArray = Array.CreateInstance(null, parameter);
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
    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: lengths is a null reference .");

        try
        {
            int[] parameter =null;
            Array myArray = Array.CreateInstance(typeof(int), parameter);
            TestLibrary.TestFramework.LogError("011", "lengths is a null reference  should not be created.");
            retVal = false;
        }
        catch (ArgumentNullException)
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

        TestLibrary.TestFramework.BeginScenario("NegTest3: elementType is not supported. Void is not supported.");
        try
        {
            int[] parameter ={ c_MaxValue, c_MaxValue };
            Array myArray = Array.CreateInstance(typeof(void), parameter);
            TestLibrary.TestFramework.LogError("013", "Void should not be supported.");
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
    public  bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: elementType is an open generic type.");

        try
        {
            int[] parameter ={ c_MaxValue, c_MaxValue };
            Array myArray = Array.CreateInstance(typeof(TestClass1<>), parameter);
            TestLibrary.TestFramework.LogError("015", "elementType is an open generic type.");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("016", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool NegTest5()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest5: Any value in lengths is less than zero.");

        try
        {
            int[] parameter ={ c_MinValue - 1, c_MaxValue };
            Array myArray = Array.CreateInstance(typeof(int), parameter);
            TestLibrary.TestFramework.LogError("017", "Any value in lengths is less than zero.");
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("018", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    // Returns true if the expected result is right
    // Returns false if the expected result is wrong
    public bool NegTest6()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest6: The lengths array contains less than one element.");

        try
        {
            int[] parameter ={  };
            Array myArray = Array.CreateInstance(typeof(int), parameter);
            TestLibrary.TestFramework.LogError("019", "The lengths array contains less than one element.");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("020", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #region private method
    private bool ToAssureReturnValue(Array myArray, int [,] myExpectArray)
    {
        int indexi = c_MinValue;
        int indexj = c_MinValue;
        for (IEnumerator itr = myArray.GetEnumerator(); itr.MoveNext(); )
        {
            object current = itr.Current;
            if (!current.Equals(myExpectArray[indexi, indexj]))
            {
                return false;
            }
            indexj++;
            if (indexj == c_MaxValue)
            {
                indexj = c_MinValue;
                indexi++;
            }
        }
        return true;
    }
    private bool ToAssureReturnValue(Array myArray, string[,] myExpectArray)
    {
        int indexi = c_MinValue;
        int indexj = c_MinValue;
        for (IEnumerator itr = myArray.GetEnumerator(); itr.MoveNext(); )
        {
            object current = itr.Current;
            if (!current.Equals(myExpectArray[indexi, indexj]))
            {
                return false;
            }
            indexj++;
            if (indexj == c_MaxValue)
            {
                indexj = c_MinValue;
                indexi++;
            }
        }
        return true;
    }
    private bool ToAssureReturnValue(Array myArray, Temperature [,] myExpectArray)
    {
        int indexi = c_MinValue;
        int indexj = c_MinValue;
        for (IEnumerator itr = myArray.GetEnumerator(); itr.MoveNext(); )
        {
            object current = itr.Current;
            if (!current.Equals(myExpectArray[indexi, indexj]))
            {
                return false;
            }
            indexj++;
            if (indexj == c_MaxValue)
            {
                indexj = c_MinValue;
                indexi++;
            }
        }
        return true;
    }
    private bool ToAssureReturnValue(Array myArray, TestClass1<int>[,] myExpectArray)
    {
        int indexi = c_MinValue;
        int indexj = c_MinValue;
        for (IEnumerator itr = myArray.GetEnumerator(); itr.MoveNext(); )
        {
            object current = itr.Current;
            if (!current.Equals(myExpectArray[indexi, indexj]))
            {
                return false;
            }
            indexj++;
            if (indexj == c_MaxValue)
            {
                indexj = c_MinValue;
                indexi++;
            }
        }
        return true;
    }
    #endregion
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

