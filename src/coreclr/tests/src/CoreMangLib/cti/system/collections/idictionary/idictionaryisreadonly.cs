// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections; 
using System.Collections.Generic;

/// <summary>
/// System.Collections.IDictionary.IsReadOnly
/// </summary>
public class IDictionaryIsReadOnly
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;
        retVal = PosTest3() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: The property is false");

        try
        {
            IDictionary iDictionary = new Dictionary<object,object>();
            if (iDictionary.IsReadOnly)
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Test the property in List<T>");

        try
        {
            IDictionary iDictionary = new Dictionary<object, object>();
            if (iDictionary.IsReadOnly)
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

        TestLibrary.TestFramework.BeginScenario("PosTest3: Check a custom class derived from the interface");

        try
        {
            IDictionary iDictionary = new MyIDictionary();
            if (!iDictionary.IsReadOnly)
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
    #endregion
    #endregion

    public static int Main()
    {
        IDictionaryIsReadOnly test = new IDictionaryIsReadOnly();

        TestLibrary.TestFramework.BeginTestCase("IDictionaryIsReadOnly");

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