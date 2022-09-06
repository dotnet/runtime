using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    public class XmlSerializableBase : IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException();
        }

        public static XmlSchema GetSchema(string ns, XmlSchemaSet schemas)
        {
            if (ns == null)
            {
                ns = String.Empty;
            }

            ICollection currentSchemas = schemas.Schemas();
            foreach (XmlSchema schema in currentSchemas) 
            {
                if ((schema.TargetNamespace == null && ns.Length == 0) || ns.Equals(schema.TargetNamespace))
                    return schema;
            }
            if (ns.Length > 0)
            {
                XmlSchema newSchema = new XmlSchema();
                newSchema.TargetNamespace = ns;
                newSchema.Namespaces.Add("tns", ns);
                schemas.Add(newSchema); 
                return newSchema;
            }
            return null;
        }
    }

    [XmlSchemaProvider("StaticGetSchema")]
    [XmlRoot(ElementName ="ComplexTypeElement", Namespace ="http://ElementNs/", IsNullable = false)]
    public class ComplexType : XmlSerializableBase
    {
        static XmlSchemaType StaticGetSchema(XmlSchemaSet schemas)
        {
            XmlSchema schema = GetSchema("http://TypeNs/", schemas);
            XmlSchemaComplexType schemaType = new XmlSchemaComplexType();
            schemaType.Name = "MyComplexType";
            XmlSchemaSequence sequence = new XmlSchemaSequence();
            schemaType.Particle = sequence;
            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = "MyElement";
            element.SchemaTypeName = new XmlQualifiedName("int", XmlSchema.Namespace);
            sequence.Items.Add(element);
            schema.Items.Add(schemaType);
            schemas.Add(schema);
            return schemaType;
        }
    }

    [XmlSchemaProvider("StaticGetSchema")]
    public class SimpleType : XmlSerializableBase
    {
        static XmlQualifiedName StaticGetSchema(XmlSchemaSet schemas)
        {
            XmlSchema schema = GetSchema("http://TypeNs/", schemas);
            XmlSchemaSimpleType schemaType = new XmlSchemaSimpleType();
            schemaType.Name = "MySimpleType";
            XmlSchemaSimpleTypeRestriction content = new XmlSchemaSimpleTypeRestriction();
            schemaType.Content = content;
            content.BaseType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean);
            schema.Items.Add(schemaType);
            schemas.Add(schema);
            return new XmlQualifiedName(schemaType.Name, schema.TargetNamespace);
        }
    }

    [XmlSchemaProvider("StaticGetSchema")]
    [XmlRoot(ElementName ="IntElement", Namespace ="http://ElementNs/", IsNullable = false)]
    public struct XsdInt : IXmlSerializable
    {
        static XmlQualifiedName StaticGetSchema(XmlSchemaSet schemas)
        {
            XmlSchemaType schemaType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Int);
            return schemaType.QualifiedName;
        }

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
    }

    [XmlSchemaProvider("StaticGetSchema")]
    [XmlRoot(ElementName ="StringElement", Namespace ="http://ElementNs/", IsNullable = true)]
    public class XsdString : IXmlSerializable
    {
        static XmlQualifiedName StaticGetSchema(XmlSchemaSet schemas)
        {
            XmlSchemaType schemaType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String);
            return schemaType.QualifiedName;
        }

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
    }

    [XmlSchemaProvider("StaticGetSchema")]
    [XmlRoot(ElementName="ComplexStructElement", Namespace="http://ElementNs/", IsNullable = false)]
    public struct ComplexStruct : IXmlSerializable
    {
        static XmlSchemaType StaticGetSchema(XmlSchemaSet schemas)
        {
            XmlSchema schema = XmlSerializableBase.GetSchema("http://TypeNs/", schemas);
            XmlSchemaComplexType schemaType = new XmlSchemaComplexType();
            schemaType.Name = "MyComplexStruct";
            XmlSchemaSequence sequence = new XmlSchemaSequence();
            schemaType.Particle = sequence;
            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = "MyElement";
            element.SchemaTypeName = new XmlQualifiedName("int", XmlSchema.Namespace);
            sequence.Items.Add(element);
            schema.Items.Add(schemaType);
            schemas.Add(schema);
            return schemaType;
        }

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
    }

    [XmlSchemaProvider("StaticGetSchema")]
    [XmlRoot(ElementName ="AnonElement", Namespace ="http://ElementNs/", IsNullable = true)]
    public class AnonymousType : XmlSerializableBase
    {
        static XmlSchemaType StaticGetSchema(XmlSchemaSet schemas)
        {
            XmlSchemaComplexType schemaType = new XmlSchemaComplexType();
            XmlSchemaSequence sequence = new XmlSchemaSequence();
            schemaType.Particle = sequence;
            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = "MyElement";
            element.SchemaTypeName = new XmlQualifiedName("int", XmlSchema.Namespace);
            sequence.Items.Add(element);
            return schemaType;
        }
    }

    public class NoSchema : XmlSerializableBase
    {
    }

    [XmlRoot]
    public class EmptyXmlRoot : XmlSerializableBase
    {
    }
    
    [XmlRoot(IsNullable=true)]
    public class NullableOnlyXmlRoot : XmlSerializableBase
    {
    }
    
    [XmlRoot(ElementName=null)]
    public class NullElementXmlRoot : XmlSerializableBase
    {
    }
    
    [XmlRoot(ElementName="")]
    public class EmptyElementXmlRoot : XmlSerializableBase
    {
    }

    [XmlSchemaProvider(null, IsAny=true)]
    public class AnyBasic : XmlSerializableBase
    {
    }
    
    [XmlSchemaProvider("StaticGetSchema", IsAny = true)]
    public class AnyWithSchemaTypeMethod : XmlSerializableBase
    {
        static XmlSchemaType StaticGetSchema(XmlSchemaSet schemas)
        {
            return null;
        }
    }

    [XmlSchemaProvider("StaticGetSchema", IsAny = true)]
    public class AnyWithQnameMethod : XmlSerializableBase
    {
        static XmlQualifiedName StaticGetSchema(XmlSchemaSet schemas)
        {
            return null;
        }
    }

    [XmlSchemaProvider("StaticGetSchema")]
    public class AnyImplicitWithSchemaTypeMethod : XmlSerializableBase
    {
        static XmlSchemaType StaticGetSchema(XmlSchemaSet schemas)
        {
            return null;
        }
    }

    [XmlSchemaProvider("StaticGetSchema")]
    public class AnyImplicitWithQnameMethod : XmlSerializableBase
    {
        static XmlQualifiedName StaticGetSchema(XmlSchemaSet schemas)
        {
            return null;
        }
    }

    public class NoSchemaProviderWithSchema : IXmlSerializable
    {
        public XmlSchema GetSchema()
        {
            XmlSchema schema = new XmlSchema();
            schema.Id = this.GetType().Name;
            XmlSchemaElement element = new XmlSchemaElement();
            element.Name = "userElement";
            element.SchemaTypeName = new XmlQualifiedName("int", XmlSchema.Namespace);
            schema.Items.Add(element);
            return schema;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException();
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException();
        }
    }
    
    [XmlSchemaProvider("GetTypedDataSetSchema")]
    [XmlRoot("TypedDataSet", Namespace = "http://datasetns/")]
    public class TypedDataSet : DataSet
    {
        public static System.Xml.Schema.XmlSchemaComplexType GetTypedDataSetSchema(System.Xml.Schema.XmlSchemaSet xs)
        {
            TypedDataSet ds = new TypedDataSet();
            System.Xml.Schema.XmlSchemaComplexType type = new System.Xml.Schema.XmlSchemaComplexType();
            System.Xml.Schema.XmlSchemaSequence sequence = new System.Xml.Schema.XmlSchemaSequence();
            xs.Add(ds.GetSchemaSerializable());
            if (PublishLegacyWSDL())
            {
                System.Xml.Schema.XmlSchemaAny any = new System.Xml.Schema.XmlSchemaAny();
                any.Namespace = ds.Namespace;
                sequence.Items.Add(any);
            }
            else
            {
                System.Xml.Schema.XmlSchemaAny any1 = new System.Xml.Schema.XmlSchemaAny();
                any1.Namespace = "http://www.w3.org/2001/XMLSchema";
                any1.MinOccurs = new System.Decimal(0);
                any1.ProcessContents = System.Xml.Schema.XmlSchemaContentProcessing.Lax;
                sequence.Items.Add(any1);
                System.Xml.Schema.XmlSchemaAny any2 = new System.Xml.Schema.XmlSchemaAny();
                any2.Namespace = "urn:schemas-microsoft-com:xml-diffgram-v1";
                any2.MinOccurs = new System.Decimal(0);
                any2.ProcessContents = System.Xml.Schema.XmlSchemaContentProcessing.Lax;
                sequence.Items.Add(any2);
                sequence.MaxOccurs = System.Decimal.MaxValue;
                System.Xml.Schema.XmlSchemaAttribute attribute = new System.Xml.Schema.XmlSchemaAttribute();
                attribute.Name = "namespace";
                attribute.FixedValue = ds.Namespace;
                type.Attributes.Add(attribute);
            }
            type.Particle = sequence;
            return type;
        }
        protected override System.Xml.Schema.XmlSchema GetSchemaSerializable()
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            this.WriteXmlSchema(new System.Xml.XmlTextWriter(stream, null));
            stream.Position = 0;
            return System.Xml.Schema.XmlSchema.Read(new System.Xml.XmlTextReader(stream), null);
        }
        protected static bool PublishLegacyWSDL()
        {
            //System.Collections.Specialized.NameValueCollection settings = ((System.Collections.Specialized.NameValueCollection)(System.Configuration.ConfigurationManager.GetSection("system.data.dataset")));
            //if ((settings != null))
            //{
            //    string[] values = settings.GetValues("WSDL_VERSION");
            //    if ((values != null))
            //    {
            //        System.Single version = System.Single.Parse(((string)(values[0])), ((System.IFormatProvider)(null)));
            //        return(version < 2);
            //    }
            //}
            return true;
        }
    }

    [Serializable]
    public class IXmlSerializablesContainer
    {
        ComplexType complexType;
        ComplexStruct complexStruct;
        SimpleType simpleType;
        AnonymousType anonymousType;
        NoSchema noSchema;
        XsdInt xsdInt;
        XsdString xsdString;
        DataSet dataSet;
        TypedDataSet typedDataSet;
        AnyBasic anyBasic;
        AnyBasic[] anyArray;
        AnyWithSchemaTypeMethod anyWithSchemaType;
        AnyWithQnameMethod anyWithQname;
    	AnyImplicitWithSchemaTypeMethod anyImplicitWithSchemaType;
        AnyImplicitWithQnameMethod anyImplicitWithQname;
        NoSchemaProviderWithSchema noSchemaProviderWithSchema;
        XmlElement xmlElement;
        XmlElement[] xmlElementArray;
        XmlNode[] xmlNodes;
        XmlNode[][] xmlNodesArray;
        Dictionary<XmlElement, XmlNode[]> xmlElementDictionary;
    }

    [Serializable]
    public class SqlTypeContainer
    {
		// The following were disabled in NetFx test... but should work now.
		// SqlBinary, SqlChars, SqlInt32, SqlString, SqlDateTime, SqlGuid

            public SqlBinary sqlBinary = new SqlBinary(new byte[]{4,2});
            public SqlByte sqlByte = new SqlByte(4);
            public SqlBytes sqlBytes = new SqlBytes(new byte[]{4,2});
            public SqlChars sqlChars = new SqlChars(new char[]{'4', '2'});
            public SqlDecimal sqlDecimal = new SqlDecimal(4.2);
            public SqlDouble sqlDouble = new SqlDouble(4.2);
            public SqlInt16 sqlInt16 = new SqlInt16(42);
            public SqlInt32 sqlInt32 = new SqlInt32(42);
            public SqlInt64 sqlInt64 = new SqlInt64(42L);
            public SqlMoney sqlMoney = new SqlMoney(42);
            public SqlSingle sqlSingle = new SqlSingle(4.2);
            public SqlString sqlString = new SqlString("MySqlString");
            public SqlDateTime sqlDateTime = new SqlDateTime();
            public SqlGuid sqlGuid = new SqlGuid();
    }
}

