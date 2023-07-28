// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SerializationTestTypes
{
    [DataContract(IsReference = false)]
    public class BaseDC
    {
        [DataMember]
        public string data;
        [DataMember]
        public string data2;

        public BaseDC() { }

        public virtual string Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }

        public virtual string Data2
        {
            get
            {
                return data2;
            }
            set
            {
                data2 = value;
            }
        }

        public BaseDC(bool init)
        {
            data = "TestString";
            data2 = "TestString2";
        }
    }

    [Serializable]
    [DataContract(IsReference = false)]
    public class BaseSerializable
    {
        [DataMember]
        public string data;
        [DataMember]
        public string data2;
        [DataMember]
        public string[] days;

        public BaseSerializable() { }

        public virtual string Data
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }

        public virtual string Data2
        {
            get
            {
                return data2;
            }
            set
            {
                data2 = value;
            }
        }

        public BaseSerializable(bool init)
        {
            data = "TestString";
            data2 = "TestString2";
            days = new string[] { "Base1", "Base2", "Base3", "Base4", "Base5", "Base6", "Base7" };
        }
    }

    [DataContract(IsReference = false)]
    public class DerivedDC : BaseDC
    {
        [DataMember]
        public string data0;

        [DataMember]
        public string data1;

        [DataMember]
        public string data3;

        public DerivedDC() { }

        public override string Data
        {
            get
            {
                return data0;
            }
            set
            {
                data0 = value;
            }
        }

        public virtual string Data1
        {
            get
            {
                return data1;
            }
            set
            {
                data1 = value;
            }
        }

        public string Data3
        {
            get
            {
                return data3;
            }
            set
            {
                data3 = value;
            }
        }

        public DerivedDC(bool init)
            : base(init)
        {
            data0 = "TestString0";
            data1 = "TestString1";
            data3 = "TestString3";
        }
    }

    [Serializable]
    public class DerivedSerializable : BaseSerializable
    {
        public string data0;
        public string data1;
        public string data3;

        public DerivedSerializable() { }

        public override string Data
        {
            get
            {
                return data0;
            }
            set
            {
                data0 = value;
            }
        }

        public virtual string Data1
        {
            get
            {
                return data1;
            }
            set
            {
                data1 = value;
            }
        }

        public string Data3
        {
            get
            {
                return data3;
            }
            set
            {
                data3 = value;
            }
        }

        public DerivedSerializable(bool init)
            : base(init)
        {
            data0 = "TestString0";
            data1 = "TestString1";
            data3 = "TestString3";
        }
    }

    [DataContract(IsReference = false)]
    public class DerivedDCIsRefBaseSerializable : BaseSerializable
    {
        [DataMember]
        public string Data33;
    }

    [DataContract(IsReference = false)]
    public class DerivedDCBaseSerializable : BaseSerializable
    {
        [DataMember]
        public string Data33;
    }

    [DataContract(IsReference = false)]
    public class Derived2DC : DerivedDC
    {
        [DataMember]
        public string data11;
        [DataMember]
        public string data12;
        [DataMember]
        public string data4;

        public Derived2DC() { }

        public override string Data
        {
            get
            {
                return data11;
            }
            set
            {
                data11 = value;
            }
        }

        public override string Data1
        {
            get
            {
                return data12;
            }
            set
            {
                data12 = value;
            }
        }

        public string Data4
        {
            get
            {
                return data4;
            }
            set
            {
                data4 = value;
            }
        }

        public Derived2DC(bool init)
            : base(init)
        {
            data11 = "TestString11";
            data12 = "TestString12";
            data4 = "TestString4";
        }
    }

    [DataContract]
    public class BaseDCNoIsRef
    {
        [DataMember]
        private string _data;

        public BaseDCNoIsRef()
        {
            _data = string.Empty;
        }
    }

    public class DerivedPOCOBaseDCNOISRef : BaseDCNoIsRef
    {
        public string Data22;
    }

    public class DerivedIXmlSerializable_POCOBaseDCNOISRef : DerivedPOCOBaseDCNOISRef, IXmlSerializable
    {
        public System.Xml.Schema.XmlSchema GetSchema()
        {
            XmlSchema schema = new XmlSchema();
            XmlSchemaElement element1 = new XmlSchemaElement();
            element1.Name = "Data";
            element1.SchemaType = XmlSchemaSimpleType.GetBuiltInComplexType(new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema"));
            schema.Items.Add(element1);
            schema.Id = "Schema1";
            return schema;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            XmlDictionaryReader xmlReader = XmlDictionaryReader.CreateDictionaryReader(reader);
            string data = xmlReader.ReadString();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            XmlDictionaryWriter xmlWriter = XmlDictionaryWriter.CreateDictionaryWriter(writer);
            xmlWriter.WriteString("TestString");
        }
    }

    [CollectionDataContract(IsReference = false)]
    public class DerivedCDCFromBaseDC : BaseDC, IList<string>
    {
        public string Data223 = string.Empty;
        private List<string> _internalData = new List<string>();

        public int IndexOf(string item)
        {
            return _internalData.IndexOf(item);
        }

        public void Insert(int index, string item)
        {
            _internalData.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
        }

        public string this[int index]
        {
            get
            {
                return _internalData[index];
            }
            set
            {
                _internalData[index] = value;
            }
        }

        public void Add(string item)
        {
            _internalData.Add(item);
        }

        public void Clear()
        {
        }

        public bool Contains(string item)
        {
            return _internalData.Contains(item);
        }

        public void CopyTo(string[] array, int arrayIndex)
        {
        }

        public int Count
        {
            get { return _internalData.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(string item)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _internalData.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _internalData.GetEnumerator();
        }

        public static DerivedCDCFromBaseDC CreateInstance()
        {
            DerivedCDCFromBaseDC list = new DerivedCDCFromBaseDC();
            list.Add("112");
            return list;
        }
    }

    [Serializable]
    public class Derived2Serializable : DerivedDC
    {
        public string data00;
        public string data122;
        public string data4;

        public Derived2Serializable() { }

        public override string Data
        {
            get
            {
                return data00;
            }
            set
            {
                data00 = value;
            }
        }

        public override string Data1
        {
            get
            {
                return data122;
            }
            set
            {
                data122 = value;
            }
        }

        public string Data4
        {
            get
            {
                return data4;
            }
            set
            {
                data4 = value;
            }
        }

        public Derived2Serializable(bool init)
            : base(init)
        {
            data00 = "TestString00";
            data122 = "TestString122";
            data4 = "TestString4";
        }
    }

    [Serializable]
    public class Derived2SerializablePositive : DerivedDC
    {
        [OptionalField]
        public string data00;
        [OptionalField]
        public string data122;
        [OptionalField]
        public string data4;

        public Derived2SerializablePositive() { }

        public override string Data
        {
            get
            {
                return data00;
            }
            set
            {
                data00 = value;
            }
        }

        public override string Data1
        {
            get
            {
                return data122;
            }
            set
            {
                data122 = value;
            }
        }

        public string Data4
        {
            get
            {
                return data4;
            }
            set
            {
                data4 = value;
            }
        }

        public Derived2SerializablePositive(bool init)
            : base(init)
        {
            data00 = "TestString00";
            data122 = "TestString122";
            data4 = "TestString4";
        }
    }

    [DataContract(IsReference = false)]
    public class Derived2Derived2Serializable : Derived2Serializable
    {
        public Derived2Derived2Serializable() { }
        public Derived2Derived2Serializable(bool init) : base(init) { }
    }

    [DataContract(IsReference = false)]
    public class Derived3Derived2Serializable : Derived2Serializable
    {
        public Derived3Derived2Serializable() { }
        public Derived3Derived2Serializable(bool init) : base(init) { }
    }

    public class Derived31Derived2SerializablePOCO : Derived2Serializable
    {
        public SimpleDCWithRef SimpleDCWithRefData;
        public SimpleDCWithRef RefData;

        public Derived31Derived2SerializablePOCO() { }
        public Derived31Derived2SerializablePOCO(bool init)
            : base(init)
        {
            SimpleDCWithRefData = new SimpleDCWithRef(true);
            RefData = SimpleDCWithRefData;
        }
    }

    [DataContract]
    public class Derived4Derived2Serializable : Derived3Derived2Serializable
    {
        public Derived4Derived2Serializable() { }
        public Derived4Derived2Serializable(bool init) : base(init) { }
    }

    [Serializable]
    public class Derived5Derived2Serializable : Derived3Derived2Serializable
    {
        public Derived5Derived2Serializable() { }
        public Derived5Derived2Serializable(bool init) : base(init) { }
    }

    public class Derived6Derived2SerializablePOCO : Derived3Derived2Serializable
    {
        public SimpleDCWithRef SimpleDCWithRefData;
        public SimpleDCWithRef RefData;

        public Derived6Derived2SerializablePOCO() { }
        public Derived6Derived2SerializablePOCO(bool init)
            : base(init)
        {
            SimpleDCWithRefData = new SimpleDCWithRef(true);
            RefData = SimpleDCWithRefData;
        }
    }
}
