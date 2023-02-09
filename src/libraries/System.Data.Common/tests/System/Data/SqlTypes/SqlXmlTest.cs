// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.SqlTypes;
using System.IO;
using System.Text;
using System.Xml;

using Xunit;

namespace System.Data.Tests.SqlTypes
{
    public class SqlXmlTest
    {
        [Fact]
        public void Constructor2_Stream_Unicode()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(xmlStr));
            SqlXml xmlSql = new SqlXml(stream);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(xmlStr, xmlSql.Value);
        }

        [Fact]
        public void Constructor2_Stream_Empty()
        {
            MemoryStream ms = new MemoryStream();
            SqlXml xmlSql = new SqlXml(ms);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(string.Empty, xmlSql.Value);
        }

        [Fact]
        public void Constructor2_Stream_Null()
        {
            SqlXml xmlSql = new SqlXml((Stream)null);
            Assert.True(xmlSql.IsNull);

            Assert.Throws<SqlNullValueException>(() => xmlSql.Value);
        }

        [Fact]
        public void Constructor3()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            XmlReader xrdr = new XmlTextReader(new StringReader(xmlStr));
            SqlXml xmlSql = new SqlXml(xrdr);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(xmlStr, xmlSql.Value);
        }

        [Fact]
        public void Constructor3_XmlReader_Empty()
        {
            XmlReaderSettings xs = new XmlReaderSettings();
            xs.ConformanceLevel = ConformanceLevel.Fragment;
            XmlReader xrdr = XmlReader.Create(new StringReader(string.Empty), xs);
            SqlXml xmlSql = new SqlXml(xrdr);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(string.Empty, xmlSql.Value);
        }

        [Fact]
        public void Constructor3_XmlReader_Null()
        {
            SqlXml xmlSql = new SqlXml((XmlReader)null);
            Assert.True(xmlSql.IsNull);

            Assert.Throws<SqlNullValueException>(() => xmlSql.Value);
        }

        [Fact]
        public void CreateReader_Stream_Unicode()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(xmlStr));
            SqlXml xmlSql = new SqlXml(stream);

            XmlReader xrdr = xmlSql.CreateReader();
            xrdr.MoveToContent();

            Assert.Equal(xmlStr, xrdr.ReadOuterXml());
        }

        [Fact]
        public void SqlXml_fromXmlReader_CreateReaderTest()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            XmlReader rdr = new XmlTextReader(new StringReader(xmlStr));
            SqlXml xmlSql = new SqlXml(rdr);

            XmlReader xrdr = xmlSql.CreateReader();
            xrdr.MoveToContent();

            Assert.Equal(xmlStr, xrdr.ReadOuterXml());
        }

        [Theory]
        [InlineData("element_whitespace-text.xml")]
        [InlineData("root_qname.xml")]
        [InlineData("sample_ecommerce.xml")]
        [InlineData("sql_batch_request.xml")]
        [InlineData("sql_batch_response.xml")]
        [InlineData("sql_datatypes-1.xml")]
        [InlineData("sql_datatypes-2.xml")]
        [InlineData("sql_datatypes-3.xml")]
        [InlineData("xmlns-1.xml")]
        [InlineData("xmlns-2.xml")]
        [InlineData("xmlns-3.xml")]
        [InlineData("xmlns-4.xml")]
        [InlineData("comments_pis.xml")]
        [InlineData("element_content_growth.xml")]
        [InlineData("element_nested-1.xml")]
        [InlineData("element_nested-2.xml")]
        [InlineData("element_nested-3.xml")]
        [InlineData("element_single.xml")]
        [InlineData("element_stack_growth.xml")]
        [InlineData("element_tagname_growth.xml")]
        [InlineData("element_types.xml")]
        [InlineData("element_whitespace-modes.xml")]
        public void SqlXml_fromXmlReader_TextXml(string filename)
        {
            string filepath = Path.Combine("TestFiles/SqlXml/TextXml", filename);

            using FileStream xmlStream = new FileStream(filepath, FileMode.Open);
            SqlXml sqlXml = new SqlXml(xmlStream);

            // Reading XML stored as SQL Binary XML will result in using
            // the XmlTextReader implementation
            using XmlReader sqlXmlReader = sqlXml.CreateReader();

            // Read to the end to verify no exceptions are thrown
            while(sqlXmlReader.Read());
        }

        [Theory]
        [InlineData("element_whitespace-text.bmx")]
        [InlineData("root_qname.bmx")]
        [InlineData("sample_ecommerce.bmx")]
        [InlineData("sql_batch_request.bmx")]
        [InlineData("sql_batch_response.bmx")]
        [InlineData("sql_datatypes-1.bmx")]
        [InlineData("sql_datatypes-2.bmx")]
        [InlineData("sql_datatypes-3.bmx")]
        [InlineData("xmlns-1.bmx")]
        [InlineData("xmlns-2.bmx")]
        [InlineData("xmlns-3.bmx")]
        [InlineData("xmlns-4.bmx")]
        [InlineData("comments_pis.bmx")]
        [InlineData("element_content_growth.bmx")]
        [InlineData("element_nested-1.bmx")]
        [InlineData("element_nested-2.bmx")]
        [InlineData("element_nested-3.bmx")]
        [InlineData("element_single.bmx")]
        [InlineData("element_stack_growth.bmx")]
        [InlineData("element_tagname_growth.bmx")]
        [InlineData("element_types.bmx")]
        [InlineData("element_whitespace-modes.bmx")]
        public void SqlXml_fromXmlReader_SqlBinaryXml(string filename)
        {
            string filepath = Path.Combine("TestFiles/SqlXml/SqlBinaryXml", filename);

            using FileStream xmlStream = new FileStream(filepath, FileMode.Open);
            SqlXml sqlXml = new SqlXml(xmlStream);

            // Reading XML stored as SQL Binary XML will result in using
            // the XmlSqlBinaryReader implementation
            using XmlReader sqlXmlReader = sqlXml.CreateReader();

            // Read to the end to verify no exceptions are thrown
            while(sqlXmlReader.Read());
        }

        [Fact]
        public void SqlXml_fromZeroLengthStream_CreateReaderTest()
        {
            MemoryStream stream = new MemoryStream();
            SqlXml xmlSql = new SqlXml(stream);

            XmlReader xrdr = xmlSql.CreateReader();

            Assert.False(xrdr.Read());
        }

        [Fact]
        public void SqlXml_fromZeroLengthXmlReader_CreateReaderTest_withFragment()
        {
            XmlReaderSettings xs = new XmlReaderSettings();
            xs.ConformanceLevel = ConformanceLevel.Fragment;

            XmlReader rdr = XmlReader.Create(new StringReader(string.Empty), xs);
            SqlXml xmlSql = new SqlXml(rdr);

            XmlReader xrdr = xmlSql.CreateReader();

            Assert.False(xrdr.Read());
        }

        [Fact]
        public void SqlXml_fromZeroLengthXmlReader_CreateReaderTest()
        {
            XmlReader rdr = new XmlTextReader(new StringReader(string.Empty));

            Assert.Throws<XmlException>(() => new SqlXml(rdr));
        }

        [Fact]
        public void CreateReader_Stream_Null()
        {
            SqlXml xmlSql = new SqlXml((Stream)null);

            Assert.Throws<SqlNullValueException>(() => xmlSql.CreateReader());
        }

        [Fact]
        public void CreateReader_XmlReader_Null()
        {
            SqlXml xmlSql = new SqlXml((XmlReader)null);

            Assert.Throws<SqlNullValueException>(() => xmlSql.CreateReader());
        }
    }
}
