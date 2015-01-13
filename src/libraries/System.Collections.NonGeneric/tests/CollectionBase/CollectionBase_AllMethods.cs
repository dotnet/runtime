// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using Xunit;

public class CollectionBase_AllMethods
{
    public bool runTest()
    {
        //////////// Global Variables used for all tests
        int iCountErrors = 0;
        int iCountTestcases = 0;

        MyCollectionBase mycol1;
        MyCollectionBase mycol2;
        Foo f1;
        Foo[] arrF1;
        IEnumerator enu1;
        Int32 iCount;
        Object obj1;

        try
        {
            do
            {
                /////////////////////////  START TESTS ////////////////////////////
                ///////////////////////////////////////////////////////////////////

                //[]CollectionBase implements IList (which means both ICollection and IEnumerator as well :-()
                //To test this class, we will implement our own strongly typed CollectionBase and call its methods
                iCountTestcases++;

                //[] Count property
                iCountTestcases++;
                mycol1 = new MyCollectionBase();
                if (mycol1.Count != 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_234dnvf! Expected value not returned, " + mycol1.Count);
                }
                for (int i = 0; i < 100; i++)
                    mycol1.Add(new Foo());
                if (mycol1.Count != 100)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2075sg! Expected value not returned, " + mycol1.Count);
                }

                //[]this[int index] argument checking 
                iCountTestcases++;
                mycol1 = new MyCollectionBase();
                for (int i = 0; i < 100; i++)
                    mycol1.Add(new Foo());

                try
                {
                    Object o = mycol1[-1];
                    iCountErrors++;
                    Console.WriteLine("Err_0823afds Expected CollectionBase[-1] to throw ArgumentOutOfRangeException");
                }
                catch (ArgumentOutOfRangeException) { }

                try
                {
                    Object o = mycol1[100];
                    iCountErrors++;
                    Console.WriteLine("Err_8094sadd Expected CollectionBase[100] with only 100 elements to throw ArgumentOutOfRangeException");
                }
                catch (ArgumentOutOfRangeException) { }

                try
                {
                    mycol1[-1] = new Foo();
                    iCountErrors++;
                    Console.WriteLine("Err_2773apnn Expected CollectionBase[-1] to throw ArgumentOutOfRangeException");
                }
                catch (ArgumentOutOfRangeException) { }

                try
                {
                    mycol1[100] = new Foo();
                    iCountErrors++;
                    Console.WriteLine("Err_3402hnxz Expected CollectionBase[100] with only 100 elements to throw ArgumentOutOfRangeException");
                }
                catch (ArgumentOutOfRangeException) { }

                //[]CopyTo
                iCountTestcases++;
                mycol1 = new MyCollectionBase();
                for (int i = 0; i < 100; i++)
                    mycol1.Add(new Foo(i, i.ToString()));
                arrF1 = new Foo[100];
                mycol1.CopyTo(arrF1, 0);
                for (int i = 0; i < 100; i++)
                {
                    if ((arrF1[i].IValue != i) || (arrF1[i].SValue != i.ToString()))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2874sf_" + i + "! Expected value not returned");
                    }
                }

                //Argument checking
                mycol1 = new MyCollectionBase();
                for (int i = 0; i < 100; i++)
                    mycol1.Add(new Foo(i, i.ToString()));
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

                iCountTestcases++;
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
                mycol1 = new MyCollectionBase();
                for (int i = 0; i < 100; i++)
                    mycol1.Add(new Foo(i, i.ToString()));

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
                    Console.WriteLine("fail, should throw InvalidOperationException, thrown, " + ex.GetType().Name);
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
                mycol1 = new MyCollectionBase();
                if (mycol1.IsSynchronized)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_275eg! Expected value not returned, " + ((ICollection)mycol1).IsSynchronized);
                }

                //[]IsReadOnly
                iCountTestcases++;
                mycol1 = new MyCollectionBase();
                if (mycol1.IsReadOnly)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2508aszp! Expected value not returned, " + mycol1.IsReadOnly);
                }

                //[]IsFixedSize
                iCountTestcases++;
                mycol1 = new MyCollectionBase();
                if (mycol1.IsFixedSize)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_0803puj! Expected value not returned, " + mycol1.IsFixedSize);
                }

                //[]SyncRoot - SyncRoot should be the reference to the underlying collection, not to MyCollectionBase
                iCountTestcases++;
                mycol1 = new MyCollectionBase();
                obj1 = mycol1.SyncRoot;
                mycol2 = mycol1;
                if (obj1 != mycol2.SyncRoot)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9745sg! Expected value not returned");
                }

                //End of ICollection and IEnumerator methods
                //Now to IList methods
                //[]Add, this, Contains, RemoveAt, IndexOf
                iCountTestcases++;
                mycol1 = new MyCollectionBase();
                for (int i = 0; i < 100; i++)
                    mycol1.Add(new Foo(i, i.ToString()));
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
                }
                for (int i = 99; i >= 0; i--)
                {
                    mycol1.RemoveAt(0);
                    if ((mycol1.Count != (i)))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2975sg_" + i + "! Expected value not returned");
                    }
                    //WE have removed the first half by the time the counter comes there
                    if (i >= 50)
                    {
                        if ((!mycol1.Contains(new Foo(i, i.ToString()))))
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_975wg_" + i + "! Expected value not returned");
                        }
                    }
                    else
                    {
                        if ((mycol1.Contains(new Foo(i, i.ToString()))))
                        {
                            iCountErrors++;
                            Console.WriteLine("Err_975wg_" + i + "! Expected value not returned");
                        }
                    }
                }

                //[]Rest of the IList methods: this-set, Remove, Insert, Clear, IsFixedSize, IsReadOnly
                iCountTestcases++;
                mycol1 = new MyCollectionBase();
                if (((IList)mycol1).IsFixedSize)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_9753sfg! Expected value not returned, " + ((IList)mycol1).IsFixedSize);
                }
                if (((IList)mycol1).IsReadOnly)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_834sg! Expected value not returned, " + ((IList)mycol1).IsReadOnly);
                }
                for (int i = 0; i < 100; i++)
                {
                    mycol1.Insert(i, new Foo(i, i.ToString()));
                    if (!(mycol1.Contains(new Foo(i, i.ToString()))))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_753sg_" + i + "! Expected value not returned");
                    }
                }
                for (int i = 0; i < 100; i++)
                {
                    mycol1.Remove(new Foo(i, i.ToString()));
                    if ((mycol1.Contains(new Foo(i, i.ToString()))))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2085sg_" + i + "! Expected value not returned");
                    }
                }
                mycol1 = new MyCollectionBase();
                for (int i = 0; i < 100; i++)
                    mycol1.Add(new Foo(i, i.ToString()));
                mycol1.Clear();
                if (mycol1.Count != 0)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_847tfgdg! Expected value not returned, " + mycol1.Count);
                }
                mycol1 = new MyCollectionBase();
                for (int i = 0; i < 100; i++)
                    mycol1.Add(new Foo(i, i.ToString()));
                for (int i = 0, j = 100; i < 100; i++, j--)
                    mycol1[i] = new Foo(j, j.ToString());
                for (int i = 0, j = 100; i < 100; i++, j--)
                {
                    if ((mycol1.IndexOf(new Foo(j, j.ToString())) != i))
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_7342rfg_" + i + "! Expected value not returned");
                    }
                }

                mycol1 = new MyCollectionBase();
                mycol1.Add(new Foo());

                ///////////////////////////////////////////////////////////////////
                /////////////////////////// END TESTS /////////////////////////////


            } while (false);
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine(" : Error Err_8888yyy! exc_general==\n" + exc_general.ToString());
        }

        ////  Finish Diagnostics
        if (iCountErrors == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    [Fact]
    public static void ExecuteCollectionBase_AllMethods()
    {
        bool bResult = false;
        var test = new CollectionBase_AllMethods();

        try
        {
            bResult = test.runTest();
        }
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine("Fail! Error Err_main! Uncaught Exception in main(), exc_main==" + exc_main);
        }

        Assert.Equal(true, bResult);
    }

    //CollectionBase is provided to be used as the base class for strongly typed collections. Lets use one of our own here
    public class MyCollectionBase : CollectionBase
    {
        public MyCollectionBase()
        {
        }

        public int Add(Foo f1)
        {
            return List.Add(f1);
        }

        public Foo this[int indx]
        {
            get { return (Foo)List[indx]; }
            set { List[indx] = value; }
        }

        public void CopyTo(Array array, Int32 index)
        {
            ((ICollection)List).CopyTo(array, index);
        }

        public Int32 IndexOf(Foo f)
        {
            return ((IList)List).IndexOf(f);
        }

        public Boolean Contains(Foo f)
        {
            return ((IList)List).Contains(f);
        }

        public void Insert(Int32 index, Foo f)
        {
            ((IList)List).Insert(index, f);
        }

        public void Remove(Foo f)
        {
            ((IList)List).Remove(f);
        }

        public bool IsSynchronized
        {
            get { return ((ICollection)this).IsSynchronized; }
        }

        public Object SyncRoot
        {
            get { return ((ICollection)this).SyncRoot; }
        }

        public bool IsReadOnly
        {
            get { return List.IsReadOnly; }
        }

        public bool IsFixedSize
        {
            get { return List.IsFixedSize; }
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
}