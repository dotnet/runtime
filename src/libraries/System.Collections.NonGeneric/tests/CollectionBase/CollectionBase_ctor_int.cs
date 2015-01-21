// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class CollectionBase_ctor_int
{
    public const int MAX_RND_CAPACITY = 512;
    public const int LARGE_CAPACITY = 1024;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _retValue = 100;

    public delegate bool TestDelegate();

    [Fact]
    public static void ctor_intTests()
    {
        var objTest = new CollectionBase_ctor_int();

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

        retValue &= BeginTestcase(new TestDelegate(Capacity_Int32MinValue));
        retValue &= BeginTestcase(new TestDelegate(Capacity_NegRnd));
        retValue &= BeginTestcase(new TestDelegate(Capacity_Neg1));
        retValue &= BeginTestcase(new TestDelegate(Capacity_0));
        retValue &= BeginTestcase(new TestDelegate(Capacity_1));
        retValue &= BeginTestcase(new TestDelegate(Capacity_PosRnd));
        retValue &= BeginTestcase(new TestDelegate(Capacity_Large));
        retValue &= BeginTestcase(new TestDelegate(Capacity_Int32MaxValue));


        return retValue;
    }

    public bool Capacity_Int32MinValue()
    {
        bool retValue = true;

        retValue &= VerifyCapacity(Int32.MinValue, typeof(ArgumentOutOfRangeException));

        if (!retValue)
        {
            Console.WriteLine("Err_001!!! Verifying Int32.MinValue Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_NegRnd()
    {
        bool retValue = true;
        Random rndGen = new Random(-55);

        retValue &= VerifyCapacity(rndGen.Next(Int32.MinValue, 0), typeof(ArgumentOutOfRangeException));

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying negative random Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_Neg1()
    {
        bool retValue = true;

        retValue &= VerifyCapacity(-1, typeof(ArgumentOutOfRangeException));

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying -1 Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_0()
    {
        bool retValue = true;

        retValue &= VerifyCapacity(0, 0, null);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying 0 Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_1()
    {
        bool retValue = true;

        retValue &= VerifyCapacity(1, 1, null);

        if (!retValue)
        {
            Console.WriteLine("Err_005!!! Verifying 1 Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_PosRnd()
    {
        bool retValue = true;
        Random rndGen = new Random(-55);
        int rndCapacity = rndGen.Next(2, MAX_RND_CAPACITY);

        retValue &= VerifyCapacity(rndCapacity, rndCapacity, null);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying positive random Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_Large()
    {
        bool retValue = true;

        retValue &= VerifyCapacity(LARGE_CAPACITY, LARGE_CAPACITY, null);

        if (!retValue)
        {
            Console.WriteLine("Err_007!!! Verifying1 Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_Int32MaxValue()
    {
        bool retValue = true;

        retValue &= VerifyCapacity(Int32.MaxValue, Int32.MaxValue, typeof(OutOfMemoryException));

        if (!retValue)
        {
            Console.WriteLine("Err_008!!! Verifying Int32.MaxValue Capacity FAILED");
        }

        return retValue;
    }

    public bool VerifyCapacity(int capacity, Type expectedException)
    {
        return VerifyCapacity(capacity, 0, expectedException);
    }

    public bool VerifyCapacity(int capacity, int expectedCapacity, Type expectedException)
    {
        bool retValue = true;
        MyCollection mc = null;

        try
        {
            mc = new MyCollection(capacity);

            if (null != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected exception {0} and NOTHING was thrown", expectedException);
                retValue = false;
            }
        }
        catch (System.Exception e)
        {
            if (null == expectedException)
            {
                Console.WriteLine("ERROR!!! Expected NO exception to be thrown and {0} was thrown", e.GetType());
                retValue = false;
            }
            else if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!! Expected exception {0} and {1} was thrown", expectedException, e.GetType());
                retValue = false;
            }
        }

        if (mc == null && expectedException == null)
        {
            Console.WriteLine("ERROR!!! Expected MyCollection NOT to be null");
            retValue = false;
        }

        if (mc != null && expectedException != null)
        {
            Console.WriteLine("ERROR!!! Expected MyCollection to be null");
            retValue = false;
        }

        if (mc != null)
        {
            if (mc.Capacity != expectedCapacity)
            {
                Console.WriteLine("ERROR!!! MyCollection.Capacity={0} expected={1}", mc.Capacity, expectedCapacity);
                retValue = false;
            }

            if (mc.InnerListCapacity != expectedCapacity)
            {
                Console.WriteLine("ERROR!!! MyCollection.InnerList.Capacity={0} expected={1}", mc.InnerListCapacity, expectedCapacity);
                retValue = false;
            }
        }

        return retValue;
    }


    private bool BeginTestcase(TestDelegate test)
    {
        bool retValue = true;

        if (test())
        {
            _numTestcases++;
            retValue = true;
        }
        else
        {
            _numErrors++;
            retValue = false;
        }

        return retValue;
    }

    public class MyCollection : CollectionBase
    {
        public MyCollection(int capacity) : base(capacity)
        {

        }

        public int InnerListCapacity
        {
            get
            {
                return InnerList.Capacity;
            }
        }
    }
}

