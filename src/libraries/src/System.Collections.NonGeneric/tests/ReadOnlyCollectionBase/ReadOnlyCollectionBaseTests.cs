// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using Xunit;

public class ReadOnlyCollectionBaseTests
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        MyReadOnlyCollectionBase mycol1;
        MyReadOnlyCollectionBase mycol2;
        Foo f1;
        Foo[] arrF1;
        Foo[] arrF2;
        IEnumerator enu1;
        Int32 iCount;
        Object obj1;
        ReadOnlyCollectionBase collectionBase;

        try
        {
            do
            {
                /////////////////////////  START TESTS ////////////////////////////
                ///////////////////////////////////////////////////////////////////

                //[]ReadOnlyCollectionBase implements IList (which means both ICollection and IEnumerator as well :-()
                //To test this class, we will implement our own strongly typed ReadOnlyCollectionBase and call its methods
                iCountTestcases++;

                //[] SyncRoot property
                iCountTestcases++;
                arrF1 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF1[i] = new Foo();
                mycol1 = new MyReadOnlyCollectionBase(arrF1);

                if (mycol1.SyncRoot is ArrayList)
                {
                    iCountErrors++;
                    Console.WriteLine("Error SyncRoot returned ArrayList");
                }

                //[] Count property
                iCountTestcases++;
                arrF1 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF1[i] = new Foo();
                mycol1 = new MyReadOnlyCollectionBase(arrF1);
                if (mycol1.Count != 100)
                {
                    iCountErrors++;
                    Console.WriteLine("Error Expected value not returned, " + mycol1.Count);
                }

                //[]CopyTo
                iCountTestcases++;
                arrF2 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF2[i] = new Foo(i, i.ToString());
                mycol1 = new MyReadOnlyCollectionBase(arrF2);
                arrF1 = new Foo[100];
                mycol1.CopyTo(arrF1, 0);
                for (int i = 0; i < 100; i++)
                {
                    if ((arrF1[i].IValue != i) || (arrF1[i].SValue != i.ToString()))
                    {
                        iCountErrors++;
                        Console.WriteLine("Error #" + i + "! Expected value not returned");
                    }
                }

                //Argument checking
                arrF2 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF2[i] = new Foo(i, i.ToString());
                mycol1 = new MyReadOnlyCollectionBase(arrF2);
                arrF1 = new Foo[100];
                try
                {
                    mycol1.CopyTo(arrF1, 50);

                    iCountErrors++;
                    Console.WriteLine("Err_2075dfgv! Exception not thrown");
                }
                catch (ArgumentException)
                {
                }
                catch (Exception ex)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_854732f! Unexception not thrown, " + ex.GetType().Name);
                }

                try
                {
                    mycol1.CopyTo(arrF1, -1);

                    iCountErrors++;
                    Console.WriteLine("Err_2075dfgv! Exception not thrown");
                }
                catch (ArgumentException)
                {
                }
                catch (Exception ex)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_854732f! Unexception not thrown, " + ex.GetType().Name);
                }

                arrF1 = new Foo[200];
                mycol1.CopyTo(arrF1, 100);
                for (int i = 0; i < 100; i++)
                {
                    if ((arrF1[100 + i].IValue != i) || (arrF1[100 + i].SValue != i.ToString()))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2874sf_" + i + "! Expected value not returned");
                    }
                }

                //[]GetEnumerator
                iCountTestcases++;
                arrF2 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF2[i] = new Foo(i, i.ToString());
                mycol1 = new MyReadOnlyCollectionBase(arrF2);

                enu1 = mycol1.GetEnumerator();
                //Calling current should throw here
                try
                {
                    //Calling current should throw here
                    f1 = (Foo)enu1.Current;

                    iCountErrors++;
                    Console.WriteLine("Err_87543! Exception not thrown");
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine("failure should throw InvalidOperationException, thrown, " + ex.GetType().Name);
                }

                iCount = 0;
                while (enu1.MoveNext())
                {
                    f1 = (Foo)enu1.Current;
                    if ((f1.IValue != iCount) || (f1.SValue != iCount.ToString()))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_87543! does not match, " + f1.IValue);
                    }
                    iCount++;
                }
                if (iCount != 100)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_87543! doesnot match");
                }

                //Calling current should throw here
                try
                {
                    //Calling current should throw here
                    f1 = (Foo)enu1.Current;

                    iCountErrors++;
                    Console.WriteLine("Err_438fsfd! Exception not thrown");
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception ex)
                {
                    Console.WriteLine("fail, should throw InvalidOperationException, thrown, " + ex.GetType().Name);
                }

                //[]IsSynchronized
                iCountTestcases++;
                arrF2 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF2[i] = new Foo(i, i.ToString());
                if (((ICollection)mycol1).IsSynchronized)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_275eg! Expected value not returned, " + ((ICollection)mycol1).IsSynchronized);
                }

                //[]SyncRoot
                iCountTestcases++;
                arrF2 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF2[i] = new Foo(i, i.ToString());
                obj1 = mycol1.SyncRoot;
                mycol2 = mycol1;
                if (obj1 != mycol2.SyncRoot)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9745sg! Expected value not returned");
                }

                //End of ICollection and IEnumerator methods
                //Now to IList methods
                //[]this, Contains, IndexOf
                iCountTestcases++;
                arrF2 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF2[i] = new Foo(i, i.ToString());
                mycol1 = new MyReadOnlyCollectionBase(arrF2);
                for (int i = 0; i < 100; i++)
                {
                    if ((mycol1[i].IValue != i) || (mycol1[i].SValue != i.ToString()))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2974swsg_" + i + "! Expected value not returned");
                    }
                    if ((mycol1.IndexOf(new Foo(i, i.ToString())) != i))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_205sg_" + i + "! Expected value not returned");
                    }
                    if ((!mycol1.Contains(new Foo(i, i.ToString()))))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_975wg_" + i + "! Expected value not returned");
                    }
                }

                //[]Rest of the IList methods: IsFixedSize, IsReadOnly
                iCountTestcases++;
                arrF2 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF2[i] = new Foo(i, i.ToString());
                mycol1 = new MyReadOnlyCollectionBase(arrF2);
                if (!mycol1.IsFixedSize)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9753sfg! Expected value not returned, " + mycol1.IsFixedSize);
                }
                if (!mycol1.IsReadOnly)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_834sg! Expected value not returned, " + mycol1.IsReadOnly);
                }

                //The following operations are not allowed by the compiler. Hence, commented out
                arrF2 = new Foo[100];
                for (int i = 0; i < 100; i++)
                    arrF2[i] = new Foo(i, i.ToString());
                mycol1 = new MyReadOnlyCollectionBase(arrF2);

                //[] Verify Count is virtual
                iCountTestcases++;
                collectionBase = new VirtualTestReadOnlyCollection();

                if (collectionBase.Count != Int32.MinValue)
                {
                    Console.WriteLine("Err_707272hapba Expected Count={0} actual={1}", Int32.MinValue, collectionBase.Count);
                }

                //[] Verify Count is virtual
                iCountTestcases++;
                collectionBase = new VirtualTestReadOnlyCollection();

                if (collectionBase.GetEnumerator() != null)
                {
                    Console.WriteLine("Err_456548ahpba Expected GetEnumerator()=null actual={1}", null, collectionBase.GetEnumerator());
                }


                ///////////////////////////////////////////////////////////////////
                /////////////////////////// END TESTS /////////////////////////////
            } while (false);

        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  , exc_general==\n" + exc_general.ToString());
        }


        Assert.Equal(0, iCountErrors);

        ////  Finish Diagnostics
        if (iCountErrors == 0)
        {
            Console.WriteLine("Passed: iCountTestcases==" + iCountTestcases);
            return true;
        }
        else
        {
            Console.WriteLine("Failure! iCountErrors==" + iCountErrors);
            return false;
        }

    }



    [Fact]
    public static void ExecuteReadOnlyCollectionBaseTests()
    {
        bool bResult = false;
        var cbA = new ReadOnlyCollectionBaseTests();

        try
        {
            bResult = cbA.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("FAiL! Error Err_9999zzz! Uncaught Exception in main(), exc_main==" + exc_main);
        }
        Assert.Equal(true, bResult);
    }
}

//ReadOnlyCollectionBase is provided to be used as the base class for strongly typed collections. Lets use one of our own here.
//This collection only allows the type Foo
public class MyReadOnlyCollectionBase : ReadOnlyCollectionBase
{
    //we need a way of initializing this collection
    public MyReadOnlyCollectionBase(Foo[] values)
    {
        InnerList.AddRange(values);
    }

    public Foo this[int indx]
    {
        get { return (Foo)InnerList[indx]; }
    }

    public void CopyTo(Array array, Int32 index)
    {
        ((ICollection)this).CopyTo(array, index);// Use the base class explicit implemenation of ICollection.CopyTo
    }

    public virtual Object SyncRoot
    {
        get
        {
            return ((ICollection)this).SyncRoot;// Use the base class explicit implemenation of ICollection.SyncRoot
        }
    }

    public Int32 IndexOf(Foo f)
    {
        return ((IList)InnerList).IndexOf(f);
    }

    public Boolean Contains(Foo f)
    {
        return ((IList)InnerList).Contains(f);
    }

    public Boolean IsFixedSize
    {
        get { return true; }
    }

    public Boolean IsReadOnly
    {
        get { return true; }
    }
}

public class VirtualTestReadOnlyCollection : ReadOnlyCollectionBase
{
    public override int Count
    {
        get
        {
            return Int32.MinValue;
        }
    }

    public override IEnumerator GetEnumerator()
    {
        return null;
    }
}

public class Foo
{
    private Int32 _iValue;
    private String _strValue;

    public Foo()
    {
    }

    public Foo(Int32 i, String str)
    {
        _iValue = i;
        _strValue = str;
    }

    public Int32 IValue
    {
        get { return _iValue; }
        set { _iValue = value; }
    }

    public String SValue
    {
        get { return _strValue; }
        set { _strValue = value; }
    }

    public override Boolean Equals(Object obj)
    {
        if (obj == null)
            return false;
        if (!(obj is Foo))
            return false;
        if ((((Foo)obj).IValue == _iValue) && (((Foo)obj).SValue == _strValue))
            return true;
        return false;
    }

    public override Int32 GetHashCode()
    {
        return _iValue;
    }
}
