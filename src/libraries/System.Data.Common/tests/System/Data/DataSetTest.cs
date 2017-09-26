// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) Copyright 2002 Ville Palo
// (C) Copyright 2003 Martin Willemoes Hansen
// (C) 2005 Mainsoft Corporation (http://www.mainsoft.com)
// Copyright 2011 Xamarin Inc.
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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
//


using Xunit;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.IO;
using System.Data.SqlTypes;
using System.Globalization;
using System.Text;
using System.Diagnostics;

namespace System.Data.Tests
{
    public class DataSetTest : RemoteExecutorTestBase
    {
        public DataSetTest()
        {
            MyDataSet.count = 0;
        }

        [Fact]
        public void Properties()
        {
            var ds = new DataSet();
            Assert.Equal(string.Empty, ds.Namespace);
            ds.Namespace = null; // setting null == setting ""
            Assert.Equal(string.Empty, ds.Namespace);

            Assert.Equal(string.Empty, ds.Prefix);
            ds.Prefix = null; // setting null == setting ""
            Assert.Equal(string.Empty, ds.Prefix);
        }

        [Fact]
        public void ReadXmlSchema()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(DataProvider.own_schema));

            Assert.Equal(2, ds.Tables.Count);
            DataTable Table = ds.Tables[0];
            Assert.Equal("test_table", Table.TableName);
            Assert.Equal("", Table.Namespace);
            Assert.Equal(2, Table.Columns.Count);
            Assert.Equal(0, Table.Rows.Count);
            Assert.False(Table.CaseSensitive);
            Assert.Equal(1, Table.Constraints.Count);
            Assert.Equal("", Table.Prefix);

            Constraint cons = Table.Constraints[0];
            Assert.Equal("Constraint1", cons.ConstraintName.ToString());
            Assert.Equal("Constraint1", cons.ToString());

            DataColumn column = Table.Columns[0];
            Assert.True(column.AllowDBNull);
            Assert.False(column.AutoIncrement);
            Assert.Equal(0L, column.AutoIncrementSeed);
            Assert.Equal(1L, column.AutoIncrementStep);
            Assert.Equal("test", column.Caption);
            Assert.Equal("Element", column.ColumnMapping.ToString());
            Assert.Equal("first", column.ColumnName);
            Assert.Equal("System.String", column.DataType.ToString());
            Assert.Equal("test_default_value", column.DefaultValue.ToString());
            Assert.False(column.DesignMode);
            Assert.Equal("", column.Expression);
            Assert.Equal(100, column.MaxLength);
            Assert.Equal("", column.Namespace);
            Assert.Equal(0, column.Ordinal);
            Assert.Equal("", column.Prefix);
            Assert.False(column.ReadOnly);
            Assert.True(column.Unique);

            DataColumn column2 = Table.Columns[1];
            Assert.True(column2.AllowDBNull);
            Assert.False(column2.AutoIncrement);
            Assert.Equal(0L, column2.AutoIncrementSeed);
            Assert.Equal(1L, column2.AutoIncrementStep);
            Assert.Equal("second", column2.Caption);
            Assert.Equal("Element", column2.ColumnMapping.ToString());
            Assert.Equal("second", column2.ColumnName);
            Assert.Equal("System.Data.SqlTypes.SqlGuid", column2.DataType.ToString());
            Assert.Equal(SqlGuid.Null, column2.DefaultValue);
            Assert.False(column2.DesignMode);
            Assert.Equal("", column2.Expression);
            Assert.Equal(-1, column2.MaxLength);
            Assert.Equal("", column2.Namespace);
            Assert.Equal(1, column2.Ordinal);
            Assert.Equal("", column2.Prefix);
            Assert.False(column2.ReadOnly);
            Assert.False(column2.Unique);

            DataTable Table2 = ds.Tables[1];
            Assert.Equal("second_test_table", Table2.TableName);
            Assert.Equal("", Table2.Namespace);
            Assert.Equal(1, Table2.Columns.Count);
            Assert.Equal(0, Table2.Rows.Count);
            Assert.False(Table2.CaseSensitive);
            Assert.Equal(1, Table2.Constraints.Count);
            Assert.Equal("", Table2.Prefix);

            DataColumn column3 = Table2.Columns[0];
            Assert.True(column3.AllowDBNull);
            Assert.False(column3.AutoIncrement);
            Assert.Equal(0L, column3.AutoIncrementSeed);
            Assert.Equal(1L, column3.AutoIncrementStep);
            Assert.Equal("second_first", column3.Caption);
            Assert.Equal("Element", column3.ColumnMapping.ToString());
            Assert.Equal("second_first", column3.ColumnName);
            Assert.Equal("System.String", column3.DataType.ToString());
            Assert.Equal("default_value", column3.DefaultValue.ToString());
            Assert.False(column3.DesignMode);
            Assert.Equal("", column3.Expression);
            Assert.Equal(100, column3.MaxLength);
            Assert.Equal("", column3.Namespace);
            Assert.Equal(0, column3.Ordinal);
            Assert.Equal("", column3.Prefix);
            Assert.False(column3.ReadOnly);
            Assert.True(column3.Unique);
        }

        [Fact]
        public void OwnWriteXmlSchema()
        {
            DataSet ds = new DataSet("test_dataset");
            DataTable table = new DataTable("test_table");
            DataColumn column = new DataColumn("first", typeof(string));
            column.AllowDBNull = true;
            column.DefaultValue = "test_default_value";
            column.MaxLength = 100;
            column.Caption = "test";
            column.Unique = true;
            table.Columns.Add(column);

            DataColumn column2 = new DataColumn("second", typeof(SqlGuid));
            column2.ColumnMapping = MappingType.Element;
            table.Columns.Add(column2);
            ds.Tables.Add(table);

            DataTable table2 = new DataTable("second_test_table");
            DataColumn column3 = new DataColumn("second_first", typeof(string));
            column3.AllowDBNull = true;
            column3.DefaultValue = "default_value";
            column3.MaxLength = 100;
            column3.Unique = true;
            table2.Columns.Add(column3);
            ds.Tables.Add(table2);

            TextWriter writer = new StringWriter();
            ds.WriteXmlSchema(writer);

            string TextString = DataSetAssertion.GetNormalizedSchema(writer.ToString());
            //			string TextString = writer.ToString ();

            string substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16\"?>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            // This is original DataSet.WriteXmlSchema() output
            //			Assert.Equal ("<xs:schema id=\"test_dataset\" xmlns=\"\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\">", substring);
            Assert.Equal("<xs:schema id=\"test_dataset\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            // This is original DataSet.WriteXmlSchema() output
            //			Assert.Equal ("  <xs:element name=\"test_dataset\" msdata:IsDataSet=\"true\" msdata:Locale=\"fi-FI\">", substring);
            Assert.Equal("  <xs:element msdata:IsDataSet=\"true\" msdata:UseCurrentLocale=\"true\" name=\"test_dataset\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    <xs:complexType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <xs:choice maxOccurs=\"unbounded\" minOccurs=\"0\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("        <xs:element name=\"test_table\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("          <xs:complexType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("            <xs:sequence>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            // This is original DataSet.WriteXmlSchema() output
            //			Assert.Equal ("              <xs:element name=\"first\" msdata:Caption=\"test\" default=\"test_default_value\" minOccurs=\"0\">", substring);
            Assert.Equal("              <xs:element default=\"test_default_value\" minOccurs=\"0\" msdata:Caption=\"test\" name=\"first\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                <xs:simpleType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                  <xs:restriction base=\"xs:string\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                    <xs:maxLength value=\"100\" />", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                  </xs:restriction>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                </xs:simpleType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("              </xs:element>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            // This is original DataSet.WriteXmlSchema() output
            Assert.Equal("              <xs:element minOccurs=\"0\" msdata:DataType=\"System.Data.SqlTypes.SqlGuid\" name=\"second\" type=\"xs:string\" />", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("            </xs:sequence>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("          </xs:complexType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("        </xs:element>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("        <xs:element name=\"second_test_table\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("          <xs:complexType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("            <xs:sequence>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            // This is original DataSet.WriteXmlSchema() output
            //			Assert.Equal ("              <xs:element name=\"second_first\" default=\"default_value\" minOccurs=\"0\">", substring);
            Assert.Equal("              <xs:element default=\"default_value\" minOccurs=\"0\" name=\"second_first\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                <xs:simpleType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                  <xs:restriction base=\"xs:string\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                    <xs:maxLength value=\"100\" />", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                  </xs:restriction>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("                </xs:simpleType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("              </xs:element>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("            </xs:sequence>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("          </xs:complexType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("        </xs:element>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      </xs:choice>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    </xs:complexType>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    <xs:unique name=\"Constraint1\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <xs:selector xpath=\".//test_table\" />", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <xs:field xpath=\"first\" />", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    </xs:unique>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            // This is original DataSet.WriteXmlSchema() output
            //			Assert.Equal ("    <xs:unique name=\"second_test_table_Constraint1\" msdata:ConstraintName=\"Constraint1\">", substring);
            Assert.Equal("    <xs:unique msdata:ConstraintName=\"Constraint1\" name=\"second_test_table_Constraint1\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <xs:selector xpath=\".//second_test_table\" />", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <xs:field xpath=\"second_first\" />", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    </xs:unique>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("  </xs:element>", substring);
            Assert.Equal("</xs:schema>", TextString);
        }

        [Fact]
        public void ReadWriteXml()
        {
            var ds = new DataSet();
            ds.ReadXml(new StringReader(DataProvider.region));
            TextWriter writer = new StringWriter();
            ds.WriteXml(writer);

            string TextString = writer.ToString();
            string substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("<Root>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("  <Region>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    <RegionID>1</RegionID>", substring);
            // Here the end of line is text markup "\n"
            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    <RegionDescription>Eastern", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("               </RegionDescription>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("  </Region>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("  <Region>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    <RegionID>2</RegionID>", substring);

            // Here the end of line is text markup "\n"
            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    <RegionDescription>Western", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("               </RegionDescription>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("  </Region>", substring);

            Assert.Equal("</Root>", TextString);
        }

        [Fact]
        public void ReadWriteXmlDiffGram()
        {
            var ds = new DataSet();
            // It is not a diffgram, so no data loading should be done.
            ds.ReadXml(new StringReader(DataProvider.region), XmlReadMode.DiffGram);
            TextWriter writer = new StringWriter();
            ds.WriteXml(writer);

            string TextString = writer.ToString();
            Assert.Equal("<NewDataSet />", TextString);

            ds.WriteXml(writer, XmlWriteMode.DiffGram);
            TextString = writer.ToString();

            Assert.Equal("<NewDataSet /><diffgr:diffgram xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" xmlns:diffgr=\"urn:schemas-microsoft-com:xml-diffgram-v1\" />", TextString);


            ds = new DataSet();
            ds.ReadXml(new StringReader(DataProvider.region));
            DataTable table = ds.Tables["Region"];
            table.Rows[0][0] = "64";
            ds.ReadXml(new StringReader(DataProvider.region), XmlReadMode.DiffGram);
            ds.WriteXml(writer, XmlWriteMode.DiffGram);

            TextString = writer.ToString();
            string substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("<NewDataSet /><diffgr:diffgram xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" xmlns:diffgr=\"urn:schemas-microsoft-com:xml-diffgram-v1\" /><diffgr:diffgram xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" xmlns:diffgr=\"urn:schemas-microsoft-com:xml-diffgram-v1\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("  <Root>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    <Region diffgr:id=\"Region1\" msdata:rowOrder=\"0\" diffgr:hasChanges=\"inserted\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <RegionID>64</RegionID>", substring);

            // not '\n' but literal '\n'
            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <RegionDescription>Eastern", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("               </RegionDescription>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    </Region>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    <Region diffgr:id=\"Region2\" msdata:rowOrder=\"1\" diffgr:hasChanges=\"inserted\">", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <RegionID>2</RegionID>", substring);

            // not '\n' but literal '\n'
            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("      <RegionDescription>Western", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("               </RegionDescription>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("    </Region>", substring);

            substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
            TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
            Assert.Equal("  </Root>", substring);

            Assert.Equal("</diffgr:diffgram>", TextString);
        }

        [Fact]
        public void WriteXmlSchema()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("fi-FI");

                var ds = new DataSet();
                ds.ReadXml(new StringReader(DataProvider.region));
                TextWriter writer = new StringWriter();
                ds.WriteXmlSchema(writer);


                string TextString = DataSetAssertion.GetNormalizedSchema(writer.ToString());
                // string TextString = writer.ToString ();

                string substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("<?xml version=\"1.0\" encoding=\"utf-16\"?>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                // This is original DataSet.WriteXmlSchema() output
                // Assert.Equal ("<xs:schema id=\"Root\" xmlns=\"\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\">", substring);
                Assert.Equal("<xs:schema id=\"Root\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("  <xs:element msdata:IsDataSet=\"true\" msdata:Locale=\"en-US\" name=\"Root\">", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("    <xs:complexType>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("      <xs:choice maxOccurs=\"unbounded\" minOccurs=\"0\">", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("        <xs:element name=\"Region\">", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("          <xs:complexType>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("            <xs:sequence>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                // This is original DataSet.WriteXmlSchema() output
                // Assert.Equal ("              <xs:element name=\"RegionID\" type=\"xs:string\" minOccurs=\"0\" />", substring);
                Assert.Equal("              <xs:element minOccurs=\"0\" name=\"RegionID\" type=\"xs:string\" />", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                // This is original DataSet.WriteXmlSchema() output
                // Assert.Equal ("              <xs:element name=\"RegionDescription\" type=\"xs:string\" minOccurs=\"0\" />", substring);
                Assert.Equal("              <xs:element minOccurs=\"0\" name=\"RegionDescription\" type=\"xs:string\" />", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("            </xs:sequence>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("          </xs:complexType>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("        </xs:element>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("      </xs:choice>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("    </xs:complexType>", substring);

                substring = TextString.Substring(0, TextString.IndexOfAny(new[] { '\r', '\n' }));
                TextString = TextString.Substring(TextString.IndexOf('\n') + 1);
                Assert.Equal("  </xs:element>", substring);

                Assert.Equal("</xs:schema>", TextString);

                return SuccessExitCode;
            }).Dispose();

        }

        [Fact]
        public void IgnoreColumnEmptyNamespace()
        {
            DataColumn col = new DataColumn("TEST");
            col.Namespace = "urn:foo";
            DataSet ds = new DataSet("DS");
            ds.Namespace = "urn:foo";
            DataTable dt = new DataTable("tab");
            ds.Tables.Add(dt);
            dt.Columns.Add(col);
            dt.Rows.Add(new object[] { "test" });
            StringWriter sw = new StringWriter();
            ds.WriteXml(new XmlTextWriter(sw));
            string xml = @"<DS xmlns=""urn:foo""><tab><TEST>test</TEST></tab></DS>";
            Assert.Equal(xml, sw.ToString());
        }

        [Fact]
        public void SerializeDataSet()
        {
            // see GetReady() for current culture

            string xml = "<?xml version='1.0' encoding='utf-16'?><DataSet><xs:schema id='DS' xmlns='' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'><xs:element name='DS' msdata:IsDataSet='true' " +
              "msdata:UseCurrentLocale='true'"
              + "><xs:complexType><xs:choice minOccurs='0' maxOccurs='unbounded' /></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:msdata='urn:schemas-microsoft-com:xml-msdata' xmlns:diffgr='urn:schemas-microsoft-com:xml-diffgram-v1' /></DataSet>";
            var ds = new DataSet();
            ds.DataSetName = "DS";
            XmlSerializer ser = new XmlSerializer(typeof(DataSet));
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            xw.QuoteChar = '\'';
            ser.Serialize(xw, ds);

            string result = sw.ToString();
            Assert.Equal(result.Replace("\r\n", "\n"), xml.Replace("\r\n", "\n"));
        }

        [Fact]
        public void SerializeDataSet2()
        {
            DataSet quota = new DataSet("Quota");

            // Dimension
            DataTable dt = new DataTable("Dimension");
            quota.Tables.Add(dt);

            dt.Columns.Add("Number", typeof(int));
            dt.Columns["Number"].AllowDBNull = false;
            dt.Columns["Number"].ColumnMapping = MappingType.Attribute;

            dt.Columns.Add("Title", typeof(string));
            dt.Columns["Title"].AllowDBNull = false;
            dt.Columns["Title"].ColumnMapping =
            MappingType.Attribute;

            dt.Rows.Add(new object[] { 0, "Hospitals" });
            dt.Rows.Add(new object[] { 1, "Doctors" });

            dt.Constraints.Add("PK_Dimension", dt.Columns["Number"], true);

            quota.AcceptChanges();

            XmlSerializer ser = new XmlSerializer(quota.GetType());

            StringWriter sw = new StringWriter();
            ser.Serialize(sw, quota);

            DataSet ds = (DataSet)ser.Deserialize(new StringReader(sw.ToString()));
        }

        public void SerializeDataSet3()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?><DataSet><xs:schema id=""Example"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""><xs:element name=""Example"" msdata:IsDataSet=""true""><xs:complexType><xs:choice maxOccurs=""unbounded"" minOccurs=""0""><xs:element name=""Packages""><xs:complexType><xs:attribute name=""ID"" type=""xs:int"" use=""required"" /><xs:attribute name=""ShipDate"" type=""xs:dateTime"" /><xs:attribute name=""Message"" type=""xs:string"" /><xs:attribute name=""Handlers"" type=""xs:int"" /></xs:complexType></xs:element></xs:choice></xs:complexType></xs:element></xs:schema><diffgr:diffgram xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"" xmlns:diffgr=""urn:schemas-microsoft-com:xml-diffgram-v1""><Example><Packages diffgr:id=""Packages1"" msdata:rowOrder=""0"" ID=""0"" ShipDate=""2004-10-11T17:46:18.6962302-05:00"" Message=""Received with no breakage!"" Handlers=""3"" /><Packages diffgr:id=""Packages2"" msdata:rowOrder=""1"" ID=""1"" /></Example></diffgr:diffgram></DataSet>";

            DataSet ds = new DataSet("Example");

            // Add a DataTable
            DataTable dt = new DataTable("Packages");
            ds.Tables.Add(dt);

            // Add an ID DataColumn w/ ColumnMapping = MappingType.Attribute
            dt.Columns.Add(new DataColumn("ID", typeof(int), "",
                MappingType.Attribute));
            dt.Columns["ID"].AllowDBNull = false;

            // Add a nullable DataColumn w/ ColumnMapping = MappingType.Attribute
            dt.Columns.Add(new DataColumn("ShipDate",
                typeof(DateTime), "", MappingType.Attribute));
            dt.Columns["ShipDate"].AllowDBNull = true;

            // Add a nullable DataColumn w/ ColumnMapping = MappingType.Attribute
            dt.Columns.Add(new DataColumn("Message",
                typeof(string), "", MappingType.Attribute));
            dt.Columns["Message"].AllowDBNull = true;

            // Add a nullable DataColumn w/ ColumnMapping = MappingType.Attribute
            dt.Columns.Add(new DataColumn("Handlers",
                typeof(int), "", MappingType.Attribute));
            dt.Columns["Handlers"].AllowDBNull = true;

            // Add a non-null value row
            DataRow newRow = dt.NewRow();
            newRow["ID"] = 0;
            newRow["ShipDate"] = DateTime.Now;
            newRow["Message"] = "Received with no breakage!";
            newRow["Handlers"] = 3;
            dt.Rows.Add(newRow);

            // Add a null value row
            newRow = dt.NewRow();
            newRow["ID"] = 1;
            newRow["ShipDate"] = DBNull.Value;
            newRow["Message"] = DBNull.Value;
            newRow["Handlers"] = DBNull.Value;
            dt.Rows.Add(newRow);

            ds.AcceptChanges();

            XmlSerializer ser = new XmlSerializer(ds.GetType());
            StringWriter sw = new StringWriter();
            ser.Serialize(sw, ds);

            string result = sw.ToString();

            Assert.Equal(xml, result);
        }

        [Fact]
        public void DeserializeDataSet()
        {
            string xml = @"<DataSet>
  <diffgr:diffgram xmlns:msdata='urn:schemas-microsoft-com:xml-msdata' xmlns:diffgr='urn:schemas-microsoft-com:xml-diffgram-v1'>
    <Quota>
      <Dimension diffgr:id='Dimension1' msdata:rowOrder='0' Number='0' Title='Hospitals' />
      <Dimension diffgr:id='Dimension2' msdata:rowOrder='1' Number='1' Title='Doctors' />
    </Quota>
  </diffgr:diffgram>
</DataSet>";
            XmlSerializer ser = new XmlSerializer(typeof(DataSet));
            ser.Deserialize(new XmlTextReader(
                xml, XmlNodeType.Document, null));
        }

        /* To be added
		[Fact]
		public void WriteDiffReadAutoWriteSchema ()
		{
			DataSet ds = new DataSet ();
			ds.Tables.Add ("Table1");
			ds.Tables.Add ("Table2");
			ds.Tables [0].Columns.Add ("Column1_1");
			ds.Tables [0].Columns.Add ("Column1_2");
			ds.Tables [0].Columns.Add ("Column1_3");
			ds.Tables [1].Columns.Add ("Column2_1");
			ds.Tables [1].Columns.Add ("Column2_2");
			ds.Tables [1].Columns.Add ("Column2_3");
			ds.Tables [0].Rows.Add (new object [] {"ppp", "www", "xxx"});

			// save as diffgram
			StringWriter sw = new StringWriter ();
			ds.WriteXml (sw, XmlWriteMode.DiffGram);
			string xml = sw.ToString ();
			string result = new StreamReader ("Test/System.Data/DataSetReadXmlTest1.xml", Encoding.ASCII).ReadToEnd ();
			Assert.Equal (result, xml);

			// load diffgram above
			ds.ReadXml (new StringReader (sw.ToString ()));
			sw = new StringWriter ();
			ds.WriteXml (sw, XmlWriteMode.WriteSchema);
			xml = sw.ToString ();
			result = new StreamReader ("Test/System.Data/DataSetReadXmlTest2.xml", Encoding.ASCII).ReadToEnd ();
			Assert.Equal (result, xml);
		}
		*/

        [Fact]
        public void CloneCopy()
        {
            DataTable table = new DataTable("pTable");
            DataTable table1 = new DataTable("cTable");
            DataSet set = new DataSet();

            set.Tables.Add(table);
            set.Tables.Add(table1);

            DataColumn col = new DataColumn();
            col.ColumnName = "Id";
            col.DataType = Type.GetType("System.Int32");
            table.Columns.Add(col);
            UniqueConstraint uc = new UniqueConstraint("UK1", table.Columns[0]);
            table.Constraints.Add(uc);

            col = new DataColumn();
            col.ColumnName = "Name";
            col.DataType = Type.GetType("System.String");
            table.Columns.Add(col);

            col = new DataColumn();
            col.ColumnName = "Id";
            col.DataType = Type.GetType("System.Int32");
            table1.Columns.Add(col);

            col = new DataColumn();
            col.ColumnName = "Name";
            col.DataType = Type.GetType("System.String");
            table1.Columns.Add(col);
            ForeignKeyConstraint fc = new ForeignKeyConstraint("FK1", table.Columns[0], table1.Columns[0]);
            table1.Constraints.Add(fc);


            DataRow row = table.NewRow();

            row["Id"] = 147;
            row["name"] = "Row1";
            row.RowError = "Error#1";
            table.Rows.Add(row);

            // Set column to RO as commonly used by auto-increment fields.
            // ds.Copy() has to omit the RO check when cloning DataRows 
            table.Columns["Id"].ReadOnly = true;

            row = table1.NewRow();
            row["Id"] = 147;
            row["Name"] = "Row1";
            table1.Rows.Add(row);

            //Setting properties of DataSet
            set.CaseSensitive = true;
            set.DataSetName = "My DataSet";
            set.EnforceConstraints = false;
            set.Namespace = "Namespace#1";
            set.Prefix = "Prefix:1";
            DataRelation dr = new DataRelation("DR", table.Columns[0], table1.Columns[0]);
            set.Relations.Add(dr);
            set.ExtendedProperties.Add("TimeStamp", DateTime.Now);
            CultureInfo cultureInfo = new CultureInfo("ar-SA");
            set.Locale = cultureInfo;

            //Testing Copy ()
            DataSet copySet = set.Copy();
            Assert.Equal(set.CaseSensitive, copySet.CaseSensitive);
            Assert.Equal(set.DataSetName, copySet.DataSetName);
            Assert.Equal(set.EnforceConstraints, copySet.EnforceConstraints);
            Assert.Equal(set.HasErrors, copySet.HasErrors);
            Assert.Equal(set.Namespace, copySet.Namespace);
            Assert.Equal(set.Prefix, copySet.Prefix);
            Assert.Equal(set.Relations.Count, copySet.Relations.Count);
            Assert.Equal(set.Tables.Count, copySet.Tables.Count);
            Assert.Equal(set.ExtendedProperties["TimeStamp"], copySet.ExtendedProperties["TimeStamp"]);
            for (int i = 0; i < copySet.Tables.Count; i++)
            {
                Assert.Equal(set.Tables[i].Rows.Count, copySet.Tables[i].Rows.Count);
                Assert.Equal(set.Tables[i].Columns.Count, copySet.Tables[i].Columns.Count);
            }
            //Testing Clone ()
            copySet = set.Clone();
            Assert.Equal(set.CaseSensitive, copySet.CaseSensitive);
            Assert.Equal(set.DataSetName, copySet.DataSetName);
            Assert.Equal(set.EnforceConstraints, copySet.EnforceConstraints);
            Assert.False(copySet.HasErrors);
            Assert.Equal(set.Namespace, copySet.Namespace);
            Assert.Equal(set.Prefix, copySet.Prefix);
            Assert.Equal(set.Relations.Count, copySet.Relations.Count);
            Assert.Equal(set.Tables.Count, copySet.Tables.Count);
            Assert.Equal(set.ExtendedProperties["TimeStamp"], copySet.ExtendedProperties["TimeStamp"]);
            for (int i = 0; i < copySet.Tables.Count; i++)
            {
                Assert.Equal(0, copySet.Tables[i].Rows.Count);
                Assert.Equal(set.Tables[i].Columns.Count, copySet.Tables[i].Columns.Count);
            }
        }

        [Fact]
        public void CloneCopy2()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(DataProvider.store));
            ds.Clone();
        }

        [Fact]
        public void CloneCopy_TestForeignKeyConstraints()
        {
            DataTable dirTable = new DataTable("Directories");

            DataColumn dir_UID = new DataColumn("UID", typeof(int));
            dir_UID.Unique = true;
            dir_UID.AllowDBNull = false;

            dirTable.Columns.Add(dir_UID);

            // Build a simple Files table
            DataTable fileTable = new DataTable("Files");

            DataColumn file_DirID = new DataColumn("DirectoryID", typeof(int));
            file_DirID.Unique = false;
            file_DirID.AllowDBNull = false;

            fileTable.Columns.Add(file_DirID);

            // Build the DataSet
            DataSet ds = new DataSet("TestDataset");
            ds.Tables.Add(dirTable);
            ds.Tables.Add(fileTable);

            // Add a foreign key constraint
            DataColumn[] parentColumns = new DataColumn[1];
            parentColumns[0] = ds.Tables["Directories"].Columns["UID"];

            DataColumn[] childColumns = new DataColumn[1];
            childColumns[0] = ds.Tables["Files"].Columns["DirectoryID"];

            ForeignKeyConstraint fk = new ForeignKeyConstraint("FK_Test", parentColumns, childColumns);
            ds.Tables["Files"].Constraints.Add(fk);
            ds.EnforceConstraints = true;

            Assert.Equal(1, ds.Tables["Directories"].Constraints.Count);
            Assert.Equal(1, ds.Tables["Files"].Constraints.Count);

            // check clone works fine
            DataSet cloned_ds = ds.Clone();
            Assert.Equal(1, cloned_ds.Tables["Directories"].Constraints.Count);
            Assert.Equal(1, cloned_ds.Tables["Files"].Constraints.Count);

            ForeignKeyConstraint clonedFk = (ForeignKeyConstraint)cloned_ds.Tables["Files"].Constraints[0];
            Assert.Equal("FK_Test", clonedFk.ConstraintName);
            Assert.Equal(1, clonedFk.Columns.Length);
            Assert.Equal("DirectoryID", clonedFk.Columns[0].ColumnName);

            UniqueConstraint clonedUc = (UniqueConstraint)cloned_ds.Tables["Directories"].Constraints[0];
            UniqueConstraint origUc = (UniqueConstraint)ds.Tables["Directories"].Constraints[0];
            Assert.Equal(origUc.ConstraintName, clonedUc.ConstraintName);
            Assert.Equal(1, clonedUc.Columns.Length);
            Assert.Equal("UID", clonedUc.Columns[0].ColumnName);

            // check copy works fine
            DataSet copy_ds = ds.Copy();
            Assert.Equal(1, copy_ds.Tables["Directories"].Constraints.Count);
            Assert.Equal(1, copy_ds.Tables["Files"].Constraints.Count);

            ForeignKeyConstraint copyFk = (ForeignKeyConstraint)copy_ds.Tables["Files"].Constraints[0];
            Assert.Equal("FK_Test", copyFk.ConstraintName);
            Assert.Equal(1, copyFk.Columns.Length);
            Assert.Equal("DirectoryID", copyFk.Columns[0].ColumnName);

            UniqueConstraint copyUc = (UniqueConstraint)copy_ds.Tables["Directories"].Constraints[0];
            origUc = (UniqueConstraint)ds.Tables["Directories"].Constraints[0];
            Assert.Equal(origUc.ConstraintName, copyUc.ConstraintName);
            Assert.Equal(1, copyUc.Columns.Length);
            Assert.Equal("UID", copyUc.Columns[0].ColumnName);
        }

        [Fact]
        public void WriteNestedTableXml()
        {
            string xml = @"<NewDataSet>
  <tab1>
    <ident>1</ident>
    <name>hoge</name>
    <tab2>
      <timestamp>2004-05-05</timestamp>
    </tab2>
  </tab1>
  <tab1>
    <ident>2</ident>
    <name>fuga</name>
    <tab2>
      <timestamp>2004-05-06</timestamp>
    </tab2>
  </tab1>
</NewDataSet>";
            var ds = new DataSet();
            DataTable dt = new DataTable("tab1");
            dt.Columns.Add("ident");
            dt.Columns.Add("name");
            dt.Rows.Add(new object[] { "1", "hoge" });
            dt.Rows.Add(new object[] { "2", "fuga" });
            DataTable dt2 = new DataTable("tab2");
            dt2.Columns.Add("idref");
            dt2.Columns[0].ColumnMapping = MappingType.Hidden;
            dt2.Columns.Add("timestamp");
            dt2.Rows.Add(new object[] { "1", "2004-05-05" });
            dt2.Rows.Add(new object[] { "2", "2004-05-06" });
            ds.Tables.Add(dt);
            ds.Tables.Add(dt2);
            DataRelation rel = new DataRelation("rel", dt.Columns[0], dt2.Columns[0]);
            rel.Nested = true;
            ds.Relations.Add(rel);
            StringWriter sw = new StringWriter();
            ds.WriteXml(sw);
            Assert.Equal(sw.ToString().Replace("\r\n", "\n"), xml.Replace("\r\n", "\n"));
        }

        [Fact]
        public void WriteXmlToStream()
        {
            string xml = "<set><table1><col1>sample text</col1><col2/></table1><table2 attr='value'><col3>sample text 2</col3></table2></set>";
            var ds = new DataSet();
            ds.ReadXml(new StringReader(xml));
            MemoryStream ms = new MemoryStream();
            ds.WriteXml(ms);
            MemoryStream ms2 = new MemoryStream(ms.ToArray());
            StreamReader sr = new StreamReader(ms2, Encoding.UTF8);
            string result = @"<set>
  <table1>
    <col1>sample text</col1>
    <col2 />
  </table1>
  <table2 attr=""value"">
    <col3>sample text 2</col3>
  </table2>
</set>";
            Assert.Equal(sr.ReadToEnd().Replace("\r\n", "\n"), result.Replace("\r\n", "\n"));
        }

        [Fact]
        public void WtiteXmlEncodedXml()
        {
            string xml = @"<an_x0020_example_x0020_dataset.>
  <WOW_x0021__x0020_that_x0027_s_x0020_nasty...>
    <URL_x0020_is_x0020_http_x003A__x002F__x002F_www.go-mono.com>content string.</URL_x0020_is_x0020_http_x003A__x002F__x002F_www.go-mono.com>
  </WOW_x0021__x0020_that_x0027_s_x0020_nasty...>
</an_x0020_example_x0020_dataset.>";
            DataSet ds = new DataSet("an example dataset.");
            ds.Tables.Add(new DataTable("WOW! that's nasty..."));
            ds.Tables[0].Columns.Add("URL is http://www.go-mono.com");
            ds.Tables[0].Rows.Add(new object[] { "content string." });
            StringWriter sw = new StringWriter();
            ds.WriteXml(sw);
            Assert.Equal(sw.ToString().Replace("\r\n", "\n"), xml.Replace("\r\n", "\n"));
        }

        [Fact]
        public void ReadWriteXml2()
        {
            string xml = "<FullTextResponse><Domains><AvailResponse info='y' name='novell-ximian-group' /><AvailResponse info='n' name='ximian' /></Domains></FullTextResponse>";
            var ds = new DataSet();
            ds.ReadXml(new StringReader(xml));
            DataSetAssertion.AssertDataSet("ds", ds, "FullTextResponse", 2, 1);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt1", dt, "Domains", 1, 1, 0, 1, 1, 1);
            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("dt2", dt, "AvailResponse", 3, 2, 1, 0, 1, 0);
            StringWriter sw = new StringWriter();
            XmlTextWriter xtw = new XmlTextWriter(sw);
            xtw.QuoteChar = '\'';
            ds.WriteXml(xtw);
            Assert.Equal(xml, sw.ToString());
        }

        [Fact]
        public void ReadWriteXml3()
        {
            string input = @"<FullTextResponse>
  <Domains>
    <AvailResponse info='y' name='novell-ximian-group' />
    <AvailResponse info='n' name='ximian' />
  </Domains>
</FullTextResponse>";
            var ds = new DataSet();
            ds.ReadXml(new StringReader(input));

            StringWriter sw = new StringWriter();
            XmlTextWriter xtw = new XmlTextWriter(sw);
            xtw.Formatting = Formatting.Indented;
            xtw.QuoteChar = '\'';
            ds.WriteXml(xtw);
            xtw.Flush();
            Assert.Equal(input.Replace("\r\n", "\n"), sw.ToString().Replace("\r\n", "\n"));
        }

        [Fact]
        public void WriteXmlSchema2()
        {
            string xml = @"<myDataSet xmlns='NetFrameWork'><myTable><id>0</id><item>item 0</item></myTable><myTable><id>1</id><item>item 1</item></myTable><myTable><id>2</id><item>item 2</item></myTable><myTable><id>3</id><item>item 3</item></myTable><myTable><id>4</id><item>item 4</item></myTable><myTable><id>5</id><item>item 5</item></myTable><myTable><id>6</id><item>item 6</item></myTable><myTable><id>7</id><item>item 7</item></myTable><myTable><id>8</id><item>item 8</item></myTable><myTable><id>9</id><item>item 9</item></myTable></myDataSet>";
            string schema = @"<?xml version='1.0' encoding='utf-16'?>
<xs:schema id='myDataSet' targetNamespace='NetFrameWork' xmlns:mstns='NetFrameWork' xmlns='NetFrameWork' xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata' attributeFormDefault='qualified' elementFormDefault='qualified'>
  <xs:element name='myDataSet' msdata:IsDataSet='true' " +
            "msdata:UseCurrentLocale='true'"
            + @">
    <xs:complexType>
      <xs:choice minOccurs='0' maxOccurs='unbounded'>
        <xs:element name='myTable'>
          <xs:complexType>
            <xs:sequence>
              <xs:element name='id' msdata:AutoIncrement='true' type='xs:int' minOccurs='0' />
              <xs:element name='item' type='xs:string' minOccurs='0' />
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
  </xs:element>
</xs:schema>";
            DataSet OriginalDataSet = new DataSet("myDataSet");
            OriginalDataSet.Namespace = "NetFrameWork";
            DataTable myTable = new DataTable("myTable");
            DataColumn c1 = new DataColumn("id", typeof(int));
            c1.AutoIncrement = true;
            DataColumn c2 = new DataColumn("item");
            myTable.Columns.Add(c1);
            myTable.Columns.Add(c2);
            OriginalDataSet.Tables.Add(myTable);
            // Add ten rows.
            DataRow newRow;
            for (int i = 0; i < 10; i++)
            {
                newRow = myTable.NewRow();
                newRow["item"] = "item " + i;
                myTable.Rows.Add(newRow);
            }
            OriginalDataSet.AcceptChanges();

            StringWriter sw = new StringWriter();
            XmlTextWriter xtw = new XmlTextWriter(sw);
            xtw.QuoteChar = '\'';
            OriginalDataSet.WriteXml(xtw);
            string result = sw.ToString();

            Assert.Equal(xml, result);

            sw = new StringWriter();
            xtw = new XmlTextWriter(sw);
            xtw.Formatting = Formatting.Indented;
            OriginalDataSet.WriteXmlSchema(xtw);
            result = sw.ToString();

            result = result.Replace("\r\n", "\n").Replace('"', '\'');
            Assert.Equal(schema.Replace("\r\n", "\n"), result);
        }

        [Fact]
        public void WriteXmlSchema3()
        {
            string xmlschema = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema id=""ExampleDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
  <xs:element name=""ExampleDataSet"" msdata:IsDataSet=""true"" ";
            xmlschema = xmlschema + "msdata:UseCurrentLocale=\"true\"";
            xmlschema = xmlschema + @">
    <xs:complexType>
      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
        <xs:element name=""ExampleDataTable"">
          <xs:complexType>
            <xs:attribute name=""PrimaryKeyColumn"" type=""xs:int"" use=""required"" />
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
    <xs:unique name=""PK_ExampleDataTable"" msdata:PrimaryKey=""true"">
      <xs:selector xpath="".//ExampleDataTable"" />
      <xs:field xpath=""@PrimaryKeyColumn"" />
    </xs:unique>
  </xs:element>
</xs:schema>";
            DataSet ds = new DataSet("ExampleDataSet");

            ds.Tables.Add(new DataTable("ExampleDataTable"));
            ds.Tables["ExampleDataTable"].Columns.Add(
                new DataColumn("PrimaryKeyColumn", typeof(int), "", MappingType.Attribute));
            ds.Tables["ExampleDataTable"].Columns["PrimaryKeyColumn"].AllowDBNull = false;

            ds.Tables["ExampleDataTable"].Constraints.Add(
                "PK_ExampleDataTable",
                ds.Tables["ExampleDataTable"].Columns["PrimaryKeyColumn"],
                true);

            ds.AcceptChanges();
            StringWriter sw = new StringWriter();
            ds.WriteXmlSchema(sw);

            string result = sw.ToString();

            Assert.Equal(result.Replace("\r\n", "\n"), xmlschema.Replace("\r\n", "\n"));
        }

        [Fact]
        public void WriteXmlSchema4()
        {
            string xmlschema = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema id=""Example"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
";
            xmlschema = xmlschema + "  <xs:element name=\"Example\" msdata:IsDataSet=\"true\" msdata:UseCurrentLocale=\"true\"";
            xmlschema = xmlschema + @">
    <xs:complexType>
      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
        <xs:element name=""MyType"">
          <xs:complexType>
            <xs:attribute name=""ID"" type=""xs:int"" use=""required"" />
            <xs:attribute name=""Desc"" type=""xs:string"" />
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
  </xs:element>
</xs:schema>";
            DataSet ds = new DataSet("Example");

            // Add MyType DataTable
            DataTable dt = new DataTable("MyType");
            ds.Tables.Add(dt);

            dt.Columns.Add(new DataColumn("ID", typeof(int), "",
                MappingType.Attribute));
            dt.Columns["ID"].AllowDBNull = false;

            dt.Columns.Add(new DataColumn("Desc", typeof
                (string), "", MappingType.Attribute));

            ds.AcceptChanges();

            StringWriter sw = new StringWriter();
            ds.WriteXmlSchema(sw);

            string result = sw.ToString();

            Assert.Equal(result.Replace("\r\n", "\n"), xmlschema.Replace("\r\n", "\n"));
        }

        [Fact]
        public void WriteXmlSchema5()
        {
            string xmlschema = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema id=""Example"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
" +
"  <xs:element name=\"Example\" msdata:IsDataSet=\"true\" msdata:UseCurrentLocale=\"true\""
              + @">
    <xs:complexType>
      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
        <xs:element name=""StandAlone"">
          <xs:complexType>
            <xs:attribute name=""ID"" type=""xs:int"" use=""required"" />
            <xs:attribute name=""Desc"" type=""xs:string"" use=""required"" />
          </xs:complexType>
        </xs:element>
        <xs:element name=""Dimension"">
          <xs:complexType>
            <xs:attribute name=""Number"" msdata:ReadOnly=""true"" type=""xs:int"" use=""required"" />
            <xs:attribute name=""Title"" type=""xs:string"" use=""required"" />
          </xs:complexType>
        </xs:element>
        <xs:element name=""Element"">
          <xs:complexType>
            <xs:attribute name=""Dimension"" msdata:ReadOnly=""true"" type=""xs:int"" use=""required"" />
            <xs:attribute name=""Number"" msdata:ReadOnly=""true"" type=""xs:int"" use=""required"" />
            <xs:attribute name=""Title"" type=""xs:string"" use=""required"" />
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
    <xs:unique name=""PK_Dimension"" msdata:PrimaryKey=""true"">
      <xs:selector xpath="".//Dimension"" />
      <xs:field xpath=""@Number"" />
    </xs:unique>
    <xs:unique name=""PK_Element"" msdata:PrimaryKey=""true"">
      <xs:selector xpath="".//Element"" />
      <xs:field xpath=""@Dimension"" />
      <xs:field xpath=""@Number"" />
    </xs:unique>
    <xs:keyref name=""FK_Element_To_Dimension"" refer=""PK_Dimension"">
      <xs:selector xpath="".//Element"" />
      <xs:field xpath=""@Dimension"" />
    </xs:keyref>
  </xs:element>
</xs:schema>";
            DataSet ds = new DataSet("Example");

            // Add a DataTable with no ReadOnly columns
            DataTable dt1 = new DataTable("StandAlone");
            ds.Tables.Add(dt1);

            // Add a ReadOnly column
            dt1.Columns.Add(new DataColumn("ID", typeof(int), "",
                MappingType.Attribute));
            dt1.Columns["ID"].AllowDBNull = false;

            dt1.Columns.Add(new DataColumn("Desc", typeof
                (string), "", MappingType.Attribute));
            dt1.Columns["Desc"].AllowDBNull = false;

            // Add related DataTables with ReadOnly columns
            DataTable dt2 = new DataTable("Dimension");
            ds.Tables.Add(dt2);
            dt2.Columns.Add(new DataColumn("Number", typeof
                (int), "", MappingType.Attribute));
            dt2.Columns["Number"].AllowDBNull = false;
            dt2.Columns["Number"].ReadOnly = true;

            dt2.Columns.Add(new DataColumn("Title", typeof
                (string), "", MappingType.Attribute));
            dt2.Columns["Title"].AllowDBNull = false;

            dt2.Constraints.Add("PK_Dimension", dt2.Columns["Number"], true);

            DataTable dt3 = new DataTable("Element");
            ds.Tables.Add(dt3);

            dt3.Columns.Add(new DataColumn("Dimension", typeof
                (int), "", MappingType.Attribute));
            dt3.Columns["Dimension"].AllowDBNull = false;
            dt3.Columns["Dimension"].ReadOnly = true;

            dt3.Columns.Add(new DataColumn("Number", typeof
                (int), "", MappingType.Attribute));
            dt3.Columns["Number"].AllowDBNull = false;
            dt3.Columns["Number"].ReadOnly = true;

            dt3.Columns.Add(new DataColumn("Title", typeof
                (string), "", MappingType.Attribute));
            dt3.Columns["Title"].AllowDBNull = false;

            dt3.Constraints.Add("PK_Element", new DataColumn[] {
                dt3.Columns ["Dimension"],
                dt3.Columns ["Number"] }, true);

            ds.Relations.Add("FK_Element_To_Dimension",
                dt2.Columns["Number"], dt3.Columns["Dimension"]);

            ds.AcceptChanges();

            StringWriter sw = new StringWriter();
            ds.WriteXmlSchema(sw);

            string result = sw.ToString();

            Assert.Equal(result.Replace("\r\n", "\n"), xmlschema.Replace("\r\n", "\n"));
        }

        [Fact]
        public void WriteXmlSchema6()
        {
            string xmlschema = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema id=""Example"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
" +
              @"  <xs:element name=""Example"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true"""
              + @">
    <xs:complexType>
      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
        <xs:element name=""MyType"">
          <xs:complexType>
            <xs:attribute name=""Desc"">
              <xs:simpleType>
                <xs:restriction base=""xs:string"">
                  <xs:maxLength value=""32"" />
                </xs:restriction>
              </xs:simpleType>
            </xs:attribute>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
  </xs:element>
</xs:schema>";
            DataSet ds = new DataSet("Example");

            // Add MyType DataTable
            ds.Tables.Add("MyType");

            ds.Tables["MyType"].Columns.Add(new DataColumn(
                "Desc", typeof(string), "", MappingType.Attribute));
            ds.Tables["MyType"].Columns["Desc"].MaxLength = 32;

            ds.AcceptChanges();

            StringWriter sw = new StringWriter();
            ds.WriteXmlSchema(sw);

            string result = sw.ToString();

            Assert.Equal(result.Replace("\r\n", "\n"), xmlschema.Replace("\r\n", "\n"));
        }

        [Fact]
        public void WriteXmlSchema7()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable("table");
            dt.Columns.Add("col1");
            dt.Columns.Add("col2");
            ds.Tables.Add(dt);
            dt.Rows.Add(new object[] { "foo", "bar" });
            StringWriter sw = new StringWriter();
            ds.WriteXmlSchema(sw);
            Assert.True(sw.ToString().IndexOf("xmlns=\"\"") > 0);
        }

        [Fact]
        public void WriteXmlExtendedProperties()
        {
            string xml = @"<?xml version=""1.0"" encoding=""utf-16""?>
<xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"" xmlns:msprop=""urn:schemas-microsoft-com:xml-msprop"">
" +
@"  <xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true"" msprop:version=""version 2.1"">"
              + @"
    <xs:complexType>
      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
        <xs:element name=""Foo"">
          <xs:complexType>
            <xs:sequence>
              <xs:element name=""col1"" type=""xs:string"" minOccurs=""0"" />
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:choice>
    </xs:complexType>
  </xs:element>
</xs:schema>";
            var ds = new DataSet();
            ds.ExtendedProperties["version"] = "version 2.1";
            DataTable dt = new DataTable("Foo");
            dt.Columns.Add("col1");
            dt.Rows.Add(new object[] { "foo" });
            ds.Tables.Add(dt);

            StringWriter sw = new StringWriter();
            ds.WriteXmlSchema(sw);

            string result = sw.ToString();

            Assert.Equal(result.Replace("\r\n", "\n"), xml.Replace("\r\n", "\n"));
        }

        [Fact]
        public void WriteXmlModeSchema()
        {
            // This is the MS output of WriteXmlSchema().

            string xml = @"<Example>
  <xs:schema id=""Example"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
" +
@"    <xs:element name=""Example"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true"">"
              + @"
      <xs:complexType>
        <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
          <xs:element name=""Dimension"">
            <xs:complexType>
              <xs:sequence>
                <xs:element name=""Number"" type=""xs:int"" />
              </xs:sequence>
            </xs:complexType>
          </xs:element>
          <xs:element name=""Element"">
            <xs:complexType>
              <xs:sequence>
                <xs:element name=""Dimension"" type=""xs:int"" />
                <xs:element name=""Number"" type=""xs:int"" />
              </xs:sequence>
            </xs:complexType>
          </xs:element>
        </xs:choice>
      </xs:complexType>
      <xs:unique name=""PK_Dimension"" msdata:PrimaryKey=""true"">
        <xs:selector xpath="".//Dimension"" />
        <xs:field xpath=""Number"" />
      </xs:unique>
      <xs:unique name=""PK_Element"" msdata:PrimaryKey=""true"">
        <xs:selector xpath="".//Element"" />
        <xs:field xpath=""Dimension"" />
        <xs:field xpath=""Number"" />
      </xs:unique>
      <xs:keyref name=""FK_Element_To_Dimension"" refer=""PK_Dimension"">
        <xs:selector xpath="".//Element"" />
        <xs:field xpath=""Dimension"" />
      </xs:keyref>
    </xs:element>
  </xs:schema>
  <Dimension>
    <Number>0</Number>
  </Dimension>
  <Dimension>
    <Number>1</Number>
  </Dimension>
  <Element>
    <Dimension>0</Dimension>
    <Number>0</Number>
  </Element>
  <Element>
    <Dimension>0</Dimension>
    <Number>1</Number>
  </Element>
  <Element>
    <Dimension>0</Dimension>
    <Number>2</Number>
  </Element>
  <Element>
    <Dimension>0</Dimension>
    <Number>3</Number>
  </Element>
  <Element>
    <Dimension>1</Dimension>
    <Number>0</Number>
  </Element>
  <Element>
    <Dimension>1</Dimension>
    <Number>1</Number>
  </Element>
</Example>";
            DataSet ds = new DataSet("Example");

            // Dimension DataTable
            DataTable dt1 = new DataTable("Dimension");
            ds.Tables.Add(dt1);

            dt1.Columns.Add(new DataColumn("Number", typeof(int)));
            dt1.Columns["Number"].AllowDBNull = false;

            dt1.Constraints.Add("PK_Dimension", dt1.Columns["Number"], true);

            // Element DataTable
            DataTable dt2 = new DataTable("Element");
            ds.Tables.Add(dt2);

            dt2.Columns.Add(new DataColumn("Dimension", typeof(int)));
            dt2.Columns["Dimension"].AllowDBNull = false;

            dt2.Columns.Add(new DataColumn("Number", typeof(int)));
            dt2.Columns["Number"].AllowDBNull = false;

            dt2.Constraints.Add("PK_Element", new DataColumn[] {
                dt2.Columns ["Dimension"],
                dt2.Columns ["Number"] },
                true);

            // Add DataRelations
            ds.Relations.Add("FK_Element_To_Dimension",
                dt1.Columns["Number"],
                dt2.Columns["Dimension"], true);

            // Add 2 Dimensions
            for (int i = 0; i < 2; i++)
            {
                DataRow newRow = dt1.NewRow();
                newRow["Number"] = i;
                dt1.Rows.Add(newRow);
            }

            // Dimension 0 => 4 Elements
            for (int i = 0; i < 4; i++)
            {
                DataRow newRow = dt2.NewRow();
                newRow["Dimension"] = 0;
                newRow["Number"] = i;
                dt2.Rows.Add(newRow);
            }

            // Dimension 1 => 2 Elements
            for (int i = 0; i < 2; i++)
            {
                DataRow newRow = dt2.NewRow();
                newRow["Dimension"] = 1;
                newRow["Number"] = i;
                dt2.Rows.Add(newRow);
            }

            ds.AcceptChanges();

            StringWriter sw = new StringWriter();
            ds.WriteXml(sw, XmlWriteMode.WriteSchema);

            string result = sw.ToString();

            Assert.Equal(result.Replace("\r\n", "\n"), xml.Replace("\r\n", "\n"));
        }

        [Fact]
        public void WriteXmlModeSchema1()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("fi-FI");
                string SerializedDataTable =
@"<rdData>
  <MyDataTable CustomerID='VINET' CompanyName='Vins et alcools Chevalier' ContactName='Paul Henriot' />
</rdData>";
                string expected =
    @"<rdData>
  <xs:schema id=""rdData"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
    <xs:element name=""rdData"" msdata:IsDataSet=""true"" " +
                  @"msdata:Locale=""en-US"">" +
    @"
      <xs:complexType>
        <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
          <xs:element name=""MyDataTable"">
            <xs:complexType>
              <xs:attribute name=""CustomerID"" type=""xs:string"" />
              <xs:attribute name=""CompanyName"" type=""xs:string"" />
              <xs:attribute name=""ContactName"" type=""xs:string"" />
            </xs:complexType>
          </xs:element>
        </xs:choice>
      </xs:complexType>
    </xs:element>
  </xs:schema>
  <MyDataTable CustomerID=""VINET"" CompanyName=""Vins et alcools Chevalier"" ContactName=""Paul Henriot"" />
</rdData>";
                DataSet set;
                set = new DataSet();
                set.ReadXml(new StringReader(SerializedDataTable));

                StringWriter w = new StringWriter();
                set.WriteXml(w, XmlWriteMode.WriteSchema);
                string result = w.ToString();
                Assert.Equal(expected.Replace("\r", ""), result.Replace("\r", ""));

                return SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void DeserializeModifiedDataSet()
        {
            // Serialization begins
            DataSet prevDs = new DataSet();
            DataTable dt = prevDs.Tables.Add();
            dt.Columns.Add(new DataColumn("Id", typeof(string)));

            DataRow dr = dt.NewRow();
            dr[0] = "a";
            dt.Rows.Add(dr);
            prevDs.AcceptChanges();
            dr = prevDs.Tables[0].Rows[0];
            dr[0] = "b";

            XmlSerializer serializer = new XmlSerializer(typeof(DataSet));
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            xw.QuoteChar = '\'';
            serializer.Serialize(xw, prevDs);

            // Deserialization begins
            StringReader sr = new StringReader(sw.ToString());
            XmlTextReader reader = new XmlTextReader(sr);
            XmlSerializer serializer1 = new XmlSerializer(typeof(DataSet));
            DataSet ds = serializer1.Deserialize(reader) as DataSet;
            Assert.Equal(
                    prevDs.Tables[0].Rows[0][0, DataRowVersion.Original].ToString(),
    ds.Tables[0].Rows[0][0, DataRowVersion.Original].ToString());
            Assert.Equal(
                    prevDs.Tables[0].Rows[0][0, DataRowVersion.Current].ToString(),
    ds.Tables[0].Rows[0][0, DataRowVersion.Current].ToString());
        }

        [Fact]
        public void Bug420862()
        {
            DataSet ds = new DataSet("d");
            DataTable dt = ds.Tables.Add("t");
            dt.Columns.Add("c", typeof(ushort));

            XmlSchema xs = XmlSchema.Read(new StringReader(ds.GetXmlSchema()), null);
#pragma warning disable 0618
            xs.Compile(null);
#pragma warning restore 0618

            // follow the nesting of the schema in the foreach
            foreach (XmlSchemaElement d in xs.Items)
            {
                Assert.Equal("d", d.Name);
                XmlSchemaChoice dsc = (XmlSchemaChoice)((XmlSchemaComplexType)d.SchemaType).Particle;
                foreach (XmlSchemaElement t in dsc.Items)
                {
                    Assert.Equal("t", t.Name);
                    XmlSchemaSequence tss = (XmlSchemaSequence)((XmlSchemaComplexType)t.SchemaType).Particle;
                    foreach (XmlSchemaElement c in tss.Items)
                    {
                        Assert.Equal("c", c.Name);
                        Assert.Equal("unsignedShort", c.SchemaTypeName.Name);
                        return;
                    }
                }
            }
            Assert.False(true);
        }

        /// <summary>
        /// Test for testing DataSet.Clear method with foreign key relations
        /// This is expected to clear all the related datatable rows also
        /// </summary>
        [Fact]
        public void DataSetClearTest()
        {
            var ds = new DataSet();
            DataTable parent = ds.Tables.Add("Parent");
            DataTable child = ds.Tables.Add("Child");

            parent.Columns.Add("id", typeof(int));
            child.Columns.Add("ref_id", typeof(int));

            child.Constraints.Add(new ForeignKeyConstraint("fk_constraint", parent.Columns[0], child.Columns[0]));

            DataRow dr = parent.NewRow();
            dr[0] = 1;
            parent.Rows.Add(dr);
            dr.AcceptChanges();

            dr = child.NewRow();
            dr[0] = 1;
            child.Rows.Add(dr);
            dr.AcceptChanges();

            try
            {
                ds.Clear(); // this should clear all the rows in parent & child tables
            }
            catch (Exception e)
            {
                throw (new Exception("Exception should not have been thrown at Clear method" + e.ToString()));
            }
            Assert.Equal(0, parent.Rows.Count);
            Assert.Equal(0, child.Rows.Count);
        }

        [Fact]
        public void CloneSubClassTest()
        {
            MyDataSet ds1 = new MyDataSet();
            MyDataSet ds = (MyDataSet)(ds1.Clone());
            Assert.Equal(2, MyDataSet.count);
        }

        #region DataSet.GetChanges Tests
        public void GetChanges_Relations_DifferentRowStatesTest()
        {
            DataSet ds = new DataSet("ds");
            DataTable parent = ds.Tables.Add("parent");
            DataTable child = ds.Tables.Add("child");

            parent.Columns.Add("id", typeof(int));
            parent.Columns.Add("name", typeof(string));


            child.Columns.Add("id", typeof(int));
            child.Columns.Add("parent", typeof(int));
            child.Columns.Add("name", typeof(string));

            parent.Rows.Add(new object[] { 1, "mono parent 1" });
            parent.Rows.Add(new object[] { 2, "mono parent 2" });
            parent.Rows.Add(new object[] { 3, "mono parent 3" });
            parent.Rows.Add(new object[] { 4, "mono parent 4" });
            parent.AcceptChanges();

            child.Rows.Add(new object[] { 1, 1, "mono child 1" });
            child.Rows.Add(new object[] { 2, 2, "mono child 2" });
            child.Rows.Add(new object[] { 3, 3, "mono child 3" });
            child.AcceptChanges();

            DataRelation relation = ds.Relations.Add("parent_child",
                                  parent.Columns["id"],
                                  child.Columns["parent"]);

            // modify the parent and get changes
            child.Rows[1]["parent"] = 4;
            DataSet changes = ds.GetChanges();
            DataRow row = changes.Tables["parent"].Rows[0];
            Assert.Equal((int)parent.Rows[3][0], (int)row[0]);
            Assert.Equal(1, changes.Tables["parent"].Rows.Count);
            ds.RejectChanges();

            // delete a child row and get changes.
            child.Rows[0].Delete();
            changes = ds.GetChanges();

            Assert.Equal(changes.Tables.Count, 2);
            Assert.Equal(1, changes.Tables["parent"].Rows.Count);
            Assert.Equal(1, (int)changes.Tables["parent"].Rows[0][0]);
        }
        #endregion // DataSet.GetChanges Tests

        [Fact]
        public void RuleTest()
        {
            DataSet ds = new DataSet("testds");
            DataTable parent = ds.Tables.Add("parent");
            DataTable child = ds.Tables.Add("child");

            parent.Columns.Add("id", typeof(int));
            parent.Columns.Add("name", typeof(string));
            parent.PrimaryKey = new DataColumn[] { parent.Columns["id"] };

            child.Columns.Add("id", typeof(int));
            child.Columns.Add("parent", typeof(int));
            child.Columns.Add("name", typeof(string));
            child.PrimaryKey = new DataColumn[] { child.Columns["id"] };

            DataRelation relation = ds.Relations.Add("parent_child",
                                  parent.Columns["id"],
                                  child.Columns["parent"]);

            parent.Rows.Add(new object[] { 1, "mono test 1" });
            parent.Rows.Add(new object[] { 2, "mono test 2" });
            parent.Rows.Add(new object[] { 3, "mono test 3" });

            child.Rows.Add(new object[] { 1, 1, "mono child test 1" });
            child.Rows.Add(new object[] { 2, 2, "mono child test 2" });
            child.Rows.Add(new object[] { 3, 3, "mono child test 3" });

            ds.AcceptChanges();

            parent.Rows[0]["name"] = "mono changed test 1";

            Assert.Equal(DataRowState.Unchanged, parent.Rows[0].GetChildRows(relation)[0].RowState);

            ds.RejectChanges();
            parent.Rows[0]["id"] = "4";

            DataRow childRow = parent.Rows[0].GetChildRows(relation)[0];
            Assert.Equal(DataRowState.Modified, childRow.RowState);
            Assert.Equal(4, (int)childRow["parent"]);
        }

        [Fact]
        public void WriteXmlEscapeName()
        {
            // create dataset
            DataSet data = new DataSet();

            DataTable mainTable = data.Tables.Add("main");
            DataColumn mainkey = mainTable.Columns.Add("mainkey", typeof(Guid));
            mainTable.Columns.Add("col.2<hi/>", typeof(string));
            mainTable.Columns.Add("#col3", typeof(string));

            // populate data
            mainTable.Rows.Add(new object[] { Guid.NewGuid(), "hi there", "my friend" });
            mainTable.Rows.Add(new object[] { Guid.NewGuid(), "what is", "your name" });
            mainTable.Rows.Add(new object[] { Guid.NewGuid(), "I have", "a bean" });

            // write xml
            StringWriter writer = new StringWriter();
            data.WriteXml(writer, XmlWriteMode.WriteSchema);
            string xml = writer.ToString();
            Assert.True(xml.IndexOf("name=\"col.2_x003C_hi_x002F__x003E_\"") > 0);
            Assert.True(xml.IndexOf("name=\"_x0023_col3\"") > 0);
            Assert.True(xml.IndexOf("<col.2_x003C_hi_x002F__x003E_>hi there</col.2_x003C_hi_x002F__x003E_>") > 0);

            // read xml
            DataSet data2 = new DataSet();
            data2.ReadXml(new StringReader(
                writer.GetStringBuilder().ToString()));
        }

        #region DataSet.CreateDataReader Tests and DataSet.Load Tests

        private DataSet _ds;
        private DataTable _dt1,_dt2;

        private void localSetup()
        {
            _ds = new DataSet("test");
            _dt1 = new DataTable("test1");
            _dt1.Columns.Add("id1", typeof(int));
            _dt1.Columns.Add("name1", typeof(string));
            //dt1.PrimaryKey = new DataColumn[] { dt1.Columns["id"] };
            _dt1.Rows.Add(new object[] { 1, "mono 1" });
            _dt1.Rows.Add(new object[] { 2, "mono 2" });
            _dt1.Rows.Add(new object[] { 3, "mono 3" });
            _dt1.AcceptChanges();
            _dt2 = new DataTable("test2");
            _dt2.Columns.Add("id2", typeof(int));
            _dt2.Columns.Add("name2", typeof(string));
            _dt2.Columns.Add("name3", typeof(string));
            //dt2.PrimaryKey = new DataColumn[] { dt2.Columns["id"] };
            _dt2.Rows.Add(new object[] { 4, "mono 4", "four" });
            _dt2.Rows.Add(new object[] { 5, "mono 5", "five" });
            _dt2.Rows.Add(new object[] { 6, "mono 6", "six" });
            _dt2.AcceptChanges();
            _ds.Tables.Add(_dt1);
            _ds.Tables.Add(_dt2);
            _ds.AcceptChanges();
        }

        [Fact]
        public void CreateDataReader1()
        {
            // For First CreateDataReader Overload
            localSetup();
            DataTableReader dtr = _ds.CreateDataReader();
            Assert.True(dtr.HasRows);
            int ti = 0;
            do
            {
                Assert.Equal(_ds.Tables[ti].Columns.Count, dtr.FieldCount);
                int ri = 0;
                while (dtr.Read())
                {
                    for (int i = 0; i < dtr.FieldCount; i++)
                        Assert.Equal(_ds.Tables[ti].Rows[ri][i], dtr[i]);
                    ri++;
                }
                ti++;
            } while (dtr.NextResult());
        }

        [Fact]
        public void CreateDataReader2()
        {
            // For Second CreateDataReader Overload -
            // compare to ds.Tables
            localSetup();
            DataTableReader dtr = _ds.CreateDataReader(_dt1, _dt2);
            Assert.True(dtr.HasRows);
            int ti = 0;
            do
            {
                Assert.Equal(_ds.Tables[ti].Columns.Count, dtr.FieldCount);
                int ri = 0;
                while (dtr.Read())
                {
                    for (int i = 0; i < dtr.FieldCount; i++)
                        Assert.Equal(_ds.Tables[ti].Rows[ri][i], dtr[i]);
                    ri++;
                }
                ti++;
            } while (dtr.NextResult());
        }

        [Fact]
        public void CreateDataReader3()
        {
            // For Second CreateDataReader Overload -
            // compare to dt1 and dt2
            localSetup();
            _ds.Tables.Clear();
            DataTableReader dtr = _ds.CreateDataReader(_dt1, _dt2);
            Assert.True(dtr.HasRows);
            string name = "dt1";
            DataTable dtn = _dt1;
            do
            {
                Assert.Equal(dtn.Columns.Count, dtr.FieldCount);
                int ri = 0;
                while (dtr.Read())
                {
                    for (int i = 0; i < dtr.FieldCount; i++)
                        Assert.Equal(dtn.Rows[ri][i], dtr[i]);
                    ri++;
                }
                if (dtn == _dt1)
                {
                    dtn = _dt2;
                    name = "dt2";
                }
                else
                {
                    dtn = null;
                    name = null;
                }
            } while (dtr.NextResult());
        }

        [Fact]
        public void CreateDataReaderNoTable()
        {
            DataSet dsr = new DataSet();
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               DataTableReader dtr = dsr.CreateDataReader();
           });
        }

        internal struct fillErrorStruct
        {
            internal string error;
            internal string tableName;
            internal int rowKey;
            internal bool contFlag;
            internal void init(string tbl, int row, bool cont, string err)
            {
                tableName = tbl;
                rowKey = row;
                contFlag = cont;
                error = err;
            }
        }
        private fillErrorStruct[] _fillErr = new fillErrorStruct[3];
        private int _fillErrCounter;
        private void fillErrorHandler(object sender, FillErrorEventArgs e)
        {
            e.Continue = _fillErr[_fillErrCounter].contFlag;
            Assert.Equal(_fillErr[_fillErrCounter].tableName, e.DataTable.TableName);
            Assert.Equal(_fillErr[_fillErrCounter].contFlag, e.Continue);
            _fillErrCounter++;
        }

        [Fact]
        public void Load_Basic()
        {
            localSetup();
            DataSet dsLoad = new DataSet("LoadBasic");
            DataTable table1 = new DataTable();
            dsLoad.Tables.Add(table1);
            DataTable table2 = new DataTable();
            dsLoad.Tables.Add(table2);
            DataTableReader dtr = _ds.CreateDataReader();
            dsLoad.Load(dtr, LoadOption.OverwriteChanges, table1, table2);
            CompareTables(dsLoad);
        }

        [Fact]
        public void Load_TableUnknown()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               localSetup();
               DataSet dsLoad = new DataSet("LoadTableUnknown");
               DataTable table1 = new DataTable();
               dsLoad.Tables.Add(table1);
               DataTable table2 = new DataTable();
               // table2 is not added to dsLoad [dsLoad.Tables.Add (table2);]
               DataTableReader dtr = _ds.CreateDataReader();
               dsLoad.Load(dtr, LoadOption.OverwriteChanges, table1, table2);
           });
        }

        [Fact]
        public void Load_TableConflictT()
        {
            _fillErrCounter = 0;
            _fillErr[0].init("Table1", 1, true,
                "Input string was not in a correct format.Couldn't store <mono 1> in name1 Column.  Expected type is Double.");
            _fillErr[1].init("Table1", 2, true,
                "Input string was not in a correct format.Couldn't store <mono 2> in name1 Column.  Expected type is Double.");
            _fillErr[2].init("Table1", 3, true,
                "Input string was not in a correct format.Couldn't store <mono 3> in name1 Column.  Expected type is Double.");
            localSetup();
            DataSet dsLoad = new DataSet("LoadTableConflict");
            DataTable table1 = new DataTable();
            table1.Columns.Add("name1", typeof(double));
            dsLoad.Tables.Add(table1);
            DataTable table2 = new DataTable();
            dsLoad.Tables.Add(table2);
            DataTableReader dtr = _ds.CreateDataReader();
            dsLoad.Load(dtr, LoadOption.PreserveChanges,
                     fillErrorHandler, table1, table2);
        }
        [Fact]
        public void Load_TableConflictF()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               _fillErrCounter = 0;
               _fillErr[0].init("Table1", 1, false,
                   "Input string was not in a correct format.Couldn't store <mono 1> in name1 Column.  Expected type is Double.");
               localSetup();
               DataSet dsLoad = new DataSet("LoadTableConflict");
               DataTable table1 = new DataTable();
               table1.Columns.Add("name1", typeof(double));
               dsLoad.Tables.Add(table1);
               DataTable table2 = new DataTable();
               dsLoad.Tables.Add(table2);
               DataTableReader dtr = _ds.CreateDataReader();
               dsLoad.Load(dtr, LoadOption.Upsert,
                        fillErrorHandler, table1, table2);
           });
        }

        [Fact]
        public void Load_StringsAsc()
        {
            localSetup();
            DataSet dsLoad = new DataSet("LoadStrings");
            DataTable table1 = new DataTable("First");
            dsLoad.Tables.Add(table1);
            DataTable table2 = new DataTable("Second");
            dsLoad.Tables.Add(table2);
            DataTableReader dtr = _ds.CreateDataReader();
            dsLoad.Load(dtr, LoadOption.OverwriteChanges, "First", "Second");
            CompareTables(dsLoad);
        }

        [Fact]
        public void Load_StringsDesc()
        {
            localSetup();
            DataSet dsLoad = new DataSet("LoadStrings");
            DataTable table1 = new DataTable("First");
            dsLoad.Tables.Add(table1);
            DataTable table2 = new DataTable("Second");
            dsLoad.Tables.Add(table2);
            DataTableReader dtr = _ds.CreateDataReader();
            dsLoad.Load(dtr, LoadOption.PreserveChanges, "Second", "First");
            Assert.Equal(2, dsLoad.Tables.Count);
            Assert.Equal(3, dsLoad.Tables[0].Rows.Count);
            Assert.Equal(3, dsLoad.Tables[0].Columns.Count);
            Assert.Equal(3, dsLoad.Tables[1].Rows.Count);
            Assert.Equal(2, dsLoad.Tables[1].Columns.Count);
        }

        [Fact]
        public void Load_StringsNew()
        {
            localSetup();
            DataSet dsLoad = new DataSet("LoadStrings");
            DataTable table1 = new DataTable("First");
            dsLoad.Tables.Add(table1);
            DataTable table2 = new DataTable("Second");
            dsLoad.Tables.Add(table2);
            DataTableReader dtr = _ds.CreateDataReader();
            dsLoad.Load(dtr, LoadOption.Upsert, "Third", "Fourth");
            Assert.Equal(4, dsLoad.Tables.Count);
            Assert.Equal("First", dsLoad.Tables[0].TableName);
            Assert.Equal(0, dsLoad.Tables[0].Rows.Count);
            Assert.Equal(0, dsLoad.Tables[0].Columns.Count);
            Assert.Equal("Second", dsLoad.Tables[1].TableName);
            Assert.Equal(0, dsLoad.Tables[1].Rows.Count);
            Assert.Equal(0, dsLoad.Tables[1].Columns.Count);
            Assert.Equal("Third", dsLoad.Tables[2].TableName);
            Assert.Equal(3, dsLoad.Tables[2].Rows.Count);
            Assert.Equal(2, dsLoad.Tables[2].Columns.Count);
            Assert.Equal("Fourth", dsLoad.Tables[3].TableName);
            Assert.Equal(3, dsLoad.Tables[3].Rows.Count);
            Assert.Equal(3, dsLoad.Tables[3].Columns.Count);
        }

        [Fact]
        public void Load_StringsNewMerge()
        {
            localSetup();
            DataSet dsLoad = new DataSet("LoadStrings");
            DataTable table1 = new DataTable("First");
            table1.Columns.Add("col1", typeof(string));
            table1.Rows.Add(new object[] { "T1Row1" });
            dsLoad.Tables.Add(table1);
            DataTable table2 = new DataTable("Second");
            table2.Columns.Add("col2", typeof(string));
            table2.Rows.Add(new object[] { "T2Row1" });
            table2.Rows.Add(new object[] { "T2Row2" });
            dsLoad.Tables.Add(table2);
            DataTableReader dtr = _ds.CreateDataReader();
            dsLoad.Load(dtr, LoadOption.OverwriteChanges, "Third", "First");
            Assert.Equal(3, dsLoad.Tables.Count);
            Assert.Equal("First", dsLoad.Tables[0].TableName);
            Assert.Equal(4, dsLoad.Tables[0].Rows.Count);
            Assert.Equal(4, dsLoad.Tables[0].Columns.Count);
            Assert.Equal("Second", dsLoad.Tables[1].TableName);
            Assert.Equal(2, dsLoad.Tables[1].Rows.Count);
            Assert.Equal(1, dsLoad.Tables[1].Columns.Count);
            Assert.Equal("Third", dsLoad.Tables[2].TableName);
            Assert.Equal(3, dsLoad.Tables[2].Rows.Count);
            Assert.Equal(2, dsLoad.Tables[2].Columns.Count);
        }

        [Fact]
        public void ReadDiff()
        {
            DataSet dsTest = new DataSet("MonoTouchTest");
            var dt = new DataTable("123");
            dt.Columns.Add(new DataColumn("Value1"));
            dt.Columns.Add(new DataColumn("Value2"));
            dsTest.Tables.Add(dt);
            dsTest.ReadXml(new StringReader(@"
<diffgr:diffgram
   xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'
   xmlns:diffgr='urn:schemas-microsoft-com:xml-diffgram-v1'>
  <MonoTouchTest>
    <_x0031_23 diffgr:id='1231' msdata:rowOrder='0'>
      <Value1>Row1Value1</Value1>
      <Value2>Row1Value2</Value2>
    </_x0031_23>
  </MonoTouchTest>
</diffgr:diffgram>
"));
            Assert.Equal("123", dsTest.Tables[0].TableName);
            Assert.Equal(1, dsTest.Tables[0].Rows.Count);
        }

        private void CompareTables(DataSet dsLoad)
        {
            Assert.Equal(_ds.Tables.Count, dsLoad.Tables.Count);
            for (int tc = 0; tc < dsLoad.Tables.Count; tc++)
            {
                Assert.Equal(_ds.Tables[tc].Columns.Count, dsLoad.Tables[tc].Columns.Count);
                Assert.Equal(_ds.Tables[tc].Rows.Count, dsLoad.Tables[tc].Rows.Count);
                for (int cc = 0; cc < dsLoad.Tables[tc].Columns.Count; cc++)
                {
                    Assert.Equal(_ds.Tables[tc].Columns[cc].ColumnName,
                             dsLoad.Tables[tc].Columns[cc].ColumnName);
                }
                for (int rc = 0; rc < dsLoad.Tables[tc].Rows.Count; rc++)
                {
                    for (int cc = 0; cc < dsLoad.Tables[tc].Columns.Count; cc++)
                    {
                        Assert.Equal(_ds.Tables[tc].Rows[rc].ItemArray[cc],
                                 dsLoad.Tables[tc].Rows[rc].ItemArray[cc]);
                    }
                }
            }
        }

        #endregion // DataSet.CreateDataReader Tests and DataSet.Load Tests
    }

    public class MyDataSet : DataSet
    {
        public static int count;

        public MyDataSet()
        {
            count++;
        }
    }
}
