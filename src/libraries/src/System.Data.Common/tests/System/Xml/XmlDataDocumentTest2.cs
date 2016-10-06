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


using System;
using System.Data;
using System.IO;
using System.Xml;
using Xunit;

namespace MonoTests.System.Xml
{
    public class XmlDataDocumentTest2
    {
        private string _xml = "<NewDataSet><table><row><col1>1</col1><col2>2</col2></row></table></NewDataSet>";

        [Fact]
        public void TestCtorNullArgs()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new XmlDataDocument(null);
            });
        }

        [Fact]
        public void TestDefaultCtor()
        {
            XmlDataDocument doc = new XmlDataDocument();
            Assert.NotNull(doc.DataSet);
            Assert.Equal("NewDataSet", doc.DataSet.DataSetName);
        }

        [Fact]
        public void TestMultipleLoadError()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var ds = new DataSet();
                ds.ReadXml(new XmlTextReader(_xml, XmlNodeType.Document, null));
                // If there is already data element, Load() fails.
                XmlDataDocument doc = new XmlDataDocument(ds);
                doc.LoadXml(_xml);
            });
        }

        [Fact]
        public void TestMultipleLoadNoError()
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            dt.Columns.Add("col1");
            ds.Tables.Add(dt);

            XmlDataDocument doc = new XmlDataDocument(ds);
            doc.LoadXml(_xml);
        }

        [Fact]
        public void TestMultipleDataDocFromDataSet()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var ds = new DataSet();
                XmlDataDocument doc = new XmlDataDocument(ds);
                XmlDataDocument doc2 = new XmlDataDocument(ds);
            });
        }

        [Fact]
        public void TestLoadXml()
        {
            XmlDataDocument doc = new XmlDataDocument();
            doc.LoadXml("<NewDataSet><TestTable><TestRow><TestColumn>1</TestColumn></TestRow></TestTable></NewDataSet>");

            doc = new XmlDataDocument();
            doc.LoadXml("<test>value</test>");
        }

        [Fact]
        public void TestCreateElementAndRow()
        {
            DataSet ds = new DataSet("set");
            DataTable dt = new DataTable("tab1");
            dt.Columns.Add("col1");
            dt.Columns.Add("col2");
            ds.Tables.Add(dt);
            DataTable dt2 = new DataTable("child");
            dt2.Columns.Add("ref");
            dt2.Columns.Add("val");
            ds.Tables.Add(dt2);
            DataRelation rel = new DataRelation("rel",
                dt.Columns[0], dt2.Columns[0]);
            rel.Nested = true;
            ds.Relations.Add(rel);
            XmlDataDocument doc = new XmlDataDocument(ds);
            doc.LoadXml("<set><tab1><col1>1</col1><col2/><child><ref>1</ref><val>aaa</val></child></tab1></set>");
            Assert.Equal(1, ds.Tables[0].Rows.Count);
            Assert.Equal(1, ds.Tables[1].Rows.Count);

            // document element - no mapped row
            XmlElement el = doc.DocumentElement;
            Assert.Null(doc.GetRowFromElement(el));

            // tab1 element - has mapped row
            el = el.FirstChild as XmlElement;
            DataRow row = doc.GetRowFromElement(el);
            Assert.NotNull(row);
            Assert.Equal(DataRowState.Added, row.RowState);

            // col1 - it is column. no mapped row
            el = el.FirstChild as XmlElement;
            row = doc.GetRowFromElement(el);
            Assert.Null(row);

            // col2 - it is column. np mapped row
            el = el.NextSibling as XmlElement;
            row = doc.GetRowFromElement(el);
            Assert.Null(row);

            // child - has mapped row
            el = el.NextSibling as XmlElement;
            row = doc.GetRowFromElement(el);
            Assert.NotNull(row);
            Assert.Equal(DataRowState.Added, row.RowState);

            // created (detached) table 1 element (used later)
            el = doc.CreateElement("tab1");
            row = doc.GetRowFromElement(el);
            Assert.Equal(DataRowState.Detached, row.RowState);
            Assert.Equal(1, dt.Rows.Count); // not added yet

            // adding a node before setting EnforceConstraints
            // raises an error
            Assert.Throws<InvalidOperationException>(() => doc.DocumentElement.AppendChild(el));

            // try again...
            ds.EnforceConstraints = false;
            Assert.Equal(1, dt.Rows.Count); // not added yet
            doc.DocumentElement.AppendChild(el);
            Assert.Equal(2, dt.Rows.Count); // added
            row = doc.GetRowFromElement(el);
            Assert.Equal(DataRowState.Added, row.RowState); // changed

            // Irrelevant element
            XmlElement el2 = doc.CreateElement("hoge");
            row = doc.GetRowFromElement(el2);
            Assert.Null(row);

            // created table 2 element (used later)
            el = doc.CreateElement("child");
            row = doc.GetRowFromElement(el);
            Assert.Equal(DataRowState.Detached, row.RowState);

            // Adding it to irrelevant element performs no row state change.
            Assert.Equal(1, dt2.Rows.Count); // not added yet
            el2.AppendChild(el);
            Assert.Equal(1, dt2.Rows.Count); // still not added
            row = doc.GetRowFromElement(el);
            Assert.Equal(DataRowState.Detached, row.RowState); // still detached here
        }

        public void TypedDataDocument()
        {
            string xml = @"<top xmlns=""urn:test"">
  <foo>
    <s>first</s>
    <d>2004-02-14T10:37:03</d>
  </foo>
  <foo>
    <s>second</s>
    <d>2004-02-17T12:41:49</d>
  </foo>
</top>";
            string xmlschema = @"<xs:schema id=""webstore"" targetNamespace=""urn:test"" xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
  <xs:element name=""top"">
    <xs:complexType>
      <xs:sequence maxOccurs=""unbounded"">
        <xs:element name=""foo"">
          <xs:complexType>
            <xs:sequence maxOccurs=""unbounded"">
              <xs:element name=""s"" type=""xs:string""/>
              <xs:element name=""d"" type=""xs:dateTime""/>
            </xs:sequence>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>";
            XmlDataDocument doc = new XmlDataDocument();
            doc.DataSet.ReadXmlSchema(new StringReader(xmlschema));
            doc.LoadXml(xml);
            DataTable foo = doc.DataSet.Tables["foo"];
            DataRow newRow = foo.NewRow();
            newRow["s"] = "new";
            newRow["d"] = DateTime.Now;
            foo.Rows.Add(newRow);
            doc.Save(new StringWriter());
        }

        [Fact]
        public void DataSet_AddRowAfterInitingXmlDataDocument()
        {
            DataSet ds = new DataSet();
            DataTable table = ds.Tables.Add("table1");
            table.Columns.Add("col1", typeof(int));

            XmlDataDocument doc = new XmlDataDocument(ds);
            table.Rows.Add(new object[] { 1 });

            StringWriter sw = new StringWriter();
            doc.Save(sw);

            DataSet ds1 = ds.Clone();
            XmlDataDocument doc1 = new XmlDataDocument(ds1);
            StringReader sr = new StringReader(sw.ToString());
            doc1.Load(sr);

            Assert.Equal(1, ds1.Tables[0].Rows.Count);
            Assert.Equal(1, ds1.Tables[0].Rows[0][0]);
        }

        [Fact]
        public void Rows_With_Null_Values()
        {
            DataSet ds = new DataSet();
            DataTable table = ds.Tables.Add("table1");
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));

            XmlDataDocument doc = new XmlDataDocument(ds);
            table.Rows.Add(new object[] { 1 });

            StringWriter sw = new StringWriter();
            doc.Save(sw);

            DataSet ds1 = ds.Clone();
            XmlDataDocument doc1 = new XmlDataDocument(ds1);
            StringReader sr = new StringReader(sw.ToString());
            doc1.Load(sr);

            Assert.Equal(1, ds1.Tables[0].Rows[0][0]);
            Assert.Equal(true, ds1.Tables[0].Rows[0].IsNull(1));
        }

        [Fact]
        public void DataSet_ColumnNameWithSpaces()
        {
            DataSet ds = new DataSet("New Data Set");

            DataTable table1 = ds.Tables.Add("New Table 1");
            DataTable table2 = ds.Tables.Add("New Table 2");

            table1.Columns.Add("col 1", typeof(int));
            table1.Columns.Add("col 2", typeof(int));

            table1.PrimaryKey = new DataColumn[] { ds.Tables[0].Columns[0] };

            // No exception shud be thrown
            XmlDataDocument doc = new XmlDataDocument(ds);

            // Should not fail to save because of "no rows"
            doc.Save(new StringWriter());

            table1.Rows.Add(new object[] { 0 });
            table1.Rows.Add(new object[] { 1 });
            table1.Rows.Add(new object[] { 2 });

            // No exception shud be thrown
            StringWriter swriter = new StringWriter();
            doc.Save(swriter);

            StringReader sreader = new StringReader(swriter.ToString());
            DataSet ds1 = ds.Clone();
            XmlDataDocument doc1 = new XmlDataDocument(ds1);
            Assert.Equal(0, ds1.Tables[0].Rows.Count);
            doc1.Load(sreader);
            Assert.Equal(3, ds1.Tables[0].Rows.Count);
        }
    }
}
