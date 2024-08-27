// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace System.Runtime.Serialization.Schema.Tests
{
    internal static class SchemaUtils
    {
        internal static string SerializationNamespace = "http://schemas.microsoft.com/2003/10/Serialization/";

        static XmlWriterSettings writerSettings = new XmlWriterSettings() { Indent = true };

        #region Test Data
        private static string[] _positiveSchemas = new string[] {
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/fooNs' xmlns:tns='http://schemas.datacontract.org/2004/07/fooNs' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='ValidType'><sequence><element name='member' nillable='true' type='string' /></sequence></complexType>
                      <element name='ValidType' nillable='true' type='tns:ValidType' />
                    </schema>",
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/barNs' xmlns:tns='http://schemas.datacontract.org/2004/07/barNs' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='AnotherValidType'><sequence><element name='member' nillable='true' type='string' minOccurs='0' /></sequence></complexType>
                      <element name='AnotherValidType' nillable='true' type='tns:AnotherValidType' />
                    </schema>",
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/Suites.SchemaImport' xmlns:tns='http://schemas.datacontract.org/2004/07/Suites.SchemaImport' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='NonAttributedType'><sequence><element minOccurs='0' name='Length' nillable='true' type='tns:NonAttributedSquare' /></sequence></complexType>
                      <element name='NonAttributedType' nillable='true' type='tns:NonAttributedType' />
                      <complexType name='NonAttributedSquare'><sequence><element minOccurs='0' name='Length' type='int' /></sequence></complexType>
                      <element name='NonAttributedSquare' nillable='true' type='tns:NonAttributedSquare' />
                    </schema>",
            };
        internal static XmlSchemaSet PositiveSchemas => ReadStringsIntoSchemaSet(_positiveSchemas);
        internal static XmlSchemaSet AllPositiveSchemas => ReadStringsIntoSchemaSet(_positiveSchemas).AddStrings(_referenceSchemas);

        private static string[] _referenceSchemas = new string[] {
                    @"<?xml version='1.0' encoding='utf-8'?>
                    <xs:schema xmlns:tns='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.ReferencedTypes' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.ReferencedTypes' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                      <xs:import namespace='http://schemas.microsoft.com/2003/10/Serialization/' />
                      <xs:import namespace='http://schemas.microsoft.com/2003/10/Serialization/Arrays' />
                      <xs:complexType name='NonRefType'>
                        <xs:sequence></xs:sequence>
                        <xs:attribute ref='ser:Id'></xs:attribute>
                        <xs:attribute ref='ser:Ref'></xs:attribute>
                      </xs:complexType>
                    </xs:schema>",
                    @"<?xml version='1.0' encoding='utf-8'?>
                    <xs:schema xmlns:tns='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.ReferencedTypes' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.ReferencedTypes' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                      <xs:import namespace='http://schemas.microsoft.com/2003/10/Serialization/' />
                      <xs:import namespace='http://schemas.microsoft.com/2003/10/Serialization/Arrays' />
                      <xs:complexType name='RefType1'>
                        <xs:sequence>
                        </xs:sequence>
                      </xs:complexType>
                    </xs:schema>",
            };
        internal static XmlSchemaSet ReferenceSchemas => ReadStringsIntoSchemaSet(_referenceSchemas);

        private static string[] _mixedSchemas = new string[] {
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/fooNs' xmlns:tns='http://schemas.datacontract.org/2004/07/fooNs' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='InvalidType'><all /></complexType>
                      <element name='InvalidType' nillable='true' type='tns:InvalidType' />
                      <complexType name='ValidType'><sequence><element name='member' nillable='true' type='string' /></sequence></complexType>
                      <element name='ValidType' nillable='true' type='tns:ValidType' />
                    </schema>",
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/barNs' xmlns:tns='http://schemas.datacontract.org/2004/07/barNs' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='AnotherValidType'><sequence><element name='member' nillable='true' type='string' minOccurs='0' /></sequence></complexType>
                      <element name='AnotherValidType' nillable='true' type='tns:AnotherValidType' />
                    </schema>",
        };
        internal static XmlSchemaSet MixedSchemas => ReadStringsIntoSchemaSet(_mixedSchemas);

        private static string[] _xmlTypeSchemas = new string[] {
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/Suites.SchemaImport' xmlns:tns='http://schemas.datacontract.org/2004/07/Suites.SchemaImport' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='SerializableWithSpecialAttributes'><attribute name='nestedAsAttributeString' type='string' use='optional' /></complexType>
                    </schema>",
            };
        internal static XmlSchemaSet XmlTypeSchemas => ReadStringsIntoSchemaSet(_xmlTypeSchemas);

        internal static string[] NegativeSchemaStrings =
            new string[] {
                    @"", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://EmptySchema' xmlns='http://www.w3.org/2001/XMLSchema'>
                    </schema>", // new XmlQualifiedName("FooType", "http://EmptySchema"),
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://EmptySchema' xmlns='http://www.w3.org/2001/XMLSchema'>
                    </schema>", // new XmlQualifiedName("FooType", "http://NonExistantSchema"),
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='InvalidTopLevelElementType'><sequence/></complexType>
                      <element name='InvalidTopLevelElementType' type='int' nillable='true' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='ExtraAttributesType'><sequence /><attribute name='AdditionalAttribute' type='int' /></complexType>
                      <element name='ExtraAttributesType' type='tns:ExtraAttributesType' nillable='true' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='ExtraAttributeWildcardType'><sequence /><anyAttribute namespace='##other' /></complexType>
                      <element name='ExtraAttributeWildcardType' type='tns:ExtraAttributeWildcardType' nillable='true' />
                    </schema>", // new XmlQualifiedName("ExtraAttributeWildcardType", "http://schemas.datacontract.org/2004/07/foo"),
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='InvalidRootParticleType'><choice minOccurs='0' /></complexType>
                      <element name='InvalidRootParticleType' type='tns:InvalidRootParticleType' nillable='true' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='InvalidTopLevelElement'><sequence /></complexType>
                      <element name='InvalidTopLevelElement' type='string' nillable='true' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='TypeWithElementsOfSameName'><sequence><element name='DuplicatedName' nillable='true' type='string' /><element name='DuplicatedName' nillable='true' type='string' /></sequence></complexType>
                      <element name='TypeWithElementsOfSameName' type='tns:TypeWithElementsOfSameName' nillable='true' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema xmlns:tns='http://schemas.datacontract.org/2004/07/foo' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <simpleType name='SimpleTypeUnion'><union memberTypes='int' /></simpleType>
                      <element name='SimpleTypeUnion' type='tns:SimpleTypeUnion' nillable='true' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema xmlns:tns='http://schemas.datacontract.org/2004/07/foo' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <simpleType name='EnumOnlyList'><list itemType='string' /></simpleType>
                      <element name='EnumOnlyList' type='tns:EnumOnlyList' nillable='true' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema xmlns:tns='http://schemas.datacontract.org/2004/07/foo' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <simpleType name='EnumNonStringBaseType'><list><simpleType><restriction base='int' /></simpleType></list></simpleType>
                      <element name='EnumNonStringBaseType' type='tns:EnumNonStringBaseType' nillable='true' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema xmlns:tns='http://schemas.datacontract.org/2004/07/foo' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <complexType name='ComplexTypeWithSimpleContent'><simpleContent><restriction base='anyType'><simpleType><union memberTypes='int' /></simpleType></restriction></simpleContent></complexType>
                      <element name='ComplexTypeWithSimpleContent' nillable='true' type='tns:ComplexTypeWithSimpleContent' />
                    </schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><xs:schema xmlns:tns='http://schemas.datacontract.org/2004/07/foo' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                      <xs:complexType name='ArrayOfBar'><xs:sequence><xs:element form='unqualified' maxOccurs='unbounded' minOccurs='0' name='Bar' nillable='true' type='tns:Bar' /></xs:sequence></xs:complexType>
                      <xs:element name='ArrayOfBar' nillable='true' type='tns:ArrayOfBar' />
                      <xs:complexType name='Bar' />
                      <xs:element name='Bar' nillable='true' type='tns:Bar' />
                    </xs:schema>", // null
                    @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://tempuri.org/' xmlns:tns='http://tempuri.org/' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                      <element name='DataSet'><complexType><sequence><element minOccurs='0' name='dataset'><complexType><sequence><any namespace='http://tempuri.org/TestDataSet.xsd' /></sequence></complexType></element></sequence></complexType></element>
                    </schema>", // null
        };

        internal static (bool expectedResult, bool isElement, XmlQualifiedName[] qnames, string schemaString)[] CanImportTests = new (bool, bool, XmlQualifiedName[], string)[] {
            (false, false, new XmlQualifiedName[] { new XmlQualifiedName("InvalidTopLevelElementType", "http://schemas.datacontract.org/2004/07/foo") },
                @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                  <complexType name='InvalidTopLevelElementType' />
                  <element name='InvalidTopLevelElementType' type='tns:ValidType' />
                  <complexType name='ValidType'><sequence><element name='member' nillable='true' type='string' /></sequence></complexType>
                </schema>"),
            (true, false, new XmlQualifiedName[] { new XmlQualifiedName("ValidType", "http://schemas.datacontract.org/2004/07/foo") },
                @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                  <complexType name='InvalidType'><attribute name='j' type='int' /></complexType>
                  <complexType name='ValidType'><sequence><element name='member' nillable='true' type='string' /></sequence></complexType>
                  <element name='ValidType' nillable='true' type='tns:ValidType' />
                </schema>"),
            (true, false, new XmlQualifiedName[] {
                new XmlQualifiedName("Address", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes"),
                new XmlQualifiedName("Person", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes"),
                new XmlQualifiedName("ArrayOfAddress", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes"),
                new XmlQualifiedName("ArrayOfArrayOfAddress", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes"),
                },
                @"<?xml version='1.0' encoding='utf-8'?><schema xmlns:tns='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes' xmlns='http://www.w3.org/2001/XMLSchema'>
                  <complexType name='Address'><sequence><element name='city' nillable='true' type='string' /><element name='state' nillable='true' type='string' /><element name='street' nillable='true' type='string' /><element name='zip' type='int' /></sequence></complexType>
                  <element name='Address' nillable='true' type='tns:Address' />
                  <complexType name='Person'><sequence><element name='address' nillable='true' type='tns:Address' /><element name='age' type='int' /><element name='name' nillable='true' type='string' /></sequence></complexType>
                  <element name='Person' nillable='true' type='tns:Person' />
                  <complexType name='ArrayOfAddress'><sequence><element minOccurs='0' maxOccurs='unbounded' name='Address' nillable='true' type='tns:Address' /></sequence></complexType>
                  <element name='ArrayOfAddress' nillable='true' type='tns:ArrayOfAddress' />
                  <complexType name='ArrayOfArrayOfAddress'><sequence><element minOccurs='0' maxOccurs='unbounded' name='ArrayOfAddress' nillable='true' type='tns:ArrayOfAddress' /></sequence></complexType>
                  <element name='ArrayOfArrayOfAddress' nillable='true' type='tns:ArrayOfArrayOfAddress' />
                </schema>"),
            (true, false, null,
                @"<?xml version='1.0' encoding='utf-8'?><schema xmlns:tns='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes' xmlns='http://www.w3.org/2001/XMLSchema'>
                  <complexType name='Address'><sequence><element name='city' nillable='true' type='string' /><element name='state' nillable='true' type='string' /><element name='street' nillable='true' type='string' /><element name='zip' type='int' /></sequence></complexType>
                  <complexType name='Person'><sequence><element name='address' nillable='true' type='tns:Address' /><element name='age' type='int' /><element name='name' nillable='true' type='string' /></sequence></complexType>
                  <element name='Person' nillable='true' type='tns:Person' />
                </schema>"),
            (true, false, null,
                @"<?xml version='1.0' encoding='utf-8'?><schema xmlns:tns='http://schemas.datacontract.org/2004/07/foo' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                  <simpleType name='ValidEnum'><list><simpleType><restriction base='string'><enumeration value='Primary' /><enumeration value='Secondary' /><enumeration value='Graduate' /><enumeration value='PostGraduate' /></restriction></simpleType></list></simpleType>
                  <element name='ValidEnum' nillable='true' type='tns:ValidEnum' />
                </schema>"),
            (false, false, new XmlQualifiedName[] { new XmlQualifiedName("TypeWithExtraAttributes", "http://schemas.datacontract.org/2004/07/foo") },
                @"<?xml version='1.0' encoding='utf-8'?><schema elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/foo' xmlns:tns='http://schemas.datacontract.org/2004/07/foo' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns='http://www.w3.org/2001/XMLSchema'>
                  <complexType name='TypeWithExtraAttributes'><sequence><element name='i' type='int' /></sequence><attribute name='j' type='int' /></complexType>
                  <element name='TypeWithExtraAttributes' nillable='true' type='tns:TypeWithExtraAttributes' />
                </schema>"),
            (true, true, new XmlQualifiedName[] { new XmlQualifiedName("Address", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes") },
                @"<?xml version='1.0' encoding='utf-8'?><schema xmlns:tns='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes' xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' elementFormDefault='qualified' targetNamespace='http://schemas.datacontract.org/2004/07/Suites.SchemaImport.Classes' xmlns='http://www.w3.org/2001/XMLSchema'>
                  <import namespace='http://schemas.microsoft.com/2003/10/Serialization/' />
                  <complexType name='Address'><sequence><element name='city' nillable='true' type='string' /><element name='state' nillable='true' type='string' /><element name='street' nillable='true' type='string' /><element name='zip' type='int' /></sequence></complexType>
                  <element name='Address' nillable='true' type='tns:Address' />
                  <complexType name='Person'><sequence><element name='address' nillable='true' type='tns:Address' /><element name='age' type='int' /><element name='name' nillable='true' type='string' /></sequence></complexType>
                  <element name='Person' nillable='true' type='tns:Person' />
                  <complexType name='TypeWithExtraAttributes'><sequence><element name='i' type='int' /></sequence><attribute name='j' type='int' /></complexType>
                  <element name='TypeWithExtraAttributes' nillable='true' type='tns:TypeWithExtraAttributes' />
                </schema>"),
        };

        internal static XmlQualifiedName[] ValidTypeNames = new XmlQualifiedName[] {
                new XmlQualifiedName("ValidType", "http://schemas.datacontract.org/2004/07/fooNs"),
                new XmlQualifiedName("AnotherValidType", "http://schemas.datacontract.org/2004/07/barNs"),
                new XmlQualifiedName("NonAttributedType", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport"),
                new XmlQualifiedName("NonRefType", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport.ReferencedTypes"),
                new XmlQualifiedName("RefType1", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport.ReferencedTypes"),
            };

        internal static XmlQualifiedName[] XmlTypeNames = new XmlQualifiedName[] {
                new XmlQualifiedName("SerializableWithSpecialAttributes", "http://schemas.datacontract.org/2004/07/Suites.SchemaImport"),
            };

        internal static XmlQualifiedName[] InvalidTypeNames = new XmlQualifiedName[] {
                new XmlQualifiedName("InvalidType", "http://schemas.datacontract.org/2004/07/fooNs"),
            };

        // These correspond with the set in 'NegativeSchemaStrings'
        internal static XmlQualifiedName[] NegativeTypeNames = new XmlQualifiedName[] {
                null,
                new XmlQualifiedName("FooType", "http://EmptySchema"),
                new XmlQualifiedName("FooType", "http://NonExistantSchema"),
                null,
                null,
                new XmlQualifiedName("ExtraAttributeWildcardType", "http://schemas.datacontract.org/2004/07/foo"),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
            };

        internal static string GlobalSchema = @"<xs:schema targetNamespace='http://myns/'  xmlns:tns='http://myns/' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
              <xs:element name='MyGlobalElement' />
              <xs:attribute name='MyGlobalAttribute' />
            </xs:schema>";
        #endregion


        internal static XsdDataContractImporter CreateImporterWithOptions(ImportOptions opts = null)
        {
            XsdDataContractImporter importer = new XsdDataContractImporter();
            importer.Options = opts ?? new ImportOptions();
            return importer;
        }

        internal static string DumpCode(CodeCompileUnit ccu, CodeDomProvider provider = null)
        {
            provider ??= CodeDomProvider.CreateProvider("csharp");

            CodeGeneratorOptions options = new CodeGeneratorOptions()
            {
                BlankLinesBetweenMembers = true,
                BracingStyle = "C",
            };

            StringWriter sw = new StringWriter();
            provider.GenerateCodeFromCompileUnit(ccu, sw, options);
            return sw.ToString();
        }

        public static string DumpSchema(XmlSchemaSet schemas)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            foreach (XmlSchema schema in schemas.Schemas())
            {
                if (schema.TargetNamespace != SerializationNamespace)
                {
                    schema.Write(sw);
                }
                sw.WriteLine();
            }
            sw.Flush();
            return sb.ToString();
        }

        internal static XmlSchema GetSchema(XmlSchemaSet schemaSet, string targetNs)
        {
            XmlSchema schema = null;
            foreach (XmlSchema ctSchema in schemaSet.Schemas())
            {
                if (ctSchema.TargetNamespace == targetNs)
                {
                    schema = ctSchema;
                    break;
                }
            }
            return schema;
        }

        internal static XmlSchemaElement GetSchemaElement(XmlSchemaSet schemaSet, XmlQualifiedName qname)
        {
            foreach (XmlSchema schema in schemaSet.Schemas(qname.Namespace))
            {
                XmlSchemaElement schemaElement = (XmlSchemaElement)schema.Elements[qname];
                if (schemaElement != null)
                    return schemaElement;
            }
            throw new Exception(String.Format("Element {0} is not found", qname));
        }

        internal static string GetSchemaString(XmlSchemaSet schemaSet, string targetNs)
        {
            XmlSchema schema = GetSchema(schemaSet, targetNs);
            StringWriter stringWriter = new StringWriter();
            XmlWriter xmlWriter = XmlWriter.Create(stringWriter, writerSettings);
            schema.Write(xmlWriter);
            xmlWriter.Flush();
            return stringWriter.ToString();
        }

        internal static void SetSchemaString(XmlSchemaSet schemaSet, string targetNs, string schemaString)
        {
            XmlSchema schema = null;
            foreach (XmlSchema ctSchema in schemaSet.Schemas())
            {
                if (ctSchema.TargetNamespace == targetNs)
                {
                    schema = ctSchema;
                    break;
                }
            }
            schemaSet.Remove(schema);
            schema = XmlSchema.Read(new StringReader(schemaString), null);
            schemaSet.Add(schema);
        }

        internal static string GetString(CodeTypeReference typeReference)
        {
            if (typeReference.ArrayRank > 0)
            {
                CodeTypeReference arrayType = typeReference;
                string arrayString = String.Empty;
                for (; ; )
                {
                    int rank = typeReference.ArrayRank;
                    arrayString += "[";
                    for (int r = 1; r < rank; r++)
                        arrayString += ",";
                    arrayString += "]";

                    typeReference = typeReference.ArrayElementType;
                    if (typeReference.ArrayRank == 0)
                        break;
                }
                return String.Format("Array of {0}{1}", typeReference.BaseType, arrayString);
            }
            else
                return typeReference.BaseType;
        }

        internal static string InsertAttribute(string xml, string xpath, string prefix, string localName, string ns, string value)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(XmlReader.Create(new StringReader(xml)));
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("xs", XmlSchema.Namespace);
            XmlElement xmlElement = (XmlElement)xmlDoc.SelectSingleNode(xpath, nsMgr);
            XmlAttribute xmlAttribute = xmlDoc.CreateAttribute(prefix, localName, ns);
            xmlAttribute.Value = value;
            xmlElement.Attributes.Append(xmlAttribute);

            StringWriter stringWriter = new StringWriter();
            xmlDoc.Save(XmlWriter.Create(stringWriter, writerSettings));
            return stringWriter.ToString();
        }

        internal static string InsertElement(string xml, string xpath, string xmlFrag, bool insertAfter)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(XmlReader.Create(new StringReader(xml)));
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("xs", XmlSchema.Namespace);
            XmlNode xmlNode = xmlDoc.SelectSingleNode(xpath, nsMgr);
            if (insertAfter)
                xmlNode.ParentNode.InsertAfter(xmlDoc.ReadNode(XmlReader.Create(new StringReader(xmlFrag))), xmlNode);
            else
                xmlNode.ParentNode.InsertBefore(xmlDoc.ReadNode(XmlReader.Create(new StringReader(xmlFrag))), xmlNode);

            StringWriter stringWriter = new StringWriter();
            xmlDoc.Save(XmlWriter.Create(stringWriter, writerSettings));
            return stringWriter.ToString();
        }


        internal static XmlSchemaSet ReadStringsIntoSchemaSet(params string[] schemaStrings)
        {
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            return schemaSet.AddStrings(schemaStrings);
        }

        internal static XmlSchemaSet AddStrings(this XmlSchemaSet schemaSet, params string[] schemaStrings)
        {
            foreach (string schemaString in schemaStrings)
            {
                StringReader reader = new StringReader(schemaString);
                XmlSchema schema = XmlSchema.Read(reader, null);
                if (schema == null)
                    throw new Exception("Could not read schema");
                schemaSet.Add(schema);
            }
            return schemaSet;
        }
    }
}
