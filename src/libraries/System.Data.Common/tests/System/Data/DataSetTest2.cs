// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2004 Mainsoft Co.
// Copyright (c) 2009 Novell Inc.
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

using System.Text;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Tests;
using System.Globalization;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Data.Tests
{
    public class DataSetTest2
    {
        private DataSet _ds = null;
        private bool _eventRaised = false;

        [Fact]
        public void AcceptChanges()
        {
            var ds = new DataSet();
            DataTable dtP = DataProvider.CreateParentDataTable();
            DataTable dtC = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dtP);
            ds.Tables.Add(dtC);
            ds.Relations.Add(new DataRelation("myRelation", dtP.Columns[0], dtC.Columns[0]));

            //create changes
            dtP.Rows[0][0] = "70";
            dtP.Rows[1].Delete();
            dtP.Rows.Add(new object[] { 9, "string1", "string2" });

            // AcceptChanges
            ds.AcceptChanges();
            Assert.Equal(null, dtP.GetChanges());

            //read only exception
            dtP.Columns[0].ReadOnly = true;
            // check ReadOnlyException
            Assert.Throws<ReadOnlyException>(() => dtP.Rows[0][0] = 99);

            // check invoke AcceptChanges
            ds.AcceptChanges();
        }

        [Fact]
        public void CaseSensitive()
        {
            var ds = new DataSet();
            DataTable dt = new DataTable();

            // CaseSensitive - default value (false)
            Assert.Equal(false, ds.CaseSensitive);

            ds.CaseSensitive = true;

            // CaseSensitive - get
            Assert.Equal(true, ds.CaseSensitive);

            //add a datatable to a dataset
            ds.Tables.Add(dt);
            // DataTable CaseSensitive from DataSet - true
            Assert.Equal(true, dt.CaseSensitive);

            ds.Tables.Clear();
            ds.CaseSensitive = false;
            dt = new DataTable();
            ds.Tables.Add(dt);

            // DataTable CaseSensitive from DataSet - false
            Assert.Equal(false, dt.CaseSensitive);

            //change DataSet CaseSensitive and check DataTables in it
            ds.Tables.Clear();
            ds.CaseSensitive = false;
            dt = new DataTable();
            ds.Tables.Add(dt);

            // Change DataSet CaseSensitive - check Table - true
            ds.CaseSensitive = true;
            Assert.Equal(true, dt.CaseSensitive);

            // Change DataSet CaseSensitive - check Table - false
            ds.CaseSensitive = false;
            Assert.Equal(false, dt.CaseSensitive);

            //Add new table to DataSet with CaseSensitive,check the table case after adding it to DataSet
            ds.Tables.Clear();
            ds.CaseSensitive = true;
            dt = new DataTable();
            dt.CaseSensitive = false;
            ds.Tables.Add(dt);

            // DataTable get case sensitive from DataSet - false
            Assert.Equal(false, dt.CaseSensitive);

            ds.Tables.Clear();
            ds.CaseSensitive = false;
            dt = new DataTable();
            dt.CaseSensitive = true;
            ds.Tables.Add(dt);

            // DataTable get case sensitive from DataSet - true
            Assert.Equal(true, dt.CaseSensitive);

            //Add new table to DataSet and change the DataTable CaseSensitive
            ds.Tables.Clear();
            ds.CaseSensitive = true;
            dt = new DataTable();
            ds.Tables.Add(dt);

            // Add new table to DataSet and change the DataTable CaseSensitive - false
            dt.CaseSensitive = false;
            Assert.Equal(false, dt.CaseSensitive);

            ds.Tables.Clear();
            ds.CaseSensitive = false;
            dt = new DataTable();
            ds.Tables.Add(dt);

            // Add new table to DataSet and change the DataTable CaseSensitive - true
            dt.CaseSensitive = true;
            Assert.Equal(true, dt.CaseSensitive);

            //Add DataTable to Dataset, Change DataSet CaseSensitive, check DataTable
            ds.Tables.Clear();
            ds.CaseSensitive = true;
            dt = new DataTable();
            dt.CaseSensitive = true;
            ds.Tables.Add(dt);

            // Add DataTable to Dataset, Change DataSet CaseSensitive, check DataTable - true
            ds.CaseSensitive = false;
            Assert.Equal(true, dt.CaseSensitive);
        }

        [Fact]
        public void Clear()
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());
            ds.Tables[0].Rows.Add(new object[] { 9, "", "" });

            // Clear
            ds.Clear();
            Assert.Equal(0, ds.Tables[0].Rows.Count);
        }

        [Fact]
        public void Clear_WithNoDataWithConstraint()
        {
            // Test dataset with no data and with constraint
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());
            ds.Tables.Add(DataProvider.CreateChildDataTable());
            ds.Tables[0].Rows.Clear();
            ds.Tables[1].Rows.Clear();

            ds.Tables[0].Constraints.Add("test", ds.Tables[1].Columns[0], ds.Tables[0].Columns[0]);
            ds.Clear();
        }

        [Fact]
        public void Clone()
        {
            DataSet ds = new DataSet(), dsTarget = null;
            ds.Tables.Add(DataProvider.CreateParentDataTable());
            ds.Tables.Add(DataProvider.CreateChildDataTable());
            ds.Relations.Add(new DataRelation("myRelation", ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]));
            ds.Tables[0].Rows.Add(new object[] { 9, "", "" });
            ds.Tables[1].Columns[2].ReadOnly = true;
            ds.Tables[0].PrimaryKey = new DataColumn[] { ds.Tables[0].Columns[0], ds.Tables[0].Columns[1] };

            //copy schema only, no data

            // Clone 1
            dsTarget = ds.Clone();
            //Assert.Equal(ds.GetXmlSchema(), dsTarget.GetXmlSchema() );
            //use my function because GetXmlSchema not implemented in java
            Assert.Equal(DataProvider.GetDSSchema(ds), DataProvider.GetDSSchema(dsTarget));

            // Clone 2
            Assert.Equal(false, dsTarget.GetXml() == ds.GetXml());
        }

        [Fact]
        public void Copy()
        {
            DataSet ds = new DataSet(), dsTarget = null;
            ds.Tables.Add(DataProvider.CreateParentDataTable());
            ds.Tables.Add(DataProvider.CreateChildDataTable());
            ds.Relations.Add(new DataRelation("myRelation", ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]));
            ds.Tables[0].Rows.Add(new object[] { 9, "", "" });
            ds.Tables[1].Columns[2].ReadOnly = true;
            ds.Tables[0].PrimaryKey = new DataColumn[] { ds.Tables[0].Columns[0], ds.Tables[0].Columns[1] };

            //copy data and schema

            // Copy 1
            dsTarget = ds.Copy();
            //Assert.Equal(ds.GetXmlSchema(), dsTarget.GetXmlSchema() );
            //using my function because GetXmlSchema in not implemented in java
            Assert.Equal(DataProvider.GetDSSchema(ds), DataProvider.GetDSSchema(dsTarget));

            // Copy 2
            Assert.Equal(true, dsTarget.GetXml() == ds.GetXml());
        }

        [Fact]
        public void DataSetName()
        {
            var ds = new DataSet();

            // DataSetName - default value
            Assert.Equal("NewDataSet", ds.DataSetName);

            ds.DataSetName = "NewName";

            // DataSetName - get
            Assert.Equal("NewName", ds.DataSetName);
        }

        [Fact]
        public void EnforceConstraints()
        {
            var ds = new DataSet();

            // EnforceConstraints - default value (true)
            Assert.Equal(true, ds.EnforceConstraints);

            ds.EnforceConstraints = false;

            // EnforceConstraints - get
            Assert.Equal(false, ds.EnforceConstraints);
        }

        [Fact]
        public void EnforceConstraints_CheckPrimaryConstraint()
        {
            var ds = new DataSet();
            ds.Tables.Add("table");
            ds.Tables[0].Columns.Add("col");
            ds.Tables[0].PrimaryKey = new DataColumn[] { ds.Tables[0].Columns[0] };
            ds.EnforceConstraints = false;
            ds.Tables[0].Rows.Add(new object[] { null });
            try
            {
                ds.EnforceConstraints = true;
                Assert.False(true);
            }
            catch (ConstraintException e)
            {
                // Never premise English.
                //Assert.Equal ("Failed to enable constraints. One or more rows contain values " + 
                //		"violating non-null, unique, or foreign-key constraints.", e.Message, "#2");
            }
        }

        [Fact]
        public void EnforceConstraints_NonNullCols()
        {
            var ds = new DataSet();
            ds.Tables.Add("table");
            ds.Tables[0].Columns.Add("col");
            ds.Tables[0].Columns[0].AllowDBNull = false;

            ds.EnforceConstraints = false;
            ds.Tables[0].Rows.Add(new object[] { null });
            try
            {
                ds.EnforceConstraints = true;
                Assert.False(true);
            }
            catch (ConstraintException e)
            {
                // Never premise English.
                //Assert.Equal ("Failed to enable constraints. One or more rows contain values " + 
                //		"violating non-null, unique, or foreign-key constraints.", e.Message, "#2");
            }
        }

        [Fact]
        public void GetChanges()
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());

            // GetChanges 1
            Assert.Equal(null, ds.GetChanges());

            DataRow dr = ds.Tables[0].NewRow();
            dr[0] = 9;
            ds.Tables[0].Rows.Add(dr);

            // GetChanges 2
            Assert.Equal(true, ds.GetChanges() != null);

            // GetChanges 3
            Assert.Equal(dr.ItemArray, ds.GetChanges().Tables[0].Rows[0].ItemArray);
        }

        [Fact]
        public void GetChanges_ByDataRowState()
        {
            var ds = new DataSet();
            object[] arrAdded, arrDeleted, arrModified, arrUnchanged;
            //object[] arrDetached;

            DataRow dr;
            ds.Tables.Add(DataProvider.CreateParentDataTable());

            // GetChanges 1
            Assert.Equal(null, ds.GetChanges());

            //make some changes

            // can't check detached
            //		dr = ds.Tables[0].Rows[0];
            //		arrDetached = dr.ItemArray;
            //		dr.Delete();
            //		ds.Tables[0].AcceptChanges();

            dr = ds.Tables[0].Rows[1];
            arrDeleted = dr.ItemArray;
            dr.Delete();

            dr = ds.Tables[0].Rows[2];
            dr[1] = "NewValue";
            arrModified = dr.ItemArray;

            dr = ds.Tables[0].Select("", "", DataViewRowState.Unchanged)[0];
            arrUnchanged = dr.ItemArray;

            dr = ds.Tables[0].NewRow();
            dr[0] = 1;
            ds.Tables[0].Rows.Add(dr);
            arrAdded = dr.ItemArray;

            // GetChanges Added
            Assert.Equal(arrAdded, ds.GetChanges(DataRowState.Added).Tables[0].Rows[0].ItemArray);

            // GetChanges Deleted
            dr = ds.GetChanges(DataRowState.Deleted).Tables[0].Rows[0];
            object[] tmp = new object[] { dr[0, DataRowVersion.Original], dr[1, DataRowVersion.Original], dr[2, DataRowVersion.Original], dr[3, DataRowVersion.Original], dr[4, DataRowVersion.Original], dr[5, DataRowVersion.Original] };
            Assert.Equal(arrDeleted, tmp);

            //	can't check it	
            //		// GetChanges Detached
            //		dr = ds.GetChanges(DataRowState.Detached).Tables[0].Rows[0];
            //		object[] tmp = new object[] {dr[0,DataRowVersion.Original],dr[1,DataRowVersion.Original],dr[2,DataRowVersion.Original]};
            //		Assert.Equal(arrDetached, tmp);

            // GetChanges Modified
            Assert.Equal(arrModified, ds.GetChanges(DataRowState.Modified).Tables[0].Rows[0].ItemArray);

            // GetChanges Unchanged
            Assert.Equal(arrUnchanged, ds.GetChanges(DataRowState.Unchanged).Tables[0].Rows[0].ItemArray);
        }

        [Fact]
        public void BeginInitTest()
        {
            DataSet ds = new DataSet();

            DataTable table1 = new DataTable("table1");
            DataTable table2 = new DataTable("table2");

            DataColumn col1 = new DataColumn("col1", typeof(int));
            DataColumn col2 = new DataColumn("col2", typeof(int));
            table1.Columns.Add(col1);
            table2.Columns.Add(col2);

            UniqueConstraint pkey = new UniqueConstraint("pk", new string[] { "col1" }, true);
            ForeignKeyConstraint fkey = new ForeignKeyConstraint("fk", "table1", new string[] { "col1" },
                                new string[] { "col2" }, AcceptRejectRule.Cascade,
                                Rule.Cascade, Rule.Cascade);
            DataRelation relation = new DataRelation("rel", "table1", "table2", new string[] { "col1" },
                                 new string[] { "col2" }, false);
            ds.BeginInit();
            table1.BeginInit();
            table2.BeginInit();

            ds.Tables.AddRange(new DataTable[] { table1, table2 });
            ds.Relations.AddRange(new DataRelation[] { relation });

            table1.Constraints.AddRange(new Constraint[] { pkey });
            table2.Constraints.AddRange(new Constraint[] { fkey });

            // The tables/relations shud not get added to the DataSet yet
            Assert.Equal(0, ds.Tables.Count);
            Assert.Equal(0, ds.Relations.Count);
            Assert.Equal(0, table1.Constraints.Count);
            Assert.Equal(0, table2.Constraints.Count);
            ds.EndInit();

            Assert.Equal(2, ds.Tables.Count);
            Assert.Equal(1, ds.Relations.Count);
            Assert.Equal(1, ds.Tables[0].Constraints.Count);
            Assert.Equal(1, ds.Tables[1].Constraints.Count);

            // Table shud still be in BeginInit .. 
            DataColumn col3 = new DataColumn("col2");
            UniqueConstraint uc = new UniqueConstraint("uc", new string[] { "col2" }, false);

            table1.Columns.AddRange(new DataColumn[] { col3 });
            table1.Constraints.AddRange(new Constraint[] { uc });

            Assert.Equal(1, table1.Columns.Count);
            Assert.Equal(1, table1.Constraints.Count);

            table1.EndInit();
            Assert.Equal(2, table1.Columns.Count);
            Assert.Equal(2, table1.Columns.Count);
        }

        [Fact]
        public void GetXml()
        {
            var ds = new DataSet();
            ds.Namespace = "namespace"; //if we don't add namespace the test will fail because GH (by design) always add namespace
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Clear();
            dt.Rows.Add(new object[] { 1, "Value1", "Value2" });
            dt.Rows.Add(new object[] { 2, "Value3", "Value4" });
            dt.Rows.Add(new object[] { 3, "Value5", "Value5" });

            StringBuilder resultXML = new StringBuilder();

            resultXML.Append("<" + ds.DataSetName + "xmlns=\"namespace\">");

            resultXML.Append("<Parent>");
            resultXML.Append("<ParentId>1</ParentId>");
            resultXML.Append("<String1>Value1</String1>");
            resultXML.Append("<String2>Value2</String2>");
            resultXML.Append("</Parent>");

            resultXML.Append("<Parent>");
            resultXML.Append("<ParentId>2</ParentId>");
            resultXML.Append("<String1>Value3</String1>");
            resultXML.Append("<String2>Value4</String2>");
            resultXML.Append("</Parent>");

            resultXML.Append("<Parent>");
            resultXML.Append("<ParentId>3</ParentId>");
            resultXML.Append("<String1>Value5</String1>");
            resultXML.Append("<String2>Value5</String2>");
            resultXML.Append("</Parent>");

            resultXML.Append("</" + ds.DataSetName + ">");

            ds.Tables.Add(dt);
            string strXML = ds.GetXml();
            strXML = strXML.Replace(" ", "");
            strXML = strXML.Replace("\t", "");
            strXML = strXML.Replace("\n", "");
            strXML = strXML.Replace("\r", "");

            // GetXml
            Assert.Equal(resultXML.ToString(), strXML);
        }

        [Fact]
        public void HasChanges()
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());

            // HasChanges 1
            Assert.Equal(false, ds.HasChanges());

            DataRow dr = ds.Tables[0].NewRow();
            dr[0] = 9;
            ds.Tables[0].Rows.Add(dr);

            // HasChanges 2
            Assert.Equal(true, ds.HasChanges());
        }

        [Fact]
        public void HasChanges_ByDataRowState()
        {
            var ds = new DataSet();

            DataRow dr;
            ds.Tables.Add(DataProvider.CreateParentDataTable());

            // HasChanges 1
            Assert.Equal(false, ds.HasChanges());

            //make some changes

            dr = ds.Tables[0].Rows[1];
            dr.Delete();

            dr = ds.Tables[0].Rows[2];
            dr[1] = "NewValue";

            dr = ds.Tables[0].Select("", "", DataViewRowState.Unchanged)[0];

            dr = ds.Tables[0].NewRow();
            dr[0] = 1;
            ds.Tables[0].Rows.Add(dr);

            // HasChanges Added
            Assert.Equal(true, ds.HasChanges(DataRowState.Added));

            // HasChanges Deleted
            Assert.Equal(true, ds.HasChanges(DataRowState.Deleted));

            // HasChanges Modified
            Assert.Equal(true, ds.HasChanges(DataRowState.Modified));

            // HasChanges Unchanged
            Assert.Equal(true, ds.HasChanges(DataRowState.Unchanged));
        }

        [Fact]
        public void HasErrors()
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());

            // HasErrors - default
            Assert.Equal(false, ds.HasErrors);

            ds.Tables[0].Rows[0].RowError = "ErrDesc";

            // HasErrors
            Assert.Equal(true, ds.HasErrors);
        }

        #region test namespaces

        [Fact]
        public void InferXmlSchema_BasicXml()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<NewDataSet xmlns:od='urn:schemas-microsoft-com:officedata'>");
            sb.Append("<Categories>");
            sb.Append("<CategoryID od:adotype='3'>1</CategoryID>");
            sb.Append("<CategoryName od:maxLength='15' od:adotype='130'>Beverages</CategoryName>");
            sb.Append("<Description od:adotype='203'>Soft drinks and teas</Description>");
            sb.Append("</Categories>");
            sb.Append("<Products>");
            sb.Append("<ProductID od:adotype='20'>1</ProductID>");
            sb.Append("<ReorderLevel od:adotype='3'>10</ReorderLevel>");
            sb.Append("<Discontinued od:adotype='11'>0</Discontinued>");
            sb.Append("</Products>");
            sb.Append("</NewDataSet>");

            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));

            var ds = new DataSet();
            //	ds.ReadXml(myStream);
            ds.InferXmlSchema(myStream, new string[] { "urn:schemas-microsoft-com:officedata" });
            Assert.Equal(2, ds.Tables.Count);
            Assert.Equal("CategoryID", ds.Tables[0].Columns[0].ColumnName);
            Assert.Equal("CategoryName", ds.Tables[0].Columns[1].ColumnName);
            Assert.Equal("Description", ds.Tables[0].Columns[2].ColumnName);

            Assert.Equal("ProductID", ds.Tables[1].Columns[0].ColumnName);
            Assert.Equal("ReorderLevel", ds.Tables[1].Columns[1].ColumnName);
            Assert.Equal("Discontinued", ds.Tables[1].Columns[2].ColumnName);
        }

        [Fact]
        public void InferXmlSchema_WithoutIgnoreNameSpaces()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<NewDataSet xmlns:od='urn:schemas-microsoft-com:officedata'>");
            sb.Append("<Categories>");
            sb.Append("<CategoryID od:adotype='3'>1</CategoryID>");
            sb.Append("<CategoryName od:maxLength='15' od:adotype='130'>Beverages</CategoryName>");
            sb.Append("<Description od:adotype='203'>Soft drinks and teas</Description>");
            sb.Append("</Categories>");
            sb.Append("<Products>");
            sb.Append("<ProductID od:adotype='20'>1</ProductID>");
            sb.Append("<ReorderLevel od:adotype='3'>10</ReorderLevel>");
            sb.Append("<Discontinued od:adotype='11'>0</Discontinued>");
            sb.Append("</Products>");
            sb.Append("</NewDataSet>");

            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));

            var ds = new DataSet();
            //ds.ReadXml(myStream);
            ds.InferXmlSchema(myStream, new string[] { "urn:schemas-microsoft-com:officedata1" });
            Assert.Equal(8, ds.Tables.Count);
        }

        [Fact]
        public void InferXmlSchema_IgnoreNameSpace()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<NewDataSet xmlns:od='urn:schemas-microsoft-com:officedata'>");
            sb.Append("<Categories>");
            sb.Append("<CategoryID od:adotype='3'>1</CategoryID>");
            sb.Append("<CategoryName od:maxLength='15' adotype='130'>Beverages</CategoryName>");
            sb.Append("<Description od:adotype='203'>Soft drinks and teas</Description>");
            sb.Append("</Categories>");
            sb.Append("<Products>");
            sb.Append("<ProductID od:adotype='20'>1</ProductID>");
            sb.Append("<ReorderLevel od:adotype='3'>10</ReorderLevel>");
            sb.Append("<Discontinued od:adotype='11'>0</Discontinued>");
            sb.Append("</Products>");
            sb.Append("</NewDataSet>");

            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));

            var ds = new DataSet();
            //	ds.ReadXml(myStream);
            ds.InferXmlSchema(myStream, new string[] { "urn:schemas-microsoft-com:officedata" });
            Assert.Equal(3, ds.Tables.Count);

            Assert.Equal(3, ds.Tables[0].Columns.Count);
            Assert.Equal("CategoryID", ds.Tables[0].Columns["CategoryID"].ColumnName);
            Assert.Equal("Categories_Id", ds.Tables[0].Columns["Categories_Id"].ColumnName);
            Assert.Equal("Description", ds.Tables[0].Columns["Description"].ColumnName);

            Assert.Equal(3, ds.Tables[1].Columns.Count);
            Assert.Equal("adotype", ds.Tables[1].Columns["adotype"].ColumnName);
            Assert.Equal("CategoryName_Text", ds.Tables[1].Columns["CategoryName_Text"].ColumnName);
            Assert.Equal("Categories_Id", ds.Tables[1].Columns["Categories_Id"].ColumnName);

            Assert.Equal(3, ds.Tables[2].Columns.Count);
            Assert.Equal("ProductID", ds.Tables[2].Columns["ProductID"].ColumnName);
            Assert.Equal("ReorderLevel", ds.Tables[2].Columns["ReorderLevel"].ColumnName);
            Assert.Equal("Discontinued", ds.Tables[2].Columns["Discontinued"].ColumnName);
        }

        [Fact]
        public void InferXmlSchema_IgnoreNameSpaces() //Ignoring 2 namespaces
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<h:html xmlns:xdc='http://www.xml.com/books' xmlns:h='http://www.w3.org/HTML/1998/html4'>");
            sb.Append("<h:head><h:title>Book Review</h:title></h:head>");
            sb.Append("<h:body>");
            sb.Append("<xdc:bookreview>");
            sb.Append("<xdc:title h:attrib1='1' xdc:attrib2='2' >XML: A Primer</xdc:title>");
            sb.Append("</xdc:bookreview>");
            sb.Append("</h:body>");
            sb.Append("</h:html>");

            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            DataSet tempDs = new DataSet();
            tempDs.ReadXml(myStream);
            myStream.Seek(0, SeekOrigin.Begin);
            var ds = new DataSet();
            ds.InferXmlSchema(myStream, new string[] { "http://www.xml.com/books", "http://www.w3.org/HTML/1998/html4" });
            //Assert.Equal(8, ds.Tables.Count);

            //			string str1 = tempDs.GetXmlSchema(); //DataProvider.GetDSSchema(tempDs);
            //			string str2 = ds.GetXmlSchema(); //DataProvider.GetDSSchema(ds);
            Assert.Equal(3, ds.Tables.Count);
            Assert.Equal("bookreview", ds.Tables[2].TableName);
            Assert.Equal(2, ds.Tables[2].Columns.Count);
        }
        #endregion

        #region inferringTables
        [Fact]
        public void InferXmlSchema_inferringTables1()
        {
            //According to the msdn documantaion :
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringtables.htm
            //Elements that have attributes specified in them will result in inferred tables

            // inferringTables1
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1 attr1='value1'/>");
            sb.Append("<Element1 attr1='value2'>Text1</Element1>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);
            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("attr1", ds.Tables[0].Columns["attr1"].ColumnName);
            Assert.Equal("Element1_Text", ds.Tables[0].Columns["Element1_Text"].ColumnName);
        }

        [Fact]
        public void InferXmlSchema_inferringTables2()
        {
            //According to the msdn documantaion :
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringtables.htm
            //Elements that have child elements will result in inferred tables

            // inferringTables2
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1>");
            sb.Append("<ChildElement1>Text1</ChildElement1>");
            sb.Append("</Element1>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);
            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("ChildElement1", ds.Tables[0].Columns["ChildElement1"].ColumnName);
        }

        [Fact]
        public void InferXmlSchema_inferringTables3()
        {
            //According to the msdn documantaion :
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringtables.htm
            //The document, or root, element will result in an inferred table if it has attributes
            //or child elements that will be inferred as columns.
            //If the document element has no attributes and no child elements that would be inferred as columns, the element will be inferred as a DataSet

            // inferringTables3
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1>Text1</Element1>");
            sb.Append("<Element2>Text2</Element2>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);
            Assert.Equal("NewDataSet", ds.DataSetName);
            Assert.Equal("DocumentElement", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("Element1", ds.Tables[0].Columns["Element1"].ColumnName);
            Assert.Equal("Element2", ds.Tables[0].Columns["Element2"].ColumnName);
        }

        [Fact]
        public void InferXmlSchema_inferringTables4()
        {
            //According to the msdn documantaion :
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringtables.htm
            //The document, or root, element will result in an inferred table if it has attributes
            //or child elements that will be inferred as columns.
            //If the document element has no attributes and no child elements that would be inferred as columns, the element will be inferred as a DataSet

            // inferringTables4
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1 attr1='value1' attr2='value2'/>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);
            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("attr1", ds.Tables[0].Columns["attr1"].ColumnName);
            Assert.Equal("attr2", ds.Tables[0].Columns["attr2"].ColumnName);
        }

        [Fact]
        public void InferXmlSchema_inferringTables5()
        {
            //According to the msdn documantaion :
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringtables.htm
            //Elements that repeat will result in a single inferred table

            // inferringTables5
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1>Text1</Element1>");
            sb.Append("<Element1>Text2</Element1>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);
            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("Element1_Text", ds.Tables[0].Columns["Element1_Text"].ColumnName);
        }
        #endregion

        #region inferringColumns
        [Fact]
        public void InferXmlSchema_inferringColumns1()
        {
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringcolumns.htm
            // inferringColumns1
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1 attr1='value1' attr2='value2'/>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);
            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("attr1", ds.Tables[0].Columns["attr1"].ColumnName);
            Assert.Equal("attr2", ds.Tables[0].Columns["attr2"].ColumnName);
            Assert.Equal(MappingType.Attribute, ds.Tables[0].Columns["attr1"].ColumnMapping);
            Assert.Equal(MappingType.Attribute, ds.Tables[0].Columns["attr2"].ColumnMapping);
            Assert.Equal(typeof(string), ds.Tables[0].Columns["attr1"].DataType);
            Assert.Equal(typeof(string), ds.Tables[0].Columns["attr2"].DataType);
        }

        [Fact]
        public void InferXmlSchema_inferringColumns2()
        {
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringcolumns.htm
            //If an element has no child elements or attributes, it will be inferred as a column.
            //The ColumnMapping property of the column will be set to MappingType.Element.
            //The text for child elements is stored in a row in the table

            // inferringColumns2
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1>");
            sb.Append("<ChildElement1>Text1</ChildElement1>");
            sb.Append("<ChildElement2>Text2</ChildElement2>");
            sb.Append("</Element1>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);
            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal("ChildElement1", ds.Tables[0].Columns["ChildElement1"].ColumnName);
            Assert.Equal("ChildElement2", ds.Tables[0].Columns["ChildElement2"].ColumnName);
            Assert.Equal(MappingType.Element, ds.Tables[0].Columns["ChildElement1"].ColumnMapping);
            Assert.Equal(MappingType.Element, ds.Tables[0].Columns["ChildElement2"].ColumnMapping);
            Assert.Equal(typeof(string), ds.Tables[0].Columns["ChildElement1"].DataType);
            Assert.Equal(typeof(string), ds.Tables[0].Columns["ChildElement2"].DataType);
        }

        #endregion

        #region Inferring Relationships

        [Fact]
        public void InferXmlSchema_inferringRelationships1()
        {
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringrelationships.htm

            // inferringRelationships1
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1>");
            sb.Append("<ChildElement1 attr1='value1' attr2='value2'/>");
            sb.Append("<ChildElement2>Text2</ChildElement2>");
            sb.Append("</Element1>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);
            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal("ChildElement1", ds.Tables[1].TableName);
            Assert.Equal(2, ds.Tables.Count);

            Assert.Equal("Element1_Id", ds.Tables["Element1"].Columns["Element1_Id"].ColumnName);
            Assert.Equal(MappingType.Hidden, ds.Tables["Element1"].Columns["Element1_Id"].ColumnMapping);
            Assert.Equal(typeof(int), ds.Tables["Element1"].Columns["Element1_Id"].DataType);

            Assert.Equal("ChildElement2", ds.Tables["Element1"].Columns["ChildElement2"].ColumnName);
            Assert.Equal(MappingType.Element, ds.Tables["Element1"].Columns["ChildElement2"].ColumnMapping);
            Assert.Equal(typeof(string), ds.Tables["Element1"].Columns["ChildElement2"].DataType);

            Assert.Equal("attr1", ds.Tables["ChildElement1"].Columns["attr1"].ColumnName);
            Assert.Equal(MappingType.Attribute, ds.Tables["ChildElement1"].Columns["attr1"].ColumnMapping);
            Assert.Equal(typeof(string), ds.Tables["ChildElement1"].Columns["attr1"].DataType);

            Assert.Equal("attr2", ds.Tables["ChildElement1"].Columns["attr2"].ColumnName);
            Assert.Equal(MappingType.Attribute, ds.Tables["ChildElement1"].Columns["attr2"].ColumnMapping);
            Assert.Equal(typeof(string), ds.Tables["ChildElement1"].Columns["attr2"].DataType);

            Assert.Equal("Element1_Id", ds.Tables["ChildElement1"].Columns["Element1_Id"].ColumnName);
            Assert.Equal(MappingType.Hidden, ds.Tables["ChildElement1"].Columns["Element1_Id"].ColumnMapping);
            Assert.Equal(typeof(int), ds.Tables["ChildElement1"].Columns["Element1_Id"].DataType);

            //Checking dataRelation :
            Assert.Equal("Element1", ds.Relations["Element1_ChildElement1"].ParentTable.TableName);
            Assert.Equal("Element1_Id", ds.Relations["Element1_ChildElement1"].ParentColumns[0].ColumnName);
            Assert.Equal("ChildElement1", ds.Relations["Element1_ChildElement1"].ChildTable.TableName);
            Assert.Equal("Element1_Id", ds.Relations["Element1_ChildElement1"].ChildColumns[0].ColumnName);
            Assert.Equal(true, ds.Relations["Element1_ChildElement1"].Nested);

            //Checking ForeignKeyConstraint

            ForeignKeyConstraint con = (ForeignKeyConstraint)ds.Tables["ChildElement1"].Constraints["Element1_ChildElement1"];

            Assert.Equal("Element1_Id", con.Columns[0].ColumnName);
            Assert.Equal(Rule.Cascade, con.DeleteRule);
            Assert.Equal(AcceptRejectRule.None, con.AcceptRejectRule);
            Assert.Equal("Element1", con.RelatedTable.TableName);
            Assert.Equal("ChildElement1", con.Table.TableName);
        }

        #endregion

        #region Inferring Element Text

        [Fact]
        public void InferXmlSchema_elementText1()
        {
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringelementtext.htm

            // elementText1
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1 attr1='value1'>Text1</Element1>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);

            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);

            Assert.Equal("attr1", ds.Tables["Element1"].Columns["attr1"].ColumnName);
            Assert.Equal(MappingType.Attribute, ds.Tables["Element1"].Columns["attr1"].ColumnMapping);
            Assert.Equal(typeof(string), ds.Tables["Element1"].Columns["attr1"].DataType);

            Assert.Equal("Element1_Text", ds.Tables["Element1"].Columns["Element1_Text"].ColumnName);
            Assert.Equal(MappingType.SimpleContent, ds.Tables["Element1"].Columns["Element1_Text"].ColumnMapping);
            Assert.Equal(typeof(string), ds.Tables["Element1"].Columns["Element1_Text"].DataType);
        }

        [Fact]
        public void InferXmlSchema_elementText2()
        {
            //ms-help://MS.MSDNQTR.2003FEB.1033/cpguide/html/cpconinferringelementtext.htm

            // elementText1
            StringBuilder sb = new StringBuilder();

            sb.Append("<DocumentElement>");
            sb.Append("<Element1>");
            sb.Append("Text1");
            sb.Append("<ChildElement1>Text2</ChildElement1>");
            sb.Append("Text3");
            sb.Append("</Element1>");
            sb.Append("</DocumentElement>");
            var ds = new DataSet();
            MemoryStream myStream = new MemoryStream(new ASCIIEncoding().GetBytes(sb.ToString()));
            ds.InferXmlSchema(myStream, null);

            Assert.Equal("DocumentElement", ds.DataSetName);
            Assert.Equal("Element1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);

            Assert.Equal("ChildElement1", ds.Tables["Element1"].Columns["ChildElement1"].ColumnName);
            Assert.Equal(MappingType.Element, ds.Tables["Element1"].Columns["ChildElement1"].ColumnMapping);
            Assert.Equal(typeof(string), ds.Tables["Element1"].Columns["ChildElement1"].DataType);
            Assert.Equal(1, ds.Tables["Element1"].Columns.Count);
        }

        #endregion

        [Fact]
        public void Locale()
        {
            DataSet ds = new DataSet("MyDataSet");
            CultureInfo culInfo = CultureInfo.CurrentCulture;

            // Checking Locale default from system
            Assert.Equal(culInfo, ds.Locale);

            // Checking Locale get/set
            culInfo = new CultureInfo("fr"); // = french
            ds.Locale = culInfo;
            Assert.Equal(culInfo, ds.Locale);
        }

        [Fact]
        public void DataSetSpecificCulture()
        {
            RemoteExecutor.Invoke(() =>
            {
                CultureInfo.CurrentCulture = new CultureInfo("cs-CZ");

                var ds = new DataSet();
                ds.Locale = CultureInfo.GetCultureInfo(1033);
                var dt = ds.Tables.Add("machine");
                dt.Locale = ds.Locale;
                Assert.Same(dt, ds.Tables["MACHINE"]);

                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }

        [Fact]
        public void MergeFailed()
        {
            _eventRaised = false;
            DataSet ds1, ds2;
            ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            //add primary key to the FIRST column
            ds1.Tables[0].PrimaryKey = new DataColumn[] { ds1.Tables[0].Columns[0] };

            //create target dataset which is a copy of the source
            ds2 = ds1.Copy();
            //clear the data
            ds2.Clear();
            //add primary key to the SECOND columnn
            ds2.Tables[0].PrimaryKey = new DataColumn[] { ds2.Tables[0].Columns[1] };
            //add a new row that already exists in the source dataset
            //ds2.Tables[0].Rows.Add(ds1.Tables[0].Rows[0].ItemArray);

            //enforce constraints
            ds2.EnforceConstraints = true;
            ds1.EnforceConstraints = true;

            // Add MergeFailed event handler for the table.
            ds2.MergeFailed += new MergeFailedEventHandler(Merge_Failed);

            ds2.Merge(ds1); //will raise MergeFailed event

            // MergeFailed event
            Assert.Equal(true, _eventRaised);
        }
        private void Merge_Failed(object sender, MergeFailedEventArgs e)
        {
            _eventRaised = true;
        }

        [Fact]
        public void Merge_ByDataRowsNoPreserveIgnoreMissingSchema()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.TableName = "Table1";
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };

            //create target dataset (copy of source dataset)
            DataSet dsTarget = new DataSet();
            dsTarget.Tables.Add(dt.Copy());

            DataRow[] drArr = new DataRow[3];
            //Update row
            string OldValue = dt.Select("ParentId=1")[0][1].ToString();
            drArr[0] = dt.Select("ParentId=1")[0];
            drArr[0][1] = "NewValue";
            //delete rows
            drArr[1] = dt.Select("ParentId=2")[0];
            drArr[1].Delete();
            //add row
            drArr[2] = dt.NewRow();
            drArr[2].ItemArray = new object[] { 99, "NewRowValue1", "NewRowValue2" };
            dt.Rows.Add(drArr[2]);

            dsTarget.Merge(drArr, false, MissingSchemaAction.Ignore);

            // Merge update row
            Assert.Equal("NewValue", dsTarget.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge added row
            Assert.Equal(1, dsTarget.Tables["Table1"].Select("ParentId=99").Length);

            // Merge deleted row
            Assert.Equal(0, dsTarget.Tables["Table1"].Select("ParentId=2").Length);
        }

        [Fact]
        public void Merge_ByDataRowsPreserveMissingSchema()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.TableName = "Table1";
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };

            //create target dataset (copy of source dataset)
            DataSet dsTarget = new DataSet();
            dsTarget.Tables.Add(dt.Copy());

            //add new column (for checking MissingSchemaAction)
            DataColumn dc = new DataColumn("NewColumn", typeof(float));
            dt.Columns.Add(dc);

            DataRow[] drArr = new DataRow[3];

            //Update row
            string OldValue = dt.Select("ParentId=1")[0][1].ToString();
            drArr[0] = dt.Select("ParentId=1")[0];
            drArr[0][1] = "NewValue";
            //delete rows
            drArr[1] = dt.Select("ParentId=2")[0];
            drArr[1].Delete();
            //add row
            drArr[2] = dt.NewRow();
            drArr[2].ItemArray = new object[] { 99, "NewRowValue1", "NewRowValue2", null };
            dt.Rows.Add(drArr[2]);

            DataSet dsTarget1 = null;

            #region "Merge(drArr,true,MissingSchemaAction.Ignore )"
            dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(drArr, true, MissingSchemaAction.Ignore);
            // Merge true,Ignore - Column
            Assert.Equal(false, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge true,Ignore - changed values
            Assert.Equal(OldValue, dsTarget1.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge true,Ignore - added values
            Assert.Equal(1, dsTarget1.Tables["Table1"].Select("ParentId=99").Length);

            // Merge true,Ignore - deleted row
            Assert.Equal(true, dsTarget1.Tables["Table1"].Select("ParentId=2").Length > 0);
            #endregion

            #region "Merge(drArr,false,MissingSchemaAction.Ignore )"
            dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(drArr, false, MissingSchemaAction.Ignore);
            // Merge true,Ignore - Column
            Assert.Equal(false, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge true,Ignore - changed values
            Assert.Equal("NewValue", dsTarget1.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge true,Ignore - added values
            Assert.Equal(1, dsTarget1.Tables["Table1"].Select("ParentId=99").Length);

            // Merge true,Ignore - deleted row
            Assert.Equal(0, dsTarget1.Tables["Table1"].Select("ParentId=2").Length);
            #endregion

            #region "Merge(drArr,true,MissingSchemaAction.Add )"
            dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(drArr, true, MissingSchemaAction.Add);
            // Merge true,Ignore - Column
            Assert.Equal(true, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge true,Ignore - changed values
            Assert.Equal(OldValue, dsTarget1.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge true,Ignore - added values
            Assert.Equal(1, dsTarget1.Tables["Table1"].Select("ParentId=99").Length);

            // Merge true,Ignore - deleted row
            Assert.Equal(true, dsTarget1.Tables["Table1"].Select("ParentId=2").Length > 0);
            #endregion

            #region "Merge(drArr,false,MissingSchemaAction.Add )"
            dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(drArr, false, MissingSchemaAction.Add);
            // Merge true,Ignore - Column
            Assert.Equal(true, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge true,Ignore - changed values
            Assert.Equal("NewValue", dsTarget1.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge true,Ignore - added values
            Assert.Equal(1, dsTarget1.Tables["Table1"].Select("ParentId=99").Length);

            // Merge true,Ignore - deleted row
            Assert.Equal(0, dsTarget1.Tables["Table1"].Select("ParentId=2").Length);
            #endregion
        }

        [Fact]
        public void Merge_ByDataSet()
        {
            //create source dataset
            var ds = new DataSet();
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.TableName = "Table1";
            ds.Tables.Add(dt.Copy());
            dt.TableName = "Table2";
            //add primary key
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };
            ds.Tables.Add(dt.Copy());

            //create target dataset (copy of source dataset)
            DataSet dsTarget = ds.Copy();
            int iTable1RowsCount = dsTarget.Tables["Table1"].Rows.Count;

            //Source - add another table, don't exists on the target dataset
            ds.Tables.Add(new DataTable("SomeTable"));
            ds.Tables["SomeTable"].Columns.Add("Id");
            ds.Tables["SomeTable"].Rows.Add(new object[] { 777 });

            //Target - add another table, don't exists on the source dataset
            dsTarget.Tables.Add(new DataTable("SmallTable"));
            dsTarget.Tables["SmallTable"].Columns.Add("Id");
            dsTarget.Tables["SmallTable"].Rows.Add(new object[] { 777 });

            //update existing row
            ds.Tables["Table2"].Select("ParentId=1")[0][1] = "OldValue1";
            //add new row
            object[] arrAddedRow = new object[] { 99, "NewValue1", "NewValue2", new DateTime(0), 0.5, true };
            ds.Tables["Table2"].Rows.Add(arrAddedRow);
            //delete existing rows
            foreach (DataRow dr in ds.Tables["Table2"].Select("ParentId=2"))
            {
                dr.Delete();
            }

            // Merge - changed values
            dsTarget.Merge(ds);
            Assert.Equal("OldValue1", dsTarget.Tables["Table2"].Select("ParentId=1")[0][1]);

            // Merge - added values
            Assert.Equal(arrAddedRow, dsTarget.Tables["Table2"].Select("ParentId=99")[0].ItemArray);

            // Merge - deleted row
            Assert.Equal(0, dsTarget.Tables["Table2"].Select("ParentId=2").Length);

            //Table1 rows count should be double (no primary key)
            // Merge - Unchanged table 1
            Assert.Equal(iTable1RowsCount * 2, dsTarget.Tables["Table1"].Rows.Count);

            //SmallTable rows count should be the same
            // Merge - Unchanged table 2
            Assert.Equal(1, dsTarget.Tables["SmallTable"].Rows.Count);

            //SomeTable - new table
            // Merge - new table
            Assert.Equal(true, dsTarget.Tables["SomeTable"] != null);
        }

        [Fact]
        public void Merge_ByDataSetPreserve()
        {
            //create source dataset
            var ds = new DataSet();
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.TableName = "Table1";
            ds.Tables.Add(dt.Copy());
            dt.TableName = "Table2";
            //add primary key
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };
            ds.Tables.Add(dt.Copy());

            //create target dataset (copy of source dataset)
            DataSet dsTarget1 = ds.Copy();
            DataSet dsTarget2 = ds.Copy();
            int iTable1RowsCount = dsTarget1.Tables["Table1"].Rows.Count;

            //update existing row
            string oldValue = ds.Tables["Table2"].Select("ParentId=1")[0][1].ToString();
            ds.Tables["Table2"].Select("ParentId=1")[0][1] = "NewValue";
            //add new row
            object[] arrAddedRow = new object[] { 99, "NewValue1", "NewValue2", new DateTime(0), 0.5, true };
            ds.Tables["Table2"].Rows.Add(arrAddedRow);
            //delete existing rows
            int iDeleteLength = dsTarget1.Tables["Table2"].Select("ParentId=2").Length;
            foreach (DataRow dr in ds.Tables["Table2"].Select("ParentId=2"))
            {
                dr.Delete();
            }

            #region "Merge(ds,true)"
            //only new added rows are merged (preserveChanges = true)
            dsTarget1.Merge(ds, true);
            // Merge - changed values
            Assert.Equal(oldValue, dsTarget1.Tables["Table2"].Select("ParentId=1")[0][1]);

            // Merge - added values
            Assert.Equal(arrAddedRow, dsTarget1.Tables["Table2"].Select("ParentId=99")[0].ItemArray);

            // Merge - deleted row
            Assert.Equal(iDeleteLength, dsTarget1.Tables["Table2"].Select("ParentId=2").Length);
            #endregion

            #region "Merge(ds,false)"
            //all changes are merged (preserveChanges = false)
            dsTarget2.Merge(ds, false);
            // Merge - changed values
            Assert.Equal("NewValue", dsTarget2.Tables["Table2"].Select("ParentId=1")[0][1]);

            // Merge - added values
            Assert.Equal(arrAddedRow, dsTarget2.Tables["Table2"].Select("ParentId=99")[0].ItemArray);

            // Merge - deleted row
            Assert.Equal(0, dsTarget2.Tables["Table2"].Select("ParentId=2").Length);
            #endregion
        }

        [Fact]
        public void Merge_ByDataSetPreserveMissingSchemaAction()
        {
            //create source dataset
            var ds = new DataSet();

            DataTable dt = DataProvider.CreateParentDataTable();
            dt.TableName = "Table1";

            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };

            //add table to dataset
            ds.Tables.Add(dt.Copy());

            dt = ds.Tables[0];

            //create target dataset (copy of source dataset)
            DataSet dsTarget = ds.Copy();

            //add new column (for checking MissingSchemaAction)
            DataColumn dc = new DataColumn("NewColumn", typeof(float));
            //make the column to be primary key
            dt.Columns.Add(dc);

            //add new table (for checking MissingSchemaAction)
            ds.Tables.Add(new DataTable("NewTable"));
            ds.Tables["NewTable"].Columns.Add("NewColumn1", typeof(int));
            ds.Tables["NewTable"].Columns.Add("NewColumn2", typeof(long));
            ds.Tables["NewTable"].Rows.Add(new object[] { 1, 2 });
            ds.Tables["NewTable"].Rows.Add(new object[] { 3, 4 });
            ds.Tables["NewTable"].PrimaryKey = new DataColumn[] { ds.Tables["NewTable"].Columns["NewColumn1"] };

            #region "ds,false,MissingSchemaAction.Add)"
            DataSet dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(ds, false, MissingSchemaAction.Add);
            // Merge MissingSchemaAction.Add - Column
            Assert.Equal(true, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge MissingSchemaAction.Add - Table
            Assert.Equal(true, dsTarget1.Tables.Contains("NewTable"));

            //failed, should be success by MSDN Library documentation
            //		// Merge MissingSchemaAction.Add - PrimaryKey
            //		Assert.Equal(0, dsTarget1.Tables["NewTable"].PrimaryKey.Length);
            #endregion

            #region "ds,false,MissingSchemaAction.AddWithKey)"
            //MissingSchemaAction.Add,MissingSchemaAction.AddWithKey - behave the same, checked only Add

            //		DataSet dsTarget2 = dsTarget.Copy();
            //		dsTarget2.Merge(ds,false,MissingSchemaAction.AddWithKey);
            //		// Merge MissingSchemaAction.AddWithKey - Column
            //		Assert.Equal(true, dsTarget2.Tables["Table1"].Columns.Contains("NewColumn"));
            //
            //		// Merge MissingSchemaAction.AddWithKey - Table
            //		Assert.Equal(true, dsTarget2.Tables.Contains("NewTable"));
            //
            //		// Merge MissingSchemaAction.AddWithKey - PrimaryKey
            //		Assert.Equal(dsTarget2.Tables["NewTable"].Columns["NewColumn1"], dsTarget2.Tables["NewTable"].PrimaryKey[0]);
            #endregion

            #region "ds,false,MissingSchemaAction.Ignore )"
            DataSet dsTarget4 = dsTarget.Copy();
            dsTarget4.Merge(ds, false, MissingSchemaAction.Ignore);
            // Merge MissingSchemaAction.Ignore - Column
            Assert.Equal(false, dsTarget4.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge MissingSchemaAction.Ignore - Table
            Assert.Equal(false, dsTarget4.Tables.Contains("NewTable"));
            #endregion
        }

        [Fact]
        public void Merge_ByComplexDataSet()
        {
            //create source dataset
            var ds = new DataSet();

            ds.Tables.Add(DataProvider.CreateParentDataTable());
            ds.Tables.Add(DataProvider.CreateChildDataTable());
            ds.Tables["Child"].TableName = "Child2";
            ds.Tables.Add(DataProvider.CreateChildDataTable());

            //craete a target dataset to the merge operation
            DataSet dsTarget = ds.Copy();

            //craete a second target dataset to the merge operation
            DataSet dsTarget1 = ds.Copy();

            //------------------ make some changes in the second target dataset schema --------------------
            //add primary key
            dsTarget1.Tables["Parent"].PrimaryKey = new DataColumn[] { dsTarget1.Tables["Parent"].Columns["ParentId"] };
            dsTarget1.Tables["Child"].PrimaryKey = new DataColumn[] { dsTarget1.Tables["Child"].Columns["ParentId"], dsTarget1.Tables["Child"].Columns["ChildId"] };

            //add Foreign Key (different name)
            dsTarget1.Tables["Child2"].Constraints.Add("Child2_FK_2", dsTarget1.Tables["Parent"].Columns["ParentId"], dsTarget1.Tables["Child2"].Columns["ParentId"]);

            //add relation (different name)
            //dsTarget1.Relations.Add("Parent_Child_1",dsTarget1.Tables["Parent"].Columns["ParentId"],dsTarget1.Tables["Child"].Columns["ParentId"]);

            //------------------ make some changes in the source dataset schema --------------------
            //add primary key
            ds.Tables["Parent"].PrimaryKey = new DataColumn[] { ds.Tables["Parent"].Columns["ParentId"] };
            ds.Tables["Child"].PrimaryKey = new DataColumn[] { ds.Tables["Child"].Columns["ParentId"], ds.Tables["Child"].Columns["ChildId"] };

            //unique column
            ds.Tables["Parent"].Columns["String2"].Unique = true; //will not be merged

            //add Foreign Key
            ds.Tables["Child2"].Constraints.Add("Child2_FK", ds.Tables["Parent"].Columns["ParentId"], ds.Tables["Child2"].Columns["ParentId"]);

            //add relation
            ds.Relations.Add("Parent_Child", ds.Tables["Parent"].Columns["ParentId"], ds.Tables["Child"].Columns["ParentId"]);

            //add allow null constraint
            ds.Tables["Parent"].Columns["ParentBool"].AllowDBNull = false; //will not be merged

            //add Indentity column
            ds.Tables["Parent"].Columns.Add("Indentity", typeof(int));
            ds.Tables["Parent"].Columns["Indentity"].AutoIncrement = true;
            ds.Tables["Parent"].Columns["Indentity"].AutoIncrementStep = 2;

            //modify default value
            ds.Tables["Child"].Columns["String1"].DefaultValue = "Default"; //will not be merged

            //remove column
            ds.Tables["Child"].Columns.Remove("String2"); //will not be merged

            //-------------------- begin to check ----------------------------------------------
            // merge 1 - make sure the merge method invoked without exceptions
            dsTarget.Merge(ds);
            Assert.Equal("Success", "Success");

            CompareResults_1("merge 1", ds, dsTarget);

            //merge again,
            // merge 2 - make sure the merge method invoked without exceptions
            dsTarget.Merge(ds);
            Assert.Equal("Success", "Success");

            CompareResults_1("merge 2", ds, dsTarget);

            // merge second dataset - make sure the merge method invoked without exceptions
            dsTarget1.Merge(ds);
            Assert.Equal("Success", "Success");

            CompareResults_2("merge 3", ds, dsTarget1);
        }

        [Fact]
        public void Merge_RelationWithoutConstraints()
        {
            DataSet ds = new DataSet();

            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");

            DataColumn pcol = table1.Columns.Add("col1", typeof(int));
            DataColumn ccol = table2.Columns.Add("col1", typeof(int));

            DataSet ds1 = ds.Copy();
            DataRelation rel = ds1.Relations.Add("rel1", ds1.Tables[0].Columns[0],
                                ds1.Tables[1].Columns[0], false);

            ds.Merge(ds1);
            Assert.Equal(1, ds.Relations.Count);
            Assert.Equal(0, ds.Tables[0].Constraints.Count);
            Assert.Equal(0, ds.Tables[1].Constraints.Count);
        }

        [Fact]
        public void Merge_DuplicateConstraints()
        {
            DataSet ds = new DataSet();

            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");

            DataColumn pcol = table1.Columns.Add("col1", typeof(int));
            DataColumn ccol = table2.Columns.Add("col1", typeof(int));

            DataSet ds1 = ds.Copy();

            DataRelation rel = ds.Relations.Add("rel1", pcol, ccol);

            ds1.Tables[1].Constraints.Add("fk", ds1.Tables[0].Columns[0], ds1.Tables[1].Columns[0]);

            // No Exceptions shud be thrown
            ds.Merge(ds1);
            Assert.Equal(1, table2.Constraints.Count);
        }

        [Fact]
        public void Merge_DuplicateConstraints_1()
        {
            DataSet ds = new DataSet();

            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");

            DataColumn pcol = table1.Columns.Add("col1", typeof(int));
            DataColumn ccol = table2.Columns.Add("col1", typeof(int));
            DataColumn pcol1 = table1.Columns.Add("col2", typeof(int));
            DataColumn ccol1 = table2.Columns.Add("col2", typeof(int));

            DataSet ds1 = ds.Copy();

            table2.Constraints.Add("fk", pcol, ccol);
            ds1.Tables[1].Constraints.Add("fk", ds1.Tables[0].Columns["col2"], ds1.Tables[1].Columns["col2"]);

            // No Exceptions shud be thrown
            ds.Merge(ds1);
            Assert.Equal(2, table2.Constraints.Count);
            Assert.Equal("Constraint1", table2.Constraints[1].ConstraintName);
        }

        [Fact]
        public void CopyClone_RelationWithoutConstraints()
        {
            DataSet ds = new DataSet();

            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");

            DataColumn pcol = table1.Columns.Add("col1", typeof(int));
            DataColumn ccol = table2.Columns.Add("col1", typeof(int));

            DataRelation rel = ds.Relations.Add("rel1", pcol, ccol, false);

            DataSet ds1 = ds.Copy();
            DataSet ds2 = ds.Clone();

            Assert.Equal(1, ds1.Relations.Count);
            Assert.Equal(1, ds2.Relations.Count);

            Assert.Equal(0, ds1.Tables[0].Constraints.Count);
            Assert.Equal(0, ds1.Tables[1].Constraints.Count);

            Assert.Equal(0, ds2.Tables[0].Constraints.Count);
            Assert.Equal(0, ds2.Tables[1].Constraints.Count);
        }

        [Fact]
        public void Merge_ConstraintsFromReadXmlSchema()
        {
            DataSet ds = new DataSet();
            ds.ReadXml(new StringReader(
                @"<MyDataSet>
                  <xs:schema id=""MyDataSet"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema""
                 xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
                    <xs:element name=""MyDataSet"" msdata:IsDataSet=""true"" msdata:MainDataTable=""Main"" msdata:UseCurrentLocale=""true"">
                      <xs:complexType>
                        <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
                          <xs:element name=""Main"">
                            <xs:complexType>
                              <xs:sequence>
                                <xs:element name=""ID"" type=""xs:int"" minOccurs=""0"" />
                                <xs:element name=""Data"" type=""xs:string"" minOccurs=""0"" />
                              </xs:sequence>
                            </xs:complexType>
                          </xs:element>
                          <xs:element name=""Child"">
                            <xs:complexType>
                              <xs:sequence>
                                <xs:element name=""ID"" type=""xs:int"" minOccurs=""0"" />
                                <xs:element name=""PID"" type=""xs:int"" minOccurs=""0"" />
                                <xs:element name=""ChildData"" type=""xs:string"" minOccurs=""0"" />
                              </xs:sequence>
                            </xs:complexType>
                          </xs:element>
                        </xs:choice>
                      </xs:complexType>
                      <xs:unique name=""Constraint1"">
                        <xs:selector xpath="".//Main"" />
                        <xs:field xpath=""ID"" />
                      </xs:unique>
                      <xs:keyref name=""MainToChild"" refer=""Constraint1"">
                        <xs:selector xpath="".//Child"" />
                        <xs:field xpath=""PID"" />
                      </xs:keyref>
                    </xs:element>
                  </xs:schema>
                  <Main>
                    <ID>1</ID>
                    <Data>One</Data>
                  </Main>
                  <Main>
                    <ID>2</ID>
                    <Data>Two</Data>
                  </Main>
                  <Main>
                    <ID>3</ID>
                    <Data>Three</Data>
                  </Main>
                  <Child>
                    <ID>1</ID>
                    <PID>1</PID>
                    <ChildData>Parent1Child1</ChildData>
                  </Child>
                  <Child>
                    <ID>2</ID>
                    <PID>1</PID>
                    <ChildData>Parent1Child2</ChildData>
                  </Child>
                  <Child>
                    <ID>3</ID>
                    <PID>2</PID>
                    <ChildData>Parent2Child3</ChildData>
                  </Child>
                </MyDataSet>"));
            DataSet ds2 = new DataSet();
            ds2.Merge(ds, true, MissingSchemaAction.AddWithKey);
            DataRelation c = ds2.Tables[0].ChildRelations[0];
            Assert.NotNull(c.ParentKeyConstraint);
            Assert.NotNull(c.ChildKeyConstraint);
        }

        [Fact]
        public void Merge_MissingEventHandler()
        {
            Assert.Throws<DataException>(() =>
               {
                   var ds = new DataSet();
                   DataTable table1 = ds.Tables.Add("table1");

                   DataColumn pcol = table1.Columns.Add("col1", typeof(int));
                   DataColumn pcol1 = table1.Columns.Add("col2", typeof(int));

                   DataSet ds1 = ds.Copy();
                   table1.PrimaryKey = new DataColumn[] { pcol };
                   ds1.Tables[0].PrimaryKey = new DataColumn[] { ds1.Tables[0].Columns[1] };

                   // Exception shud be raised when handler is not set for MergeFailed Event
                   ds1.Merge(ds);
               });
        }

        [Fact]
        public void Merge_MissingColumn()
        {
            Assert.Throws<DataException>(() =>
           {
               var ds = new DataSet();
               DataTable table1 = ds.Tables.Add("table1");
               DataTable table2 = ds.Tables.Add("table2");

               table1.Columns.Add("col1", typeof(int));
               table2.Columns.Add("col1", typeof(int));

               DataSet ds1 = ds.Copy();

               ds1.Tables[0].Columns.Add("col2");

               ds.Merge(ds1, true, MissingSchemaAction.Error);
           });
        }

        [Fact]
        public void Merge_MissingConstraint()
        {
            DataSet ds = new DataSet();
            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");
            table1.Columns.Add("col1", typeof(int));
            table2.Columns.Add("col1", typeof(int));

            try
            {
                DataSet ds1 = ds.Copy();
                DataSet ds2 = ds.Copy();
                ds2.Tables[0].Constraints.Add("uc", ds2.Tables[0].Columns[0], false);
                ds1.Merge(ds2, true, MissingSchemaAction.Error);
                Assert.False(true);
            }
            catch (DataException e)
            {
            }

            try
            {
                DataSet ds1 = ds.Copy();
                DataSet ds2 = ds.Copy();
                ds2.Tables[0].Constraints.Add("fk", ds2.Tables[0].Columns[0], ds2.Tables[1].Columns[0]);
                ds1.Tables[0].Constraints.Add("uc", ds1.Tables[0].Columns[0], false);
                ds1.Merge(ds2, true, MissingSchemaAction.Error);
                Assert.False(true);
            }
            catch (DataException e)
            {
            }

            try
            {
                DataSet ds1 = ds.Copy();
                DataSet ds2 = ds.Copy();
                ds2.Relations.Add("rel", ds2.Tables[0].Columns[0], ds2.Tables[1].Columns[0], false);
                ds1.Merge(ds2, true, MissingSchemaAction.Error);
                Assert.False(true);
            }
            catch (ArgumentException e)
            {
            }
        }

        [Fact]
        public void Merge_PrimaryKeys_IncorrectOrder()
        {
            Assert.Throws<DataException>(() =>
           {
               var ds = new DataSet();
               DataTable table1 = ds.Tables.Add("table1");
               DataTable table2 = ds.Tables.Add("table2");
               DataColumn pcol = table1.Columns.Add("col1", typeof(int));
               DataColumn pcol1 = table1.Columns.Add("col2", typeof(int));
               DataColumn ccol = table2.Columns.Add("col1", typeof(int));

               DataSet ds1 = ds.Copy();
               table1.PrimaryKey = new DataColumn[] { pcol, pcol1 };
               ds1.Tables[0].PrimaryKey = new DataColumn[] { ds1.Tables[0].Columns[1], ds1.Tables[0].Columns[0] };

               // Though the key columns are the same, if the order is incorrect
               // Exception must be raised
               ds1.Merge(ds);
           });
        }

        private void CompareResults_1(string Msg, DataSet ds, DataSet dsTarget)
        {
            // check Parent Primary key length
            Assert.Equal(dsTarget.Tables["Parent"].PrimaryKey.Length, ds.Tables["Parent"].PrimaryKey.Length);

            // check Child Primary key length
            Assert.Equal(dsTarget.Tables["Child"].PrimaryKey.Length, ds.Tables["Child"].PrimaryKey.Length);

            // check Parent Primary key columns
            Assert.Equal(dsTarget.Tables["Parent"].PrimaryKey[0].ColumnName, ds.Tables["Parent"].PrimaryKey[0].ColumnName);

            // check Child Primary key columns[0]
            Assert.Equal(dsTarget.Tables["Child"].PrimaryKey[0].ColumnName, ds.Tables["Child"].PrimaryKey[0].ColumnName);

            // check Child Primary key columns[1]
            Assert.Equal(dsTarget.Tables["Child"].PrimaryKey[1].ColumnName, ds.Tables["Child"].PrimaryKey[1].ColumnName);

            // check Parent Unique columns
            Assert.Equal(dsTarget.Tables["Parent"].Columns["String2"].Unique, ds.Tables["Parent"].Columns["String2"].Unique);

            // check Child2 Foreign Key name
            Assert.Equal(dsTarget.Tables["Child2"].Constraints[0].ConstraintName, ds.Tables["Child2"].Constraints[0].ConstraintName);

            // check dataset relation count
            Assert.Equal(dsTarget.Relations.Count, ds.Relations.Count);

            // check dataset relation - Parent column
            Assert.Equal(dsTarget.Relations[0].ParentColumns[0].ColumnName, ds.Relations[0].ParentColumns[0].ColumnName);

            // check dataset relation - Child column 
            Assert.Equal(dsTarget.Relations[0].ChildColumns[0].ColumnName, ds.Relations[0].ChildColumns[0].ColumnName);

            // check allow null constraint
            Assert.Equal(true, dsTarget.Tables["Parent"].Columns["ParentBool"].AllowDBNull);

            // check Indentity column
            Assert.Equal(dsTarget.Tables["Parent"].Columns.Contains("Indentity"), ds.Tables["Parent"].Columns.Contains("Indentity"));

            // check Indentity column - AutoIncrementStep
            Assert.Equal(dsTarget.Tables["Parent"].Columns["Indentity"].AutoIncrementStep, ds.Tables["Parent"].Columns["Indentity"].AutoIncrementStep);

            // check Indentity column - AutoIncrement
            Assert.Equal(dsTarget.Tables["Parent"].Columns["Indentity"].AutoIncrement, ds.Tables["Parent"].Columns["Indentity"].AutoIncrement);

            // check Indentity column - DefaultValue
            Assert.Equal(true, dsTarget.Tables["Child"].Columns["String1"].DefaultValue == DBNull.Value);

            // check remove colum
            Assert.Equal(true, dsTarget.Tables["Child"].Columns.Contains("String2"));
        }

        private void CompareResults_2(string Msg, DataSet ds, DataSet dsTarget)
        {
            // check Parent Primary key length
            Assert.Equal(dsTarget.Tables["Parent"].PrimaryKey.Length, ds.Tables["Parent"].PrimaryKey.Length);

            // check Child Primary key length
            Assert.Equal(dsTarget.Tables["Child"].PrimaryKey.Length, ds.Tables["Child"].PrimaryKey.Length);

            // check Parent Primary key columns
            Assert.Equal(dsTarget.Tables["Parent"].PrimaryKey[0].ColumnName, ds.Tables["Parent"].PrimaryKey[0].ColumnName);

            // check Child Primary key columns[0]
            Assert.Equal(dsTarget.Tables["Child"].PrimaryKey[0].ColumnName, ds.Tables["Child"].PrimaryKey[0].ColumnName);

            // check Child Primary key columns[1]
            Assert.Equal(dsTarget.Tables["Child"].PrimaryKey[1].ColumnName, ds.Tables["Child"].PrimaryKey[1].ColumnName);

            // check Parent Unique columns
            Assert.Equal(dsTarget.Tables["Parent"].Columns["String2"].Unique, ds.Tables["Parent"].Columns["String2"].Unique);

            // check Child2 Foreign Key name
            Assert.Equal("Child2_FK_2", dsTarget.Tables["Child2"].Constraints[0].ConstraintName);

            // check dataset relation count
            Assert.Equal(dsTarget.Relations.Count, ds.Relations.Count);

            // check dataset relation - Parent column
            Assert.Equal(dsTarget.Relations[0].ParentColumns[0].ColumnName, ds.Relations[0].ParentColumns[0].ColumnName);

            // check dataset relation - Child column 
            Assert.Equal(dsTarget.Relations[0].ChildColumns[0].ColumnName, ds.Relations[0].ChildColumns[0].ColumnName);

            // check allow null constraint
            Assert.Equal(true, dsTarget.Tables["Parent"].Columns["ParentBool"].AllowDBNull);

            // check Indentity column
            Assert.Equal(dsTarget.Tables["Parent"].Columns.Contains("Indentity"), ds.Tables["Parent"].Columns.Contains("Indentity"));

            // check Indentity column - AutoIncrementStep
            Assert.Equal(dsTarget.Tables["Parent"].Columns["Indentity"].AutoIncrementStep, ds.Tables["Parent"].Columns["Indentity"].AutoIncrementStep);

            // check Indentity column - AutoIncrement
            Assert.Equal(dsTarget.Tables["Parent"].Columns["Indentity"].AutoIncrement, ds.Tables["Parent"].Columns["Indentity"].AutoIncrement);

            // check Indentity column - DefaultValue
            Assert.Equal(true, dsTarget.Tables["Child"].Columns["String1"].DefaultValue == DBNull.Value);

            // check remove colum
            Assert.Equal(true, dsTarget.Tables["Child"].Columns.Contains("String2"));
            // Check Relation.Nested value
            DataSet orig = new DataSet();

            DataTable parent = orig.Tables.Add("Parent");
            parent.Columns.Add("Id", typeof(int));
            parent.Columns.Add("col1", typeof(string));
            parent.Rows.Add(new object[] { 0, "aaa" });

            DataTable child = orig.Tables.Add("Child");
            child.Columns.Add("ParentId", typeof(int));
            child.Columns.Add("col1", typeof(string));
            child.Rows.Add(new object[] { 0, "bbb" });

            orig.Relations.Add("Parent_Child", parent.Columns["Id"], child.Columns["ParentId"]);
            orig.Relations["Parent_Child"].Nested = true;

            DataSet merged = new DataSet();
            merged.Merge(orig);
            Assert.Equal(orig.Relations["Parent_Child"].Nested, merged.Relations["Parent_Child"].Nested);
        }

        [Fact]
        public void Merge_ByDataTable()
        {
            //create source dataset
            var ds = new DataSet();
            //create datatable
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.TableName = "Table1";
            //add a copy of the datatable to the dataset
            ds.Tables.Add(dt.Copy());

            dt.TableName = "Table2";
            //add primary key
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };
            ds.Tables.Add(dt.Copy());
            //now the dataset hase two tables

            //create target dataset (copy of source dataset)
            DataSet dsTarget = ds.Copy();

            dt = ds.Tables["Table2"];
            //update existing row
            dt.Select("ParentId=1")[0][1] = "OldValue1";
            //add new row
            object[] arrAddedRow = new object[] { 99, "NewValue1", "NewValue2", new DateTime(0), 0.5, true };
            dt.Rows.Add(arrAddedRow);
            //delete existing rows
            foreach (DataRow dr in dt.Select("ParentId=2"))
            {
                dr.Delete();
            }

            // Merge - changed values
            dsTarget.Merge(dt);
            Assert.Equal("OldValue1", dsTarget.Tables["Table2"].Select("ParentId=1")[0][1]);

            // Merge - added values
            Assert.Equal(arrAddedRow, dsTarget.Tables["Table2"].Select("ParentId=99")[0].ItemArray);

            // Merge - deleted row
            Assert.Equal(0, dsTarget.Tables["Table2"].Select("ParentId=2").Length);

            //when merging a DataTable with TableName=null, GH throw null reference exception.
            ds = new DataSet();
            dt = new DataTable();
            dt.Columns.Add("Col1");
            dt.Rows.Add(new object[] { 1 });

            // Merge - add a table with no name
            ds.Merge(dt);
            Assert.Equal(1, ds.Tables.Count);

            // Merge - add a table with no name - check Rows.Count
            Assert.Equal(dt.Rows.Count, ds.Tables[0].Rows.Count);
        }

        [Fact]
        public void Merge_ByDataTablePreserveMissingSchemaAction()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.TableName = "Table1";
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] };

            //create target dataset (copy of source dataset)
            DataSet dsTarget = new DataSet();
            dsTarget.Tables.Add(dt.Copy());

            //add new column (for checking MissingSchemaAction)
            DataColumn dc = new DataColumn("NewColumn", typeof(float));
            dt.Columns.Add(dc);

            //Update row
            string OldValue = dt.Select("ParentId=1")[0][1].ToString();
            dt.Select("ParentId=1")[0][1] = "NewValue";
            //delete rows
            dt.Select("ParentId=2")[0].Delete();
            //add row
            object[] arrAddedRow = new object[] { 99, "NewRowValue1", "NewRowValue2", new DateTime(0), 0.5, true };
            dt.Rows.Add(arrAddedRow);

            #region "Merge(dt,true,MissingSchemaAction.Ignore )"
            DataSet dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(dt, true, MissingSchemaAction.Ignore);
            // Merge true,Ignore - Column
            Assert.Equal(false, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge true,Ignore - changed values
            Assert.Equal(OldValue, dsTarget1.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge true,Ignore - added values
            Assert.Equal(arrAddedRow, dsTarget1.Tables["Table1"].Select("ParentId=99")[0].ItemArray);

            // Merge true,Ignore - deleted row
            Assert.Equal(true, dsTarget1.Tables["Table1"].Select("ParentId=2").Length > 0);
            #endregion

            #region "Merge(dt,false,MissingSchemaAction.Ignore )"

            dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(dt, false, MissingSchemaAction.Ignore);
            // Merge true,Ignore - Column
            Assert.Equal(false, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge true,Ignore - changed values
            Assert.Equal("NewValue", dsTarget1.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge true,Ignore - added values
            Assert.Equal(arrAddedRow, dsTarget1.Tables["Table1"].Select("ParentId=99")[0].ItemArray);

            // Merge true,Ignore - deleted row
            Assert.Equal(0, dsTarget1.Tables["Table1"].Select("ParentId=2").Length);
            #endregion

            #region "Merge(dt,true,MissingSchemaAction.Add  )"
            dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(dt, true, MissingSchemaAction.Add);
            // Merge true,Add - Column
            Assert.Equal(true, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge true,Add - changed values
            Assert.Equal(OldValue, dsTarget1.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge true,Add - added values
            Assert.Equal(1, dsTarget1.Tables["Table1"].Select("ParentId=99").Length);

            // Merge true,Add - deleted row
            Assert.Equal(true, dsTarget1.Tables["Table1"].Select("ParentId=2").Length > 0);
            #endregion

            #region "Merge(dt,false,MissingSchemaAction.Add  )"
            dsTarget1 = dsTarget.Copy();
            dsTarget1.Merge(dt, false, MissingSchemaAction.Add);
            // Merge true,Add - Column
            Assert.Equal(true, dsTarget1.Tables["Table1"].Columns.Contains("NewColumn"));

            // Merge true,Add - changed values
            Assert.Equal("NewValue", dsTarget1.Tables["Table1"].Select("ParentId=1")[0][1]);

            // Merge true,Add - added values
            Assert.Equal(1, dsTarget1.Tables["Table1"].Select("ParentId=99").Length);

            // Merge true,Add - deleted row
            Assert.Equal(0, dsTarget1.Tables["Table1"].Select("ParentId=2").Length);
            #endregion
        }

        [Fact]
        public void Namespace()
        {
            var ds = new DataSet();

            // Checking Namespace default
            Assert.Equal(string.Empty, ds.Namespace);

            // Checking Namespace set/get
            string s = "MyNamespace";
            ds.Namespace = s;
            Assert.Equal(s, ds.Namespace);
        }

        [Fact]
        public void Prefix()
        {
            var ds = new DataSet();

            // Checking Prefix default
            Assert.Equal(string.Empty, ds.Prefix);

            // Checking Prefix set/get
            string s = "MyPrefix";
            ds.Prefix = s;
            Assert.Equal(s, ds.Prefix);
        }

        [Fact]
        public void ReadXmlSchema_ByStream()
        {
            DataSet ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            ds1.Tables.Add(DataProvider.CreateChildDataTable());

            MemoryStream ms = new MemoryStream();
            //write xml  schema only
            ds1.WriteXmlSchema(ms);

            MemoryStream ms1 = new MemoryStream(ms.GetBuffer());
            //copy schema
            DataSet ds2 = new DataSet();
            ds2.ReadXmlSchema(ms1);

            //check xml schema
            // ReadXmlSchema - Tables count
            Assert.Equal(ds2.Tables.Count, ds1.Tables.Count);

            // ReadXmlSchema - Tables 0 Col count
            Assert.Equal(ds1.Tables[0].Columns.Count, ds2.Tables[0].Columns.Count);

            // ReadXmlSchema - Tables 1 Col count
            Assert.Equal(ds1.Tables[1].Columns.Count, ds2.Tables[1].Columns.Count);

            //check some colummns types
            // ReadXmlSchema - Tables 0 Col type
            Assert.Equal(ds1.Tables[0].Columns[0].GetType(), ds2.Tables[0].Columns[0].GetType());

            // ReadXmlSchema - Tables 1 Col type
            Assert.Equal(ds1.Tables[1].Columns[3].GetType(), ds2.Tables[1].Columns[3].GetType());

            //check that no data exists
            // ReadXmlSchema - Table 1 row count
            Assert.Equal(0, ds2.Tables[0].Rows.Count);

            // ReadXmlSchema - Table 2 row count
            Assert.Equal(0, ds2.Tables[1].Rows.Count);
        }

        [Fact]
        public void ReadXmlSchema_ByFileName()
        {
            string sTempFileName = Path.Combine(Path.GetTempPath(), "tmpDataSet_ReadWriteXml_43899.xml");

            DataSet ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            ds1.Tables.Add(DataProvider.CreateChildDataTable());

            //write xml file, schema only
            ds1.WriteXmlSchema(sTempFileName);

            //copy both data and schema
            DataSet ds2 = new DataSet();

            ds2.ReadXmlSchema(sTempFileName);

            //check xml schema
            // ReadXmlSchema - Tables count
            Assert.Equal(ds2.Tables.Count, ds1.Tables.Count);

            // ReadXmlSchema - Tables 0 Col count
            Assert.Equal(ds1.Tables[0].Columns.Count, ds2.Tables[0].Columns.Count);

            // ReadXmlSchema - Tables 1 Col count
            Assert.Equal(ds1.Tables[1].Columns.Count, ds2.Tables[1].Columns.Count);

            //check some colummns types
            // ReadXmlSchema - Tables 0 Col type
            Assert.Equal(ds1.Tables[0].Columns[0].GetType(), ds2.Tables[0].Columns[0].GetType());

            // ReadXmlSchema - Tables 1 Col type
            Assert.Equal(ds1.Tables[1].Columns[3].GetType(), ds2.Tables[1].Columns[3].GetType());

            //check that no data exists
            // ReadXmlSchema - Table 1 row count
            Assert.Equal(0, ds2.Tables[0].Rows.Count);

            // ReadXmlSchema - Table 2 row count
            Assert.Equal(0, ds2.Tables[1].Rows.Count);

            //try to delete the file
            File.Delete(sTempFileName);
        }

        [Fact]
        public void ReadXmlSchema_ByTextReader()
        {
            DataSet ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            ds1.Tables.Add(DataProvider.CreateChildDataTable());

            StringWriter sw = new StringWriter();
            //write xml file, schema only
            ds1.WriteXmlSchema(sw);

            StringReader sr = new StringReader(sw.GetStringBuilder().ToString());
            //copy both data and schema
            DataSet ds2 = new DataSet();
            ds2.ReadXmlSchema(sr);

            //check xml schema
            // ReadXmlSchema - Tables count
            Assert.Equal(ds2.Tables.Count, ds1.Tables.Count);

            // ReadXmlSchema - Tables 0 Col count
            Assert.Equal(ds1.Tables[0].Columns.Count, ds2.Tables[0].Columns.Count);

            // ReadXmlSchema - Tables 1 Col count
            Assert.Equal(ds1.Tables[1].Columns.Count, ds2.Tables[1].Columns.Count);

            //check some colummns types
            // ReadXmlSchema - Tables 0 Col type
            Assert.Equal(ds1.Tables[0].Columns[0].GetType(), ds2.Tables[0].Columns[0].GetType());

            // ReadXmlSchema - Tables 1 Col type
            Assert.Equal(ds1.Tables[1].Columns[3].GetType(), ds2.Tables[1].Columns[3].GetType());

            //check that no data exists
            // ReadXmlSchema - Table 1 row count
            Assert.Equal(0, ds2.Tables[0].Rows.Count);

            // ReadXmlSchema - Table 2 row count
            Assert.Equal(0, ds2.Tables[1].Rows.Count);
        }

        [Fact]
        public void ReadXmlSchema_ByXmlReader()
        {
            DataSet ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            ds1.Tables.Add(DataProvider.CreateChildDataTable());

            StringWriter sw = new StringWriter();
            XmlTextWriter xmlTW = new XmlTextWriter(sw);
            //write xml file, schema only
            ds1.WriteXmlSchema(xmlTW);
            xmlTW.Flush();

            StringReader sr = new StringReader(sw.ToString());
            XmlTextReader xmlTR = new XmlTextReader(sr);

            //copy both data and schema
            DataSet ds2 = new DataSet();
            ds2.ReadXmlSchema(xmlTR);

            //check xml schema
            // ReadXmlSchema - Tables count
            Assert.Equal(ds2.Tables.Count, ds1.Tables.Count);

            // ReadXmlSchema - Tables 0 Col count
            Assert.Equal(ds1.Tables[0].Columns.Count, ds2.Tables[0].Columns.Count);

            // ReadXmlSchema - Tables 1 Col count
            Assert.Equal(ds1.Tables[1].Columns.Count, ds2.Tables[1].Columns.Count);

            //check some colummns types
            // ReadXmlSchema - Tables 0 Col type
            Assert.Equal(ds1.Tables[0].Columns[0].GetType(), ds2.Tables[0].Columns[0].GetType());

            // ReadXmlSchema - Tables 1 Col type
            Assert.Equal(ds1.Tables[1].Columns[3].GetType(), ds2.Tables[1].Columns[3].GetType());

            //check that no data exists
            // ReadXmlSchema - Table 1 row count
            Assert.Equal(0, ds2.Tables[0].Rows.Count);

            // ReadXmlSchema - Table 2 row count
            Assert.Equal(0, ds2.Tables[1].Rows.Count);
        }

        [Fact]
        public void ReadXml_Strm2()
        {
            string input = string.Empty;

            StringReader sr;
            var ds = new DataSet();

            input += "<?xml version=\"1.0\"?>";
            input += "<Stock name=\"MSFT\">";
            input += "		<Company name=\"Microsoft Corp.\"/>";
            input += "		<Price type=\"high\">";
            input += "			<Value>10.0</Value>";
            input += "			<Date>01/20/2000</Date>";
            input += "		</Price>";
            input += "		<Price type=\"low\">";
            input += "			<Value>1.0</Value>";
            input += "			<Date>03/21/2002</Date>";
            input += "		</Price>";
            input += "		<Price type=\"current\">";
            input += "			<Value>3.0</Value>";
            input += "			<Date>TODAY</Date>";
            input += "		</Price>";
            input += "</Stock>";

            sr = new StringReader(input);

            ds.ReadXml(sr);

            // Relation Count
            Assert.Equal(2, ds.Relations.Count);

            // RelationName 1
            Assert.Equal("Stock_Company", ds.Relations[0].RelationName);

            // RelationName 2
            Assert.Equal("Stock_Price", ds.Relations[1].RelationName);

            // Tables count
            Assert.Equal(3, ds.Tables.Count);

            // Tables[0] ChildRelations count
            Assert.Equal(2, ds.Tables[0].ChildRelations.Count);

            // Tables[0] ChildRelations[0] name
            Assert.Equal("Stock_Company", ds.Tables[0].ChildRelations[0].RelationName);

            // Tables[0] ChildRelations[1] name
            Assert.Equal("Stock_Price", ds.Tables[0].ChildRelations[1].RelationName);

            // Tables[1] ChildRelations count
            Assert.Equal(0, ds.Tables[1].ChildRelations.Count);

            // Tables[2] ChildRelations count
            Assert.Equal(0, ds.Tables[2].ChildRelations.Count);

            // Tables[0] ParentRelations count
            Assert.Equal(0, ds.Tables[0].ParentRelations.Count);

            // Tables[1] ParentRelations count
            Assert.Equal(1, ds.Tables[1].ParentRelations.Count);

            // Tables[1] ParentRelations[0] name
            Assert.Equal("Stock_Company", ds.Tables[1].ParentRelations[0].RelationName);

            // Tables[2] ParentRelations count
            Assert.Equal(1, ds.Tables[2].ParentRelations.Count);

            // Tables[2] ParentRelations[0] name
            Assert.Equal("Stock_Price", ds.Tables[2].ParentRelations[0].RelationName);
        }
        [Fact]
        public void ReadXml_Strm3()
        {
            DataSet ds = new DataSet("TestDataSet");
            string input = string.Empty;
            StringReader sr;

            input += "<?xml version=\"1.0\" standalone=\"yes\"?>";
            input += "<Stocks><Stock name=\"MSFT\"><Company name=\"Microsoft Corp.\" /><Price type=\"high\"><Value>10.0</Value>";
            input += "<Date>01/20/2000</Date></Price><Price type=\"low\"><Value>10</Value><Date>03/21/2002</Date></Price>";
            input += "<Price type=\"current\"><Value>3.0</Value><Date>TODAY</Date></Price></Stock><Stock name=\"GE\">";
            input += "<Company name=\"General Electric\" /><Price type=\"high\"><Value>22.23</Value><Date>02/12/2001</Date></Price>";
            input += "<Price type=\"low\"><Value>1.97</Value><Date>04/20/2003</Date></Price><Price type=\"current\"><Value>3.0</Value>";
            input += "<Date>TODAY</Date></Price></Stock></Stocks>";
            sr = new StringReader(input);
            ds.EnforceConstraints = false;
            ds.ReadXml(sr);

            //Test that all added columns have "Hidden" mapping type.
            // StockTable.Stock_IdCol.ColumnMapping
            Assert.Equal(MappingType.Hidden, ds.Tables["Stock"].Columns["Stock_Id"].ColumnMapping);

            // CompanyTable.Stock_IdCol.ColumnMapping
            Assert.Equal(MappingType.Hidden, ds.Tables["Company"].Columns["Stock_Id"].ColumnMapping);

            // PriceTable.Stock_IdCol.ColumnMapping
            Assert.Equal(MappingType.Hidden, ds.Tables["Price"].Columns["Stock_Id"].ColumnMapping);
        }

        [Fact]
        public void ReadXml_Strm4()
        {
            _ds = new DataSet("Stocks");
            string input = string.Empty;
            StringReader sr;

            input += "<?xml version=\"1.0\"?>";
            input += "<Stocks>";
            input += "		<Stock name=\"MSFT\">";
            input += "			<Company name=\"Microsoft Corp.\" />";
            input += "			<Company name=\"General Electric\"/>";
            input += "			<Price type=\"high\">";
            input += "				<Value>10.0</Value>";
            input += "				<Date>01/20/2000</Date>";
            input += "			</Price>";
            input += "			<Price type=\"low\">";
            input += "				<Value>1.0</Value>";
            input += "				<Date>03/21/2002</Date>";
            input += "			</Price>";
            input += "			<Price type=\"current\">";
            input += "				<Value>3.0</Value>";
            input += "				<Date>TODAY</Date>";
            input += "			</Price>";
            input += "		</Stock>";
            input += "		<Stock name=\"GE\">";
            input += "			<Company name=\"GE company\"/>";
            input += "			<Price type=\"high\">";
            input += "				<Value>22.23</Value>";
            input += "				<Date>02/12/2001</Date>";
            input += "			</Price>";
            input += "			<Price type=\"low\">";
            input += "				<Value>1.97</Value>";
            input += "				<Date>04/20/2003</Date>";
            input += "			</Price>";
            input += "			<Price type=\"current\">";
            input += "				<Value>3.0</Value>";
            input += "				<Date>TODAY</Date>";
            input += "			</Price>";
            input += "		</Stock>";
            input += "		<Stock name=\"Intel\">";
            input += "			<Company name=\"Intel Corp.\"/>";
            input += "			<Company name=\"Test1\" />";
            input += "			<Company name=\"Test2\"/>";
            input += "			<Price type=\"high\">";
            input += "				<Value>15.0</Value>";
            input += "				<Date>01/25/2000</Date>";
            input += "			</Price>";
            input += "			<Price type=\"low\">";
            input += "				<Value>1.0</Value>";
            input += "				<Date>03/23/2002</Date>";
            input += "			</Price>";
            input += "			<Price type=\"current\">";
            input += "				<Value>3.0</Value>";
            input += "				<Date>TODAY</Date>";
            input += "			</Price>";
            input += "		</Stock>";
            input += "		<Stock name=\"Mainsoft\">";
            input += "			<Company name=\"Mainsoft Corp.\"/>";
            input += "			<Price type=\"high\">";
            input += "				<Value>30.0</Value>";
            input += "				<Date>01/26/2000</Date>";
            input += "			</Price>";
            input += "			<Price type=\"low\">";
            input += "				<Value>1.0</Value>";
            input += "				<Date>03/26/2002</Date>";
            input += "			</Price>";
            input += "			<Price type=\"current\">";
            input += "				<Value>27.0</Value>";
            input += "				<Date>TODAY</Date>";
            input += "			</Price>";
            input += "		</Stock>";
            input += "</Stocks>";

            sr = new StringReader(input);
            _ds.EnforceConstraints = true;
            _ds.ReadXml(sr);
            privateTestCase("TestCase 1", "Company", "name='Microsoft Corp.'", "Stock", "name='MSFT'", "DS320");
            privateTestCase("TestCase 2", "Company", "name='General Electric'", "Stock", "name='MSFT'", "DS321");
            privateTestCase("TestCase 3", "Price", "Date='01/20/2000'", "Stock", "name='MSFT'", "DS322");
            privateTestCase("TestCase 4", "Price", "Date='03/21/2002'", "Stock", "name='MSFT'", "DS323");
            privateTestCase("TestCase 5", "Company", "name='GE company'", "Stock", "name='GE'", "DS324");
            privateTestCase("TestCase 6", "Price", "Date='02/12/2001'", "Stock", "name='GE'", "DS325");
            privateTestCase("TestCase 7", "Price", "Date='04/20/2003'", "Stock", "name='GE'", "DS326");
            privateTestCase("TestCase 8", "Company", "name='Intel Corp.'", "Stock", "name='Intel'", "DS327");
            privateTestCase("TestCase 9", "Company", "name='Test1'", "Stock", "name='Intel'", "DS328");
            privateTestCase("TestCase 10", "Company", "name='Test2'", "Stock", "name='Intel'", "DS329");
            privateTestCase("TestCase 11", "Price", "Date='01/25/2000'", "Stock", "name='Intel'", "DS330");
            privateTestCase("TestCase 12", "Price", "Date='03/23/2002'", "Stock", "name='Intel'", "DS331");
            privateTestCase("TestCase 13", "Company", "name='Mainsoft Corp.'", "Stock", "name='Mainsoft'", "DS332");
            privateTestCase("TestCase 12", "Price", "Date='01/26/2000'", "Stock", "name='Mainsoft'", "DS333");
            privateTestCase("TestCase 12", "Price", "Date='03/26/2002'", "Stock", "name='Mainsoft'", "DS334");
        }

        private void privateTestCase(string name, string toTable, string toTestSelect, string toCompareTable, string toCompareSelect, string AssertTag)
        {
            DataRow drToTest = _ds.Tables[toTable].Select(toTestSelect)[0];
            DataRow drToCompare = _ds.Tables[toCompareTable].Select(toCompareSelect)[0];
            Assert.Equal(_ds.Tables[toTable].Select(toTestSelect)[0]["Stock_Id"], _ds.Tables[toCompareTable].Select(toCompareSelect)[0]["Stock_Id"]);
        }

        [Fact]
        public void ReadXml_Strm5()
        {
            string xmlData;
            string name;
            string expected;
            #region "TestCase 1 - Empty string"
            // Empty string
            var ds = new DataSet();
            StringReader sr = new StringReader(string.Empty);
            XmlTextReader xReader = new XmlTextReader(sr);
            Assert.Throws<XmlException>(() => ds.ReadXml(xReader));
            #endregion
            #region "TestCase 2 - Single element"
            name = "Single element";
            expected = "DataSet Name=a Tables count=0";
            xmlData = "<a>1</a>";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 3 - Nesting one level single element."
            name = "Nesting one level single element.";
            expected = "DataSet Name=NewDataSet Tables count=1 Table Name=a Rows count=1 Items count=1 1";
            xmlData = "<a><b>1</b></a>";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 4 - Nesting one level multiple elements."
            name = "Nesting one level multiple elements.";
            expected = "DataSet Name=NewDataSet Tables count=1 Table Name=a Rows count=1 Items count=3 bb cc dd";
            xmlData = "<a><b>bb</b><c>cc</c><d>dd</d></a>";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 5 - Nesting two levels single elements."
            name = "Nesting two levels single elements.";
            expected = "DataSet Name=a Tables count=1 Table Name=b Rows count=1 Items count=1 cc";
            xmlData = "<a><b><c>cc</c></b></a>";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 6 - Nesting two levels multiple elements."
            name = "Nesting two levels multiple elements.";
            expected = "DataSet Name=a Tables count=1 Table Name=b Rows count=1 Items count=2 cc dd";
            xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>cc</c>";
            xmlData += "<d>dd</d>";
            xmlData += "</b>";
            xmlData += "</a>";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 7 - Nesting two levels multiple elements."
            name = "Nesting two levels multiple elements.";
            expected = "DataSet Name=a Tables count=2 Table Name=b Rows count=1 Items count=2 cc dd Table Name=e Rows count=1 Items count=2 cc dd";
            xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>cc</c>";
            xmlData += "<d>dd</d>";
            xmlData += "</b>";
            xmlData += "<e>";
            xmlData += "<c>cc</c>";
            xmlData += "<d>dd</d>";
            xmlData += "</e>";
            xmlData += "</a>";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 8 - Nesting three levels single element."
            name = "Nesting three levels single element.";
            xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>";
            xmlData += "<d>dd</d>";
            xmlData += "</c>";
            xmlData += "</b>";
            xmlData += "</a>";
            expected = "DataSet Name=a Tables count=2 Table Name=b Rows count=1 Items count=1 0 Table Name=c Rows count=1 Items count=2 0 dd";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 9 - Nesting three levels multiple elements."
            name = "Nesting three levels multiple elements.";
            xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>";
            xmlData += "<d>dd</d>";
            xmlData += "<e>ee</e>";
            xmlData += "</c>";
            xmlData += "</b>";
            xmlData += "</a>";
            expected = "DataSet Name=a Tables count=2 Table Name=b Rows count=1 Items count=1 0 Table Name=c Rows count=1 Items count=3 0 dd ee";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 10 - Nesting three levels multiple elements."
            name = "Nesting three levels multiple elements.";
            xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>";
            xmlData += "<d>dd</d>";
            xmlData += "<e>ee</e>";
            xmlData += "</c>";
            xmlData += "<f>ff</f>";
            xmlData += "</b>";
            xmlData += "</a>";
            expected = "DataSet Name=a Tables count=2 Table Name=b Rows count=1 Items count=2 0 ff Table Name=c Rows count=1 Items count=3 0 dd ee";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 11 - Nesting three levels multiple elements."
            name = "Nesting three levels multiple elements.";
            xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>";
            xmlData += "<d>dd</d>";
            xmlData += "<e>ee</e>";
            xmlData += "</c>";
            xmlData += "<f>ff</f>";
            xmlData += "<g>";
            xmlData += "<h>hh</h>";
            xmlData += "<i>ii</i>";
            xmlData += "</g>";
            xmlData += "<j>jj</j>";
            xmlData += "</b>";
            xmlData += "</a>";
            expected = "DataSet Name=a Tables count=3 Table Name=b Rows count=1 Items count=3 0 ff jj Table Name=c Rows count=1 Items count=3 0 dd ee Table Name=g Rows count=1 Items count=3 0 hh ii";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 12 - Nesting three levels multiple elements."
            name = "Nesting three levels multiple elements.";
            xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>";
            xmlData += "<d>dd</d>";
            xmlData += "<e>ee</e>";
            xmlData += "</c>";
            xmlData += "<f>ff</f>";
            xmlData += "</b>";
            xmlData += "<g>";
            xmlData += "<h>";
            xmlData += "<i>ii</i>";
            xmlData += "<j>jj</j>";
            xmlData += "</h>";
            xmlData += "<f>ff</f>";
            xmlData += "</g>";
            xmlData += "</a>";
            expected = "DataSet Name=a Tables count=4 Table Name=b Rows count=1 Items count=2 0 ff Table Name=c Rows count=1 Items count=3 0 dd ee Table Name=g Rows count=1 Items count=2 ff 0 Table Name=h Rows count=1 Items count=3 0 ii jj";
            PrivateTestCase(name, expected, xmlData);
            #endregion
            #region "TestCase 13 - Nesting three levels multiple elements."
            name = "Nesting three levels multiple elements.";
            xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>";
            xmlData += "<d>dd</d>";
            xmlData += "<e>ee</e>";
            xmlData += "</c>";
            xmlData += "<f>ff</f>";
            xmlData += "<k>";
            xmlData += "<l>ll</l>";
            xmlData += "<m>mm</m>";
            xmlData += "</k>";
            xmlData += "<n>nn</n>";
            xmlData += "</b>";
            xmlData += "<g>";
            xmlData += "<h>";
            xmlData += "<i>ii</i>";
            xmlData += "<j>jj</j>";
            xmlData += "</h>";
            xmlData += "<o>oo</o>";
            xmlData += "</g>";
            xmlData += "</a>";
            expected = "DataSet Name=a Tables count=5 Table Name=b Rows count=1 Items count=3 0 ff nn Table Name=c Rows count=1 Items count=3 0 dd ee Table Name=k Rows count=1 Items count=3 0 ll mm Table Name=g Rows count=1 Items count=2 0 oo Table Name=h Rows count=1 Items count=3 0 ii jj";
            PrivateTestCase(name, expected, xmlData);
            #endregion

            #region "TestCase 14 - for Bug 2387 (System.Data.DataSet.ReadXml(..) - ArgumentException while reading specific XML)"

            name = "Specific XML - for Bug 2387";
            expected = "DataSet Name=PKRoot Tables count=2 Table Name=Content Rows count=4 Items count=2 0  Items count=2 1 103 Items count=2 2 123 Items count=2 3 252 Table Name=Cont Rows count=3 Items count=3 1 103 0 Items count=3 2 123 0 Items count=3 3 252 -4";
            xmlData = "<PKRoot><Content /><Content><ContentId>103</ContentId><Cont><ContentId>103</ContentId><ContentStatusId>0</ContentStatusId></Cont></Content><Content><ContentId>123</ContentId><Cont><ContentId>123</ContentId><ContentStatusId>0</ContentStatusId></Cont></Content><Content><ContentId>252</ContentId><Cont><ContentId>252</ContentId><ContentStatusId>-4</ContentStatusId></Cont></Content></PKRoot>";
            PrivateTestCase(name, expected, xmlData);

            #endregion
        }

        private void PrivateTestCase(string a_name, string a_expected, string a_xmlData)
        {
            var ds = new DataSet();
            StringReader sr = new StringReader(a_xmlData);
            XmlTextReader xReader = new XmlTextReader(sr);
            ds.ReadXml(xReader);
            Assert.Equal(a_expected, dataSetDescription(ds));
        }

        private string dataSetDescription(DataSet ds)
        {
            string desc = string.Empty;
            desc += "DataSet Name=" + ds.DataSetName;
            desc += " Tables count=" + ds.Tables.Count;
            foreach (DataTable dt in ds.Tables)
            {
                desc += " Table Name=" + dt.TableName;
                desc += " Rows count=" + dt.Rows.Count;

                string[] colNames = new string[dt.Columns.Count];
                for (int i = 0; i < dt.Columns.Count; i++)
                    colNames[i] = dt.Columns[i].ColumnName;

                Array.Sort(colNames);

                foreach (DataRow dr in dt.Rows)
                {
                    desc += " Items count=" + dr.ItemArray.Length;
                    foreach (string name in colNames)
                    {
                        desc += " " + dr[name].ToString();
                    }
                }
            }
            return desc;
        }

        [Fact]
        public void ReadXml_Strm6()
        {
            // TC1
            var ds = new DataSet();
            string xmlData = string.Empty;
            xmlData += "<a>";
            xmlData += "<b>";
            xmlData += "<c>1</c>";
            xmlData += "<c>2</c>";
            xmlData += "<c>3</c>";
            xmlData += "</b>";
            xmlData += "</a>";
            StringReader sr = new StringReader(xmlData);
            XmlTextReader xReader = new XmlTextReader(sr);
            ds.ReadXml(xReader);
            Assert.Equal(3, ds.Tables["c"].Rows.Count);
        }

        [Fact]
        public void ReadXmlSchema_2()
        {
            var ds = new DataSet();
            string xmlData = string.Empty;
            xmlData += "<?xml version=\"1.0\"?>";
            xmlData += "<xs:schema id=\"SiteConfiguration\" targetNamespace=\"http://tempuri.org/PortalCfg.xsd\" xmlns:mstns=\"http://tempuri.org/PortalCfg.xsd\" xmlns=\"http://tempuri.org/PortalCfg.xsd\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" attributeFormDefault=\"qualified\" elementFormDefault=\"qualified\">";
            xmlData += "<xs:element name=\"SiteConfiguration\" msdata:IsDataSet=\"true\" msdata:EnforceConstraints=\"False\">";
            xmlData += "<xs:complexType>";
            xmlData += "<xs:choice maxOccurs=\"unbounded\">";
            xmlData += "<xs:element name=\"Tab\">";
            xmlData += "<xs:complexType>";
            xmlData += "<xs:sequence>";
            xmlData += "<xs:element name=\"Module\" minOccurs=\"0\" maxOccurs=\"unbounded\">";
            xmlData += "<xs:complexType>";
            xmlData += "<xs:attribute name=\"ModuleId\" form=\"unqualified\" type=\"xs:int\" />";
            xmlData += "</xs:complexType>";
            xmlData += "</xs:element>";
            xmlData += "</xs:sequence>";
            xmlData += "<xs:attribute name=\"TabId\" form=\"unqualified\" type=\"xs:int\" />";
            xmlData += "</xs:complexType>";
            xmlData += "</xs:element>";
            xmlData += "</xs:choice>";
            xmlData += "</xs:complexType>";
            xmlData += "<xs:key name=\"TabKey\" msdata:PrimaryKey=\"true\">";
            xmlData += "<xs:selector xpath=\".//mstns:Tab\" />";
            xmlData += "<xs:field xpath=\"@TabId\" />";
            xmlData += "</xs:key>";
            xmlData += "<xs:key name=\"ModuleKey\" msdata:PrimaryKey=\"true\">";
            xmlData += "<xs:selector xpath=\".//mstns:Module\" />";
            xmlData += "<xs:field xpath=\"@ModuleID\" />";
            xmlData += "</xs:key>";
            xmlData += "</xs:element>";
            xmlData += "</xs:schema>";

            ds.ReadXmlSchema(new StringReader(xmlData));
        }

        [Fact]
        public void WriteXmlSchema_ForignKeyConstraint()
        {
            DataSet ds1 = new DataSet();

            DataTable table1 = ds1.Tables.Add();
            DataTable table2 = ds1.Tables.Add();

            DataColumn col1_1 = table1.Columns.Add("col1", typeof(int));
            DataColumn col2_1 = table2.Columns.Add("col1", typeof(int));

            table2.Constraints.Add("fk", col1_1, col2_1);

            StringWriter sw = new StringWriter();
            ds1.WriteXmlSchema(sw);
            string xml = sw.ToString();

            Assert.True(xml.IndexOf(@"<xs:keyref name=""fk"" refer=""Constraint1"" " +
                        @"msdata:ConstraintOnly=""true"">") != -1, "#1");
        }

        [Fact]
        public void WriteXmlSchema_RelationAnnotation()
        {
            DataSet ds1 = new DataSet();

            DataTable table1 = ds1.Tables.Add();
            DataTable table2 = ds1.Tables.Add();

            DataColumn col1_1 = table1.Columns.Add("col1", typeof(int));
            DataColumn col2_1 = table2.Columns.Add("col1", typeof(int));

            ds1.Relations.Add("rel", col1_1, col2_1, false);

            StringWriter sw = new StringWriter();
            ds1.WriteXmlSchema(sw);
            string xml = sw.ToString();


            Assert.True(xml.IndexOf(@"<msdata:Relationship name=""rel"" msdata:parent=""Table1""" +
                        @" msdata:child=""Table2"" msdata:parentkey=""col1"" " +
                        @"msdata:childkey=""col1"" />") != -1, "#1");
        }

        [Fact]
        public void WriteXmlSchema_Relations_ForeignKeys()
        {
            MemoryStream ms = null;
            MemoryStream ms1 = null;

            DataSet ds1 = new DataSet();

            DataTable table1 = ds1.Tables.Add("Table 1");
            DataTable table2 = ds1.Tables.Add("Table 2");

            DataColumn col1_1 = table1.Columns.Add("col 1", typeof(int));
            DataColumn col1_2 = table1.Columns.Add("col 2", typeof(int));
            DataColumn col1_3 = table1.Columns.Add("col 3", typeof(int));
            DataColumn col1_4 = table1.Columns.Add("col 4", typeof(int));
            DataColumn col1_5 = table1.Columns.Add("col 5", typeof(int));
            DataColumn col1_6 = table1.Columns.Add("col 6", typeof(int));
            DataColumn col1_7 = table1.Columns.Add("col 7", typeof(int));

            DataColumn col2_1 = table2.Columns.Add("col 1", typeof(int));
            DataColumn col2_2 = table2.Columns.Add("col 2", typeof(int));
            DataColumn col2_3 = table2.Columns.Add("col 3", typeof(int));
            DataColumn col2_4 = table2.Columns.Add("col 4", typeof(int));
            DataColumn col2_5 = table2.Columns.Add("col 5", typeof(int));
            DataColumn col2_6 = table2.Columns.Add("col 6", typeof(int));

            ds1.Relations.Add("rel 1",
                new DataColumn[] { col1_1, col1_2 },
                new DataColumn[] { col2_1, col2_2 });
            ds1.Relations.Add("rel 2",
                new DataColumn[] { col1_3, col1_4 },
                new DataColumn[] { col2_3, col2_4 },
                false);

            table1.Constraints.Add("pk 1", col1_7, true);

            table2.Constraints.Add("fk 1",
                new DataColumn[] { col1_5, col1_6 },
                new DataColumn[] { col2_5, col2_6 });

            ms = new MemoryStream();
            ds1.WriteXmlSchema(ms);

            ms1 = new MemoryStream(ms.GetBuffer());
            DataSet ds2 = new DataSet();
            ds2.ReadXmlSchema(ms1);

            Assert.Equal(2, ds2.Relations.Count);
            Assert.Equal(3, ds2.Tables[0].Constraints.Count);
            Assert.Equal(2, ds2.Tables[1].Constraints.Count);

            Assert.True(ds2.Relations.Contains("rel 1"));
            Assert.True(ds2.Relations.Contains("rel 2"));

            Assert.True(ds2.Tables[0].Constraints.Contains("pk 1"));
            Assert.True(ds2.Tables[1].Constraints.Contains("fk 1"));
            Assert.True(ds2.Tables[1].Constraints.Contains("rel 1"));

            Assert.Equal(2, ds2.Relations["rel 1"].ParentColumns.Length);
            Assert.Equal(2, ds2.Relations["rel 1"].ChildColumns.Length);

            Assert.Equal(2, ds2.Relations["rel 2"].ParentColumns.Length);
            Assert.Equal(2, ds2.Relations["rel 2"].ChildColumns.Length);

            ForeignKeyConstraint fk = (ForeignKeyConstraint)ds2.Tables[1].Constraints["fk 1"];
            Assert.Equal(2, fk.RelatedColumns.Length);
            Assert.Equal(2, fk.Columns.Length);
        }

        [Fact]
        public void RejectChanges()
        {
            DataSet ds1, ds2 = new DataSet();
            ds2.Tables.Add(DataProvider.CreateParentDataTable());
            ds1 = ds2.Copy();

            //create changes
            ds2.Tables[0].Rows[0][0] = "70";
            ds2.Tables[0].Rows[1].Delete();
            ds2.Tables[0].Rows.Add(new object[] { 9, "string1", "string2" });

            // RejectChanges
            ds2.RejectChanges();
            Assert.Equal(ds2.GetXml(), ds1.GetXml());
        }

        [Fact]
        public void Relations()
        {
            DataTable dtChild1, dtChild2, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild1 = DataProvider.CreateChildDataTable();
            dtChild1.TableName = "Child";
            dtChild2 = DataProvider.CreateChildDataTable();
            dtChild2.TableName = "CHILD";
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild1);
            ds.Tables.Add(dtChild2);

            ds.Tables.Add(dtParent);

            DataRelation drl = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild1.Columns["ParentId"]);
            DataRelation drl1 = new DataRelation("Parent-CHILD", dtParent.Columns["ParentId"], dtChild2.Columns["ParentId"]);

            // Checking Relations - default value
            //Check default
            Assert.Equal(0, ds.Relations.Count);

            ds.Relations.Add(drl);

            // Checking Relations Count
            Assert.Equal(1, ds.Relations.Count);

            // Checking Relations Value
            Assert.Equal(drl, ds.Relations[0]);

            // Checking Relations - get by name
            Assert.Equal(drl, ds.Relations["Parent-Child"]);

            // Checking Relations - get by name case sensetive
            Assert.Equal(drl, ds.Relations["PARENT-CHILD"]);

            // Checking Relations Count 2
            ds.Relations.Add(drl1);
            Assert.Equal(2, ds.Relations.Count);

            // Checking Relations - get by name case sensetive,ArgumentException
            AssertExtensions.Throws<ArgumentException>(null, () => ds.Relations["PARENT-CHILD"]);
        }

        [Fact]
        public void Reset()
        {
            DataTable dt1 = DataProvider.CreateParentDataTable();
            DataTable dt2 = DataProvider.CreateChildDataTable();
            dt1.PrimaryKey = new DataColumn[] { dt1.Columns[0] };
            dt2.PrimaryKey = new DataColumn[] { dt2.Columns[0], dt2.Columns[1] };
            var ds = new DataSet();
            ds.Tables.AddRange(new DataTable[] { dt1, dt2 });
            DataRelation rel = new DataRelation("Rel", dt1.Columns["ParentId"], dt2.Columns["ParentId"]);
            ds.Relations.Add(rel);

            ds.Reset();

            // Reset - Relations
            Assert.Equal(0, ds.Relations.Count);
            // Reset - Tables
            Assert.Equal(0, ds.Tables.Count);
        }

        [Fact]
        public void ShouldSerializeRelations()
        {
            // DataSet ShouldSerializeRelations
            newDataSet ds = new newDataSet();

            Assert.Equal(true, ds.testMethod());
        }

        private class newDataSet : DataSet
        {
            public bool testMethod()
            {
                return ShouldSerializeRelations();
            }
        }
        [Fact]
        public void ShouldSerializeTables()
        {
            // DataSet ShouldSerializeTables
            newDataSet1 ds = new newDataSet1();

            Assert.Equal(true, ds.testMethod());
        }

        private class newDataSet1 : DataSet
        {
            public bool testMethod()
            {
                return ShouldSerializeTables();
            }
        }
        [Fact]
        public void Tables()
        {
            //References by name to tables and relations in a DataSet are case-sensitive. Two or more tables or relations can exist in a DataSet that have the same name, but that differ in case. For example you can have Table1 and table1. In this situation, a reference to one of the tables by name must match the case of the table name exactly, otherwise an exception is thrown. For example, if the DataSet myDS contains tables Table1 and table1, you would reference Table1 by name as myDS.Tables["Table1"], and table1 as myDS.Tables ["table1"]. Attempting to reference either of the tables as myDS.Tables ["TABLE1"] would generate an exception.
            //The case-sensitivity rule does not apply if only one table or relation exists with a particular name. That is, if no other table or relation object in the DataSet matches the name of that particular table or relation object, even by a difference in case, you can reference the object by name using any case and no exception is thrown. For example, if the DataSet has only Table1, you can reference it using myDS.Tables["TABLE1"].
            //The CaseSensitive property of the DataSet does not affect this behavior. The CaseSensitive property

            var ds = new DataSet();

            DataTable dt1 = new DataTable();
            DataTable dt2 = new DataTable();
            DataTable dt3 = new DataTable();
            dt3.TableName = "Table3";
            DataTable dt4 = new DataTable(dt3.TableName.ToUpper());

            // Checking Tables - default value
            //Check default
            Assert.Equal(0, ds.Tables.Count);

            ds.Tables.Add(dt1);
            ds.Tables.Add(dt2);
            ds.Tables.Add(dt3);

            // Checking Tables Count
            Assert.Equal(3, ds.Tables.Count);

            // Checking Tables Value 1
            Assert.Equal(dt1, ds.Tables[0]);

            // Checking Tables Value 2
            Assert.Equal(dt2, ds.Tables[1]);

            // Checking Tables Value 3
            Assert.Equal(dt3, ds.Tables[2]);

            // Checking get table by name.ToUpper
            Assert.Equal(dt3, ds.Tables[dt3.TableName.ToUpper()]);

            // Checking get table by name.ToLower
            Assert.Equal(dt3, ds.Tables[dt3.TableName.ToLower()]);

            // Checking Tables add with name case insensetive
            ds.Tables.Add(dt4); //same name as Table3, but different case
            Assert.Equal(4, ds.Tables.Count);

            // Checking get table by name
            Assert.Equal(dt4, ds.Tables[dt4.TableName]);

            // Checking get table by name with diferent case, ArgumentException
            AssertExtensions.Throws<ArgumentException>(null, () => ds.Tables[dt4.TableName.ToLower()]);
        }

        [Fact]
        public void WriteXml_ByTextWriterXmlWriteMode()
        {
            StringReader sr = null;
            StringWriter sw = null;

            try  // For real
            {
                // ReadXml - DataSetOut

                DataSet oDataset = new DataSet("DataSetOut");
                sw = new StringWriter();
                oDataset.WriteXml(sw, XmlWriteMode.WriteSchema);

                sr = new StringReader(sw.GetStringBuilder().ToString());
                oDataset = new DataSet("DataSetOut");

                oDataset.ReadXml(sr);
                Assert.Equal(0, oDataset.Tables.Count);
            }
            finally
            {
                sw.Close();
            }
        }

        [Fact]
        public void ctor()
        {
            DataSet ds;

            // ctor
            ds = new DataSet();
            Assert.Equal(true, ds != null);
        }

        [Fact]
        public void ctor_ByDataSetName()
        {
            DataSet ds = null;

            // ctor
            ds = new DataSet("NewDataSet");
            Assert.Equal(true, ds != null);

            // ctor - name
            Assert.Equal("NewDataSet", ds.DataSetName);
        }

        [Fact]
        public void extendedProperties()
        {
            var ds = new DataSet();
            PropertyCollection pc;

            pc = ds.ExtendedProperties;

            // Checking ExtendedProperties default
            Assert.Equal(true, pc != null);

            // Checking ExtendedProperties count
            Assert.Equal(0, pc.Count);
        }

        [Fact]
        public void SchemaSerializationModeTest()
        {
            DataSet ds = new DataSet();
            Assert.Equal(SchemaSerializationMode.IncludeSchema, ds.SchemaSerializationMode);
            try
            {
                ds.SchemaSerializationMode = SchemaSerializationMode.ExcludeSchema;
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
                //ok 	
            }
        }

        ///<?xml version="1.0" encoding="utf-16"?>
        ///<xs:schema id="NewDataSet" xmlns="" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:msdata="urn:schemas-microsoft-com:xml-msdata">
        ///	<xs:element name="NewDataSet" msdata:IsDataSet="true">
        ///		<xs:complexType>
        ///			<xs:choice maxOccurs="unbounded">
        ///				<xs:element name="Parent">
        ///					<xs:complexType>
        ///						<xs:sequence>
        ///							<xs:element name="ParentId" type="xs:int" minOccurs="0"/>
        ///							<xs:element name="String1" type="xs:string" minOccurs="0"/>
        ///							<xs:element name="String2" type="xs:string" minOccurs="0"/>
        ///							<xs:element name="ParentDateTime" type="xs:dateTime" minOccurs="0"/>
        ///							<xs:element name="ParentDouble" type="xs:double" minOccurs="0"/>
        ///							<xs:element name="ParentBool" type="xs:boolean" minOccurs="0"/>
        ///						</xs:sequence>
        ///					</xs:complexType>
        ///				</xs:element>
        ///			</xs:choice>
        ///		</xs:complexType>
        ///	</xs:element>
        ///</xs:schema>

        [Fact]
        public void ParentDataTableSchema()
        {
            XmlDocument testedSchema;
            XmlNamespaceManager testedSchemaNamepaces;
            InitParentDataTableSchema(out testedSchema, out testedSchemaNamepaces);

            CheckNode("DataSet name", "/xs:schema/xs:element[@name='NewDataSet']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("Parent datatable name", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element[@name='Parent']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("ParentId column - name", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@name='ParentId']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("String1 column - name", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@name='String1']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("String2 column - name", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@name='String1']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("ParentDateTime column - name", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@name='ParentDateTime']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("ParentDouble column - name", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@name='ParentDouble']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("ParentBool column - name", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@name='ParentBool']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("Int columns", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@type='xs:int']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("string columns", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@type='xs:string']", 2, testedSchema, testedSchemaNamepaces);

            CheckNode("dateTime columns", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@type='xs:dateTime']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("double columns", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@type='xs:double']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("boolean columns", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@type='xs:boolean']", 1, testedSchema, testedSchemaNamepaces);

            CheckNode("minOccurs columns", "/xs:schema/xs:element/xs:complexType/xs:choice/xs:element/xs:complexType/xs:sequence/xs:element[@minOccurs='0']", 6, testedSchema, testedSchemaNamepaces);
        }

        private void InitParentDataTableSchema(out XmlDocument schemaDocInit, out XmlNamespaceManager namespaceManagerToInit)
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());
            string strXML = ds.GetXmlSchema();
            schemaDocInit = new XmlDocument();
            schemaDocInit.LoadXml(strXML);
            namespaceManagerToInit = new XmlNamespaceManager(schemaDocInit.NameTable);
            namespaceManagerToInit.AddNamespace("xs", "http://www.w3.org/2001/XMLSchema");
            namespaceManagerToInit.AddNamespace("msdata", "urn:schemas-microsoft-com:xml-msdata");
        }

        private void CheckNode(string description, string xPath, int expectedNodesCout, XmlDocument schemaDoc, XmlNamespaceManager nm)
        {
            int actualNodeCount = schemaDoc.SelectNodes(xPath, nm).Count;
            Assert.Equal(expectedNodesCout, actualNodeCount);
        }

        [Fact]
        public void WriteXml_Stream()
        {
            {
                var ds = new DataSet();
                string input = "<a><b><c>2</c></b></a>";
                StringReader sr = new StringReader(input);
                XmlTextReader xReader = new XmlTextReader(sr);
                ds.ReadXml(xReader);

                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);
                XmlTextWriter xWriter = new XmlTextWriter(sw);
                ds.WriteXml(xWriter);
                string output = sb.ToString();
                Assert.Equal(input, output);
            }
            {
                var ds = new DataSet();
                string input = "<?xml version=\"1.0\" encoding=\"utf-8\" ?><a><b><c>2</c></b></a>";
                string expectedOutput = "<a><b><c>2</c></b></a>";
                StringReader sr = new StringReader(input);
                XmlTextReader xReader = new XmlTextReader(sr);
                ds.ReadXml(xReader);

                StringBuilder sb = new StringBuilder();
                StringWriter sw = new StringWriter(sb);
                XmlTextWriter xWriter = new XmlTextWriter(sw);
                ds.WriteXml(xWriter);
                string output = sb.ToString();
                Assert.Equal(expectedOutput, output);
            }
            {
                DataSet ds = new DataSet("DSName");
                StringWriter sr = new StringWriter();
                ds.WriteXml(sr);
                Assert.Equal("<DSName />", sr.ToString());
            }
            {
                var ds = new DataSet();
                DataTable dt;

                //Create parent table.
                dt = ds.Tables.Add("ParentTable");
                dt.Columns.Add("ParentTable_Id", typeof(int));
                dt.Columns.Add("ParentTableCol", typeof(int));
                dt.Rows.Add(new object[] { 0, 1 });

                //Create child table.
                dt = ds.Tables.Add("ChildTable");
                dt.Columns.Add("ParentTable_Id", typeof(int));
                dt.Columns.Add("ChildTableCol", typeof(string));
                dt.Rows.Add(new object[] { 0, "aa" });

                //Add a relation between parent and child table.
                ds.Relations.Add("ParentTable_ChildTable", ds.Tables["ParentTable"].Columns["ParentTable_Id"], ds.Tables["ChildTable"].Columns["ParentTable_Id"], true);
                ds.Relations["ParentTable_ChildTable"].Nested = true;

                //Reomve the Parent_Child relation.
                dt = ds.Tables["ChildTable"];
                dt.ParentRelations.Remove("ParentTable_ChildTable");

                //Remove the constraint created automatically to enforce the "ParentTable_ChildTable" relation.
                dt.Constraints.Remove("ParentTable_ChildTable");

                //Remove the child table from the dataset.
                ds.Tables.Remove("ChildTable");

                //Get the xml representation of the dataset.
                StringWriter sr = new StringWriter();
                ds.WriteXml(sr);
                string xml = sr.ToString();

                Assert.Equal(-1, xml.IndexOf("<ChildTable>"));
            }
        }

        [Fact]
        public void WriteXmlSchema_ConstraintNameWithSpaces()
        {
            DataSet ds = new DataSet();
            DataTable table1 = ds.Tables.Add("table1");
            DataTable table2 = ds.Tables.Add("table2");

            table1.Columns.Add("col1", typeof(int));
            table2.Columns.Add("col1", typeof(int));

            table1.Constraints.Add("uc 1", table1.Columns[0], false);
            table2.Constraints.Add("fc 1", table1.Columns[0], table2.Columns[0]);

            StringWriter sw = new StringWriter();

            //should not throw an exception
            ds.WriteXmlSchema(sw);
        }

        [Fact]
        public void ReadWriteXmlSchema_Nested()
        {
            DataSet ds = new DataSet("dataset");
            ds.Tables.Add("table1");
            ds.Tables.Add("table2");
            ds.Tables[0].Columns.Add("col");
            ds.Tables[1].Columns.Add("col");
            ds.Relations.Add("rel", ds.Tables[0].Columns[0], ds.Tables[1].Columns[0], true);
            ds.Relations[0].Nested = true;

            MemoryStream ms = new MemoryStream();
            ds.WriteXmlSchema(ms);

            DataSet ds1 = new DataSet();
            ds1.ReadXmlSchema(new MemoryStream(ms.GetBuffer()));

            // no new relation, and <table>_Id columns, should get created when 
            // Relation.Nested = true
            Assert.Equal(1, ds1.Relations.Count);
            Assert.Equal(1, ds1.Tables[0].Columns.Count);
            Assert.Equal(1, ds1.Tables[1].Columns.Count);
        }

        [Fact]
        public void ReadXmlSchema_Nested()
        {
            //when Relation.Nested = false, and the schema is nested, create new relations on <table>_Id
            //columns.
            DataSet ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<?xml version=""1.0"" standalone=""yes""?>
                <xs:schema id=""dataset"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
                  <xs:element name=""dataset"" msdata:IsDataSet=""true"" msdata:Locale=""en-US"">
                    <xs:complexType>
                      <xs:choice maxOccurs=""unbounded"">
                        <xs:element name=""table1"">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name=""col"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""table1_Id"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""table2"" minOccurs=""0"" maxOccurs=""unbounded"">
                                <xs:complexType>
                                  <xs:sequence>
                                    <xs:element name=""col"" type=""xs:string"" minOccurs=""0"" />
                                    <xs:element name=""table1_Id"" type=""xs:string"" minOccurs=""0"" />
                                  </xs:sequence>
                                </xs:complexType>
                              </xs:element>
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                      </xs:choice>
                    </xs:complexType>
                    <xs:unique name=""Constraint1"">
                      <xs:selector xpath="".//table1"" />
                      <xs:field xpath=""col"" />
                    </xs:unique>
                    <xs:keyref name=""rel"" refer=""Constraint1"" msdata:IsNested=""false"">
                      <xs:selector xpath="".//table2"" />
                      <xs:field xpath=""col"" />
                    </xs:keyref>
                  </xs:element>
                </xs:schema>"));
            Assert.Equal(2, ds.Relations.Count);
            Assert.Equal(3, ds.Tables[0].Columns.Count);
            Assert.Equal(3, ds.Tables[1].Columns.Count);
            Assert.Equal("table1_Id_0", ds.Tables[0].Columns[2].ColumnName);
            Assert.Equal("table1_Id_0", ds.Tables[0].PrimaryKey[0].ColumnName);
        }

        [Fact]
        public void ReadXmlSchema_TableOrder()
        {
            DataSet ds = new DataSet();
            ds.ReadXmlSchema(new StringReader(
                @"<?xml version=""1.0"" standalone=""yes""?>
                <xs:schema id=""items"" xmlns="""" xmlns:xs=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">
                  <xs:element name=""items"" msdata:IsDataSet=""true"" msdata:UseCurrentLocale=""true"">
                    <xs:complexType>
                      <xs:choice minOccurs=""0"" maxOccurs=""unbounded"">
                        <xs:element name=""category"">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name=""id"" type=""xs:string"" />
                              <xs:element name=""visible"" type=""xs:string"" />
                              <xs:element name=""title"" type=""xs:string"" />
                              <xs:element name=""description"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""imageUrl"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""imageAltText"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""parentCategoryId"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""childItemId"" nillable=""true"" minOccurs=""0"" maxOccurs=""unbounded"">
                                <xs:complexType>
                                  <xs:simpleContent msdata:ColumnName=""childItemId_Text"" msdata:Ordinal=""0"">
                                    <xs:extension base=""xs:string"">
                                    </xs:extension>
                                  </xs:simpleContent>
                                </xs:complexType>
                              </xs:element>
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                        <xs:element name=""item"">
                          <xs:complexType>
                            <xs:sequence>
                              <xs:element name=""id"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""visible"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""title"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""description"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""price"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""inStock"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""imageUrl"" type=""xs:string"" minOccurs=""0"" />
                              <xs:element name=""imageAltText"" type=""xs:string"" minOccurs=""0"" />
                            </xs:sequence>
                          </xs:complexType>
                        </xs:element>
                      </xs:choice>
                    </xs:complexType>
                  </xs:element>
                </xs:schema>"));
            Assert.Equal("category", ds.Tables[0].TableName);
            Assert.Equal("childItemId", ds.Tables[1].TableName);
            Assert.Equal("item", ds.Tables[2].TableName);
        }

        [Fact]
        public void ReadXml_Diffgram_MissingSchema()
        {
            DataSet ds = new DataSet();
            ds.Tables.Add("table");
            ds.Tables[0].Columns.Add("col1");
            ds.Tables[0].Columns.Add("col2");

            ds.Tables[0].Rows.Add(new object[] { "a", "b" });
            ds.Tables[0].Rows.Add(new object[] { "a", "b" });

            MemoryStream ms = new MemoryStream();
            ds.WriteXml(ms, XmlWriteMode.DiffGram);

            DataSet ds1 = new DataSet();
            ds1.Tables.Add("table");
            ds1.Tables[0].Columns.Add("col1");

            // When table schema is missing, it shud load up the data
            // for the existing schema
            ds1.ReadXml(new MemoryStream(ms.GetBuffer()), XmlReadMode.DiffGram);

            Assert.Equal(2, ds1.Tables[0].Rows.Count);
            Assert.Equal(1, ds1.Tables[0].Columns.Count);
            Assert.Equal("a", ds1.Tables[0].Rows[0][0]);
            Assert.Equal("a", ds1.Tables[0].Rows[1][0]);
        }

        [Fact]
        public void WriteXml_Morethan2Relations()
        {
            DataSet ds = new DataSet();
            DataTable p1 = ds.Tables.Add("parent1");
            DataTable p2 = ds.Tables.Add("parent2");
            DataTable p3 = ds.Tables.Add("parent3");
            DataTable c1 = ds.Tables.Add("child");

            c1.Columns.Add("col1");
            c1.Columns.Add("col2");
            c1.Columns.Add("col3");
            c1.Columns.Add("col4");

            p1.Columns.Add("col1");
            p2.Columns.Add("col1");
            p3.Columns.Add("col1");

            ds.Relations.Add("rel1", p1.Columns[0], c1.Columns[0], false);
            ds.Relations.Add("rel2", p2.Columns[0], c1.Columns[1], false);
            ds.Relations.Add("rel3", p3.Columns[0], c1.Columns[2], false);
            ds.Relations[2].Nested = true;

            p1.Rows.Add(new object[] { "p1" });
            p2.Rows.Add(new object[] { "p2" });
            p3.Rows.Add(new object[] { "p3" });

            c1.Rows.Add(new object[] { "p1", "p2", "p3", "c1" });

            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            ds.WriteXml(xw);
            string dataset_xml = sw.ToString();
            string child_xml = "<child><col1>p1</col1><col2>p2</col2><col3>p3</col3><col4>c1</col4></child>";
            //the child table data must not be repeated.
            Assert.Equal(dataset_xml.IndexOf(child_xml), dataset_xml.LastIndexOf(child_xml));
        }

        [Fact]
        public void MergeTest_ColumnTypeMismatch()
        {
            DataSet dataSet = new DataSet();
            dataSet.Tables.Add(new DataTable());
            dataSet.Tables[0].Columns.Add(new DataColumn("id", typeof(int)));
            dataSet.Tables[0].Columns.Add(new DataColumn("name", typeof(string)));

            DataSet ds = new DataSet();
            ds.Tables.Add(new DataTable());
            ds.Tables[0].Columns.Add(new DataColumn("id", typeof(string)));

            try
            {
                ds.Merge(dataSet, true, MissingSchemaAction.Add);
                Assert.False(true);
            }
            catch (DataException e) { }

            ds = new DataSet();
            ds.Tables.Add(new DataTable());
            ds.Tables[0].Columns.Add(new DataColumn("id", typeof(string)));

            ds.Merge(dataSet, true, MissingSchemaAction.Ignore);

            Assert.Equal("Table1", ds.Tables[0].TableName);
            Assert.Equal(1, ds.Tables.Count);
            Assert.Equal(1, ds.Tables[0].Columns.Count);
            Assert.Equal(typeof(string), ds.Tables[0].Columns[0].DataType);
        }

        [Fact]
        public void MergeTest_SameDataSet_536194()
        {
            DataSet dataSet = new DataSet("Test");

            DataTable dataTable = new DataTable("Test");
            dataTable.Columns.Add("Test");
            dataTable.Rows.Add("Test");
            dataSet.Tables.Add(dataTable);
            dataSet.Merge(dataTable);
            Assert.Equal(1, dataSet.Tables.Count);
        }

        [Fact]
        public void LoadTest1()
        {
            DataSet ds1 = new DataSet();
            DataSet ds2 = new DataSet();
            DataTable dt1 = new DataTable("T1");
            DataTable dt2 = new DataTable("T2");
            DataTable dt3 = new DataTable("T1");
            DataTable dt4 = new DataTable("T2");
            dt1.Columns.Add("ID", typeof(int));
            dt1.Columns.Add("Name", typeof(string));
            dt2.Columns.Add("EmpNO", typeof(int));
            dt2.Columns.Add("EmpName", typeof(string));

            dt1.Rows.Add(new object[] { 1, "Andrews" });
            dt1.Rows.Add(new object[] { 2, "Mathew" });
            dt1.Rows.Add(new object[] { 3, "Jaccob" });

            dt2.Rows.Add(new object[] { 1, "Arul" });
            dt2.Rows.Add(new object[] { 2, "Jothi" });
            dt2.Rows.Add(new object[] { 3, "Murugan" });

            ds2.Tables.Add(dt1);
            ds2.Tables.Add(dt2);
            ds1.Tables.Add(dt3);
            ds1.Tables.Add(dt4);

            DataTableReader reader = ds2.CreateDataReader();
            //ds1.Load (reader, LoadOption.PreserveChanges, dt3, dt4);
            ds1.Load(reader, LoadOption.OverwriteChanges, dt3, dt4);

            Assert.Equal(ds2.Tables.Count, ds1.Tables.Count);
            int i = 0;
            foreach (DataTable dt in ds1.Tables)
            {
                DataTable dt5 = ds2.Tables[i];
                Assert.Equal(dt5.Rows.Count, dt.Rows.Count);
                int j = 0;
                DataRow row1;
                foreach (DataRow row in dt.Rows)
                {
                    row1 = dt5.Rows[j];
                    for (int k = 0; k < dt.Columns.Count; k++)
                    {
                        Assert.Equal(row1[k], row[k]);
                    }
                    j++;
                }
                i++;
            }
        }
        [Fact]
        public void LoadTest2()
        {
            DataSet ds1 = new DataSet();
            DataSet ds2 = new DataSet();
            DataTable dt1 = new DataTable("T1");
            DataTable dt2 = new DataTable("T2");
            DataTable dt3 = new DataTable("T1");
            DataTable dt4 = new DataTable("T2");
            dt1.Columns.Add("ID", typeof(int));
            dt1.Columns.Add("Name", typeof(string));
            dt2.Columns.Add("EmpNO", typeof(int));
            dt2.Columns.Add("EmpName", typeof(string));

            dt1.Rows.Add(new object[] { 1, "Andrews" });
            dt1.Rows.Add(new object[] { 2, "Mathew" });
            dt1.Rows.Add(new object[] { 3, "Jaccob" });

            dt2.Rows.Add(new object[] { 1, "Arul" });
            dt2.Rows.Add(new object[] { 2, "Jothi" });
            dt2.Rows.Add(new object[] { 3, "Murugan" });

            ds2.Tables.Add(dt1);
            ds2.Tables.Add(dt2);
            ds1.Tables.Add(dt3);
            ds1.Tables.Add(dt4);

            DataTableReader reader = ds2.CreateDataReader();
            //ds1.Load (reader, LoadOption.PreserveChanges, dt3, dt4);
            ds1.Load(reader, LoadOption.OverwriteChanges, dt3, dt4);

            Assert.Equal(ds2.Tables.Count, ds1.Tables.Count);
            int i = 0;
            foreach (DataTable dt in ds1.Tables)
            {
                DataTable dt5 = ds2.Tables[i];
                Assert.Equal(dt5.Rows.Count, dt.Rows.Count);
                int j = 0;
                DataRow row1;
                foreach (DataRow row in dt.Rows)
                {
                    row1 = dt5.Rows[j];
                    for (int k = 0; k < dt.Columns.Count; k++)
                    {
                        Assert.Equal(row1[k], row[k]);
                    }
                    j++;
                }
                i++;
            }
        }
        private void AssertDataTableValues(DataTable dt)
        {
            Assert.Equal("data1", dt.Rows[0]["_ID"]);
            Assert.Equal("data2", dt.Rows[0]["#ID"]);
            Assert.Equal("data3", dt.Rows[0]["%ID"]);
            Assert.Equal("data4", dt.Rows[0]["$ID"]);
            Assert.Equal("data5", dt.Rows[0][":ID"]);
            Assert.Equal("data6", dt.Rows[0][".ID"]);
            Assert.Equal("data7", dt.Rows[0]["ID"]);
            Assert.Equal("data8", dt.Rows[0]["*ID"]);
            Assert.Equal("data8", dt.Rows[0]["+ID"]);
            Assert.Equal("data8", dt.Rows[0]["-ID"]);
            Assert.Equal("data8", dt.Rows[0]["~ID"]);
            Assert.Equal("data8", dt.Rows[0]["@ID"]);
            Assert.Equal("data8", dt.Rows[0]["&ID"]);
        }

        [Fact]
        public void Bug537229_BinFormatSerializer_Test()
        {
            DataSet ds = new DataSet();
            DataTable dt = new DataTable();
            ds.Tables.Add(dt);
            dt.Columns.Add("_ID", typeof(string));
            dt.Columns.Add("#ID", typeof(string));
            dt.Columns.Add("%ID", typeof(string));
            dt.Columns.Add("$ID", typeof(string));
            dt.Columns.Add(":ID", typeof(string));
            dt.Columns.Add(".ID", typeof(string));
            dt.Columns.Add("ID", typeof(string));
            dt.Columns.Add("*ID", typeof(string));
            dt.Columns.Add("+ID", typeof(string));
            dt.Columns.Add("-ID", typeof(string));
            dt.Columns.Add("~ID", typeof(string));
            dt.Columns.Add("@ID", typeof(string));
            dt.Columns.Add("&ID", typeof(string));
            DataRow row = dt.NewRow();
            row["#ID"] = "data2";
            row["%ID"] = "data3";
            row["$ID"] = "data4";
            row["ID"] = "data7";
            row[":ID"] = "data5";
            row[".ID"] = "data6";
            row["_ID"] = "data1";
            row["*ID"] = "data8";
            row["+ID"] = "data8";
            row["-ID"] = "data8";
            row["~ID"] = "data8";
            row["@ID"] = "data8";
            row["&ID"] = "data8";
            dt.Rows.Add(row);

            AssertDataTableValues(dt);

            DataTable vdt = BinaryFormatterHelpers.Clone(dt);
            AssertDataTableValues(vdt);
        }
    }
}
