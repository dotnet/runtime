// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// System.Collections.ICollection.CopyTo(System.Array,System.Int32)
/// </summary>
public class ICollectionCopyTo
{
    const int Count = 1000;
    static Random m_rand = new Random(-55);

    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;
        retVal = NegTest5() && retVal;
        retVal = NegTest6() && retVal;
        retVal = NegTest7() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        Byte[] byteValue = new byte[Count];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<object> arrayList = new List<object>();
        for (int i = 0; i < Count; i++)
        {
            arrayList.Add(byteValue[i]);
        }

        byte[] array = new byte[Count];

        TestLibrary.TestFramework.BeginScenario("PosTest1: Using Arraylist which implemented the CopyTo method in ICollection ");

        try
        {

            ((ICollection)arrayList).CopyTo(array, 0);

            for (int i = 0; i < Count; i++)
            {
                if ((byte)(arrayList[i]) != array[i])
                {
                    TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool PosTest2()
    {
        bool retVal = true;
        int index =m_rand.Next(1,1000);

        Byte[] byteValue = new byte[Count];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<object> arrayList = new List<object>();
        for (int i = 0; i < Count; i++)
        {
            arrayList.Add(byteValue[i]);
        }

        byte[] array = new byte[Count+index];

        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the start index is not zero...");

        try
        {

            ((ICollection)arrayList).CopyTo(array, index);

            for (int i = 0; i < Count; i++)
            {
                if ((byte)(arrayList[i]) != array[i+index])
                {
                    TestLibrary.TestFramework.LogError("002", "The result is not the value as expected");
                    retVal = false;
                }
            }
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("002", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;
        int index = m_rand.Next(1, 1000);

        Byte[] byteValue = new byte[Count];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<object> arrayList = new List<object>();
        for (int i = 0; i < Count; i++)
        {
            arrayList.Add(byteValue[i]);
        }

        byte[] array = null;

        TestLibrary.TestFramework.BeginScenario("NegTest1: Verify the array is a null reference");

        try
        {
            ((ICollection)arrayList).CopyTo(array, index);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected when array is a null reference");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        int index = m_rand.Next(1, 1000);
        byte[] array = new byte[Count];

        Byte[] byteValue = new byte[Count];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<object> arrayList = new List<object>();
        for (int i = 0; i < Count; i++)
        {
            arrayList.Add(byteValue[i]);
        }

        TestLibrary.TestFramework.BeginScenario("NegTest2: Verify the index is less than zero");

        try
        {
            index = -index;
            ((ICollection)arrayList).CopyTo(array, index);
            TestLibrary.TestFramework.LogError("103", "The ArgumentOutOfRangeException was not thrown as expected when index is "+index.ToString());
            retVal = false;
        }
        catch (ArgumentOutOfRangeException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest3()
    {
        bool retVal = true;

        byte[] array = new byte[Count];
        int index = array.Length;
        Byte[] byteValue = new byte[Count];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<object> arrayList = new List<object>();
        for (int i = 0; i < Count; i++)
        {
            arrayList.Add(byteValue[i]);
        }

        TestLibrary.TestFramework.BeginScenario("NegTest3: Verify the index is equal the length of array");

        try
        {
            ((ICollection)arrayList).CopyTo(array, index);
            TestLibrary.TestFramework.LogError("105", "The ArgumentException was not thrown as expected when index equal length of array");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        
        byte[] array = new byte[Count];
        int index = array.Length +m_rand.Next(1, 1000);

        List<object> arrayList = new List<object>();
        Byte[] byteValue = new byte[Count];
        TestLibrary.Generator.GetBytes(-55, byteValue);

        for (int i = 0; i < Count; i++)
        {
            arrayList.Add(byteValue[i]);
        }

        TestLibrary.TestFramework.BeginScenario("NegTest4: Verify the index is greater than the length of array");

        try
        {
            ((ICollection)arrayList).CopyTo(array, index);
            TestLibrary.TestFramework.LogError("107", "The ArgumentException was not thrown as expected. \n index is " + index.ToString() + "\n array length is " + array.Length.ToString());
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest5()
    {
        bool retVal = true;


        Array array = new byte[Count,Count];
        int index = m_rand.Next(1, 1000);

        byte[]  byteValue = new byte[Count];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<object> arrayList = new List<object>();
        for (int i = 0; i < Count; i++)
        {
            arrayList.Add(byteValue[i]);
        }

        TestLibrary.TestFramework.BeginScenario("NegTest5: Verify the array is multidimensional");

        try
        {
            ((ICollection)arrayList).CopyTo(array, array.Length);
            TestLibrary.TestFramework.LogError("109", "The ArgumentException was not thrown as expected when the array is multidimensional");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("110", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    public bool NegTest6()
    {
        bool retVal = true;

        int count = Count + m_rand.Next(1, 1000);

        byte[] array = new byte[Count];
        int index = m_rand.Next(1,1000);

        byte[] byteValue = new byte[count];
        TestLibrary.Generator.GetBytes(-55, byteValue);
        List<object> arrayList = new List<object>();
        for (int i = 0; i < count; i++)
        {
            arrayList.Add(byteValue[i]);
        }

        TestLibrary.TestFramework.BeginScenario("NegTest6: The number of elements in the ICollection is greater than the available space from index to the end of array");

        try
        {
            ((ICollection)arrayList).CopyTo(array, array.Length);
            TestLibrary.TestFramework.LogError("111", "The ArgumentException was not thrown as expecteds");
            retVal = false;
        }
        catch (ArgumentException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("112", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest7()
    {
        bool retVal = true;

        int index = m_rand.Next(1, 1000);
        byte[] array = new byte[Count+index];

        
        List<object> arrayList = new List<object>();
        for (int i = 0; i < Count; i++)
        {
            arrayList.Add(new object());
        }

        TestLibrary.TestFramework.BeginScenario("NegTest7: Verify the type of the ICollection cannot be cast automatically to the type of array");

        try
        {
            ((ICollection)arrayList).CopyTo(array, index);
            TestLibrary.TestFramework.LogError("113", "The InvalidCastException was not thrown as expected");
            retVal = false;
        }
        catch (InvalidCastException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("114", "Unexpected exception: " + e);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        ICollectionCopyTo test = new ICollectionCopyTo();

        TestLibrary.TestFramework.BeginTestCase("Test for method:System.Collections.ICollection.CopyTo(System.Array,System.Int32)");

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
}

