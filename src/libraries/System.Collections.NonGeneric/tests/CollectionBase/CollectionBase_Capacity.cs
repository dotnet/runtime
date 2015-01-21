// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class Capacity
{
    public const int MAX_RND_CAPACITY = 512;
    public const int LARGE_CAPACITY = 1024;
    public static readonly int DEFAULT_CAPACITY = new ArrayList().Capacity;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _retValue = 100;

    public delegate bool TestDelegate();

    [Fact]
    public static void CapacityTests()
    {
        Capacity objTest = new Capacity();

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

        retValue &= BeginTestcase(new TestDelegate(Capacity_2_With2Elements));
        retValue &= BeginTestcase(new TestDelegate(Capacity_Neg1_With2Elements));
        retValue &= BeginTestcase(new TestDelegate(Capacity_0_With2Elements));
        retValue &= BeginTestcase(new TestDelegate(Capacity_1_With2Elements));
        retValue &= BeginTestcase(new TestDelegate(Capacity_3_With2Elements));
        retValue &= BeginTestcase(new TestDelegate(Capacity_64_With2Elements));

        retValue &= BeginTestcase(new TestDelegate(Capacity_Grow));
        retValue &= BeginTestcase(new TestDelegate(Capacity_Remove));
        retValue &= BeginTestcase(new TestDelegate(Capacity_GrowRemove));
        retValue &= BeginTestcase(new TestDelegate(Capacity_EqualNumElements));

        return retValue;
    }

    public bool Capacity_Int32MinValue()
    {
        bool retValue = true;

        retValue &= SetCapacityAndVerify(Int32.MinValue, typeof(ArgumentOutOfRangeException));

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

        retValue &= SetCapacityAndVerify(rndGen.Next(Int32.MinValue, 0), typeof(ArgumentOutOfRangeException));

        if (!retValue)
        {
            Console.WriteLine("Err_002!!! Verifying negative random Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_Neg1()
    {
        bool retValue = true;

        retValue &= SetCapacityAndVerify(-1, typeof(ArgumentOutOfRangeException));

        if (!retValue)
        {
            Console.WriteLine("Err_003!!! Verifying -1 Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_0()
    {
        bool retValue = true;

        retValue &= SetCapacityAndVerify(0, 0, null);

        if (!retValue)
        {
            Console.WriteLine("Err_004!!! Verifying 0 Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_1()
    {
        bool retValue = true;

        retValue &= SetCapacityAndVerify(1, 1, null);

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

        retValue &= SetCapacityAndVerify(rndCapacity, rndCapacity, null);

        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying positive random Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_Large()
    {
        bool retValue = true;

        retValue &= SetCapacityAndVerify(LARGE_CAPACITY, LARGE_CAPACITY, null);

        if (!retValue)
        {
            Console.WriteLine("Err_007!!! Verifying1 Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_Int32MaxValue()
    {
        bool retValue = true;

        retValue &= SetCapacityAndVerify(Int32.MaxValue, typeof(OutOfMemoryException));

        if (!retValue)
        {
            Console.WriteLine("Err_008!!! Verifying Int32.MaxValue Capacity FAILED");
        }

        return retValue;
    }

    public bool Capacity_2_With2Elements()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection();
        Random rndGen = new Random(-55);

        mc.Add(rndGen.Next());
        mc.Add(rndGen.Next());

        retValue &= SetCapacityAndVerify(mc, 2, 2, null);

        if (!retValue)
        {
            Console.WriteLine("Err_009!!! Verifying setting capacity to 2 with 2 elements in the collection FAILED");
        }

        return retValue;
    }

    public bool Capacity_Neg1_With2Elements()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection();
        Random rndGen = new Random(-55);

        mc.Add(rndGen.Next());
        mc.Add(rndGen.Next());

        retValue &= SetCapacityAndVerify(mc, -1, mc.Capacity, typeof(ArgumentOutOfRangeException));

        if (!retValue)
        {
            Console.WriteLine("Err_010!!! Verifying setting capacity to -1 with 2 elements in the collection FAILED");
        }

        return retValue;
    }

    public bool Capacity_0_With2Elements()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection();
        Random rndGen = new Random(-55);

        mc.Add(rndGen.Next());
        mc.Add(rndGen.Next());

        retValue &= SetCapacityAndVerify(mc, 0, mc.Capacity, typeof(ArgumentOutOfRangeException));

        if (!retValue)
        {
            Console.WriteLine("Err_011!!! Verifying setting capacity to 0 with 2 elements in the collection FAILED");
        }

        return retValue;
    }

    public bool Capacity_1_With2Elements()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection();
        Random rndGen = new Random(-55);

        mc.Add(rndGen.Next());
        mc.Add(rndGen.Next());

        retValue &= SetCapacityAndVerify(mc, 1, mc.Capacity, typeof(ArgumentOutOfRangeException));

        if (!retValue)
        {
            Console.WriteLine("Err_012!!! Verifying setting capacity to 1 with 2 elements in the collection FAILED");
        }

        return retValue;
    }

    public bool Capacity_3_With2Elements()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection();
        Random rndGen = new Random(-55);

        mc.Add(rndGen.Next());
        mc.Add(rndGen.Next());

        retValue &= SetCapacityAndVerify(mc, 3, 3, null);

        if (!retValue)
        {
            Console.WriteLine("Err_012!!! Verifying setting capacity to 3 with 2 elements in the collection FAILED");
        }

        return retValue;
    }

    public bool Capacity_64_With2Elements()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection();
        Random rndGen = new Random(-55);

        mc.Add(rndGen.Next());
        mc.Add(rndGen.Next());

        retValue &= SetCapacityAndVerify(mc, 64, 64, null);

        if (!retValue)
        {
            Console.WriteLine("Err_013!!! Verifying setting capacity to 64 with 2 elements in the collection FAILED");
        }

        return retValue;
    }

    public bool Capacity_Grow()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection(4);
        int initialCapacity = mc.Capacity;
        Random rndGen = new Random(-55);

        for (int i = 0; i < initialCapacity + 1; i++)
        {
            mc.Add(rndGen.Next());
        }

        retValue &= VerifyCapacity(mc, initialCapacity * 2);

        if (!retValue)
        {
            Console.WriteLine("Err_014!!! Verifying growing the capacity by adding elements tot he collection FAILED");
        }

        return retValue;
    }

    public bool Capacity_Remove()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection(4);
        int initialCapacity = mc.Capacity;
        Random rndGen = new Random(-55);

        for (int i = 0; i < initialCapacity; i++)
        {
            mc.Add(rndGen.Next());
        }

        mc.RemoveAt(initialCapacity - 1);

        retValue &= SetCapacityAndVerify(mc, initialCapacity - 1, initialCapacity - 1, null);

        if (!retValue)
        {
            Console.WriteLine("Err_015!!! Verifying adding the same number of elements to the collection as the Capacity then remoiving one FAILED");
        }

        return retValue;
    }

    public bool Capacity_GrowRemove()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection(4);
        int initialCapacity = mc.Capacity;
        Random rndGen = new Random(-55);

        for (int i = 0; i < initialCapacity + 1; i++)
        {
            mc.Add(rndGen.Next());
        }
        mc.RemoveAt(initialCapacity);

        retValue &= SetCapacityAndVerify(mc, initialCapacity, initialCapacity, null);

        if (!retValue)
        {
            Console.WriteLine("Err_016!!! Verifying growing the capacity by adding elements tot he collection then removing one FAILED");
        }

        return retValue;
    }

    public bool Capacity_EqualNumElements()
    {
        bool retValue = true;
        MyCollection mc = new MyCollection(4);
        int initialCapacity = mc.Capacity;
        Random rndGen = new Random(-55);

        for (int i = 0; i < initialCapacity; i++)
        {
            mc.Add(rndGen.Next());
        }

        retValue &= VerifyCapacity(mc, 4);

        if (!retValue)
        {
            Console.WriteLine("Err_017!!! Verifying adding the same number of elements to the collection as the Capacity then remoiving one FAILED");
        }

        return retValue;
    }

    public bool SetCapacityAndVerify(int capacity, Type expectedException)
    {
        return SetCapacityAndVerify(capacity, DEFAULT_CAPACITY, expectedException);
    }

    public bool SetCapacityAndVerify(int capacity, int expectedCapacity, Type expectedException)
    {
        MyCollection mc = new MyCollection();
        return SetCapacityAndVerify(mc, capacity, expectedCapacity, expectedException);
    }

    public bool SetCapacityAndVerify(MyCollection mc, int capacity, int expectedCapacity, Type expectedException)
    {
        bool retValue = true;

        try
        {
            mc.Capacity = capacity;

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

        retValue &= VerifyCapacity(mc, expectedCapacity);

        return retValue;
    }

    public bool VerifyCapacity(MyCollection mc, int expectedCapacity)
    {
        bool retValue = true;

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
        public MyCollection() : base()
        {

        }

        public MyCollection(int capacity) : base(capacity)
        {

        }

        public void Add(int value)
        {
            InnerList.Add(value);
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