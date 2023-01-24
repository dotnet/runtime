using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Security;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    [Serializable]
    internal class DateTimeOffsetGeneric<T>
    {
        T single;
        T[] array;
        List<T> list;
        Dictionary<string, T> dictionary;
    }

    [Serializable]
    internal class Foo<Q> where Q : new()
    {
        Q single;
        Q[] array;
        List<Q> list;
        Dictionary<string, Q> dictionary;
    }

    [DataContract(Name = "PairOf{0}and{1}")]
    internal struct Pair<T1, T2>
    {
        [DataMember]
        T1 t1;
        [DataMember]
        T2 t2;
    }

    [KnownType(typeof(Pair<Foo<Pair<int, float>>, Foo<Bar>>))]
    [Serializable]
    public class Bar
    {
        Foo<Bar> fooBar;
        XsdType<float> xsdFloat;
        XsdType<decimal> xsdDecimal;
        WrapperISerializable<int> wrappedInt;
        WrapperISerializable<int[]> wrappedIntArray;
        DateTimeOffsetGeneric<DateTimeOffset> genericDateTimeOffset;
        DateTimeOffsetGeneric<DateTimeOffset[]> genericDateTimeOffsetArray;
    }

    [Serializable]
    internal class WrapperISerializable<T> : ISerializable
    {
        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context) { }
    }

    [XmlSchemaProvider("StaticGetSchema")]
    public class XsdType<T> : IXmlSerializable
    {
        static XmlSchemaType StaticGetSchema(XmlSchemaSet schemas)
        {
            const string ns = "http://GenericXmlSerializableNs";
            XmlSchema schema = new XmlSchema();
            schema.TargetNamespace = ns;
            schema.Namespaces.Add("tns", ns);
            schemas.Add(schema);

            XmlSchemaComplexType schemaType = new XmlSchemaComplexType();
            schemaType.Name = typeof(T).Name + "Wrapper";
            XmlSchemaSequence sequence = new XmlSchemaSequence();
            schemaType.Particle = sequence;
            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = "MyElement";
            if (typeof(T) == typeof(decimal))
                element.SchemaTypeName = new XmlQualifiedName("decimal", XmlSchema.Namespace);
            else if (typeof(T) == typeof(float))
                element.SchemaTypeName = new XmlQualifiedName("float", XmlSchema.Namespace);
            else
                element.SchemaTypeName = new XmlQualifiedName("anyType", XmlSchema.Namespace);
            sequence.Items.Add(element);
            schema.Items.Add(schemaType);
            schemas.Add(schema);
            return schemaType;
        }

        public XmlSchema GetSchema() { throw new NotImplementedException(); }
        public void ReadXml(XmlReader reader) { throw new NotImplementedException(); }
        public void WriteXml(XmlWriter writer) { throw new NotImplementedException(); }
    }

}

