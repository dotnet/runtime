// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;

namespace SerializationTypes
{
    public class MyTypeA
    {
        public MyTypeB PropX { get; set; }
        public int PropY { get; set; }

        public MyTypeB[] P_Col_Array { get; set; }
    }

    [KnownType(typeof(MyTypeC))]
    [KnownType(typeof(MyTypeD))]
    public class MyTypeB
    {
        public char PropC { get; set; }
        public MyTypeA PropA { get; set; }
    }

    public class MyTypeC : MyTypeB
    {
        public bool PropB { get; set; }
    }

    public class MyTypeD : MyTypeB
    {
        public string PropS { get; set; }
        public int PropI { get; set; }
    }

    public class TypeWithDictionaryGenericMembers
    {
        public Dictionary<string, int> F1;
        public IDictionary<string, int> F2;

        // read-write properties: strong type and interface type
        public Dictionary<string, int> P1 { get; set; }
        public IDictionary<string, int> P2 { get; set; }

        // read-only properties: strong type and interface type
        private Dictionary<bool, char> _ro1 = new Dictionary<bool, char>();
        public Dictionary<bool, char> RO1
        {
            get
            {
                return _ro1;
            }
        }

        private IDictionary<bool, char> _ro2 = new Dictionary<bool, char>();
        public IDictionary<bool, char> RO2
        {
            get
            {
                return _ro2;
            }
        }
    }

    public class MyDictionary : IDictionary
    {
        private Dictionary<object, object> _d = new Dictionary<object, object>();

        public void Add(object key, object value)
        {
            _d.Add(key, value);
        }

        public void Clear()
        {
            _d.Clear();
        }

        public bool Contains(object key)
        {
            return _d.ContainsKey(key);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return ((IDictionary)_d).GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return ((IDictionary)_d).IsFixedSize; }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary)_d).IsReadOnly; }
        }

        public ICollection Keys
        {
            get { return _d.Keys; }
        }

        public void Remove(object key)
        {
            _d.Remove(key);
        }

        public ICollection Values
        {
            get { return _d.Values; }
        }

        public object this[object key]
        {
            get
            {
                return _d[key];
            }
            set
            {
                _d[key] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)_d).CopyTo(array, index);
        }

        public int Count
        {
            get { return _d.Count; }
        }

        public bool IsSynchronized
        {
            get { return ((IDictionary)_d).IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return ((IDictionary)_d).SyncRoot; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_d).GetEnumerator();
        }
    }

    public struct ReadOnlyNonSerialziableProperty
    {
        public Uri Uri
        {
            get { return new Uri("http://www.microsoft.com"); }
        }
    }

    public class DictionaryWithVariousKeyValueTypes
    {
        public Dictionary<MyEnum, MyEnum> WithEnums;
        public Dictionary<StructNotSerializable, StructNotSerializable> WithStructs;
        public Dictionary<Nullable<short>, Nullable<bool>> WithNullables;

        public DictionaryWithVariousKeyValueTypes() { }

        public DictionaryWithVariousKeyValueTypes(bool init)
        {
            WithEnums = new Dictionary<MyEnum, MyEnum>();
            WithEnums.Add(MyEnum.Two, MyEnum.Three);
            WithEnums.Add(MyEnum.One, MyEnum.One);

            WithStructs = new Dictionary<StructNotSerializable, StructNotSerializable>();
            WithStructs.Add(new StructNotSerializable() { value = 10 }, new StructNotSerializable() { value = 12 });
            WithStructs.Add(new StructNotSerializable() { value = int.MaxValue }, new StructNotSerializable() { value = int.MinValue });

            WithNullables = new Dictionary<Nullable<short>, Nullable<bool>>();
            WithNullables.Add(short.MinValue, true);
            WithNullables.Add(0, false);
            WithNullables.Add(short.MaxValue, null);
        }
    }

    public class TypeWithDictionaryMembers
    {
        public MyDictionary F1;
        public IDictionary F2;

        // read-write properties: strong type and interface type
        public MyDictionary P1 { get; set; }
        public IDictionary P2 { get; set; }

        // read-only properties: strong type and interface type
        private MyDictionary _ro1 = new MyDictionary();
        public MyDictionary RO1
        {
            get
            {
                return _ro1;
            }
        }

        private IDictionary _ro2 = new MyDictionary();
        public IDictionary RO2
        {
            get
            {
                return _ro2;
            }
        }
    }

    public class TypeWithSimpleDictionaryMember
    {
        public Dictionary<string, int> F1;
    }

    public class TypeWithIDictionaryPropertyInitWithConcreteType
    {
        private IDictionary<string, string> _dictionaryProperty;

        public IDictionary<string, string> DictionaryProperty
        {
            get
            {
                return _dictionaryProperty;
            }
            set
            {
                _dictionaryProperty = value;
            }
        }

        public TypeWithIDictionaryPropertyInitWithConcreteType()
        {
            _dictionaryProperty = new Dictionary<string, string>();
        }
    }

    public class TypeWithListGenericMembers
    {
        public List<string> F1;
        public IList<string> F2;

        // read-write properties: strong type and interface type
        public List<int> P1 { get; set; }
        public IList<int> P2 { get; set; }

        // read-only properties: strong type and interface type
        private List<char> _ro1 = new List<char>();
        public List<char> RO1
        {
            get
            {
                return _ro1;
            }
        }

        private IList<char> _ro2 = new List<char>();
        public IList<char> RO2
        {
            get
            {
                return _ro2;
            }
        }
    }

    public class TypeWithCollectionGenericMembers
    {
        public MyCollection<string> F1;
        public ICollection<string> F2;

        // read-write properties: strong type and interface type
        public MyCollection<string> P1 { get; set; }
        public ICollection<string> P2 { get; set; }

        // read-only properties: strong type and interface type
        private MyCollection<string> _ro1 = new MyCollection<string>();
        public MyCollection<string> RO1
        {
            get
            {
                return _ro1;
            }
        }

        private ICollection<string> _ro2 = new MyCollection<string>();
        public ICollection<string> RO2
        {
            get
            {
                return _ro2;
            }
        }
    }

    public class TypeWithListMembers
    {
        public MyList F1;
        public IList F2;

        // read-write properties: strong type and interface type
        public MyList P1 { get; set; }
        public IList P2 { get; set; }

        // read-only properties: strong type and interface type
        private MyList _ro1 = new MyList();
        public MyList RO1
        {
            get
            {
                return _ro1;
            }
        }

        private IList _ro2 = new MyList();
        public IList RO2
        {
            get
            {
                return _ro2;
            }
        }
    }

    public class MyEnumerable<T> : IEnumerable<T>
    {
        private List<T> _items = new List<T>();

        public MyEnumerable()
        {
        }

        public MyEnumerable(params T[] values)
        {
            _items.AddRange(values);
        }

        public void Add(T value)
        {
            _items.Add(value);
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }

    public class TypeWithEnumerableGenericMembers
    {
        public MyEnumerable<string> F1;
        public IEnumerable<string> F2;

        // read-write properties: strong type and interface type
        public MyEnumerable<string> P1 { get; set; }
        public IEnumerable<string> P2 { get; set; }

        // read-only properties: strong type and interface type
        private MyEnumerable<string> _ro1 = new MyEnumerable<string>();
        public MyEnumerable<string> RO1
        {
            get
            {
                return _ro1;
            }
        }
    }

    public class MyCollection : ICollection
    {
        private List<object> _items = new List<object>();

        public MyCollection()
        {
        }

        public MyCollection(params object[] values)
        {
            _items.AddRange(values);
        }

        public object this[int index]
        {
            get
            {
                return _items[index];
            }
        }

        public void Add(object value)
        {
            _items.Add(value);
        }

        public void CopyTo(Array array, int index)
        {
            ((ICollection)_items).CopyTo(array, index);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsSynchronized
        {
            get { return ((ICollection)_items).IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return ((ICollection)_items).SyncRoot; }
        }

        public IEnumerator GetEnumerator()
        {
            return ((ICollection)_items).GetEnumerator();
        }
    }

    public class TypeWithCollectionMembers
    {
        public MyCollection F1;
        public ICollection F2;

        // read-write properties: strong type and interface type
        public MyCollection P1 { get; set; }
        public ICollection P2 { get; set; }

        // read-only properties: strong type and interface type
        private MyCollection _ro1 = new MyCollection();
        public MyCollection RO1
        {
            get
            {
                return _ro1;
            }
        }
    }

    public class MyEnumerable : IEnumerable
    {
        private List<object> _items = new List<object>();

        public MyEnumerable()
        {
        }

        public MyEnumerable(params object[] values)
        {
            _items.AddRange(values);
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }

        public void Add(object value)
        {
            _items.Add(value);
        }

        public object this[int index]
        {
            get
            {
                return _items[index];
            }
        }

        public int Count
        {
            get
            {
                return _items.Count;
            }
        }
    }

    public class TypeWithEnumerableMembers
    {
        public MyEnumerable F1;
        public IEnumerable F2;

        // read-write properties: strong type and interface type
        public MyEnumerable P1 { get; set; }
        public IEnumerable P2 { get; set; }

        // read-only properties: strong type and interface type
        private MyEnumerable _ro1 = new MyEnumerable();
        public MyEnumerable RO1
        {
            get
            {
                return _ro1;
            }
        }
    }

    [DataContract]
    public class DCA_1
    {
        public string P1 { get; set; }
    }

    [DataContract(Name = "abc")]
    public class DCA_2
    {
        public string P1 { get; set; }
    }

    [DataContract(Namespace = "def")]
    public class DCA_3
    {
        public string P1 { get; set; }
    }

    [DataContract(IsReference = true)]
    public class DCA_4
    {
        public string P1 { get; set; }
    }

    [DataContract(Name = "abc", Namespace = "def", IsReference = false)]
    public class DCA_5
    {
        public string P1 { get; set; }
    }

    [DataContract]
    public class DMA_1
    {
        [DataMember]
        public string P1 { get; set; }

        [DataMember(Name = "xyz")]
        public int P2 { get; set; }

        [DataMember(Order = 100)]
        public bool Order100 { get; set; }

        [DataMember(Order = 2)]
        public bool P3 { get; set; }

        [DataMember(Order = int.MaxValue)]
        public bool OrderMaxValue { get; set; }

        [DataMember(IsRequired = true)]
        public char P4 { get; set; }

        [DataMember(EmitDefaultValue = true)]
        public short P5 { get; set; }

        [DataMember]
        public MyDataContractClass04_1 MyDataMemberInAnotherNamespace { get; set; }
    }

    [DataContract(Namespace = "http://MyDataContractClass04_1.com/")]
    public class MyDataContractClass04_1
    {
        [DataMember]
        public string MyDataMember { get; set; }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(CircularLinkDerived))]
    public class CircularLink
    {
        [DataMember]
        public CircularLink Link;
        [DataMember]
        public CircularLink RandomHangingLink;

        public CircularLink() { }
        public CircularLink(bool init)
        {
            Link = new CircularLink();
            Link.Link = new CircularLink();
            Link.Link.Link = this;

            RandomHangingLink = new CircularLink();
            RandomHangingLink.Link = new CircularLink();
            RandomHangingLink.Link.Link = new CircularLinkDerived();
            RandomHangingLink.Link.Link.Link = RandomHangingLink;
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(CircularLink))]
    public class CircularLinkDerived : CircularLink
    {
        public CircularLinkDerived() { }
        public CircularLinkDerived(bool init) : base(init) { }
    }

    [DataContract]
    public class IDMA_1
    {
        [DataMember]
        public string MyDataMember { get; set; }

        [IgnoreDataMember]
        public string MyIgnoreDataMember { get; set; }

        public string MyUnsetDataMember { get; set; }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(SimpleBaseDerived))]
    public class SimpleBase
    {
        [DataMember]
        public string BaseData = string.Empty;

        public SimpleBase() { }
        public SimpleBase(bool init) { }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(SimpleBaseDerived))]
    [KnownType(typeof(SimpleBaseDerived2))]
    public class GenericBase2<T, K>
        where T : new()
        where K : new()
    {
        [DataMember]
        public T genericData1;
        [DataMember]
        public K genericData2;

        public GenericBase2() { }
        public GenericBase2(bool init)
        {
            genericData1 = new T();
            genericData2 = new K();
        }
    }

    [DataContract(IsReference = true)]
    public class SimpleBaseDerived : SimpleBase
    {
        [DataMember]
        public string DerivedData = string.Empty;

        public SimpleBaseDerived() { }
        public SimpleBaseDerived(bool init) : base(init) { }
    }

    [DataContract(IsReference = true)]
    public class SimpleBaseDerived2 : SimpleBase
    {
        [DataMember]
        public string DerivedData = string.Empty;

        public SimpleBaseDerived2() { }
        public SimpleBaseDerived2(bool init) : base(init) { }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(GenericBase<SimpleBaseContainer>))]
    [KnownType(typeof(SimpleBaseContainer))]
    public class GenericContainer
    {
        [DataMember]
        public object GenericData;

        public GenericContainer() { }
        public GenericContainer(bool init)
        {
            GenericData = new GenericBase<SimpleBaseContainer>(init);
        }
    }

    [DataContract(IsReference = true)]
    public class GenericBase<T> where T : new()
    {
        [DataMember]
        public object genericData;

        public GenericBase() { }
        public GenericBase(bool init)
        {
            genericData = new T();
        }
    }

    [DataContract(IsReference = true)]
    [KnownType(typeof(SimpleBaseDerived2))]
    public class SimpleBaseContainer
    {
        [DataMember]
        public SimpleBase Base1;
        [DataMember]
        public object Base2;

        public SimpleBaseContainer() { }
        public SimpleBaseContainer(bool init)
        {
            Base1 = new SimpleBaseDerived();
            Base2 = new SimpleBaseDerived2();
        }
    }

    public class WithDuplicateNames
    {
        public DuplicateTypeNamesTest.ns1.ClassA ClassA1 { get; set; }
        public DuplicateTypeNamesTest.ns1.StructA StructA1 { get; set; }
        public DuplicateTypeNamesTest.ns1.EnumA EnumA1 { get; set; }
        public DuplicateTypeNamesTest.ns2.ClassA ClassA2;
        public DuplicateTypeNamesTest.ns2.StructA StructA2;
        public DuplicateTypeNamesTest.ns2.EnumA EnumA2;

        public WithDuplicateNames() { }
        public WithDuplicateNames(bool init)
        {
            ClassA1 = new DuplicateTypeNamesTest.ns1.ClassA() { Name = "Hello World! \u6F22 \u00F1" };
            StructA1 = new DuplicateTypeNamesTest.ns1.StructA() { Text = "" };
            EnumA1 = DuplicateTypeNamesTest.ns1.EnumA.two;
            ClassA2 = new DuplicateTypeNamesTest.ns2.ClassA() { Nombre = "" };
            StructA2 = new DuplicateTypeNamesTest.ns2.StructA() { Texto = "" };
            EnumA2 = DuplicateTypeNamesTest.ns2.EnumA.dos;
        }
    }

    public class WithXElement
    {
        public XElement e;

        public WithXElement() { }

        public WithXElement(bool init)
        {
            e = new XElement("ElementName1");
            e.SetAttributeValue(XName.Get("Attribute1"), "AttributeValue1");
            e.SetValue("Value1");
        }
    }

    public class WithXmlElement
    {
        public XmlElement xml;

        public WithXmlElement() { }

        public WithXmlElement(bool init)
        {
            var doc = new XmlDocument();
            xml = doc.CreateElement("Element1");
        }
    }

    public class WithXElementWithNestedXElement
    {
        public XElement e1;

        public WithXElementWithNestedXElement() { }

        public WithXElementWithNestedXElement(bool init)
        {
            e1 = new XElement("ElementName1");
            e1.SetAttributeValue(XName.Get("Attribute1"), "AttributeValue1");

            XElement e2 = new XElement("ElementName2");
            e2.SetAttributeValue(XName.Get("Attribute2"), "AttributeValue2");
            e2.SetValue("Value2");

            e1.Add(e2);
        }
    }

    public class WithArrayOfXElement
    {
        public XElement[] a;

        public WithArrayOfXElement() { }

        public WithArrayOfXElement(bool init)
        {
            string ns = "http://p.com/";

            a = new XElement[]
            {
                new XElement(XName.Get("item", ns), "item0"),
                new XElement(XName.Get("item", ns), "item1"),
                new XElement(XName.Get("item", ns), "item2"),
            };
        }
    }

    public class WithListOfXElement
    {
        public List<XElement> list;

        public WithListOfXElement() { }

        public WithListOfXElement(bool init)
        {
            string ns = "http://p.com/";

            list = new List<XElement>()
            {
                new XElement(XName.Get("item", ns), "item0"),
                new XElement(XName.Get("item", ns), "item1"),
                new XElement(XName.Get("item", ns), "item2"),
            };
        }
    }

    public class BaseType
    {
        public virtual string Name1 { get; set; }

        public string Name2 { get; set; }

        public string Name3 { get; set; }

        public string Name4 { get; set; }

        public string @Name5 { get; set; }

        public virtual string Name6 { get; set; }

        public virtual string Name7 { get; set; }
    }

    public class DerivedTypeWithDifferentOverrides : BaseType
    {
        public override string Name1 { get; set; }

        public new string Name2 { get; set; }

        public new string Name3 { get; set; }

        new internal string Name4 { get; set; }

        public new string Name5 { get; set; }

        public override string Name6 { get; set; }

        public override string Name7 { set { base.Name7 = value; } }
    }

    public class DerivedTypeWithDifferentOverrides2 : DerivedTypeWithDifferentOverrides
    {
        public override string Name1 { get; set; }

        public new string Name2 { get; set; }

        public new string Name3 { get; set; }

        new internal string Name4 { get; set; }

        new internal string Name5 { get; set; }

        new internal string Name6 { get; set; }
    }

    [DataContract]
    public class MyOtherType
    {
        [DataMember]
        public string Str;
    }

    [DataContract]
    public class MyType
    {
        [IgnoreDataMember]
        public bool OnSerializingMethodInvoked;

        [IgnoreDataMember]
        public bool OnSerializedMethodInvoked;

        [IgnoreDataMember]
        public bool OnDeserializingMethodInvoked;

        [IgnoreDataMember]
        public bool OnDeserializedMethodInvoked;

        [DataMember]
        public object Value;

        [OnSerializing()]
        private void OnSerializingMethod(StreamingContext context)
        {
            OnSerializingMethodInvoked = true;
        }

        [OnSerialized()]
        private void OnSerializedMethod(StreamingContext context)
        {
            OnSerializedMethodInvoked = true;
        }

        [OnDeserializing()]
        private void OnDeserializingMethod(StreamingContext context)
        {
            OnDeserializingMethodInvoked = true;
        }

        [OnDeserialized()]
        private void OnDeserializedMethod(StreamingContext context)
        {
            OnDeserializedMethodInvoked = true;
        }
    }

    public struct EnumerableStruct : IEnumerable<string>
    {
        private List<string> _values;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator<string> IEnumerable<string>.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public void Add(string value)
        {
            if (_values == null)
            {
                _values = new List<string>();
            }

            _values.Add(value);
        }
    }

    public class EnumerableCollection : IEnumerable<DateTime>
    {
        private List<DateTime> _values = new List<DateTime>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        IEnumerator<DateTime> IEnumerable<DateTime>.GetEnumerator()
        {
            return _values.GetEnumerator();
        }

        public void Add(DateTime value)
        {
            _values.Add(value);
        }
    }

    [DataContract(IsReference = false)]
    public class SimpleDCWithRef
    {
        [DataMember]
        public SimpleDC Data;

        [DataMember]
        public SimpleDC RefData;

        public SimpleDCWithRef() { }
        public SimpleDCWithRef(bool init)
        {
            Data = new SimpleDC(true);
            RefData = Data;
        }
    }

    [DataContract]
    public class ContainsLinkedList
    {
        [DataMember]
        public LinkedList<SimpleDCWithRef> Data;

        public ContainsLinkedList() { }
        public ContainsLinkedList(bool init)
        {
            this.Data = new LinkedList<SimpleDCWithRef>();
            SimpleDCWithRef d1 = new SimpleDCWithRef(true);
            SimpleDCWithRef d2 = new SimpleDCWithRef(true);
            d2.Data.Data = d1.RefData.Data;
            Data.AddLast(d1);
            Data.AddLast(d2);
            Data.AddLast(d2);
            Data.AddLast(d1);
            SimpleDCWithRef d3 = new SimpleDCWithRef(true);
            SimpleDCWithRef d4 = new SimpleDCWithRef(true);
            d4.Data = d3.RefData;
            Data.AddLast(d4);
            Data.AddLast(d3);
            SimpleDCWithRef d5 = new SimpleDCWithRef(true);
            SimpleDCWithRef d6 = new SimpleDCWithRef(true);
            SimpleDCWithRef d7 = new SimpleDCWithRef(true);
            d6.Data = d5.Data;
            d7.Data = d5.RefData;
            d7.RefData = d6.RefData;
            Data.AddLast(d7);
        }
    }

    [CollectionDataContract(Name = "SimpleCDC", ItemName = "Item")]
    public class SimpleCDC : ICollection<string>
    {
        private List<string> _data = new List<string>();
        public SimpleCDC() { }
        public SimpleCDC(bool init)
        {
            _data.Add("One");
            _data.Add("Two");
            _data.Add("Three");
        }

        #region ICollection<string> Members

        public void Add(string item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains(string item)
        {
            return _data.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            return _data.Remove(item);
        }

        #endregion

        #region IEnumerable<string> Members

        public IEnumerator<string> GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
        #endregion
    }

    [XmlRoot(Namespace = "http://www.w3.org/2003/05/soap-envelope", ElementName = "Envelope")]
    public class TypeWithMemberWithXmlNamespaceDeclarationsAttribute
    {
        public string header;
        public string body;
        [XmlNamespaceDeclarations]
        public XmlSerializerNamespaces xmlns;
    }

    [CollectionDataContract]
    public class MyDerivedCollection : LinkedList<string>
    {
        public MyDerivedCollection() { }
    }

    [DataContract]
    public class MyDerivedCollectionContainer
    {
        public MyDerivedCollectionContainer() { Items = new MyDerivedCollection(); }

        [DataMember]
        public MyDerivedCollection Items { get; set; }
    }

    #region XmlSerializer specific

    public class LocalReadingPosition
    {
        public string Ean { get; set; }
        public DateTime LastReadTime { get; set; }
        public int PageCount { get; set; }
        public string PageNumber { get; set; }
        public string PlatformOffset { get; set; }
    }

    public struct SimpleStructWithProperties
    {
        public int Num { get; set; }
        public string Text { get; set; }
    }

    public enum UShortEnum : ushort
    {
        Option0, Option1, Option2
    }

    public class TypeWithNestedGenericClassImplementingIXmlSerialiable
    {
        // T can only be string
        public class NestedGenericClassImplementingIXmlSerialiable<T> : IXmlSerializable
        {
            public static bool WriteXmlInvoked = false;
            public static bool ReadXmlInvoked = false;

            public string StringValue { get; set; }
            private T GenericValue { get; set; }

            public NestedGenericClassImplementingIXmlSerialiable()
            {
                GenericValue = default(T);
            }

            public T GetPrivateMember()
            {
                return GenericValue;
            }

            public System.Xml.Schema.XmlSchema GetSchema()
            {
                return null;
            }

            public void ReadXml(System.Xml.XmlReader reader)
            {
                ReadXmlInvoked = true;
                reader.MoveToContent();
                StringValue = reader.GetAttribute("StringValue");
            }

            public void WriteXml(System.Xml.XmlWriter writer)
            {
                WriteXmlInvoked = true;
                writer.WriteAttributeString("StringValue", StringValue);
            }
        }
    }

    #endregion

    public class UnspecifiedRootSerializationType
    {
        public int MyIntProperty { get; set; }

        public string MyStringProperty { get; set; }
    }

    [DataContract]
    internal class InternalType
    {
        public InternalType()
        {
            PrivateProperty = 100;
        }

        [DataMember]
        internal int InternalProperty { get; set; }

        [DataMember]
        private int PrivateProperty { get; set; }

        public int GetPrivatePropertyValue()
        {
            return PrivateProperty;
        }
    }

    [DataContract]
    public class TypeWithUriTypeProperty
    {
        [DataMember]
        public Uri ConfigUri
        {
            get;
            set;
        }
    }

    [DataContract]
    public class TypeWithDateTimeOffsetTypeProperty
    {
        [DataMember]
        public DateTimeOffset ModifiedTime
        {
            get;
            set;
        }
    }

    public class TypeWithCommonTypeProperties
    {
        public Guid Id { get; set; }
        public TimeSpan Ts { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is TypeWithCommonTypeProperties)
            {
                TypeWithCommonTypeProperties other = (TypeWithCommonTypeProperties)obj;
                return (this.Id == other.Id && this.Ts == other.Ts);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode() + Ts.GetHashCode();
        }
    }

    public class BaseClassForInvalidDerivedClass
    {
        public int Id;
    }

    public class InvalidDerivedClass : BaseClassForInvalidDerivedClass
    {
        public ICollection<int> Member1;
    }

    public class AnotherInvalidDerivedClass : BaseClassForInvalidDerivedClass
    {
        public ICollection<int> Member1;
    }

    public class TypeWithTypeProperty
    {
        public int Id { get; set; }
        public Type Type { get; set; }
        public string Name { get; set; }
    }

    [DataContract(Namespace = "SerializationTypes.GenericTypeWithPrivateSetter")]
    public class GenericTypeWithPrivateSetter<T>
    {
        public GenericTypeWithPrivateSetter()
        {
        }

        public GenericTypeWithPrivateSetter(string value)
        {
            PropertyWithPrivateSetter = value;
        }

        [DataMember]
        public string PropertyWithPrivateSetter { get; private set; }
    }

    public class TypeWithExplicitIEnumerableImplementation : IEnumerable
    {
        private List<string> _innerCollection = new List<string>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _innerCollection.GetEnumerator();
        }
        public int Count { get { return _innerCollection.Count; } }
        public void Add(object item)
        {
            _innerCollection.Add((string)item);
        }
    }

    public class Level
    {
        public string Name { get; set; }
        public int LevelNo { get; set; }
    }

    [KnownType(typeof(Dictionary<int, Level>))]
    public class TypeWithGenericDictionaryAsKnownType
    {
        [DataMember]
        public Dictionary<int, Level> Foo = new Dictionary<int, Level>();
    }

    public interface IArticle
    {
        string Title { get; set; }
        string Category { get; set; }
    }

    [DataContract]
    public class ArticleBase : IArticle
    {
        public ArticleBase() : this("Untitled", "Uncategorized") { }

        public ArticleBase(string title, string category)
        {
            _title = title;
            _category = category;
        }

        private string _title;

        [DataMember]
        public string Title { get { return _title; } set { _title = value; } }

        private string _category;

        [DataMember]
        public string Category { get { return _category; } set { _category = value; } }

        public override string ToString()
        {
            return Category + " - " + Title;
        }
    }

    public class NewsArticle : ArticleBase
    {
        public NewsArticle() : base("Untitled News", "News") { }
    }

    public class SummaryArticle : ArticleBase
    {
        public SummaryArticle() : base("Untitled Summary", "Summary") { }
    }

    [DataContract]
    [KnownType(typeof(ArticleBase))]
    [KnownType(typeof(NewsArticle))]
    [KnownType(typeof(SummaryArticle))]
    public class TypeWithKnownTypeAttributeAndInterfaceMember
    {
        [DataMember]
        public IArticle HeadLine { get; set; }
    }

    [DataContract]
    [KnownType(typeof(ArticleBase))]
    [KnownType(typeof(NewsArticle))]
    [KnownType(typeof(SummaryArticle))]
    public class TypeWithKnownTypeAttributeAndListOfInterfaceMember
    {
        [DataMember]
        public List<IArticle> Articles { get; set; }
    }

    public class MyGenericList<T> : IList<T>
    {
        private List<T> _internalList = new List<T>();

        public int IndexOf(T item)
        {
            return _internalList.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            _internalList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _internalList.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return _internalList[index];
            }
            set
            {
                _internalList[index] = value;
            }
        }

        public void Add(T item)
        {
            _internalList.Add(item);
        }

        public void Clear()
        {
            _internalList.Clear();
        }

        public bool Contains(T item)
        {
            return _internalList.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _internalList.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _internalList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return _internalList.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalList.GetEnumerator();
        }
    }

    public class TypeWithListPropertiesWithoutPublicSetters
    {
        private List<string> _anotherStringList = new List<string>();

        static TypeWithListPropertiesWithoutPublicSetters()
        {
            StaticProperty = "Static property should not be checked for public setter";
        }

        public TypeWithListPropertiesWithoutPublicSetters()
        {
            PropertyWithXmlElementAttribute = new List<string>();
            IntList = new MyGenericList<int>();
            StringList = new List<string>();
            PrivateIntListField = new List<int>();
            PublicIntListField = new List<int>();
            PublicIntListFieldWithXmlElementAttribute = new List<int>();
        }

        public static string StaticProperty { get; private set; }


        [XmlElement("PropWithXmlElementAttr")]
        public List<string> PropertyWithXmlElementAttribute { get; private set; }
        public MyGenericList<int> IntList { get; private set; }
        public List<string> StringList { get; private set; }
        public List<string> AnotherStringList { get { return _anotherStringList; } }

        private List<int> PrivateIntListField;
        public List<int> PublicIntListField;
        [XmlElement("FieldWithXmlElementAttr")]
        public List<int> PublicIntListFieldWithXmlElementAttribute;
    }

    public abstract class HighScoreManager<T> where T : HighScoreManager<T>.HighScoreBase
    {
        public abstract class HighScoreBase
        {
        }
    }

    public class HighScores : HighScoreManager<HighScores.BridgeGameHighScore>
    {
        public class BridgeGameHighScore : HighScoreManager<HighScores.BridgeGameHighScore>.HighScoreBase
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }

    public class TypeWithNestedPublicType
    {
        public TypeWithNestedPublicType()
        {
            Level = 10;
        }

        public uint Level { get; private set; }
        public class LevelData
        {
            public string Name { get; set; }
        }
    }

    public class PublicTypeWithNestedPublicTypeWithNestedPublicType
    {
        public PublicTypeWithNestedPublicTypeWithNestedPublicType()
        {
            Level = 10;
        }

        public uint Level { get; private set; }

        public class NestedPublicType
        {
            public class LevelData
            {
                public string Name { get; set; }
            }
        }
    }

    internal class InternalTypeWithNestedPublicType
    {
        public InternalTypeWithNestedPublicType()
        {
            Level = 10;
        }

        public uint Level { get; private set; }
        public class LevelData
        {
            public string Name { get; set; }
        }
    }

    internal class InternalTypeWithNestedPublicTypeWithNestedPublicType
    {
        public InternalTypeWithNestedPublicTypeWithNestedPublicType()
        {
            Level = 10;
        }

        public uint Level { get; private set; }

        public class NestedPublicType
        {
            public class LevelData
            {
                public string Name { get; set; }
            }
        }
    }

    [DataContract]
    public class BaseTypeWithDataMember
    {
        [DataMember]
        public TypeAsEmbeddedDataMember EmbeddedDataMember { get; set; }
    }

    [DataContract]
    public class DerivedTypeWithDataMemberInBaseType : BaseTypeWithDataMember
    {
    }

    [DataContract]
    public class TypeAsEmbeddedDataMember
    {
        [DataMember]
        public string Name { get; set; }
    }

    public class PocoBaseTypeWithDataMember
    {
        public PocoTypeAsEmbeddedDataMember EmbeddedDataMember { get; set; }
    }

    public class PocoDerivedTypeWithDataMemberInBaseType : PocoBaseTypeWithDataMember
    {
    }

    public class PocoTypeAsEmbeddedDataMember
    {
        public string Name { get; set; }
    }

    public class SpotlightDescription
    {
        public SpotlightDescription() { }
        public bool IsDynamic { get; set; }
        public string Value { get; set; }
    }

    public enum SlideEventType
    {
        None,
        LaunchURL,
        LaunchSection,
        LaunchVideo,
        LaunchImage
    }

    public class SerializableSlide
    {
        public SerializableSlide() { }
        public SpotlightDescription Description { get; set; }
        public string DisplayCondition { get; set; }
        public string EventData { get; set; }
        public SlideEventType EventType { get; set; }
        public string ImageName { get; set; }
        public string ImagePath { get; set; }
        public string Title { get; set; }
    }

    public class GenericTypeWithNestedGenerics<T>
    {
        public class InnerGeneric<U>
        {
            public T data1;
            public U data2;
        }
    }

    public class TypeWithNoDefaultCtor
    {
        public TypeWithNoDefaultCtor(string id)
        {
            ID = id;
        }
        public string ID { get; set; }
    }

    public class TypeWithPropertyWithoutDefaultCtor
    {
        public TypeWithPropertyWithoutDefaultCtor()
        {
        }

        public string Name { get; set; }
        public TypeWithNoDefaultCtor MemberWithInvalidDataContract { get; set; }
    }


    [DataContract(Name = "DCWith.InName")]
    public class DataContractWithDotInName
    {
        [DataMember]
        public string Name { get; set; }
    }

    [DataContract(Name = "DCWith-InName")]
    public class DataContractWithMinusSignInName
    {
        [DataMember]
        public string Name { get; set; }
    }

    [DataContract(Name = "DCWith{}[]().,:;+-*/%&|^!~=<>?++--&&||<<>>==!=<=>=+=-=*=/=%=&=|=^=<<=>>=->InName")]
    public class DataContractWithOperatorsInName
    {
        [DataMember]
        public string Name { get; set; }
    }

    [DataContract(Name = "DCWith`@#$'\" 	InName")]
    public class DataContractWithOtherSymbolsInName
    {
        [DataMember]
        public string Name { get; set; }
    }

    [CollectionDataContract(Name = "MyHeaders", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect", ItemName = "MyHeader", KeyName = "MyKey", ValueName = "MyValue")]
    public sealed class CollectionDataContractWithCustomKeyName : Dictionary<int, int>
    {
        public CollectionDataContractWithCustomKeyName()
        {
        }
    }

    // Dictionary<int, int> is already used above so there will be a conflict.
    [CollectionDataContract(Name = "MyHeaders2", Namespace = "http://schemas.microsoft.com/netservices/2010/10/servicebus/connect", ItemName = "MyHeader2", KeyName = "MyKey2", ValueName = "MyValue2")]
    public sealed class CollectionDataContractWithCustomKeyNameDuplicate : Dictionary<int, int>
    {
        public CollectionDataContractWithCustomKeyNameDuplicate()
        {
        }
    }

    public class CollectionWithoutDefaultConstructor : MyCollection<string>
    {
        internal CollectionWithoutDefaultConstructor(string name) : base()
        {
            Name = name;
        }

        public string Name { get; set; }
    }

    public class TypeWithCollectionWithoutDefaultConstructor
    {
        public TypeWithCollectionWithoutDefaultConstructor()
        {
            _collectionWithoutDefaultConstructor = new CollectionWithoutDefaultConstructor("MyName");
        }

        CollectionWithoutDefaultConstructor _collectionWithoutDefaultConstructor;
        public CollectionWithoutDefaultConstructor CollectionProperty { get { return _collectionWithoutDefaultConstructor; } }
    }

    public class TypeMissingSerializationCodeBase
    {
    }

    public class TypeMissingSerializationCodeDerived : TypeMissingSerializationCodeBase
    {
        public string Name { get; set; }
    }

    public class SimpleTypeWihtMoreProperties
    {
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }
        public MyEnum EnumProperty { get; set; }
        public List<string> CollectionProperty { get; set; }
        public List<SimpleTypeWihtMoreProperties> SimpleTypeList { get; set; }
    }

    public class TypeWith2DArrayProperty1
    {
        [System.Xml.Serialization.XmlArrayItemAttribute("SimpleType", typeof(SimpleType), NestingLevel = 1, IsNullable = false)]
        public SimpleType[][] TwoDArrayOfSimpleType;
    }

    public class SimpleTypeWithMoreFields
    {
        public string StringField;
        public int IntField;
        public MyEnum EnumField;
        public List<string> CollectionField;
        public List<SimpleTypeWithMoreFields> SimpleTypeList;
    }

    // New types
    public class TypeWithPrimitiveProperties
    {
        public string P1 { get; set; }
        public int P2 { get; set; }
        public override bool Equals(object obj)
        {
            TypeWithPrimitiveProperties other = obj as TypeWithPrimitiveProperties;
            if (other == this)
            {
                return true;
            }
            if (other == null)
            {
                return false;
            }
            return this.P1 == other.P1 && this.P2 == other.P2;
        }
        public override int GetHashCode()
        {
            return P1.GetHashCode() ^ P2.GetHashCode();
        }
    }

    public class TypeWithPrimitiveFields
    {
        public string P1;
        public int P2;
    }

    public class TypeWithAllPrimitiveProperties
    {
        public bool BooleanMember { get; set; }
        //public byte[] ByteArrayMember { get; set; }
        public char CharMember { get; set; }
        public DateTime DateTimeMember { get; set; }
        public decimal DecimalMember { get; set; }
        public double DoubleMember { get; set; }
        public float FloatMember { get; set; }
        public Guid GuidMember { get; set; }
        //public byte[] HexBinaryMember { get; set; }
        public string StringMember { get; set; }
        public int IntMember { get; set; }
    }

    public class TypeImplementsGenericICollection<T> : ICollection<T>
    {
        private List<T> _items = new List<T>();

        public TypeImplementsGenericICollection()
        {
        }

        public TypeImplementsGenericICollection(params T[] values)
        {
            _items.AddRange(values);
        }

        public void Add(T item)
        {
            _items.Add(item);
        }

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsReadOnly
        {
            get { return ((ICollection<T>)_items).IsReadOnly; }
        }

        public bool Remove(T item)
        {
            return _items.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((ICollection<T>)_items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }

    public class MyNonGenericDictionary : IDictionary
    {
        private Dictionary<object, object> _d = new Dictionary<object, object>();

        public void Add(object key, object value)
        {
            _d.Add(key, value);
        }

        public void Clear()
        {
            _d.Clear();
        }

        public bool Contains(object key)
        {
            return _d.ContainsKey(key);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return ((IDictionary)_d).GetEnumerator();
        }

        public bool IsFixedSize
        {
            get { return ((IDictionary)_d).IsFixedSize; }
        }

        public bool IsReadOnly
        {
            get { return ((IDictionary)_d).IsReadOnly; }
        }

        public ICollection Keys
        {
            get { return _d.Keys; }
        }

        public void Remove(object key)
        {
            _d.Remove(key);
        }

        public ICollection Values
        {
            get { return _d.Values; }
        }

        public object this[object key]
        {
            get
            {
                return _d[key];
            }
            set
            {
                _d[key] = value;
            }
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary)_d).CopyTo(array, index);
        }

        public int Count
        {
            get { return _d.Count; }
        }

        public bool IsSynchronized
        {
            get { return ((IDictionary)_d).IsSynchronized; }
        }

        public object SyncRoot
        {
            get { return ((IDictionary)_d).SyncRoot; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_d).GetEnumerator();
        }
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithStringArrayAsXmlAttribute
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified)]
        public string[] XmlAttributeForms;
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithArrayLikeXmlAttributeWithFields
    {
        public string StringField;

        [XmlAttribute(Form = XmlSchemaForm.Qualified)]
        public string[] XmlAttributeForms;

        public int IntField;
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithQNameArrayAsXmlAttributeInvalidDefaultValue
    {
        [DefaultValue("DefaultValue")]
        public XmlQualifiedName XmlAttributeForms;
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithQNameArrayAsXmlAttribute
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified)]
        public XmlQualifiedName[] XmlAttributeForms;
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithEnumArrayAsXmlAttribute
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified)]
        public IntEnum[] XmlAttributeForms;
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithByteArrayAsXmlAttribute
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified)]
        public byte[] XmlAttributeForms;
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithNullableByteArray
    {
        [XmlElement(DataType = "base64Binary", IsNullable = true)]
        public byte[] XmlAttributeForms { get; set; }
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithByteArrayArrayAsXmlAttribute
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified)]
        public byte[][] XmlAttributeForms;
    }

    [XmlType(TypeName = "MyXmlType")]
    [XmlRoot]
    public class TypeWithXmlElementsAndUnnamedXmlAny
    {
        [XmlElement(Type = typeof(string))]
        [XmlElement(Type = typeof(int))]
        [XmlAnyElement]
        public object[] Things;
    }

    [XmlType(TypeName = "MyXmlType")]
    [XmlRoot]
    public class TypeWithMultiNamedXmlAnyElement
    {
        [XmlAnyElement(Name = "name1", Namespace = "ns1")]
        [XmlAnyElement(Name = "name2", Namespace = "ns2")]
        public object[] Things;
    }

    [XmlType(TypeName = "MyXmlType")]
    [XmlRoot]
    public class TypeWithMultiNamedXmlAnyElementAndOtherFields
    {
        [XmlAnyElement(Name = "name1", Namespace = "ns1")]
        [XmlAnyElement(Name = "name2", Namespace = "ns2")]
        public object[] Things;

        public string StringField;

        public int IntField;
    }

    public class TypeWithPropertyHavingChoice
    {
        // The ManyChoices field can contain an array
        // of choices. Each choice must be matched to
        // an array item in the ChoiceArray field.
        [XmlChoiceIdentifier("ChoiceArray")]
        [XmlElement("Item", typeof(string))]
        [XmlElement("Amount", typeof(int))]
        public object[] ManyChoices { get; set; }

        // TheChoiceArray field contains the enumeration
        // values, one for each item in the ManyChoices array.
        [XmlIgnore]
        public MoreChoices[] ChoiceArray;
    }

    internal class MyFileStreamSurrogateProvider : ISerializationSurrogateProvider
    {
        static MyFileStreamSurrogateProvider()
        {
            Singleton = new MyFileStreamSurrogateProvider();
        }

        internal static MyFileStreamSurrogateProvider Singleton { get; private set; }

        public Type GetSurrogateType(Type type)
        {
            if (type == typeof(MyFileStream))
            {
                return typeof(MyFileStreamReference);
            }

            return type;
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            if (obj == null)
            {
                return null;
            }
            MyFileStream myFileStream = obj as MyFileStream;
            if (null != myFileStream)
            {
                if (targetType != typeof(MyFileStreamReference))
                {
                    throw new ArgumentException("Target type for serialization must be MyFileStream");
                }
                return MyFileStreamReference.Create(myFileStream);
            }

            return obj;
        }

        public object GetDeserializedObject(object obj, Type targetType)
        {
            if (obj == null)
            {
                return null;
            }
            MyFileStreamReference myFileStreamRef = obj as MyFileStreamReference;
            if (null != myFileStreamRef)
            {
                if (targetType != typeof(MyFileStream))
                {
                    throw new ArgumentException("Target type for deserialization must be MyFileStream");
                }
                return myFileStreamRef.ToMyFileStream();
            }
            return obj;
        }
    }

    public class MyPersonSurrogateProvider : ISerializationSurrogateProvider
    {
        public Type GetSurrogateType(Type type)
        {
            if (type == typeof(NonSerializablePerson))
            {
                return typeof(NonSerializablePersonSurrogate);
            }
            else if (type == typeof(NonSerializablePersonForStress))
            {
                return typeof(NonSerializablePersonForStressSurrogate);
            }
            else
            {
                return type;
            }
        }

        public object GetDeserializedObject(object obj, Type targetType)
        {
            if (obj is NonSerializablePersonSurrogate)
            {
                NonSerializablePersonSurrogate person = (NonSerializablePersonSurrogate)obj;
                return new NonSerializablePerson(person.Name, person.Age);
            }
            else if (obj is NonSerializablePersonForStressSurrogate)
            {
                NonSerializablePersonForStressSurrogate person = (NonSerializablePersonForStressSurrogate)obj;
                return new NonSerializablePersonForStress(person.Name, person.Age);
            }

            return obj;
        }

        public object GetObjectToSerialize(object obj, Type targetType)
        {
            if (obj is NonSerializablePerson)
            {
                NonSerializablePerson nsp = (NonSerializablePerson)obj;
                NonSerializablePersonSurrogate serializablePerson = new NonSerializablePersonSurrogate
                {
                    Name = nsp.Name,
                    Age = nsp.Age,
                };

                return serializablePerson;
            }
            else if (obj is NonSerializablePersonForStress)
            {
                NonSerializablePersonForStress nsp = (NonSerializablePersonForStress)obj;
                NonSerializablePersonForStressSurrogate serializablePerson = new NonSerializablePersonForStressSurrogate
                {
                    Name = nsp.Name,
                    Age = nsp.Age,
                };

                return serializablePerson;
            }

            return obj;
        }
    }
}

namespace DuplicateTypeNamesTest.ns1
{
    public class ClassA
    {
        public string Name;
    }

    public struct StructA
    {
        public string Text;
    }

    public enum EnumA
    {
        one, two, three,
    }
}

namespace DuplicateTypeNamesTest.ns2
{
    public class ClassA
    {
        public string Nombre;
    }

    public struct StructA
    {
        public string Texto;
    }

    public enum EnumA
    {
        uno, dos, tres,
    }
}

public class TypeWithPrivateFieldAndPrivateGetPublicSetProperty
{
    private string _name;

    public string Name
    {
        private get
        {
            return _name;
        }
        set
        {
            _name = value;
        }
    }

    public string GetName()
    {
        return _name;
    }
}

public class TypeWithoutPublicSetter
{
    public string Name { get; private set; }

    [XmlIgnore]
    public int Age { get; private set; }

    public Type MyType { get; private set; }

    public string ValidProperty { get; set; }

    public string PropertyWrapper
    {
        get
        {
            return ValidProperty;
        }
    }
}

[CompilerGenerated]
public class TypeWithCompilerGeneratedAttributeButWithoutPublicSetter
{
    [CompilerGenerated]
    public string Name { get; private set; }
}

public class TestableDerivedException : System.Exception
{
    public TestableDerivedException()
        : base()
    { }

    public TestableDerivedException(string message)
        : base(message)
    { }

    public TestableDerivedException(string message, Exception innerException)
        : base(message, innerException)
    { }

    public string TestProperty { get; set; }
}

namespace DirectRef
{
    public class TypeWithIndirectRef
    {
        public static implicit operator Task<object>(TypeWithIndirectRef v)
        {
            throw new NotImplementedException();
        }

        public string Name { get; set; }
    }
}

public class NookAppLocalState
{
    public int ArticleViewCount { get; set; }
    public string CurrentlyReadingProductEAN { get; set; }
    public PaymentType CurrentPaymentType { get; set; }
    public bool IsFirstRun { get; set; }
    public List<LocalReadingPosition> LocalReadingPositionState { get; set; }
    public List<string> PreviousSearchQueries { get; set; }
    public System.Drawing.Color TextColor;

    [XmlIgnore]
    public int IgnoreProperty;

    public bool IsFirstRunDuplicate { get; set; }
    // Nested Types
    public enum PaymentType
    {
        Unconfigured,
        Nook,
        Microsoft
    }
}

public class LocalReadingPosition
{
    public string Ean { get; set; }
    public DateTime LastReadTime { get; set; }
    public int PageCount { get; set; }
    public string PageNumber { get; set; }
    public string PlatformOffset { get; set; }
}

public class TypeWithNonParameterlessConstructor
{
    public string StringProperty { get; set; }

    public TypeWithNonParameterlessConstructor(string value)
    {
        StringProperty = value;
    }
}

[DataContract]
public class AppEnvironment
{
    [DataMember(Name = "screen:orientation")]
    public string ScreenOrientation { get; set; }

    [DataMember(Name = "screen_dpi(x:y)")]
    public int ScreenDpi { get; set; }
}

#region Types for data contract surrogate tests

public class NonSerializablePerson
{
    public string Name { get; private set; }
    public int Age { get; private set; }

    public NonSerializablePerson(string name, int age)
    {
        this.Name = name;
        this.Age = age;
    }

    public override string ToString()
    {
        return string.Format("Person[Name={0},Age={1}]", this.Name, this.Age);
    }
}

public class NonSerializablePersonForStress
{
    public string Name { get; private set; }
    public int Age { get; private set; }

    public NonSerializablePersonForStress(string name, int age)
    {
        this.Name = name;
        this.Age = age;
    }

    public override string ToString()
    {
        return string.Format("Person[Name={0},Age={1}]", this.Name, this.Age);
    }
}

public class Family
{
    public NonSerializablePerson[] Members;

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Family members:");
        foreach (var member in this.Members)
        {
            sb.AppendLine($"  {member}");
        }

        return sb.ToString();
    }
}

public class FamilyForStress
{
    public NonSerializablePersonForStress[] Members;

    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Family members:");
        foreach (var member in this.Members)
        {
            sb.AppendLine($"  {member}");
        }

        return sb.ToString();
    }
}

[DataContract]
public class NonSerializablePersonSurrogate
{
    [DataMember(Name = "PersonName")]
    public string Name { get; set; }
    [DataMember(Name = "PersonAge")]
    public int Age { get; set; }
}

// Note that DataContractAttribute.IsReference is set to true.
[DataContract(IsReference = true)]
public class NonSerializablePersonForStressSurrogate
{
    [DataMember(Name = "PersonName")]
    public string Name { get; set; }
    [DataMember(Name = "PersonAge")]
    public int Age { get; set; }
}

[DataContract]
class MyFileStream : IDisposable
{
    private FileStream stream;

    internal string Name
    {
        get
        {
            return this.stream.Name;
        }
    }

    internal MyFileStream(string fileName)
    {
        this.stream = new FileStream(
                            fileName,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,
                            FileShare.ReadWrite);
    }

    internal void WriteLine(string line)
    {
        using (StreamWriter writer = new StreamWriter(this.stream))
        {
            writer.WriteLine(line);
        }
    }

    internal string ReadLine()
    {
        using (StreamReader reader = new StreamReader(this.stream))
        {
            return reader.ReadLine();
        }
    }

    public void Dispose()
    {
        this.stream.Dispose();
    }
}

[DataContract]
class MyFileStreamReference
{
    [DataMember]
    private string fileStreamName;

    private MyFileStreamReference(string fileStreamName)
    {
        this.fileStreamName = fileStreamName;
    }

    internal static MyFileStreamReference Create(MyFileStream myFileStream)
    {
        return new MyFileStreamReference(myFileStream.Name);
    }

    internal MyFileStream ToMyFileStream()
    {
        return new MyFileStream(fileStreamName);
    }
}

#endregion

[DataContract(Name = "TypeWithIntAndStringProperty", Namespace = "")]
public class TypeWithIntAndStringProperty
{
    [DataMember]
    public int SampleInt { get; set; }

    [DataMember]
    public string SampleString { get; set; }
}

[DataContract(Name = "TypeWithTypeWithIntAndStringPropertyProperty", Namespace = "")]
public class TypeWithTypeWithIntAndStringPropertyProperty
{
    [DataMember]
    public TypeWithIntAndStringProperty ObjectProperty { get; set; }
}

[DataContract]
public class TypeWithCollectionInterfaceGetOnlyCollection
{
    List<string> items;

    [DataMember]
    public ICollection<string> Items
    {
        get
        {
            if (items == null)
            {
                items = new List<string>();
            }
            return this.items;
        }
    }

    public TypeWithCollectionInterfaceGetOnlyCollection() { }

    public TypeWithCollectionInterfaceGetOnlyCollection(List<string> items)
    {
        this.items = items;
    }
}

[DataContract]
public class TypeWithEnumerableInterfaceGetOnlyCollection
{
    List<string> items;

    [DataMember]
    public IEnumerable<string> Items
    {
        get
        {
            if (items == null)
            {
                items = new List<string>();
            }
            return this.items;
        }
    }

    public TypeWithEnumerableInterfaceGetOnlyCollection() { }

    public TypeWithEnumerableInterfaceGetOnlyCollection(List<string> items)
    {
        this.items = items;
    }
}

[CollectionDataContract]
public class RecursiveCollection : List<RecursiveCollection2>
{

}

[CollectionDataContract]
public class RecursiveCollection2 : List<RecursiveCollection3>
{

}

[CollectionDataContract]
public class RecursiveCollection3 : List<RecursiveCollection>
{

}

[DataContract]
public class TypeWithListOfReferenceChildren
{
    [DataMember]
    public List<TypeOfReferenceChild> Children { get; set; }
}

[DataContract(IsReference = true)]
public class TypeOfReferenceChild
{
    [DataMember]
    public TypeWithListOfReferenceChildren Root { get; set; }
    [DataMember]
    public string Name { get; set; }
}

[DataContract]
public sealed class TypeWithInternalDefaultConstructor
{
    internal TypeWithInternalDefaultConstructor()
    {
    }

    internal static TypeWithInternalDefaultConstructor CreateInstance()
    {
        return new TypeWithInternalDefaultConstructor();
    }

    [DataMember]
    public string Name { get; set; }
}

public sealed class TypeWithInternalDefaultConstructorWithoutDataContractAttribute
{
    internal TypeWithInternalDefaultConstructorWithoutDataContractAttribute()
    {
    }

    internal static TypeWithInternalDefaultConstructorWithoutDataContractAttribute CreateInstance()
    {
        return new TypeWithInternalDefaultConstructorWithoutDataContractAttribute();
    }

    [DataMember]
    public string Name { get; set; }
}

[DataContract]
public class TypeWithEmitDefaultValueFalse
{
    [DataMember(EmitDefaultValue = false)]
    public string Name = null;
    [DataMember(EmitDefaultValue = false)]
    public int ID = 0;
}

[DataContract(Namespace = "ItemTypeNamespace")]
public class TypeWithNonDefaultNamcespace
{
    [DataMember]
    public string Name;
}

[CollectionDataContract(Namespace = "CollectionNamespace")]
public class CollectionOfTypeWithNonDefaultNamcespace : List<TypeWithNonDefaultNamcespace>
{

}

#region Type for Xml_ConstructorWithXmlAttributeOverrides

namespace Music
{
    public class Orchestra
    {
        public Instrument[] Instruments;
    }

    public class Instrument
    {
        public string Name;
    }

    public class Brass : Instrument
    {
        public bool IsValved;
    }
}

#endregion
[DataContract]
public class ObjectContainer
{
    [DataMember]
    private object _data;

    public ObjectContainer(object input)
    {
        _data = input;
    }

    public object Data
    {
        get { return _data; }
    }
}

[DataContract]
public class DTOContainer
{
    [DataMember]
    public object nDTO = DateTimeOffset.MaxValue;
}

[DataContract]
public class DTOResolver : DataContractResolver
{
    public override bool TryResolveType(Type dcType, Type declaredType, DataContractResolver knownTypeResolver, out XmlDictionaryString typeName, out XmlDictionaryString typeNamespace)
    {
        string resolvedTypeName = string.Empty;
        string resolvedNamespace = string.Empty;
        resolvedNamespace = "http://www.default.com";
        switch (dcType.Name)
        {
            case "ObjectContainer":
            case "DTOContainer":
                {
                    resolvedTypeName = dcType.Name;
                }
                break;
            case "DateTimeOffset":
                {
                    resolvedTypeName = "DTO";
                }
                break;
            default:
                {
                    return knownTypeResolver.TryResolveType(dcType, declaredType, null, out typeName, out typeNamespace);
                }
        }
        XmlDictionary dic = new XmlDictionary();
        typeName = dic.Add(resolvedTypeName);
        typeNamespace = dic.Add(resolvedNamespace);
        return true;
    }

    public override Type ResolveName(string typeName, string typeNamespace, Type declaredType, DataContractResolver knownTypeResolver)
    {
        switch (typeNamespace)
        {
            case "http://www.default.com":
                {
                    switch (typeName)
                    {
                        case "ObjectContainer":
                            {
                                return typeof(ObjectContainer);
                            }
                        case "DTOContainer":
                            {
                                return typeof(DTOContainer);
                            }
                        case "DTO":
                            {
                                return typeof(DateTimeOffset);
                            }
                        default: break;
                    }
                }
                break;
        }
        Type result = knownTypeResolver.ResolveName(typeName, typeNamespace, declaredType, null);
        return result;
    }
}

public class Person1
{
    public string Name;
    public int Age;
}

[DataContract(Name = "Person", Namespace = "http://www.msn.com/employees")]
class Person : IExtensibleDataObject
{
    private ExtensionDataObject extensionDataObject_value;
    public ExtensionDataObject ExtensionData
    {
        get
        {
            return extensionDataObject_value;
        }
        set
        {
            extensionDataObject_value = value;
        }
    }
    [DataMember]
    public string Name=string.Empty;
}

[DataContract(Name = "Person", Namespace = "http://www.msn.com/employees")]
class PersonV2 : IExtensibleDataObject
{
    // Best practice: add an Order number to new members.
    [DataMember(Order = 2)]
    public int ID = 0;

    [DataMember]
    public string Name = string.Empty;

    private ExtensionDataObject extensionDataObject_value;
    public ExtensionDataObject ExtensionData
    {
        get
        {
            return extensionDataObject_value;
        }
        set
        {
            extensionDataObject_value = value;
        }
    }
}

[DataContract]
public class Name
{
    [DataMember]
    public string firstName;
    public string middlename;
    [DataMember]
    public string lastName;
}

[DataContract(Namespace = "http://msn.com")]
public class Order
{
    private string productValue;
    private int quantityValue;
    private decimal valueValue;

    [DataMember(Name = "cost")]
    public decimal Value
    {
        get { return valueValue; }
        set { valueValue = value; }
    }

    [DataMember(Name = "quantity")]
    public int Quantity
    {
        get { return quantityValue; }
        set { quantityValue = value; }
    }

    [DataMember(Name = "productName")]
    public string Product
    {
        get { return productValue; }
        set { productValue = value; }
    }
}

[DataContract(Namespace = "http://www.msn.com/")]
public class Line
{
    private Order[] itemsValue;

    [DataMember]
    public Order[] Items
    {
        get { return itemsValue; }
        set { itemsValue = value; }
    }
}

public class MyReader : XmlSerializationReader
{
    protected override void InitCallbacks() { }
    protected override void InitIDs() { }

    public static byte[] HexToBytes(string value)
    {
        return ToByteArrayHex(value);
    }
}

public class MyWriter : XmlSerializationWriter
{
    protected override void InitCallbacks() { }

    public static string BytesToHex(byte[] by)
    {
        return FromByteArrayHex(by);
    }
}

class MyStreamProvider : IStreamProvider
{
    Stream stream;
    bool streamReleased;
    public MyStreamProvider(Stream stream)
    {
        this.stream = stream;
        this.streamReleased = false;
    }
    public bool StreamReleased
    {
        get { return this.streamReleased; }
    }
    public Stream GetStream()
    {
        return this.stream;
    }
    public void ReleaseStream(Stream stream)
    {
        this.streamReleased = true;
    }
}

public class ReaderWriterFactory
{
    public enum ReaderWriterType
    {
        Binary,
        Text,
        MTOM,
        WebData,
        WrappedWebData
    };

    public enum TransferMode
    {
        Buffered,
        Streamed
    };

    public enum ReaderMode
    {
        Buffered,
        Streamed
    };

    public static ReaderWriterType Binary = ReaderWriterType.Binary;
    public static ReaderWriterType Text = ReaderWriterType.Text;
    public static ReaderWriterType MTOM = ReaderWriterType.MTOM;
    public static ReaderWriterType WebData = ReaderWriterType.WebData;
    public static ReaderWriterType WrappedWebData = ReaderWriterType.WrappedWebData;

    public static XmlReader CreateXmlReader(ReaderWriterType rwType, byte[] buffer, Encoding encoding, XmlDictionaryReaderQuotas quotas, IXmlDictionary dictionary, OnXmlDictionaryReaderClose onClose)
    {
        XmlReader result = null;
        switch (rwType)
        {
            case ReaderWriterType.Binary:
                result = XmlDictionaryReader.CreateBinaryReader(buffer, 0, buffer.Length, dictionary, quotas, null, onClose);
                break;
            case ReaderWriterType.Text:
                result = XmlDictionaryReader.CreateTextReader(buffer, 0, buffer.Length, encoding, quotas, onClose);
                break;
            case ReaderWriterType.WebData:
                if (quotas != XmlDictionaryReaderQuotas.Max)
                {
                    throw new Exception("Cannot enforce quotas on the Webdata readers!");
                }
                if (onClose != null)
                {
                    throw new Exception("Webdata readers do not support the OnClose callback!");
                }
                XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.Default, encoding);
                result = XmlReader.Create(new MemoryStream(buffer), new XmlReaderSettings(), context);
                break;
            case ReaderWriterType.MTOM:
                result = XmlDictionaryReader.CreateMtomReader(buffer, 0, buffer.Length, new Encoding[] { encoding }, null, quotas, int.MaxValue, onClose);
                break;
            case ReaderWriterType.WrappedWebData:
                if (quotas != XmlDictionaryReaderQuotas.Max)
                {
                    throw new Exception("There is no overload to create the webdata readers with quotas!");
                }
                if (onClose != null)
                {
                    throw new Exception("Webdata readers do not support the OnClose callback!");
                }
                XmlParserContext context2 = new XmlParserContext(null, null, null, XmlSpace.Default, encoding);
                result = XmlReader.Create(new MemoryStream(buffer), new XmlReaderSettings(), context2);
                result = XmlDictionaryReader.CreateDictionaryReader(result);
                break;
            default:
                throw new ArgumentOutOfRangeException("rwType");
        }
        return result;
    }

    public static XmlReader CreateXmlReader(ReaderWriterType rwType, Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, IXmlDictionary dictionary, OnXmlDictionaryReaderClose onClose)
    {
        XmlReader result = null;
        switch (rwType)
        {
            case ReaderWriterType.Binary:
                result = XmlDictionaryReader.CreateBinaryReader(stream, dictionary, quotas, null, onClose);
                break;
            case ReaderWriterType.Text:
                result = XmlDictionaryReader.CreateTextReader(stream, encoding, quotas, onClose);
                break;
            case ReaderWriterType.MTOM:
                result = XmlDictionaryReader.CreateMtomReader(stream, new Encoding[] { encoding }, null, quotas, int.MaxValue, onClose);
                break;
            case ReaderWriterType.WebData:
            case ReaderWriterType.WrappedWebData:
                if (quotas != XmlDictionaryReaderQuotas.Max)
                {
                    throw new Exception("Webdata readers do not support quotas!");
                }
                if (onClose != null)
                {
                    throw new Exception("Webdata readers do not support the OnClose callback!");
                }
                XmlParserContext context = new XmlParserContext(null, null, null, XmlSpace.Default, encoding);
                result = XmlReader.Create(stream, new XmlReaderSettings(), context);
                if (rwType == ReaderWriterType.WrappedWebData)
                {
                    result = XmlDictionaryReader.CreateDictionaryReader(result);
                }
                break;
            default:
                throw new ArgumentOutOfRangeException("rwType");
        }
        return result;
    }

    public static XmlReader CreateXmlReader(ReaderWriterType rwType, byte[] buffer, Encoding encoding, XmlDictionaryReaderQuotas quotas, IXmlDictionary dictionary)
    {
        return CreateXmlReader(rwType, buffer, encoding, quotas, dictionary, null);
    }

    public static XmlReader CreateXmlReader(ReaderWriterType rwType, Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas, IXmlDictionary dictionary)
    {
        return CreateXmlReader(rwType, stream, encoding, quotas, dictionary, null);
    }

    public static XmlReader CreateXmlReader(ReaderWriterType rwType, byte[] buffer, Encoding encoding, XmlDictionaryReaderQuotas quotas)
    {
        return CreateXmlReader(rwType, buffer, encoding, quotas, null);
    }

    public static XmlReader CreateXmlReader(ReaderWriterType rwType, Stream stream, Encoding encoding, XmlDictionaryReaderQuotas quotas)
    {
        return CreateXmlReader(rwType, stream, encoding, quotas, null);
    }

    public static XmlReader CreateXmlReader(ReaderWriterType rwType, byte[] buffer, Encoding encoding)
    {
        return CreateXmlReader(rwType, buffer, encoding, XmlDictionaryReaderQuotas.Max);
    }

    public static XmlReader CreateXmlReader(ReaderWriterType rwType, Stream stream, Encoding encoding)
    {
        return CreateXmlReader(rwType, stream, encoding, XmlDictionaryReaderQuotas.Max);
    }

    public static XmlWriter CreateXmlWriter(ReaderWriterType rwType, Stream stream, Encoding encoding)
    {
        return CreateXmlWriter(rwType, stream, encoding, null);
    }

    public static XmlWriter CreateXmlWriter(ReaderWriterType rwType, Stream stream, Encoding encoding, IXmlDictionary dictionary)
    {
        XmlWriter result = null;
        switch (rwType)
        {
            case ReaderWriterType.Binary:
                result = XmlDictionaryWriter.CreateBinaryWriter(stream, dictionary);
                break;
            case ReaderWriterType.Text:
                result = XmlDictionaryWriter.CreateTextWriter(stream, encoding);
                break;
            case ReaderWriterType.MTOM:
                result = XmlDictionaryWriter.CreateMtomWriter(stream, encoding, int.MaxValue, "myStartInfo", null, null, true, false);
                break;
            case ReaderWriterType.WebData:
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Encoding = encoding;
                result = XmlWriter.Create(stream, settings);
                break;
            case ReaderWriterType.WrappedWebData:
                XmlWriterSettings settings2 = new XmlWriterSettings();
                settings2.Encoding = encoding;
                result = XmlWriter.Create(stream, settings2);
                result = XmlDictionaryWriter.CreateDictionaryWriter(result);
                break;
            default:
                throw new ArgumentOutOfRangeException("rwType");
        }
        return result;
    }

}

[DataContract]
public class TestData
{
    [DataMember]
    public string TestString;
}

[Serializable]
public class MyISerializableType : ISerializable
{
    public MyISerializableType()
    {
    }

    private string _stringValue;

    public string StringValue
    {
        get { return _stringValue; }
        set { _stringValue = value; }
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(_stringValue), _stringValue, typeof(string));

    }

    public MyISerializableType(SerializationInfo info, StreamingContext context)
    {
        _stringValue = (string)info.GetValue(nameof(_stringValue), typeof(string));
    }
}

[DataContract]
public class TypeForRootNameTest
{
    [DataMember]
    public string StringProperty { get; set; }
}

[Serializable]
public class TypeWithSerializableAttributeAndNonSerializedField
{
    public int Member1;
    private string _member2;
    private int _member3;

    [NonSerialized()]
    public string Member4;

    public string Member2
    {
        get
        {
            return _member2;
        }

        set
        {
            _member2 = value;
        }
    }

    public int Member3
    {
        get
        {
            return _member3;
        }
    }

    public void SetMember3(int value)
    {
        _member3 = value;
    }
}

[Serializable]
public class TypeWithOptionalField
{
    public int Member1;
    [OptionalField]
    public int Member2;
}

[Serializable]
public enum SerializableEnumWithNonSerializedValue
{
    One = 1,
    [NonSerialized]
    Two = 2,
}

public class TypeWithSerializableEnum
{
    public SerializableEnumWithNonSerializedValue EnumField;
}

[DataContract]
public class Poseesions
{
    [DataMember]
    public string ItemName;
}

public static class ReaderWriterConstants
{
    public const string ReaderWriterType = "ReaderWriterType";
    public const string Encoding = "Encoding";
    public const string TransferMode = "TransferMode";
    public const string ReaderMode = "ReaderMode";
    public const string ReaderMode_Streamed = "Streamed";
    public const string ReaderMode_Buffered = "Buffered";

    public const string RootElementName = "Root";
    public const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
}

public static class FragmentHelper
{
    public static bool CanFragment(XmlDictionaryWriter writer)
    {
        IFragmentCapableXmlDictionaryWriter fragmentWriter = writer as IFragmentCapableXmlDictionaryWriter;
        return fragmentWriter != null && fragmentWriter.CanFragment;
    }

    public static void Start(XmlDictionaryWriter writer, Stream stream)
    {
        Start(writer, stream, false);
    }

    public static void Start(XmlDictionaryWriter writer, Stream stream, bool generateSelfContainedText)
    {
        EnsureWriterCanFragment(writer);
        ((IFragmentCapableXmlDictionaryWriter)writer).StartFragment(stream, generateSelfContainedText);
    }

    public static void End(XmlDictionaryWriter writer)
    {
        EnsureWriterCanFragment(writer);
        ((IFragmentCapableXmlDictionaryWriter)writer).EndFragment();
    }

    public static void Write(XmlDictionaryWriter writer, byte[] buffer, int offset, int count)
    {
        EnsureWriterCanFragment(writer);
        ((IFragmentCapableXmlDictionaryWriter)writer).WriteFragment(buffer, offset, count);
    }

    static void EnsureWriterCanFragment(XmlDictionaryWriter writer)
    {
        if (!CanFragment(writer))
        {
            throw new InvalidOperationException("Fragment cannot be done using writer " + writer.GetType());
        }
    }
}

[System.Xml.Serialization.XmlTypeAttribute(Namespace = "http://schemas.xmlsoap.org/ws/2003/03/addressing")]
[System.Xml.Serialization.XmlRootAttribute("Action", Namespace = "http://schemas.xmlsoap.org/ws/2003/03/addressing", IsNullable = false)]
public class AttributedURI
{
    [XmlText]
    public string Value;

    public bool[] BooleanValues = new bool[] { false, true, false, true, true };
}

[XmlSerializerAssembly(AssemblyName = "AssemblyAttrTestClass")]
public class AssemblyAttrTestClass
{
    public string TestString { get; set;  }
}

public class MyXmlTextParser : IXmlTextParser
{
    private XmlTextReader _myreader;
    public MyXmlTextParser(XmlTextReader reader)
    {
        _myreader = reader;
    }
    bool IXmlTextParser.Normalized
    {
        get
        {
            return _myreader.Normalization;
        }

        set
        {
            _myreader.Normalization = value;
        }
    }

    WhitespaceHandling IXmlTextParser.WhitespaceHandling
    {
        get
        {
            return _myreader.WhitespaceHandling;
        }

        set
        {
            _myreader.WhitespaceHandling = value;
        }
    }
}

[Serializable]
public class SquareWithDeserializationCallback : IDeserializationCallback
{

    public int Edge;

    [NonSerialized]
    private int _area;

    public int Area => _area;

    public SquareWithDeserializationCallback(int edge)
    {
        Edge = edge;
        _area = edge * edge;
    }

    void IDeserializationCallback.OnDeserialization(object sender)
    {
        // After being deserialized, initialize the _area field
        // using the deserialized Radius value.
        _area = Edge * Edge;
    }
}

public class SampleTextWriter : IXmlTextWriterInitializer
{
    public Encoding Encoding;
    public Stream Stream;
    public SampleTextWriter()
    {

    }
    public void SetOutput(Stream stream, Encoding encoding, bool ownsStream)
    {
        Encoding = encoding;
        Stream = stream;
    }
}

public class MycodeGenerator : XmlSerializationGeneratedCode
{

}

public class SoapEncodedTestType1
{
    public int IntValue;
    public double DoubleValue;
    public string StringValue;
    public DateTime DateTimeValue;
}

public enum SoapEncodedTestEnum
{
    [SoapEnum("Small")]
    A,
    [SoapEnum("Large")]
    B
}

public class SoapEncodedTestType2
{
    [SoapElement(IsNullable = true)]
    public SoapEncodedTestType3 TestType3;

}

public class SoapEncodedTestType3
{
    [SoapElement(IsNullable = true)]
    public string StringValue;
}

public class SoapEncodedTestType4
{
    [SoapElement(IsNullable = true)]
    public int? IntValue;
    [SoapElement(IsNullable = true)]
    public double? DoubleValue;
}

public class SoapEncodedTestType5
{
    public string Name;

    [SoapElement(DataType = "nonNegativeInteger", ElementName = "PosInt")]
    public string PostitiveInt;

    public DateTime Today;
}

public class MyCircularLink
{
    public MyCircularLink Link;
    public int IntValue;

    public MyCircularLink() { }
    public MyCircularLink(bool init)
    {
        Link = new MyCircularLink() { IntValue = 1 };
        Link.Link = new MyCircularLink() { IntValue = 2 };
        Link.Link.Link = this;
    }
}
public class MyGroup
{
    public string GroupName;
    public MyItem[] MyItems;
}

public class MyGroup2
{
    public string GroupName;
    public List<MyItem> MyItems;
}

public class MyGroup3
{
    public string GroupName;
    public Dictionary<int, MyItem> MyItems;
}

public class MyItem
{
    public string ItemName;
}

public class MyOrder
{
    public int ID;
    public string Name;
}

public class MySpecialOrder : MyOrder
{
    public int SecondaryID;
}

public class MySpecialOrder2 : MyOrder
{
    public int SecondaryID;
}

[System.Runtime.Serialization.DataContractAttribute(Namespace = "http://tempuri.org/")]
public partial class GetDataRequestBody
{
    [System.Runtime.Serialization.DataMemberAttribute(Order = 0)]
    public int value;

    public GetDataRequestBody()
    {
    }

    public GetDataRequestBody(int value)
    {
        this.value = value;
    }
}

[System.Runtime.Serialization.DataContractAttribute(Namespace = "http://tempuri.org/")]
public partial class GetDataUsingDataContractRequestBody
{
    [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue = false, Order = 0)]
    public CompositeTypeForXmlMembersMapping composite;

    public GetDataUsingDataContractRequestBody()
    {
    }

    public GetDataUsingDataContractRequestBody(CompositeTypeForXmlMembersMapping composite)
    {
        this.composite = composite;
    }
}

[System.Runtime.Serialization.DataContractAttribute(Name = "CompositeType", Namespace = "http://tempuri.org/")]
[System.SerializableAttribute()]
public partial class CompositeTypeForXmlMembersMapping
{
    private bool BoolValueField;

    [System.Runtime.Serialization.OptionalFieldAttribute()]
    private string StringValueField;

    [System.Runtime.Serialization.DataMemberAttribute(IsRequired = true)]
    public bool BoolValue
    {
        get
        {
            return BoolValueField;
        }
        set
        {
            BoolValueField = value;
        }
    }

    [System.Runtime.Serialization.DataMemberAttribute(EmitDefaultValue = false)]
    public string StringValue
    {
        get
        {
            return StringValueField;
        }
        set
        {
            StringValueField = value;
        }
    }
}

public class XmlMembersMappingTypeHavingIntArray
{
    public int[] IntArray;
}

public class TypeWithXmlAttributes
{
    [XmlAttribute(Namespace = "http://www.MyNs.org")]
    public string MyName;

    [XmlAttribute(DataType = "date", AttributeName = "CreationDate")]
    public DateTime Today;
}

public class TypeWithNullableObject
{
    [SoapElement(IsNullable = true)]
    public object MyObject;
}

public delegate void MyDelegate();

[Serializable]
public class TypeWithDelegate : ISerializable
{
    public TypeWithDelegate()
    {

    }

    public TypeWithDelegate(SerializationInfo info, StreamingContext context)
    {
        IntProperty = info.GetInt32("IntValue");
    }

    public int IntProperty { get; set; }

    public MyDelegate DelegateProperty { get; set; }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("IntValue", IntProperty);
    }
}

public class JsonTypes
{
    public Dictionary<string, string> StringKeyValue
    {
        get
        {
            return new Dictionary<string, string>()
            {
                {
                   "Hi", "There"
                }
            };
        }
    }

    public Dictionary<TestEnumValues, TestEnumValues> EnumKeyValue
    {
        get
        {
            return new Dictionary<TestEnumValues, TestEnumValues>()
            {
                {
                    TestEnumValues.Value1, TestEnumValues.Value2
                }
            };
        }
    }

    public Dictionary<TestStruct, TestStruct> StructKeyValue
    {
        get
        {
            return new Dictionary<TestStruct, TestStruct>()
            {
                {
                    new TestStruct(){value1 = 12}, new TestStruct(){value1 = 15}
                }
            };
        }
    }

    public Dictionary<TestClass, object> ObjectKeyValue
    {
        get
        {
            return new Dictionary<TestClass, object>()
            {
                {
                    new TestClass(){intList = new List<int>(){1,2}, floatNum = 45f},
                    new TestClass(){intList = new List<int>(){4,5}, floatNum = 90f}
                },
                {
                    new TestClass(){intList = new List<int>(){6,7}, floatNum = 10f},
                    new TestStruct(){value1 = 25}
                },
            };
        }
    }

    [DataContract]
    public class DictionaryClass
    {
        [DataMember]
        private Dictionary<string, string> _dict = new Dictionary<string, string>()
        {
            {
              "Title", "Sherlocl Kholmes"
            },
            {
              "Name", "study scarlet"
            }
        };
    }

    public DateTimeFormat DTF_DMMMM
    {
        get
        {
            return new DateTimeFormat("d, MMMM", CultureInfo.CreateSpecificCulture("es-AR"));
        }
    }

    public DateTimeFormat DTF_hmsFt
    {
        get
        {
            return new DateTimeFormat("hh:mm:ss.ff tt", CultureInfo.CreateSpecificCulture("es-AR"));
        }
    }

    public DateTimeFormat DTF_MMMM
    {
        get
        {
            return new DateTimeFormat("MMMM", CultureInfo.CurrentCulture);
        }
    }

    public DateTimeFormat DTF_s
    {
        get
        {
            return new DateTimeFormat("ss", CultureInfo.CreateSpecificCulture("de-DE"));
        }
    }

    public DateTimeFormat DTF_yyyygg
    {
        get
        {
            return new DateTimeFormat("yyyy gg", CultureInfo.InvariantCulture);
        }
    }

    public DateTimeFormat DTF_UTC
    {
        get
        {
            return new DateTimeFormat("yyyy-MM-ddTHH:mm:ss.fffK", CultureInfo.InvariantCulture);
        }
    }

    public DateTimeFormat DTF_DefaultFormatProviderIsDateTimeFormatInfoDotCurrentInfo
    {
        get
        {
            return new DateTimeFormat("yyyy-MM-ddTHH:mm:ss.fffK");
        }
    }

    [DataContract]
    public class DTF_class
    {
        [DataMember]
        public DateTime dt1 { get; set; }
        [DataMember]
        public DateTime dt2 { get; set; }
        [DataMember]
        public DateTime dt3 { get; set; }
        [DataMember]
        public DateTime dt4 { get; set; }
    }

    public List<DateTime> DT_List
    {
        get
        {
            return new List<DateTime>()
            {
                new DateTime(1, 1, 1, 3, 58, 32),
                new DateTime(DateTime.Now.Year, 12, 20),
                new DateTime(1998, 1, 1),
                new DateTime(1, 1, 1, 3, 58, 32,DateTimeKind.Utc)
            };
        }
    }

    public Dictionary<DateTime, DateTime> DT_Dictionary
    {
        get
        {
            return new Dictionary<DateTime, DateTime>
            {
                { new DateTime(1, 1, 1, 3, 58, 32), new DateTime(1, 1, 1, 3, 58, 32,DateTimeKind.Utc) },
                { new DateTime(1998, 1, 1), new DateTime(DateTime.Now.Year, 12, 20) }
            };
        }
    }

    public List<object> ObjectList
    {
        get
        {
            return new List<object>()
            {
                new Dictionary<string,string>()
                {
                    {
                      "Title", "Sherlocl Kholmes"
                    }
                },
                new int[]{1,2,3},
                new object[]{"hi", 1, "there"}
            };
        }
    }

    public List<object> ObjectListDeserialized
    {
        get
        {
            return new List<object>()
            {
                new object[]{new KeyValuePair<string,string>("Title", "Sherlocl Kholmes")},
                new object[]{1,2,3},
                new object[]{"hi", 1, "there"}
            };
        }
    }
}

public enum TestEnumValues
{
    Value1 = 3,
    Value2 = 4
}

public struct TestStruct
{
    public int value1;

    public override string ToString()
    {
        return this.value1.ToString();
    }

    public static TestStruct Parse(string value)
    {
        TestStruct result = new TestStruct();
        result.value1 = int.Parse(value);
        return result;
    }
}

public class TestClass
{
    public List<int> intList { get; set; }
    public float floatNum { get; set; }
    private static char s_listSeparator = ',';
    private static char s_memberSeparator = '#';

    public override string ToString()
    {
        string ints = string.Join(",", intList);
        return string.Format("{0}{1}{2}", ints, s_memberSeparator, floatNum);
    }

    public static TestClass Parse(string value)
    {
        string[] members = value.Split(s_memberSeparator);
        string[] numbers = members[0].Split(s_listSeparator);

        List<int> ints = new List<int>();
        foreach (string number in numbers)
        {
            ints.Add(int.Parse(number));
        }
        TestClass o = new TestClass();
        o.intList = ints;
        o.floatNum = float.Parse(members[1]);
        return o;
    }

    public override int GetHashCode()
    {
        return (int)this.floatNum;
    }
}

public class TestClassWithoutKT
{
    public object testClass;
}

[KnownType(typeof(TestClass))]
public class TestClassWithKT
{
    public object testClass;
}

public class ImplementDictionary : IDictionary
{
    private DictionaryEntry[] _items;
    private int _itemsInUse = 0;

    public ImplementDictionary()
    {
        _items = new DictionaryEntry[10];
    }

    public ImplementDictionary(int numItems)
    {
        _items = new DictionaryEntry[numItems];
    }

    #region IDictionary Members
    public bool IsReadOnly { get { return false; } }
    public bool Contains(object key)
    {
        int index;
        return TryGetIndexOfKey(key, out index);
    }
    public bool IsFixedSize { get { return false; } }
    public void Remove(object key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        int index;
        if (TryGetIndexOfKey(key, out index))
        {
            Array.Copy(_items, index + 1, _items, index, _itemsInUse - index - 1);
            _itemsInUse--;
        }
        else
        {
        }
    }
    public void Clear() { _itemsInUse = 0; }
    public void Add(object key, object value)
    {
        if (_itemsInUse == _items.Length)
            throw new InvalidOperationException("The dictionary cannot hold any more items.");
        _items[_itemsInUse++] = new DictionaryEntry(key, value);
    }
    public ICollection Keys
    {
        get
        {
            object[] keys = new object[_itemsInUse];
            for (int n = 0; n < _itemsInUse; n++)
                keys[n] = _items[n].Key;
            return keys;
        }
    }
    public ICollection Values
    {
        get
        {
            object[] values = new object[_itemsInUse];
            for (int n = 0; n < _itemsInUse; n++)
                values[n] = _items[n].Value;
            return values;
        }
    }
    public object this[object key]
    {
        get
        {
            int index;
            if (TryGetIndexOfKey(key, out index))
            {
                return _items[index].Value;
            }
            else
            {
                return null;
            }
        }
        set
        {
            int index;
            if (TryGetIndexOfKey(key, out index))
            {
                _items[index].Value = value;
            }
            else
            {
                Add(key, value);
            }
        }
    }
    private bool TryGetIndexOfKey(object key, out int index)
    {
        for (index = 0; index < _itemsInUse; index++)
        {
            if (_items[index].Key.Equals(key)) return true;
        }
        return false;
    }
    private class ImplementDictionaryEnumerator : IDictionaryEnumerator
    {
        private DictionaryEntry[] _items;
        private int _index = -1;

        public ImplementDictionaryEnumerator(ImplementDictionary sd)
        {
            _items = new DictionaryEntry[sd.Count];
            Array.Copy(sd._items, _items, sd.Count);
        }

        public object Current { get { ValidateIndex(); return _items[_index]; } }

        public DictionaryEntry Entry
        {
            get { return (DictionaryEntry)Current; }
        }

        public object Key { get { ValidateIndex(); return _items[_index].Key; } }

        public object Value { get { ValidateIndex(); return _items[_index].Value; } }

        public bool MoveNext()
        {
            if (_index < _items.Length - 1) { _index++; return true; }
            return false;
        }

        private void ValidateIndex()
        {
            if (_index < 0 || _index >= _items.Length)
                throw new InvalidOperationException("Enumerator is before or after the collection.");
        }

        public void Reset()
        {
            _index = -1;
        }
    }
    public IDictionaryEnumerator GetEnumerator()
    {
        return new ImplementDictionaryEnumerator(this);
    }
    #endregion

    #region ICollection Members
    public bool IsSynchronized { get { return false; } }
    public object SyncRoot { get { throw new NotImplementedException(); } }
    public int Count { get { return _itemsInUse; } }
    public void CopyTo(Array array, int index) { throw new NotImplementedException(); }
    #endregion

    #region IEnumerable Members
    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IDictionary)this).GetEnumerator();
    }
    #endregion
}

[DataContract]
[KnownType(typeof(DerivedType))]
public class BaseType
{
    [DataMember]
    public string StrBase = "base";
}
[DataContract]
public class DerivedType : BaseType
{
    [DataMember]
    public string StrDerived = "derived";
}

public class Group1WithXmlTextAttr
{
    [XmlText(typeof(string))]
    [XmlElement(typeof(int))]
    [XmlElement(typeof(double))]
    public object[] All = new object[] { 321, "One", 2, 3.0, "Two" };
}

public class Group2WithXmlTextAttr
{
    [XmlText(Type = typeof(GroupType))]
    public GroupType TypeOfGroup;
}

public enum GroupType
{
    Small,
    Medium,
    Large
}

public class Group3WithXmlTextAttr
{
    [XmlText(Type = typeof(DateTime))]
    public DateTime CreationTime = new DateTime(2017, 4, 20, 3, 8, 15, DateTimeKind.Utc);
}

public class Group4WithXmlTextAttr
{
    [XmlText(Type = typeof(DateTime))]
    public DateTime CreationTime = new DateTime(2017, 4, 20, 3, 8, 15, DateTimeKind.Utc);

    [XmlText]
    public string Text = "SomeText";
}

[DataContract]
public class DelegateClass
{
    public DelegateClass() { }

    [DataMember]
    public object container;

    [DataMember]
    public static string delegateVariable = "";

    [DataMember]
    public static object someType;

    public static void TestingTheDelegate(People P)
    {
        delegateVariable = "Verifying the Delegate Test";
        someType = P;
    }
}

public delegate void Del(People P);

[DataContract]
public class People
{
    public People(string variation)
    {
        Age = 6;
        Name = "smith";
    }

    public People()
    {
    }

    [DataMember]
    public int Age;

    [DataMember]
    public string Name;
}

public class SoapComplexType
{
    public bool BoolValue;
    public string StringValue;
}

public class SoapComplexTypeWithArray
{
    public int[] IntArray;
    public string[] StringArray;
    public List<int> IntList;
    public List<string> StringList;
}
[KnownType("KnownTypes")]
[DataContract]
public class EmployeeC
{
    public EmployeeC(string name)
    {
        Name = name;
    }

    [DataMember]
    public string Name;

    static Type[] KnownTypes()
    {
        return new Type[] { typeof(Manager), typeof(EmployeeC) };
    }
}

[DataContract]
public class Manager : EmployeeC
{
    public Manager(string name) : base(name)
    {
    }

    [DataMember]
    public int age;

    [DataMember]
    public EmployeeC[] emps;
}

[Serializable]
public class MyArgumentException : Exception, ISerializable
{
    private string _paramName;

    public MyArgumentException() : base() { }

    public MyArgumentException(string message) : base(message)
    {
    }

    public MyArgumentException(string message, string paramName) : base(message)
    {
        _paramName = paramName;
    }

    protected MyArgumentException(SerializationInfo info, StreamingContext context) : base(info, context) {
        _paramName = info.GetString("ParamName");
    }

    public string ParamName
    {
        get
        {
            return _paramName;
        }
        internal set
        {
            _paramName = value;
        }
    }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        if (info == null)
        {
            throw new ArgumentNullException(nameof(info));
        }

        base.GetObjectData(info, context);
        info.AddValue("ParamName", _paramName, typeof(string));
    }
}

[DataContract(IsReference = true)]
public class DC
{
    [DataMember]
    public string Data = new DateTime().ToLongDateString();

    [DataMember]
    public DC Next;
}

[CollectionDataContract(Name = "SampleICollectionTExplicitWithoutDC")]
public class SampleICollectionTExplicitWithoutDC : ICollection<DC>
{
    private List<DC> _internalList = new List<DC>();
    public SampleICollectionTExplicitWithoutDC() { }
    public SampleICollectionTExplicitWithoutDC(bool init)
    {
        DC dc1 = new DC();
        _internalList.Add(dc1);
        _internalList.Add(new DC());
        _internalList.Add(dc1);
    }

    void ICollection<DC>.Add(DC item)
    {
        _internalList.Add(item);
    }

    void ICollection<DC>.Clear()
    {
        _internalList.Clear();
    }

    bool ICollection<DC>.Contains(DC item)
    {
        return _internalList.Contains(item);
    }

    void ICollection<DC>.CopyTo(DC[] array, int arrayIndex)
    {
        _internalList.CopyTo(array, arrayIndex);
    }

    int ICollection<DC>.Count
    {
        get { return _internalList.Count; }
    }

    bool ICollection<DC>.IsReadOnly
    {
        get { return false; }
    }

    bool ICollection<DC>.Remove(DC item)
    {
        return _internalList.Remove(item);
    }

    IEnumerator<DC> IEnumerable<DC>.GetEnumerator()
    {
        return _internalList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _internalList.GetEnumerator();
    }
}

public class NetNativeTestData
{
    public static NetNativeTestData[] InvalidTypes = new NetNativeTestData[] {
            new NetNativeTestData(typeof(Invalid_Class_No_Parameterless_Ctor),
                () => new Invalid_Class_No_Parameterless_Ctor("test"),
                "Type 'Invalid_Class_No_Parameterless_Ctor' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute.  If the type is a collection, consider marking it with the CollectionDataContractAttribute.  See the Microsoft .NET Framework documentation for other supported types."),
            new NetNativeTestData(typeof(Invalid_Class_Derived_With_DataContract),
                () => new Invalid_Class_Derived_With_DataContract(),
                "Type 'Invalid_Class_Derived_With_DataContract' cannot inherit from a type that is not marked with DataContractAttribute or SerializableAttribute.  Consider marking the base type 'Invalid_Class_Base_Without_DataContract' with DataContractAttribute or SerializableAttribute, or removing them from the derived type." ),
            new NetNativeTestData(typeof(Invalid_Class_KnownType_Invalid_Type),
                () => new Invalid_Class_KnownType_Invalid_Type(),
                "Type 'Invalid_Class_No_Parameterless_Ctor' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute.  If the type is a collection, consider marking it with the CollectionDataContractAttribute.  See the Microsoft .NET Framework documentation for other supported types." ),
        };

    // This list exists solely to expose all the root objects being serialized.
    // Without this ILC removes our test data types
    // All new test data types *must* be added to one of these lists to appear to the ILC.
    // Test data now added here will result in "serializer not found" exception at runtime.
    public static DataContractSerializer[] Serializers = new DataContractSerializer[]
    {
            new DataContractSerializer(typeof(Invalid_Class_No_Parameterless_Ctor)),
            new DataContractSerializer(typeof(List<Invalid_Class_No_Parameterless_Ctor>)),
            new DataContractSerializer(typeof(Invalid_Class_Derived_With_DataContract)),
            new DataContractSerializer(typeof(Invalid_Class_KnownType_Invalid_Type))
    };

    public NetNativeTestData(Type type, Func<object> instantiate, string errorMessage)
    {
        Type = type;
        ErrorMessage = errorMessage;
        Instantiate = instantiate;
    }

    public Type Type { get; set; }

    public string ErrorMessage { get; set; }

    public Func<object> Instantiate
    {
        get; set;
    }
}
public abstract class Invalid_Class_Base_Without_DataContract
{

}

// Invalid because it is a derived [DataContract] class whose base class is not
[DataContract]
public class Invalid_Class_Derived_With_DataContract : Invalid_Class_Base_Without_DataContract
{

}

// Invalid because its [KnownType] is an invalid type
[KnownType(typeof(Invalid_Class_No_Parameterless_Ctor))]
public class Invalid_Class_KnownType_Invalid_Type
{
    public Invalid_Class_KnownType_Invalid_Type()
    {

    }
}

public class Invalid_Class_No_Parameterless_Ctor
{
    public Invalid_Class_No_Parameterless_Ctor(string id)
    {
        ID = id;
    }

    public string ID { get; set; }
}

public class NativeJsonTestData
{
    public static NativeJsonTestData[] Json_InvalidTypes = new NativeJsonTestData[] {
                new NativeJsonTestData(typeof(Invalid_Class_No_Parameterless_Ctor),
                    () => new Invalid_Class_No_Parameterless_Ctor("test")),
                new NativeJsonTestData(typeof(Invalid_Class_Derived_With_DataContract),
                    () => new Invalid_Class_Derived_With_DataContract()),
            };

    // This list exists solely to expose all the root objects being serialized.
    // Without this ILC removes our test data types
    // All new test data types *must* be added to one of these lists to appear to the ILC.
    // Test data now added here will result in "serializer not found" exception at runtime.
    public static DataContractJsonSerializer[] JsonSerializers = new DataContractJsonSerializer[]
    {
                new DataContractJsonSerializer(typeof(Invalid_Class_No_Parameterless_Ctor)),
                new DataContractJsonSerializer(typeof(List<Invalid_Class_No_Parameterless_Ctor>)),
                new DataContractJsonSerializer(typeof(Invalid_Class_Derived_With_DataContract)),
    };

    public NativeJsonTestData(Type type, Func<object> instantiate)
    {
        Type = type;
        Instantiate = instantiate;
    }

    public Type Type { get; set; }
    public Func<object> Instantiate { get; set; }
}

[DataContract]
public class ContractGeneric : IExtensibleDataObject
{
    public ExtensionDataObject ExtensionData { get; set; }
}

[DataContract(Name = "ContractGeneric")]
public class ContractExtended
{
    [DataMember(Name = "item", Order = 1)]
    public Item Item;
}

[DataContract]
public class Item
{
    [DataMember(Name = "id", Order = 1, EmitDefaultValue = false)]
    public long? Id;

    [DataMember(Name = "code", Order = 2, EmitDefaultValue = false)]
    public long? Code;
}

public class TypeWithCollectionAndDateTimeOffset
{
    public TypeWithCollectionAndDateTimeOffset()
    {
        _anIntList = new List<int>();
    }

    public TypeWithCollectionAndDateTimeOffset(List<int> list, DateTimeOffset dateTimeOffset)
    {
        _anIntList = list;
        DateTimeOffset = dateTimeOffset;
    }

    private List<int> _anIntList;
    public List<int> AnIntList
    {
        get
        {
            return _anIntList;
        }
    }

    public DateTimeOffset DateTimeOffset { get; set; }
}

[KnownType(typeof(bool))]
[KnownType(typeof(byte[]))]
[KnownType(typeof(char))]
[KnownType(typeof(DateTime))]
[KnownType(typeof(decimal))]
[KnownType(typeof(double))]
[KnownType(typeof(float))]
[KnownType(typeof(Guid))]
[KnownType(typeof(int))]
[KnownType(typeof(long))]
[KnownType(typeof(object))]
[KnownType(typeof(XmlQualifiedName))]
[KnownType(typeof(short))]
[KnownType(typeof(sbyte))]
[KnownType(typeof(string))]
[KnownType(typeof(TimeSpan))]
[KnownType(typeof(byte))]
[KnownType(typeof(uint))]
[KnownType(typeof(ulong))]
[KnownType(typeof(ushort))]
[KnownType(typeof(Uri))]
[CollectionDataContract]
public class TypeWithPrimitiveKnownTypes : List<object>
{

}

public enum TestEnum { Off, On, Both }
public class EnumTestBase { }
public class EnumTestDerived : EnumTestBase
{
    [XmlText]
    public TestEnum Test { get; set; }
}

public class PrimiveAttributeTestBase { }
public class PrimiveAttributeTestDerived : PrimiveAttributeTestBase
{
    [XmlText]
    public int Number { get; set; }
}
