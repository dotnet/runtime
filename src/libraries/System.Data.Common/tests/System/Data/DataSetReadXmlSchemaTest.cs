// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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


using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using Xunit;

namespace System.Data.Tests
{
    public class DataSetReadXmlSchemaTest : RemoteExecutorTestBase
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
        public void SingleElementTreatmentDifference()
        {
            // This is one of the most complicated case. When the content
            // type particle of 'Root' element is a complex element, it
            // is DataSet element. Otherwise, it is just a data table.
            //
            // But also note that there is another test named
            // LocaleOnRootWithoutIsDataSet(), that tests if locale on
            // the (mere) data table modifies *DataSet's* locale.

            // Moreover, when the schema contains another element
            // (regardless of its schema type), the elements will
            // never be treated as a DataSet.
            string xsbase = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' id='hoge'>
	<xs:element name='Root'> <!-- When simple, it becomes table. When complex, it becomes DataSet -->
		<xs:complexType>
			<xs:choice>
				{0}
			</xs:choice>
		</xs:complexType>
	</xs:element>
</xs:schema>";

            string xsbase2 = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' id='hoge'>
	<xs:element name='Root'> <!-- When simple, it becomes table. When complex, it becomes DataSet -->
		<xs:complexType>
			<xs:choice>
				{0}
			</xs:choice>
		</xs:complexType>
	</xs:element>
	<xs:element name='more' type='xs:string' />
</xs:schema>";

            string simple = "<xs:element name='Child' type='xs:string' />";
            string complex = @"<xs:element name='Child'>
	<xs:complexType>
		<xs:attribute name='a1' />
		<xs:attribute name='a2' type='xs:integer' />
	</xs:complexType>
</xs:element>";
            string elref = "<xs:element ref='more' />";

            string xs2 = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' id='hoge'>
	<xs:element name='Root' type='RootType' />
	<xs:complexType name='RootType'>
		<xs:choice>
			<xs:element name='Child'>
				<xs:complexType>
					<xs:attribute name='a1' />
					<xs:attribute name='a2' type='xs:integer' />
				</xs:complexType>
			</xs:element>
		</xs:choice>
	</xs:complexType>
</xs:schema>";

            var ds = new DataSet();

            string xs = string.Format(xsbase, simple);
            ds.ReadXmlSchema(new StringReader(xs));
            DataSetAssertion.AssertDataSet("simple", ds, "hoge", 1, 0);
            DataSetAssertion.AssertDataTable("simple", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

            // reference to global complex type
            ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs2));
            DataSetAssertion.AssertDataSet("external complexType", ds, "hoge", 2, 1);
            DataSetAssertion.AssertDataTable("external Tab1", ds.Tables[0], "Root", 1, 0, 0, 1, 1, 1);
            DataSetAssertion.AssertDataTable("external Tab2", ds.Tables[1], "Child", 3, 0, 1, 0, 1, 0);

            // xsbase2 + complex -> datatable
            ds = new DataSet();
            xs = string.Format(xsbase2, complex);
            ds.ReadXmlSchema(new StringReader(xs));
            DataSetAssertion.AssertDataSet("complex", ds, "hoge", 2, 1);
            DataSetAssertion.AssertDataTable("complex", ds.Tables[0], "Root", 1, 0, 0, 1, 1, 1);
            DataTable dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("complex", dt, "Child", 3, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("a1", dt.Columns["a1"], "a1", true, false, 0, 1, "a1", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, /*0*/-1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("a2", dt.Columns["a2"], "a2", true, false, 0, 1, "a2", MappingType.Attribute, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, /*1*/-1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("Root_Id", dt.Columns[2], "Root_Id", true, false, 0, 1, "Root_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 2, string.Empty, false, false);

            // xsbase + complex -> dataset
            ds = new DataSet();
            xs = string.Format(xsbase, complex);
            ds.ReadXmlSchema(new StringReader(xs));
            DataSetAssertion.AssertDataSet("complex", ds, "Root", 1, 0);

            ds = new DataSet();
            xs = string.Format(xsbase2, elref);
            ds.ReadXmlSchema(new StringReader(xs));
            DataSetAssertion.AssertDataSet("complex", ds, "hoge", 1, 0);
            DataSetAssertion.AssertDataTable("complex", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);
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
            ds.ReadXmlSchema(new StringReader(schema));
            DataSetAssertion.AssertDataSet("ds", ds, "doc", 1, 0);
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

            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs));
            // Here "unusedType" table is never imported.
            DataSetAssertion.AssertDataSet("ds", ds, "hoge", 1, 0);
            DataSetAssertion.AssertDataTable("dt", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);
        }

        [Fact]
        public void SimpleTypeComponentsIgnored()
        {
            string xs = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
	<xs:element name='Root' type='xs:string'/>
	<xs:attribute name='Attr' type='xs:string'/>
</xs:schema>";

            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs));
            // nothing is imported.
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 0, 0);
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

            // Even if a global element uses a complexType, it will be
            // ignored if the element has msdata:IsDataSet='true'
            string xs = string.Format(xsbase, "true");

            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs));
            DataSetAssertion.AssertDataSet("ds", ds, "Root", 0, 0); // name is "Root"

            // But when explicit msdata:IsDataSet value is "false", then
            // treat as usual.
            xs = string.Format(xsbase, "false");

            ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs));
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
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

            // DataSet element cannot be converted into a DataTable.
            // (i.e. cannot be referenced in any other elements)
            var ds = new DataSet();
            AssertExtensions.Throws<ArgumentException>(null, () =>
           {
               ds.ReadXmlSchema(new StringReader(xs));
           });
        }

        [Fact]
        public void IsDataSetOnLocalElementIgnored()
        {
            string xsbase = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
	<xs:element name='Root' type='unusedType'>
	</xs:element>
	<xs:complexType name='unusedType'>
		<xs:sequence>
			<xs:element name='Child' type='xs:string' msdata:IsDataSet='True' />
		</xs:sequence>
	</xs:complexType>
</xs:schema>";

            // msdata:IsDataSet does not affect even if the value is invalid
            string xs = string.Format(xsbase, "true");

            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs));
            // Child should not be regarded as DataSet element
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
        }

        [Fact]
        public void LocaleOnRootWithoutIsDataSet()
        {
            RemoteInvoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("fi-FI");
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
                ds.ReadXmlSchema(new StringReader(xs));
                DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
                Assert.Equal("fi-FI", ds.Locale.Name); // DataSet's Locale comes from current thread
                DataTable dt = ds.Tables[0];
                DataSetAssertion.AssertDataTable("dt", dt, "Root", 2, 0, 0, 0, 0, 0);
                Assert.Equal("ja-JP", dt.Locale.Name); // DataTable's Locale comes from msdata:Locale
                DataSetAssertion.AssertDataColumn("col1", dt.Columns[0], "Attr", true, false, 0, 1, "Attr", MappingType.Attribute, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
                DataSetAssertion.AssertDataColumn("col2", dt.Columns[1], "Child", false, false, 0, 1, "Child", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

                return SuccessExitCode;
            }).Dispose();
        }


        [Fact]
        public void ElementHasIdentityConstraint()
        {
            string constraints = @"
		<xs:key name='key'>
			<xs:selector xpath='./any/string_is_OK/R1'/>
			<xs:field xpath='Child2'/>
		</xs:key>
		<xs:keyref name='kref' refer='key'>
			<xs:selector xpath='.//R2'/>
			<xs:field xpath='Child2'/>
		</xs:keyref>";
            string xsbase = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'>
	<xs:element name='DS' msdata:IsDataSet='true'>
		<xs:complexType>
			<xs:choice>
				<xs:element ref='R1' />
				<xs:element ref='R2' />
			</xs:choice>
		</xs:complexType>
		{0}
	</xs:element>
	<xs:element name='R1' type='RootType'>
	      {1}
	</xs:element>
	<xs:element name='R2' type='RootType'>
	</xs:element>
	<xs:complexType name='RootType'>
		<xs:choice>
			<xs:element name='Child1' type='xs:string'>
				{2}
			</xs:element>
			<xs:element name='Child2' type='xs:string' />
		</xs:choice>
		<xs:attribute name='Attr' type='xs:integer' />
	</xs:complexType>
</xs:schema>";

            // Constraints on DataSet element.
            // Note that in xs:key xpath is crazy except for the last step
            string xs = string.Format(xsbase, constraints, string.Empty, string.Empty);
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs));
            Assert.Equal(1, ds.Relations.Count);

            // Constraints on another global element - just ignored
            xs = string.Format(xsbase, string.Empty, constraints, string.Empty);
            ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs));
            Assert.Equal(0, ds.Relations.Count);

            // Constraints on local element - just ignored
            xs = string.Format(xsbase, string.Empty, string.Empty, constraints);
            ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(xs));
            Assert.Equal(0, ds.Relations.Count);
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
            ds.ReadXmlSchema(new StringReader(xs));
            DataSetAssertion.AssertDataSet("ds", ds, "DS", 3, 1);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("R3", dt, "R3", 3, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col1", dt.Columns[0], "Attr", true, false, 0, 1, "Attr", MappingType.Attribute, typeof(long), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
        }

        [Fact]
        public void ReadTest1()
        {
            DataSet ds = CreateTestSet();

            StringWriter sw = new StringWriter();
            ds.WriteXmlSchema(sw);

            string schema = sw.ToString();

            // ReadXmlSchema()
            ds = new DataSet();
            ds.ReadXmlSchema(new XmlTextReader(schema, XmlNodeType.Document, null));
            ReadTest1Check(ds);

            // ReadXml() should also be the same
            ds = new DataSet();
            ds.ReadXml(new XmlTextReader(schema, XmlNodeType.Document, null));
            ReadTest1Check(ds);
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
        // 001-004
        public void TestSampleFileNoTables()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(@"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><!-- empty --></xs:schema>"));
            DataSetAssertion.AssertDataSet("001", ds, "NewDataSet", 0, 0);

            ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(@"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'><xs:element name='foo' /></xs:schema>"));
            DataSetAssertion.AssertDataSet("002", ds, "NewDataSet", 0, 0);

            ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <xs:element name='foo' type='xs:integer' />
                    <xs:element name='bar' type='xs:string' />
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("003", ds, "NewDataSet", 0, 0);

            ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                    <xs:element name='foo' type='st' />
                    <xs:simpleType name='st'>
                      <xs:restriction base='xs:string'>
                        <xs:maxLength value='5' />
                      </xs:restriction>
                    </xs:simpleType>
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("004", ds, "NewDataSet", 0, 0);
        }

        [Fact]
        public void TestSampleFileSimpleTables()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
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
            ds.ReadXmlSchema(new StringReader(
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
        public void TestSampleFileComplexTables()
        {
            // Nested simple type element
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<!-- nested tables, root references to complex type -->
                <xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:foo' xmlns:x='urn:foo'>
                  <xs:element name='uno' type='x:t' />
                  <xs:complexType name='t'>
                    <xs:sequence>
                      <xs:element name='des'>
                        <xs:complexType>
                          <xs:sequence>
                            <xs:element name='tres' />
                          </xs:sequence>
                        </xs:complexType>
                      </xs:element>
                    </xs:sequence>
                  </xs:complexType>
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("007", ds, "NewDataSet", 2, 1);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("tab1", dt, "uno", 1, 0, 0, 1, 1, 1);
            DataSetAssertion.AssertDataColumn("id", dt.Columns[0], "uno_Id", false, true, 0, 1, "uno_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, "urn:foo", 0, string.Empty, false, true);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("tab2", dt, "des", 2, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("child", dt.Columns[0], "tres", false, false, 0, 1, "tres", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("id", dt.Columns[1], "uno_Id", true, false, 0, 1, "uno_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

            // External simple type element
            ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<!-- reference to external simple element -->
                <xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema' targetNamespace='urn:foo' xmlns:x='urn:foo'>
                  <xs:element name='uno' type='x:t' />
                  <xs:element name='tres' type='xs:string' />
                  <xs:complexType name='t'>
                    <xs:sequence>
                      <xs:element name='des'>
                        <xs:complexType>
                          <xs:sequence>
                            <xs:element ref='x:tres' />
                          </xs:sequence>
                        </xs:complexType>
                      </xs:element>
                    </xs:sequence>
                  </xs:complexType>
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("008", ds, "NewDataSet", 2, 1);
            dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("tab1", dt, "uno", 1, 0, 0, 1, 1, 1);
            DataSetAssertion.AssertDataColumn("id", dt.Columns[0], "uno_Id", false, true, 0, 1, "uno_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, "urn:foo", 0, string.Empty, false, true);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("tab2", dt, "des", 2, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("child", dt.Columns[0], "tres", false, false, 0, 1, "tres", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, "urn:foo", 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("id", dt.Columns[1], "uno_Id", true, false, 0, 1, "uno_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
        }

        [Fact]
        public void TestSampleFileComplexTables3()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
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
            DataSetAssertion.AssertDataSet("013", ds, "root", 1, 0);

            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("root", dt, "e", 2, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("attr", dt.Columns[0], "a", true, false, 0, 1, "a", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("simple", dt.Columns[1], "e_text", false, false, 0, 1, "e_text", MappingType.SimpleContent, typeof(decimal), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
        }

        [Fact]
        public void TestSampleFileXPath()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
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
        public void TestAnnotatedRelation1()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<xs:schema xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
                 <xs:element name=""root"" msdata:IsDataSet=""true"">
                  <xs:complexType>
                    <xs:choice maxOccurs=""unbounded"">
                      <xs:element name=""p"">
                       <xs:complexType>
                         <xs:sequence>
                           <xs:element name=""pk"" type=""xs:string"" />
                           <xs:element name=""name"" type=""xs:string"" />
                         </xs:sequence>
                       </xs:complexType>
                      </xs:element>
                      <xs:element name=""c"">
                       <xs:complexType>
                         <xs:sequence>
                           <xs:element name=""fk"" type=""xs:string"" />
                           <xs:element name=""count"" type=""xs:int"" />
                         </xs:sequence>
                       </xs:complexType>
                      </xs:element>
                    </xs:choice>
                  </xs:complexType>

                  </xs:element>
                   <xs:annotation>
                     <xs:appinfo>
                       <msdata:Relationship name=""rel""
                                            msdata:parent=""p"" 
                                            msdata:child=""c"" 
                                            msdata:parentkey=""pk"" 
                                            msdata:childkey=""fk""/>
                     </xs:appinfo>
                  </xs:annotation>
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("101", ds, "root", 2, 1);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("parent_table", dt, "p", 2, 0, 0, 1, 0, 0);
            DataSetAssertion.AssertDataColumn("pk", dt.Columns[0], "pk", false, false, 0, 1, "pk", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("child_table", dt, "c", 2, 0, 1, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("fk", dt.Columns[0], "fk", false, false, 0, 1, "fk", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);

            DataSetAssertion.AssertDataRelation("rel", ds.Relations[0], "rel", false, new string[] { "pk" }, new string[] { "fk" }, false, false);
        }

        [Fact]
        public void TestAnnotatedRelation2()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
                <!-- just modified MSDN example -->
                <xs:element name=""ds"" msdata:IsDataSet=""true"">
                 <xs:complexType>
                  <xs:choice maxOccurs=""unbounded"">
                   <xs:element name=""p"">
                    <xs:complexType>
                     <xs:sequence>
                       <xs:element name=""pk"" type=""xs:string"" />
                       <xs:element name=""name"" type=""xs:string"" />
                       <xs:element name=""c"">
                          <xs:annotation>
                           <xs:appinfo>
                            <msdata:Relationship name=""rel"" 
                             msdata:parent=""p"" 
                             msdata:child=""c"" 
                             msdata:parentkey=""pk"" 
                             msdata:childkey=""fk""/>
                           </xs:appinfo>
                          </xs:annotation>
                          <xs:complexType>
                            <xs:sequence>
                             <xs:element name=""fk"" type=""xs:string"" />
                             <xs:element name=""count"" type=""xs:int"" />
                            </xs:sequence>
                         </xs:complexType>
                       </xs:element>
                     </xs:sequence>
                    </xs:complexType>
                   </xs:element>
                  </xs:choice>
                 </xs:complexType>
                </xs:element>
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("102", ds, "ds", 2, 1);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("parent_table", dt, "p", 2, 0, 0, 1, 0, 0);
            DataSetAssertion.AssertDataColumn("pk", dt.Columns[0], "pk", false, false, 0, 1, "pk", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("child_table", dt, "c", 2, 0, 1, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("fk", dt.Columns[0], "fk", false, false, 0, 1, "fk", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);

            DataSetAssertion.AssertDataRelation("rel", ds.Relations[0], "rel", true, new string[] { "pk" }, new string[] { "fk" }, false, false);
        }

        [Fact]
        public void RepeatableSimpleElement()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
                  <!-- empty -->
                  <xs:element name='Foo' type='FooType' />
                  <!-- defining externally to avoid being regarded as dataset element -->
                  <xs:complexType name='FooType'>
	                <xs:sequence>
		                <xs:element name='Bar' maxOccurs='2' />
	                </xs:sequence>
                  </xs:complexType>
                </xs:schema>"));
            DataSetAssertion.AssertDataSet("012", ds, "NewDataSet", 2, 1);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("parent", dt, "Foo", 1, 0, 0, 1, 1, 1);
            DataSetAssertion.AssertDataColumn("key", dt.Columns[0], "Foo_Id", false, true, 0, 1, "Foo_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, true);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("repeated", dt, "Bar", 2, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("data", dt.Columns[0], "Bar_Column", false, false, 0, 1, "Bar_Column", MappingType.SimpleContent, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("refkey", dt.Columns[1], "Foo_Id", true, false, 0, 1, "Foo_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

            DataSetAssertion.AssertDataRelation("rel", ds.Relations[0], "Foo_Bar", true, new string[] { "Foo_Id" }, new string[] { "Foo_Id" }, true, true);
        }

        [Fact]
        public void TestMoreThanOneRepeatableColumns()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<xsd:schema xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
	                <xsd:element name=""root"">
		                <xsd:complexType>
			                <xsd:sequence>
				                <xsd:element name=""x"" maxOccurs=""2"" />
				                <xsd:element ref=""y"" maxOccurs=""unbounded"" />
			                </xsd:sequence>
		                </xsd:complexType>
	                </xsd:element>
	                <xsd:element name=""y"" />
                </xsd:schema>"));
            DataSetAssertion.AssertDataSet("014", ds, "NewDataSet", 3, 2);

            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("parent", dt, "root", 1, 0, 0, 2, 1, 1);
            DataSetAssertion.AssertDataColumn("key", dt.Columns[0], "root_Id", false, true, 0, 1, "root_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, true);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("repeated", dt, "x", 2, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("data_1", dt.Columns[0], "x_Column", false, false, 0, 1, "x_Column", MappingType.SimpleContent, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("refkey_1", dt.Columns[1], "root_Id", true, false, 0, 1, "root_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

            dt = ds.Tables[2];
            DataSetAssertion.AssertDataTable("repeated", dt, "y", 2, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("data", dt.Columns[0], "y_Column", false, false, 0, 1, "y_Column", MappingType.SimpleContent, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("refkey", dt.Columns[1], "root_Id", true, false, 0, 1, "root_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

            DataSetAssertion.AssertDataRelation("rel", ds.Relations[0], "root_x", true, new string[] { "root_Id" }, new string[] { "root_Id" }, true, true);

            DataSetAssertion.AssertDataRelation("rel", ds.Relations[1], "root_y", true, new string[] { "root_Id" }, new string[] { "root_Id" }, true, true);
        }

        [Fact]
        public void AutoIncrementStep()
        {
            DataSet ds = new DataSet("testds");
            DataTable tbl = ds.Tables.Add("testtbl");
            DataColumn col = tbl.Columns.Add("id", typeof(int));
            col.AutoIncrement = true;
            col.AutoIncrementSeed = -1;
            col.AutoIncrementStep = -1;
            tbl.Columns.Add("data", typeof(string));
            Assert.True(ds.GetXmlSchema().IndexOf("AutoIncrementStep") > 0);
        }

        [Fact]
        public void ReadConstraints()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
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
                </xs:schema>"));

            Assert.Equal(0, ds.Relations.Count);
            Assert.Equal(1, ds.Tables[0].Constraints.Count);
            Assert.Equal(1, ds.Tables[1].Constraints.Count);
            Assert.Equal("fk1", ds.Tables[1].Constraints[0].ConstraintName);
        }

        [Fact]
        public void ReadAnnotatedRelations_MultipleColumns()
        {
            var ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<?xml version=""1.0"" standalone=""yes""?>
                <xs:schema id=""NewDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
                  <xs:element name=""NewDataSet"" msdata:IsDataSet=""true"">
                    <xs:complexType>
                      <xs:choice maxOccurs=""unbounded"">
                        <xs:element name=""Table1"">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name=""col_x0020_1"" type=""xs:int"" minOccurs=""0"" />
                              <xs:element name=""col2"" type=""xs:int"" minOccurs=""0"" />
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                        <xs:element name=""Table2"">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name=""col1"" type=""xs:int"" minOccurs=""0"" />
                              <xs:element name=""col_x0020__x0020_2"" type=""xs:int"" minOccurs=""0"" />
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                      </xs:choice>
                    </xs:complexType>
                  </xs:element>
                  <xs:annotation>
                    <xs:appinfo>
                      <msdata:Relationship name=""rel"" msdata:parent=""Table1"" msdata:child=""Table2"" msdata:parentkey=""col_x0020_1 col2"" msdata:childkey=""col1 col_x0020__x0020_2"" />
                    </xs:appinfo>
                  </xs:annotation>
                </xs:schema>"));

            Assert.Equal(1, ds.Relations.Count);
            Assert.Equal("rel", ds.Relations[0].RelationName);
            Assert.Equal(2, ds.Relations[0].ParentColumns.Length);
            Assert.Equal(2, ds.Relations[0].ChildColumns.Length);
            Assert.Equal(0, ds.Tables[0].Constraints.Count);
            Assert.Equal(0, ds.Tables[1].Constraints.Count);

            DataSetAssertion.AssertDataRelation("TestRel", ds.Relations[0], "rel", false, new string[] { "col 1", "col2" },
                    new string[] { "col1", "col  2" }, false, false);
        }
    }
}
