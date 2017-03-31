// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Specialized.Tests
{
    public partial class MyNameObjectCollection : NameObjectCollectionBase
    {
        public MyNameObjectCollection() : base() { }
        public MyNameObjectCollection(int capacity) : base(capacity) { }
        public MyNameObjectCollection(IEqualityComparer comparer) : base(comparer) { }
        public MyNameObjectCollection(int capacity, IEqualityComparer comparer) : base(capacity, comparer) { }
#pragma warning disable CS0618 // Type or member is obsolete
        public MyNameObjectCollection(IHashCodeProvider hashProvider, IComparer comparer) : base(hashProvider, comparer) { }
        public MyNameObjectCollection(int capacity, IHashCodeProvider hashProvider, IComparer comparer) : base(capacity, hashProvider, comparer) { }
#pragma warning restore CS0618 // Type or member is obsolete

        public new bool IsReadOnly
        {
            get { return base.IsReadOnly; }
            set { base.IsReadOnly = value; }
        }

        public bool HasKeys() => BaseHasKeys();

        public void Add(string name, Foo value) =>  BaseAdd(name, value);

        public void Remove(string name) => BaseRemove(name);

        public void RemoveAt(int index) => BaseRemoveAt(index);

        public void Clear() => BaseClear();

        public Foo this[string name]
        {
            get { return (Foo)BaseGet(name); }
            set { BaseSet(name, value); }
        }

        public Foo this[int index]
        {
            get { return (Foo)BaseGet(index); }
            set { BaseSet(index, value); }
        }

        public string GetKey(int index) => BaseGetKey(index);

        public string[] GetAllKeys() => BaseGetAllKeys();

        public object[] GetAllValues() => BaseGetAllValues();

        public object[] GetAllValues(Type type) => BaseGetAllValues(type);
    }

    public class Foo
    {
        public Foo(string stringValue)
        {
            StringValue = stringValue;
        }
        
        public string StringValue { get; }

        public override bool Equals(object obj)
        {
            if (!(obj is Foo))
                return false;
            return ((Foo)obj).StringValue == StringValue;
        }

        public override int GetHashCode() => StringValue.GetHashCode();

        public override string ToString() => StringValue;
    }
}
