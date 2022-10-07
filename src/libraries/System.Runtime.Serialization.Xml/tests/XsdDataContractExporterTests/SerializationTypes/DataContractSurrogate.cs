using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

#if UseSeparateAssemblyNamespace
namespace SerializableTypes.XsdDataContractExporterTests
#else
namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
#endif
{
    public class DataContractSurrogate
    {
        [DataContract]
        public class CircleContainer
        {
          [DataMember]
          Circle circle;
          [DataMember]
          public Circle[] Circles{ get { return null;} set {}}
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
          public XmlSerializerPerson(){}
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
            schemas.Compile(new ValidationEventHandler (ValidationCallbackWithErrorCode), true);
            for (int i = 0; i < schemas.Count; i++)
            {
              XmlSchema schema = schemas[i];
              schemaSet.Add(schema);
            }
            return new XmlQualifiedName(xmlTypeMapping.TypeName, xmlTypeMapping.Namespace);
          }
          
          private static void ValidationCallbackWithErrorCode (object sender, ValidationEventArgs args) {
            Console.WriteLine("Schema warning: " + args.Message);
          }
        }
    }
}

