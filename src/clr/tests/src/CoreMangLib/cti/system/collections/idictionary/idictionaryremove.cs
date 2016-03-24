// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections; 
using System.Collections.Generic;

/// <summary>
/// System.Collections.IDictionary.Remove(object)
/// </summary>
public class IDictionaryRemove
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;
        retVal = NegTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Remove all the element in the IDictionary");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            Int32[] key = new Int32[1000];
            Byte[] value = new Byte[1000];
            for (int i = 0; i < 1000; i++)
            {
                key[i] = i;
            }
            TestLibrary.Generator.GetBytes(-55, value);
            for (int i = 0; i < 1000; i++)
            {
                iDictionary.Add(key[i], value[i]);
            }
            for (int i = 0; i < 1000; i++)
            {
                iDictionary.Remove(i);
            }
            if (iDictionary.Count != 0)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected ");
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

    public bool PosTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest2: The key to be remove is a custom class");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            MyClass mc = new MyClass();
            int value = TestLibrary.Generator.GetInt32(-55);
            iDictionary.Add(mc, value);
            iDictionary.Remove(mc);
            if (iDictionary.Contains(mc))
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected ");
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

    public bool PosTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest3: the IDictionary object does not contain an element ");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            Int32[] key = new Int32[1000];
            Byte[] value = new Byte[1000];
            for (int i = 0; i < 1000; i++)
            {
                key[i] = i;
            }
            TestLibrary.Generator.GetBytes(-55, value);
            for (int i = 0; i < 1000; i++)
            {
                iDictionary.Add(key[i], value[i]);
            }
            iDictionary.Remove("hello");
            if (iDictionary.Count != 1000)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected ");
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The object is null reference");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            object ob = null;
            iDictionary.Remove(ob);
            TestLibrary.TestFramework.LogError("101", "The ArgumentNullException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentNullException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("102", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest2()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest2: The IDictionary is readonly");

        try
        {
            IDictionary iDictionary = new MyIDictionary();
            iDictionary.Remove(new MyClass());
            TestLibrary.TestFramework.LogError("103", "The NotSupportedException was not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("104", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        IDictionaryRemove test = new IDictionaryRemove();

        TestLibrary.TestFramework.BeginTestCase("IDictionaryRemove");

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
public class MyClass
{
}

public class MyIDictionary : IDictionary
{

    #region IDictionary Members

    public void Add(object key, object value)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public void Clear()
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public bool Contains(object key)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public IDictionaryEnumerator GetEnumerator()
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public bool IsFixedSize
    {
        get { return true; }
    }

    public bool IsReadOnly
    {
        get { return true; }
    }

    public ICollection Keys
    {
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public void Remove(object key)
    {
        if (IsReadOnly || IsFixedSize)
        {
            throw new NotSupportedException("The IDictionary is read-only or has a fixed size");
        }
        else
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    public ICollection Values
    {
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public object this[object key]
    {
        get
        {
            throw new Exception("The method or operation is not implemented.");
        }
        set
        {
            throw new Exception("The method or operation is not implemented.");
        }
    }

    #endregion

    #region ICollection Members

    public void CopyTo(Array array, int index)
    {
        throw new Exception("The method or operation is not implemented.");
    }

    public int Count
    {
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public bool IsSynchronized
    {
        get { throw new Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
        get { throw new Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new Exception("The method or operation is not implemented.");
    }

    #endregion
}
