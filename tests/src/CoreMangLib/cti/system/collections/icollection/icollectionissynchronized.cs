// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// System.Collections.ICollection.IsSynchronized
/// </summary>
public class ICollectionIsSynchronized
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

        TestICollectionIsSynchronized test = new TestICollectionIsSynchronized();

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the default value of IsSynchronized property is false");

        try
        {
            if (test.IsSynchronized)
            {
                TestLibrary.TestFramework.LogError("001", "The result is not the value as expected");
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

        TestICollectionIsSynchronized test = new TestICollectionIsSynchronized();
        test.setIsSynchronized(true);

        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify set the IsSynchronized properyty is true");

        try
        {
            if (!test.IsSynchronized)
            {
                TestLibrary.TestFramework.LogError("003", "The result is not the value as expected");
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

        List<object> arrayList = new List<object>();

        TestLibrary.TestFramework.BeginScenario("PosTest3: Verify the ArrayList's IsSynchronized property");

        try
        {
            if (((ICollection)arrayList).IsSynchronized)
            {
                TestLibrary.TestFramework.LogError("005", "The result is not the value as expected");
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


    #endregion

    public static int Main()
    {
        ICollectionIsSynchronized test = new ICollectionIsSynchronized();

        TestLibrary.TestFramework.BeginTestCase("Test for Property:System.Collections.ICollection.IsSynchronized");

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
public class TestICollectionIsSynchronized : ICollection
{
    #region ICollection Members

    private bool m_isSynchronized = false;

    public void setIsSynchronized(bool isSyn)
    {
        m_isSynchronized = isSyn;
    }

    public void CopyTo(Array array, int index)
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    public int Count
    {
        get { throw new System.Exception("The method or operation is not implemented."); }
    }

    public bool IsSynchronized
    {
        get { return m_isSynchronized; }
    }

    public object SyncRoot
    {
        get { throw new System.Exception("The method or operation is not implemented."); }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    #endregion
}

