// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2006
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
    public class DataTableReadWriteXmlTest
    {
        public static readonly string EOL = Environment.NewLine;

        private void StandardizeXmlFormat(ref string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            StringWriter sw = new StringWriter();
            doc.Save(sw);
            xml = sw.ToString();
        }

        private void GenerateTestData(out DataSet ds,
                              out DataTable dtMainInDS,
                              out DataTable dtChildInDS,
                              out DataTable dtMain)
        {
            ds = new DataSet("MyDataSet");

            // Create a primary table and populate it with some data.  Make a
            // copy of the primary table and put it into the dataset.
            dtMain = new DataTable("Main");
            dtMain.Columns.Add(new DataColumn("ID", typeof(int)));
            dtMain.Columns.Add(new DataColumn("Data", typeof(string)));

            DataRow row = dtMain.NewRow();
            row["ID"] = 1;
            row["Data"] = "One";
            dtMain.Rows.Add(row);

            row = dtMain.NewRow();
            row["ID"] = 2;
            row["Data"] = "Two";
            dtMain.Rows.Add(row);

            row = dtMain.NewRow();
            row["ID"] = 3;
            row["Data"] = "Three";
            dtMain.Rows.Add(row);

            dtMainInDS = dtMain.Copy();
            ds.Tables.Add(dtMainInDS);

            // Create a child table.  Make a copy of the child table and put
            // it into the dataset.
            dtChildInDS = new DataTable("Child");
            dtChildInDS.Columns.Add(new DataColumn("ID", typeof(int)));
            dtChildInDS.Columns.Add(new DataColumn("PID", typeof(int)));
            dtChildInDS.Columns.Add(new DataColumn("ChildData", typeof(string)));

            row = dtChildInDS.NewRow();
            row["ID"] = 1;
            row["PID"] = 1;
            row["ChildData"] = "Parent1Child1";
            dtChildInDS.Rows.Add(row);

            row = dtChildInDS.NewRow();
            row["ID"] = 2;
            row["PID"] = 1;
            row["ChildData"] = "Parent1Child2";
            dtChildInDS.Rows.Add(row);

            row = dtChildInDS.NewRow();
            row["ID"] = 3;
            row["PID"] = 2;
            row["ChildData"] = "Parent2Child3";
            dtChildInDS.Rows.Add(row);

            ds.Tables.Add(dtChildInDS);

            // Set up the relation in the dataset.
            ds.Relations.Add(new DataRelation("MainToChild",
                                              dtMainInDS.Columns["ID"],
                                              dtChildInDS.Columns["PID"]));
        }

        [Fact]
        public void TestWriteXml()
        {
            DataSet ds;
            DataTable dtMainInDS, dtChildInDS, dtMain;

            GenerateTestData(out ds,
                             out dtMainInDS,
                             out dtChildInDS,
                             out dtMain);

            StringWriter sw = new StringWriter();

            // Get XML for DataSet writes.
            sw.GetStringBuilder().Length = 0;
            ds.WriteXml(sw);
            string xmlDSNone = sw.ToString().Replace("\n", EOL).Replace("\r\r\n", EOL);

            sw.GetStringBuilder().Length = 0;
            ds.WriteXml(sw, XmlWriteMode.DiffGram);
            string xmlDSDiffGram = sw.ToString().Replace("\n", EOL).Replace("\r\r\n", EOL);

            sw.GetStringBuilder().Length = 0;
            ds.WriteXml(sw, XmlWriteMode.WriteSchema);
            string xmlDSWriteSchema = sw.ToString();

            // Get XML for recursive DataTable writes of the same data as in
            // the DataSet.
            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw, true);
            string xmlDTNone = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw, XmlWriteMode.DiffGram, true);
            string xmlDTDiffGram = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw, XmlWriteMode.WriteSchema, true);
            string xmlDTWriteSchema = sw.ToString();

            // The schema XML written by the DataTable call has an extra element
            // in the element for the dataset schema definition.  We remove that
            // extra attribute and then check to see if the rest of the xml is
            // identical.
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlDTWriteSchema);
            XmlNode node = doc.DocumentElement.FirstChild.FirstChild;
            XmlAttribute a = (XmlAttribute)node.Attributes.GetNamedItem("msdata:MainDataTable");
            Assert.NotNull(a);
            Assert.Equal("Main", a.Value);

            node.Attributes.Remove(a);
            sw.GetStringBuilder().Length = 0;
            doc.Save(sw);
            xmlDTWriteSchema = sw.ToString();

            StandardizeXmlFormat(ref xmlDSWriteSchema);

            Assert.Equal(xmlDSNone, xmlDTNone);
            Assert.Equal(xmlDSDiffGram, xmlDTDiffGram);
            Assert.True(xmlDSWriteSchema.IndexOf("UseCurrentLocale") > 0);
            Assert.Equal(xmlDSWriteSchema, xmlDTWriteSchema);

            // Now that we've tested writing tables (including children),
            // we will go on to test the cases where the hierarchy flag
            // is false.  For this, we will test one table inside the
            // dataset and one table outside the dataset.

            // First, we fix our test DataSet to only have a single table
            // with no relations.  Then, we go about comparing the XML.
            // Get XML for DataSet writes.
            ds.Tables[1].Constraints.Remove(ds.Tables[1].Constraints[0]);
            ds.Tables[0].Constraints.Remove(ds.Tables[0].Constraints[0]);
            ds.Tables[0].ChildRelations.Remove("MainToChild");
            ds.Tables.Remove("Child");

            sw.GetStringBuilder().Length = 0;
            ds.WriteXml(sw);
            xmlDSNone = sw.ToString().Replace("\n", EOL).Replace("\r\r\n", EOL);

            sw.GetStringBuilder().Length = 0;
            ds.WriteXml(sw, XmlWriteMode.DiffGram);
            xmlDSDiffGram = sw.ToString().Replace("\n", EOL).Replace("\r\r\n", EOL);

            sw.GetStringBuilder().Length = 0;
            ds.WriteXml(sw, XmlWriteMode.WriteSchema);
            xmlDSWriteSchema = sw.ToString();

            // Get all the DataTable.WriteXml results.
            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw);
            string xmlDTNoneInDS = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw, XmlWriteMode.DiffGram);
            string xmlDTDiffGramInDS = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw, XmlWriteMode.WriteSchema);
            string xmlDTWriteSchemaInDS = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMain.WriteXml(sw);
            string xmlDTNoneNoDS = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMain.WriteXml(sw, XmlWriteMode.DiffGram);
            string xmlDTDiffGramNoDS = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMain.WriteXml(sw, XmlWriteMode.WriteSchema);
            string xmlDTWriteSchemaNoDS = sw.ToString();

            Assert.Equal(xmlDSNone, xmlDTNoneInDS);

            // The only difference between the xml output from inside the
            // dataset and the xml output from outside the dataset is that
            // there's a fake <DocumentElement> tag surrounding tbe table
            // in the second case.  We replace it with the name of the
            // dataset for testing purposes.
            doc.LoadXml(xmlDTNoneNoDS);
            Assert.Equal("DocumentElement", doc.DocumentElement.Name);
            sw.GetStringBuilder().Length = 0;
            doc.Save(sw);
            xmlDTNoneNoDS = sw.ToString();
            xmlDTNoneNoDS = xmlDTNoneNoDS.Replace("<DocumentElement>", "<MyDataSet>");
            xmlDTNoneNoDS = xmlDTNoneNoDS.Replace("</DocumentElement>", "</MyDataSet>");

            StandardizeXmlFormat(ref xmlDSNone);

            Assert.Equal(xmlDSNone, xmlDTNoneNoDS);

            // Now check the DiffGram.
            Assert.Equal(xmlDSDiffGram, xmlDTDiffGramInDS);

            doc.LoadXml(xmlDTDiffGramNoDS);
            Assert.Equal("DocumentElement", doc.DocumentElement.FirstChild.Name);
            xmlDTDiffGramNoDS = xmlDTDiffGramNoDS.Replace("<DocumentElement>", "<MyDataSet>");
            xmlDTDiffGramNoDS = xmlDTDiffGramNoDS.Replace("</DocumentElement>", "</MyDataSet>");

            Assert.Equal(xmlDSDiffGram, xmlDTDiffGramNoDS);

            // Finally we check the WriteSchema version of the data.  First
            // we remove the extra "msdata:MainDataTable" attribute from
            // the schema declaration part of the DataTable xml.
            doc = new XmlDocument();
            doc.LoadXml(xmlDTWriteSchemaInDS);
            node = doc.DocumentElement.FirstChild.FirstChild;
            a = (XmlAttribute)node.Attributes.GetNamedItem("msdata:MainDataTable");
            Assert.NotNull(a);
            Assert.Equal("Main", a.Value);
            node.Attributes.Remove(a);
            sw.GetStringBuilder().Length = 0;
            doc.Save(sw);
            xmlDTWriteSchemaInDS = sw.ToString();

            StandardizeXmlFormat(ref xmlDSWriteSchema);

            Assert.Equal(xmlDSWriteSchema, xmlDTWriteSchemaInDS);

            // Remove the extra "msdata:MainDataTable" for the other test case.
            // Also make sure we have "NewDataSet" in the appropriate locations.
            doc = new XmlDocument();
            doc.LoadXml(xmlDTWriteSchemaNoDS);
            node = doc.DocumentElement.FirstChild.FirstChild;
            a = (XmlAttribute)node.Attributes.GetNamedItem("msdata:MainDataTable");
            Assert.NotNull(a);
            Assert.Equal("Main", a.Value);
            node.Attributes.Remove(a);
            sw.GetStringBuilder().Length = 0;
            doc.Save(sw);

            Assert.Equal("NewDataSet", doc.DocumentElement.Name);
            Assert.Equal("NewDataSet", doc.DocumentElement.FirstChild.Attributes["id"].Value);
            Assert.Equal("NewDataSet", doc.DocumentElement.FirstChild.FirstChild.Attributes["name"].Value);

            xmlDTWriteSchemaNoDS = sw.ToString();

            xmlDTWriteSchemaNoDS = xmlDTWriteSchemaNoDS.Replace("<NewDataSet>", "<MyDataSet>");
            xmlDTWriteSchemaNoDS = xmlDTWriteSchemaNoDS.Replace("</NewDataSet>", "</MyDataSet>");
            xmlDTWriteSchemaNoDS = xmlDTWriteSchemaNoDS.Replace("\"NewDataSet\"", "\"MyDataSet\"");

            Assert.Equal(xmlDSWriteSchema, xmlDTWriteSchemaNoDS);
        }

        [Fact]
        public void TestReadXml()
        {
            // For reading, DataTable.ReadXml only supports reading in xml with
            // the schema included.  This means that we can only read in XML
            // that was generated with the WriteSchema flag.  
            DataSet ds;
            DataTable dtMainInDS, dtChildInDS, dtMain;

            GenerateTestData(out ds,
                             out dtMainInDS,
                             out dtChildInDS,
                             out dtMain);

            StringWriter sw = new StringWriter();

            // Get XML for recursive DataTable writes of the same data as in
            // the DataSet.
            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw, true);
            string xmlDTNone = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw, XmlWriteMode.DiffGram, true);
            string xmlDTDiffGram = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMainInDS.WriteXml(sw, XmlWriteMode.WriteSchema, true);
            string xmlMultiTable = sw.ToString();

            sw.GetStringBuilder().Length = 0;
            dtMain.WriteXml(sw, XmlWriteMode.WriteSchema);
            string xmlSingleTable = sw.ToString();

            DataTable newdt = new DataTable();

            try
            {
                newdt.ReadXml(new StringReader(xmlDTNone));
                Assert.False(true);
            }
            catch (InvalidOperationException)
            {
                // DataTable does not support schema inference from Xml.
            }

            try
            {
                newdt.ReadXml(new StringReader(xmlDTDiffGram));
                Assert.False(true);
            }
            catch (InvalidOperationException)
            {
                // DataTable does not support schema inference from Xml.
            }

            DataTable multiTable = new DataTable();
            multiTable.ReadXml(new StringReader(xmlMultiTable));
            // Do some simple checks to see if the main dataset was created
            // and if there are relationships present.
            Assert.Equal("MyDataSet", multiTable.DataSet.DataSetName);
            Assert.Equal(1, multiTable.ChildRelations.Count);
            Assert.Equal(1, multiTable.Constraints.Count);
            // Write the table back out and check to see that the XML is
            // the same as before.
            sw.GetStringBuilder().Length = 0;
            multiTable.WriteXml(sw, XmlWriteMode.WriteSchema, true);
            string xmlMultiTableCheck = sw.ToString();
            Assert.True(xmlMultiTable.IndexOf("UseCurrentLocale") > 0);
            Assert.True(xmlMultiTable.IndexOf("keyref") > 0);
            Assert.Equal(xmlMultiTable, xmlMultiTableCheck);

            DataTable singleTable = new DataTable();
            singleTable.ReadXml(new StringReader(xmlSingleTable));
            // Do some simple checks on the table.
            Assert.Null(singleTable.DataSet);
            Assert.Equal("Main", singleTable.TableName);
            // Write the table out and check if it's the same.
            sw.GetStringBuilder().Length = 0;
            singleTable.WriteXml(sw, XmlWriteMode.WriteSchema);
            string xmlSingleTableCheck = sw.ToString();
            Assert.Equal(xmlSingleTable, xmlSingleTableCheck);
        }
    }
}
