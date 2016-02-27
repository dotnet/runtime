// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections;

/// <summary>
/// System.Collections.ICollection.SyncRoot
/// </summary>
public class ICollectionSyncRoot
{
    #region Public Methods
    public bool RunTests()
    {
        bool retVal = true;

        TestLibrary.TestFramework.LogInformation("[Positive]");
        retVal = PosTest1() && retVal;
        retVal = PosTest2() && retVal;

        return retVal;
    }

    #region Positive Test Cases
    public bool PosTest1()
    {
        bool retVal = true;

        TestICollectionSyncRoot test = new TestICollectionSyncRoot();

        TestLibrary.TestFramework.BeginScenario("PosTest1: Verify the  value of SyncRoot property is itself");

        try
        {
            if (test.SyncRoot != test)
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
        int  count = 2;

        Array array = new int[count];

        TestLibrary.TestFramework.BeginScenario("PosTest2: Verify the  array of SyncRoot property is itself");

        try
        {
            if (((ICollection)array).SyncRoot != array)
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
    #endregion


    #endregion

    public static int Main()
    {
        ICollectionSyncRoot test = new ICollectionSyncRoot();

        TestLibrary.TestFramework.BeginTestCase("Test for Property:System.Collections.ICollection.SyncRoot");

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
public class TestICollectionSyncRoot : ICollection
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
        get { throw new System.Exception("The method or operation is not implemented."); }
    }

    public object SyncRoot
    {
        get { return this; }
    }

    #endregion

    #region IEnumerable Members

    public IEnumerator GetEnumerator()
    {
        throw new System.Exception("The method or operation is not implemented.");
    }

    #endregion
}

