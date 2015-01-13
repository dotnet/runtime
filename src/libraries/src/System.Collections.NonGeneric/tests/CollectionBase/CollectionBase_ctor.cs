// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class CollectionBase_ctor
{
    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _retValue = 100;

    public delegate bool TestDelegate();

    [Fact]
    public static void ctorTests()
    {
        var objTest = new CollectionBase_ctor();

        try
        {
            objTest.RunTest();
        }
        catch (Exception e)
        {
            Console.WriteLine(" : FAIL The following exception was thorwn in RunTest(): \n" + e.ToString());
            objTest._numErrors++;
            objTest._retValue = 1;
        }

        Assert.Equal(objTest._retValue, 100);
    }

    public bool RunTest()
    {
        bool retValue = true;

        retValue &= BeginTestcase(new TestDelegate(DefaultCapacity));

        return retValue;
    }

    public bool DefaultCapacity()
    {
        bool retValue = true;
        MyCollection cb = new MyCollection();
        ArrayList al = new ArrayList();

        if (cb.Capacity != al.Capacity)
        {
            Console.WriteLine("ERROR!!! CollectionBase.Capacity={0} expected={1}", cb.Capacity, al.Capacity);
            retValue = false;
        }

        if (cb.InnerListCapacity != al.Capacity)
        {
            Console.WriteLine("ERROR!!! CollectionBase.InnerList.Capacity={0} expected={1}", cb.InnerListCapacity, al.Capacity);
            retValue = false;
        }

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying Capacity with default constructor FAILED");
        }
        return retValue;
    }

    private bool BeginTestcase(TestDelegate test)
    {
        if (test())
        {
            _numTestcases++;
            return true;
        }
        else
        {
            _numErrors++;
            return false;
        }
    }

    public class MyCollection : CollectionBase
    {
        public int InnerListCapacity
        {
            get
            {
                return InnerList.Capacity;
            }
        }


    }
}
