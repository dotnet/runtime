// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections; 
using System.Collections.Generic;

/// <summary>
/// System.Collections.IDictionary.Add(System.Object,System.Object)
/// </summary>
public class IDictionaryAdd
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
        retVal = NegTest3() && retVal;
        retVal = NegTest4() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Check the add method of Dictionary<K,V> ");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            string key = "key";
            int value = TestLibrary.Generator.GetInt32(-55);
            iDictionary.Add(key, value);
            if ((int)iDictionary[key] != value)
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: The key is a custom class and the value is char");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            MyKey key = new MyKey();
            char value = TestLibrary.Generator.GetCharLetter(-55);
            iDictionary.Add(key, value);
            if ((char)iDictionary[key] != value)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: The value is a null reference");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            string key = "key";
            object value = null;
            iDictionary.Add(key, value);
            if (iDictionary["key"] != null)
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

        TestLibrary.TestFramework.BeginScenario("NegTest1: The key is a null reference");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            string key = null;
            int value = TestLibrary.Generator.GetInt32(-55);
            iDictionary.Add(key, value);
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

        TestLibrary.TestFramework.BeginScenario("NegTest2: An element with the same key already exists in the IDictionary object");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            string key = "key";
            int value1 = TestLibrary.Generator.GetInt32(-55);
            iDictionary.Add(key, value1);
            int value2 = TestLibrary.Generator.GetInt32(-55);
            iDictionary.Add(key, value2);
            TestLibrary.TestFramework.LogError("103", "The ArgumentException was not thrown as expected");
            retVal = false;
        }
        catch (ArgumentException)
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

    public bool NegTest3()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest3: The Idictionary is readonly");

        try
        {
            IDictionary subHash = new SubHash();
            string key = "key";
            int value = TestLibrary.Generator.GetInt32(-55);
            subHash.Add(key, value);
            TestLibrary.TestFramework.LogError("105", "The NotSupportedException was not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("106", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }

    public bool NegTest4()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest4: The Idictionary has a fixed size");

        try
        {
            IDictionary subHash = new SubHash2();
            string key = "key";
            int value = TestLibrary.Generator.GetInt32(-55);
            subHash.Add(key, value);
            TestLibrary.TestFramework.LogError("107", "The NotSupportedException was not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
        {
        }
        catch (Exception e)
        {
            TestLibrary.TestFramework.LogError("108", "Unexpected exception: " + e);
            TestLibrary.TestFramework.LogInformation(e.StackTrace);
            retVal = false;
        }

        return retVal;
    }
    #endregion
    #endregion

    public static int Main()
    {
        IDictionaryAdd test = new IDictionaryAdd();

        TestLibrary.TestFramework.BeginTestCase("IDictionaryAdd");

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
public class MyKey
{
}

public class SubHash : IDictionary
{

    #region IDictionary Members

    public void Add(object key, object value)
    {
        if (IsFixedSize || IsReadOnly)
        {
            throw new NotSupportedException("The IDictionary is readonly or has a fixed size");
        }
        else
        {
            throw new Exception("The method or operation is not implemented.");
        }
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
        throw new Exception("The method or operation is not implemented.");
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

public class SubHash2: IDictionary
{

    #region IDictionary Members

    public void Add(object key, object value)
    {
        if (IsFixedSize || IsReadOnly)
        {
            throw new NotSupportedException("The IDictionary is readonly or has a fixed size");
        }
        else
        {
            throw new Exception("The method or operation is not implemented.");
        }
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
        throw new Exception("The method or operation is not implemented.");
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
