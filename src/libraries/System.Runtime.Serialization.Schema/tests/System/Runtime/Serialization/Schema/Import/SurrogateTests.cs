// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Serialization.Schema;
using System.Runtime.Serialization.Schema.Tests.DataContracts;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace System.Runtime.Serialization.Schema.Tests
{
    // TODO - Add a test covering 'ISerializationCodeDomSurrogateProvider'/ProcessImportedType - There was nothing in NetFx test suites for this.
    public class SurrogateTests
    {
        static Type[] testTypes = new Type[]
        {
          typeof(CircleContainer),
          typeof(Node),
          typeof(XmlSerializerPerson),
        };

        private readonly ITestOutputHelper _output;
        public SurrogateTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void DefaultScenario()
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            exporter.Options = new ExportOptions();
            exporter.Options.DataContractSurrogate = new SurrogateProvider(false);
            for (int i = 0; i < testTypes.Length; i++)
                exporter.Export((Type)testTypes[i]);

            XsdDataContractImporter importer = new XsdDataContractImporter();
            importer.Options = new ImportOptions();
            importer.Options.DataContractSurrogate = exporter.Options.DataContractSurrogate;
            importer.Options.ImportXmlType = true;
            importer.Import(exporter.Schemas);

            string code = SchemaUtils.DumpCode(importer.CodeCompileUnit);
            _output.WriteLine(code);

            Assert.Contains(@"[assembly: System.Runtime.Serialization.ContractNamespaceAttribute(""http://special1.tempuri.org"", ClrNamespace=""special1.tempuri.org"")]", code);
            Assert.Contains(@"[assembly: System.Runtime.Serialization.ContractNamespaceAttribute("""", ClrNamespace="""")]", code);

            Assert.Contains(@"namespace special1.tempuri.org", code);
            Assert.Matches(@"\[System.Runtime.Serialization.DataContractAttribute\(Name=""CircleContainer"", Namespace=""http://special1.tempuri.org""\)\]\s*public partial class CircleContainer : object, System.Runtime.Serialization.IExtensibleDataObject", code);
            Assert.Contains(@"private System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableSquare[] circlesField;", code);
            Assert.Contains(@"private System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableSquare CircleField;", code);
            Assert.Matches(@"\[System.Runtime.Serialization.DataMemberAttribute\(\)\]\s*public System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableSquare Circle", code);
            Assert.Contains(@"public partial class SerializableSquare : object, System.Runtime.Serialization.IExtensibleDataObject", code);

            Assert.Contains(@"namespace System.Runtime.Serialization.Schema.Tests.DataContracts", code);
            Assert.Matches(@"\[System.Runtime.Serialization.DataContractAttribute\(Name\s*=\s*""SerializableNode"", Namespace\s*=\s*\(""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests""\s*\+\s*"".DataContracts""\)\)\]\s*public partial class SerializableNode : object, System.Runtime.Serialization.IExtensibleDataObject", code);
            Assert.Matches(@"\[System.Xml.Serialization.XmlSchemaProviderAttribute\(""ExportSchema""\)\]\s*\[System.Xml.Serialization.XmlRootAttribute\(ElementName\s*=\s*""XmlSerializerPersonElement"", Namespace\s*=\s*""""\)\]\s*public partial class XmlSerializerPerson : object, System.Xml.Serialization.IXmlSerializable", code);
        }

        [Fact]
        public void WithReferencedType()
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            exporter.Options = new ExportOptions();
            exporter.Options.DataContractSurrogate = new SurrogateProvider(false);
            for (int i = 0; i < testTypes.Length; i++)
                exporter.Export((Type)testTypes[i]);

            XsdDataContractImporter importer = new XsdDataContractImporter();
            importer.Options = new ImportOptions();
            importer.Options.DataContractSurrogate = exporter.Options.DataContractSurrogate;
            importer.Options.ImportXmlType = true;
            importer.Options.ReferencedTypes.Add(typeof(SerializableCircle));
            importer.Import(exporter.Schemas);

            string code = SchemaUtils.DumpCode(importer.CodeCompileUnit);
            _output.WriteLine(code);

            Assert.Contains(@"[assembly: System.Runtime.Serialization.ContractNamespaceAttribute(""http://special1.tempuri.org"", ClrNamespace=""special1.tempuri.org"")]", code);
            Assert.Contains(@"[assembly: System.Runtime.Serialization.ContractNamespaceAttribute("""", ClrNamespace="""")]", code);

            Assert.Contains(@"namespace special1.tempuri.org", code);
            Assert.Matches(@"\[System.Runtime.Serialization.DataContractAttribute\(Name=""CircleContainer"", Namespace=""http://special1.tempuri.org""\)\]\s*public partial class CircleContainer : object, System.Runtime.Serialization.IExtensibleDataObject", code);
            Assert.Contains(@"private System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableSquare[] circlesField;", code);
            Assert.Contains(@"private System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableCircle CircleField;", code);
            Assert.Matches(@"\[System.Runtime.Serialization.DataMemberAttribute\(\)\]\s*public System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableCircle Circle", code);

            Assert.Contains(@"namespace System.Runtime.Serialization.Schema.Tests.DataContracts", code);
            Assert.Matches(@"\[System.Runtime.Serialization.DataContractAttribute\(Name\s*=\s*""SerializableNode"", Namespace\s*=\s*\(""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests""\s*\+\s*"".DataContracts""\)\)\]\s*public partial class SerializableNode : object, System.Runtime.Serialization.IExtensibleDataObject", code);
            Assert.Matches(@"\[System.Xml.Serialization.XmlSchemaProviderAttribute\(""ExportSchema""\)\]\s*\[System.Xml.Serialization.XmlRootAttribute\(ElementName\s*=\s*""XmlSerializerPersonElement"", Namespace\s*=\s*""""\)\]\s*public partial class XmlSerializerPerson : object, System.Xml.Serialization.IXmlSerializable", code);
            Assert.DoesNotContain(@"public partial class SerializableSquare : object, System.Runtime.Serialization.IExtensibleDataObject", code);
        }

        [Fact]
        public void WithSurrogateBinding()
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            exporter.Options = new ExportOptions();
            exporter.Options.DataContractSurrogate = new SurrogateProvider(true);
            for (int i = 0; i < testTypes.Length; i++)
                exporter.Export((Type)testTypes[i]);

            XsdDataContractImporter importer = new XsdDataContractImporter();
            importer = new XsdDataContractImporter();
            importer.Options = new ImportOptions();
            importer.Options.DataContractSurrogate = exporter.Options.DataContractSurrogate;
            importer.Options.ImportXmlType = true;
            importer.Options.ReferencedTypes.Add(typeof(Circle));
            importer.Import(exporter.Schemas);

            string code = SchemaUtils.DumpCode(importer.CodeCompileUnit);
            _output.WriteLine(code);

            Assert.Contains(@"[assembly: System.Runtime.Serialization.ContractNamespaceAttribute(""http://special1.tempuri.org"", ClrNamespace=""special1.tempuri.org"")]", code);
            Assert.DoesNotContain(@"[assembly: System.Runtime.Serialization.ContractNamespaceAttribute("""", ClrNamespace="""")]", code);

            Assert.Contains(@"namespace special1.tempuri.org", code);
            Assert.Matches(@"\[System.Runtime.Serialization.DataContractAttribute\(Name=""CircleContainer"", Namespace=""http://special1.tempuri.org""\)\]\s*public partial class CircleContainer : object, System.Runtime.Serialization.IExtensibleDataObject", code);
            Assert.Contains(@"private System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableCircle[] circlesField;", code);
            Assert.Contains(@"private System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableCircle CircleField;", code);
            Assert.Matches(@"\[System.Runtime.Serialization.DataMemberAttribute\(\)\]\s*public System.Runtime.Serialization.Schema.Tests.DataContracts.SerializableCircle Circle", code);

            Assert.DoesNotContain(@"namespace System.Runtime.Serialization.Schema.Tests.DataContracts", code);
            Assert.DoesNotContain(@"class SerializableSquare", code);
            Assert.DoesNotContain(@"class SerializableNode", code);
            Assert.DoesNotContain(@"class XmlSerializerPerson", code);
        }
    }

    internal class SurrogateProvider : ISerializationSurrogateProvider2
    {
        static XmlQualifiedName s_circleList = new XsdDataContractExporter().GetSchemaTypeName(typeof(SerializableCircle[]));
        static XmlQualifiedName s_square = new XsdDataContractExporter().GetSchemaTypeName(typeof(SerializableSquare));
        static XmlQualifiedName s_serializableNode = new XsdDataContractExporter().GetSchemaTypeName(typeof(SerializableNode));
        static XmlQualifiedName s_xmlSerializerPersonAdapter = new XsdDataContractExporter().GetSchemaTypeName(typeof(XmlSerializerAdapter<XmlSerializerPerson>));

        bool _surrogateBinding;
        public SurrogateProvider(bool surrogateBinding) { _surrogateBinding = surrogateBinding; }

        public object? GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType) => memberInfo.MemberType.ToString();
        public object? GetCustomDataToExport(Type runtimeType, Type dataContractType) => runtimeType.Name;
        public object GetDeserializedObject(object obj, Type targetType) => throw new NotImplementedException();
        public void GetKnownCustomDataTypes(Collection<Type> customDataTypes) { }
        public object GetObjectToSerialize(object obj, Type targetType) => throw new NotImplementedException();
        public Type GetSurrogateType(Type type) // Formerly Known As GetDataContractType(Type)... but this was in the first interface, so we can't change this name.
        {
            if (type == typeof(Node))
                return typeof(SerializableNode);
            if (type == typeof(SerializableCircle))
                return typeof(SerializableSquare);
            if (type == typeof(XmlSerializerPerson))
                return typeof(XmlSerializerAdapter<XmlSerializerPerson>);
            return type;
        }
        public Type? GetReferencedTypeOnImport(string name, string ns, object? customData)
        {
            if (!_surrogateBinding)
            {
                // Collection item and type name mismatch must be handled by surrogate to avoid exception
                return (name == s_circleList.Name && ns == s_circleList.Namespace) ? typeof(SerializableSquare[]) : null;
            }

            if (name == s_square.Name && ns == s_square.Namespace)
                return typeof(SerializableCircle);
            if (name == s_circleList.Name && ns == s_circleList.Namespace)
                return typeof(SerializableCircle[]);
            if (name == s_serializableNode.Name && ns == s_serializableNode.Namespace)
                return typeof(Node);
            if (name == s_xmlSerializerPersonAdapter.Name && ns == s_xmlSerializerPersonAdapter.Namespace)
                return typeof(XmlSerializerPerson);
            return null;
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
    }
}
