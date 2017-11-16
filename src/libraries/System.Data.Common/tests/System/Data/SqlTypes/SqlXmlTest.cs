// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

using Xunit;

namespace System.Data.Tests.SqlTypes
{
    public class SqlXmlTest
    {
        // Test constructor
        [Fact] // .ctor (Stream)
               //[Category ("NotDotNet")] // Name cannot begin with the '.' character, hexadecimal value 0x00. Line 1, position 2
        public void Constructor2_Stream_Unicode()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(xmlStr));
            SqlXml xmlSql = new SqlXml(stream);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(xmlStr, xmlSql.Value);
        }

        [Fact] // .ctor (Stream)
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

            try
            {
                string value = xmlSql.Value;
                Assert.False(true);
            }
            catch (SqlNullValueException)
            {
            }
        }

        [Fact] // .ctor (XmlReader)
        public void Constructor3()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            XmlReader xrdr = new XmlTextReader(new StringReader(xmlStr));
            SqlXml xmlSql = new SqlXml(xrdr);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(xmlStr, xmlSql.Value);
        }

        [Fact] // .ctor (XmlReader)
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

            try
            {
                string value = xmlSql.Value;
                Assert.False(true);
            }
            catch (SqlNullValueException)
            {
            }
        }

        [Fact]
        //[Category ("NotDotNet")] // Name cannot begin with the '.' character, hexadecimal value 0x00. Line 1, position 2
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

        [Fact]
        public void SqlXml_fromZeroLengthStream_CreateReaderTest()
        {
            MemoryStream stream = new MemoryStream();
            SqlXml xmlSql = new SqlXml(stream);

            XmlReader xrdr = xmlSql.CreateReader();

            Assert.Equal(false, xrdr.Read());
        }

        [Fact]
        public void SqlXml_fromZeroLengthXmlReader_CreateReaderTest_withFragment()
        {
            XmlReaderSettings xs = new XmlReaderSettings();
            xs.ConformanceLevel = ConformanceLevel.Fragment;

            XmlReader rdr = XmlReader.Create(new StringReader(string.Empty), xs);
            SqlXml xmlSql = new SqlXml(rdr);

            XmlReader xrdr = xmlSql.CreateReader();

            Assert.Equal(false, xrdr.Read());
        }

        [Fact]
        public void SqlXml_fromZeroLengthXmlReader_CreateReaderTest()
        {
            XmlReader rdr = new XmlTextReader(new StringReader(string.Empty));
            try
            {
                new SqlXml(rdr);
                Assert.False(true);
            }
            catch (XmlException)
            {
            }
        }

        [Fact]
        public void CreateReader_Stream_Null()
        {
            SqlXml xmlSql = new SqlXml((Stream)null);
            try
            {
                xmlSql.CreateReader();
                Assert.False(true);
            }
            catch (SqlNullValueException)
            {
            }
        }

        [Fact]
        public void CreateReader_XmlReader_Null()
        {
            SqlXml xmlSql = new SqlXml((XmlReader)null);
            try
            {
                xmlSql.CreateReader();
                Assert.False(true);
            }
            catch (SqlNullValueException)
            {
            }
        }
    }
}
