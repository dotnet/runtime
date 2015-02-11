// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Collections;
using Xunit;

namespace System.Collections.NonGenericTests
{
    public class ReadOnlyCollectionBaseTests
    {
        [Fact]
        public void TesReadOnlyCollectionBasBasic()
        {
            MyReadOnlyCollectionBase mycol1;
            MyReadOnlyCollectionBase mycol2;
            Foo f1;
            Foo[] arrF1;
            Foo[] arrF2;
            IEnumerator enu1;
            int iCount;
            object obj1;
            ReadOnlyCollectionBase collectionBase;

            //[]ReadOnlyCollectionBase implements IList (which means both ICollection and IEnumerator as well :-()
            //To test this class, we will implement our own strongly typed ReadOnlyCollectionBase and call its methods

            //[] SyncRoot property
            arrF1 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF1[i] = new Foo();
            mycol1 = new MyReadOnlyCollectionBase(arrF1);
            Assert.False(mycol1.SyncRoot is ArrayList, "Error SyncRoot returned ArrayList");

            //[] Count property
            arrF1 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF1[i] = new Foo();
            mycol1 = new MyReadOnlyCollectionBase(arrF1);

            Assert.Equal(100, mycol1.Count);

            //[]CopyTo
            arrF2 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF2[i] = new Foo(i, i.ToString());
            mycol1 = new MyReadOnlyCollectionBase(arrF2);
            arrF1 = new Foo[100];
            mycol1.CopyTo(arrF1, 0);

            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, arrF1[i].IValue);
                Assert.Equal(i.ToString(), arrF1[i].SValue);
            }

            //Argument checking
            arrF2 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF2[i] = new Foo(i, i.ToString());
            mycol1 = new MyReadOnlyCollectionBase(arrF2);
            arrF1 = new Foo[100];

            Assert.Throws<ArgumentException>(() =>
                         {
                             mycol1.CopyTo(arrF1, 50);
                         }
            );


            Assert.Throws<ArgumentOutOfRangeException>(() =>
                         {
                             mycol1.CopyTo(arrF1, -1);
                         }
            );

            arrF1 = new Foo[200];
            mycol1.CopyTo(arrF1, 100);
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i, arrF1[100 + i].IValue);
                Assert.Equal(i.ToString(), arrF1[100 + i].SValue);
            }

            //[]GetEnumerator
            arrF2 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF2[i] = new Foo(i, i.ToString());

            mycol1 = new MyReadOnlyCollectionBase(arrF2);

            enu1 = mycol1.GetEnumerator();
            //Calling current should throw here
            Assert.Throws<InvalidOperationException>(() =>
                         {
                             f1 = (Foo)enu1.Current;
                         }
            );

            iCount = 0;
            while (enu1.MoveNext())
            {
                f1 = (Foo)enu1.Current;

                Assert.False((f1.IValue != iCount) || (f1.SValue != iCount.ToString()), "Error, does not match, " + f1.IValue);
                iCount++;
            }

            Assert.Equal(100, iCount);

            //Calling current should throw here
            Assert.Throws<InvalidOperationException>(() =>
                         {
                             f1 = (Foo)enu1.Current;
                         }
            );

            //[]IsSynchronized
            arrF2 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF2[i] = new Foo(i, i.ToString());

            Assert.False(((ICollection)mycol1).IsSynchronized);

            //[]SyncRoot
            arrF2 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF2[i] = new Foo(i, i.ToString());
            obj1 = mycol1.SyncRoot;
            mycol2 = mycol1;

            Assert.Equal(obj1, mycol2.SyncRoot);

            //End of ICollection and IEnumerator methods
            //Now to IList methods
            //[]this, Contains, IndexOf
            arrF2 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF2[i] = new Foo(i, i.ToString());
            mycol1 = new MyReadOnlyCollectionBase(arrF2);
            for (int i = 0; i < 100; i++)
            {

                Assert.False((mycol1[i].IValue != i) || (mycol1[i].SValue != i.ToString()));

                Assert.False((mycol1.IndexOf(new Foo(i, i.ToString())) != i));

                Assert.False((!mycol1.Contains(new Foo(i, i.ToString()))));
            }

            //[]Rest of the IList methods: IsFixedSize, IsReadOnly
            arrF2 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF2[i] = new Foo(i, i.ToString());
            mycol1 = new MyReadOnlyCollectionBase(arrF2);

            Assert.True(mycol1.IsFixedSize);
            Assert.True(mycol1.IsReadOnly);

            //The following operations are not allowed by the compiler. Hence, commented out
            arrF2 = new Foo[100];
            for (int i = 0; i < 100; i++)
                arrF2[i] = new Foo(i, i.ToString());
            mycol1 = new MyReadOnlyCollectionBase(arrF2);

            //[] Verify Count is virtual
            collectionBase = new VirtualTestReadOnlyCollection();

            Assert.Equal(collectionBase.Count, int.MinValue);

            //[] Verify Count is virtual
            collectionBase = new VirtualTestReadOnlyCollection();

            Assert.Null(collectionBase.GetEnumerator());
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

        public void CopyTo(Array array, int index)
        {
            ((ICollection)this).CopyTo(array, index);// Use the base class explicit implemenation of ICollection.CopyTo
        }

        public virtual object SyncRoot
        {
            get
            {
                return ((ICollection)this).SyncRoot;// Use the base class explicit implemenation of ICollection.SyncRoot
            }
        }

        public int IndexOf(Foo f)
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
                return int.MinValue;
            }
        }

        public override IEnumerator GetEnumerator()
        {
            return null;
        }
    }

    public class Foo
    {
        private int _iValue;
        private String _strValue;

        public Foo()
        {
        }

        public Foo(int i, String str)
        {
            _iValue = i;
            _strValue = str;
        }

        public int IValue
        {
            get { return _iValue; }
            set { _iValue = value; }
        }

        public String SValue
        {
            get { return _strValue; }
            set { _strValue = value; }
        }

        public override Boolean Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is Foo))
                return false;
            if ((((Foo)obj).IValue == _iValue) && (((Foo)obj).SValue == _strValue))
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return _iValue;
        }
    }
}
