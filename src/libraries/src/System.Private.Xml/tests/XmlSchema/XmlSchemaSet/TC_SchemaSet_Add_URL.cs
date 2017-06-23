// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.IO;
using System.Xml.Schema;
using System.Xml.XPath;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    //[TestCase(Name = "TC_SchemaSet_Add_URL", Desc = "")]
    public class TC_SchemaSet_Add_URL : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Add_URL(ITestOutputHelper output)
        {
            _output = output;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v1 - ns = null, URL = null", Priority = 0)]
        public void v1()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Add((String)null, (String)null);
            }
            catch (ArgumentNullException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v2 - ns = null, URL = valid", Priority = 0)]
        public void v2()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = sc.Add((String)null, TestData._FileXSD1);
            Assert.Equal(Schema != null, true);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v3 - ns = valid, URL = valid")]
        public void v3()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema = sc.Add("xsdauthor", TestData._XsdAuthor);

            Assert.Equal(Schema != null, true);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v4 - ns = valid, URL = invalid")]
        public void v4()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Add("xsdauthor", "http://Bla");
            }
            catch (Exception)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v5 - ns = unmatching, URL = valid")]
        public void v5()
        {
            try
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Add("", TestData._FileXSD1);
            }
            catch (XmlSchemaException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v6 - adding same chameleon for diff NS")]
        public void v6()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor1", TestData._XsdNoNs);
            XmlSchema Schema2 = sc.Add("xsdauthor2", TestData._XsdNoNs);

            Assert.Equal(sc.Count, 2);
            Assert.Equal(Schema1 != null, true);
            // the second call to add should be ignored with Add returning the first obj
            Assert.Equal((Schema2 == Schema1), false);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v7 - adding same URL for null ns")]
        public void v7()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add(null, TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add(null, TestData._XsdAuthor);

            Assert.Equal(sc.Count, 1);
            Assert.Equal(Schema1 != null, true);

            // the second call to add should be ignored with Add returning the first obj
            Assert.Equal(Schema2, Schema1);

            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v8 - adding a schema with NS and one without, to a NS.")]
        public void v8()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add("xsdauthor", TestData._XsdNoNs);

            Assert.Equal(sc.Count, 2);
            Assert.Equal(Schema1 != null, true);
            Assert.Equal((Schema2 == Schema1), false);
            return;
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v9 - adding URL to XSD schema")]
        public void v9()
        {
            XmlSchemaSet sc = new XmlSchemaSet();

            try
            {
                sc.Add(null, Path.Combine(TestData._Root, "schema1.xdr"));
            }
            catch (XmlSchemaException)
            {
                Assert.Equal(sc.Count, 0);
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v10 - Adding schema with top level element collision")]
        public void v10()
        {
            XmlSchemaSet sc = new XmlSchemaSet();

            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add("xsdauthor", TestData._XsdAuthorDup);

            // schemas should be successfully added
            Assert.Equal(sc.Count, 2);
            try
            {
                sc.Compile();
            }
            catch (XmlSchemaException)
            {
                Assert.Equal(sc.Count, 2);
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v11 - Adding schema with top level element collision to Compiled Schemaset")]
        public void v11()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdAuthor);
            sc.Compile();
            XmlSchema Schema2 = sc.Add("xsdauthor", TestData._XsdAuthorDup);

            // schemas should be successfully added
            Assert.Equal(sc.Count, 2);
            try
            {
                sc.Compile();
            }
            catch (XmlSchemaException)
            {
                Assert.Equal(sc.Count, 2);

                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "v12 - Adding schema with no tagetNS with element already existing in NS")]
        public void v12()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlSchema Schema1 = sc.Add("xsdauthor", TestData._XsdAuthor);
            XmlSchema Schema2 = sc.Add("xsdauthor", TestData._XsdAuthorNoNs);
            // schemas should be successfully added
            try
            {
                sc.Compile();
            }
            catch (XmlSchemaException)
            {
                // GLOBALIZATION
                return;
            }
            Assert.True(false);
        }

        //-----------------------------------------------------------------------------------
        [Fact]
        //[Variation(Desc = "435368 - schema validation error")]
        public void v13()
        {
            string xsdPath = Path.Combine(TestData._Root, @"bug435368.xsd");
            string xmlPath = Path.Combine(TestData._Root, @"bug435368.xml");

            XmlSchemaSet xs = new XmlSchemaSet();
            xs.Add(null, xsdPath);

            XmlDocument xd = new XmlDocument();
            xd.Load(xmlPath);
            xd.Schemas = xs;

            // Modify a, partially validate
            XPathNavigator xpn = xd.CreateNavigator().SelectSingleNode("/root/a");
            xpn.SetValue("b");
            xd.Validate(null, ((IHasXmlNode)xpn).GetNode());

            // Modify sg1, partially validate- validate will throw exception
            xpn = xd.CreateNavigator().SelectSingleNode("/root/sg1");
            xpn.SetValue("a");
            xd.Validate(null, ((IHasXmlNode)xpn).GetNode());

            return;
        }

        //====================TFS_298991 XMLSchemaSet.Compile of an XSD containing with a large number of elements results in a System.StackOverflow error

        private string GenerateSequenceXsdFile(int size, string xsdFileName)
        {
            // generate the xsd file, the file is some thing like this
            //-------------------------------------------------------
            //<?xml version='1.0'?>
            //<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' >
            //<xsd:element name='field0' />
            //<xsd:element name='field1' />
            //<xsd:element name='field2' />
            //<xsd:element name='myFields'>
            //    <xsd:complexType>
            //        <xsd:sequence>
            //          <xsd:element ref='field0' minOccurs='0' />
            //          <xsd:element ref='field1' minOccurs='0' />
            //          <xsd:element ref='field2' minOccurs='0' />
            //        </xsd:sequence>
            //    </xsd:complexType>
            //</xsd:element>
            //</xsd:schema>
            //------------------------------------------------------
            string path = Path.Combine(TestDirectory, xsdFileName);

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                string head = @"<?xml version='1.0'?>
                <xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' >";

                string body = @" <xsd:element name='myFields'>
                                  <xsd:complexType>
                                   <xsd:sequence>";

                string end = @"    </xsd:sequence>
                                  </xsd:complexType>
                                </xsd:element>
                              </xsd:schema>";

                sw.WriteLine(head);

                for (int ii = 0; ii < size; ++ii)
                    sw.WriteLine("       <xsd:element name='field{0}' />", ii);

                sw.WriteLine(body);

                for (int ii = 0; ii < size; ++ii)
                    sw.WriteLine("  <xsd:element ref='field{0}' minOccurs='0' />", ii);

                sw.WriteLine(end);
            }

            return path;
        }

        private string GenerateChoiceXsdFile(int size, string xsdFileName)
        {
            // generate the xsd file, the file is some thing like this
            //-------------------------------------------------------
            //<?xml version='1.0'?>
            //<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' >
            //<xsd:element name='field0' />
            //<xsd:element name='field1' />
            //<xsd:element name='field2' />
            //<xsd:element name='myFields'>
            //    <xsd:complexType>
            //        <xsd:choice>
            //          <xsd:element ref='field0' minOccurs='0' />
            //          <xsd:element ref='field1' minOccurs='0' />
            //          <xsd:element ref='field2' minOccurs='0' />
            //        </xsd:choice>
            //    </xsd:complexType>
            //</xsd:element>
            //</xsd:schema>
            //------------------------------------------------------
            string path = Path.Combine(TestDirectory, xsdFileName);

            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (StreamWriter sw = new StreamWriter(fs))
            {
                string head = @"<?xml version='1.0'?>
                <xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' >";

                string body = @" <xsd:element name='myFields'>
                                  <xsd:complexType>
                                   <xsd:choice>";

                string end = @"    </xsd:choice>
                                  </xsd:complexType>
                                </xsd:element>
                              </xsd:schema>";

                sw.WriteLine(head);

                for (int ii = 0; ii < size; ++ii)
                    sw.WriteLine("       <xsd:element name='field{0}' />", ii);

                sw.WriteLine(body);

                for (int ii = 0; ii < size; ++ii)
                    sw.WriteLine("  <xsd:element ref='field{0}' minOccurs='0' />", ii);

                sw.WriteLine(end);
            }

            return path;
        }

        public void verifyXsd(string file)
        {
            try
            {
                XmlSchemaSet ss = new XmlSchemaSet();
                ss.Add("", file);
                ss.Compile();    // if throws StackOfFlowException will cause test failure
            }
            catch (OutOfMemoryException)
            {
                // throw OutOfMemoryException is ok since it is catchable.
            }
        }

        [Theory]
        [InlineData(1000, "1000s.xsd")]
        //[Variation(Desc = "Bug 298991 XMLSchemaSet.Compile cause StackOverflow - Sequence, 5000", Params = new object[] { 5000, "5000s.xsd" })]
        //[Variation(Desc = "Bug 298991 XMLSchemaSet.Compile cause StackOverflow - Sequence, 10000", Params = new object[] { 10000, "10000s.xsd" })]
        public void bug298991Sequence(int size, string xsdFileName)
        {
            xsdFileName = GenerateSequenceXsdFile(size, xsdFileName);

            verifyXsd(xsdFileName);
        }

        [Theory]
        [InlineData(5000, "5000c.xsd")]
        //[Variation(Desc = "Bug 298991 XMLSchemaSet.Compile cause StackOverflow - Choice, 5000", Params = new object[] { 5000, "5000c.xsd" })]
        public void bug298991Choice(int size, string xsdFileName)
        {
            xsdFileName = GenerateChoiceXsdFile(size, xsdFileName);

            verifyXsd(xsdFileName);
        }
    }
}
