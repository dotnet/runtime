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
    public class TypeWithDateTimeStringProperty
    {
        public string DateTimeString;
        public DateTime CurrentDateTime;

        public TypeWithDateTimeStringProperty() { }
    }
    public class SimpleType
    {
        public string P1 { get; set; }
        public int P2 { get; set; }

        public static bool AreEqual(SimpleType x, SimpleType y)
        {
            if (x == null)
            {
                return y == null;
            }
            else if (y == null)
            {
                return x == null;
            }
            else
            {
                return (x.P1 == y.P1) && (x.P2 == y.P2);
            }
        }
    }

    public class TypeWithGetSetArrayMembers
    {
        public SimpleType[] F1;
        public int[] F2;

        public SimpleType[] P1 { get; set; }
        public int[] P2 { get; set; }
    }

    public class TypeWithGetOnlyArrayProperties
    {
        private SimpleType[] _p1 = new SimpleType[2];
        private int[] _p2 = new int[2];
        public SimpleType[] P1
        {
            get
            {
                return _p1;
            }
        }

        public int[] P2
        {
            get
            {
                return _p2;
            }
        }
    }

    public struct StructNotSerializable
    {
        public int value;

        public override int GetHashCode()
        {
            return value;
        }
    }

    public class MyCollection<T> : ICollection<T>
    {
        private List<T> _items = new List<T>();

        public MyCollection()
        {
        }

        public MyCollection(params T[] values)
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

    public class TypeWithMyCollectionField
    {
        public MyCollection<string> Collection;
    }

    public class TypeWithReadOnlyMyCollectionProperty
    {
        private MyCollection<string> _ro = new MyCollection<string>();
        public MyCollection<string> Collection
        {
            get
            {
                return _ro;
            }
        }
    }

    public class MyList : IList
    {
        private List<object> _items = new List<object>();

        public MyList()
        {
        }

        public MyList(params object[] values)
        {
            _items.AddRange(values);
        }

        public int Add(object value)
        {
            return ((IList)_items).Add(value);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(object value)
        {
            return _items.Contains(value);
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public bool IsFixedSize
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public object this[int index]
        {
            get
            {
                return _items[index];
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public bool IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        public object SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        public IEnumerator GetEnumerator()
        {
            return ((IEnumerable)_items).GetEnumerator();
        }
    }
    public enum MyEnum
    {
        [EnumMember]
        One,
        Two,
        [EnumMember]
        Three
    }

    public class TypeWithEnumMembers
    {
        public MyEnum F1;
        public MyEnum P1 { get; set; }
    }

    [DataContract]
    public struct DCStruct
    {
        [DataMember]
        public string Data;
        public DCStruct(bool init)
        {
            Data = "Data";
        }
    }

    [DataContract]
    public class DCClassWithEnumAndStruct
    {
        [DataMember]
        public DCStruct MyStruct;

        [DataMember]
        public MyEnum MyEnum1;

        public DCClassWithEnumAndStruct() { }
        public DCClassWithEnumAndStruct(bool init)
        {
            MyStruct = new DCStruct(init);
        }
    }
    public class BuiltInTypes
    {
        public byte[] ByteArray { get; set; }
    }

    public class TypeA
    {
        public string Name;
    }

    public class TypeB
    {
        public string Name;

        public static implicit operator TypeA(TypeB i)
        {
            return new TypeA { Name = i.Name };
        }

        public static implicit operator TypeB(TypeA i)
        {
            return new TypeB { Name = i.Name };
        }
    }

    public class TypeHasArrayOfASerializedAsB
    {
        public TypeA[] Items;

        public TypeHasArrayOfASerializedAsB() { }
        public TypeHasArrayOfASerializedAsB(bool init)
        {
            Items = new TypeA[]
            {
                new TypeA { Name = "typeAValue" },
                new TypeB { Name = "typeBValue" },
            };
        }
    }

    public class __TypeNameWithSpecialCharacters\u6F22\u00F1
    {
        public string PropertyNameWithSpecialCharacters\u6F22\u00F1 { get; set; }
    }

    public class BaseClassWithSamePropertyName
    {
        [DataMember]
        public string StringProperty;

        [DataMember]
        public int IntProperty;

        [DataMember]
        public DateTime DateTimeProperty;

        [DataMember]
        public List<string> ListProperty;
    }

    public class DerivedClassWithSameProperty : BaseClassWithSamePropertyName
    {
        [DataMember]
        public new string StringProperty;

        [DataMember]
        public new int IntProperty;

        [DataMember]
        public new DateTime DateTimeProperty;

        [DataMember]
        public new List<string> ListProperty;
    }

    public class DerivedClassWithSameProperty2 : DerivedClassWithSameProperty
    {
        [DataMember]
        public new DateTime DateTimeProperty;

        [DataMember]
        public new List<string> ListProperty;
    }

    public class TypeWithDateTimePropertyAsXmlTime
    {
        DateTime _value;

        [XmlText(DataType = "time")]
        public DateTime Value
        {
            get { return _value; }
            set { _value = value; }
        }
    }

    public class TypeWithByteArrayAsXmlText
    {
        [XmlText(DataType = "base64Binary")]
        public byte[] Value;
    }

    [DataContract(IsReference = false)]
    public class SimpleDC
    {
        [DataMember]
        public string Data;
        public SimpleDC() { }
        public SimpleDC(bool init)
        {
            Data = DateTime.MaxValue.ToString("T", CultureInfo.InvariantCulture);
        }
    }

    [XmlRoot(Namespace = "http://schemas.xmlsoap.org/ws/2005/04/discovery", IsNullable = false)]
    public class TypeWithXmlTextAttributeOnArray
    {
        [XmlText]
        public string[] Text;
    }

    [Flags]
    public enum EnumFlags
    {
        [EnumMember]
        One = 0x01,
        [EnumMember]
        Two = 0x02,
        [EnumMember]
        Three = 0x04,
        [EnumMember]
        Four = 0x08
    }

    public interface IBaseInterface
    {
        string ClassID { get; }

        string DisplayName { get; set; }

        string Id { get; set; }

        bool IsLoaded { get; set; }
    }

    [DataContract]
    public class ClassImplementsInterface : IBaseInterface
    {
        public virtual string ClassID { get; set; }

        [DataMember]
        public string DisplayName { get; set; }

        [DataMember]
        public string Id { get; set; }

        public bool IsLoaded { get; set; }
    }


    #region XmlSerializer specific
    public class WithStruct
    {
        public SomeStruct Some { get; set; }
    }

    public struct SomeStruct
    {
        public int A;
        public int B;
    }

    public class WithEnums
    {
        public IntEnum Int { get; set; }
        public ShortEnum Short { get; set; }
    }

    public class WithNullables
    {
        public IntEnum? Optional { get; set; }
        public IntEnum? Optionull { get; set; }
        public int? OptionalInt { get; set; }
        public Nullable<int> OptionullInt { get; set; }
        public SomeStruct? Struct1 { get; set; }
        public SomeStruct? Struct2 { get; set; }
    }

    public enum ByteEnum : byte
    {
        Option0, Option1, Option2
    }

    public enum SByteEnum : sbyte
    {
        Option0, Option1, Option2
    }

    public enum ShortEnum : short
    {
        Option0, Option1, Option2
    }

    public enum IntEnum
    {
        Option0, Option1, Option2
    }

    public enum UIntEnum : uint
    {
        Option0, Option1, Option2
    }

    public enum LongEnum : long
    {
        Option0, Option1, Option2
    }

    public enum ULongEnum : ulong
    {
        Option0, Option1, Option2
    }

    [XmlRoot(DataType = "XmlSerializerAttributes", ElementName = "AttributeTesting", IsNullable = false)]
    [XmlInclude(typeof(ItemChoiceType))]
    public class XmlSerializerAttributes
    {
        public XmlSerializerAttributes()
        {
            XmlElementProperty = 1;
            XmlAttributeProperty = 2;
            XmlArrayProperty = new string[] { "one", "two", "three" };
            EnumType = ItemChoiceType.Word;
            MyChoice = "String choice value";
            XmlIncludeProperty = ItemChoiceType.DecimalNumber;
            XmlEnumProperty = new ItemChoiceType[] { ItemChoiceType.DecimalNumber, ItemChoiceType.Number, ItemChoiceType.Word, ItemChoiceType.None };
            XmlTextProperty = "<xml>Hello XML</xml>";
            XmlNamespaceDeclarationsProperty = "XmlNamespaceDeclarationsPropertyValue";
        }

        [XmlElement(DataType = "int", ElementName = "XmlElementPropertyNode", Namespace = "http://element", Type = typeof(int))]
        public int XmlElementProperty { get; set; }

        [XmlAttribute(AttributeName = "XmlAttributeName")]
        public int XmlAttributeProperty { get; set; }

        [XmlArray(ElementName = "CustomXmlArrayProperty", Namespace = "http://mynamespace")]
        [XmlArrayItem(typeof(string))]
        public object[] XmlArrayProperty { get; set; }

        [XmlChoiceIdentifier("EnumType")]
        [XmlElement("Word", typeof(string))]
        [XmlElement("Number", typeof(int))]
        [XmlElement("DecimalNumber", typeof(double))]
        public object MyChoice;

        // Don't serialize this field. The EnumType field contains the enumeration value that corresponds to the MyChoice field value.
        [XmlIgnore]
        public ItemChoiceType EnumType;

        [XmlElement]
        public object XmlIncludeProperty;

        [XmlEnum("EnumProperty")]
        public ItemChoiceType[] XmlEnumProperty;

        [XmlText]
        public string XmlTextProperty;

        [XmlNamespaceDeclarations]
        public string XmlNamespaceDeclarationsProperty;
    }

    [XmlType(IncludeInSchema = false)]
    public enum ItemChoiceType
    {
        None,
        Word,
        Number,
        DecimalNumber
    }

    public class TypeWithAnyAttribute
    {
        public string Name;

        [XmlAttribute]
        public int IntProperty { get; set; }

        [XmlAnyAttribute]
        public XmlAttribute[] Attributes { get; set; }
    }

    public class KnownTypesThroughConstructor
    {
        public object EnumValue;

        public object SimpleTypeValue;
    }

    public class SimpleKnownTypeValue
    {
        public string StrProperty { get; set; }
    }

    public class ClassImplementingIXmlSerialiable : IXmlSerializable
    {
        public static bool WriteXmlInvoked = false;
        public static bool ReadXmlInvoked = false;

        public string StringValue { get; set; }
        private bool BoolValue { get; set; }

        public ClassImplementingIXmlSerialiable()
        {
            BoolValue = true;
        }

        public bool GetPrivateMember()
        {
            return BoolValue;
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
            BoolValue = bool.Parse(reader.GetAttribute("BoolValue"));
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            WriteXmlInvoked = true;
            writer.WriteAttributeString("StringValue", StringValue);
            writer.WriteAttributeString("BoolValue", BoolValue.ToString());
        }
    }
    public class TypeWithPropertyNameSpecified
    {
        public string MyField;

        [XmlIgnore]
        public bool MyFieldSpecified;

        public int MyFieldIgnored;

        [XmlIgnore]
        public bool MyFieldIgnoredSpecified;
    }

    [XmlType(Namespace = ""), XmlRoot(Namespace = "", IsNullable = true)]
    public class TypeWithXmlSchemaFormAttribute
    {
        [XmlArray(Form = XmlSchemaForm.Unqualified)]
        public List<int> UnqualifiedSchemaFormListProperty { get; set; }

        [XmlArray(Form = XmlSchemaForm.None), XmlArrayItem("NoneParameter", Form = XmlSchemaForm.None, IsNullable = false)]
        public List<string> NoneSchemaFormListProperty { get; set; }

        [XmlArray(Form = XmlSchemaForm.Qualified), XmlArrayItem("QualifiedParameter", Form = XmlSchemaForm.Qualified, IsNullable = false)]
        public List<bool> QualifiedSchemaFormListProperty { get; set; }
    }

    [XmlType(TypeName = "MyXmlType")]
    public class TypeWithTypeNameInXmlTypeAttribute
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified)]
        public string XmlAttributeForm;
    }

    [XmlType(AnonymousType = true)]
    public class TypeWithSchemaFormInXmlAttribute
    {
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "http://test.com")]
        public string TestProperty;
    }


    #endregion

    public class TypeWithNonPublicDefaultConstructor
    {
        private static string s_prefix;
        static TypeWithNonPublicDefaultConstructor()
        {
            s_prefix = "Mr. ";
        }

        private TypeWithNonPublicDefaultConstructor()
        {
            Name = s_prefix + "FooName";
        }
        public string Name { get; set; }
    }

    // Comes from app: The Weather Channel. See bug 1101076 for details
    public class ServerSettings
    {
        public string DS2Root { get; set; }
        public string MetricConfigUrl { get; set; }
    }

    [DataContract]
    public class TypeWithXmlQualifiedName
    {
        [DataMember(IsRequired = true, EmitDefaultValue = false)]
        public XmlQualifiedName Value { get; set; }
    }

    public class TypeWith2DArrayProperty2
    {
        [System.Xml.Serialization.XmlArrayItemAttribute("SimpleType", typeof(SimpleType[]), IsNullable = false)]
        public SimpleType[][] TwoDArrayOfSimpleType;
    }

    public class TypeWithPropertiesHavingDefaultValue
    {
        [DefaultValue("")]
        public string EmptyStringProperty { get; set; } = "";

        [DefaultValue("DefaultString")]
        public string StringProperty { get; set; } = "DefaultString";

        [DefaultValue(11)]
        public int IntProperty { get; set; } = 11;

        [DefaultValue('m')]
        public char CharProperty { get; set; } = 'm';
    }

    public class TypeWithEnumPropertyHavingDefaultValue
    {
        [DefaultValue(1)]
        public IntEnum EnumProperty { get; set; } = IntEnum.Option1;
    }

    public class TypeWithEnumFlagPropertyHavingDefaultValue
    {
        [DefaultValue(EnumFlags.One | EnumFlags.Four)]
        public EnumFlags EnumProperty { get; set; } = EnumFlags.One | EnumFlags.Four;
    }

    public class TypeWithShouldSerializeMethod
    {
        private static readonly string DefaultFoo = "default";

        public string Foo { get; set; } = DefaultFoo;

        public bool ShouldSerializeFoo()
        {
            return Foo != DefaultFoo;
        }
    }

    public class KnownTypesThroughConstructorWithArrayProperties
    {
        public object StringArrayValue;
        public object IntArrayValue;
    }

    public class KnownTypesThroughConstructorWithValue
    {
        public object Value;
    }

    public class TypeWithTypesHavingCustomFormatter
    {
        [XmlElement(DataType = "dateTime")]
        public DateTime DateTimeContent;

        [XmlElement(DataType = "QName")]
        public XmlQualifiedName QNameContent;

        // The case where DataType = "date" is verified by Xml_TypeWithDateTimePropertyAsXmlTime.
        [XmlElement(DataType = "date")]
        public DateTime DateContent;

        [XmlElement(DataType = "Name")]
        public string NameContent;

        [XmlElement(DataType = "NCName")]
        public string NCNameContent;

        [XmlElement(DataType = "NMTOKEN")]
        public string NMTOKENContent;

        [XmlElement(DataType = "NMTOKENS")]
        public string NMTOKENSContent;

        [XmlElement(DataType = "base64Binary")]
        public byte[] Base64BinaryContent;

        [XmlElement(DataType = "hexBinary")]
        public byte[] HexBinaryContent;
    }

    public class TypeWithArrayPropertyHavingChoice
    {
        // The ManyChoices field can contain an array
        // of choices. Each choice must be matched to
        // an array item in the ChoiceArray field.
        [XmlChoiceIdentifier("ChoiceArray")]
        [XmlElement("Item", typeof(string))]
        [XmlElement("Amount", typeof(int))]
        public object[] ManyChoices;

        // TheChoiceArray field contains the enumeration
        // values, one for each item in the ManyChoices array.
        [XmlIgnore]
        public MoreChoices[] ChoiceArray;
    }

    public enum MoreChoices
    {
        None,
        Item,
        Amount
    }

    public class TypeWithFieldsOrdered
    {
        [XmlElement(Order = 0)]
        public int IntField1;
        [XmlElement(Order = 1)]
        public int IntField2;
        [XmlElement(Order = 3)]
        public string StringField1;
        [XmlElement(Order = 2)]
        public string StringField2;
    }

    [KnownType(typeof(List<SimpleType>))]
    [KnownType(typeof(SimpleType[]))]
    [DataContract]
    public class TypeWithKnownTypesOfCollectionsWithConflictingXmlName
    {
        [DataMember]
        public object Value1 = new List<SimpleType>();

        [DataMember]
        public object Value2 = new SimpleType[1];

    }

    namespace TypeNameClashA
    {
        [System.Xml.Serialization.XmlType("TypeClashA")]
        public class TypeNameClash
        {
            public string Name { get; set; }
        }
    }

    namespace TypeNameClashB
    {
        [System.Xml.Serialization.XmlType("TypeClashB")]
        public class TypeNameClash
        {
            public string Name { get; set; }
        }
    }

    [System.Xml.Serialization.XmlRootAttribute("Root")]
    [System.Xml.Serialization.XmlType("ContainerType")]
    public class NamespaceTypeNameClashContainer
    {
        [System.Xml.Serialization.XmlElementAttribute("A")]
        public TypeNameClashA.TypeNameClash[] A { get; set; }

        [System.Xml.Serialization.XmlElementAttribute("B")]
        public TypeNameClashB.TypeNameClash[] B { get; set; }
    }
}

public class TypeWithXmlElementProperty
{
    [XmlAnyElement]
    public XmlElement[] Elements;
}

public class TypeWithXmlDocumentProperty
{
    public XmlDocument Document;
}

public class TypeWithBinaryProperty
{
    [XmlElement(DataType = "hexBinary")]
    public byte[] BinaryHexContent { get; set; }
    [XmlElement(DataType = "base64Binary")]
    public byte[] Base64Content { get; set; }
}

public class TypeWithDateTimeOffsetProperties
{
    public DateTimeOffset DTO { get; set; }
    public DateTimeOffset DTO2 { get; set; }

    [XmlElement(ElementName = "DefaultDTO")]
    [DefaultValue(typeof(DateTimeOffset), "1/1/0001 0:00:00 AM +00:00")]
    public DateTimeOffset DTOWithDefault { get; set; }

    public DateTimeOffset? NullableDTO { get; set; }

    [XmlElement(ElementName = "NullableDefaultDTO")]
    [DefaultValue(typeof(DateTimeOffset), "1/1/0001 0:00:00 AM +00:00")]
    public DateTimeOffset? NullableDTOWithDefault { get; set; }
}

public class TypeWithTimeSpanProperty
{
    public TimeSpan TimeSpanProperty;
}

public class TypeWithDefaultTimeSpanProperty
{
    public TypeWithDefaultTimeSpanProperty()
    {
        TimeSpanProperty = GetDefaultValue("TimeSpanProperty");
        TimeSpanProperty2 = GetDefaultValue("TimeSpanProperty2");
    }

    [DefaultValue(typeof(TimeSpan), "00:01:00")]
    public TimeSpan TimeSpanProperty { get; set; }

    [DefaultValue(typeof(TimeSpan), "00:00:01")]
    public TimeSpan TimeSpanProperty2 { get; set; }

    public TimeSpan GetDefaultValue(string propertyName)
    {
        var property = this.GetType().GetProperty(propertyName);

        var attribute = property.GetCustomAttribute(typeof(DefaultValueAttribute))
                as DefaultValueAttribute;

        if (attribute != null)
        {
            return (TimeSpan)attribute.Value;
        }
        else
        {
            return new TimeSpan(0, 0, 0);
        }
    }
}

public class TypeWithByteProperty
{
    public byte ByteProperty;
}


[XmlRoot()]
public class TypeWithXmlNodeArrayProperty
{
    [XmlText]
    public XmlNode[] CDATA { get; set; }
}

public class Animal
{
    public int Age;
    public string Name;
}

public class Dog : Animal
{
    public DogBreed Breed;
}

public enum DogBreed
{
    GermanShepherd,
    LabradorRetriever
}

public class Group
{
    public string GroupName;
    public Vehicle GroupVehicle;
}

public class Vehicle
{
    public string LicenseNumber;
}

[DataContract(Namespace = "www.msn.com/Examples/")]
public class Employee
{
    [DataMember]
    public string EmployeeName;
    [DataMember]
    private string ID = string.Empty;
}

public class SerializeIm : XmlSerializerImplementation
{
    public override XmlSerializer GetSerializer(Type type)
    {
        return new XmlSerializer(type);
    }
}

[XmlInclude(typeof(DerivedClass))]
public class BaseClass
{
    public string value { get; set; }
    public string Value;
}

public class DerivedClass : BaseClass
{
    public new string value;
    public new string Value { get; set; }
}

[XmlRootAttribute("PurchaseOrder", Namespace = "http://www.contoso1.com", IsNullable = false)]
public class PurchaseOrder
{
    public Address ShipTo;
    public string OrderDate;

    [XmlArrayAttribute("Items")]
    public OrderedItem[] OrderedItems;
    public decimal SubTotal;
    public decimal ShipCost;
    public decimal TotalCost;

    public static PurchaseOrder CreateInstance()
    {
        PurchaseOrder po = new PurchaseOrder();
        Address billAddress = new Address();
        billAddress.Name = "John Doe";
        billAddress.Line1 = "1 Main St.";
        billAddress.City = "AnyTown";
        billAddress.State = "WA";
        billAddress.Zip = "00000";
        po.ShipTo = billAddress;
        po.OrderDate = new DateTime(2017, 4, 10).ToString("D", CultureInfo.InvariantCulture);

        OrderedItem item = new OrderedItem();
        item.ItemName = "Widget S";
        item.Description = "Small widget";
        item.UnitPrice = (decimal)5.23;
        item.Quantity = 3;
        item.Calculate();

        OrderedItem[] items = { item };
        po.OrderedItems = items;
        decimal subTotal = new decimal();
        foreach (OrderedItem oi in items)
        {
            subTotal += oi.LineTotal;
        }
        po.SubTotal = subTotal;
        po.ShipCost = (decimal)12.51;
        po.TotalCost = po.SubTotal + po.ShipCost;
        return po;
    }
}

public class Address
{
    [XmlAttribute]
    public string Name;
    public string Line1;

    [XmlElementAttribute(IsNullable = false)]
    public string City;
    public string State;
    public string Zip;

    public static void CreateInstance()
    {
        Address obj = new Address();
        obj.City = "Pune";
        obj.State = "WA";
        obj.Zip = "98052";
    }
}

public class OrderedItem
{
    public string ItemName;
    public string Description;
    public decimal UnitPrice;
    public int Quantity;
    public decimal LineTotal;

    public void Calculate()
    {
        LineTotal = UnitPrice * Quantity;
    }
}

[XmlType("AliasedTestType")]
public class AliasedTestType
{
    [XmlElement("X", typeof(List<int>))]
    [XmlElement("Y", typeof(List<string>))]
    [XmlElement("Z", typeof(List<double>))]
    public object Aliased { get; set; }
}

public class BaseClass1
{
    [XmlElement]
    public MyCollection1 Prop;
}

public class DerivedClass1 : BaseClass1
{
    [XmlElement]
    public new MyCollection1 Prop;
}

public class MyCollection1 : IEnumerable<DateTime>, IEnumerable
{
    private List<DateTime> _values = new List<DateTime>();

    public void Add(DateTime value)
    {
        _values.Add(value);
    }

    IEnumerator<DateTime> IEnumerable<DateTime>.GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _values.GetEnumerator();
    }
}

public static class Outer
{
    public class Person
    {
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
    }
}

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

public class Trumpet : Brass
{
    public char Modulation;
}

public class Pet
{
    [DefaultValueAttribute("Dog")]
    public string Animal;
    [XmlIgnoreAttribute]
    public string Comment;
    public string Comment2;
}

public class TypeWithVirtualGenericProperty<T>
{
    public virtual T Value { get; set; }
}

public class TypeWithVirtualGenericPropertyDerived<T> : TypeWithVirtualGenericProperty<T>
{
    public override T Value { get; set; }
}

public class DefaultValuesSetToNaN
{
    [DefaultValue(double.NaN)]
    public double DoubleProp { get; set; }

    [DefaultValue(float.NaN)]
    public float FloatProp { get; set; }

    [DefaultValue(double.NaN)]
    public double DoubleField;

    [DefaultValue(float.NaN)]
    public float SingleField;

    public override bool Equals(object obj)
    {
        var other = obj as DefaultValuesSetToNaN;
        return other == null ? false :
            other.DoubleProp == this.DoubleProp && other.FloatProp == this.FloatProp &&
            other.DoubleField == this.DoubleField && other.SingleField == this.SingleField;
    }

    public override int GetHashCode()
    {
        return this.DoubleProp.GetHashCode() ^ this.FloatProp.GetHashCode() ^
            this.DoubleField.GetHashCode() ^ this.SingleField.GetHashCode();
    }
}

public class DefaultValuesSetToPositiveInfinity
{
    [DefaultValue(double.PositiveInfinity)]
    public double DoubleProp { get; set; }

    [DefaultValue(float.PositiveInfinity)]
    public float FloatProp { get; set; }

    [DefaultValue(double.PositiveInfinity)]
    public double DoubleField;

    [DefaultValue(float.PositiveInfinity)]
    public float SingleField;

    public override bool Equals(object obj)
    {
        var other = obj as DefaultValuesSetToPositiveInfinity;
        return other == null ? false :
            other.DoubleProp == this.DoubleProp && other.FloatProp == this.FloatProp &&
            other.DoubleField == this.DoubleField && other.SingleField == this.SingleField;
    }

    public override int GetHashCode()
    {
        return this.DoubleProp.GetHashCode() ^ this.FloatProp.GetHashCode() ^
            this.DoubleField.GetHashCode() ^ this.SingleField.GetHashCode();
    }
}

public class DefaultValuesSetToNegativeInfinity
{
    [DefaultValue(double.NegativeInfinity)]
    public double DoubleProp { get; set; }

    [DefaultValue(float.NegativeInfinity)]
    public float FloatProp { get; set; }

    [DefaultValue(double.NegativeInfinity)]
    public double DoubleField;

    [DefaultValue(float.NegativeInfinity)]
    public float SingleField;

    public override bool Equals(object obj)
    {
        var other = obj as DefaultValuesSetToNegativeInfinity;
        return other == null ? false :
            other.DoubleProp == this.DoubleProp && other.FloatProp == this.FloatProp &&
            other.DoubleField == this.DoubleField && other.SingleField == this.SingleField;
    }

    public override int GetHashCode()
    {
        return this.DoubleProp.GetHashCode() ^ this.FloatProp.GetHashCode() ^
            this.DoubleField.GetHashCode() ^ this.SingleField.GetHashCode();
    }
}

[XmlRoot("RootElement")]
public class TypeWithMismatchBetweenAttributeAndPropertyType
{
    private int _intValue = 120;

    [DefaultValue(true), XmlAttribute("IntValue")]
    public int IntValue
    {
        get
        {
            return _intValue;
        }
        set
        {
            _intValue = value;
        }
    }
}

[DataContract(IsReference = true)]
public class TypeWithLinkedProperty
{
    [DataMember]
    public TypeWithLinkedProperty Child { get; set; }
    [DataMember]
    public List<TypeWithLinkedProperty> Children { get; set; }
}

[Serializable()]
[System.Xml.Serialization.XmlType("MsgDocumentType", Namespace = "http://example.com")]
[System.Xml.Serialization.XmlRoot("Document", Namespace = "http://example.com")]
public partial class MsgDocumentType
{
    [System.Xml.Serialization.XmlAttribute("id", DataType = "ID")]
    public string Id { get; set; }

    [System.Xml.Serialization.XmlAttribute("refs", DataType = "IDREFS")]
    public string[] Refs { get; set; }
}

public class RootClass
{
    [XmlArray]
    public List<Parameter> Parameters { get; set; }
}

[XmlInclude(typeof(Parameter<string>))]
public class Parameter
{
    [XmlAttribute]
    public string Name { get; set; }
}

public class Parameter<T> : Parameter
{
    public T Value { get; set; }
}
