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
using System.Xml;
using Xunit;

namespace System.Data.Tests
{
    public class DataSetInferXmlSchemaTest
    {
        private string _xml1 = "<root/>";
        private string _xml2 = "<root attr='value' />";
        private string _xml3 = "<root attr='value' attr2='2' />";
        private string _xml4 = "<root>simple.txt</root>";
        private string _xml5 = "<root><child/></root>";
        private string _xml6 = "<root><col1>sample</col1></root>";
        private string _xml7 = @"<root>
<col1>column 1 test</col1>
<col2>column2test</col2>
<col3>3get</col3>
</root>";
        private string _xml8 = @"<set>
<tab>
<col1>1</col1>
<col2>1</col2>
<col3>1</col3>
</tab>
</set>";
        private string _xml9 = @"<el1 attr1='val1' attrA='valA'>
  <el2 attr2='val2' attrB='valB'>
    <el3 attr3='val3' attrC='valC'>3</el3>
      <column2>1</column2>
      <column3>1</column3>
    <el4 attr4='val4' attrD='valD'>4</el4>
  </el2>
</el1>";
        // mixed content
        private string _xml10 = "<root>Here is a <b>mixed</b> content.</root>";
        // xml:space support
        private string _xml11 = @"<root xml:space='preserve'>
   <child_after_significant_space />
</root>";
        // This is useless ... since xml:space becomes a DataColumn here.
        //		string xml12 = "<root xml:space='preserve'>     </root>";
        // The result is silly under MS.NET. It never ignores comment, so
        // They differ:
        //   1) <root>simple string.</root>
        //   2) <root>simple <!-- comment -->string.</root>
        // The same applies to PI.
        //		string xml13 = "<root><tab><col>test <!-- out --> comment</col></tab></root>";

        // simple namespace/prefix support
        private string _xml14 = "<p:root xmlns:p='urn:foo'>test string</p:root>";
        // two tables that have the same content type.
        private string _xml15 = @"<root>
<table1>
        <col1_1>test1</col1_1>
        <col1_2>test2</col1_2>
</table1>
<table2>
        <col2_1>test1</col2_1>
        <col2_2>test2</col2_2>
</table2>
</root>";
        // foo cannot be both table chikd and root child
        private string _xml16 = @"<root>
<table>
        <foo>
                <tableFooChild1>1</tableFooChild1>
                <tableFooChild2>2</tableFooChild2>
        </foo>
        <bar />
</table>
<foo></foo>
<bar />
</root>";
        // simple namespace support
        private string _xml17 = @"<root xmlns='urn:foo' />";
        // conflict between simple and complex type element col
        private string _xml18 = @"<set>
<table>
<col>
        <another_col />
</col>
<col>simple text here.</col>
</table>
</set>";
        // variant of xml18: complex column appeared latter
        private string _xml19 = @"<set>
<table>
<col>simple text</col><!-- ignored -->
<col>
        <another_col />
</col>
</table>
</set>";
        // conflict check (actually it is not conflict) on two "col" tables
        private string _xml20 = @"<set>
<table>
<col>
        <another_col />
</col>
<col attr='value' />
</table>
</set>";
        // conflict between the attribute and the child element
        private string _xml21 = @"<set>
<table>
<col data='value'>
        <data />
</col>
</table>
</set>";
        // simple nest
        private string _xml22 = "<set><table><col><descendant/></col></table><table2><col2>v2</col2></table2></set>";
        /*
          // simple diffgram
          string xml23 = @"<set>
    <xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
      <xs:element name='table'>
        <xs:complexType>
        <xs:choice>
          <xs:any />
        </xs:choice>
        </xs:complexType>
      </xs:element>
    </xs:schema>
    <diffgr:diffgram
          xmlns:msdata='urn:schemas-microsoft-com:xml-msdata'
          xmlns:diffgr='urn:schemas-microsoft-com:xml-diffgram-v1'>
      <table>
        <col>1</col>
      </table>
    </diffgr:diffgram>
  </set>";
          // just deep table
          string xml24 = "<p1><p2><p3><p4><p5><p6/></p5></p4></p3></p2></p1>";
        */

        private DataSet GetDataSet(string xml, string[] nss)
        {
            DataSet ds = new DataSet();
            ds.InferXmlSchema(new XmlTextReader(xml, XmlNodeType.Document, null), nss);
            return ds;
        }

        [Fact]
        public void NullFileName()
        {
            DataSet ds = new DataSet();
            ds.InferXmlSchema((XmlReader)null, null);
            DataSetAssertion.AssertDataSet("null", ds, "NewDataSet", 0, 0);
        }

        [Fact]
        public void SingleElement()
        {
            DataSet ds = GetDataSet(_xml1, null);
            DataSetAssertion.AssertDataSet("xml1", ds, "root", 0, 0);

            ds = GetDataSet(_xml4, null);
            DataSetAssertion.AssertDataSet("xml4", ds, "root", 0, 0);

            // namespaces
            ds = GetDataSet(_xml14, null);
            DataSetAssertion.AssertDataSet("xml14", ds, "root", 0, 0);
            Assert.Equal(string.Empty, ds.Prefix);
            Assert.Equal("urn:foo", ds.Namespace);

            ds = GetDataSet(_xml17, null);
            DataSetAssertion.AssertDataSet("xml17", ds, "root", 0, 0);
            Assert.Equal("urn:foo", ds.Namespace);
        }

        [Fact]
        public void SingleElementWithAttribute()
        {
            DataSet ds = GetDataSet(_xml2, null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "root", 1, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col", dt.Columns[0], "attr", true, false, 0, 1, "attr", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
        }

        [Fact]
        public void SingleElementWithTwoAttribute()
        {
            DataSet ds = GetDataSet(_xml3, null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "root", 2, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col", dt.Columns[0], "attr", true, false, 0, 1, "attr", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("col", dt.Columns[1], "attr2", true, false, 0, 1, "attr2", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
        }

        [Fact]
        public void SingleChild()
        {
            DataSet ds = GetDataSet(_xml5, null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "root", 1, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col", dt.Columns[0], "child", true, false, 0, 1, "child", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);

            ds = GetDataSet(_xml6, null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "root", 1, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col", dt.Columns[0], "col1", true, false, 0, 1, "col1", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
        }

        [Fact]
        public void SimpleElementTable()
        {
            DataSet ds = GetDataSet(_xml7, null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "root", 3, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col", dt.Columns[0], "col1", true, false, 0, 1, "col1", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("col2", dt.Columns[1], "col2", true, false, 0, 1, "col2", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("col3", dt.Columns[2], "col3", true, false, 0, 1, "col3", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 2, string.Empty, false, false);
        }

        [Fact]
        public void SimpleDataSet()
        {
            DataSet ds = GetDataSet(_xml8, null);
            DataSetAssertion.AssertDataSet("ds", ds, "set", 1, 0);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "tab", 3, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col", dt.Columns[0], "col1", true, false, 0, 1, "col1", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("col2", dt.Columns[1], "col2", true, false, 0, 1, "col2", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("col3", dt.Columns[2], "col3", true, false, 0, 1, "col3", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 2, string.Empty, false, false);
        }

        [Fact]
        public void ComplexElementAttributeTable1()
        {
            // FIXME: Also test ReadXml (, XmlReadMode.InferSchema) and
            // make sure that ReadXml() stores DataRow to el1 (and maybe to others)
            DataSet ds = GetDataSet(_xml9, null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 4, 3);
            DataTable dt = ds.Tables[0];

            DataSetAssertion.AssertDataTable("dt1", dt, "el1", 3, 0, 0, 1, 1, 1);
            DataSetAssertion.AssertDataColumn("el1_Id", dt.Columns[0], "el1_Id", false, true, 0, 1, "el1_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, true);
            DataSetAssertion.AssertDataColumn("el1_attr1", dt.Columns[1], "attr1", true, false, 0, 1, "attr1", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el1_attrA", dt.Columns[2], "attrA", true, false, 0, 1, "attrA", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 2, string.Empty, false, false);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("dt2", dt, "el2", 6, 0, 1, 2, 2, 1);
            DataSetAssertion.AssertDataColumn("el2_Id", dt.Columns[0], "el2_Id", false, true, 0, 1, "el2_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, true);
            DataSetAssertion.AssertDataColumn("el2_col2", dt.Columns[1], "column2", true, false, 0, 1, "column2", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el2_col3", dt.Columns[2], "column3", true, false, 0, 1, "column3", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 2, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el2_attr2", dt.Columns[3], "attr2", true, false, 0, 1, "attr2", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 3, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el2_attrB", dt.Columns[4], "attrB", true, false, 0, 1, "attrB", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 4, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el2_el1Id", dt.Columns[5], "el1_Id", true, false, 0, 1, "el1_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 5, string.Empty, false, false);

            dt = ds.Tables[2];
            DataSetAssertion.AssertDataTable("dt3", dt, "el3", 4, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("el3_attr3", dt.Columns[0], "attr3", true, false, 0, 1, "attr3", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el3_attrC", dt.Columns[1], "attrC", true, false, 0, 1, "attrC", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el3_Text", dt.Columns[2], "el3_Text", true, false, 0, 1, "el3_Text", MappingType.SimpleContent, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 2, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el3_el2Id", dt.Columns[3], "el2_Id", true, false, 0, 1, "el2_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 3, string.Empty, false, false);

            dt = ds.Tables[3];
            DataSetAssertion.AssertDataTable("dt4", dt, "el4", 4, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("el3_attr4", dt.Columns[0], "attr4", true, false, 0, 1, "attr4", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el4_attrD", dt.Columns[1], "attrD", true, false, 0, 1, "attrD", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el4_Text", dt.Columns[2], "el4_Text", true, false, 0, 1, "el4_Text", MappingType.SimpleContent, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 2, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("el4_el2Id", dt.Columns[3], "el2_Id", true, false, 0, 1, "el2_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 3, string.Empty, false, false);
        }

        [Fact]
        public void MixedContent()
        {
            // Note that text part is ignored.

            DataSet ds = GetDataSet(_xml10, null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "root", 1, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col", dt.Columns[0], "b", true, false, 0, 1, "b", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
        }

        [Fact]
        public void SignificantWhitespaceIgnored()
        {
            // Note that 1) significant whitespace is ignored, and 
            // 2) xml:space is treated as column (and also note namespaces).
            DataSet ds = GetDataSet(_xml11, null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "root", 1, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("element", dt.Columns[0], "child_after_significant_space", true, false, 0, 1, "child_after_significant_space", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            Assert.Equal(1, dt.Columns.Count);
        }

        [Fact]
        public void SignificantWhitespaceIgnored2()
        {
            // To make sure, create pure significant whitespace element
            // using XmlNodeReader (that does not have xml:space attribute
            // column).
            DataSet ds = new DataSet();
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateElement("root"));
            doc.DocumentElement.AppendChild(doc.CreateSignificantWhitespace
            ("      \n\n"));
            XmlReader xr = new XmlNodeReader(doc);
            ds.InferXmlSchema(xr, null);
            DataSetAssertion.AssertDataSet("pure_whitespace", ds, "root", 0, 0);
        }

        [Fact]
        public void TwoElementTable()
        {
            // FIXME: Also test ReadXml (, XmlReadMode.InferSchema) and
            // make sure that ReadXml() stores DataRow to el1 (and maybe to others)
            DataSet ds = GetDataSet(_xml15, null);
            DataSetAssertion.AssertDataSet("ds", ds, "root", 2, 0);

            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "table1", 2, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col1_1", dt.Columns[0], "col1_1", true, false, 0, 1, "col1_1", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("col1_2", dt.Columns[1], "col1_2", true, false, 0, 1, "col1_2", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("dt", dt, "table2", 2, 0, 0, 0, 0, 0);
            DataSetAssertion.AssertDataColumn("col2_1", dt.Columns[0], "col2_1", true, false, 0, 1, "col2_1", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("col2_2", dt.Columns[1], "col2_2", true, false, 0, 1, "col2_2", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);
        }

        [Fact]
        public void ConflictSimpleComplexColumns()
        {
            DataSet ds = GetDataSet(_xml18, null);
            DataSetAssertion.AssertDataSet("ds", ds, "set", 2, 1);

            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "table", 1, 0, 0, 1, 1, 1);
            DataSetAssertion.AssertDataColumn("table_Id", dt.Columns[0], "table_Id", false, true, 0, 1, "table_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, true);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("dt", dt, "col", 2, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("another_col", dt.Columns[0], "another_col", true, false, 0, 1, "another_col", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("table_refId", dt.Columns[1], "table_Id", true, false, 0, 1, "table_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 1, string.Empty, false, false);

            DataRelation dr = ds.Relations[0];
            DataSetAssertion.AssertDataRelation("rel", dr, "table_col", true, new string[] { "table_Id" }, new string[] { "table_Id" }, true, true);
            DataSetAssertion.AssertUniqueConstraint("uniq", dr.ParentKeyConstraint, "Constraint1", true, new string[] { "table_Id" });
            DataSetAssertion.AssertForeignKeyConstraint("fkey", dr.ChildKeyConstraint, "table_col", AcceptRejectRule.None, Rule.Cascade, Rule.Cascade, new string[] { "table_Id" }, new string[] { "table_Id" });
        }

        [Fact]
        public void ConflictColumnTable()
        {
            DataSet ds = GetDataSet(_xml19, null);
            DataSetAssertion.AssertDataSet("ds", ds, "set", 2, 1);

            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "table", 1, 0, 0, 1, 1, 1);
            DataSetAssertion.AssertDataColumn("table_Id", dt.Columns[0], "table_Id", false, true, 0, 1, "table_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, true);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("dt", dt, "col", 2, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("table_refId", dt.Columns["table_Id"], "table_Id", true, false, 0, 1, "table_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, /*0*/-1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("another_col", dt.Columns["another_col"], "another_col", true, false, 0, 1, "another_col", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, /*1*/-1, string.Empty, false, false);

            DataRelation dr = ds.Relations[0];
            DataSetAssertion.AssertDataRelation("rel", dr, "table_col", true, new string[] { "table_Id" }, new string[] { "table_Id" }, true, true);
            DataSetAssertion.AssertUniqueConstraint("uniq", dr.ParentKeyConstraint, "Constraint1", true, new string[] { "table_Id" });
            DataSetAssertion.AssertForeignKeyConstraint("fkey", dr.ChildKeyConstraint, "table_col", AcceptRejectRule.None, Rule.Cascade, Rule.Cascade, new string[] { "table_Id" }, new string[] { "table_Id" });
        }

        [Fact]
        public void ConflictColumnTableAttribute()
        {
            // Conflicts between a column and a table, additionally an attribute.
            DataSet ds = GetDataSet(_xml20, null);
            DataSetAssertion.AssertDataSet("ds", ds, "set", 2, 1);

            DataTable dt = ds.Tables[0];
            DataSetAssertion.AssertDataTable("dt", dt, "table", 1, 0, 0, 1, 1, 1);
            DataSetAssertion.AssertDataColumn("table_Id", dt.Columns[0], "table_Id", false, true, 0, 1, "table_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, true);

            dt = ds.Tables[1];
            DataSetAssertion.AssertDataTable("dt", dt, "col", 3, 0, 1, 0, 1, 0);
            DataSetAssertion.AssertDataColumn("another_col", dt.Columns[0], "another_col", true, false, 0, 1, "another_col", MappingType.Element, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, 0, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("table_refId", dt.Columns["table_Id"], "table_Id", true, false, 0, 1, "table_Id", MappingType.Hidden, typeof(int), DBNull.Value, string.Empty, -1, string.Empty, /*1*/-1, string.Empty, false, false);
            DataSetAssertion.AssertDataColumn("attr", dt.Columns["attr"], "attr", true, false, 0, 1, "attr", MappingType.Attribute, typeof(string), DBNull.Value, string.Empty, -1, string.Empty, /*2*/-1, string.Empty, false, false);

            DataRelation dr = ds.Relations[0];
            DataSetAssertion.AssertDataRelation("rel", dr, "table_col", true, new string[] { "table_Id" }, new string[] { "table_Id" }, true, true);
            DataSetAssertion.AssertUniqueConstraint("uniq", dr.ParentKeyConstraint, "Constraint1", true, new string[] { "table_Id" });
            DataSetAssertion.AssertForeignKeyConstraint("fkey", dr.ChildKeyConstraint, "table_col", AcceptRejectRule.None, Rule.Cascade, Rule.Cascade, new string[] { "table_Id" }, new string[] { "table_Id" });
        }

        [Fact]
        public void ConflictAttributeDataTable()
        {
            Assert.Throws<DataException>(() =>
           {
               // attribute "data" becomes DataTable, and when column "data"
               // appears, it cannot be DataColumn, since the name is 
               // already allocated for DataTable.
               DataSet ds = GetDataSet(_xml21, null);
           });
        }

        [Fact]
        public void ConflictExistingPrimaryKey()
        {
            Assert.Throws<ConstraintException>(() =>
           {
               // <wrong>The 'col' DataTable tries to create another primary key (and fails)</wrong> The data violates key constraint.
               var ds = new DataSet();
               ds.Tables.Add(new DataTable("table"));
               DataColumn c = new DataColumn("pk");
               ds.Tables[0].Columns.Add(c);
               ds.Tables[0].PrimaryKey = new DataColumn[] { c };
               XmlTextReader xtr = new XmlTextReader(_xml22, XmlNodeType.Document, null);
               xtr.Read();
               ds.ReadXml(xtr, XmlReadMode.InferSchema);
           });
        }

        [Fact]
        public void IgnoredNamespaces()
        {
            string xml = "<root attr='val' xmlns:a='urn:foo' a:foo='hogehoge' />";
            DataSet ds = new DataSet();
            ds.InferXmlSchema(new StringReader(xml), null);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            DataSetAssertion.AssertDataTable("dt", ds.Tables[0], "root", 2, 0, 0, 0, 0, 0);

            ds = new DataSet();
            ds.InferXmlSchema(new StringReader(xml), new string[] { "urn:foo" });
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            // a:foo is ignored
            DataSetAssertion.AssertDataTable("dt", ds.Tables[0], "root", 1, 0, 0, 0, 0, 0);
        }

        [Fact]
        public void ContainsSchema()
        {
            var ds = new DataSet();
            DataTable dt1 = new DataTable();
            ds.Tables.Add(dt1);
            DataColumn dc1 = new DataColumn("Col1");
            dt1.Columns.Add(dc1);
            dt1.Rows.Add(new string[] { "aaa" });
            DataTable dt2 = new DataTable();
            ds.Tables.Add(dt2);
            DataColumn dc2 = new DataColumn("Col2");
            dt2.Columns.Add(dc2);
            dt2.Rows.Add(new string[] { "bbb" });

            DataRelation rel = new DataRelation("Rel1", dc1, dc2, false);
            ds.Relations.Add(rel);

            StringWriter sw = new StringWriter();
            ds.WriteXml(sw, XmlWriteMode.WriteSchema);

            ds = new DataSet();
            ds.ReadXml(new StringReader(sw.ToString()));
            sw = new StringWriter();
            ds.WriteXml(sw);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(sw.ToString());
            Assert.Equal(2, doc.DocumentElement.ChildNodes.Count);
        }
    }
}
