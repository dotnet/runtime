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
using System.Globalization;
using Xunit;

namespace System.Data.Tests
{
    public class DataTableReadXmlSchemaTest : DataSetAssertion, IDisposable
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

        private CultureInfo _currentCultureBackup;

        public DataTableReadXmlSchemaTest()
        {
            _currentCultureBackup = CultureInfo.CurrentCulture; ;
            CultureInfo.CurrentCulture = new CultureInfo("fi-FI");
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _currentCultureBackup;
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
            AssertDataTable("table", ds.Tables[0], "elem", 2, 0, 0, 0, 0, 0);
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

            Assert.Throws<ArgumentException>(() =>
            {
                var ds = new DataSet();
                ds.Tables.Add(new DataTable("Root"));
                ds.Tables.Add(new DataTable("unusedType"));
                ds.Tables[0].ReadXmlSchema(new StringReader(xs));
                AssertDataTable("dt", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);
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

            Assert.Throws<ArgumentException>(() =>
            {
                // When explicit msdata:IsDataSet value is "false", then
                // treat as usual.
                string xs = string.Format(xsbase, "false");
                var ds = new DataSet();
                ds.Tables.Add(new DataTable("Root"));
                ds.Tables[0].ReadXmlSchema(new StringReader(xs));
                AssertDataTable("dt", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

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

            Assert.Throws<ArgumentException>(() =>
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
            AssertDataTable("dt", dt, "Root", 2, 0, 0, 0, 0, 0);
            Assert.Equal("ja-JP", dt.Locale.Name); // DataTable's Locale comes from msdata:Locale
            AssertDataColumn("col1", dt.Columns[0], "Attr", true, false, 0, 1, "Attr", MappingType.Attribute, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            AssertDataColumn("col2", dt.Columns[1], "Child", false, false, 0, 1, "Child", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
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
            AssertDataTable("R3", dt, "R3", 3, 0, 0, 0, 0, 0);
            AssertDataColumn("col1", dt.Columns[0], "Attr", true, false, 0, 1, "Attr", MappingType.Attribute, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
        }

        private void ReadTest1Check(DataSet ds)
        {
            AssertDataSet("dataset", ds, "NewDataSet", 2, 1);
            AssertDataTable("tbl1", ds.Tables[0], "Table1", 3, 0, 0, 1, 1, 0);
            AssertDataTable("tbl2", ds.Tables[1], "Table2", 3, 0, 1, 0, 1, 0);

            DataRelation rel = ds.Relations[0];
            AssertDataRelation("rel", rel, "Rel1", false,
                new string[] { "Column1_3" },
                new string[] { "Column2_1" }, true, true);
            AssertUniqueConstraint("uc", rel.ParentKeyConstraint,
                "Constraint1", false, new string[] { "Column1_3" });
            AssertForeignKeyConstraint("fk", rel.ChildKeyConstraint, "Rel1",
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
            AssertDataSet("005", ds, "NewDataSet", 1, 0);
            DataTable dt = ds.Tables[0];
            AssertDataTable("tab", dt, "foo", 2, 0, 0, 0, 0, 0);
            AssertDataColumn("attr", dt.Columns[0], "attr", true, false, 0, 1, "attr", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            AssertDataColumn("text", dt.Columns[1], "foo_text", false, false, 0, 1, "foo_text", MappingType.SimpleContent, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

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
            AssertDataSet("006", ds, "NewDataSet", 1, 0);
            dt = ds.Tables[0];
            AssertDataTable("tab", dt, "foo", 2, 0, 0, 0, 0, 0);
            AssertDataColumn("att1", dt.Columns["att1"], "att1", true, false, 0, 1, "att1", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, /*0*/-1, string.Empty, false, false);
            AssertDataColumn("att2", dt.Columns["att2"], "att2", true, false, 0, 1, "att2", MappingType.Attribute, typeof(int), 2, string.Empty, -1, string.Empty, /*1*/-1, string.Empty, false, false);
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
            AssertDataTable("root", dt, "e", 2, 0, 0, 0, 0, 0);
            AssertDataColumn("attr", dt.Columns[0], "a", true, false, 0, 1, "a", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            AssertDataColumn("simple", dt.Columns[1], "e_text", false, false, 0, 1, "e_text", MappingType.SimpleContent, typeof(decimal), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
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
    }
}
