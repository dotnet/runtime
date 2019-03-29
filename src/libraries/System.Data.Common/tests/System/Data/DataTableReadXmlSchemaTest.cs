// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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


using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Data.Tests
{
    public class DataTableReadXmlSchemaTest
    {
        private DataSet CreateTestSet()
        {
            var ds = new DataSet();
            ds.Tables.Add("Table1");
            ds.Tables.Add("Table2");
            ds.Tables[0].Columns.Add("Column1_1");
            ds.Tables[0].Columns.Add("Column1_2");
            ds.Tables[0].Columns.Add("Column1_3");
            ds.Tables[1].Columns.Add("Column2_1");
            ds.Tables[1].Columns.Add("Column2_2");
            ds.Tables[1].Columns.Add("Column2_3");
            ds.Tables[0].Rows.Add(new object[] { "ppp", "www", "xxx" });
            ds.Relations.Add("Rel1", ds.Tables[0].Columns[2], ds.Tables[1].Columns[0]);
            return ds;
        }

        [Fact]
        public void SuspiciousDataSetElement()
        {
            string schema = @"<?xml version='1.0'?>
<xsd:schema xmlns:xsd='http://www.w3.org/2001/XMLSchema'>
	<xsd:attribute name='foo' type='xsd:string'/>
	<xsd:attribute name='bar' type='xsd:string'/>
	<xsd:complexType name='attRef'>
		<xsd:attribute name='att1' type='xsd:int'/>
		<xsd:attribute name='att2' type='xsd:string'/>
	</xsd:complexType>
	<xsd:element name='doc'>
		<xsd:complexType>
			<xsd:choice>
				<xsd:element name='elem' type='attRef'/>
			</xsd:choice>
		</xsd:complexType>
	</xsd:element>
</xsd:schema>";
            var ds = new DataSet();
            ds.Tables.Add(new DataTable("elem"));
            ds.Tables[0].ReadXmlSchema(new StringReader(schema));
            DataSetAssertion.AssertDataTable("table", ds.Tables[0], "elem", 2, 0, 0, 0, 0, 0);
        }

        [Fact]
        public void UnusedComplexTypesIgnored()
        {
            string xs = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' id='hoge'>
	<xs:element name='Root'>
		<xs:complexType>
			<xs:sequence>
				<xs:element name='Child' type='xs:string' />
			</xs:sequence>
		</xs:complexType>
	</xs:element>
	<xs:complexType name='unusedType'>
		<xs:sequence>
			<xs:element name='Orphan' type='xs:string' />
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                var ds = new DataSet();
                ds.Tables.Add(new DataTable("Root"));
                ds.Tables.Add(new DataTable("unusedType"));
                ds.Tables[0].ReadXmlSchema(new StringReader(xs));
                DataSetAssertion.AssertDataTable("dt", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);
                // Here "unusedType" table is never imported.
                ds.Tables[1].ReadXmlSchema(new StringReader(xs));
            });
        }

        [Fact]
        public void IsDataSetAndTypeIgnored()
        {
            string xsbase = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
	<xs:element name='Root' type='unusedType' msdata:IsDataSet='{0}'>
	</xs:element>
	<xs:complexType name='unusedType'>
		<xs:sequence>
			<xs:element name='Child' type='xs:string' />
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                // When explicit msdata:IsDataSet value is "false", then
                // treat as usual.
                string xs = string.Format(xsbase, "false");
                var ds = new DataSet();
                ds.Tables.Add(new DataTable("Root"));
                ds.Tables[0].ReadXmlSchema(new StringReader(xs));
                DataSetAssertion.AssertDataTable("dt", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

                // Even if a global element uses a complexType, it will be
                // ignored if the element has msdata:IsDataSet='true'
                xs = string.Format(xsbase, "true");
                ds = new DataSet();
                ds.Tables.Add(new DataTable("Root"));
                ds.Tables[0].ReadXmlSchema(new StringReader(xs));
            });
        }

        [Fact]
        public void NestedReferenceNotAllowed()
        {
            string xs = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
	<xs:element name='Root' type='unusedType' msdata:IsDataSet='true'>
	</xs:element>
	<xs:complexType name='unusedType'>
		<xs:sequence>
			<xs:element name='Child' type='xs:string' />
		</xs:sequence>
	</xs:complexType>
	<xs:element name='Foo'>
		<xs:complexType>
			<xs:sequence>
				<xs:element ref='Root' />
			</xs:sequence>
		</xs:complexType>
	</xs:element>
</xs:schema>";

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                // DataSet element cannot be converted into a DataTable.
                // (i.e. cannot be referenced in any other elements)
                var ds = new DataSet();
                ds.Tables.Add(new DataTable());
                ds.Tables[0].ReadXmlSchema(new StringReader(xs));
            });
        }

        [Fact]
        public void LocaleOnRootWithoutIsDataSet()
        {
            string xs = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
	<xs:element name='Root' msdata:Locale='ja-JP'>
		<xs:complexType>
			<xs:sequence>
				<xs:element name='Child' type='xs:string' />
			</xs:sequence>
			<xs:attribute name='Attr' type='xs:integer' />
		</xs:complexType>
	</xs:element>
</xs:schema>";

            var ds = new DataSet();
            ds.Tables.Add("Root");
            ds.Tables[0].ReadXmlSchema(new StringReader(xs));
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "Root", 2, 0, 0, 0, 0, 0);
            Assert.Equal("ja-JP", dt.Locale.Name); // DataTable's Locale comes from msdata:Locale
            DataSetAssertion.AssertDataColumn("col1", dt.Columns[0], "Attr", true, false, 0, 1, "Attr", MappingType.Attribute, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("col2", dt.Columns[1], "Child", false, false, 0, 1, "Child", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
        }

        [Fact]
        public void PrefixedTargetNS()
        {
            string xs = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata' xmlns:x='urn:foo' targetNamespace='urn:foo' elementFormDefault='qualified'>
	<xs:element name='DS' msdata:IsDataSet='true'>
		<xs:complexType>
			<xs:choice>
				<xs:element ref='x:R1' />
				<xs:element ref='x:R2' />
			</xs:choice>
		</xs:complexType>
		<xs:key name='key'>
			<xs:selector xpath='./any/string_is_OK/x:R1'/>
			<xs:field xpath='x:Child2'/>
		</xs:key>
		<xs:keyref name='kref' refer='x:key'>
			<xs:selector xpath='.//x:R2'/>
			<xs:field xpath='x:Child2'/>
		</xs:keyref>
	</xs:element>
	<xs:element name='R3' type='x:RootType' />
	<xs:complexType name='extracted'>
		<xs:choice>
			<xs:element ref='x:R1' />
			<xs:element ref='x:R2' />
		</xs:choice>
	</xs:complexType>
	<xs:element name='R1' type='x:RootType'>
		<xs:unique name='Rkey'>
			<xs:selector xpath='.//x:Child1'/>
			<xs:field xpath='.'/>
		</xs:unique>
		<xs:keyref name='Rkref' refer='x:Rkey'>
			<xs:selector xpath='.//x:Child2'/>
			<xs:field xpath='.'/>
		</xs:keyref>
	</xs:element>
	<xs:element name='R2' type='x:RootType'>
	</xs:element>
	<xs:complexType name='RootType'>
		<xs:choice>
			<xs:element name='Child1' type='xs:string'>
			</xs:element>
			<xs:element name='Child2' type='xs:string' />
		</xs:choice>
		<xs:attribute name='Attr' type='xs:integer' />
	</xs:complexType>
</xs:schema>";
            // No prefixes on tables and columns
            var ds = new DataSet();
            ds.Tables.Add(new DataTable("R3"));
            ds.Tables[0].ReadXmlSchema(new StringReader(xs));
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("R3", dt, "R3", 3, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col1", dt.Columns[0], "Attr", true, false, 0, 1, "Attr", MappingType.Attribute, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
        }

        private void ReadTest1Check(DataSet ds)
        {
            DataSetAssertion.AssertDataSet("dataset", ds, "NewDataSet", 2, 1);
            DataSetAssertion.AssertDataTable("tbl1", ds.Tables[0], "Table1", 3, 0, 0, 1, 1, 0);
            DataSetAssertion.AssertDataTable("tbl2", ds.Tables[1], "Table2", 3, 0, 1, 0, 1, 0);

            DataRelation rel = ds.Relations[0];
            DataSetAssertion.AssertDataRelation("rel", rel, "Rel1", false,
                new string[] { "Column1_3" },
                new string[] { "Column2_1" }, true, true);
            DataSetAssertion.AssertUniqueConstraint("uc", rel.ParentKeyConstraint,
                "Constraint1", false, new string[] { "Column1_3" });
            DataSetAssertion.AssertForeignKeyConstraint("fk", rel.ChildKeyConstraint, "Rel1",
                AcceptRejectRule.None, Rule.Cascade, Rule.Cascade,
                new string[] { "Column2_1" },
                new string[] { "Column1_3" });
        }

        [Fact]
        public void TestSampleFileSimpleTables()
        {
            var ds = new DataSet();
            ds.Tables.Add(new DataTable("foo"));
            ds.Tables[0].ReadXmlSchema(new StringReader(
                @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                <xs:element name='foo' type='ct' />
                <xs:complexType name='ct'>
                  <xs:simpleContent>
                    <xs:extension base='xs:integer'>
                      <xs:attribute name='attr' />
                    </xs:extension>
                  </xs:simpleContent>
                </xs:complexType>
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("005", ds, "NewDataSet", 1, 0);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("tab", dt, "foo", 2, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("attr", dt.Columns[0], "attr", true, false, 0, 1, "attr", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("text", dt.Columns[1], "foo_text", false, false, 0, 1, "foo_text", MappingType.SimpleContent, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

            ds = new DataSet();
            ds.Tables.Add(new DataTable("foo"));
            ds.Tables[0].ReadXmlSchema(new StringReader(
                @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <xs:element name='foo' type='st' />
                    <xs:complexType name='st'>
                      <xs:attribute name='att1' />
                      <xs:attribute name='att2' type='xs:int' default='2' />
                    </xs:complexType>
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("006", ds, "NewDataSet", 1, 0);
            dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("tab", dt, "foo", 2, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("att1", dt.Columns["att1"], "att1", true, false, 0, 1, "att1", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, /*0*/-1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("att2", dt.Columns["att2"], "att2", true, false, 0, 1, "att2", MappingType.Attribute, typeof(int), 2, string.Empty, -1, string.Empty, /*1*/-1, string.Empty, false, false);
        }

        [Fact]
        public void TestSampleFileComplexTables3()
        {
            var ds = new DataSet();
            ds.Tables.Add(new DataTable("e"));
            ds.Tables[0].ReadXmlSchema(new StringReader(
                @"<!-- Modified w3ctests attQ014.xsd -->
                <xsd:schema xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" targetNamespace=""http://xsdtesting"" xmlns:x=""http://xsdtesting"">
	                <xsd:element name=""root"">
		                <xsd:complexType>
			                <xsd:sequence>
				                <xsd:element name=""e"">
					                <xsd:complexType>
						                <xsd:simpleContent>
							                <xsd:extension base=""xsd:decimal"">
								                <xsd:attribute name=""a"" type=""xsd:string""/>
							                </xsd:extension>
						                </xsd:simpleContent>
					                </xsd:complexType>
				                </xsd:element>
			                </xsd:sequence>
		                </xsd:complexType>
	                </xsd:element>
                </xsd:schema>"));
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("root", dt, "e", 2, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("attr", dt.Columns[0], "a", true, false, 0, 1, "a", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("simple", dt.Columns[1], "e_text", false, false, 0, 1, "e_text", MappingType.SimpleContent, typeof(decimal), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
        }

        [Fact]
        public void TestSampleFileXPath()
        {
            var ds = new DataSet();
            ds.Tables.Add(new DataTable("Track"));
            ds.Tables[0].ReadXmlSchema(new StringReader(
                @"<?xml version=""1.0"" encoding=""utf-8"" ?>
                    <xs:schema targetNamespace=""http://neurosaudio.com/Tracks.xsd"" xmlns=""http://neurosaudio.com/Tracks.xsd"" xmlns:mstns=""http://neurosaudio.com/Tracks.xsd"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"" elementFormDefault=""qualified"" id=""Tracks"">
	                    <xs:element name=""Tracks"">
		                    <xs:complexType>
			                    <xs:sequence>
				                    <xs:element name=""Track"" minOccurs=""0"" maxOccurs=""unbounded"">
					                    <xs:complexType>
						                    <xs:sequence>
							                    <xs:element name=""Title"" type=""xs:string"" />
							                    <xs:element name=""Artist"" type=""xs:string"" minOccurs=""0"" />
							                    <xs:element name=""Album"" type=""xs:string"" minOccurs=""0"" />
							                    <xs:element name=""Performer"" type=""xs:string"" minOccurs=""0"" />
							                    <xs:element name=""Sequence"" type=""xs:unsignedInt"" minOccurs=""0"" />
							                    <xs:element name=""Genre"" type=""xs:string"" minOccurs=""0"" />
							                    <xs:element name=""Comment"" type=""xs:string"" minOccurs=""0"" />
							                    <xs:element name=""Year"" type=""xs:string"" minOccurs=""0"" />
							                    <xs:element name=""Duration"" type=""xs:unsignedInt"" minOccurs=""0"" />
							                    <xs:element name=""Path"" type=""xs:string"" />
							                    <xs:element name=""DevicePath"" type=""xs:string"" minOccurs=""0"" />
							                    <xs:element name=""FileSize"" type=""xs:unsignedInt"" minOccurs=""0"" />
							                    <xs:element name=""Source"" type=""xs:string"" minOccurs=""0"" />
							                    <xs:element name=""FlashStatus"" type=""xs:unsignedInt"" />
							                    <xs:element name=""HDStatus"" type=""xs:unsignedInt"" />
						                    </xs:sequence>
						                    <xs:attribute name=""ID"" type=""xs:unsignedInt"" msdata:AutoIncrement=""true"" msdata:AutoIncrementSeed=""1"" />
					                    </xs:complexType>
				                    </xs:element>
			                    </xs:sequence>
		                    </xs:complexType>
		                    <xs:key name=""TrackPK"" msdata:PrimaryKey=""true"">
			                    <xs:selector xpath="".//mstns:Track"" />
			                    <xs:field xpath=""@ID"" />
		                    </xs:key>
	                    </xs:element>
                    </xs:schema>"));
        }

        [Fact]
        public void ReadConstraints()
        {
            const string Schema =
                @"<?xml version=""1.0"" encoding=""utf-8""?>
                <xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
                  <xs:element name=""NewDataSet"" msdata:IsDataSet=""true"" msdata:Locale=""en-US"">
                    <xs:complexType>
                      <xs:choice maxOccurs=""unbounded"">
                        <xs:element name=""Table1"">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name=""col1"" type=""xs:int"" minOccurs=""0"" />
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                        <xs:element name=""Table2"">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name=""col1"" type=""xs:int"" minOccurs=""0"" />
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                      </xs:choice>
                    </xs:complexType>
                    <xs:unique name=""Constraint1"">
                      <xs:selector xpath="".//Table1"" />
                      <xs:field xpath=""col1"" />
                    </xs:unique>
                    <xs:keyref name=""fk1"" refer=""Constraint1"" msdata:ConstraintOnly=""true"">
                      <xs:selector xpath="".//Table2"" />
                      <xs:field xpath=""col1"" />
                    </xs:keyref>
                  </xs:element>
                </xs:schema>";

            var ds = new DataSet();
            ds.Tables.Add(new DataTable());
            ds.Tables.Add(new DataTable());
            ds.Tables[0].ReadXmlSchema(new StringReader(Schema));
            ds.Tables[1].ReadXmlSchema(new StringReader(Schema));
            Assert.Equal(0, ds.Relations.Count);
            Assert.Equal(1, ds.Tables[0].Constraints.Count);
            Assert.Equal(0, ds.Tables[1].Constraints.Count);
            Assert.Equal("Constraint1", ds.Tables[0].Constraints[0].ConstraintName);
        }

        [Fact]
        public void XsdSchemaSerializationIgnoresLocale()
        {
            RemoteExecutor.Invoke(() =>
            {
                var serializer = new BinaryFormatter();
                var table = new DataTable();
                table.Columns.Add(new DataColumn("RowID", typeof(int))
                    {
                        AutoIncrement = true,
                        AutoIncrementSeed = -1, // These lines produce attributes within the schema portion of the underlying XML representation of the DataTable with the values "-1" and "-2".
                        AutoIncrementStep = -2,
                    });
                table.Columns.Add("Value", typeof(string));
                table.Rows.Add(1, "Test");
                table.Rows.Add(2, "Data");

                var buffer = new MemoryStream();
                var savedCulture = CultureInfo.CurrentCulture;
                try
                {
                    // Before serializing, update the culture to use a weird negative number format. This test is ensuring that this is ignored.
                    CultureInfo.CurrentCulture = new CultureInfo("en-US")
                        {
                            NumberFormat = new NumberFormatInfo()
                                {
                                    NegativeSign = "()"
                                }
                        };
                    serializer.Serialize(buffer, table);
                }
                finally
                {
                    CultureInfo.CurrentCulture = savedCulture;
                }

                // The raw serialized data now contains an embedded XML schema. We need to verify that this embedded schema used "-1" for the numeric value
                // negative 1, instead of "()1" as indicated by the current culture.

                string rawSerializedData = System.Text.Encoding.ASCII.GetString(buffer.ToArray());

                const string SchemaStartTag = "<xs:schema";
                const string SchemaEndTag = "</xs:schema>";

                int schemaStart = rawSerializedData.IndexOf(SchemaStartTag);
                int schemaEnd = rawSerializedData.IndexOf(SchemaEndTag);
                Assert.True(schemaStart >= 0);
                Assert.True(schemaEnd > schemaStart);
                Assert.True(rawSerializedData.IndexOf("<xs:schema", schemaStart + 1) < 0);

                schemaEnd += SchemaEndTag.Length;

                string rawSchemaXML = rawSerializedData.Substring(
                    startIndex: schemaStart,
                    length: schemaEnd - schemaStart);
                Assert.Contains(@"AutoIncrementSeed=""-1""", rawSchemaXML);
                Assert.Contains(@"AutoIncrementStep=""-2""", rawSchemaXML);
                Assert.DoesNotContain("()1", rawSchemaXML);
                Assert.DoesNotContain("()2", rawSchemaXML);
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Full framework does not yet have the fix for this bug")]
        public void XsdSchemaDeserializationIgnoresLocale()
        {
            RemoteExecutor.Invoke(() =>
            {
                var serializer = new BinaryFormatter();

                /*

                Test data generator:

                    var table = new DataTable();
                    table.Columns.Add(new DataColumn("RowID", typeof(int))
                        {
                            AutoIncrement = true,
                            AutoIncrementSeed = -1, // These lines produce attributes within the schema portion of the underlying XML representation of the DataTable with the value "-1".
                            AutoIncrementStep = -2,
                        });
                    table.Columns.Add("Value", typeof(string));
                    table.Rows.Add(1, "Test");
                    table.Rows.Add(2, "Data");

                    var buffer = new MemoryStream();
                    serializer.Serialize(buffer, table);

                This test data (binary serializer output) embeds the following XML schema:

                    <?xml version="1.0" encoding="utf-16"?>
                    <xs:schema xmlns="" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
                      <xs:element name="Table1">
                        <xs:complexType>
                          <xs:sequence>
                            <xs:element name="RowID" msdata:AutoIncrement="true" msdata:AutoIncrementSeed="-1" msdata:AutoIncrementStep="-2" type="xs:int" msdata:targetNamespace="" minOccurs="0" />
                            <xs:element name="Value" type="xs:string" msdata:targetNamespace="" minOccurs="0" />
                          </xs:sequence>
                        </xs:complexType>
                      </xs:element>
                      <xs:element name="tmpDataSet" msdata:IsDataSet="true" msdata:MainDataTable="Table1" msdata:UseCurrentLocale="true">
                        <xs:complexType>
                          <xs:choice minOccurs="0" maxOccurs="unbounded" />
                        </xs:complexType>
                      </xs:element>
                    </xs:schema>

                The bug being tested here is that the negative integer values in AutoInecrementSeed and AutoIncrementStep fail to parse because the deserialization code
                incorrectly uses the current culture instead of the invariant culture when parsing strings like "-1" and "-2".

                */

                var buffer = new MemoryStream(new byte[]
                    {
                        0,1,0,0,0,255,255,255,255,1,0,0,0,0,0,0,0,12,2,0,0,0,78,83,121,115,116,101,109,46,68,97,116,97,44,32,86,101,114,115,105,111,110,61,52,46,48,46,48,46,48,44,32,67,117,
                        108,116,117,114,101,61,110,101,117,116,114,97,108,44,32,80,117,98,108,105,99,75,101,121,84,111,107,101,110,61,98,55,55,97,53,99,53,54,49,57,51,52,101,48,56,57,5,1,0,
                        0,0,21,83,121,115,116,101,109,46,68,97,116,97,46,68,97,116,97,84,97,98,108,101,3,0,0,0,25,68,97,116,97,84,97,98,108,101,46,82,101,109,111,116,105,110,103,86,101,114,
                        115,105,111,110,9,88,109,108,83,99,104,101,109,97,11,88,109,108,68,105,102,102,71,114,97,109,3,1,1,14,83,121,115,116,101,109,46,86,101,114,115,105,111,110,2,0,0,0,9,
                        3,0,0,0,6,4,0,0,0,177,6,60,63,120,109,108,32,118,101,114,115,105,111,110,61,34,49,46,48,34,32,101,110,99,111,100,105,110,103,61,34,117,116,102,45,49,54,34,63,62,13,
                        10,60,120,115,58,115,99,104,101,109,97,32,120,109,108,110,115,61,34,34,32,120,109,108,110,115,58,120,115,61,34,104,116,116,112,58,47,47,119,119,119,46,119,51,46,111,
                        114,103,47,50,48,48,49,47,88,77,76,83,99,104,101,109,97,34,32,120,109,108,110,115,58,109,115,100,97,116,97,61,34,117,114,110,58,115,99,104,101,109,97,115,45,109,105,
                        99,114,111,115,111,102,116,45,99,111,109,58,120,109,108,45,109,115,100,97,116,97,34,62,13,10,32,32,60,120,115,58,101,108,101,109,101,110,116,32,110,97,109,101,61,34,
                        84,97,98,108,101,49,34,62,13,10,32,32,32,32,60,120,115,58,99,111,109,112,108,101,120,84,121,112,101,62,13,10,32,32,32,32,32,32,60,120,115,58,115,101,113,117,101,110,
                        99,101,62,13,10,32,32,32,32,32,32,32,32,60,120,115,58,101,108,101,109,101,110,116,32,110,97,109,101,61,34,82,111,119,73,68,34,32,109,115,100,97,116,97,58,65,117,116,
                        111,73,110,99,114,101,109,101,110,116,61,34,116,114,117,101,34,32,109,115,100,97,116,97,58,65,117,116,111,73,110,99,114,101,109,101,110,116,83,101,101,100,61,34,45,
                        49,34,32,109,115,100,97,116,97,58,65,117,116,111,73,110,99,114,101,109,101,110,116,83,116,101,112,61,34,45,50,34,32,116,121,112,101,61,34,120,115,58,105,110,116,34,
                        32,109,115,100,97,116,97,58,116,97,114,103,101,116,78,97,109,101,115,112,97,99,101,61,34,34,32,109,105,110,79,99,99,117,114,115,61,34,48,34,32,47,62,13,10,32,32,32,
                        32,32,32,32,32,60,120,115,58,101,108,101,109,101,110,116,32,110,97,109,101,61,34,86,97,108,117,101,34,32,116,121,112,101,61,34,120,115,58,115,116,114,105,110,103,34,
                        32,109,115,100,97,116,97,58,116,97,114,103,101,116,78,97,109,101,115,112,97,99,101,61,34,34,32,109,105,110,79,99,99,117,114,115,61,34,48,34,32,47,62,13,10,32,32,32,
                        32,32,32,60,47,120,115,58,115,101,113,117,101,110,99,101,62,13,10,32,32,32,32,60,47,120,115,58,99,111,109,112,108,101,120,84,121,112,101,62,13,10,32,32,60,47,120,115,
                        58,101,108,101,109,101,110,116,62,13,10,32,32,60,120,115,58,101,108,101,109,101,110,116,32,110,97,109,101,61,34,116,109,112,68,97,116,97,83,101,116,34,32,109,115,100,
                        97,116,97,58,73,115,68,97,116,97,83,101,116,61,34,116,114,117,101,34,32,109,115,100,97,116,97,58,77,97,105,110,68,97,116,97,84,97,98,108,101,61,34,84,97,98,108,101,
                        49,34,32,109,115,100,97,116,97,58,85,115,101,67,117,114,114,101,110,116,76,111,99,97,108,101,61,34,116,114,117,101,34,62,13,10,32,32,32,32,60,120,115,58,99,111,109,
                        112,108,101,120,84,121,112,101,62,13,10,32,32,32,32,32,32,60,120,115,58,99,104,111,105,99,101,32,109,105,110,79,99,99,117,114,115,61,34,48,34,32,109,97,120,79,99,99,
                        117,114,115,61,34,117,110,98,111,117,110,100,101,100,34,32,47,62,13,10,32,32,32,32,60,47,120,115,58,99,111,109,112,108,101,120,84,121,112,101,62,13,10,32,32,60,47,
                        120,115,58,101,108,101,109,101,110,116,62,13,10,60,47,120,115,58,115,99,104,101,109,97,62,6,5,0,0,0,221,3,60,100,105,102,102,103,114,58,100,105,102,102,103,114,97,
                        109,32,120,109,108,110,115,58,109,115,100,97,116,97,61,34,117,114,110,58,115,99,104,101,109,97,115,45,109,105,99,114,111,115,111,102,116,45,99,111,109,58,120,109,108,
                        45,109,115,100,97,116,97,34,32,120,109,108,110,115,58,100,105,102,102,103,114,61,34,117,114,110,58,115,99,104,101,109,97,115,45,109,105,99,114,111,115,111,102,116,45,
                        99,111,109,58,120,109,108,45,100,105,102,102,103,114,97,109,45,118,49,34,62,13,10,32,32,60,116,109,112,68,97,116,97,83,101,116,62,13,10,32,32,32,32,60,84,97,98,108,
                        101,49,32,100,105,102,102,103,114,58,105,100,61,34,84,97,98,108,101,49,49,34,32,109,115,100,97,116,97,58,114,111,119,79,114,100,101,114,61,34,48,34,32,100,105,102,
                        102,103,114,58,104,97,115,67,104,97,110,103,101,115,61,34,105,110,115,101,114,116,101,100,34,62,13,10,32,32,32,32,32,32,60,82,111,119,73,68,62,49,60,47,82,111,119,73,
                        68,62,13,10,32,32,32,32,32,32,60,86,97,108,117,101,62,84,101,115,116,60,47,86,97,108,117,101,62,13,10,32,32,32,32,60,47,84,97,98,108,101,49,62,13,10,32,32,32,32,60,
                        84,97,98,108,101,49,32,100,105,102,102,103,114,58,105,100,61,34,84,97,98,108,101,49,50,34,32,109,115,100,97,116,97,58,114,111,119,79,114,100,101,114,61,34,49,34,32,
                        100,105,102,102,103,114,58,104,97,115,67,104,97,110,103,101,115,61,34,105,110,115,101,114,116,101,100,34,62,13,10,32,32,32,32,32,32,60,82,111,119,73,68,62,50,60,47,
                        82,111,119,73,68,62,13,10,32,32,32,32,32,32,60,86,97,108,117,101,62,68,97,116,97,60,47,86,97,108,117,101,62,13,10,32,32,32,32,60,47,84,97,98,108,101,49,62,13,10,32,
                        32,60,47,116,109,112,68,97,116,97,83,101,116,62,13,10,60,47,100,105,102,102,103,114,58,100,105,102,102,103,114,97,109,62,4,3,0,0,0,14,83,121,115,116,101,109,46,86,
                        101,114,115,105,111,110,4,0,0,0,6,95,77,97,106,111,114,6,95,77,105,110,111,114,6,95,66,117,105,108,100,9,95,82,101,118,105,115,105,111,110,0,0,0,0,8,8,8,8,2,0,0,0,0,
                        0,0,0,255,255,255,255,255,255,255,255,11
                    });

                DataTable table;
                var savedCulture = CultureInfo.CurrentCulture;
                try
                {
                    // Before deserializing, update the culture to use a weird negative number format. This test is ensuring that this is ignored.
                    // The bug this test is testing would cause "-1" to no longer be treated as a valid representation of the value -1, instead
                    // only accepting the string "()1".
                    CultureInfo.CurrentCulture = new CultureInfo("en-US")
                        {
                            NumberFormat = new NumberFormatInfo()
                                {
                                    NegativeSign = "()"
                                }
                        };
                    table = (DataTable)serializer.Deserialize(buffer); // BUG: System.Exception: "-1 is not a valid value for Int64."        }
                }
                finally
                {
                    CultureInfo.CurrentCulture = savedCulture;
                }

                DataColumn rowIDColumn = table.Columns["RowID"];
                Assert.Equal(-1, rowIDColumn.AutoIncrementSeed);
                Assert.Equal(-2, rowIDColumn.AutoIncrementStep);
            }).Dispose();
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "Exists to provide notification of when the full framework gets the bug fix, at which point the preceding test can be re-enabled")]
        public void XsdSchemaDeserializationOnFullFrameworkStillHasBug()
        {
            var serializer = new BinaryFormatter();
            var buffer = new MemoryStream(new byte[]
                {
                    0,1,0,0,0,255,255,255,255,1,0,0,0,0,0,0,0,12,2,0,0,0,78,83,121,115,116,101,109,46,68,97,116,97,44,32,86,101,114,115,105,111,110,61,52,46,48,46,48,46,48,44,32,67,117,
                    108,116,117,114,101,61,110,101,117,116,114,97,108,44,32,80,117,98,108,105,99,75,101,121,84,111,107,101,110,61,98,55,55,97,53,99,53,54,49,57,51,52,101,48,56,57,5,1,0,
                    0,0,21,83,121,115,116,101,109,46,68,97,116,97,46,68,97,116,97,84,97,98,108,101,3,0,0,0,25,68,97,116,97,84,97,98,108,101,46,82,101,109,111,116,105,110,103,86,101,114,
                    115,105,111,110,9,88,109,108,83,99,104,101,109,97,11,88,109,108,68,105,102,102,71,114,97,109,3,1,1,14,83,121,115,116,101,109,46,86,101,114,115,105,111,110,2,0,0,0,9,
                    3,0,0,0,6,4,0,0,0,177,6,60,63,120,109,108,32,118,101,114,115,105,111,110,61,34,49,46,48,34,32,101,110,99,111,100,105,110,103,61,34,117,116,102,45,49,54,34,63,62,13,
                    10,60,120,115,58,115,99,104,101,109,97,32,120,109,108,110,115,61,34,34,32,120,109,108,110,115,58,120,115,61,34,104,116,116,112,58,47,47,119,119,119,46,119,51,46,111,
                    114,103,47,50,48,48,49,47,88,77,76,83,99,104,101,109,97,34,32,120,109,108,110,115,58,109,115,100,97,116,97,61,34,117,114,110,58,115,99,104,101,109,97,115,45,109,105,
                    99,114,111,115,111,102,116,45,99,111,109,58,120,109,108,45,109,115,100,97,116,97,34,62,13,10,32,32,60,120,115,58,101,108,101,109,101,110,116,32,110,97,109,101,61,34,
                    84,97,98,108,101,49,34,62,13,10,32,32,32,32,60,120,115,58,99,111,109,112,108,101,120,84,121,112,101,62,13,10,32,32,32,32,32,32,60,120,115,58,115,101,113,117,101,110,
                    99,101,62,13,10,32,32,32,32,32,32,32,32,60,120,115,58,101,108,101,109,101,110,116,32,110,97,109,101,61,34,82,111,119,73,68,34,32,109,115,100,97,116,97,58,65,117,116,
                    111,73,110,99,114,101,109,101,110,116,61,34,116,114,117,101,34,32,109,115,100,97,116,97,58,65,117,116,111,73,110,99,114,101,109,101,110,116,83,101,101,100,61,34,45,
                    49,34,32,109,115,100,97,116,97,58,65,117,116,111,73,110,99,114,101,109,101,110,116,83,116,101,112,61,34,45,50,34,32,116,121,112,101,61,34,120,115,58,105,110,116,34,
                    32,109,115,100,97,116,97,58,116,97,114,103,101,116,78,97,109,101,115,112,97,99,101,61,34,34,32,109,105,110,79,99,99,117,114,115,61,34,48,34,32,47,62,13,10,32,32,32,
                    32,32,32,32,32,60,120,115,58,101,108,101,109,101,110,116,32,110,97,109,101,61,34,86,97,108,117,101,34,32,116,121,112,101,61,34,120,115,58,115,116,114,105,110,103,34,
                    32,109,115,100,97,116,97,58,116,97,114,103,101,116,78,97,109,101,115,112,97,99,101,61,34,34,32,109,105,110,79,99,99,117,114,115,61,34,48,34,32,47,62,13,10,32,32,32,
                    32,32,32,60,47,120,115,58,115,101,113,117,101,110,99,101,62,13,10,32,32,32,32,60,47,120,115,58,99,111,109,112,108,101,120,84,121,112,101,62,13,10,32,32,60,47,120,115,
                    58,101,108,101,109,101,110,116,62,13,10,32,32,60,120,115,58,101,108,101,109,101,110,116,32,110,97,109,101,61,34,116,109,112,68,97,116,97,83,101,116,34,32,109,115,100,
                    97,116,97,58,73,115,68,97,116,97,83,101,116,61,34,116,114,117,101,34,32,109,115,100,97,116,97,58,77,97,105,110,68,97,116,97,84,97,98,108,101,61,34,84,97,98,108,101,
                    49,34,32,109,115,100,97,116,97,58,85,115,101,67,117,114,114,101,110,116,76,111,99,97,108,101,61,34,116,114,117,101,34,62,13,10,32,32,32,32,60,120,115,58,99,111,109,
                    112,108,101,120,84,121,112,101,62,13,10,32,32,32,32,32,32,60,120,115,58,99,104,111,105,99,101,32,109,105,110,79,99,99,117,114,115,61,34,48,34,32,109,97,120,79,99,99,
                    117,114,115,61,34,117,110,98,111,117,110,100,101,100,34,32,47,62,13,10,32,32,32,32,60,47,120,115,58,99,111,109,112,108,101,120,84,121,112,101,62,13,10,32,32,60,47,
                    120,115,58,101,108,101,109,101,110,116,62,13,10,60,47,120,115,58,115,99,104,101,109,97,62,6,5,0,0,0,221,3,60,100,105,102,102,103,114,58,100,105,102,102,103,114,97,
                    109,32,120,109,108,110,115,58,109,115,100,97,116,97,61,34,117,114,110,58,115,99,104,101,109,97,115,45,109,105,99,114,111,115,111,102,116,45,99,111,109,58,120,109,108,
                    45,109,115,100,97,116,97,34,32,120,109,108,110,115,58,100,105,102,102,103,114,61,34,117,114,110,58,115,99,104,101,109,97,115,45,109,105,99,114,111,115,111,102,116,45,
                    99,111,109,58,120,109,108,45,100,105,102,102,103,114,97,109,45,118,49,34,62,13,10,32,32,60,116,109,112,68,97,116,97,83,101,116,62,13,10,32,32,32,32,60,84,97,98,108,
                    101,49,32,100,105,102,102,103,114,58,105,100,61,34,84,97,98,108,101,49,49,34,32,109,115,100,97,116,97,58,114,111,119,79,114,100,101,114,61,34,48,34,32,100,105,102,
                    102,103,114,58,104,97,115,67,104,97,110,103,101,115,61,34,105,110,115,101,114,116,101,100,34,62,13,10,32,32,32,32,32,32,60,82,111,119,73,68,62,49,60,47,82,111,119,73,
                    68,62,13,10,32,32,32,32,32,32,60,86,97,108,117,101,62,84,101,115,116,60,47,86,97,108,117,101,62,13,10,32,32,32,32,60,47,84,97,98,108,101,49,62,13,10,32,32,32,32,60,
                    84,97,98,108,101,49,32,100,105,102,102,103,114,58,105,100,61,34,84,97,98,108,101,49,50,34,32,109,115,100,97,116,97,58,114,111,119,79,114,100,101,114,61,34,49,34,32,
                    100,105,102,102,103,114,58,104,97,115,67,104,97,110,103,101,115,61,34,105,110,115,101,114,116,101,100,34,62,13,10,32,32,32,32,32,32,60,82,111,119,73,68,62,50,60,47,
                    82,111,119,73,68,62,13,10,32,32,32,32,32,32,60,86,97,108,117,101,62,68,97,116,97,60,47,86,97,108,117,101,62,13,10,32,32,32,32,60,47,84,97,98,108,101,49,62,13,10,32,
                    32,60,47,116,109,112,68,97,116,97,83,101,116,62,13,10,60,47,100,105,102,102,103,114,58,100,105,102,102,103,114,97,109,62,4,3,0,0,0,14,83,121,115,116,101,109,46,86,
                    101,114,115,105,111,110,4,0,0,0,6,95,77,97,106,111,114,6,95,77,105,110,111,114,6,95,66,117,105,108,100,9,95,82,101,118,105,115,105,111,110,0,0,0,0,8,8,8,8,2,0,0,0,0,
                    0,0,0,255,255,255,255,255,255,255,255,11
                });

            Exception exception;
            CultureInfo savedCulture = CultureInfo.CurrentCulture;
            try
            {
                exception = Assert.Throws<TargetInvocationException>(() =>
                    {
                        // Before deserializing, update the culture to use a weird negative number format. The bug this test is testing causes "-1" to no
                        // longer be treated as a valid representation of the value -1, instead only accepting the string "()1".
                        CultureInfo.CurrentCulture = new CultureInfo("en-US")
                            {
                                NumberFormat = new NumberFormatInfo()
                                    {
                                        NegativeSign = "()"
                                    }
                            };
                        serializer.Deserialize(buffer); // BUG: System.Exception: "-1 is not a valid value for Int64."
                    });
            }
            finally
            {
                CultureInfo.CurrentCulture = savedCulture;
            }

            Assert.IsAssignableFrom<FormatException>(exception.InnerException.InnerException);
        }
    }
}
