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
    public class DataSetReadXmlTest
    {
        private const string xml1 = "";
        private const string xml2 = "<root/>";
        private const string xml3 = "<root></root>";
        private const string xml4 = "<root>   </root>";
        private const string xml5 = "<root>test</root>";
        private const string xml6 = "<root><test>1</test></root>";
        private const string xml7 = "<root><test>1</test><test2>a</test2></root>";
        private const string xml8 = "<dataset><table><col1>foo</col1><col2>bar</col2></table></dataset>";
        private const string xml29 = @"<PersonalSite><License Name='Sum Wang' Email='sumwang@somewhere.net' Mode='Trial' StartDate='01/01/2004' Serial='aaa' /></PersonalSite>";

        private const string diff1 = @"<diffgr:diffgram xmlns:msdata='urn:schemas-microsoft-com:xml-msdata' xmlns:diffgr='urn:schemas-microsoft-com:xml-diffgram-v1'>
  <NewDataSet>
    <Table1 diffgr:id='Table11' msdata:rowOrder='0' diffgr:hasChanges='inserted'>
      <Column1_1>ppp</Column1_1>
      <Column1_2>www</Column1_2>
      <Column1_3>xxx</Column1_3>
    </Table1>
  </NewDataSet>
</diffgr:diffgram>";
        private const string diff2 = diff1 + xml8;

        private const string schema1 = @"<xs:schema xmlns:xs='http://www.w3.org/2001/XMLSchema'>
	<xs:element name='Root'>
		<xs:complexType>
			<xs:sequence>
				<xs:element name='Child' type='xs:string' />
			</xs:sequence>
		</xs:complexType>
	</xs:element>
</xs:schema>";
        private const string schema2 = schema1 + xml8;

        [Fact]
        public void ReadSimpleAuto()
        {
            DataSet ds;

            // empty XML
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyString", xml1,
                XmlReadMode.Auto, XmlReadMode.Auto,
                "NewDataSet", 0);

            // simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyElement", xml2,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "root", 0);

            // simple element2
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "StartEndTag", xml3,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "root", 0);

            // whitespace in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Whitespace", xml4,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "root", 0);

            // text in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SingleText", xml5,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "root", 0);

            // simple table pattern:
            // root becomes a table and test becomes a column.
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable", xml6,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("xml6", ds.Tables[0], "root", 1, 1, 0, 0, 0, 0);

            // simple table with 2 columns:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable2", xml7,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("xml7", ds.Tables[0], "root", 2, 1, 0, 0, 0, 0);

            // simple dataset with 1 table:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleDataSet", xml8,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "dataset", 1);
            DataSetAssertion.AssertDataTable("xml8", ds.Tables[0], "table", 2, 1, 0, 0, 0, 0);
        }

        [Fact]
        public void ReadSimpleDiffgram()
        {
            DataSet ds;

            // empty XML
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyString", xml1,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);

            // simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyElement", xml2,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);

            // simple element2
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "StartEndTag", xml3,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);

            // whitespace in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Whitespace", xml4,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);

            // text in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SingleText", xml5,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);

            // simple table pattern:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable", xml6,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);

            // simple table with 2 columns:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable2", xml7,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);

            // simple dataset with 1 table:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleDataSet", xml8,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);
        }

        [Fact]
        public void ReadSimpleFragment()
        {
            DataSet ds;

            // empty XML
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyString", xml1,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            // simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyElement", xml2,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            // simple element2
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "StartEndTag", xml3,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            // whitespace in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Whitespace", xml4,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            // text in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SingleText", xml5,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            // simple table pattern:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable", xml6,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            // simple table with 2 columns:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable2", xml7,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            // simple dataset with 1 table:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleDataSet", xml8,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);
        }

        [Fact]
        public void ReadSimpleIgnoreSchema()
        {
            DataSet ds;

            // empty XML
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyString", xml1,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            // simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyElement", xml2,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            // simple element2
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "StartEndTag", xml3,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            // whitespace in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Whitespace", xml4,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            // text in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SingleText", xml5,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            // simple table pattern:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable", xml6,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            // simple table with 2 columns:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable2", xml7,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            // simple dataset with 1 table:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleDataSet", xml8,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);
        }

        [Fact]
        public void ReadSimpleInferSchema()
        {
            DataSet ds;

            // empty XML
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyString", xml1,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "NewDataSet", 0);

            // simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyElement", xml2,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "root", 0);

            // simple element2
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "StartEndTag", xml3,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "root", 0);

            // whitespace in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Whitespace", xml4,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "root", 0);

            // text in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SingleText", xml5,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "root", 0);

            // simple table pattern:
            // root becomes a table and test becomes a column.
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable", xml6,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("xml6", ds.Tables[0], "root", 1, 1, 0, 0, 0, 0);

            // simple table with 2 columns:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable2", xml7,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("xml7", ds.Tables[0], "root", 2, 1, 0, 0, 0, 0);

            // simple dataset with 1 table:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleDataSet", xml8,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "dataset", 1);
            DataSetAssertion.AssertDataTable("xml8", ds.Tables[0], "table", 2, 1, 0, 0, 0, 0);
        }

        [Fact]
        public void ReadSimpleReadSchema()
        {
            DataSet ds;

            // empty XML
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyString", xml1,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);

            // simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "EmptyElement", xml2,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);

            // simple element2
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "StartEndTag", xml3,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);

            // whitespace in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Whitespace", xml4,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);

            // text in simple element
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SingleText", xml5,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);

            // simple table pattern:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable", xml6,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);

            // simple table with 2 columns:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleTable2", xml7,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);

            // simple dataset with 1 table:
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "SimpleDataSet", xml8,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);
        }

        [Fact]
        public void TestSimpleDiffXmlAll()
        {
            DataSet ds;

            // ignored
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Fragment", diff1,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "IgnoreSchema", diff1,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "InferSchema", diff1,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "NewDataSet", 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "ReadSchema", diff1,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0);

            // Auto, DiffGram ... treated as DiffGram
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Auto", diff1,
                XmlReadMode.Auto, XmlReadMode.DiffGram,
                "NewDataSet", 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "DiffGram", diff1,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0);
        }

        [Fact]
        public void TestSimpleDiffPlusContentAll()
        {
            DataSet ds;

            // Fragment ... skipped
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Fragment", diff2,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 0);

            // others ... kept 
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "IgnoreSchema", diff2,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0, ReadState.Interactive);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "InferSchema", diff2,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "NewDataSet", 0, ReadState.Interactive);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "ReadSchema", diff2,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 0, ReadState.Interactive);

            // Auto, DiffGram ... treated as DiffGram
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Auto", diff2,
                XmlReadMode.Auto, XmlReadMode.DiffGram,
                "NewDataSet", 0, ReadState.Interactive);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "DiffGram", diff2,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 0, ReadState.Interactive);
        }

        [Fact]
        public void TestSimpleSchemaXmlAll()
        {
            DataSet ds;

            // ignored
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "IgnoreSchema", schema1,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "InferSchema", schema1,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "NewDataSet", 0);

            // misc ... consume schema
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Fragment", schema1,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("fragment", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "ReadSchema", schema1,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("readschema", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Auto", schema1,
                XmlReadMode.Auto, XmlReadMode.ReadSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("auto", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "DiffGram", schema1,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 1);
        }

        [Fact]
        public void TestSimpleSchemaPlusContentAll()
        {
            DataSet ds;

            // ignored
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "IgnoreSchema", schema2,
                XmlReadMode.IgnoreSchema, XmlReadMode.IgnoreSchema,
                "NewDataSet", 0, ReadState.Interactive);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "InferSchema", schema2,
                XmlReadMode.InferSchema, XmlReadMode.InferSchema,
                "NewDataSet", 0, ReadState.Interactive);

            // Fragment ... consumed both
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Fragment", schema2,
                XmlReadMode.Fragment, XmlReadMode.Fragment,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("fragment", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

            // rest ... treated as schema
            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "Auto", schema2,
                XmlReadMode.Auto, XmlReadMode.ReadSchema,
                "NewDataSet", 1, ReadState.Interactive);
            DataSetAssertion.AssertDataTable("auto", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "DiffGram", schema2,
                XmlReadMode.DiffGram, XmlReadMode.DiffGram,
                "NewDataSet", 1, ReadState.Interactive);
            DataSetAssertion.AssertDataTable("diffgram", ds.Tables[0], "Root", 1, 0, 0, 0, 0, 0);

            ds = new DataSet();
            DataSetAssertion.AssertReadXml(ds, "ReadSchema", schema2,
                XmlReadMode.ReadSchema, XmlReadMode.ReadSchema,
                "NewDataSet", 1, ReadState.Interactive);
        }

        [Fact]
        public void SequentialRead1()
        {
            // simple element -> simple table
            var ds = new DataSet();

            DataSetAssertion.AssertReadXml(ds, "SingleText", xml5,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "root", 0);

            DataSetAssertion.AssertReadXml(ds, "SimpleTable", xml6,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("seq1", ds.Tables[0], "root", 1, 1, 0, 0, 0, 0);
        }

        [Fact]
        public void SequentialRead2()
        {
            // simple element -> simple dataset
            var ds = new DataSet();

            DataSetAssertion.AssertReadXml(ds, "SingleText", xml5,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "root", 0);

            DataSetAssertion.AssertReadXml(ds, "SimpleTable2", xml7,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("#1", ds.Tables[0], "root", 2, 1, 0, 0, 0, 0);

            // simple table -> simple dataset
            ds = new DataSet();

            DataSetAssertion.AssertReadXml(ds, "SimpleTable", xml6,
                XmlReadMode.Auto, XmlReadMode.InferSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("#2", ds.Tables[0], "root", 1, 1, 0, 0, 0, 0);

            // Return value became IgnoreSchema, since there is
            // already schema information in the dataset.
            // Columns are kept 1 as old table holds.
            // Rows are up to 2 because of accumulative read.
            DataSetAssertion.AssertReadXml(ds, "SimpleTable2-2", xml7,
                XmlReadMode.Auto, XmlReadMode.IgnoreSchema,
                "NewDataSet", 1);
            DataSetAssertion.AssertDataTable("#3", ds.Tables[0], "root", 1, 2, 0, 0, 0, 0);
        }

        [Fact]
        public void ReadComplexElementDocument()
        {
            var ds = new DataSet();
            ds.ReadXml(new StringReader(xml29));
        }

        [Fact]
        public void IgnoreSchemaShouldFillData()
        {
            // no such dataset
            string xml1 = "<set><tab><col>test</col></tab></set>";
            // no wrapper element
            string xml2 = "<tab><col>test</col></tab>";
            // no such table
            string xml3 = "<tar><col>test</col></tar>";
            var ds = new DataSet();
            DataTable dt = new DataTable("tab");
            ds.Tables.Add(dt);
            dt.Columns.Add("col");
            ds.ReadXml(new StringReader(xml1), XmlReadMode.IgnoreSchema);
            DataSetAssertion.AssertDataSet("ds", ds, "NewDataSet", 1, 0);
            Assert.Equal(1, dt.Rows.Count);
            dt.Clear();

            ds.ReadXml(new StringReader(xml2), XmlReadMode.IgnoreSchema);
            Assert.Equal(1, dt.Rows.Count);
            dt.Clear();

            ds.ReadXml(new StringReader(xml3), XmlReadMode.IgnoreSchema);
            Assert.Equal(0, dt.Rows.Count);
        }

        [Fact]
        public void NameConflictDSAndTable()
        {
            string xml = @"<PriceListDetails> 
	<PriceListList>    
		<Id>1</Id>
	</PriceListList>
	<PriceListDetails> 
		<Id>1</Id>
		<Status>0</Status>
	</PriceListDetails>
</PriceListDetails>";

            var ds = new DataSet();
            ds.ReadXml(new StringReader(xml));
            Assert.NotNull(ds.Tables["PriceListDetails"]);
        }

        [Fact]
        public void ColumnOrder()
        {
            string xml = "<?xml version=\"1.0\" standalone=\"yes\"?>" +
                "<NewDataSet>" +
                "  <Table>" +
                "    <Name>Miguel</Name>" +
                "    <FirstName>de Icaza</FirstName>" +
                "    <Income>4000</Income>" +
                "  </Table>" +
                "  <Table>" +
                "    <Name>25</Name>" +
                "    <FirstName>250</FirstName>" +
                "    <Address>Belgium</Address>" +
                "    <Income>5000</Income>" +
                "</Table>" +
                "</NewDataSet>";

            var ds = new DataSet();
            ds.ReadXml(new StringReader(xml));
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("Table", ds.Tables[0].TableName);
            Assert.Equal(4, ds.Tables[0].Columns.Count);
            Assert.Equal("Name", ds.Tables[0].Columns[0].ColumnName);
            Assert.Equal(0, ds.Tables[0].Columns[0].Ordinal);
            Assert.Equal("FirstName", ds.Tables[0].Columns[1].ColumnName);
            Assert.Equal(1, ds.Tables[0].Columns[1].Ordinal);
            Assert.Equal("Address", ds.Tables[0].Columns[2].ColumnName);
            Assert.Equal(2, ds.Tables[0].Columns[2].Ordinal);
            Assert.Equal("Income", ds.Tables[0].Columns[3].ColumnName);
            Assert.Equal(3, ds.Tables[0].Columns[3].Ordinal);
        }

        [Fact]
        public void XmlSpace()
        {
            string xml = "<?xml version=\"1.0\" standalone=\"yes\"?>" +
                "<NewDataSet>" +
                "  <Table>" +
                "    <Name>Miguel</Name>" +
                "    <FirstName xml:space=\"preserve\"> de Icaza</FirstName>" +
                "    <Income>4000</Income>" +
                "  </Table>" +
                "  <Table>" +
                "    <Name>Chris</Name>" +
                "    <FirstName xml:space=\"preserve\">Toshok </FirstName>" +
                "    <Income>3000</Income>" +
                "  </Table>" +
                "</NewDataSet>";

            var ds = new DataSet();
            ds.ReadXml(new StringReader(xml));
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("Table", ds.Tables[0].TableName);
            Assert.Equal(3, ds.Tables[0].Columns.Count);
            Assert.Equal("Name", ds.Tables[0].Columns[0].ColumnName);
            Assert.Equal(0, ds.Tables[0].Columns[0].Ordinal);
            Assert.Equal("FirstName", ds.Tables[0].Columns[1].ColumnName);
            Assert.Equal(1, ds.Tables[0].Columns[1].Ordinal);
            Assert.Equal("Income", ds.Tables[0].Columns[2].ColumnName);
            Assert.Equal(2, ds.Tables[0].Columns[2].Ordinal);
        }

        public void TestSameParentChildName()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><resource type=\"parent\">" +
                            "<resource type=\"child\" /></resource>";
            var ds = new DataSet();
            ds.ReadXml(new StringReader(xml));

            DataSetAssertion.AssertReadXml(ds, "SameNameParentChild", xml,
                XmlReadMode.Auto, XmlReadMode.IgnoreSchema,
                "NewDataSet", 1);
        }

        public void TestSameColumnName()
        {
            string xml = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><resource resource_Id_0=\"parent\">" +
                            "<resource resource_Id_0=\"child\" /></resource>";
            var ds = new DataSet();
            ds.ReadXml(new StringReader(xml));

            DataSetAssertion.AssertReadXml(ds, "SameColumnName", xml,
                XmlReadMode.Auto, XmlReadMode.IgnoreSchema,
                "NewDataSet", 1);
        }

        [Fact]
        public void DataSetExtendedPropertiesTest()
        {
            DataSet dataSet1 = new DataSet();
            dataSet1.ExtendedProperties.Add("DS1", "extended0");
            DataTable table = new DataTable("TABLE1");
            table.ExtendedProperties.Add("T1", "extended1");
            table.Columns.Add("C1", typeof(int));
            table.Columns.Add("C2", typeof(string));
            table.Columns[1].MaxLength = 20;
            table.Columns[0].ExtendedProperties.Add("C1Ext1", "extended2");
            table.Columns[1].ExtendedProperties.Add("C2Ext1", "extended3");
            dataSet1.Tables.Add(table);
            table.LoadDataRow(new object[] { 1, "One" }, false);
            table.LoadDataRow(new object[] { 2, "Two" }, false);
            string file = Path.Combine(Path.GetTempPath(), "schemas-test.xml");
            try
            {
                dataSet1.WriteXml(file, XmlWriteMode.WriteSchema);
            }
            catch (Exception ex)
            {
                Assert.False(true);
            }
            finally
            {
                File.Delete(file);
            }

            DataSet dataSet2 = new DataSet();
            dataSet2.ReadXml(new StringReader(
                @"<?xml version=""1.0"" standalone=""yes""?>
                <NewDataSet>
                  <xs:schema id=""NewDataSet"" xmlns=""""
                xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata""
                xmlns:msprop=""urn:schemas-microsoft-com:xml-msprop"">
                    <xs:element name=""NewDataSet"" msdata:IsDataSet=""true""
                msdata:UseCurrentLocale=""true"" msprop:DS1=""extended0"">
                      <xs:complexType>
                        <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
                          <xs:element name=""TABLE1"" msprop:T1=""extended1"">
                            <xs:complexType>
                              <xs:sequence>
                                <xs:element name=""C1"" type=""xs:int"" minOccurs=""0""
                msprop:C1Ext1=""extended2"" />
                                <xs:element name=""C2"" type=""xs:string"" minOccurs=""0""
                msprop:C2Ext1=""extended3"" />
                              </xs:sequence>
                            </xs:complexType>
                          </xs:element>
                        </xs:choice>
                      </xs:complexType>
                    </xs:element>
                  </xs:schema>
                  <TABLE1>
                    <C1>1</C1>
                    <C2>One</C2>
                  </TABLE1>
                  <TABLE1>
                    <C1>2</C1>
                    <C2>Two</C2>
                  </TABLE1>
                </NewDataSet>"), XmlReadMode.ReadSchema);
            Assert.Equal(dataSet1.ExtendedProperties["DS1"], dataSet2.ExtendedProperties["DS1"]);

            Assert.Equal(dataSet1.Tables[0].ExtendedProperties["T1"], dataSet2.Tables[0].ExtendedProperties["T1"]);
            Assert.Equal(dataSet1.Tables[0].Columns[0].ExtendedProperties["C1Ext1"],
                             dataSet2.Tables[0].Columns[0].ExtendedProperties["C1Ext1"]);
            Assert.Equal(dataSet1.Tables[0].Columns[1].ExtendedProperties["C2Ext1"],
                             dataSet2.Tables[0].Columns[1].ExtendedProperties["C2Ext1"]);
        }
    }
}
