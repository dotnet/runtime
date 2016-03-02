// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// System.Collections.IDictionary.Clear()
/// </summary>
public class IDictionaryClear
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        TestLibrary.TestFramework.LogInformation("[Negative]");
        retVal = NegTest1() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("PosTest1: Using Dictionary<string,Byte> to implemented the clear method");

        try
        {
            IDictionary iDictionary = new Dictionary<string,Byte>();
            string[] key = new string[1000];
            Byte[] value = new Byte[1000];
            for (int i = 0; i < 1000; i++)
            {
                key[i] = TestLibrary.Generator.GetString(-55, false, 1, 100);
            }
            TestLibrary.Generator.GetBytes(-55, value);
            for (int i = 0; i < 1000; i++)
            {
                while (iDictionary.Contains(key[i]))
                {
                    key[i] = TestLibrary.Generator.GetString(false, 1, 100);
                }
                iDictionary.Add(key[i], value[i]);
            }
            iDictionary.Clear();
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

        TestLibrary.TestFramework.BeginScenario("PosTest2: Using Dictionary<Int32,Byte> to implemented the clear method");

        try
        {
            IDictionary iDictionary = new Dictionary<Int32,Byte>();
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
            iDictionary.Clear();
            if (iDictionary.Count != 0)
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
    #endregion

    #region Nagetive Test Cases
    public bool NegTest1()
    {
        bool retVal = true;

        TestLibrary.TestFramework.BeginScenario("NegTest1: The IDictionary is read-only");

        try
        {
            IDictionary iDictionary = new MyIDictionary();
            iDictionary.Clear();
            TestLibrary.TestFramework.LogError("101", "The NotSupportedException was not thrown as expected");
            retVal = false;
        }
        catch (NotSupportedException)
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
    #endregion
    #endregion

    public static int Main()
    {
        IDictionaryClear test = new IDictionaryClear();

        TestLibrary.TestFramework.BeginTestCase("IDictionaryClear");

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
        if (this.IsReadOnly)
        {
            throw new NotSupportedException("The IDictionary is read-only");
        }
        else
        {
            throw new Exception("The method or operation is not implemented.");
        }
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
