// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    public class TCValidateAfterAdd : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCValidateAfterAdd(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        private static string path = Path.Combine(FilePathUtil.GetStandardPath(), "xsd10");

        [Theory]
        [InlineData("attributeGroup", "attgC007.xsd", 1, 1, 1, 2)]
        [InlineData("attributeGroup", "attgC024.xsd", 2, 3, 2, 0)]
        [InlineData("attributeGroup", "attgC026.xsd", 1, 4, 1, 0)]
        [InlineData("complexType", "ctA001.xsd", 1, 2, 1, 0)]
        [InlineData("complexType", "ctA002.xsd", 1, 3, 1, 0)]
        [InlineData("complexType", "ctA003.xsd", 1, 3, 1, 0)]
        [InlineData("PARTICLES", "particlesA006.xsd", 1, 2, 1, 0)]
        [InlineData("PARTICLES", "particlesA002.xsd", 1, 2, 1, 0)]
        [InlineData("PARTICLES", "particlesA007.xsd", 1, 2, 1, 0)]
        [InlineData("PARTICLES", "particlesA010.xsd", 1, 2, 1, 0)]
        [InlineData("simpleType", "bug102159_1.xsd", 1, 2, 3, 0)]
        [InlineData("simpleType", "stE064.xsd", 1, 1, 1, 0)]
        [InlineData("Wildcards", "wildG007.xsd", 1, 1, 2, 0)]
        [InlineData("Wildcards", "wildG010.xsd", 3, 1, 5, 0)]
        public void v1(String testDir, String testFile, int expCount, int expCountGT, int expCountGE, int expCountGA)
        {
            Initialize();
            string xsd = Path.Combine(path, testDir, testFile);

            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), ValidationCallback);
            ss.XmlResolver = new XmlUrlResolver();

            XmlSchema Schema1 = ss.Add(Schema);
            ValidateSchemaSet(ss, expCount, false, 0, 0, 0, "Validation after add");
            ValidateWithSchemaInfo(ss);

            ss.Compile();
            ValidateSchemaSet(ss, expCount, true, expCountGT, expCountGE, expCountGA, "Validation after add/comp");
            ValidateWithSchemaInfo(ss);

            XmlSchema Schema2 = null;
            foreach (XmlSchema schema in ss.Schemas())
                Schema2 = ss.Reprocess(schema);

            ValidateSchemaSet(ss, expCount, false, 1, 0, 0, "Validation after repr");
            ValidateWithSchemaInfo(ss);

            ss.Compile();
            ValidateSchemaSet(ss, expCount, true, expCountGT, expCountGE, expCountGA, "Validation after repr/comp");
            ValidateWithSchemaInfo(ss);

            Assert.Equal(ss.RemoveRecursive(Schema), true);
            ValidateSchemaSet(ss, 0, false, 1, 0, 0, "Validation after remRec");
            ValidateWithSchemaInfo(ss);

            ss.Compile();
            ValidateSchemaSet(ss, 0, true, 0, 0, 0, "Validation after remRec/comp");
            ValidateWithSchemaInfo(ss);

            return;
        }

        [Theory]
        [InlineData("attributeGroup", "attgC007", 1, 1, 1, 2)]
        [InlineData("attributeGroup", "attgC024", 2, 3, 2, 0)]
        [InlineData("attributeGroup", "attgC026", 1, 4, 1, 0)]
        [InlineData("complexType", "ctA001", 1, 2, 1, 0)]
        [InlineData("complexType", "ctA002", 1, 3, 1, 0)]
        [InlineData("complexType", "ctA003", 1, 3, 1, 0)]
        [InlineData("PARTICLES", "particlesA006", 1, 2, 1, 0)]
        [InlineData("PARTICLES", "particlesA002", 1, 2, 1, 0)]
        [InlineData("PARTICLES", "particlesA007", 1, 2, 1, 0)]
        [InlineData("PARTICLES", "particlesA010", 1, 2, 1, 0)]
        [InlineData("simpleType", "bug102159_1", 1, 2, 3, 0)]
        [InlineData("simpleType", "stE064", 1, 1, 1, 0)]
        [InlineData("Wildcards", "wildG007", 1, 1, 2, 0)]
        [InlineData("Wildcards", "wildG010", 3, 1, 5, 0)]
        public void v2(String testDir, String testFile, int expCount, int expCountGT, int expCountGE, int expCountGA)
        {
            Initialize();
            string xsd = Path.Combine(path, testDir, testFile + ".xsd");
            string xml = Path.Combine(path, testDir, testFile + ".xml");

            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), ValidationCallback);
            ss.XmlResolver = new XmlUrlResolver();

            XmlSchema Schema1 = ss.Add(Schema);
            ValidateSchemaSet(ss, expCount, false, 0, 0, 0, "Validation after add");
            ValidateWithXmlReader(ss, xml, xsd);

            ss.Compile();
            ValidateSchemaSet(ss, expCount, true, expCountGT, expCountGE, expCountGA, "Validation after add/comp");
            ValidateWithXmlReader(ss, xml, xsd);

            XmlSchema Schema2 = null;
            foreach (XmlSchema schema in ss.Schemas())
                Schema2 = ss.Reprocess(schema);
            ValidateSchemaSet(ss, expCount, false, 1, 0, 0, "Validation after repr");
            ValidateWithXmlReader(ss, xml, xsd);

            ss.Compile();
            ValidateSchemaSet(ss, expCount, true, expCountGT, expCountGE, expCountGA, "Validation after repr/comp");
            ValidateWithXmlReader(ss, xml, xsd);

            Assert.Equal(ss.RemoveRecursive(Schema), true);
            ValidateSchemaSet(ss, 0, false, 1, 0, 0, "Validation after remRec");
            ValidateWithXmlReader(ss, xml, xsd);

            ss.Compile();
            ValidateSchemaSet(ss, 0, true, 0, 0, 0, "Validation after add");
            ValidateWithXmlReader(ss, xml, xsd);

            return;
        }
    }

    public class TCValidateAfterRemove : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCValidateAfterRemove(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        private static string path = Path.Combine(FilePathUtil.GetStandardPath(), "xsd10");

        [Theory]
        [InlineData("attributeGroup", "attgC007.xsd", 1, 1, 1, 2, 0, 0)]
        [InlineData("attributeGroup", "attgC024.xsd", 2, 3, 2, 0, 1, 1)]
        [InlineData("attributeGroup", "attgC026.xsd", 1, 4, 1, 0, 0, 0)]
        [InlineData("complexType", "ctA001.xsd", 1, 2, 1, 0, 0, 0)]
        [InlineData("complexType", "ctA002.xsd", 1, 3, 1, 0, 0, 0)]
        [InlineData("complexType", "ctA003.xsd", 1, 3, 1, 0, 0, 0)]
        [InlineData("PARTICLES", "particlesA006.xsd", 1, 2, 1, 0, 0, 0)]
        [InlineData("PARTICLES", "particlesA002.xsd", 1, 2, 1, 0, 0, 0)]
        [InlineData("PARTICLES", "particlesA007.xsd", 1, 2, 1, 0, 0, 0)]
        [InlineData("PARTICLES", "particlesA010.xsd", 1, 2, 1, 0, 0, 0)]
        [InlineData("simpleType", "bug102159_1.xsd", 1, 2, 3, 0, 0, 0)]
        [InlineData("simpleType", "stE064.xsd", 1, 1, 1, 0, 0, 0)]
        [InlineData("Wildcards", "wildG007.xsd", 1, 1, 2, 0, 0, 0)]
        [InlineData("Wildcards", "wildG010.xsd", 3, 1, 5, 0, 3, 1)]
        public void v1(String testDir, String testFile, int expCount, int expCountGT, int expCountGE, int expCountGA, int expCountGER, int expCountGERC)
        {
            Initialize();
            string xsd = Path.Combine(path, testDir, testFile);

            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), ValidationCallback);
            ss.XmlResolver = new XmlUrlResolver();

            XmlSchema Schema1 = ss.Add(Schema);
            ValidateSchemaSet(ss, expCount, false, 0, 0, 0, "Validation after add");
            ValidateWithSchemaInfo(ss);

            ss.Compile();
            ValidateSchemaSet(ss, expCount, true, expCountGT, expCountGE, expCountGA, "Validation after  add/comp");
            ValidateWithSchemaInfo(ss);

            ss.Remove(Schema);
            ValidateSchemaSet(ss, expCount - 1, false, 1, expCountGER, 0, "Validation after remove");
            ValidateWithSchemaInfo(ss);

            ss.Compile();
            ValidateSchemaSet(ss, expCount - 1, true, expCountGERC, expCountGER, 0, "Validation after rem/comp");
            ValidateWithSchemaInfo(ss);

            XmlSchema Schema2 = null;
            try
            {
                Schema2 = ss.Reprocess(Schema);
                Assert.True(false);
            }
            catch (ArgumentException e)
            {
                _output.WriteLine(e.Message);
            }
            ValidateSchemaSet(ss, expCount - 1, true, expCountGERC, expCountGER, 0, "Validation after repr");
            ValidateWithSchemaInfo(ss);

            Assert.Equal(ss.RemoveRecursive(Schema), false);
            ValidateSchemaSet(ss, expCount - 1, true, expCountGERC, expCountGER, 0, "Validation after add");
            ValidateWithSchemaInfo(ss);

            ss.Compile();
            ValidateSchemaSet(ss, expCount - 1, true, expCountGERC, expCountGER, 0, "Validation after remRec/comp");
            ValidateWithSchemaInfo(ss);

            return;
        }

        [Theory]
        [InlineData("attributeGroup", "attgC007", 1, 1, 1, 2, 0, 0)]
        [InlineData("attributeGroup", "attgC024", 2, 3, 2, 0, 1, 1)]
        [InlineData("attributeGroup", "attgC026", 1, 4, 1, 0, 0, 0)]
        [InlineData("complexType", "ctA001", 1, 2, 1, 0, 0, 0)]
        [InlineData("complexType", "ctA002", 1, 3, 1, 0, 0, 0)]
        [InlineData("complexType", "ctA003", 1, 3, 1, 0, 0, 0)]
        [InlineData("PARTICLES", "particlesA006", 1, 2, 1, 0, 0, 0)]
        [InlineData("PARTICLES", "particlesA002", 1, 2, 1, 0, 0, 0)]
        [InlineData("PARTICLES", "particlesA007", 1, 2, 1, 0, 0, 0)]
        [InlineData("PARTICLES", "particlesA010", 1, 2, 1, 0, 0, 0)]
        [InlineData("simpleType", "bug102159_1", 1, 2, 3, 0, 0, 0)]
        [InlineData("simpleType", "stE064", 1, 1, 1, 0, 0, 0)]
        [InlineData("Wildcards", "wildG007", 1, 1, 2, 0, 0, 0)]
        [InlineData("Wildcards", "wildG010", 3, 1, 5, 0, 3, 1)]
        public void v2(String testDir, String testFile, int expCount, int expCountGT, int expCountGE, int expCountGA, int expCountGER, int expCountGERC)
        {
            Initialize();
            string xsd = Path.Combine(path, testDir, testFile + ".xsd");
            string xml = Path.Combine(path, testDir, testFile + ".xml");

            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), ValidationCallback);
            ss.XmlResolver = new XmlUrlResolver();

            XmlSchema Schema1 = ss.Add(Schema);
            ValidateSchemaSet(ss, expCount, false, 0, 0, 0, "Validation after add");
            ValidateWithXmlReader(ss, xml, xsd);

            ss.Compile();
            ValidateSchemaSet(ss, expCount, true, expCountGT, expCountGE, expCountGA, "Validation after add/comp");
            ValidateWithXmlReader(ss, xml, xsd);

            ss.Remove(Schema);
            ValidateSchemaSet(ss, expCount - 1, false, 1, expCountGER, 0, "Validation after rem");
            ValidateWithXmlReader(ss, xml, xsd);

            ss.Compile();
            ValidateSchemaSet(ss, expCount - 1, true, expCountGERC, expCountGER, 0, "Validation after add");
            ValidateWithXmlReader(ss, xml, xsd);

            XmlSchema Schema2 = null;
            try
            {
                Schema2 = ss.Reprocess(Schema);
                Assert.True(false);
            }
            catch (ArgumentException e)
            {
                _output.WriteLine(e.Message);
            }
            ValidateSchemaSet(ss, expCount - 1, true, expCountGERC, expCountGER, 0, "Validation after repr");
            ValidateWithXmlReader(ss, xml, xsd);

            Assert.Equal(ss.RemoveRecursive(Schema), false);
            ValidateSchemaSet(ss, expCount - 1, true, expCountGERC, expCountGER, 0, "Validation after remRec");
            ValidateWithXmlReader(ss, xml, xsd);

            ss.Compile();
            ValidateSchemaSet(ss, expCount - 1, true, expCountGERC, expCountGER, 0, "Validation after remRec/comp");
            ValidateWithXmlReader(ss, xml, xsd);

            return;
        }
    }

    public class TCValidateAfterReprocess : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCValidateAfterReprocess(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        private static string path = Path.Combine(FilePathUtil.GetStandardPath(), "xsd10");

        [Theory]
        [InlineData("attributeGroup", "attgC007.xsd", 1)]
        [InlineData("attributeGroup", "attgC024.xsd", 2)]
        [InlineData("attributeGroup", "attgC026.xsd", 1)]
        [InlineData("complexType", "ctA001.xsd", 1)]
        [InlineData("complexType", "ctA002.xsd", 1)]
        [InlineData("complexType", "ctA003.xsd", 1)]
        [InlineData("simpleType", "bug102159_1.xsd", 1)]
        [InlineData("simpleType", "stE064.xsd", 1)]
        [InlineData("Wildcards", "wildG007.xsd", 1)]
        [InlineData("Wildcards", "wildG010.xsd", 3)]
        public void v1(String testDir, String TestFile, int expCount)
        {
            Initialize();
            string xsd = Path.Combine(path, testDir, TestFile);

            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), ValidationCallback);
            ss.XmlResolver = new XmlUrlResolver();

            XmlSchema schema = ss.Add(Schema);
            Assert.Equal(ss.Count, expCount);
            ss.Compile();
            Assert.Equal(ss.Count, expCount);

            XmlSchemaElement element = new XmlSchemaElement();
            schema.Items.Add(element);
            element.Name = "book";
            element.SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");

            foreach (XmlSchema sc in ss.Schemas())
                ss.Reprocess(sc);

            Assert.Equal(ss.Count, expCount);
            ss.Compile();
            Assert.Equal(ss.Count, expCount);

            ValidateWithSchemaInfo(ss);

            Assert.Equal(ss.Count, expCount);
            ss.Compile();
            Assert.Equal(ss.Count, expCount);

            ss.RemoveRecursive(Schema);
            Assert.Equal(ss.Count, 0);
            ss.Compile();
            Assert.Equal(ss.Count, 0);

            try
            {
                ss.Reprocess(Schema);
                Assert.True(false);
            }
            catch (ArgumentException e)
            {
                _output.WriteLine(e.Message);
                Assert.Equal(ss.Count, 0);
            }
            return;
        }

        [Theory]
        [InlineData("attributeGroup", "attgC007", 1)]
        [InlineData("attributeGroup", "attgC024", 2)]
        [InlineData("attributeGroup", "attgC026", 1)]
        [InlineData("complexType", "ctA001", 1)]
        [InlineData("complexType", "ctA002", 1)]
        [InlineData("complexType", "ctA003", 1)]
        [InlineData("PARTICLES", "particlesA006", 1)]
        [InlineData("PARTICLES", "particlesA002", 1)]
        [InlineData("PARTICLES", "particlesA007", 1)]
        [InlineData("PARTICLES", "particlesA010", 1)]
        [InlineData("simpleType", "bug102159_1", 1)]
        [InlineData("simpleType", "stE064", 1)]
        [InlineData("Wildcards", "wildG007", 1)]
        [InlineData("Wildcards", "wildG010", 3)]
        public void v2(String testDir, String testFile, int expCount)
        {
            Initialize();
            string xsd = Path.Combine(path, testDir, testFile + ".xsd");
            string xml = Path.Combine(path, testDir, testFile + ".xml");

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema schema = ss.Add(null, XmlReader.Create(xsd));
            Assert.Equal(ss.Count, expCount);
            ss.Compile();
            Assert.Equal(ss.Count, expCount);

            XmlSchemaElement element = new XmlSchemaElement();
            schema.Items.Add(element);
            element.Name = "book";
            element.SchemaTypeName = new XmlQualifiedName("string", "http://www.w3.org/2001/XMLSchema");

            foreach (XmlSchema sc in ss.Schemas())
                ss.Reprocess(sc);

            Assert.Equal(ss.Count, expCount);
            ss.Compile();
            Assert.Equal(ss.Count, expCount);

            ValidateWithXmlReader(ss, xml, xsd);

            Assert.Equal(ss.Count, expCount);
            ss.Compile();
            Assert.Equal(ss.Count, expCount);

            ss.RemoveRecursive(schema);
            Assert.Equal(ss.Count, 0);
            ss.Compile();
            Assert.Equal(ss.Count, 0);

            try
            {
                ss.Reprocess(schema);
                Assert.True(false);
            }
            catch (ArgumentException e)
            {
                _output.WriteLine(e.Message);
                Assert.Equal(ss.Count, 0);
            }
            return;
        }
    }

    public class TCValidateAfterAddInvalidSchema : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCValidateAfterAddInvalidSchema(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        private static string path = Path.Combine(FilePathUtil.GetStandardPath(), "xsd10");
        private static string testData = Path.Combine(FilePathUtil.GetTestDataPath(), "XmlSchemaCollection");

        [Theory]
        [InlineData("SCHEMA", "schE1_a.xsd", 2, 3, 3)]
        [InlineData("SCHEMA", "schE3.xsd", 1, 1, 0)]
        [InlineData("SCHEMA", "schB8.xsd", 1, 1, 1)]
        [InlineData("SCHEMA", "schB1_a.xsd", 1, 3, 3)]
        [InlineData("SCHEMA", "schM2_a.xsd", 1, 3, 3)]
        [InlineData("SCHEMA", "schH2_a.xsd", 1, 3, 3)]
        public void AddValid_Import_Include_Redefine(String testDir, String testFile, int expCount, int expCountGT, int expCountGE)
        {
            string xsd = Path.Combine(path, testDir, testFile);

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), null);
            XmlSchema Schema1 = ss.Add(Schema);
            ValidateSchemaSet(ss, expCount, false, 0, 0, 0, "Validation after add");

            ss.Compile();
            ValidateSchemaSet(ss, expCount, true, expCountGT, expCountGE, 0, "Validation after add/comp");

            foreach (XmlSchema sc in ss.Schemas())
                ss.Reprocess(sc);
            ValidateSchemaSet(ss, expCount, false, 1, 0, 0, "Validation after repr");

            ss.Compile();
            ValidateSchemaSet(ss, expCount, true, expCountGT, expCountGE, 0, "Validation after repr/comp");

            ValidateWithSchemaInfo(ss);
            return;
        }

        [Theory]
        [InlineData("SCHEMA", "schE9.xsd", 1, 1)]
        [InlineData("SCHEMA", "schA7_a.xsd", 2, 2)]
        public void AddEditInvalidImport(String testDir, String testFile, int expCountGT, int expCountGE)
        {
            string xsd = Path.Combine(path, testDir, testFile);

            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), null);
            XmlSchema Schema1 = ss.Add(Schema);
            ValidateSchemaSet(ss, 1, false, 0, 0, 0, "Validation after add");

            ss.Compile();
            ValidateSchemaSet(ss, 1, true, expCountGT, expCountGE, 0, "Validation after add/comp");

            XmlSchemaImport imp = new XmlSchemaImport();
            imp.Namespace = "ns-a";
            imp.SchemaLocation = "reprocess_v9_a.xsd";
            Schema.Includes.Add(imp);

            try
            {
                ss.Reprocess(Schema);
                Assert.True(false);
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.Message);
            }
            ValidateSchemaSet(ss, 1, false, 1, 0, 0, "Validation after repr");

            try
            {
                ss.Compile();
                Assert.True(false);
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.Message);
            }
            ValidateSchemaSet(ss, 1, false, 1, 0, 0, "Validation after repr/comp");

            try
            {
                ValidateWithSchemaInfo(ss);
                Assert.True(false);
            }
            catch (XmlSchemaValidationException e)
            {
                _output.WriteLine(e.Message);
            }
            return;
        }

        [Theory]
        [InlineData("include_v7_a.xsd", 4, 7)]
        [InlineData("include_v1_a.xsd", 3, 3)]
        public void AddEditInvalidIncludeSchema(String testFile, int expCountGT, int expCountGE)
        {
            string xsd = Path.Combine(testData, testFile);

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), null);
            XmlSchema Schema1 = ss.Add(Schema);
            ValidateSchemaSet(ss, 1, false, 0, 0, 0, "Validation after add");

            ss.Compile();
            ValidateSchemaSet(ss, 1, true, expCountGT, expCountGE, 0, "Validation after add/comp");

            XmlSchemaInclude inc = new XmlSchemaInclude();
            inc.SchemaLocation = "include_v2.xsd";
            Schema1.Includes.Add(inc);

            try
            {
                ss.Reprocess(Schema1);
                Assert.True(false);
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.Message);
            }
            ValidateSchemaSet(ss, 1, false, 1, 0, 0, "Validation after repr");

            try
            {
                ss.Compile();
                Assert.True(false);
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.Message);
            }
            ValidateSchemaSet(ss, 1, false, 1, 0, 0, "Validation after repr/comp");

            try
            {
                ValidateWithSchemaInfo(ss);
                Assert.True(false);
            }
            catch (XmlSchemaValidationException e)
            {
                _output.WriteLine(e.Message);
            }
            return;
        }

        [Theory]
        [InlineData("SCHEMA", "schH3.xsd")]
        [InlineData("SCHEMA", "schF3_a.xsd")]
        [InlineData("SCHEMA", "schE1i.xsd")]
        [InlineData("SCHEMA", "schB4_a.xsd")]
        [InlineData("SCHEMA", "schB1i.xsd")]
        public void AddInvalid_Import_Include(String testDir, String testFile)
        {
            Initialize();
            string xsd = Path.Combine(path, testDir, testFile);

            XmlSchemaSet ss = new XmlSchemaSet();
            ss.XmlResolver = new XmlUrlResolver();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(xsd), ValidationCallback);
            XmlSchema Schema1 = null;
            try
            {
                Schema1 = ss.Add(Schema);
                Assert.True(false);
            }
            catch (XmlSchemaException e)
            {
                _output.WriteLine(e.Message);
            }
            ValidateSchemaSet(ss, 0, false, 0, 0, 0, "Validation after add");

            ss.Compile();
            ValidateSchemaSet(ss, 0, true, 0, 0, 0, "Validation after add/comp");
            ValidateWithSchemaInfo(ss);

            XmlSchema Schema2 = null;
            foreach (XmlSchema schema in ss.Schemas())
                Schema2 = ss.Reprocess(schema);

            ValidateSchemaSet(ss, 0, true, 0, 0, 0, "Validation after repr");
            ValidateWithSchemaInfo(ss);

            ss.Compile();
            ValidateSchemaSet(ss, 0, true, 0, 0, 0, "Validation after repr/comp");

            foreach (XmlSchema schema in ss.Schemas())
                ss.Reprocess(schema);
            ValidateWithSchemaInfo(ss);

            return;
        }
    }

    public class TCXmlSchemaValidatorMisc : CXmlSchemaValidatorTestCase
    {
        private ITestOutputHelper _output;
        public TCXmlSchemaValidatorMisc(ITestOutputHelper output) : base(output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void One_Two_Three_XmlSchemaValidatorWithNullParams(int param)
        {
            XmlSchemaValidator val = null;
            try
            {
                switch (param)
                {
                    case 1:
                        val = new XmlSchemaValidator(null, new XmlSchemaSet(), null, XmlSchemaValidationFlags.None);
                        break;

                    case 2:
                        val = new XmlSchemaValidator(new NameTable(), null, null, XmlSchemaValidationFlags.None);
                        break;

                    case 3:
                        val = new XmlSchemaValidator(new NameTable(), new XmlSchemaSet(), null, XmlSchemaValidationFlags.None);
                        break;
                }
            }
            catch (ArgumentNullException e)
            {
                _output.WriteLine(e.Message);
                return;
            }
            Assert.True(false);
        }

        //TFS_469828
        [Fact]
        public void XmlSchemaSetCompileAfterRemovingLastSchemaInTheSetIsNotClearingCachedCompiledInformationUsedForValidation_1()
        {
            string schemaXml = @"
                <Schema:schema xmlns:Schema='http://www.w3.org/2001/XMLSchema'
                           targetNamespace='urn:test'
                           elementFormDefault='qualified'>
                    <Schema:element name='MyElement' type='Schema:int' />
                </Schema:schema>";

            string instanceXml = @"<MyElement xmlns='urn:test'>x100</MyElement>";
            XmlSchemaSet ss = new XmlSchemaSet(new NameTable());
            XmlSchema schema = XmlSchema.Read(new StringReader(schemaXml), null);

            ss.Add(schema);
            Assert.Equal(ss.Count, 1);
            ss.Compile();
            Assert.Equal(ss.Count, 1);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.Schemas = ss;
            settings.ValidationType = ValidationType.Schema;

            using (XmlReader xmlReader = XmlReader.Create(new StringReader(instanceXml), settings))
            {
                try
                {
                    while (xmlReader.Read()) ;
                    Assert.True(false); ;
                }
                catch (XmlSchemaValidationException e)
                {
                    _output.WriteLine("before remove " + e.Message);
                }
            }

            XmlSchema removedSchema = ss.Remove(schema);
            Assert.Equal(ss.Count, 0);
            ss.Compile();
            Assert.Equal(ss.Count, 0);

            settings = new XmlReaderSettings();
            settings.Schemas = ss;
            settings.ValidationType = ValidationType.Schema;

            using (XmlReader xmlReader = XmlReader.Create(new StringReader(instanceXml), settings))
            {
                while (xmlReader.Read()) ;
            }
            return;
        }

        //TFS_469828
        [Fact]
        public void XmlSchemaSetCompileAfterRemovingLastSchemaInTheSetIsNotClearingCachedCompiledInformationUsedForValidation_2()
        {
            string schemaXml = @"<Schema:schema xmlns:Schema='http://www.w3.org/2001/XMLSchema' targetNamespace='uri1'>
    <Schema:element name='doc' type='Schema:string'/>
</Schema:schema>";

            string instanceXml = @"<doc xmlns='uri1'>some</doc>";
            XmlSchemaSet ss = new XmlSchemaSet(new NameTable());
            XmlSchema schema = XmlSchema.Read(new StringReader(schemaXml), null);

            ss.Add(schema);
            Assert.Equal(ss.Count, 1);
            ss.Compile();
            Assert.Equal(ss.Count, 1);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.Schemas = ss;
            settings.ValidationType = ValidationType.Schema;

            using (XmlReader xmlReader = XmlReader.Create(new StringReader(instanceXml), settings))
            {
                while (xmlReader.Read()) ;
            }

            XmlSchema removedSchema = ss.Remove(schema);
            Assert.Equal(ss.Count, 0);
            ss.Compile();
            Assert.Equal(ss.Count, 0);

            settings = new XmlReaderSettings();
            settings.Schemas = ss;
            settings.ValidationType = ValidationType.Schema;

            using (XmlReader xmlReader = XmlReader.Create(new StringReader(instanceXml), settings))
            {
                while (xmlReader.Read()) ;
            }
            return;
        }

        private string xsd = @"<?xml version='1.0' encoding='utf-8'?>
<Schema:schema targetNamespace='mainschema'
    elementFormDefault='qualified'
    xmlns='mainschema'
    xmlns:s1='sub1'
    xmlns:s2='sub2'
    xmlns:Schema='http://www.w3.org/2001/XMLSchema'>
 <Schema:import namespace='sub2' schemaLocation='subschema2.xsd'/>
 <Schema:import namespace='sub1' schemaLocation='subschema1.xsd'/>
 <Schema:element name='root'>
  <Schema:complexType>
   <Schema:all>
    <Schema:element ref='s1:sub'/>
    <Schema:element ref='s2:sub'/>
   </Schema:all>
  </Schema:complexType>
 </Schema:element>
</Schema:schema>";

        private string xml = @"<?xml version='1.0' encoding='utf-8'?>
<root xmlns='mainschema'>
 <sub xmlns='sub2'>
  <node1>text1</node1>
  <node2>text2</node2>
 </sub>
 <sub xmlns='sub1'>
  <node1>text1</node1>
  <node2>text2</node2>
 </sub>
</root>";

        public void CreateSchema1(string testDirectory)
        {
            string commonxsd = @"<?xml version='1.0' encoding='utf-8'?>
<Schema:schema  elementFormDefault='qualified'
    xmlns:Schema='http://www.w3.org/2001/XMLSchema'>
 <Schema:complexType name='CommonType'>
  <Schema:all>
   <Schema:element name='node1' type='Schema:string'/>
   <Schema:element name='node2' type='Schema:string'/>
  </Schema:all>
 </Schema:complexType>
</Schema:schema>";
            string sub1 = @"<?xml version='1.0' encoding='utf-8'?>
<Schema:schema targetNamespace='sub1'
    elementFormDefault='qualified'
    xmlns='sub1'
    xmlns:Schema='http://www.w3.org/2001/XMLSchema'>
 <Schema:include schemaLocation='commonstructure.xsd'/>
 <Schema:element name='sub' type='CommonType'/>
</Schema:schema>";
            string sub2 = @"<?xml version='1.0' encoding='utf-8'?>
<Schema:schema targetNamespace='sub2'
    elementFormDefault='qualified'
    xmlns='sub2'
    xmlns:Schema='http://www.w3.org/2001/XMLSchema'>
 <Schema:include schemaLocation='commonstructure.xsd'/>
 <Schema:element name='sub' type='CommonType'/>
</Schema:schema>";

            using (XmlWriter w = XmlWriter.Create(Path.Combine(testDirectory, "commonstructure.xsd")))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(commonxsd)))
                {
                    w.WriteNode(r, true);
                }
            }
            using (XmlWriter w = XmlWriter.Create(Path.Combine(testDirectory, "subschema1.xsd")))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(sub1)))
                {
                    w.WriteNode(r, true);
                }
            }
            using (XmlWriter w = XmlWriter.Create(Path.Combine(testDirectory, "subschema2.xsd")))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(sub2)))
                {
                    w.WriteNode(r, true);
                }
            }
        }

        public void CreateSchema2(string testDirectory)
        {
            string sub1 = @"<?xml version='1.0' encoding='utf-8'?>
<Schema:schema targetNamespace='sub1'
    elementFormDefault='qualified'
    xmlns='sub1'
    xmlns:Schema='http://www.w3.org/2001/XMLSchema'>
 <Schema:include schemaLocation='commonstructure1.xsd'/>
 <Schema:element name='sub' type='CommonType'/>
</Schema:schema>";
            string sub2 = @"<?xml version='1.0' encoding='utf-8'?>
<Schema:schema targetNamespace='sub2'
    elementFormDefault='qualified'
    xmlns='sub2'
    xmlns:Schema='http://www.w3.org/2001/XMLSchema'>
 <Schema:include schemaLocation='commonstructure2.xsd'/>
 <Schema:element name='sub' type='CommonType'/>
</Schema:schema>";
            string commonxsd1 = @"<?xml version='1.0' encoding='utf-8'?>
<Schema:schema
    elementFormDefault='qualified'
    xmlns:Schema='http://www.w3.org/2001/XMLSchema'>
 <Schema:complexType name='CommonType'>
  <Schema:all>
   <Schema:element name='node1' type='Schema:string'/>
   <Schema:element name='node2' type='Schema:string'/>
  </Schema:all>
 </Schema:complexType>
</Schema:schema>";
            string commonxsd2 = @"<?xml version='1.0' encoding='utf-8'?>
<Schema:schema
    elementFormDefault='qualified'
    xmlns:Schema='http://www.w3.org/2001/XMLSchema'>
 <Schema:complexType name='CommonType'>
  <Schema:all>
   <Schema:element name='node1' type='Schema:string'/>
   <Schema:element name='node2' type='Schema:string'/>
  </Schema:all>
 </Schema:complexType>
</Schema:schema>";

            using (XmlWriter w = XmlWriter.Create(Path.Combine(testDirectory, "commonstructure1.xsd")))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(commonxsd1)))
                {
                    w.WriteNode(r, true);
                }
            }
            using (XmlWriter w = XmlWriter.Create(Path.Combine(testDirectory, "commonstructure2.xsd")))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(commonxsd2)))
                {
                    w.WriteNode(r, true);
                }
            }
            using (XmlWriter w = XmlWriter.Create(Path.Combine(testDirectory, "subschema1.xsd")))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(sub1)))
                {
                    w.WriteNode(r, true);
                }
            }
            using (XmlWriter w = XmlWriter.Create(Path.Combine(testDirectory, "subschema2.xsd")))
            {
                using (XmlReader r = XmlReader.Create(new StringReader(sub2)))
                {
                    w.WriteNode(r, true);
                }
            }
        }

        //TFS_538324
        [Fact]
        public void XSDValidationGeneratesInvalidError_1()
        {
            using (var tempDirectory = new TempDirectory())
            {
                Initialize();
                CreateSchema1(tempDirectory.Path);
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.XmlResolver = new XmlUrlResolver();
                settings.Schemas.XmlResolver = new XmlUrlResolver();
                // TempDirectory path must end with a DirectorySeratorChar, otherwise it will throw in the Xml validation.
                settings.Schemas.Add("mainschema", XmlReader.Create(new StringReader(xsd), null, EnsureTrailingSlash(tempDirectory.Path)));
                settings.ValidationType = ValidationType.Schema;
                XmlReader reader = XmlReader.Create(new StringReader(xml), settings);
                XmlDocument doc = new XmlDocument();

                doc.Load(reader);

                ValidationEventHandler valEventHandler = new ValidationEventHandler(ValidationCallback);
                doc.Validate(valEventHandler);
                Assert.Equal(warningCount, 0);
                Assert.Equal(errorCount, 0);
            }
        }

        //TFS_538324
        [Fact]
        public void XSDValidationGeneratesInvalidError_2()
        {
            using (var tempDirectory = new TempDirectory())
            {
                Initialize();
                CreateSchema2(tempDirectory.Path);
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.XmlResolver = new XmlUrlResolver();
                settings.Schemas.XmlResolver = new XmlUrlResolver();
                // TempDirectory path must end with a DirectorySeratorChar, otherwise it will throw in the Xml validation.
                settings.Schemas.Add("mainschema", XmlReader.Create(new StringReader(xsd), null, EnsureTrailingSlash(tempDirectory.Path)));
                settings.ValidationType = ValidationType.Schema;
                XmlReader reader = XmlReader.Create(new StringReader(xml), settings);
                XmlDocument doc = new XmlDocument();

                doc.Load(reader);

                ValidationEventHandler valEventHandler = new ValidationEventHandler(ValidationCallback);
                doc.Validate(valEventHandler);
                Assert.Equal(warningCount, 0);
                Assert.Equal(errorCount, 0);
            }
        }

        private string EnsureTrailingSlash(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException();

            return path[path.Length - 1] == Path.DirectorySeparatorChar ? 
                path : 
                path + Path.DirectorySeparatorChar;
        }

        private static string xsd445844 = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema xmlns:mstns='http://tempuri.org/XMLSchema.xsd' elementFormDefault='qualified' targetNamespace='http://tempuri.org/XMLSchema.xsd' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
  <xs:complexType name='a'>
    <xs:simpleContent>
      <xs:extension base='xs:boolean' />
    </xs:simpleContent>
  </xs:complexType>
  <xs:complexType name='b'>
    <xs:complexContent mixed='false'>
      <xs:extension base='mstns:a' />
    </xs:complexContent>
  </xs:complexType>
  <xs:element name='c'>
    <xs:complexType>
      <xs:all>
        <xs:element name='d' type='mstns:a' />
      </xs:all>
    </xs:complexType>
  </xs:element>
</xs:schema>";

        private static string xml445844 = @"<?xml version='1.0' encoding='utf-8'?>
<tns:c xmlns:tns='http://tempuri.org/XMLSchema.xsd' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:schemaLocation='http://tempuri.org/XMLSchema.xsd'>
  <tns:d xsi:type='tns:b'>true</tns:d>
</tns:c>";

        //TFS_445844, bug445844
        [Fact]
        public void NullPointerExceptionInXSDValidation()
        {
            Initialize();

            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(new StringReader(xsd445844)), ValidationCallback);
            ss.Add(Schema);
            ss.Compile();

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas.Add("http://tempuri.org/XMLSchema.xsd", XmlReader.Create(new StringReader(xsd445844)));

            using (XmlReader r = XmlReader.Create(new StringReader(xml445844), rs))
            {
                while (r.Read()) ;
            }

            Assert.Equal(warningCount, 0);
            Assert.Equal(errorCount, 0);
            return;
        }

        private static string xsd696909 = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
<xs:element name='Foo' type='FooType' />
<xs:element name='Bar' type='BarType' />
<xs:complexType name='FooType'>
<xs:attribute name='name' type='xs:string' use='optional'/>
</xs:complexType>
<xs:complexType name='BarType'>
<xs:complexContent>
<xs:extension base='FooType'>
<xs:attribute name='name' type='xs:string' use='required'/>
</xs:extension>
</xs:complexContent>
</xs:complexType>
</xs:schema>";

        //TFS_696909, bug696909
        [Fact]
        public void RedefiningAttributeDoesNotResultInXmlSchemaExceptionWhenDerivingByExtension()
        {
            Initialize();
            XmlSchema schema = XmlSchema.Read(new StringReader(xsd696909), ValidationCallback);
            XmlSchemaSet xss = new XmlSchemaSet();
            xss.Add(schema);
            xss.Compile();

            Assert.Equal(warningCount, 0);
            Assert.Equal(errorCount, 0);
            return;
        }

        private static string xsd661328 = @"<?xml version='1.0' encoding='utf-8' ?>
<xs:schema elementFormDefault='qualified' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
<xs:element name='NoContentPatternTest'>
<xs:complexType>
<xs:sequence>
<xs:element minOccurs='0' maxOccurs='unbounded' name='Collapse'>
<xs:simpleType>
<xs:restriction base='xs:string'>
<xs:whiteSpace value='collapse' />
<xs:pattern value='' />
</xs:restriction>
</xs:simpleType>
</xs:element>
</xs:sequence>
</xs:complexType>
</xs:element>
</xs:schema>";

        private static string xml661328 = @"<?xml version='1.0' encoding='utf-8'?>
<NoContentPatternTest xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:noNamespaceSchemaLocation='NoContentPattern.xsd'>
<Collapse></Collapse>
<Collapse> </Collapse>
<Collapse>

</Collapse>
</NoContentPatternTest>";

        //TFS_661328, bug661328
        [Fact]
        public void WhitespaceCollapseFacetNotDealtWithCorrectly()
        {
            Initialize();

            XmlSchemaSet ss = new XmlSchemaSet();
            XmlSchema Schema = XmlSchema.Read(XmlReader.Create(new StringReader(xsd661328)), ValidationCallback);
            ss.Add(Schema);
            ss.Compile();

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas.Add(null, XmlReader.Create(new StringReader(xsd661328)));

            using (XmlReader r = XmlReader.Create(new StringReader(xml661328), rs))
            {
                while (r.Read()) ;
            }

            Assert.Equal(warningCount, 0);
            Assert.Equal(errorCount, 2);
            return;
        }

        //TFS_722809, bug722809
        [Fact]
        public void SchemaPatternFacetHandlesRegularExpressionsWrong()
        {
            Initialize();

            Regex regex = new Regex(@"^\w+$", RegexOptions.None);
            string schemaContent = @"<xs:schema elementFormDefault='qualified' xmlns:xs='http://www.w3.org/2001/XMLSchema'>
<xs:element name='validationTest'>
<xs:simpleType>
<xs:restriction base='xs:string'><xs:pattern value='^\w+$' /></xs:restriction>
</xs:simpleType>
</xs:element>
</xs:schema>";

            XmlReaderSettings rs = new XmlReaderSettings();
            rs.ValidationEventHandler += new ValidationEventHandler(ValidationCallback);
            rs.ValidationType = ValidationType.Schema;
            rs.Schemas.Add(null, XmlReader.Create(new StringReader(schemaContent)));

            using (XmlReader r = XmlReader.Create(new StringReader("<validationTest>test_test</validationTest>"), rs))
            {
                while (r.Read()) ;
            }

            Assert.Equal(warningCount, 0);
            Assert.Equal(errorCount, 1);
            return;
        }
    }
}
