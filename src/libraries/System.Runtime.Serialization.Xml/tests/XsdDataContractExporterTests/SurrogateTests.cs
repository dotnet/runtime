// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;


namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
{
    public class SurrogateTests
    {
        private readonly ITestOutputHelper _output;
        public SurrogateTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(SurrogateProvider_MemberData))]
        public void SurrogateProvider(Type type, ISerializationSurrogateProvider surrogate, Action<string, XmlSchemaSet> schemaCheck = null)
        {
            ExportOptions options = new ExportOptions() { DataContractSurrogate = surrogate };
            XsdDataContractExporter exporter = new XsdDataContractExporter() { Options = options };

            exporter.Export(type);
            string schema = SchemaUtils.DumpSchema(exporter.Schemas);
            _output.WriteLine(schema);

            if (schemaCheck != null)
                schemaCheck(schema, exporter.Schemas);
        }
        public static IEnumerable<object[]> SurrogateProvider_MemberData()
        {
            yield return new object[] { typeof(SurrogateTests.CircleContainer), new NodeToSerializableNode(new CircleToSquare(new XmlSerializerToXmlFormatter(null))), (string s, XmlSchemaSet ss) => {
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""SurrogateTests.CircleContainer"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Circles"" nillable=""true"" type=""tns:ArrayOfSurrogateTests.Circle"">", ref s);
                SchemaUtils.OrderedContains(@"<Surrogate xmlns:d1p1=""http://www.w3.org/2001/XMLSchema"" i:type=""d1p1:string"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">Property</Surrogate>", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""circle"" nillable=""true"" type=""tns:SurrogateTests.Square"">", ref s);
                SchemaUtils.OrderedContains(@"<Surrogate xmlns:d1p1=""http://www.w3.org/2001/XMLSchema"" i:type=""d1p1:string"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">Field</Surrogate>", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""SurrogateTests.CircleContainer"" nillable=""true"" type=""tns:SurrogateTests.CircleContainer"" />", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""ArrayOfSurrogateTests.Circle"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" maxOccurs=""unbounded"" name=""SurrogateTests.Circle"" nillable=""true"" type=""tns:SurrogateTests.Square"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""ArrayOfSurrogateTests.Circle"" nillable=""true"" type=""tns:ArrayOfSurrogateTests.Circle"" />", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""SurrogateTests.Square"">", ref s);
                SchemaUtils.OrderedContains(@"<Surrogate xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" z:Id=""1"" xmlns:d1p1=""http://schemas.datacontract.org/2004/07/System"" i:type=""d1p1:Version"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">", ref s);
                SchemaUtils.OrderedContains(@"<d1p1:_Build>0</d1p1:_Build>", ref s);
                SchemaUtils.OrderedContains(@"<d1p1:_Major>8</d1p1:_Major>", ref s);
                SchemaUtils.OrderedContains(@"<d1p1:_Minor>0</d1p1:_Minor>", ref s);
                SchemaUtils.OrderedContains(@"<d1p1:_Revision>0</d1p1:_Revision>", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""Side"" type=""xs:int"">", ref s);
                SchemaUtils.OrderedContains(@"<Surrogate xmlns:d1p1=""http://www.w3.org/2001/XMLSchema"" i:type=""d1p1:string"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">Field</Surrogate>", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""SurrogateTests.Square"" nillable=""true"" type=""tns:SurrogateTests.Square"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:schema targetNamespace=""http://www.w3.org/2001/XMLSchema"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
            } };
            yield return new object[] { typeof(SurrogateTests.Node), new NodeToSerializableNode(new CircleToSquare(new XmlSerializerToXmlFormatter(null))), (string s, XmlSchemaSet ss) => {
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""SurrogateTests.SerializableNode"">", ref s);
                SchemaUtils.OrderedContains(@"<Surrogate xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" z:Id=""1"" xmlns:d1p1=""http://schemas.datacontract.org/2004/07/System"" i:type=""d1p1:Version"" xmlns:z=""http://schemas.microsoft.com/2003/10/Serialization/"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">", ref s);
                SchemaUtils.OrderedContains(@"<d1p1:_Build>0</d1p1:_Build>", ref s);
                SchemaUtils.OrderedContains(@"<d1p1:_Major>8</d1p1:_Major>", ref s);
                SchemaUtils.OrderedContains(@"<d1p1:_Minor>0</d1p1:_Minor>", ref s);
                SchemaUtils.OrderedContains(@"<d1p1:_Revision>0</d1p1:_Revision>", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""next"" nillable=""true"" type=""tns:SurrogateTests.SerializableNode"">", ref s);
                SchemaUtils.OrderedContains(@"<Surrogate xmlns:d1p1=""http://www.w3.org/2001/XMLSchema"" i:type=""d1p1:string"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">Field</Surrogate>", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""SurrogateTests.SerializableNode"" nillable=""true"" type=""tns:SurrogateTests.SerializableNode"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:schema targetNamespace=""http://www.w3.org/2001/XMLSchema"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
            } };
            yield return new object[] { typeof(SurrogateTests.XmlSerializerPerson), new NodeToSerializableNode(new CircleToSquare(new XmlSerializerToXmlFormatter(null))), (string s, XmlSchemaSet ss) => {
                SchemaUtils.OrderedContains(@"<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""XmlSerializerPersonElement"" nillable=""true"" type=""XmlSerializerPerson"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:complexType name=""XmlSerializerPerson"">", ref s);
                SchemaUtils.OrderedContains(@"<Surrogate xmlns:d1p1=""http://www.w3.org/2001/XMLSchema"" i:type=""d1p1:string"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">XmlSerializable</Surrogate>", ref s);
                SchemaUtils.OrderedContains(@"<xs:attribute name=""Name"" type=""xs:string"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:attribute name=""Age"" type=""xs:int"" use=""required"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""XmlSerializerPerson"" nillable=""true"" type=""XmlSerializerPerson"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:schema targetNamespace=""http://www.w3.org/2001/XMLSchema"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
            } };
            yield return new object[] { typeof(SurrogateTests.ValidSurrogateTest), new PersonSurrogate(), (string s, XmlSchemaSet ss) => {
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:complexType name=""SurrogateTests.ValidSurrogateTest"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Friends"" nillable=""true"" type=""tns:ArrayOfSurrogateTests.NonSerializablePerson"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""SurrogateTests.ValidSurrogateTest"" nillable=""true"" type=""tns:SurrogateTests.ValidSurrogateTest"" />", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""ArrayOfSurrogateTests.NonSerializablePerson"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" maxOccurs=""unbounded"" name=""SurrogateTests.NonSerializablePerson"" nillable=""true"" type=""tns:SurrogateTests.Person"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""ArrayOfSurrogateTests.NonSerializablePerson"" nillable=""true"" type=""tns:ArrayOfSurrogateTests.NonSerializablePerson"" />", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""SurrogateTests.Person"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""name"" nillable=""true"" type=""xs:string"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""SurrogateTests.Person"" nillable=""true"" type=""tns:SurrogateTests.Person"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:schema targetNamespace=""http://www.w3.org/2001/XMLSchema"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
            } };
            yield return new object[] { typeof(SurrogateTests.ValidSurrogateTestDC), new PersonSurrogate(), (string s, XmlSchemaSet ss) => {
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""SurrogateTests.ValidSurrogateTestDC"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Friends"" nillable=""true"" type=""tns:ArrayOfSurrogateTests.NonSerializablePersonDC"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""SurrogateTests.ValidSurrogateTestDC"" nillable=""true"" type=""tns:SurrogateTests.ValidSurrogateTestDC"" />", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""ArrayOfSurrogateTests.NonSerializablePersonDC"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" maxOccurs=""unbounded"" name=""SurrogateTests.NonSerializablePersonDC"" nillable=""true"" type=""tns:SurrogateTests.PersonDC"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""ArrayOfSurrogateTests.NonSerializablePersonDC"" nillable=""true"" type=""tns:ArrayOfSurrogateTests.NonSerializablePersonDC"" />", ref s);

                SchemaUtils.OrderedContains(@"<xs:complexType name=""SurrogateTests.PersonDC"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""name"" nillable=""true"" type=""xs:string"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""SurrogateTests.PersonDC"" nillable=""true"" type=""tns:SurrogateTests.PersonDC"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:schema targetNamespace=""http://www.w3.org/2001/XMLSchema"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
            } };
        }

        [Theory]
        [MemberData(nameof(SurrogateProvider_Negative_MemberData))]
        public void SurrogateProvider_Negative(Type badType, ISerializationSurrogateProvider surrogate, Type exceptionType, string exMsg = null)
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            exporter.Options = new ExportOptions();
            exporter.Options.DataContractSurrogate = surrogate;

            var ex = Assert.Throws(exceptionType, () => exporter.Export(badType));
            if (exMsg != null)
                Assert.Equal(exMsg, ex.Message);
        }
        public static IEnumerable<object[]> SurrogateProvider_Negative_MemberData()
        {
            yield return new object[] { typeof(SurrogateTests.InvalidSurrogateTest), new CollectionASurrogate(), typeof(InvalidDataContractException) };
            yield return new object[] { typeof(SurrogateTests.InvalidSurrogateTestDC), new CollectionASurrogate(), typeof(InvalidDataContractException) };
        }

        #region SurrogateProviders
        public class CircleToSquare : ISerializationSurrogateProvider2
        {
            ISerializationSurrogateProvider2? _nextSurrogate;
            public CircleToSquare(ISerializationSurrogateProvider2? nextSurrogate)
            {
                this._nextSurrogate = nextSurrogate;
            }

            public Type GetSurrogateType(Type type)
            {
                if (type == typeof(SurrogateTests.Circle))
                    return typeof(SurrogateTests.Square);
                return (_nextSurrogate != null) ? _nextSurrogate.GetSurrogateType(type) : type;
            }

            public object GetCustomDataToExport(Type clrType, Type dcType)
            {
                if (clrType == typeof(SurrogateTests.Circle) && dcType == typeof(SurrogateTests.Square))
                    return clrType.Assembly.GetName().Version;
                return (_nextSurrogate != null) ? _nextSurrogate.GetCustomDataToExport(clrType, dcType) : null;
            }

            public object GetCustomDataToExport(MemberInfo memberInfo, Type dcType) => memberInfo.MemberType.ToString();
            public void GetKnownCustomDataTypes(Collection<Type> knownTypes) { }
            public object GetObjectToSerialize(object obj, Type memberType) => throw new NotImplementedException();
            public object GetDeserializedObject(object obj, Type memberType) => throw new NotImplementedException();
            public Type GetReferencedTypeOnImport(string name, string ns, object customData) => null;
        }

        public class NodeToSerializableNode : ISerializationSurrogateProvider2
        {
            ISerializationSurrogateProvider2? _nextSurrogate;
            public NodeToSerializableNode(ISerializationSurrogateProvider2? nextSurrogate)
            {
                this._nextSurrogate = nextSurrogate;
            }

            public Type GetSurrogateType(Type type)
            {
                if (type == typeof(SurrogateTests.Node))
                    return typeof(SurrogateTests.SerializableNode);
                return (_nextSurrogate != null) ? _nextSurrogate.GetSurrogateType(type) : type;
            }

            public object GetCustomDataToExport(Type clrType, Type dcType)
            {
                if (clrType == typeof(SurrogateTests.Node) && dcType == typeof(SurrogateTests.SerializableNode))
                    return clrType.Assembly.GetName().Version;
                return (_nextSurrogate != null) ? _nextSurrogate.GetCustomDataToExport(clrType, dcType) : null;
            }

            public object GetCustomDataToExport(MemberInfo memberInfo, Type dcType) => memberInfo.MemberType.ToString();
            public void GetKnownCustomDataTypes(Collection<Type> knownTypes) => knownTypes.Add(typeof(Version));
            public object GetObjectToSerialize(object obj, Type memberType) => throw new NotImplementedException();
            public object GetDeserializedObject(object obj, Type memberType) => throw new NotImplementedException();
            public Type GetReferencedTypeOnImport(string name, string ns, object customData) => null;
        }


        public class XmlSerializerToXmlFormatter : ISerializationSurrogateProvider2
        {
            ISerializationSurrogateProvider2? _nextSurrogate;
            public XmlSerializerToXmlFormatter(ISerializationSurrogateProvider2? nextSurrogate)
            {
                this._nextSurrogate = nextSurrogate;
            }

            public Type GetSurrogateType(Type type)
            {
                if (type == typeof(SurrogateTests.XmlSerializerPerson))
                    return typeof(SurrogateTests.XmlSerializerAdapter<SurrogateTests.XmlSerializerPerson>);
                return (_nextSurrogate != null) ? _nextSurrogate.GetSurrogateType(type) : type;
            }

            public object GetCustomDataToExport(Type clrType, Type dcType)
            {
                if (clrType == typeof(SurrogateTests.XmlSerializerPerson) && dcType == typeof(SurrogateTests.XmlSerializerAdapter<SurrogateTests.XmlSerializerPerson>))
                    return "XmlSerializable";
                return (_nextSurrogate != null) ? _nextSurrogate.GetCustomDataToExport(clrType, dcType) : null;
            }

            public object GetCustomDataToExport(MemberInfo memberInfo, Type dcType) => memberInfo.MemberType.ToString();
            public void GetKnownCustomDataTypes(Collection<Type> knownTypes) { }
            public Type GetReferencedTypeOnImport(string name, string ns, object customData) => null;
            public object GetObjectToSerialize(object obj, Type memberType) => throw new NotImplementedException();
            public object GetDeserializedObject(object obj, Type memberType) => throw new NotImplementedException();
        }

        class PersonSurrogate : ISerializationSurrogateProvider2
        {
            public Type GetSurrogateType(Type type)
            {
                if (typeof(SurrogateTests.NonSerializablePerson).IsAssignableFrom(type))
                {
                    return typeof(SurrogateTests.Person);
                }

                if (typeof(SurrogateTests.NonSerializablePersonDC).IsAssignableFrom(type))
                {
                    return typeof(SurrogateTests.PersonDC);
                }
                return type;
            }

            public object GetObjectToSerialize(object obj, Type targetType)
            {
                SurrogateTests.NonSerializablePerson nonSerializablePerson = obj as SurrogateTests.NonSerializablePerson;
                if (nonSerializablePerson != null)
                {
                    return new Person();
                }
                SurrogateTests.NonSerializablePersonDC nonSerializablePersonDC = obj as SurrogateTests.NonSerializablePersonDC;
                if (nonSerializablePersonDC != null)
                {
                    return new SurrogateTests.PersonDC();
                }
                return obj;
            }

            public object GetDeserializedObject(object obj, Type targetType)
            {
                SurrogateTests.Person ps = obj as SurrogateTests.Person;
                if (ps != null)
                {
                    return new SurrogateTests.NonSerializablePerson("John Smith");
                }
                SurrogateTests.PersonDC psDC = obj as SurrogateTests.PersonDC;
                if (psDC != null)
                {
                    return new SurrogateTests.NonSerializablePersonDC("John Smith");
                }
                return obj;
            }

            public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
            {
                if (typeNamespace.Equals("http://schemas.datacontract.org/2004/07/Suites.SchemaExport"))
                {
                    if (typeName.Equals("DataContractSurrogateTest.Person"))
                    {
                        return typeof(SurrogateTests.NonSerializablePerson);
                    }
                    if (typeName.Equals("DataContractSurrogateTest.PersonDC"))
                    {
                        return typeof(SurrogateTests.NonSerializablePersonDC);
                    }
                }
                return null;
            }

            public object GetCustomDataToExport(Type clrType, Type dataContractType) => null;
            public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType) => null;
            public void GetKnownCustomDataTypes(Collection<Type> customDataTypes) { }
        }

        //This is the surrogate that substitutes CollectionWithoutParameterlessCtor<int> for CollectionA.
        class CollectionASurrogate : ISerializationSurrogateProvider2
        {
            public Type GetSurrogateType(Type type)
            {
                if (typeof(ExporterTypesTests.CollectionA).IsAssignableFrom(type))
                {
                    return typeof(ExporterTypesTests.CollectionWithoutParameterlessCtor<int>);
                }
                return type;
            }

            public object GetObjectToSerialize(object obj, Type targetType)
            {
                ExporterTypesTests.CollectionA collectionA = obj as ExporterTypesTests.CollectionA;
                if (collectionA != null)
                {
                    ExporterTypesTests.CollectionWithoutParameterlessCtor<int> validCollection = new ExporterTypesTests.CollectionWithoutParameterlessCtor<int>(1);
                    validCollection.Add(1);
                    return validCollection;
                }
                return obj;
            }

            public object GetDeserializedObject(object obj, Type targetType)
            {
                ExporterTypesTests.CollectionWithoutParameterlessCtor<int> validCollection = obj as ExporterTypesTests.CollectionWithoutParameterlessCtor<int>;
                if (validCollection != null)
                {
                    return new ExporterTypesTests.CollectionA();
                }
                return obj;
            }

            public Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData)
            {
                if (typeNamespace.Equals("http://schemas.datacontract.org/2004/07/Suites.SchemaExport"))
                {
                    if (typeName.Equals("ExporterTypesTests.CollectionWithoutParameterlessCtor`1"))
                    {
                        return typeof(ExporterTypesTests.CollectionA);
                    }
                }
                return null;
            }

            public object GetCustomDataToExport(Type clrType, Type dataContractType) => null;
            public object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, Type dataContractType) => null;
            public void GetKnownCustomDataTypes(Collection<Type> customDataTypes) { }
        }
        #endregion

        #region Surrogate Test Types
#pragma warning disable CS0169, CS0414
        public class ValidSurrogateTest
        {
            ExporterTypesTests.CollectionWithoutParameterlessCtor<NonSerializablePerson> friends;

            public ExporterTypesTests.CollectionWithoutParameterlessCtor<NonSerializablePerson> Friends
            {
                get
                {
                    friends = friends ?? new ExporterTypesTests.CollectionWithoutParameterlessCtor<NonSerializablePerson>(2);
                    return friends;
                }
            }
        }

        public class InvalidSurrogateTest
        {
            ExporterTypesTests.CollectionA localList = new ExporterTypesTests.CollectionA();

            public ExporterTypesTests.CollectionA Surrogated
            {
                get
                {
                    return localList;
                }
            }
        }

        [DataContract]
        public class ValidSurrogateTestDC
        {
            ExporterTypesTests.CollectionWithoutParameterlessCtor<NonSerializablePersonDC> friends;

            [DataMember]
            public ExporterTypesTests.CollectionWithoutParameterlessCtor<NonSerializablePersonDC> Friends
            {
                get
                {
                    friends = friends ?? new ExporterTypesTests.CollectionWithoutParameterlessCtor<NonSerializablePersonDC>(2);
                    return friends;
                }
            }
        }

        [DataContract]
        public class InvalidSurrogateTestDC
        {
            ExporterTypesTests.CollectionA localList = new ExporterTypesTests.CollectionA();

            [DataMember]
            public ExporterTypesTests.CollectionA Surrogated
            {
                get
                {
                    return localList;
                }
            }
        }

        [DataContract]
        public class CircleContainer
        {
            [DataMember]
            Circle circle;
            [DataMember]
            public Circle[] Circles { get { return null; } set { } }
        }

        [Serializable]
        public class Circle
        {
            public int Radius;
        }

        [Serializable]
        public class Square
        {
            public int Side;
        }

        public class Node
        {
            Node next;
        }

        [Serializable]
        public class SerializableNode
        {
            SerializableNode next;
        }

        [XmlRoot("XmlSerializerPersonElement")]
        public class XmlSerializerPerson
        {
            public XmlSerializerPerson() { }
            [XmlAttribute]
            public string Name;
            [XmlAttribute]
            public int Age;
        }

        [XmlSchemaProvider("StaticGetSchema")]
        public class XmlSerializerAdapter<T> : IXmlSerializable
        {
            public XmlSchema GetSchema()
            {
                throw new NotImplementedException();
            }

            public void ReadXml(XmlReader reader)
            {
                throw new NotImplementedException();
            }

            public void WriteXml(XmlWriter writer)
            {
                throw new NotImplementedException();
            }

            static XmlQualifiedName StaticGetSchema(XmlSchemaSet schemaSet)
            {
                XmlReflectionImporter importer = new XmlReflectionImporter();
                XmlTypeMapping xmlTypeMapping = importer.ImportTypeMapping(typeof(T));
                XmlSchemas schemas = new XmlSchemas();
                XmlSchemaExporter exporter = new XmlSchemaExporter(schemas);
                exporter.ExportTypeMapping(xmlTypeMapping);
                schemas.Compile(new ValidationEventHandler(ValidationCallbackWithErrorCode), true);
                for (int i = 0; i < schemas.Count; i++)
                {
                    XmlSchema schema = schemas[i];
                    schemaSet.Add(schema);
                }
                return new XmlQualifiedName(xmlTypeMapping.TypeName, xmlTypeMapping.Namespace);
            }

            private static void ValidationCallbackWithErrorCode(object sender, ValidationEventArgs args)
            {
                Console.WriteLine("Schema warning: " + args.Message);
            }
        }

        public class Person
        {
            public string name = "John Smith";
        }

        public class NonSerializablePerson
        {
            public string name;

            public NonSerializablePerson(string name)
            {
                this.name = name;
            }
        }

        [DataContract]
        public class PersonDC
        {
            [DataMember]
            public string name = "John Smith";
        }

        public class NonSerializablePersonDC
        {
            public string name;

            public NonSerializablePersonDC(string name)
            {
                this.name = name;
            }
        }
#pragma warning restore CS0169, CS0414
        #endregion
    }
}
