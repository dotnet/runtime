// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

using SerializableTypes.XsdDataContractExporterTests;

namespace System.Runtime.Serialization.Xml.XsdDataContractExporterTests
{
    public class ExporterApiTests
    {
        private readonly ITestOutputHelper _output;
        public ExporterApiTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Ctor_Default()
        {
            XsdDataContractExporter xce = new XsdDataContractExporter();
            Assert.NotNull(xce);
            Assert.Null(xce.Options);
        }

        [Fact]
        public void Ctor_Schemas()
        {
            XmlSchemaSet schemaSet = new XmlSchemaSet();
            XsdDataContractExporter xce = new XsdDataContractExporter(schemaSet);
            Assert.NotNull(xce);
            Assert.Null(xce.Options);
            Assert.Equal(schemaSet, xce.Schemas);
        }

        [Theory]
        [MemberData(nameof(CanExport_MemberData))]
        public void CanExport(bool expectedResult, string testname, Func<XsdDataContractExporter, bool> canExport, Type expectedExceptionType = null, string msg = null)
        {
            _output.WriteLine($"=============== {testname} ===============");
            XsdDataContractExporter importer = new XsdDataContractExporter();
            if (expectedExceptionType == null)
            {
                Assert.Equal(expectedResult, canExport(importer));
            }
            else
            {
                var ex = Assert.Throws(expectedExceptionType, () => canExport(importer));

                if (!string.IsNullOrEmpty(msg))
                    Assert.Equal(msg, ex.Message);
            }
        }
        public static IEnumerable<object[]> CanExport_MemberData()
        {
            //yield return new object[] { true, "", (XsdDataContractExporter exp) => exp.CanExport() };
            //yield return new object[] { false, "", (XsdDataContractExporter exp) => exp.CanExport(), typeof(), @"" };

            // CanExport(Type)
            yield return new object[] { true, "t1+", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.Point)) };
            yield return new object[] { false, "t2-", (XsdDataContractExporter exp) => exp.CanExport((Type)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'type')" };
            yield return new object[] { false, "t3-", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.NonSerializableSquare)) };
            yield return new object[] { true, "t4+", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.NonAttributedPersonStruct)) };
            yield return new object[] { true, "t5+", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.NonAttributedPersonClass)) };
            yield return new object[] { true, "t6+", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.ExtendedSquare)) };
            yield return new object[] { false, "t7-", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.RecursiveCollection1)) };
            yield return new object[] { false, "t8-", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.RecursiveCollection2)) };
            yield return new object[] { false, "t9-", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.RecursiveCollection3)) };
            yield return new object[] { false, "t10-", (XsdDataContractExporter exp) => exp.CanExport(typeof(Types.RecursiveCollection4)) };

            // CanExport(ICollection<Assembly>)
            yield return new object[] { true, "ca1+", (XsdDataContractExporter exp) => exp.CanExport(new Assembly[] { typeof(DataContractTypes).Assembly }) };
            yield return new object[] { false, "ca2-", (XsdDataContractExporter exp) => exp.CanExport((ICollection<Assembly>)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'assemblies')" };
            yield return new object[] { false, "ca3-", (XsdDataContractExporter exp) => exp.CanExport(new Assembly[] { null }), typeof(ArgumentException), @"Cannot export null assembly provided via 'assemblies' parameter." };
            yield return new object[] { false, "ca4-", (XsdDataContractExporter exp) => exp.CanExport(new Assembly[] { typeof(ExporterApiTests).Assembly }) };

            // CanExport(ICollection<Type>)
            yield return new object[] { true, "ct1+", (XsdDataContractExporter exp) => exp.CanExport(new Type[] { typeof(Types.Point), typeof(Types.Circle) }) };
            yield return new object[] { false, "ct2-", (XsdDataContractExporter exp) => exp.CanExport((ICollection<Type>)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'types')" };
            yield return new object[] { false, "ct3-", (XsdDataContractExporter exp) => exp.CanExport(new Type[] { null }), typeof(ArgumentException), @"Cannot export null type provided via 'types' parameter." };
            yield return new object[] { false, "ct4-", (XsdDataContractExporter exp) => exp.CanExport(new Type[] { typeof(Types.Point), typeof(Types.NonSerializableSquare) }) };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/73961", typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsBrowser))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/73961", typeof(PlatformDetection), nameof(PlatformDetection.IsWasi))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/73961", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsBuiltWithAggressiveTrimming), nameof(PlatformDetection.IsAppleMobile))]
        [MemberData(nameof(Export_MemberData))]
        public void Export(string testname, Action<XsdDataContractExporter> export, Action<string, XmlSchemaSet> schemaCheck = null)
        {
            _output.WriteLine($"=============== {testname} ===============");
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            export(exporter);

            string schemas = SchemaUtils.DumpSchema(exporter.Schemas);
            _output.WriteLine("Count = " + exporter.Schemas.Count);
            _output.WriteLine(schemas);

            // When checking schema count, be sure to include the "Serialization" schema - which is omitted from 'DumpSchema' - as
            // well as the XmlSchema, both of which are the base from which all further schemas build.
            if (schemaCheck != null)
                schemaCheck(schemas, exporter.Schemas);

            Assert.True(schemas.Length > 0);
        }
        public static IEnumerable<object[]> Export_MemberData()
        {
            // Export(Type)
            yield return new object[] { "Exp1", (XsdDataContractExporter exp) => exp.Export(typeof(Types.Point)), (string s, XmlSchemaSet ss) => {
                Assert.Equal(3, ss.Count);
                // *basic*
                // Point
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://basic"" elementFormDefault=""qualified"" targetNamespace=""http://basic"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:complexType name=""Point"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""X"" type=""xs:int"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Y"" type=""xs:int"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""Point"" nillable=""true"" type=""tns:Point"" />", ref s);
            } };

            // Export(ICollection<Assembly>)
            // AppContext SetSwitch seems to be unreliable in the unit test case. So let's not rely on it
            // for test coverage. But let's do look at the app switch to get our verification correct.
            AppContext.TryGetSwitch("Switch.System.Runtime.Serialization.DataContracts.Auto_Import_KVP", out bool autoImportKVP);
            yield return new object[] { "Exp2", (XsdDataContractExporter exp) => exp.Export(new Assembly[] { typeof(DataContractTypes).Assembly }), (string s, XmlSchemaSet ss) => {
                Assert.Equal(autoImportKVP ? 21 : 20, ss.Count);
                Assert.Equal(autoImportKVP ? 171 : 163, ss.GlobalTypes.Count);
                Assert.Equal(autoImportKVP ? 204 : 196, ss.GlobalElements.Count);
            } };

            // Export(ICollection<Type>)
            yield return new object[] { "Exp3", (XsdDataContractExporter exp) => exp.Export(new Type[] { typeof(Types.Point), typeof(Types.Circle) }), (string s, XmlSchemaSet ss) => {
                Assert.Equal(4, ss.Count);
                // *basic*
                // Point
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://basic"" elementFormDefault=""qualified"" targetNamespace=""http://basic"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:complexType name=""Point"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""X"" type=""xs:int"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Y"" type=""xs:int"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""Point"" nillable=""true"" type=""tns:Point"" />", ref s);
                // *shapes*
                // Circle
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://shapes"" elementFormDefault=""qualified"" targetNamespace=""http://shapes"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:import namespace=""http://basic"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:complexType name=""Circle"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Center"" nillable=""true"" xmlns:q1=""http://basic"" type=""q1:Point"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Radius"" type=""xs:int"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""Circle"" nillable=""true"" type=""tns:Circle"" />", ref s);
            } };
            yield return new object[] { "Exp4", (XsdDataContractExporter exp) => exp.Export(new Type[] { typeof(Types.NonAttributedPersonStruct), typeof(Types.NonAttributedPersonClass), typeof(Types.ExtendedSquare) }), (string s, XmlSchemaSet ss) => {
                Assert.Equal(5, ss.Count);
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types"" elementFormDefault=""qualified"" targetNamespace=""http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:import namespace=""http://schemas.microsoft.com/2003/10/Serialization/"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:import namespace=""http://shapes"" />", ref s);
                // *Types*
                // NonAttributedPersonStruct
                SchemaUtils.OrderedContains(@"<xs:complexType name=""NonAttributedPersonStruct"">", ref s);
                Assert.Matches(@"<xs:appinfo>\s*<IsValueType xmlns=""http://schemas.microsoft.com/2003/10/Serialization/"">true</IsValueType>", s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""firstName"" nillable=""true"" type=""xs:string"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""lastName"" nillable=""true"" type=""xs:string"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""NonAttributedPersonStruct"" nillable=""true"" type=""tns:NonAttributedPersonStruct"" />", ref s);
                // NonAttributedPersonClass
                SchemaUtils.OrderedContains(@"<xs:complexType name=""NonAttributedPersonClass"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""firstName"" nillable=""true"" type=""xs:string"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""lastName"" nillable=""true"" type=""xs:string"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""NonAttributedPersonClass"" nillable=""true"" type=""tns:NonAttributedPersonClass"" />", ref s);
                // ExtendedSquare
                SchemaUtils.OrderedContains(@"<xs:complexType name=""ExtendedSquare"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:complexContent mixed=""false"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:extension xmlns:q1=""http://shapes"" base=""q1:Square"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""lineColor"" nillable=""true"" type=""xs:string"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""ExtendedSquare"" nillable=""true"" type=""tns:ExtendedSquare"" />", ref s);
                // *shapes*
                // Square
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://shapes"" elementFormDefault=""qualified"" targetNamespace=""http://shapes"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:complexType name=""Square"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""BottomLeft"" nillable=""true"" xmlns:q1=""http://basic"" type=""q1:Point"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Side"" type=""xs:int"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""Square"" nillable=""true"" type=""tns:Square"" />", ref s);
                // *basic*
                // Point
                SchemaUtils.OrderedContains(@"<xs:schema xmlns:tns=""http://basic"" elementFormDefault=""qualified"" targetNamespace=""http://basic"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:complexType name=""Point"">", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""X"" type=""xs:int"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element minOccurs=""0"" name=""Y"" type=""xs:int"" />", ref s);
                SchemaUtils.OrderedContains(@"<xs:element name=""Point"" nillable=""true"" type=""tns:Point"" />", ref s);
            } };

            // EnumsTest - from Enums.cs
            yield return new object[] { "ExpEnum", (XsdDataContractExporter exp) => exp.Export(new Type[] { typeof(System.Reflection.TypeAttributes) }), (string s, XmlSchemaSet ss) => {
                Assert.Equal(3, ss.Count);
                //Assert.Equal(3, ss.GlobalAttributes.Count);
                Assert.Equal(5, ss.GlobalTypes.Count);
                Assert.Equal(23, ss.GlobalElements.Count);
            } };
        }

        [Theory]
        [MemberData(nameof(Export_NegativeCases_MemberData))]
        public void Export_NegativeCases(string testname, Action<XsdDataContractExporter> export, Type expectedExceptionType, string exMsg = null)
        {
            _output.WriteLine($"=============== {testname} ===============");
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            var ex = Assert.Throws(expectedExceptionType, () => export(exporter));
            _output.WriteLine(ex.Message);

            if (exMsg != null)
                Assert.Equal(exMsg, ex.Message);
        }
        public static IEnumerable<object[]> Export_NegativeCases_MemberData()
        {
            // Export(Type)
            yield return new object[] { "tn", (XsdDataContractExporter exp) => exp.Export((Type)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'type')" };
            yield return new object[] { "tinv", (XsdDataContractExporter exp) => exp.Export(typeof(Types.NonSerializableSquare)), typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types.NonSerializableSquare' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required." };

            // Export(ICollection<Assembly>)
            yield return new object[] { "can", (XsdDataContractExporter exp) => exp.Export((ICollection<Assembly>)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'assemblies')" };
            yield return new object[] { "canv", (XsdDataContractExporter exp) => exp.Export(new Assembly[] { null }), typeof(ArgumentException), @"Cannot export null assembly provided via 'assemblies' parameter." };
            // This exception message might change with updates to this test assembly. Right now, 'NonSerializablePerson' is the non-serializable type that gets found first. If this becomes an issue, consider not verifying the exception message.
            yield return new object[] { "cainv", (XsdDataContractExporter exp) => exp.Export(new Assembly[] { typeof(ExporterApiTests).Assembly }), typeof(InvalidDataContractException), @"Type 'NonSerializablePerson' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required." };

            // Export(ICollection<Type>)
            yield return new object[] { "ctn", (XsdDataContractExporter exp) => exp.Export((ICollection<Type>)null), typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'types')" };
            yield return new object[] { "ctnv", (XsdDataContractExporter exp) => exp.Export(new Type[] { null }), typeof(ArgumentException), @"Cannot export null type provided via 'types' parameter." };
            yield return new object[] { "ctinv", (XsdDataContractExporter exp) => exp.Export(new Type[] { typeof(Types.Point), typeof(Types.NonSerializableSquare) }), typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types.NonSerializableSquare' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required." };
        }

        [Theory]
        [MemberData(nameof(GetSchemaTypeName_MemberData))]
        public void GetSchemaTypeName(string testname, Type t, XmlQualifiedName qname, Type expectedExceptionType = null, string msg = null)
        {
            _output.WriteLine($"=============== {testname} ===============");
            XsdDataContractExporter exporter = new XsdDataContractExporter();

            if (expectedExceptionType == null)
            {
                XmlQualifiedName schemaTypeName = exporter.GetSchemaTypeName(t);
                Assert.Equal(qname, schemaTypeName);
            }
            else
            {
                var ex = Assert.Throws(expectedExceptionType, () => exporter.GetSchemaTypeName(t));
                if (!string.IsNullOrEmpty(msg))
                    Assert.Equal(msg, ex.Message);
            }
        }
        public static IEnumerable<object[]> GetSchemaTypeName_MemberData()
        {
            // GetSchemaTypeName(Type)
            yield return new object[] { "GSTN_Point", typeof(Types.Point), new XmlQualifiedName("Point", "http://basic") };
            yield return new object[] { "GSTN_null", null, null, typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'type')" };
            yield return new object[] { "GSTN_invalid", typeof(Types.NonSerializableSquare), null, typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types.NonSerializableSquare' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required." };
            yield return new object[] { "GSTN_Square", typeof(Types.Square), new XmlQualifiedName("Square", "http://shapes") };
            yield return new object[] { "GSTN_ExtSq", typeof(Types.ExtendedSquare), new XmlQualifiedName("ExtendedSquare", "http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types") };

            // From DataContractTypesTest.cs
            yield return new object[] { "DCTT_Addr2", typeof(DataContractTypes.Address2), new XmlQualifiedName("Address", "http://schemas.datacontract.org/2004/07/schemaexport.suites") };
        }

        [Theory]
        [MemberData(nameof(GetSchemaType_MemberData))]
        public void GetSchemaType(string testname, Type t, XmlSchemaType stName, Type expectedExceptionType = null, string msg = null)
        {
            _output.WriteLine($"=============== {testname} ===============");
            XsdDataContractExporter exporter = new XsdDataContractExporter();

            if (expectedExceptionType == null)
            {
                XmlSchemaType schemaType = exporter.GetSchemaType(t);
                Assert.Equal(stName, schemaType);
            }
            else
            {
                var ex = Assert.Throws(expectedExceptionType, () => exporter.GetSchemaType(t));
                if (!string.IsNullOrEmpty(msg))
                    Assert.Equal(msg, ex.Message);
            }
        }
        public static IEnumerable<object[]> GetSchemaType_MemberData()
        {
            // GetSchemaTypeName(Type)
            yield return new object[] { "GST_Point", typeof(Types.Point), null };   // Per the docs - "types for which the GetSchemaTypeName method returns a valid name, this method returns null."
            yield return new object[] { "GST_null", null, null, typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'type')" };
            yield return new object[] { "GST_invalid", typeof(Types.NonSerializableSquare), null, typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types.NonSerializableSquare' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required." };
        }

        [Theory]
        [MemberData(nameof(GetRootElementName_MemberData))]
        public void GetRootElementName(string testname, Type t, XmlQualifiedName rName, Type expectedExceptionType = null, string msg = null)
        {
            _output.WriteLine($"=============== {testname} ===============");
            XsdDataContractExporter exporter = new XsdDataContractExporter();

            if (expectedExceptionType == null)
            {
                XmlQualifiedName rootTypeName = exporter.GetRootElementName(t);
                Assert.Equal(rName, rootTypeName);
            }
            else
            {
                var ex = Assert.Throws(expectedExceptionType, () => exporter.GetSchemaTypeName(t));
                if (!string.IsNullOrEmpty(msg))
                    Assert.Equal(msg, ex.Message);
            }
        }
        public static IEnumerable<object[]> GetRootElementName_MemberData()
        {
            // GetSchemaTypeName(Type)
            yield return new object[] { "GREN_Point", typeof(Types.Point), new XmlQualifiedName("Point", "http://basic") };
            yield return new object[] { "GREN_null", null, null, typeof(ArgumentNullException), @"Value cannot be null. (Parameter 'type')" };
            yield return new object[] { "GREN_invalid", typeof(Types.NonSerializableSquare), null, typeof(InvalidDataContractException), @"Type 'System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types.NonSerializableSquare' cannot be serialized. Consider marking it with the DataContractAttribute attribute, and marking all of its members you want serialized with the DataMemberAttribute attribute. Alternatively, you can ensure that the type is public and has a parameterless constructor - all public members of the type will then be serialized, and no attributes will be required." };
            yield return new object[] { "GREN_Square", typeof(Types.Square), new XmlQualifiedName("Square", "http://shapes") };
            yield return new object[] { "GREN_ExtSq", typeof(Types.ExtendedSquare), new XmlQualifiedName("ExtendedSquare", "http://schemas.datacontract.org/2004/07/System.Runtime.Serialization.Xml.XsdDataContractExporterTests.Types") };
        }

        [Fact]
        public void get_Schemas_Bug()
        {
            // Bug 23200 from who knows which ancient bug database
            // I believe the gist of this is that modifying the XmlSchemaSet provided by XsdDataContractExporter.get_Schemas
            // can result in that same property throwing an exception? I'm not really sure what this bug is, or if this really
            // is a bug. Neither the code in NetFx nor here actually throws an exception without the newly added lines.
            XsdDataContractExporter exporter = new XsdDataContractExporter();
            exporter.Export(typeof(Types.Circle));
            XmlSchemaSet schemaSet = exporter.Schemas;  // added - exception
            foreach (XmlSchema schema in exporter.Schemas.Schemas("http://basic"))  // original - Still no exception
                exporter.Schemas.Remove(schema);
            var ex = Assert.Throws<XmlSchemaException>(() => exporter.Schemas); // added
            Assert.Equal(@"Type 'http://basic:Point' is not declared.", ex.Message); // added
            exporter.Export(typeof(Types.Square));
            ex = Assert.Throws<XmlSchemaException>(() => exporter.Schemas); // added
            Assert.Equal(@"Type 'http://basic:Point' is not declared.", ex.Message); // added
        }
    }
}
