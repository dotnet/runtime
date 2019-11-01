// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Xml.Schema;
using Xunit;
using Xunit.Abstractions;

namespace System.Xml.Tests
{
    public class TC_SchemaSet_Add_Reader : TC_SchemaSetBase
    {
        private ITestOutputHelper _output;

        public TC_SchemaSet_Add_Reader(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void NullNamespaceAndNullReader()
        {
            Assert.ThrowsAny<ArgumentNullException>(() =>
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                sc.Add((string)null, (XmlReader)null);
            });
        }

        [Fact]
        public void NullNamespaceValidReader()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlTextReader Reader = new XmlTextReader(TestData._XsdAuthor);
            XmlSchema Schema = sc.Add(null, Reader);

            Assert.Equal(1, sc.Count);
            Assert.NotNull(Schema);
        }

        [Fact]
        public void ValidNamespaceValidReader()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlTextReader Reader = new XmlTextReader(TestData._XsdAuthor);
            XmlSchema Schema = sc.Add("xsdauthor", Reader);

            Assert.Equal(1, sc.Count);
            Assert.NotNull(Schema);
        }

        [Fact]
        public void ValidNamespaceReaderPositionedOnElementButNoXsdSchemaTag()
        {
            Assert.ThrowsAny<XmlSchemaException>(() =>
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                XmlTextReader Reader = new XmlTextReader(TestData._XsdAuthor);

                while (Reader.Read()) { }

                sc.Add("xsdauthor", Reader);
            });
        }

        [Fact]
        public void NamespaceUnmatchingReaderValid()
        {
            Assert.ThrowsAny<XmlSchemaException>(() =>
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                XmlTextReader Reader = new XmlTextReader(TestData._XsdAuthor);
                sc.Add("", Reader);
            });
        }

        [Fact]
        public void Adding2ReadersOnSchemaWithNoNamespacesAddWithDiffNamespace()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlTextReader Reader1 = new XmlTextReader(TestData._XsdNoNs);
            XmlTextReader Reader2 = new XmlTextReader(TestData._XsdNoNs);
            XmlSchema Schema1 = sc.Add("xsdauthor1", Reader1);
            XmlSchema Schema2 = sc.Add("xsdauthor2", Reader2);

            Assert.Equal(2, sc.Count);
            Assert.NotNull(Schema1);
            Assert.NotEqual(Schema1, Schema2);
        }

        [Fact]
        public void AddingSameReaderForNullNamespace()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlTextReader Reader1 = new XmlTextReader(TestData._XsdAuthor);
            XmlTextReader Reader2 = new XmlTextReader(TestData._XsdAuthor);
            XmlSchema Schema1 = sc.Add(null, Reader1);
            XmlSchema Schema2 = sc.Add(null, Reader2);

            Assert.Equal(1, sc.Count);
            Assert.NotNull(Schema1);
            Assert.NotNull(Schema2);
            Assert.Equal(Schema1, Schema2);
        }

        [Fact]
        public void AddingReaderOnXDRSchema()
        {
            Assert.ThrowsAny<XmlSchemaException>(() =>
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                XmlTextReader Reader1 = new XmlTextReader(TestData._SchemaXdr);
                sc.Add(null, Reader1);
            });
        }

        [Fact]
        public void ValidNamespaceReaderPositionedOnANonElementNode()
        {
            Assert.ThrowsAny<XmlSchemaException>(() =>
            {
                XmlSchemaSet sc = new XmlSchemaSet();
                XmlTextReader reader = new XmlTextReader(TestData._FileXSD1);

                // positions on a non element (annotation) node
                while (reader.LocalName != "annotation")
                    reader.Read();
                sc.Add(null, reader);
            });
        }

        [Fact]
        public void ValidNamespaceValidReaderWithResolverSetToNull()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            XmlTextReader Reader = new XmlTextReader(TestData._XsdAuthor);
            Reader.XmlResolver = null;
            XmlSchema Schema = sc.Add("xsdauthor", Reader);

            Assert.Equal(1, sc.Count);
            Assert.NotNull(Schema);
        }

        [Fact]
        public void SchemasWithNoSourceURIOneLoadedFromXmlSchemaReadOtherFromDOMDiffTNS()
        {
            XmlSchemaSet sc = new XmlSchemaSet();
            //first schema
            XmlSchema schema = XmlSchema.Read(new StringReader(@"<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' targetNamespace='bar'><xsd:element name='author1' type='xsd:string'/></xsd:schema>"), null);
            XmlSchema temp = sc.Add(schema);
            Assert.Equal("bar", temp.TargetNamespace);

            //second schema
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' targetNamespace='foo'><xsd:element name='author2' type='xsd:boolean'/></xsd:schema>");
            XmlNode root = doc.FirstChild;
            XmlNodeReader reader = new XmlNodeReader(root);
            temp = sc.Add(null, reader);
            Assert.Equal("foo", temp.TargetNamespace);
            Assert.Equal(2, sc.Count);

            sc.Compile();
        }

        [Fact]
        public void SchemasWithNoSourceURIOneLoadedFromXmlSchemaReadOtherFromDOMSameTNS()
        {
            XmlSchemaSet sc = new XmlSchemaSet();

            //first schema
            XmlSchema schema = XmlSchema.Read(new StringReader(@"<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' targetNamespace='foo'><xsd:element name='author1' type='xsd:string'/></xsd:schema>"), null);

            XmlSchema temp = sc.Add(schema);
            Assert.Equal("foo", temp.TargetNamespace);

            //second schema
            XmlDocument doc = new XmlDocument();

            doc.LoadXml("<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' targetNamespace='foo'><xsd:element name='author2' type='xsd:boolean'/></xsd:schema>");

            XmlNode root = doc.FirstChild;
            XmlNodeReader reader = new XmlNodeReader(root);

            temp = sc.Add(null, reader);
            Assert.Equal("foo", temp.TargetNamespace);
            Assert.Equal(2, sc.Count);
            sc.Compile();
        }

        [Fact]
        public void SchemasWithNoSourceURIOneLoadedFromXmlSchemaReadOtherFromDOMSameNoTNS()
        {
            XmlSchemaSet sc = new XmlSchemaSet();

            //first schema
            XmlSchema schema = XmlSchema.Read(new StringReader(@"<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'><xsd:element name='author1' type='xsd:string'/></xsd:schema>"), null);

            XmlSchema temp = sc.Add(schema);
            Assert.Null(temp.TargetNamespace);

            //second schema
            XmlDocument doc = new XmlDocument();

            doc.LoadXml("<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'><xsd:element name='author2' type='xsd:boolean'/></xsd:schema>");

            XmlNode root = doc.FirstChild;
            XmlNodeReader reader = new XmlNodeReader(root);

            temp = sc.Add(null, reader);
            Assert.Null(temp.TargetNamespace);
            Assert.Equal(2, sc.Count);
            sc.Compile();
        }

        [Fact]
        public void RegressionTest1()
        {
            XmlSchemaSet sc = new XmlSchemaSet();

            //first schema
            XmlSchema schema = XmlSchema.Read(new StringReader(@"<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' targetNamespace='bar'><xsd:element name='author1' type='xsd:string'/></xsd:schema>"), null);
            Assert.Equal("bar", sc.Add(schema).TargetNamespace);
            Assert.Equal(1, sc.Count);

            //second schema
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema' targetNamespace='foo'><xsd:element name='author2' type='xsd:boolean'/></xsd:schema>");

            XmlNode root = doc.FirstChild;
            XmlNodeReader reader = new XmlNodeReader(root);
            Assert.Equal("foo", sc.Add(null, reader).TargetNamespace);
            sc.Compile();
            Assert.Equal(2, sc.Count);
        }

        [Fact]
        public void RegressionTest2()
        {
            string xsd =
@"<xsd:schema targetNamespace='x' xmlns='SMIT_Perf_TopLevelSettings_ListTypes_Large' xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
  <xsd:complexType name='Setting_type_scalarlist_int_6'>
	<xsd:sequence>
	  <xsd:element name='Setting_4_int' type='xsd:int' minOccurs='0' maxOccurs='G0XNiUJFUJvBc6UueYW75O9Hb6VKN34FAlZSSMasbKJJijolOJXAet120Pwl981bPqA0hBbR8ogvF0j2Su6Dg3kRugcDayscSr31ixEMsFCzNFx9q3SPBy5VFtGAjX1J5DdX7rUfpuVa1uDotcWggdbdwiwyrEnUO24JmaC9jxjRcfdUpbRHBUnbm9lY4gylGBM904CSaX7s4JBQFbIPwgT3OdhnznED98KbTdevZ8rnX5hc8UTdXiyksSuWWYdwT4s59j7l7XzyzpgT6EoeUlpaKjtAtHYAe4UcMldZWsgAt9vCwaaSt8crOSvZz61z8yT4UDHUlPIlEorKxqOSqGQgcmO3zDjZ0qC4p1ssSFqe00njkllIsjI1hxbrzNfhqsvXmwe68f9lawpm9oTObbrRiKX2ZB2FkIxIFP6yGpKMjhbuIzrL4QGPAdzcZmh7n4AR9ghurfIwQesBsmtjTrKUCiHlZeYdHMjohKvlS6dZHT8fvytx37n5tpPaZ6UMGpoD5Ddi0IRWNvMtklzlulkKjo515VMWmRSco4ksUL6upQr8df0xGtcfEeeRImnf65u6ctEpBGRipzeS66YZfYP3yxYKZTkriayugvftbKwAb30O9LULNNAhp1HpV4QrfjAOBtwLbo8UmPb57JKN8ZdUaQFonzXg4Ee12lNQZJtjPp5ZZLsMuCsGFpyvoIhlQbahvjCV54yzq4FuVpObHLw2VXvNxclRlHSyOXo0IWQ0N3aLkDa83GeFoiMXyzidShNimQf7BYSu46oc7knuCnXfnMJqLQBTywhqq1iThc7fxuKs4uZNMxboJr5aHzwuW95HY8vFBTMjq2s3Cbwa1yHAV2g86IMspMIyyCT58UL2l18R9CXpCV3i24wfbxJ3He4dt4E9H0cgmOZd4P84ugpTTk53QyeKKSvH6fhF7YiM3W5nfSlLyOIkylJJ8LtTVRWUdSgYs36W8cZ7oSY7MSV6JKSVWXHwpP0lAlKM' />
	</xsd:sequence>
  </xsd:complexType>
</xsd:schema>";
            XmlSchemaSet s = new XmlSchemaSet();
            using (XmlReader r = XmlReader.Create(new StringReader(xsd)))
            {
                try
                {
                    s.Add(null, r);
                }
                catch (XmlSchemaException e)
                {
                    Assert.False(e.Message.EndsWith(".."));
                    return;
                }
            }

            Assert.True(false);
        }
    }
}