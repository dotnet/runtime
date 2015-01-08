// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Specialized;

// Derived class to test NameObjectCollectionBase

namespace System.Collections.Specialized.Tests
{
    public class MyNameObjectCollection : NameObjectCollectionBase
    {
        // Constructors
        public MyNameObjectCollection()
            : base()
        {
        }
        public MyNameObjectCollection(int capacity)
            : base(capacity)
        {
        }
        public MyNameObjectCollection(IEqualityComparer comparer)
            : base(comparer)
        {
        }
        public MyNameObjectCollection(int capacity, IEqualityComparer comparer)
            : base(capacity, comparer)
        {
        }

        // Implement protected members
        public new bool IsReadOnly
        {
            get { return base.IsReadOnly; }
            set { base.IsReadOnly = value; }
        }
        public bool HasKeys()
        {
            return BaseHasKeys();
        }
        public void Add(String name, Foo value)
        {
            BaseAdd(name, value);
        }
        public void Remove(String name)
        {
            BaseRemove(name);
        }
        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }
        public void Clear()
        {
            BaseClear();
        }
        public Foo this[String name]
        {
            get { return (Foo)BaseGet(name); }
            set { BaseSet(name, value); }
        }
        public Foo this[int index]
        {
            get { return (Foo)BaseGet(index); }
            set { BaseSet(index, value); }
        }
        public String GetKey(int index)
        {
            return BaseGetKey(index);
        }
        public String[] GetAllKeys()
        {
            return BaseGetAllKeys();
        }
        public Object[] GetAllValues()
        {
            return BaseGetAllValues();
        }
        public Object[] GetAllValues(Type type)
        {
            return BaseGetAllValues(type);
        }
    }

    public class Foo
    {
        static private Random s_rand;
        private Guid _guid;
        private int _iValue;

        public Foo()
        {
            _guid = Guid.NewGuid();  // Guarantee uniqueness for Equals
            if (s_rand == null)
            {
                s_rand = new Random(-55);
            }
            _iValue = s_rand.Next();
        }

        public Guid guid
        {
            get { return _guid; }
        }

        public override Boolean Equals(Object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is Foo))
                return false;
            if (((Foo)obj).guid == _guid)
                return true;
            return false;
        }

        public override Int32 GetHashCode()
        {
            return _iValue;
        }

        public override String ToString()
        {
            return _guid.ToString();
        }
    }
}


