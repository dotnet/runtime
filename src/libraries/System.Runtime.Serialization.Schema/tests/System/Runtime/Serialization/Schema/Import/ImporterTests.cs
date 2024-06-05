// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Schema;
using System.Runtime.Serialization.Schema.Tests.DataContracts;
using System.Xml;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Runtime.Serialization.Schema.Tests
{
    public class ImporterTests
    {
        private readonly ITestOutputHelper _output;
        public ImporterTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Ctor_Default()
        {
            XsdDataContractImporter xci = new XsdDataContractImporter();
            Assert.NotNull(xci);
            Assert.Null(xci.Options);
        }

        [Fact]
        public void Ctor_CCU()
        {
            CodeCompileUnit ccu = new CodeCompileUnit();
            XsdDataContractImporter xci = new XsdDataContractImporter(ccu);
            Assert.NotNull(xci);
            Assert.Equal(ccu, xci.CodeCompileUnit);
        }

        [Theory]
        [MemberData(nameof(CanImport_MemberData))]
        public void CanImport(bool expectedResult, Func<XsdDataContractImporter, bool> canImport, Type expectedExceptionType = null, string msg = null)
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            if (expectedExceptionType == null)
            {
                Assert.Equal(expectedResult, canImport(importer));
            }
            else
            {
                var ex = Assert.Throws(expectedExceptionType, () => canImport(importer));

                if (!string.IsNullOrEmpty(msg))
                    Assert.Equal(msg, ex.Message);
            }
        }
        public static IEnumerable<object[]> CanImport_MemberData()
        {
            // CanImport(XmlSchemaSet)
            yield return new object[] { true, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.PositiveSchemas) };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'schemas')" };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.MixedSchemas) };

            // CanImport(XmlSchemaSet, ICollection<XmlQualifiedName>)
            yield return new object[] { true, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.PositiveSchemas, new XmlQualifiedName[] { SchemaUtils.ValidTypeNames[0] }) };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(null, SchemaUtils.InvalidTypeNames), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'schemas')" };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.MixedSchemas, (ICollection<XmlQualifiedName>)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'typeNames')" };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.MixedSchemas, new XmlQualifiedName[] { null }), typeof(ArgumentException), @"Cannot import type for null XmlQualifiedName specified via parameter." };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.MixedSchemas, SchemaUtils.InvalidTypeNames) };

            // CanImport(XmlSchemaSet, XmlQualifiedName)
            yield return new object[] { true, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0]) };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(null, SchemaUtils.InvalidTypeNames[0]), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'schemas')" };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.MixedSchemas, (XmlQualifiedName)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'typeName')" };
            yield return new object[] { false, (XsdDataContractImporter imp) => imp.CanImport(SchemaUtils.MixedSchemas, SchemaUtils.InvalidTypeNames[0]) };

            // CanImport(XmlSchemaSet, XmlSchemaElement)
            // TODO

            // CanImportTests.cs
            foreach (var citArgs in SchemaUtils.CanImportTests)
            {
                XmlSchemaSet schemaSet = SchemaUtils.ReadStringsIntoSchemaSet(citArgs.schemaString);
                if (citArgs.qnames == null)
                    yield return new object[] { citArgs.expectedResult, (XsdDataContractImporter imp) => imp.CanImport(schemaSet) };
                else if (citArgs.qnames.Length == 1 && citArgs.isElement)
                    yield return new object[] { citArgs.expectedResult, (XsdDataContractImporter imp) => imp.CanImport(schemaSet, SchemaUtils.GetSchemaElement(schemaSet, citArgs.qnames[0])) };
                else if (citArgs.qnames.Length == 1)
                    yield return new object[] { citArgs.expectedResult, (XsdDataContractImporter imp) => imp.CanImport(schemaSet, citArgs.qnames[0]) };
                else
                    yield return new object[] { citArgs.expectedResult, (XsdDataContractImporter imp) => imp.CanImport(schemaSet, citArgs.qnames) };
            }
        }

        [Theory]
        [MemberData(nameof(Import_MemberData))]
        public void Import(Action<XsdDataContractImporter> import, int codeLength = -1)
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            import(importer);
            string code = SchemaUtils.DumpCode(importer.CodeCompileUnit);
            _output.WriteLine(code);
            if (codeLength >= 0)
                Assert.Equal(codeLength, code.Length);
        }
        public static IEnumerable<object[]> Import_MemberData()
        {
            int newlineSize = Environment.NewLine.Length;

            // Import(XmlSchemaSet)
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.PositiveSchemas), 5060 + (168 * newlineSize) }; // 168 lines
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReferenceSchemas), 2059 + (56 * newlineSize) }; // 56 lines
            yield return new object[] { (XsdDataContractImporter imp) => { imp.Options.ImportXmlType = true; imp.Import(SchemaUtils.XmlTypeSchemas); }, 2127 + (58 * newlineSize) }; // 58 lines

            // Import(XmlSchemaSet, ICollection<XmlQualifiedName>)
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.AllPositiveSchemas, SchemaUtils.ValidTypeNames), 6770 + (215 * newlineSize) }; // 215 lines
            yield return new object[] { (XsdDataContractImporter imp) => { imp.Options.ImportXmlType = true; imp.Import(SchemaUtils.XmlTypeSchemas, SchemaUtils.XmlTypeNames); }, 2127 + (58 * newlineSize) }; // 58 lines

            // Import(XmlSchemaSet, XmlQualifiedName)
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0]), 1515 + (50 * newlineSize) }; // 50 lines
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[1]), 1514 + (50 * newlineSize) }; // 50 lines
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[2]), 2729 + (86 * newlineSize) }; // 86 lines
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReferenceSchemas, SchemaUtils.ValidTypeNames[3]), 1260 + (35 * newlineSize) }; // 35 lines
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReferenceSchemas, SchemaUtils.ValidTypeNames[4]), 1238 + (35 * newlineSize) }; // 35 lines

            // Import(XmlSchemaSet, XmlSchemaElement)
            // TODO

            // From CanImportTests.cs
            foreach (var citArgs in SchemaUtils.CanImportTests)
            {
                if (citArgs.expectedResult)
                {
                    XmlSchemaSet schemaSet = SchemaUtils.ReadStringsIntoSchemaSet(citArgs.schemaString);
                    if (citArgs.qnames == null)
                        yield return new object[] { (XsdDataContractImporter imp) => imp.Import(schemaSet) };
                    else if (citArgs.qnames.Length == 1 && citArgs.isElement)
                        yield return new object[] { (XsdDataContractImporter imp) => { imp.Import(schemaSet, SchemaUtils.GetSchemaElement(schemaSet, citArgs.qnames[0])); } };
                    else if (citArgs.qnames.Length == 1 && !citArgs.isElement)
                        yield return new object[] { (XsdDataContractImporter imp) => imp.Import(schemaSet, citArgs.qnames[0]) };
                    else
                        yield return new object[] { (XsdDataContractImporter imp) => imp.Import(schemaSet, citArgs.qnames) };
                }
            }

            // From FormatVersioning.cs : Positive tests
            (string msg, Type type, string xpath, string xmlFrag)[] formatVersioningArgs = new (string, Type, string, string)[] {
                  ("Optional Serialization Attribute in class",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ImporterTests.SerializableFormatClass']/xs:sequence", @"<xs:attribute ref='ser:V2Attribute'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>"),
                  ("Optional Serialization Attribute in ISerializable",
                      typeof(ISerializableFormatClass), @"//xs:schema/xs:complexType[@name='ISerializableFormatClass']/xs:sequence", @"<xs:attribute ref='ser:V2Attribute'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>"),
                  ("Optional Serialization Attribute in Array",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ArrayOfImporterTests.SerializableFormatClass']/xs:sequence", @"<xs:attribute ref='ser:V2Attribute'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>"),
                  ("Optional Serialization Element in class",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ImporterTests.SerializableFormatClass']/xs:sequence/xs:element", @"<xs:element ref='ser:V2Element' minOccurs='0'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>"),
                  ("Optional Serialization Element in ISerializable",
                      typeof(ISerializableFormatClass), @"//xs:schema/xs:complexType[@name='ISerializableFormatClass']/xs:sequence/xs:any", @"<xs:element ref='ser:V2Element' minOccurs='0'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>"),
                  ("Optional Serialization Element in Array",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ArrayOfImporterTests.SerializableFormatClass']/xs:sequence/xs:element", @"<xs:element ref='ser:V2Element' minOccurs='0'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>"),
            };
            foreach (var fvArg in formatVersioningArgs)
            {
                (XmlSchemaSet schemaSet, XmlQualifiedName typeName) = PrepareFormatVersioningTest(fvArg.type, fvArg.xpath, fvArg.xmlFrag);
                yield return new object[] { (XsdDataContractImporter imp) => imp.Import(schemaSet, typeName) };
            }
        }
        static (XmlSchemaSet, XmlQualifiedName) PrepareFormatVersioningTest(Type type, string xpath, string xmlFrag)
        {
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            exporter.Export(type);
            XmlSchemaSet schemaSet = exporter.Schemas;
            XmlQualifiedName typeName = exporter.GetSchemaTypeName(type);
            string schemaString = SchemaUtils.GetSchemaString(schemaSet, typeName.Namespace);
            schemaString = SchemaUtils.InsertElement(schemaString, xpath, xmlFrag, true);
            schemaString = SchemaUtils.InsertElement(schemaString, @"//xs:schema/xs:complexType", @"<xs:import namespace='http://myns/' xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", false);
            schemaString = SchemaUtils.InsertElement(schemaString, @"//xs:schema/xs:complexType", @"<xs:import namespace='http://schemas.microsoft.com/2003/10/Serialization/' xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", false);
            schemaString = SchemaUtils.InsertAttribute(schemaString, @"//xs:schema", "xmlns", @"ser", @"http://www.w3.org/2000/xmlns/", "http://schemas.microsoft.com/2003/10/Serialization/");
            SchemaUtils.SetSchemaString(schemaSet, typeName.Namespace, schemaString);
            schemaSet.Add(XmlSchema.Read(new StringReader(SchemaUtils.GlobalSchema), null));

            XmlSchema v2SerializationSchema = SchemaUtils.GetSchema(schemaSet, "http://schemas.microsoft.com/2003/10/Serialization/");
            XmlSchemaElement v2Element = new XmlSchemaElement();
            v2Element.Name = "V2Element";
            v2SerializationSchema.Items.Add(v2Element);
            XmlSchemaAttribute v2Attribute = new XmlSchemaAttribute();
            v2Attribute.Name = "V2Attribute";
            v2SerializationSchema.Items.Add(v2Attribute);
            schemaSet.Reprocess(v2SerializationSchema);

            return (schemaSet, typeName);
        }

        [Theory]
        [MemberData(nameof(Import_NegativeCases_MemberData))]
        public void Import_NegativeCases(Action<XsdDataContractImporter> import, Type expectedExceptionType, string msg = null)
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();
            var ex = Assert.Throws(expectedExceptionType, () => import(importer));

            if (!string.IsNullOrEmpty(msg))
                Assert.Equal(msg, ex.Message);
        }
        public static IEnumerable<object[]> Import_NegativeCases_MemberData()
        {
            // Import(XmlSchemaSet)
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'schemas')" };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.MixedSchemas), typeof(InvalidDataContractException), @"Type 'InvalidType' in namespace 'http://schemas.datacontract.org/2004/07/fooNs' cannot be imported. The root particle must be a sequence. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };

            // Import(XmlSchemaSet, ICollection<XmlQualifiedName>)
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(null, SchemaUtils.InvalidTypeNames), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'schemas')" };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.MixedSchemas, (ICollection<XmlQualifiedName>)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'typeNames')" };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.MixedSchemas, new XmlQualifiedName[] { null }), typeof(ArgumentException), @"Cannot import type for null XmlQualifiedName specified via parameter." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.MixedSchemas, SchemaUtils.InvalidTypeNames), typeof(InvalidDataContractException), @"Type 'InvalidType' in namespace 'http://schemas.datacontract.org/2004/07/fooNs' cannot be imported. The root particle must be a sequence. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };

            // Import(XmlSchemaSet, XmlQualifiedName)
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(null, SchemaUtils.InvalidTypeNames[0]), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'schemas')" };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.MixedSchemas, (XmlQualifiedName)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'typeName')" };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.MixedSchemas, SchemaUtils.InvalidTypeNames[0]), typeof(InvalidDataContractException), @"Type 'InvalidType' in namespace 'http://schemas.datacontract.org/2004/07/fooNs' cannot be imported. The root particle must be a sequence. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };

            // Import(XmlSchemaSet, XmlSchemaElement)
            // TODO

            // NegativeTests.cs, part 1 : Bad schema
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[1]), SchemaUtils.NegativeTypeNames[1]),
                typeof(InvalidDataContractException), @"Invalid type specified. Type with name 'FooType' not found in schema with namespace 'http://EmptySchema'." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[2]), SchemaUtils.NegativeTypeNames[2]),
                typeof(InvalidDataContractException), @"Invalid type specified. Type with name 'FooType' not found in schema with namespace 'http://NonExistantSchema'." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[3])),
                typeof(InvalidDataContractException), @"Type 'InvalidTopLevelElementType' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. The global element found in the schema with same name references a different type 'int' in namespace 'http://www.w3.org/2001/XMLSchema'. Data contract types must have the same name as their root element name. Consider removing the global element or changing its type. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[4])),
                typeof(InvalidDataContractException), @"Type 'ExtraAttributesType' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. Attributes must be optional and from namespace 'http://schemas.microsoft.com/2003/10/Serialization/'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[5]), SchemaUtils.NegativeTypeNames[5]),
                typeof(InvalidDataContractException), @"Type 'ExtraAttributeWildcardType' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. 'anyAttribute' is not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[6])),
                typeof(InvalidDataContractException), @"Type 'InvalidRootParticleType' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. The root particle must be a sequence. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[7])),
                typeof(InvalidDataContractException), @"Type 'InvalidTopLevelElement' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. The global element found in the schema with same name references a different type 'string' in namespace 'http://www.w3.org/2001/XMLSchema'. Data contract types must have the same name as their root element name. Consider removing the global element or changing its type. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[8])),
                typeof(InvalidDataContractException), @"Type 'TypeWithElementsOfSameName' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. The type contains two elements with the same name 'DuplicatedName'. Multiple elements with the same name in one type are not supported because members marked with DataMemberAttribute attribute must have unique names. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[9])),
                typeof(InvalidDataContractException), @"Type 'SimpleTypeUnion' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. Simple types with <union> content are not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[10])),
                typeof(InvalidDataContractException), @"Enum type 'EnumOnlyList' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. Simple type list must contain an anonymous type specifying enumeration facets. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[11])),
                typeof(InvalidDataContractException), @"Enum type 'EnumNonStringBaseType' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. Anonymous type with <restriction> cannot be used to create Flags enumeration because it is not a valid enum type. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[12])),
                typeof(InvalidDataContractException), @"Type 'ComplexTypeWithSimpleContent' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. Complex types with simple content extension are not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[13])),
                typeof(InvalidDataContractException), @"Array type 'ArrayOfBar' in namespace 'http://schemas.datacontract.org/2004/07/foo' cannot be imported. Form for element 'Bar' must be qualified. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };
            yield return new object[] { (XsdDataContractImporter imp) => imp.Import(SchemaUtils.ReadStringsIntoSchemaSet(SchemaUtils.NegativeSchemaStrings[14])),
                typeof(InvalidDataContractException), @"Type 'DataSet.datasetType' in namespace 'http://tempuri.org/' cannot be imported. The root sequence must contain only local elements. Group ref, choice, any and nested sequences are not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer." };

            // NegativeTests.cs, part 2 : Bad attribute
            (Type type, string xpath, string prefix, string localName, string ns, string value, string exMsg)[] badAttributeCases = new (Type, string, string, string, string, string, string)[] {
                (typeof(SerializableClass), @"//xs:schema/xs:complexType[@name='SerializableClass']", "", @"abstract", "", @"true", @"Type 'SerializableClass' in namespace 'http://special1.tempuri.org' cannot be imported. The type cannot have 'abstract' set to 'true'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                (typeof(SerializableClass), @"//xs:schema/xs:complexType[@name='SerializableClass']", null, @"mixed", "", @"true", @"Type 'SerializableClass' in namespace 'http://special1.tempuri.org' cannot be imported. Complex type with mixed content is not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                (typeof(SerializableClass), @"//xs:schema/xs:complexType[@name='SerializableClass']/xs:sequence/xs:element[@name='member']", "", @"fixed", "", @"xxx", @"Type 'SerializableClass' in namespace 'http://special1.tempuri.org' cannot be imported. Fixed value on element 'member' is not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                (typeof(SerializableClass), @"//xs:schema/xs:complexType[@name='SerializableClass']/xs:sequence/xs:element[@name='member']", "", @"default", "", @"yyy", @"Type 'SerializableClass' in namespace 'http://special1.tempuri.org' cannot be imported. Default value on element 'member' is not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                (typeof(SerializableClass), @"//xs:schema/xs:element[@name='SerializableClass']", "", @"abstract", "", @"true", @"Type 'SerializableClass' in namespace 'http://special1.tempuri.org' cannot be imported. The element cannot have 'abstract' set to 'true'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                (typeof(SerializableClass), @"//xs:schema/xs:element[@name='SerializableClass']", "", @"substitutionGroup", "", @"tns:Head", @"Type 'SerializableClass' in namespace 'http://special1.tempuri.org' cannot be imported. Substitution group on element 'SerializableClass' is not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
            };
            foreach (var bac in badAttributeCases)
            {
                XsdDataContractExporter exporter = new XsdDataContractExporter();
                exporter.Export(bac.type);
                XmlSchemaSet schemaSet = exporter.Schemas;
                XmlQualifiedName typeName = exporter.GetSchemaTypeName(bac.type);
                string schemaString = SchemaUtils.GetSchemaString(schemaSet, typeName.Namespace);
                schemaString = SchemaUtils.InsertElement(schemaString, @"//xs:schema/xs:element[@name='SerializableClass']", @"<xs:element name='Head' type='xs:anyType'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", true);
                schemaString = SchemaUtils.InsertAttribute(schemaString, bac.xpath, bac.prefix, bac.localName, bac.ns, bac.value);
                XmlSchemaSet schema = SchemaUtils.ReadStringsIntoSchemaSet(schemaString);
                yield return new object[] { (XsdDataContractImporter imp) => imp.Import(schema), typeof(InvalidDataContractException), bac.exMsg };
            }

            // NegativeTests.cs, part 3 : Bad element
            (Type type, string xpath, string xmlFrag, bool insertAfter, string exMsg)[] badElementCases = new(Type, string, string, bool, string)[] {
                (typeof(SerializableClass), @"//xs:schema/xs:complexType[@name='SerializableClass']/xs:sequence/xs:element", @"<xs:element ref='tns:SerializableClass'   xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", true, @"Type 'SerializableClass' in namespace 'http://special1.tempuri.org' cannot be imported. Ref to element 'SerializableClass' in 'http://special1.tempuri.org' namespace is not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                (typeof(DerivedClass), @"//xs:schema/xs:complexType[@name='DerivedClass']/xs:complexContent/xs:extension/xs:sequence/xs:element", @"<xs:choice   xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", false, @"Type 'DerivedClass' in namespace 'http://special1.tempuri.org' cannot be imported. The root sequence must contain only local elements. Group ref, choice, any and nested sequences are not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                (typeof(DerivedClass), @"//xs:schema/xs:complexType[@name='DerivedClass']/xs:complexContent/xs:extension/xs:sequence/xs:element", @"<xs:element ref='ser:int'  xmlns:ser='http://schemas.microsoft.com/2003/10/Serialization/' xmlns:xs='http://www.w3.org/2001/XMLSchema'/>", false, @"Type 'DerivedClass' in namespace 'http://special1.tempuri.org' cannot be imported. Ref to element 'int' in 'http://schemas.microsoft.com/2003/10/Serialization/' namespace is not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
            };
            foreach (var bec in badElementCases)
            {
                XsdDataContractExporter exporter = new XsdDataContractExporter();
                exporter.Export(bec.type);
                XmlSchemaSet schemaSet = exporter.Schemas;
                XmlQualifiedName typeName = exporter.GetSchemaTypeName(bec.type);
                string schemaString = SchemaUtils.GetSchemaString(schemaSet, typeName.Namespace);
                schemaString = SchemaUtils.InsertElement(schemaString, bec.xpath, bec.xmlFrag, bec.insertAfter);
                XmlSchemaSet schema = SchemaUtils.ReadStringsIntoSchemaSet(schemaString);
                yield return new object[] { (XsdDataContractImporter imp) => imp.Import(schema), typeof(InvalidDataContractException), bec.exMsg };
            }

            // FormatVersioning.cs : Negative tests
            (string msg, Type type, string xpath, string xmlFrag, string exMsg)[] formatVersioningNegativeArgs = new (string, Type, string, string, string)[] {
                  ("Required Serialization Attribute in class",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ImporterTests.SerializableFormatClass']/xs:sequence", @"<xs:attribute ref='ser:V2Attribute' use='required'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ImporterTests.SerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests' cannot be imported. Attributes must be optional and from namespace 'http://schemas.microsoft.com/2003/10/Serialization/'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                  ("Required Serialization Attribute in ISerializable",
                      typeof(ISerializableFormatClass), @"//xs:schema/xs:complexType[@name='ISerializableFormatClass']/xs:sequence", @"<xs:attribute ref='ser:V2Attribute' use='required'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ISerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests.DataContracts' cannot be imported. Attributes must be optional and from namespace 'http://schemas.microsoft.com/2003/10/Serialization/'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                  ("Required Serialization Attribute in Array",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ArrayOfImporterTests.SerializableFormatClass']/xs:sequence", @"<xs:attribute ref='ser:V2Attribute' use='required'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ArrayOfImporterTests.SerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests' cannot be imported. Attributes must be optional and from namespace 'http://schemas.microsoft.com/2003/10/Serialization/'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                  ("Required Serialization Element in class",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ImporterTests.SerializableFormatClass']/xs:sequence/xs:element", @"<xs:element ref='ser:V2Element' minOccurs='1'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ImporterTests.SerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests' cannot be imported. Ref to element 'V2Element' in 'http://schemas.microsoft.com/2003/10/Serialization/' namespace is not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                  ("Required Serialization Element in ISerializable",
                      typeof(ISerializableFormatClass), @"//xs:schema/xs:complexType[@name='ISerializableFormatClass']/xs:sequence/xs:any", @"<xs:element ref='ser:V2Element' minOccurs='1'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ISerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests.DataContracts' cannot be imported. The root sequence must contain only local elements. Group ref, choice, any and nested sequences are not supported. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                  ("Required Serialization Element in Array",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ArrayOfImporterTests.SerializableFormatClass']/xs:sequence/xs:element", @"<xs:element ref='ser:V2Element' minOccurs='1'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ArrayOfImporterTests.SerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests' cannot be imported. 'maxOccurs' on element 'ImporterTests.SerializableFormatClass' must be 1. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                  ("Optional Global Attribute in class",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ImporterTests.SerializableFormatClass']/xs:sequence", @"<xs:attribute ref='g:MyGlobalAttribute' xmlns:g='http://myns/'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ImporterTests.SerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests' cannot be imported. Attributes must be optional and from namespace 'http://schemas.microsoft.com/2003/10/Serialization/'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                  ("Optional Global Attribute in ISerializable",
                      typeof(ISerializableFormatClass), @"//xs:schema/xs:complexType[@name='ISerializableFormatClass']/xs:sequence", @"<xs:attribute ref='g:MyGlobalAttribute' xmlns:g='http://myns/'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ISerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests.DataContracts' cannot be imported. Attributes must be optional and from namespace 'http://schemas.microsoft.com/2003/10/Serialization/'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
                  ("Optional Global Attribute in Array",
                      typeof(SerializableFormatClass), @"//xs:schema/xs:complexType[@name='ArrayOfImporterTests.SerializableFormatClass']/xs:sequence", @"<xs:attribute ref='g:MyGlobalAttribute' xmlns:g='http://myns/'  xmlns:xs='http://www.w3.org/2001/XMLSchema'/>",
                      @"Type 'ArrayOfImporterTests.SerializableFormatClass' in namespace 'http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Schema.Tests' cannot be imported. Attributes must be optional and from namespace 'http://schemas.microsoft.com/2003/10/Serialization/'. Either change the schema so that the types can map to data contract types or use ImportXmlType or use a different serializer."),
            };
            foreach (var fvArg in formatVersioningNegativeArgs)
            {
                (XmlSchemaSet schemaSet, XmlQualifiedName typeName) = PrepareFormatVersioningTest(fvArg.type, fvArg.xpath, fvArg.xmlFrag);
                yield return new object[] { (XsdDataContractImporter imp) => imp.Import(schemaSet, typeName), typeof(InvalidDataContractException), fvArg.exMsg };
            }
        }
#pragma warning disable CS0169, IDE0051, IDE1006
        [Serializable]
        public class SerializableFormatClass
        {
            SerializableFormatClass[] array;
        }
#pragma warning restore CS0169, IDE0051, IDE1006

        [Theory]
        [MemberData(nameof(GetCodeTypeReference_MemberData))]
        public void GetCodeTypeReference(XmlSchemaSet schemas, XmlQualifiedName qname, string exptectedType, Type expectedExceptionType = null, string msg = null)
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();

            if (schemas != null)
                importer.Import(schemas);

            if (expectedExceptionType == null)
            {
                CodeTypeReference ctr = importer.GetCodeTypeReference(qname);
                Assert.NotNull(ctr);

                string typeString = SchemaUtils.GetString(ctr);
                _output.WriteLine(typeString);
                Assert.Equal(exptectedType, typeString);
            }
            else
            {
                var ex = Assert.Throws(expectedExceptionType, () => importer.GetCodeTypeReference(qname));

                if (!string.IsNullOrEmpty(msg))
                    Assert.Equal(msg, ex.Message);
            }
        }
        public static IEnumerable<object[]> GetCodeTypeReference_MemberData()
        {
            // GetCodeTypeReference(XmlQualifiedName)
            yield return new object[] { SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0], "fooNs.ValidType" };
            yield return new object[] { SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[2], "Suites.SchemaImport.NonAttributedType" };
            yield return new object[] { null, null, null, typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'typeName')" };
            yield return new object[] { null, SchemaUtils.InvalidTypeNames[0], null, typeof(InvalidOperationException), @"Type 'InvalidType' from namespace 'http://schemas.datacontract.org/2004/07/fooNs' has not been imported from schema. Consider first importing this type by calling one of the Import methods on XsdDataContractImporter." };
            yield return new object[] { SchemaUtils.PositiveSchemas, SchemaUtils.InvalidTypeNames[0], null, typeof(InvalidOperationException), @"Type 'InvalidType' from namespace 'http://schemas.datacontract.org/2004/07/fooNs' has not been imported from schema. Consider first importing this type by calling one of the Import methods on XsdDataContractImporter." };

            // GetCodeTypeReference(XmlQualifiedName, XmlSchemaElement)
            // TODO
        }


        [Theory]
        [MemberData(nameof(GetKnownTypeReferences_MemberData))]
        public void GetKnownTypeReferences(XmlSchemaSet schemas, XmlQualifiedName qname, int expectedRefCount, Type expectedExceptionType = null, string msg = null)
        {
            XsdDataContractImporter importer = SchemaUtils.CreateImporterWithOptions();

            if (schemas != null)
                importer.Import(schemas);

            if (expectedExceptionType == null)
            {
                ICollection<CodeTypeReference> knownTypeReferences = importer.GetKnownTypeReferences(qname);

                if (knownTypeReferences == null)
                {
                    _output.WriteLine("KnownType count: null");
                    Assert.Equal(0, expectedRefCount);
                }
                else
                {
                    _output.WriteLine("KnownType count: {0}", knownTypeReferences.Count);
                    foreach (CodeTypeReference knownTypeReference in knownTypeReferences)
                        _output.WriteLine(SchemaUtils.GetString(knownTypeReference));
                    Assert.Equal(expectedRefCount, knownTypeReferences.Count);
                }
            }
            else
            {
                var ex = Assert.Throws(expectedExceptionType, () => importer.GetKnownTypeReferences(qname));

                if (!string.IsNullOrEmpty(msg))
                    Assert.Equal(msg, ex.Message);
            }
        }
        public static IEnumerable<object[]> GetKnownTypeReferences_MemberData()
        {
            // GetKnownTypeReferences(XmlQualifiedName)
            yield return new object[] { SchemaUtils.PositiveSchemas, SchemaUtils.ValidTypeNames[0], 0 };
            yield return new object[] { null, null, -1, typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'typeName')" };
            yield return new object[] { null, SchemaUtils.ValidTypeNames[0], -1, typeof(InvalidOperationException), @"Type 'ValidType' from namespace 'http://schemas.datacontract.org/2004/07/fooNs' has not been imported from schema. Consider first importing this type by calling one of the Import methods on XsdDataContractImporter." };
            // TODO - a positive case with non-zero ref count.
        }
    }
}
