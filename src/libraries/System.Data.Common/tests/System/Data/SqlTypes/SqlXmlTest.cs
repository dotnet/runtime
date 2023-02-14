// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using Xunit;

namespace System.Data.Tests.SqlTypes
{
    public class SqlXmlTest
    {
        [Fact]
        public void Constructor_Stream_Unicode()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes(xmlStr));
            SqlXml xmlSql = new SqlXml(stream);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(xmlStr, xmlSql.Value);
        }

        [Fact]
        public void Constructor_Stream_Empty()
        {
            MemoryStream ms = new MemoryStream();
            SqlXml xmlSql = new SqlXml(ms);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(string.Empty, xmlSql.Value);
        }

        [Fact]
        public void Constructor_Stream_Null()
        {
            SqlXml xmlSql = new SqlXml((Stream)null);
            Assert.True(xmlSql.IsNull);

            Assert.Throws<SqlNullValueException>(() => xmlSql.Value);
        }

        [Fact]
        public void Constructor_StringReader()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            XmlReader xrdr = new XmlTextReader(new StringReader(xmlStr));
            SqlXml xmlSql = new SqlXml(xrdr);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(xmlStr, xmlSql.Value);
        }

        [Fact]
        public void Constructor_XmlReader_Empty()
        {
            XmlReaderSettings xs = new XmlReaderSettings();
            xs.ConformanceLevel = ConformanceLevel.Fragment;
            XmlReader xrdr = XmlReader.Create(new StringReader(string.Empty), xs);
            SqlXml xmlSql = new SqlXml(xrdr);
            Assert.False(xmlSql.IsNull);
            Assert.Equal(string.Empty, xmlSql.Value);
        }

        [Fact]
        public void Constructor_XmlReader_Null()
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
        public void CreateReader_XmlTextReader_CanReadContent()
        {
            string xmlStr = "<Employee><FirstName>Varadhan</FirstName><LastName>Veerapuram</LastName></Employee>";
            XmlReader rdr = new XmlTextReader(new StringReader(xmlStr));
            SqlXml xmlSql = new SqlXml(rdr);

            XmlReader xrdr = xmlSql.CreateReader();
            xrdr.MoveToContent();

            Assert.Equal(xmlStr, xrdr.ReadOuterXml());
        }

        public static class CreateReader_TestFiles
        {
            private static TheoryData<string, string> _filesAndBaselines;

            // The test files are made available through the System.Data.Common.TestData package included in dotnet/runtime-assets
            private static void EnsureFileList()
            {
                if (_filesAndBaselines is null)
                {
                    IEnumerable<string> text = Directory.EnumerateFiles(Path.Combine("SqlXml.CreateReader", "Baseline-Text"), "*.xml");
                    IEnumerable<string> binary = Directory.EnumerateFiles(Path.Combine("SqlXml.CreateReader", "SqlBinaryXml"), "*.bmx");

                    // Make sure that we found our test files; otherwise the theories would succeed without validating anything
                    Assert.NotEmpty(text);
                    Assert.NotEmpty(binary);

                    TheoryData<string, string> filesAndBaselines = new TheoryData<string, string>();

                    // Use the Text XML files as their own baselines
                    filesAndBaselines.Append(text.Select(f => new string[] { TextXmlFileName(f), TextXmlFileName(f) }).ToArray());

                    // Use the matching Text XML files as the baselines for the SQL Binary XML files
                    filesAndBaselines.Append(binary
                        .Select(Path.GetFileNameWithoutExtension)
                        .Intersect(text.Select(Path.GetFileNameWithoutExtension))
                        .Select(f => new string[] { SqlBinaryXmlFileName(f), TextXmlFileName(f) }).ToArray());

                    _filesAndBaselines = filesAndBaselines;

                    string TextXmlFileName(string name) => Path.Combine("SqlXml.CreateReader", "Baseline-Text", $"{name}.xml");
                    string SqlBinaryXmlFileName(string name) => Path.Combine("SqlXml.CreateReader", "SqlBinaryXml", $"{name}.bmx");
                }
            }

            public static TheoryData<string, string> FilesAndBaselines
            {
                get
                {
                    EnsureFileList();
                    return _filesAndBaselines;
                }
            }

            public static string ReadAllXml(XmlReader reader)
            {
                using StringWriter writer = new StringWriter();
                using XmlWriter xmlWriter = new XmlTextWriter(writer);

                while (reader.Read()) xmlWriter.WriteNode(reader, false);

                return writer.ToString();
            }
        }

        [Theory]
        [MemberData(nameof(CreateReader_TestFiles.FilesAndBaselines), MemberType = typeof(CreateReader_TestFiles))]
        public void CreateReader_TestAgainstBaseline(string testFile, string baselineFile)
        {
            // Get our expected output by using XmlReader directly
            using XmlReader baselineReader = XmlReader.Create(baselineFile);
            string expected = CreateReader_TestFiles.ReadAllXml(baselineReader);

            // Now produce the actual output through SqlXml.CreateReader
            using FileStream xmlStream = new FileStream(testFile, FileMode.Open);
            SqlXml sqlXml = new SqlXml(xmlStream);

            // When the input is text, an XmlTextReader will be returned
            // When the input is SQL Binary XML, an XmlSqlBinaryReader will be returned
            using XmlReader actualReader = sqlXml.CreateReader();
            string actual = CreateReader_TestFiles.ReadAllXml(actualReader);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SqlXml_FromZeroLengthStream_CreateReaderTest()
        {
            MemoryStream stream = new MemoryStream();
            SqlXml xmlSql = new SqlXml(stream);

            XmlReader xrdr = xmlSql.CreateReader();

            Assert.False(xrdr.Read());
        }

        [Fact]
        public void SqlXml_FromZeroLengthXmlReader_CreateReaderTest_withFragment()
        {
            XmlReaderSettings xs = new XmlReaderSettings();
            xs.ConformanceLevel = ConformanceLevel.Fragment;

            XmlReader rdr = XmlReader.Create(new StringReader(string.Empty), xs);
            SqlXml xmlSql = new SqlXml(rdr);

            XmlReader xrdr = xmlSql.CreateReader();

            Assert.False(xrdr.Read());
        }

        [Fact]
        public void SqlXml_FromZeroLengthXmlReader_CreateReaderTest()
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
