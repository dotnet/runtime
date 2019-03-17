// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2004 Mainsoft Co.
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

using System.Collections;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;


using Xunit;

namespace System.Data.Tests
{
    public class DataRowTest2
    {
        private bool _rowChanged;
        private ArrayList _eventsFired;

        public DataRowTest2()
        {
            _rowChanged = false;
            _eventsFired = new ArrayList();
        }

        [Fact]
        public void AcceptChanges()
        {
            DataTable myTable = new DataTable("myTable");
            DataRow myRow;
            myRow = myTable.NewRow();
            myTable.Rows.Add(myRow);

            // DataRow AcceptChanges
            // DataRowState.Added -> DataRowState.Unchanged 
            myTable.AcceptChanges();
            Assert.Equal(DataRowState.Unchanged, myRow.RowState);
        }

        [Fact]
        public void CancelEdit()
        {
            DataTable myTable = new DataTable("myTable");
            DataColumn dc = new DataColumn("Id", typeof(int));
            dc.Unique = true;
            myTable.Columns.Add(dc);
            myTable.Rows.Add(new object[] { 1 });
            myTable.Rows.Add(new object[] { 2 });
            myTable.Rows.Add(new object[] { 3 });

            DataRow myRow = myTable.Rows[0];
            myRow.BeginEdit();
            myRow[0] = 7;
            myRow.CancelEdit();

            // DataRow CancelEdit
            Assert.Equal(true, (int)myRow[0] == 1);
        }

        [Fact]
        public void ClearErrors()
        {
            DataTable dt = new DataTable("myTable");
            DataRow dr = dt.NewRow();
            dr.RowError = "err";

            // DataRow ClearErrors
            Assert.Equal(true, dr.HasErrors);

            // DataRow ClearErrors
            dr.ClearErrors();
            Assert.Equal(false, dr.HasErrors);
        }

        [Fact]
        public void Delete()
        {
            DataTable myTable = new DataTable("myTable");
            DataColumn dc = new DataColumn("Id", typeof(int));
            dc.Unique = true;
            myTable.Columns.Add(dc);
            myTable.Rows.Add(new object[] { 1 });
            myTable.Rows.Add(new object[] { 2 });
            myTable.Rows.Add(new object[] { 3 });
            myTable.AcceptChanges();

            DataRow myRow = myTable.Rows[0];
            myRow.Delete();

            // Delete1
            Assert.Equal(DataRowState.Deleted, myRow.RowState);

            // Delete2
            myTable.AcceptChanges();
            Assert.Equal(DataRowState.Detached, myRow.RowState);
        }

        [Fact]
        public void EndEdit()
        {
            DataTable myTable = new DataTable("myTable");
            DataColumn dc = new DataColumn("Id", typeof(int));
            dc.Unique = true;
            myTable.Columns.Add(dc);
            myTable.Rows.Add(new object[] { 1 });
            myTable.Rows.Add(new object[] { 2 });
            myTable.Rows.Add(new object[] { 3 });

            DataRow myRow = myTable.Rows[0];

            int iProposed;
            //After calling the DataRow object's BeginEdit method, if you change the value, the Current and Proposed values become available
            myRow.BeginEdit();
            myRow[0] = 7;
            iProposed = (int)myRow[0, DataRowVersion.Proposed];
            myRow.EndEdit();

            // EndEdit
            Assert.Equal(iProposed, (int)myRow[0, DataRowVersion.Current]);
        }

        [Fact]
        public void Equals()
        {
            DataTable myTable = new DataTable("myTable");
            DataRow dr1, dr2;
            dr1 = myTable.NewRow();
            dr2 = myTable.NewRow();

            // not equals
            Assert.Equal(false, dr1.Equals(dr2));

            dr1 = dr2;
            // equals
            Assert.Equal(true, dr1.Equals(dr2));
        }

        [Fact]
        public void GetChildRows_ByDataRealtion()
        {
            DataRow dr;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();

            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();

            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            dr = dtParent.Rows[0];

            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + dr["ParentId"]);
            //Get Result
            drArrResult = dr.GetChildRows(dRel);

            // GetChildRows_D
            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void GetChildRows_ByDataRealtionDataRowVersion()
        {
            DataRow drParent;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);

            drParent = dtParent.Rows[0];

            // Teting: DateTime.Now.ToShortTimeString()
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            //Get Result DataRowVersion.Current
            drArrResult = drParent.GetChildRows(dRel, DataRowVersion.Current);
            Assert.Equal(drArrExcepted, drArrResult);

            // Teting: DataRow.GetParentRows_D_D
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.OriginalRows);
            //Get Result DataRowVersion.Current
            drArrResult = drParent.GetChildRows(dRel, DataRowVersion.Original);
            Assert.Equal(drArrExcepted, drArrResult);

            // Teting: DataRow.GetParentRows_D_D
            //Get Excepted result, in this case Current = Default
            drArrExcepted = dtChild.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            //Get Result DataRowVersion.Current
            drArrResult = drParent.GetChildRows(dRel, DataRowVersion.Default);
            Assert.Equal(drArrExcepted, drArrResult);

            // Teting: DataRow.GetParentRows_D_D
            drParent.BeginEdit();
            drParent["String1"] = "Value";
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            //Get Result DataRowVersion.Current
            drArrResult = drParent.GetChildRows(dRel, DataRowVersion.Proposed);
            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void GetChildRows_ByName()
        {
            DataRow dr;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();

            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();

            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            dr = dtParent.Rows[0];

            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + dr["ParentId"]);
            //Get Result
            drArrResult = dr.GetChildRows("Parent-Child");

            // GetChildRows_S
            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void GetChildRows_ByNameDataRowVersion()
        {
            DataRow drParent;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);

            drParent = dtParent.Rows[0];

            // GetChildRows_SD 1
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            //Get Result DataRowVersion.Current
            drArrResult = drParent.GetChildRows("Parent-Child", DataRowVersion.Current);
            Assert.Equal(drArrExcepted, drArrResult);

            // GetChildRows_SD 2
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.OriginalRows);
            //Get Result DataRowVersion.Current
            drArrResult = drParent.GetChildRows("Parent-Child", DataRowVersion.Original);
            Assert.Equal(drArrExcepted, drArrResult);

            // GetParentRows_SD 3
            //Get Excepted result, in this case Current = Default
            drArrExcepted = dtChild.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            //Get Result DataRowVersion.Current
            drArrResult = drParent.GetChildRows("Parent-Child", DataRowVersion.Default);
            Assert.Equal(drArrExcepted, drArrResult);

            // GetParentRows_SD 4
            drParent.BeginEdit();
            drParent["String1"] = "Value";
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            //Get Result DataRowVersion.Current
            drArrResult = drParent.GetChildRows("Parent-Child", DataRowVersion.Proposed);
            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void GetColumnError_ByIndex()
        {
            string sColErr = "Error!";
            DataTable dt = new DataTable("myTable");
            DataColumn dc = new DataColumn("Column1");
            dt.Columns.Add(dc);
            DataRow dr = dt.NewRow();

            // GetColumnError 1
            Assert.Equal(string.Empty, dr.GetColumnError(0));

            dr.SetColumnError(0, sColErr);

            // GetColumnError 2
            Assert.Equal(sColErr, dr.GetColumnError(0));
        }

        [Fact]
        public void GetColumnError_ByName()
        {
            string sColErr = "Error!";
            DataTable dt = new DataTable("myTable");
            DataColumn dc = new DataColumn("Column1");
            dt.Columns.Add(dc);
            DataRow dr = dt.NewRow();

            // GetColumnError 1
            Assert.Equal(string.Empty, dr.GetColumnError("Column1"));

            dr.SetColumnError("Column1", sColErr);

            // GetColumnError 2
            Assert.Equal(sColErr, dr.GetColumnError("Column1"));
        }

        [Fact]
        public void GetColumnsInError()
        {
            string sColErr = "Error!";
            DataColumn[] dcArr;
            DataTable dt = new DataTable("myTable");
            //init some columns
            dt.Columns.Add(new DataColumn());
            dt.Columns.Add(new DataColumn());
            dt.Columns.Add(new DataColumn());
            dt.Columns.Add(new DataColumn());
            dt.Columns.Add(new DataColumn());

            //init some rows
            dt.Rows.Add(new object[] { });
            dt.Rows.Add(new object[] { });
            dt.Rows.Add(new object[] { });

            DataRow dr = dt.Rows[1];

            dcArr = dr.GetColumnsInError();

            // GetColumnsInError 1
            Assert.Equal(0, dcArr.Length);

            dr.SetColumnError(0, sColErr);
            dr.SetColumnError(2, sColErr);
            dr.SetColumnError(4, sColErr);

            dcArr = dr.GetColumnsInError();

            // GetColumnsInError 2
            Assert.Equal(3, dcArr.Length);

            //check that the right columns taken
            // GetColumnsInError 3
            Assert.Equal(dt.Columns[0], dcArr[0]);

            // GetColumnsInError 4
            Assert.Equal(dt.Columns[2], dcArr[1]);

            // GetColumnsInError 5
            Assert.Equal(dt.Columns[4], dcArr[2]);
        }

        [Fact]
        public new void GetHashCode()
        {
            int iHashCode;
            DataRow dr;
            DataTable dt = new DataTable();
            dr = dt.NewRow();

            iHashCode = dr.GetHashCode();
            for (int i = 0; i < 10; i++)
            {   //must return the same value each time
                // GetHashCode #" + i
                Assert.Equal(dr.GetHashCode(), iHashCode);
            }
        }

        [Fact]
        public void GetParentRow_ByDataRelation()
        {
            DataRow drExcepted, drResult, drChild;
            DataTable dtChild, dtParent;
            var ds = new DataSet();

            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();

            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);

            //Excepted result
            drExcepted = dtParent.Rows[0];

            //Get Result
            drChild = dtChild.Select("ParentId=" + drExcepted["ParentId"])[0];
            drResult = drChild.GetParentRow(dRel);

            // GetParentRow_D
            Assert.Equal(drExcepted.ItemArray, drResult.ItemArray);
        }

        [Fact]
        public void GetParentRow_ByDataRelationDataRowVersion()
        {
            DataRow drParent, drChild;
            DataRow drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);

            drParent = dtParent.Rows[0];
            drChild = dtChild.Select("ParentId=" + drParent["ParentId"])[0];

            // GetParentRow_DD 1
            //Get Excepted result
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow(dRel, DataRowVersion.Current);
            Assert.Equal(drArrExcepted.ItemArray, drArrResult.ItemArray);

            // GetParentRow_DD 2
            //Get Excepted result
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow(dRel, DataRowVersion.Original);
            Assert.Equal(drArrExcepted.ItemArray, drArrResult.ItemArray);

            // GetParentRow_DD 3
            //Get Excepted result, in this case Current = Default
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow(dRel, DataRowVersion.Default);
            Assert.Equal(drArrExcepted.ItemArray, drArrResult.ItemArray);

            // GetParentRow_DD 4
            drChild.BeginEdit();
            drChild["String1"] = "Value";
            //Get Excepted result
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow(dRel, DataRowVersion.Proposed);
            Assert.Equal(drArrExcepted.ItemArray, drArrResult.ItemArray);
        }

        [Fact]
        public void GetParentRow_ByName()
        {
            DataRow drExcepted, drResult, drChild;
            DataTable dtChild, dtParent;
            var ds = new DataSet();

            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();

            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);

            //Excepted result
            drExcepted = dtParent.Rows[0];

            //Get Result
            drChild = dtChild.Select("ParentId=" + drExcepted["ParentId"])[0];
            drResult = drChild.GetParentRow("Parent-Child");

            // GetParentRow_S
            Assert.Equal(drExcepted.ItemArray, drResult.ItemArray);
        }

        [Fact]
        public void GetParentRow_ByNameDataRowVersion()
        {
            DataRow drParent, drChild;
            DataRow drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);

            drParent = dtParent.Rows[0];
            drChild = dtChild.Select("ParentId=" + drParent["ParentId"])[0];

            // GetParentRow_SD 1
            //Get Excepted result
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow("Parent-Child", DataRowVersion.Current);
            Assert.Equal(drArrExcepted.ItemArray, drArrResult.ItemArray);

            // GetParentRow_SD 2
            //Get Excepted result
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow("Parent-Child", DataRowVersion.Original);
            Assert.Equal(drArrExcepted.ItemArray, drArrResult.ItemArray);

            // GetParentRow_SD 3
            //Get Excepted result, in this case Current = Default
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow("Parent-Child", DataRowVersion.Default);
            Assert.Equal(drArrExcepted.ItemArray, drArrResult.ItemArray);

            // GetParentRow_SD 4
            drChild.BeginEdit();
            drChild["String1"] = "Value";
            //Get Excepted result
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow("Parent-Child", DataRowVersion.Proposed);
            Assert.Equal(drArrExcepted.ItemArray, drArrResult.ItemArray);
        }

        [Fact]
        public void GetParentRows_ByDataRelation()
        {
            DataRow dr;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();

            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();

            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            dr = dtParent.Rows[0];

            //Duplicate several rows in order to create Many to Many relation
            dtParent.ImportRow(dr);
            dtParent.ImportRow(dr);
            dtParent.ImportRow(dr);

            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"], false);
            ds.Relations.Add(dRel);
            //Get Excepted result
            drArrExcepted = dtParent.Select("ParentId=" + dr["ParentId"]);
            dr = dtChild.Select("ParentId=" + dr["ParentId"])[0];
            //Get Result
            drArrResult = dr.GetParentRows(dRel);

            // GetParentRows_D
            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void GetParentRows_ByName()
        {
            DataRow dr;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();

            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();

            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            dr = dtParent.Rows[0];

            //Duplicate several rows in order to create Many to Many relation
            dtParent.ImportRow(dr);
            dtParent.ImportRow(dr);
            dtParent.ImportRow(dr);

            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"], false);
            ds.Relations.Add(dRel);
            //Get Excepted result
            drArrExcepted = dtParent.Select("ParentId=" + dr["ParentId"]);
            dr = dtChild.Select("ParentId=" + dr["ParentId"])[0];
            //Get Result
            drArrResult = dr.GetParentRows("Parent-Child");

            // GetParentRows_S
            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void GetParentRows_ByNameDataRowVersion()
        {
            DataRow drParent, drChild;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"], false);
            ds.Relations.Add(dRel);

            //Create several copies of the first row
            drParent = dtParent.Rows[0];    //row[0] has versions: Default,Current,Original
            dtParent.ImportRow(drParent);   //row[1] has versions: Default,Current,Original
            dtParent.ImportRow(drParent);   //row[2] has versions: Default,Current,Original
            dtParent.ImportRow(drParent);   //row[3] has versions: Default,Current,Original
            dtParent.ImportRow(drParent);   //row[4] has versions: Default,Current,Original
            dtParent.ImportRow(drParent);   //row[5] has versions: Default,Current,Original
            dtParent.AcceptChanges();

            //Get the first child row for drParent
            drChild = dtChild.Select("ParentId=" + drParent["ParentId"])[0];

            DataRow[] drTemp = dtParent.Select("ParentId=" + drParent["ParentId"]);
            drTemp[0].BeginEdit();
            drTemp[0]["String1"] = "NewValue"; //row now has versions: Proposed,Current,Original,Default
            drTemp[1].BeginEdit();
            drTemp[1]["String1"] = "NewValue"; //row now has versions: Proposed,Current,Original,Default

            // Check DataRowVersion.Current
            //Check DataRowVersion.Current 
            drArrExcepted = dtParent.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            drArrResult = drChild.GetParentRows("Parent-Child", DataRowVersion.Current);
            Assert.Equal(drArrExcepted, drArrResult);

            //Check DataRowVersion.Current 
            // Teting: DataRow.GetParentRows_D_D ,DataRowVersion.Original
            drArrExcepted = dtParent.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.OriginalRows);
            drArrResult = drChild.GetParentRows("Parent-Child", DataRowVersion.Original);
            Assert.Equal(drArrExcepted, drArrResult);

            //Check DataRowVersion.Default
            // Teting: DataRow.GetParentRows_D_D ,DataRowVersion.Default
            drArrExcepted = dtParent.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            drArrResult = drChild.GetParentRows("Parent-Child", DataRowVersion.Default);
            Assert.Equal(drArrExcepted, drArrResult);

            /* .NET don't work as expected
                //Check DataRowVersion.Proposed
                // Teting: DataRow.GetParentRows_D_D ,DataRowVersion.Proposed
                drArrExcepted = dtParent.Select("ParentId=" + drParent["ParentId"],"",DataViewRowState.ModifiedCurrent);
                //drArrExcepted = dtParent.Select("ParentId=" + drParent["ParentId"],"",DataViewRowState.ModifiedOriginal );

                drArrResult = drChild.GetParentRows("Parent-Child",DataRowVersion.Proposed  );
                Assert.Equal(drArrExcepted,  drArrResult);
            */
        }

        private void CheckRowVersion(DataRow dr)
        {
            if (dr.HasVersion(DataRowVersion.Current)) Console.WriteLine("Has " + DataRowVersion.Current.ToString());
            if (dr.HasVersion(DataRowVersion.Default)) Console.WriteLine("Has " + DataRowVersion.Default.ToString());
            if (dr.HasVersion(DataRowVersion.Original)) Console.WriteLine("Has " + DataRowVersion.Original.ToString());
            if (dr.HasVersion(DataRowVersion.Proposed)) Console.WriteLine("Has " + DataRowVersion.Proposed.ToString());
        }

        [Fact]
        public new void GetType()
        {
            Type myType;
            DataTable dt = new DataTable();
            DataRow dr = dt.NewRow();
            myType = typeof(DataRow);

            // GetType
            Assert.Equal(typeof(DataRow), myType);
        }

        [Fact]
        public void HasErrors()
        {
            DataTable dt = new DataTable("myTable");
            DataRow dr = dt.NewRow();

            // HasErrors (default)
            Assert.Equal(false, dr.HasErrors);

            dr.RowError = "Err";

            // HasErrors (set/get)
            Assert.Equal(true, dr.HasErrors);
        }

        [Fact]
        public void HasErrorsWithNullError()
        {
            DataTable dt = new DataTable("myTable");
            DataRow dr = dt.NewRow();

            // HasErrors (default)
            Assert.Equal(false, dr.HasErrors);

            dr.RowError = null;

            // HasErrors (set/get)
            Assert.Equal(string.Empty, dr.RowError);
            Assert.Equal(false, dr.HasErrors);
        }

        [Fact]
        public void HasVersion_ByDataRowVersion()
        {
            DataTable t = new DataTable("atable");
            t.Columns.Add("id", typeof(int));
            t.Columns.Add("name", typeof(string));
            t.Columns[0].DefaultValue = 1;
            t.Columns[1].DefaultValue = "something";

            // row r is detached
            DataRow r = t.NewRow();

            // HasVersion Test #10
            Assert.Equal(false, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #11
            Assert.Equal(false, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #12
            Assert.Equal(true, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #13
            Assert.Equal(true, r.HasVersion(DataRowVersion.Proposed));

            r[0] = 4;
            r[1] = "four";

            // HasVersion Test #20
            Assert.Equal(false, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #21
            Assert.Equal(false, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #22
            Assert.Equal(true, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #23
            Assert.Equal(true, r.HasVersion(DataRowVersion.Proposed));

            t.Rows.Add(r);
            // now it is "added"

            // HasVersion Test #30
            Assert.Equal(true, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #31
            Assert.Equal(false, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #32
            Assert.Equal(true, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #33
            Assert.Equal(false, r.HasVersion(DataRowVersion.Proposed));

            t.AcceptChanges();
            // now it is "unchanged"

            // HasVersion Test #40
            Assert.Equal(true, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #41
            Assert.Equal(true, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #42
            Assert.Equal(true, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #43
            Assert.Equal(false, r.HasVersion(DataRowVersion.Proposed));

            r.BeginEdit();
            r[1] = "newvalue";

            // HasVersion Test #50
            Assert.Equal(true, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #51
            Assert.Equal(true, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #52
            Assert.Equal(true, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #53
            Assert.Equal(true, r.HasVersion(DataRowVersion.Proposed));

            r.EndEdit();
            // now it is "modified"
            // HasVersion Test #60
            Assert.Equal(true, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #61
            Assert.Equal(true, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #62
            Assert.Equal(true, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #63
            Assert.Equal(false, r.HasVersion(DataRowVersion.Proposed));

            // this or t.AcceptChanges
            r.AcceptChanges();
            // now it is "unchanged" again
            // HasVersion Test #70
            Assert.Equal(true, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #71
            Assert.Equal(true, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #72
            Assert.Equal(true, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #73
            Assert.Equal(false, r.HasVersion(DataRowVersion.Proposed));

            r.Delete();
            // now it is "deleted"

            // HasVersion Test #80
            Assert.Equal(false, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #81
            Assert.Equal(true, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #82
            Assert.Equal(false, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #83
            Assert.Equal(false, r.HasVersion(DataRowVersion.Proposed));

            r.AcceptChanges();
            // back to detached
            // HasVersion Test #90
            Assert.Equal(false, r.HasVersion(DataRowVersion.Current));

            // HasVersion Test #91
            Assert.Equal(false, r.HasVersion(DataRowVersion.Original));

            // HasVersion Test #92
            Assert.Equal(false, r.HasVersion(DataRowVersion.Default));

            // HasVersion Test #93
            Assert.Equal(false, r.HasVersion(DataRowVersion.Proposed));
        }

        [Fact] // Object this [DataColumn]
        public void Indexer1()
        {
            EventInfo evt;
            DataColumnChangeEventArgs colChangeArgs;

            DataTable dt = new DataTable();
            dt.ColumnChanged += new DataColumnChangeEventHandler(ColumnChanged);
            dt.ColumnChanging += new DataColumnChangeEventHandler(ColumnChanging);

            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");
            Address addressB = new Address("Y", 4);
            Person personC = new Person("Jackson");
            Address addressC = new Address("Z", 3);

            dt.Rows.Add(new object[] { addressA, personA });
            dt.Rows.Add(new object[] { addressB, personB });
            DataRow dr;

            dr = dt.Rows[0];
            Assert.Equal(addressA, dr[dc0]);
            Assert.Same(personA, dr[dc1]);

            dr = dt.Rows[1];
            Assert.Equal(addressB, dr[dc0]);
            Assert.Same(personB, dr[dc1]);

            dr = dt.Rows[0];
            Assert.Equal(0, _eventsFired.Count);
            dr[dc0] = addressC;
            Assert.Equal(2, _eventsFired.Count);
            Assert.Equal(addressC, dr[dc0]);
            Assert.Same(personA, dr[dc1]);

            dr = dt.Rows[1];
            dr.BeginEdit();
            Assert.Equal(2, _eventsFired.Count);
            dr[dc1] = personC;
            Assert.Equal(4, _eventsFired.Count);
            Assert.Equal(addressB, dr[dc0]);
            Assert.Same(personC, dr[dc1]);
            dr.EndEdit();
            Assert.Equal(4, _eventsFired.Count);
            Assert.Equal(addressB, dr[dc0]);
            Assert.Same(personC, dr[dc1]);

            dr = dt.Rows[0];
            dr.BeginEdit();
            Assert.Equal(4, _eventsFired.Count);
            dr[dc0] = addressB;
            Assert.Equal(6, _eventsFired.Count);
            Assert.Equal(addressB, dr[dc0]);
            Assert.Same(personA, dr[dc1]);
            dr.CancelEdit();
            Assert.Equal(6, _eventsFired.Count);
            Assert.Equal(addressC, dr[dc0]);
            Assert.Same(personA, dr[dc1]);

            evt = (EventInfo)_eventsFired[0];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc0, colChangeArgs.Column);
            Assert.Equal(addressC, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[1];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc0, colChangeArgs.Column);
            Assert.Equal(addressC, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[2];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc1, colChangeArgs.Column);
            Assert.Equal(personC, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[1], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[3];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc1, colChangeArgs.Column);
            Assert.Equal(personC, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[1], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[4];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc0, colChangeArgs.Column);
            Assert.Equal(addressB, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[5];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc0, colChangeArgs.Column);
            Assert.Equal(addressB, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);
        }

        [Fact] // Object this [DataColumn]
        public void Indexer1_Column_NotInTable()
        {
            EventInfo evt;
            DataColumnChangeEventArgs colChangeArgs;

            DataTable dtA = new DataTable("TableA");
            dtA.ColumnChanged += new DataColumnChangeEventHandler(ColumnChanged);
            dtA.ColumnChanging += new DataColumnChangeEventHandler(ColumnChanging);
            DataColumn dcA1 = new DataColumn("Col0", typeof(Address));
            dtA.Columns.Add(dcA1);
            DataColumn dcA2 = new DataColumn("Col1", typeof(Person));
            dtA.Columns.Add(dcA2);

            DataTable dtB = new DataTable("TableB");
            dtB.ColumnChanged += new DataColumnChangeEventHandler(ColumnChanged);
            dtB.ColumnChanging += new DataColumnChangeEventHandler(ColumnChanging);
            DataColumn dcB1 = new DataColumn("Col0", typeof(Address));
            dtB.Columns.Add(dcB1);
            DataColumn dcB2 = new DataColumn("Col1", typeof(Person));
            dtB.Columns.Add(dcB2);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);

            dtA.Rows.Add(new object[] { addressA, personA });
            DataRow dr = dtA.Rows[0];

            try
            {
                object value = dr[dcB1];
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Column 'Col0' does not belong to table TableA
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "Col0" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "TableA" + @"\b", ex.Message);
            }

            try
            {
                object value = dr[new DataColumn("ZZZ")];
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Column 'Col0' does not belong to table TableA
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "ZZZ" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "TableA" + @"\b", ex.Message);
            }

            dtA.Columns.Remove(dcA2);

            try
            {
                object value = dr[dcA2];
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Column 'Col0' does not belong to table TableA
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "Col1" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "TableA" + @"\b", ex.Message);
            }
        }

        [Fact] // Object this [DataColumn]
        public void Indexer1_Column_Null()
        {
            EventInfo evt;
            DataColumnChangeEventArgs colChangeArgs;

            DataTable dt = new DataTable();
            dt.ColumnChanged += new DataColumnChangeEventHandler(ColumnChanged);
            dt.ColumnChanging += new DataColumnChangeEventHandler(ColumnChanging);
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");

            dt.Rows.Add(new object[] { addressA, personA });
            DataRow dr = dt.Rows[0];

            try
            {
                object value = dr[(DataColumn)null];
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Equal("column", ex.ParamName);
            }

            try
            {
                dr[(DataColumn)null] = personB;
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Equal("column", ex.ParamName);
            }

            Assert.Equal(0, _eventsFired.Count);
        }

        [Fact] // Object this [DataColumn]
        public void Indexer1_Value_Null()
        {
            EventInfo evt;
            DataColumnChangeEventArgs colChangeArgs;

            DataTable dt = new DataTable();
            dt.ColumnChanged += new DataColumnChangeEventHandler(ColumnChanged);
            dt.ColumnChanging += new DataColumnChangeEventHandler(ColumnChanging);
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);
            DataColumn dc2 = new DataColumn("Col2", typeof(string));
            dt.Columns.Add(dc2);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            string countryA = "U.S.";
            Person personB = new Person("Chris");
            Address addressB = new Address("Y", 4);
            string countryB = "Canada";
            Person personC = new Person("Jackson");
            Address addressC = new Address("Z", 3);

            dt.Rows.Add(new object[] { addressA, personA, countryA });
            dt.Rows.Add(new object[] { addressB, personB, countryB });

            DataRow dr = dt.Rows[0];

            try
            {
                dr[dc0] = null;
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Cannot set Column 'Col0' to be null.
                // Please use DBNull instead
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "Col0" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "DBNull" + @"\b", ex.Message);
            }

            Assert.Equal(1, _eventsFired.Count);
            Assert.Equal(addressA, dr[dc0]);
            Assert.False(dr.IsNull(dc0));
            Assert.Same(personA, dr[dc1]);
            Assert.False(dr.IsNull(dc1));
            Assert.Equal(1, _eventsFired.Count);

            dr[dc1] = null;

            Assert.Equal(3, _eventsFired.Count);
            Assert.Equal(addressA, dr[dc0]);
            Assert.False(dr.IsNull(dc0));
            Assert.Same(DBNull.Value, dr[dc1]);
            Assert.True(dr.IsNull(dc1));
            Assert.Equal(3, _eventsFired.Count);

            dr[dc0] = DBNull.Value;
            Assert.Equal(5, _eventsFired.Count);
            Assert.Same(DBNull.Value, dr[dc0]);
            Assert.True(dr.IsNull(dc0));
            Assert.Same(DBNull.Value, dr[dc1]);
            Assert.True(dr.IsNull(dc1));
            Assert.Equal(5, _eventsFired.Count);

            dr.BeginEdit();
            dr[dc1] = personC;
            Assert.Equal(7, _eventsFired.Count);
            Assert.Same(DBNull.Value, dr[dc0]);
            Assert.True(dr.IsNull(dc0));
            Assert.Equal(personC, dr[dc1]);
            Assert.False(dr.IsNull(dc1));
            dr.EndEdit();
            Assert.Same(DBNull.Value, dr[dc0]);
            Assert.True(dr.IsNull(dc0));
            Assert.Equal(personC, dr[dc1]);
            Assert.False(dr.IsNull(dc1));
            Assert.Equal(7, _eventsFired.Count);

            dr[dc1] = DBNull.Value;
            Assert.Equal(9, _eventsFired.Count);
            Assert.Same(DBNull.Value, dr[dc0]);
            Assert.True(dr.IsNull(dc0));
            Assert.Same(DBNull.Value, dr[dc1]);
            Assert.True(dr.IsNull(dc1));
            Assert.Equal(9, _eventsFired.Count);

            dr[dc2] = null;
            Assert.Equal(11, _eventsFired.Count);
            Assert.Same(DBNull.Value, dr[dc0]);
            Assert.True(dr.IsNull(dc0));
            Assert.Same(DBNull.Value, dr[dc1]);
            Assert.True(dr.IsNull(dc1));
            Assert.Same(DBNull.Value, dr[dc2]);
            Assert.True(dr.IsNull(dc2));
            Assert.Equal(11, _eventsFired.Count);

            dr[dc2] = DBNull.Value;
            Assert.Equal(13, _eventsFired.Count);
            Assert.Same(DBNull.Value, dr[dc0]);
            Assert.True(dr.IsNull(dc0));
            Assert.Same(DBNull.Value, dr[dc1]);
            Assert.True(dr.IsNull(dc1));
            Assert.Same(DBNull.Value, dr[dc2]);
            Assert.True(dr.IsNull(dc2));
            Assert.Equal(13, _eventsFired.Count);

            int index = 0;

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc0, colChangeArgs.Column);
            Assert.Null(colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc1, colChangeArgs.Column);
            Assert.Null(colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);


            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc1, colChangeArgs.Column);
            Assert.Null(colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc0, colChangeArgs.Column);
            Assert.Same(DBNull.Value, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc0, colChangeArgs.Column);
            Assert.Same(DBNull.Value, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc1, colChangeArgs.Column);
            Assert.Same(personC, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc1, colChangeArgs.Column);
            Assert.Same(personC, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc1, colChangeArgs.Column);
            Assert.Same(DBNull.Value, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc1, colChangeArgs.Column);
            Assert.Same(DBNull.Value, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc2, colChangeArgs.Column);
            Assert.Null(colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc2, colChangeArgs.Column);
            Assert.Null(colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanging", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc2, colChangeArgs.Column);
            Assert.Same(DBNull.Value, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);

            evt = (EventInfo)_eventsFired[index++];
            Assert.Equal("ColumnChanged", evt.Name);
            colChangeArgs = (DataColumnChangeEventArgs)evt.Args;
            Assert.Same(dc2, colChangeArgs.Column);
            Assert.Same(DBNull.Value, colChangeArgs.ProposedValue);
            Assert.Same(dt.Rows[0], colChangeArgs.Row);
        }

        [Fact] // Object this [Int32]
        public void Indexer2()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");
            Address addressB = new Address("Y", 4);
            Person personC = new Person("Jackson");
            Address addressC = new Address("Z", 3);

            dt.Rows.Add(new object[] { addressA, personA });
            dt.Rows.Add(new object[] { addressB, personB });
            DataRow dr;

            dr = dt.Rows[0];
            Assert.Equal(addressA, dr[0]);
            Assert.Same(personA, dr[1]);

            dr = dt.Rows[1];
            Assert.Equal(addressB, dr[0]);
            Assert.Same(personB, dr[1]);

            dr = dt.Rows[0];
            dr[0] = addressC;
            Assert.Equal(addressC, dr[0]);
            Assert.Same(personA, dr[1]);

            dr = dt.Rows[1];
            dr.BeginEdit();
            dr[1] = personC;
            Assert.Equal(addressB, dr[0]);
            Assert.Same(personC, dr[1]);
            dr.EndEdit();
            Assert.Equal(addressB, dr[0]);
            Assert.Same(personC, dr[1]);

            dr = dt.Rows[0];
            dr.BeginEdit();
            dr[0] = addressB;
            Assert.Equal(addressB, dr[0]);
            Assert.Same(personA, dr[1]);
            dr.CancelEdit();
            Assert.Equal(addressC, dr[0]);
            Assert.Same(personA, dr[1]);
        }

        [Fact] // Object this [Int32]
        public void Indexer2_Value_Null()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");
            Address addressB = new Address("Y", 4);
            Person personC = new Person("Jackson");
            Address addressC = new Address("Z", 3);

            dt.Rows.Add(new object[] { addressA, personA });
            dt.Rows.Add(new object[] { addressB, personB });

            DataRow dr = dt.Rows[0];

            try
            {
                dr[0] = null;
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Cannot set Column 'Col0' to be null.
                // Please use DBNull instead
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "Col0" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "DBNull" + @"\b", ex.Message);
            }

            Assert.Equal(addressA, dr[0]);
            Assert.False(dr.IsNull(0));
            Assert.Same(personA, dr[1]);
            Assert.False(dr.IsNull(1));

            dr[1] = null;

            Assert.Equal(addressA, dr[0]);
            Assert.False(dr.IsNull(0));
            Assert.Same(DBNull.Value, dr[1]);
            Assert.True(dr.IsNull(1));

            dr[0] = DBNull.Value;

            Assert.Same(DBNull.Value, dr[0]);
            Assert.True(dr.IsNull(0));
            Assert.Same(DBNull.Value, dr[1]);
            Assert.True(dr.IsNull(1));

            dr.BeginEdit();
            dr[1] = personC;
            Assert.Same(DBNull.Value, dr[0]);
            Assert.True(dr.IsNull(0));
            Assert.Equal(personC, dr[1]);
            Assert.False(dr.IsNull(1));
            dr.EndEdit();
            Assert.Same(DBNull.Value, dr[0]);
            Assert.True(dr.IsNull(0));
            Assert.Equal(personC, dr[1]);
            Assert.False(dr.IsNull(1));

            dr[1] = DBNull.Value;

            Assert.Same(DBNull.Value, dr[0]);
            Assert.True(dr.IsNull(0));
            Assert.Same(DBNull.Value, dr[1]);
            Assert.True(dr.IsNull(1));
        }

        [Fact] // Object this [String]
        public void Indexer3()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");
            Address addressB = new Address("Y", 4);
            Person personC = new Person("Jackson");
            Address addressC = new Address("Z", 3);

            dt.Rows.Add(new object[] { addressA, personA });
            dt.Rows.Add(new object[] { addressB, personB });
            DataRow dr;

            dr = dt.Rows[0];
            Assert.Equal(addressA, dr["Col0"]);
            Assert.Same(personA, dr["Col1"]);

            dr = dt.Rows[1];
            Assert.Equal(addressB, dr["Col0"]);
            Assert.Same(personB, dr["Col1"]);

            dr = dt.Rows[0];
            dr["Col0"] = addressC;
            Assert.Equal(addressC, dr["Col0"]);
            Assert.Same(personA, dr["Col1"]);

            dr = dt.Rows[1];
            dr.BeginEdit();
            dr["Col1"] = personC;
            Assert.Equal(addressB, dr["Col0"]);
            Assert.Same(personC, dr["Col1"]);
            dr.EndEdit();
            Assert.Equal(addressB, dr["Col0"]);
            Assert.Same(personC, dr["Col1"]);

            dr = dt.Rows[0];
            dr.BeginEdit();
            dr["Col0"] = addressB;
            Assert.Equal(addressB, dr["Col0"]);
            Assert.Same(personA, dr["Col1"]);
            dr.CancelEdit();
            Assert.Equal(addressC, dr["Col0"]);
            Assert.Same(personA, dr["Col1"]);
        }

        [Fact] // Object this [String]
        public void Indexer3_ColumnName_Empty()
        {
            DataTable dt = new DataTable("Persons");
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn(string.Empty, typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");

            dt.Rows.Add(new object[] { addressA, personA });

            DataRow dr = dt.Rows[0];

            try
            {
                object value = dr[string.Empty];
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                //  Column '' does not belong to table Persons
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "Persons" + @"\b", ex.Message);

                Assert.Null(ex.ParamName);
            }

            try
            {
                dr[string.Empty] = personB;
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Column '' does not belong to table Persons
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "Persons" + @"\b", ex.Message);

                Assert.Null(ex.ParamName);
            }
        }

        [Fact] // Object this [String]
        public void Indexer3_ColumnName_Null()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");

            dt.Rows.Add(new object[] { addressA, personA });

            DataRow dr = dt.Rows[0];

            try
            {
                object value = dr[(string)null];
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Equal("name", ex.ParamName);
            }

            try
            {
                dr[(string)null] = personB;
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Equal("name", ex.ParamName);
            }
        }

        [Fact] // Object this [String]
        public void Indexer3_Value_Null()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");
            Address addressB = new Address("Y", 4);
            Person personC = new Person("Jackson");
            Address addressC = new Address("Z", 3);

            dt.Rows.Add(new object[] { addressA, personA });
            dt.Rows.Add(new object[] { addressB, personB });

            DataRow dr = dt.Rows[0];

            try
            {
                dr["Col0"] = null;
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Cannot set Column 'Col0' to be null.
                // Please use DBNull instead
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "Col0" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "DBNull" + @"\b", ex.Message);
            }

            Assert.Equal(addressA, dr["Col0"]);
            Assert.False(dr.IsNull("Col0"));
            Assert.Same(personA, dr["Col1"]);
            Assert.False(dr.IsNull("Col1"));

            dr["Col1"] = null;

            Assert.Equal(addressA, dr["Col0"]);
            Assert.False(dr.IsNull("Col0"));
            Assert.Same(DBNull.Value, dr["Col1"]);
            Assert.True(dr.IsNull("Col1"));

            dr["Col0"] = DBNull.Value;

            Assert.Same(DBNull.Value, dr["Col0"]);
            Assert.True(dr.IsNull("Col0"));
            Assert.Same(DBNull.Value, dr["Col1"]);
            Assert.True(dr.IsNull("Col1"));

            dr["Col1"] = personC;
            dr.BeginEdit();
            Assert.Same(DBNull.Value, dr["Col0"]);
            Assert.True(dr.IsNull("Col0"));
            Assert.Equal(personC, dr["Col1"]);
            Assert.False(dr.IsNull("Col1"));
            dr.EndEdit();

            dr["Col1"] = DBNull.Value;

            Assert.Same(DBNull.Value, dr["Col0"]);
            Assert.True(dr.IsNull("Col0"));
            Assert.Same(DBNull.Value, dr["Col1"]);
            Assert.True(dr.IsNull("Col1"));
        }

        [Fact] // Object this [DataColumn, DataRowVersion]
        public void Indexer4()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");
            Address addressB = new Address("Y", 4);
            Person personC = new Person("Jackson");
            Address addressC = new Address("Z", 3);

            dt.Rows.Add(new object[] { addressA, personA });
            dt.Rows.Add(new object[] { addressB, personB });
            DataRow dr;

            dr = dt.Rows[0];
            Assert.Equal(addressA, dr[dc0, DataRowVersion.Current]);
            Assert.Equal(addressA, dr[dc0, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Proposed));
            Assert.Same(personA, dr[dc1, DataRowVersion.Current]);
            Assert.Same(personA, dr[dc1, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Proposed));

            dr = dt.Rows[1];
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Current]);
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Proposed));
            Assert.Same(personB, dr[dc1, DataRowVersion.Current]);
            Assert.Same(personB, dr[dc1, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Proposed));

            dr = dt.Rows[0];
            dr[dc0] = addressC;
            Assert.Equal(addressC, dr[dc0, DataRowVersion.Current]);
            Assert.Equal(addressC, dr[dc0, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Proposed));
            Assert.Same(personA, dr[dc1, DataRowVersion.Current]);
            Assert.Same(personA, dr[dc1, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Proposed));

            dr = dt.Rows[1];
            dr.BeginEdit();
            dr[dc1] = personC;
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Current]);
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Original));
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Proposed]);
            Assert.Same(personB, dr[dc1, DataRowVersion.Current]);
            Assert.Same(personC, dr[dc1, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Original));
            Assert.Same(personC, dr[dc1, DataRowVersion.Proposed]);
            dr.EndEdit();
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Current]);
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Proposed));
            Assert.Same(personC, dr[dc1, DataRowVersion.Current]);
            Assert.Same(personC, dr[dc1, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Proposed));
            dr.AcceptChanges();
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Current]);
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Default]);
            Assert.Equal(addressB, dr[dc0, DataRowVersion.Original]);
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Proposed));
            Assert.Same(personC, dr[dc1, DataRowVersion.Current]);
            Assert.Same(personC, dr[dc1, DataRowVersion.Default]);
            Assert.Equal(personC, dr[dc1, DataRowVersion.Original]);
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Proposed));

            dr = dt.Rows[0];
            dr.BeginEdit();
            dr[dc0] = addressA;
            Assert.Equal(addressC, dr[dc0, DataRowVersion.Current]);
            Assert.Equal(addressA, dr[dc0, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Original));
            Assert.Equal(addressA, dr[dc0, DataRowVersion.Proposed]);
            Assert.Same(personA, dr[dc1, DataRowVersion.Current]);
            Assert.Same(personA, dr[dc1, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Original));
            Assert.Same(personA, dr[dc1, DataRowVersion.Proposed]);
            dr.CancelEdit();
            Assert.Equal(addressC, dr[dc0, DataRowVersion.Current]);
            Assert.Equal(addressC, dr[dc0, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc0, DataRowVersion.Proposed));
            Assert.Same(personA, dr[dc1, DataRowVersion.Current]);
            Assert.Same(personA, dr[dc1, DataRowVersion.Default]);
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Original));
            Assert.True(AssertNotFound(dr, dc1, DataRowVersion.Proposed));
        }

        [Fact]
        public void Indexer4_Column_NotInTable()
        {
            DataTable dtA = new DataTable("TableA");
            DataColumn dcA1 = new DataColumn("Col0", typeof(Address));
            dtA.Columns.Add(dcA1);
            DataColumn dcA2 = new DataColumn("Col1", typeof(Person));
            dtA.Columns.Add(dcA2);

            DataTable dtB = new DataTable("TableB");
            DataColumn dcB1 = new DataColumn("Col0", typeof(Address));
            dtB.Columns.Add(dcB1);
            DataColumn dcB2 = new DataColumn("Col1", typeof(Person));
            dtB.Columns.Add(dcB2);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);

            dtA.Rows.Add(new object[] { addressA, personA });
            DataRow dr = dtA.Rows[0];

            try
            {
                object value = dr[dcB1, DataRowVersion.Default];
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Column 'Col0' does not belong to table TableA
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "Col0" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "TableA" + @"\b", ex.Message);
            }

            try
            {
                object value = dr[new DataColumn("ZZZ"), DataRowVersion.Default];
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Column 'Col0' does not belong to table TableA
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "ZZZ" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "TableA" + @"\b", ex.Message);
            }

            dtA.Columns.Remove(dcA2);

            try
            {
                object value = dr[dcA2, DataRowVersion.Default];
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Column 'Col0' does not belong to table TableA
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "Col1" + @"[\p{Pf}\p{Po}]", ex.Message);
                Assert.Matches(@"\b" + "TableA" + @"\b", ex.Message);
            }
        }

        [Fact] // Object this [DataColumn, DataRowVersion]
        public void Indexer4_Column_Null()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");

            dt.Rows.Add(new object[] { addressA, personA });
            DataRow dr = dt.Rows[0];

            try
            {
                object value = dr[(DataColumn)null, DataRowVersion.Default];
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Equal("column", ex.ParamName);
            }
        }

        [Fact] // Object this [DataColumn, DataRowVersion]
        public void Indexer4_Version_Invalid()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");

            dt.Rows.Add(new object[] { addressA, personA });
            DataRow dr = dt.Rows[0];

            try
            {
                object value = dr[dc0, (DataRowVersion)666];
                Assert.False(true);
            }
            catch (DataException ex)
            {
                // Version must be Original, Current, or Proposed
                Assert.Equal(typeof(DataException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("Original") != -1);
                Assert.True(ex.Message.IndexOf("Current") != -1);
                Assert.True(ex.Message.IndexOf("Proposed") != -1);
                Assert.False(ex.Message.IndexOf("Default") != -1);
            }
        }

        [Fact] // Object this [DataColumn, DataRowVersion]
        public void Indexer4_Version_NotFound()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(Address));
            dt.Columns.Add(dc0);
            DataColumn dc1 = new DataColumn("Col1", typeof(Person));
            dt.Columns.Add(dc1);

            Person personA = new Person("Miguel");
            Address addressA = new Address("X", 5);
            Person personB = new Person("Chris");

            dt.Rows.Add(new object[] { addressA, personA });
            DataRow dr = dt.Rows[0];

            try
            {
                object value = dr[dc0, DataRowVersion.Original];
                Assert.False(true);
            }
            catch (VersionNotFoundException ex)
            {
                // There is no Original data to access
                Assert.Equal(typeof(VersionNotFoundException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("Original") != -1);
            }

            try
            {
                object value = dr[dc0, DataRowVersion.Proposed];
                Assert.False(true);
            }
            catch (VersionNotFoundException ex)
            {
                // There is no Proposed data to access
                Assert.Equal(typeof(VersionNotFoundException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("Proposed") != -1);
            }
        }

        [Fact]
        public void IsNull_ByDataColumn()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(int));
            DataColumn dc1 = new DataColumn("Col1", typeof(int));
            dt.Columns.Add(dc0);
            dt.Columns.Add(dc1);
            dt.Rows.Add(new object[] { 1234 });
            DataRow dr = dt.Rows[0];

            // IsNull_I 2
            Assert.Equal(false, dr.IsNull(dc0));

            // IsNull_I 2
            Assert.Equal(true, dr.IsNull(dc1));
        }

        [Fact]
        public void IsNull_ByDataColumnDataRowVersion()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(int));
            DataColumn dc1 = new DataColumn("Col1", typeof(int));
            dt.Columns.Add(dc0);
            dt.Columns.Add(dc1);
            dt.Rows.Add(new object[] { 1234 });
            DataRow dr = dt.Rows[0];

            // IsNull - col0 Current
            Assert.Equal(false, dr.IsNull(dc0, DataRowVersion.Current));

            // IsNull - col1 Current
            Assert.Equal(true, dr.IsNull(dc1, DataRowVersion.Current));

            // IsNull - col0 Default
            Assert.Equal(false, dr.IsNull(dc0, DataRowVersion.Default));
            // IsNull - col1 Default
            Assert.Equal(true, dr.IsNull(dc1, DataRowVersion.Default));

            dr.BeginEdit();
            dr[0] = 9; //Change value, Create RowVersion Proposed

            // IsNull - col0 Proposed
            Assert.Equal(false, dr.IsNull(dc0, DataRowVersion.Proposed));
            // IsNull - col1 Proposed
            Assert.Equal(true, dr.IsNull(dc1, DataRowVersion.Proposed));

            dr.AcceptChanges();
            dr.Delete();

            // IsNull - col0 Original
            Assert.Equal(false, dr.IsNull(dc0, DataRowVersion.Original));
        }

        [Fact]
        public void IsNull_ByIndex()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(int));
            DataColumn dc1 = new DataColumn("Col1", typeof(int));
            dt.Columns.Add(dc0);
            dt.Columns.Add(dc1);
            dt.Rows.Add(new object[] { 1234 });
            DataRow dr = dt.Rows[0];

            // IsNull_I 2
            Assert.Equal(false, dr.IsNull(0));

            // IsNull_I 2
            Assert.Equal(true, dr.IsNull(1));
        }

        [Fact]
        public void IsNull_ByName()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(int));
            DataColumn dc1 = new DataColumn("Col1", typeof(int));
            dt.Columns.Add(dc0);
            dt.Columns.Add(dc1);
            dt.Rows.Add(new object[] { 1234 });
            DataRow dr = dt.Rows[0];

            #region --- assignment  ----
            // IsNull_S 1
            Assert.Equal(false, dr.IsNull("Col0"));

            // IsNull_S 2
            Assert.Equal(true, dr.IsNull("Col1"));
            #endregion

            // IsNull_S 1
            MemoryStream st = new MemoryStream();
            StreamWriter sw = new StreamWriter(st);
            sw.Write("<?xml version=\"1.0\" standalone=\"yes\"?><NewDataSet>");
            sw.Write("<Table><EmployeeNo>9</EmployeeNo></Table>");
            sw.Write("</NewDataSet>");
            sw.Flush();
            st.Position = 0;
            var ds = new DataSet();
            ds.ReadXml(st);
            //  Here we add the expression column
            ds.Tables[0].Columns.Add("ValueListValueMember", typeof(object), "EmployeeNo");

            foreach (DataRow row in ds.Tables[0].Rows)
            {
                if (row.IsNull("ValueListValueMember") == true)
                    Assert.Equal("Failed", "SubTest");
                else
                    Assert.Equal("Passed", "Passed");
            }
        }

        [Fact]
        public void IsNull_BeforeGetValue()
        {
            DataTable table = new DataTable();

            // add the row, with the value in the column
            DataColumn staticColumn = table.Columns.Add("static", typeof(string), null); // static
            DataRow row = table.Rows.Add("the value");
            Assert.False(row.IsNull("static"));
            Assert.Equal("the value", row["static"]);

            // add the first derived column
            DataColumn firstColumn = table.Columns.Add("first", typeof(string), "static"); // first -> static
            Assert.False(row.IsNull("first"));
            Assert.Equal("the value", row["first"]);

            // add the second level of related
            DataColumn secondColumn = table.Columns.Add("second", typeof(string), "first"); // second -> first -> static
            Assert.False(row.IsNull("second"));
            Assert.Equal("the value", row["second"]);
        }

        [Fact]
        public void IsNull_NullValueArguments()
        {
            DataTable table = new DataTable();

            // add the row, with the value in the column
            DataColumn staticColumn = table.Columns.Add("static", typeof(string), null);
            DataRow row = table.Rows.Add("the value");

            try
            {
                row.IsNull((string)null);
                Assert.False(true);
            }
            catch (ArgumentNullException)
            {
                // do nothing as null columns aren't allowed
            }

            try
            {
                row.IsNull("");
                Assert.False(true);
            }
            catch (ArgumentException)
            {
                // do nothing as we can't find a col with no name
            }

            try
            {
                row.IsNull(null, DataRowVersion.Default);
                Assert.False(true);
            }
            catch (ArgumentNullException)
            {
                // do nothing as null columns aren't allowed
            }
        }

        [Fact]
        public void Item()
        {
            // init table with columns
            DataTable myTable = new DataTable("myTable");

            myTable.Columns.Add(new DataColumn("Id", typeof(int)));
            myTable.Columns.Add(new DataColumn("Name", typeof(string)));
            DataColumn dc = myTable.Columns[0];

            myTable.Rows.Add(new object[] { 1, "Ofer" });
            myTable.Rows.Add(new object[] { 2, "Ofer" });

            myTable.AcceptChanges();

            DataRow myRow = myTable.Rows[0];

            //Start checking

            // Item - index
            Assert.Equal(1, (int)myRow[0]);

            // Item - string
            Assert.Equal(1, (int)myRow["Id"]);

            // Item - Column
            Assert.Equal(1, (int)myRow[dc]);

            // Item - index,Current
            Assert.Equal(1, (int)myRow[0, DataRowVersion.Current]);

            // Item - string,Current
            Assert.Equal(1, (int)myRow["Id", DataRowVersion.Current]);

            // Item - columnn,Current
            Assert.Equal(1, (int)myRow[dc, DataRowVersion.Current]);

            //	testMore();
        }

        /*public void testMore()
		{
			DataTable dt = DataProvider.CreateParentDataTable();
			dt.Rows[0].BeginEdit();
			dt.Rows[0][0] = 10;
			dt.Rows[0].EndEdit();
			dt.AcceptChanges();
		}*/

        [Fact]
        public void RejectChanges()
        {
            DataTable dt = new DataTable();
            DataColumn dc0 = new DataColumn("Col0", typeof(int));
            DataColumn dc1 = new DataColumn("Col1", typeof(int));
            dt.Columns.Add(dc0);
            dt.Columns.Add(dc1);
            dt.Rows.Add(new object[] { 1234 });
            dt.AcceptChanges();
            DataRow dr = dt.Rows[0];

            dr[0] = 567;
            dr[1] = 789;
            dr.RejectChanges();

            // RejectChanges - row 0
            Assert.Equal(1234, (int)dr[0]);

            // RejectChanges - row 1
            Assert.Equal(DBNull.Value, dr[1]);

            dr.Delete();
            dr.RejectChanges();

            // RejectChanges - count
            Assert.Equal(1, dt.Rows.Count);
        }

        [Fact]
        public void RowState()
        {
            DataTable myTable = new DataTable("myTable");
            DataColumn dc = new DataColumn("Name", typeof(string));
            myTable.Columns.Add(dc);
            DataRow myRow;

            // Create a new DataRow.
            myRow = myTable.NewRow();

            // Detached row.

            // Detached
            Assert.Equal(DataRowState.Detached, myRow.RowState);

            myTable.Rows.Add(myRow);
            // New row.

            // Added
            Assert.Equal(DataRowState.Added, myRow.RowState);

            myTable.AcceptChanges();
            // Unchanged row.

            // Unchanged
            Assert.Equal(DataRowState.Unchanged, myRow.RowState);

            myRow["Name"] = "Scott";
            // Modified row.

            // Modified
            Assert.Equal(DataRowState.Modified, myRow.RowState);

            myRow.Delete();
            // Deleted row.

            // Deleted
            Assert.Equal(DataRowState.Deleted, myRow.RowState);
        }

        [Fact]
        public void SetColumnError_ByDataColumnError()
        {
            string sColErr = "Error!";
            DataTable dt = new DataTable("myTable");
            DataColumn dc = new DataColumn("Column1");
            dt.Columns.Add(dc);
            DataRow dr = dt.NewRow();

            // empty string
            Assert.Equal(string.Empty, dr.GetColumnError(dc));

            dr.SetColumnError(dc, sColErr);

            // error string
            Assert.Equal(sColErr, dr.GetColumnError(dc));
        }

        [Fact]
        public void SetColumnError_ByIndexError()
        {
            string sColErr = "Error!";
            DataTable dt = new DataTable("myTable");
            DataColumn dc = new DataColumn("Column1");
            dt.Columns.Add(dc);
            DataRow dr = dt.NewRow();

            // empty string
            Assert.Equal(string.Empty, dr.GetColumnError(0));

            dr.SetColumnError(0, sColErr);

            // error string
            Assert.Equal(sColErr, dr.GetColumnError(0));
            dr.SetColumnError(0, "");
            Assert.Equal("", dr.GetColumnError(0));
        }

        [Fact]
        public void SetColumnError_ByColumnNameError()
        {
            string sColErr = "Error!";
            DataTable dt = new DataTable("myTable");
            DataColumn dc = new DataColumn("Column1");
            dt.Columns.Add(dc);
            DataRow dr = dt.NewRow();

            // empty string
            Assert.Equal(string.Empty, dr.GetColumnError("Column1"));

            dr.SetColumnError("Column1", sColErr);

            // error string
            Assert.Equal(sColErr, dr.GetColumnError("Column1"));
        }

        [Fact]
        public void SetParentRow_ByDataRow()
        {
            DataRow drParent, drChild;
            DataRow drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);

            drParent = dtParent.Rows[0];
            drChild = dtChild.Select("ParentId=" + drParent["ParentId"])[0];

            drChild.SetParentRow(drParent);

            //Get Excepted result
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow("Parent-Child", DataRowVersion.Current);

            // SetParentRow
            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void testMore()
        {
            DataSet ds = DataProvider.CreateForeignConstraint();
            DataRow drParent = ds.Tables[0].Rows[0];
            //DataRow[] drArray =  ds.Tables[1].Rows[0].GetParentRows(ds.Tables[1].ParentRelations[0]);
            ds.Tables[1].Rows[0].SetParentRow(drParent);
        }

        [Fact]
        public void test()
        {
            // test SetParentRow
            DataTable parent = DataProvider.CreateParentDataTable();
            DataTable child = DataProvider.CreateChildDataTable();
            DataRow dr = parent.Rows[0];
            dr.Delete();
            parent.AcceptChanges();

            child.Rows[0].SetParentRow(dr);
        }

        public void checkForLoops()
        {
            var ds = new DataSet();
            //Create tables
            DataTable dtChild = DataProvider.CreateChildDataTable();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            dtChild.Rows.Clear();
            dtParent.Rows.Clear();

            dtParent.ChildRelations.Add(dtParent.Columns[0], dtChild.Columns[0]);
            dtChild.ChildRelations.Add(dtChild.Columns[0], dtParent.Columns[0]);

            dtChild.Rows[0].SetParentRow(dtParent.Rows[0]);
            dtParent.Rows[0].SetParentRow(dtChild.Rows[0]);
        }

        public void checkForLoopsAdvanced()
        {
            //Create tables
            DataTable dtChild = new DataTable();
            dtChild.Columns.Add("Col1", typeof(int));
            dtChild.Columns.Add("Col2", typeof(int));

            DataRelation drl = new DataRelation("drl1", dtChild.Columns[0], dtChild.Columns[1]);
            dtChild.ChildRelations.Add(drl);
            dtChild.Rows[0].SetParentRow(dtChild.Rows[1]);
            dtChild.Rows[1].SetParentRow(dtChild.Rows[0]);
        }

        [Fact]
        public void SetParentRow_ByDataRowDataRelation()
        {
            DataRow drParent, drChild;
            DataRow drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);

            drParent = dtParent.Rows[0];
            drChild = dtChild.Select("ParentId=" + drParent["ParentId"])[0];

            drChild.SetParentRow(drParent, dRel);

            //Get Excepted result
            drArrExcepted = drParent;
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRow("Parent-Child", DataRowVersion.Current);

            // SetParentRow
            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void Table()
        {
            DataTable dt1, dt2;
            dt2 = new DataTable("myTable");
            DataRow dr = dt2.NewRow();
            dt1 = dr.Table;

            // ctor
            Assert.Equal(dt2, dt1);
        }

        [Fact]
        public new void ToString()
        {
            DataRow dr;
            DataTable dtParent;
            dtParent = DataProvider.CreateParentDataTable();
            dr = dtParent.Rows[0];

            // ToString
            Assert.Equal(true, dr.ToString().ToLower().StartsWith("system.data.datarow"));
        }

        [Fact]
        public void DataRow_RowError()
        {
            DataTable dt = new DataTable("myTable");
            DataRow dr = dt.NewRow();

            Assert.Equal(dr.RowError, string.Empty);

            dr.RowError = "Err";
            Assert.Equal(dr.RowError, "Err");
        }

        [Fact]
        public void DataRow_RowError2()
        {
            Assert.Throws<ConstraintException>(() =>
           {
               DataTable dt1 = DataProvider.CreateUniqueConstraint();

               dt1.BeginLoadData();

               DataRow dr = dt1.NewRow();
               dr[0] = 3;
               dt1.Rows.Add(dr);
               dt1.EndLoadData();
           });
        }

        [Fact]
        public void DataRow_RowError3()
        {
            Assert.Throws<ConstraintException>(() =>
           {
               DataSet ds = DataProvider.CreateForeignConstraint();
               ds.Tables[0].BeginLoadData();
               ds.Tables[0].Rows[0][0] = 10;
               ds.Tables[0].EndLoadData(); //Foreign constraint violation
           });
        }

        [Fact]
        public void TestRowErrors()
        {
            DataTable table = new DataTable();
            DataColumn col1 = table.Columns.Add("col1", typeof(int));
            DataColumn col2 = table.Columns.Add("col2", typeof(int));
            DataColumn col3 = table.Columns.Add("col3", typeof(int));

            col1.AllowDBNull = false;
            table.Constraints.Add("uc", new DataColumn[] { col2, col3 }, false);
            table.BeginLoadData();
            table.Rows.Add(new object[] { null, 1, 1 });
            table.Rows.Add(new object[] { 1, 1, 1 });
            try
            {
                table.EndLoadData();
                Assert.False(true);
            }
            catch (ConstraintException) { }
            Assert.True(table.HasErrors);
            DataRow[] rows = table.GetErrors();

            Assert.Equal(2, rows.Length);

            // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
            // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
            // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
            Assert.Matches(@"[\p{Pi}\p{Po}]" + "col1" + @"[\p{Pf}\p{Po}]", table.Rows[0].RowError);
            Assert.Matches(@"\b" + Regex.Escape("DBNull.Value") + @"\b", table.Rows[0].RowError);

            Assert.Matches(@"[\p{Pi}\p{Po}]" + "col2" + @"\p{Po}\s*" + "col3" + @"[\p{Pf}\p{Po}]", table.Rows[1].RowError);
            Assert.Matches(@"[\p{Pi}\p{Po}]" + "1" + @"\p{Po}\s*" + "1" + @"[\p{Pf}\p{Po}]", table.Rows[1].RowError);

            Assert.Equal(table.Rows[0].RowError, table.Rows[0].GetColumnError(0));
            Assert.Equal(table.Rows[1].RowError, table.Rows[0].GetColumnError(1));
            Assert.Equal(table.Rows[1].RowError, table.Rows[0].GetColumnError(2));

            Assert.Equal("", table.Rows[1].GetColumnError(0));
            Assert.Equal(table.Rows[1].RowError, table.Rows[1].GetColumnError(1));
            Assert.Equal(table.Rows[1].RowError, table.Rows[1].GetColumnError(2));
        }

        [Fact]
        public void BeginEdit()
        {
            DataTable myTable = new DataTable("myTable");
            DataColumn dc = new DataColumn("Id", typeof(int));
            dc.Unique = true;
            myTable.Columns.Add(dc);
            myTable.Rows.Add(new object[] { 1 });
            myTable.Rows.Add(new object[] { 2 });
            myTable.Rows.Add(new object[] { 3 });

            DataRow myRow = myTable.Rows[0];

            Assert.Throws<ConstraintException>(() => myRow[0] = 2); //row[0] now conflict with row[1] 

            //Will NOT! throw exception
            myRow.BeginEdit();
            myRow[0] = 2; //row[0] now conflict with row[1] 

            DataTable dt = DataProvider.CreateParentDataTable();
            DataRow dr = dt.Rows[0];
            dr.Delete();
            Assert.Throws<DeletedRowInaccessibleException>(() => dr.BeginEdit());
        }

        [Fact]
        public void GetChildRows_DataRelation()
        {
            DataRow dr;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();

            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();

            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);
            dr = dtParent.Rows[0];

            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);
            ds.Relations.Add(dRel);
            //Get Excepted result
            drArrExcepted = dtChild.Select("ParentId=" + dr["ParentId"]);
            //Get Result
            drArrResult = dr.GetChildRows(dRel);

            Assert.Equal(drArrExcepted, drArrResult);
        }

        [Fact]
        public void GetParentRows_DataRelation_DataRowVersion()
        {
            DataRow drParent, drChild;
            DataRow[] drArrExcepted, drArrResult;
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            drParent = dtParent.Rows[0];
            drChild = dtChild.Select("ParentId=" + drParent["ParentId"])[0];

            //Duplicate several rows in order to create Many to Many relation
            dtParent.ImportRow(drParent);
            dtParent.ImportRow(drParent);
            dtParent.ImportRow(drParent);

            //Add Relation
            DataRelation dRel = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"], false);
            ds.Relations.Add(dRel);

            //Get Excepted result
            drArrExcepted = dtParent.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRows(dRel, DataRowVersion.Current);
            Assert.Equal(drArrExcepted, drArrResult);

            //Get Excepted result
            drArrExcepted = dtParent.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.OriginalRows);
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRows(dRel, DataRowVersion.Original);
            Assert.Equal(drArrExcepted, drArrResult);

            //Get Excepted result, in this case Current = Default
            drArrExcepted = dtParent.Select("ParentId=" + drParent["ParentId"], "", DataViewRowState.CurrentRows);
            //Get Result DataRowVersion.Current
            drArrResult = drChild.GetParentRows(dRel, DataRowVersion.Default);
            Assert.Equal(drArrExcepted, drArrResult);

            Assert.Throws<InvalidConstraintException>(() =>
            {
                DataTable dtOtherParent = DataProvider.CreateParentDataTable();
                DataTable dtOtherChild = DataProvider.CreateChildDataTable();

                DataRelation drl = new DataRelation("newRelation", dtOtherParent.Columns[0], dtOtherChild.Columns[0]);
                drChild.GetParentRows(drl, DataRowVersion.Current);
            });
        }

        [Fact]
        public void ItemArray()
        {
            DataTable dt = GetDataTable();
            DataRow dr = dt.Rows[0];

            Assert.Equal(1, (int)dr.ItemArray[0]);

            Assert.Equal("Ofer", (string)dr.ItemArray[1]);

            dt = GetDataTable();

            dr = dt.Rows[0];

            //Changing row via itemArray

            dt.Rows[0].ItemArray = new object[] { 2, "Oren" };

            Assert.Equal(2, (int)dr.ItemArray[0]);
            Assert.Equal("Oren", (string)dr.ItemArray[1]);

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                dt.Rows[0].ItemArray = new object[] { 2, "Oren", "some1else" };
            });
        }

        [Fact]
        public void ItemArray_NewTable()
        {
            DataTable dt = new DataTable("Customers");

            dt.Columns.Add("name", typeof(string));
            dt.Columns.Add("address", typeof(string));
            dt.Columns.Add("phone", typeof(string));

            DataRow dr = dt.NewRow();
            dr["name"] = "myName";
            dr["address"] = "myAddress";
            dr["phone"] = "myPhone";

            // Should not throw RowNotInTableException
            object[] obj = dr.ItemArray;
        }

        private DataTable GetDataTable()
        {
            DataTable dt = new DataTable("myTable");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));

            DataRow dr = dt.NewRow();
            dr.ItemArray = new object[] { 1, "Ofer" };

            dt.Rows.Add(dr);

            return dt;
        }

        [Fact]
        public void RowError()
        {
            DataTable dt = new DataTable("myTable");
            DataRow dr = dt.NewRow();

            Assert.Equal(string.Empty, dr.RowError);

            dr.RowError = "Err";

            Assert.Equal("Err", dr.RowError);

            DataTable dt1 = DataProvider.CreateUniqueConstraint();

            Assert.Throws<ConstraintException>(() =>
            {
                dt1.BeginLoadData();

                dr = dt1.NewRow();
                dr[0] = 3;
                dt1.Rows.Add(dr);
                dt1.EndLoadData();
            });
            Assert.Equal(2, dt1.GetErrors().Length);
            Assert.Equal(true, dt1.GetErrors()[0].RowError.Length > 10);
            Assert.Equal(true, dt1.GetErrors()[1].RowError.Length > 10);

            DataSet ds = DataProvider.CreateForeignConstraint();
            Assert.Throws<ConstraintException>(() =>
            {
                ds.Tables[0].BeginLoadData();
                ds.Tables[0].Rows[0][0] = 10; //Foreign constraint violation
                                              //ds.Tables[0].AcceptChanges();
                ds.Tables[0].EndLoadData();
            });
            Assert.Equal(3, ds.Tables[1].GetErrors().Length);
            for (int index = 0; index < 3; index++)
            {
                Assert.Equal(true, ds.Tables[1].GetErrors()[index].RowError.Length > 10);
            }
        }

        [Fact]
        public void bug78885()
        {
            DataSet ds = new DataSet();
            DataTable t = ds.Tables.Add("table");
            DataColumn id;

            id = t.Columns.Add("userID", Type.GetType("System.Int32"));
            id.AutoIncrement = true;
            t.Columns.Add("name", Type.GetType("System.String"));
            t.Columns.Add("address", Type.GetType("System.String"));
            t.Columns.Add("zipcode", Type.GetType("System.Int32"));
            t.PrimaryKey = new DataColumn[] { id };

            DataRow tempRow;
            tempRow = t.NewRow();
            tempRow["name"] = "Joan";
            tempRow["address"] = "Balmes 152";
            tempRow["zipcode"] = "1";
            t.Rows.Add(tempRow);

            t.RowChanged += new DataRowChangeEventHandler(RowChangedHandler);

            /* neither of the calls to EndEdit below generate a RowChangedHandler on MS.  the first one does on mono */
            t.DefaultView[0].BeginEdit();
            t.DefaultView[0].EndEdit(); /* this generates a call to the row changed handler */
            t.DefaultView[0].EndEdit(); /* this doesn't */

            Assert.False(_rowChanged);
        }

        private void RowChangedHandler(object sender, DataRowChangeEventArgs e)
        {
            _rowChanged = true;
        }

        [Fact]
        public void SetAdded_test()
        {
            DataTable table = new DataTable();

            DataRow row = table.NewRow();
            try
            {
                row.SetAdded();
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
            }

            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));
            table.Columns.Add("col3", typeof(int));

            row = table.Rows.Add(new object[] { 1, 2, 3 });
            Assert.Equal(DataRowState.Added, row.RowState);
            try
            {
                row.SetAdded();
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
            }
            Assert.Equal(DataRowState.Added, row.RowState);

            row.AcceptChanges();
            row[0] = 10;
            Assert.Equal(DataRowState.Modified, row.RowState);
            try
            {
                row.SetAdded();
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
            }

            row.AcceptChanges();
            Assert.Equal(DataRowState.Unchanged, row.RowState);
            row.SetAdded();
            Assert.Equal(DataRowState.Added, row.RowState);
        }

        [Fact]
        public void setAdded_testRollback()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));

            table.Rows.Add(new object[] { 1, 1 });
            table.AcceptChanges();

            table.Rows[0].SetAdded();
            table.RejectChanges();
            Assert.Equal(0, table.Rows.Count);
        }

        [Fact]
        public void SetModified_test()
        {
            DataTable table = new DataTable();

            DataRow row = table.NewRow();
            try
            {
                row.SetModified();
            }
            catch (InvalidOperationException) { }

            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));
            table.Columns.Add("col3", typeof(int));

            row = table.Rows.Add(new object[] { 1, 2, 3 });
            Assert.Equal(DataRowState.Added, row.RowState);
            try
            {
                row.SetModified();
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
                // Never premise English.
                //Assert.Equal (SetAddedModified_ErrMsg, e.Message);
            }

            row.AcceptChanges();
            row[0] = 10;
            Assert.Equal(DataRowState.Modified, row.RowState);
            try
            {
                row.SetModified();
                Assert.False(true);
            }
            catch (InvalidOperationException e)
            {
                // Never premise English.
                //Assert.Equal (SetAddedModified_ErrMsg, e.Message);
            }

            row.AcceptChanges();
            Assert.Equal(DataRowState.Unchanged, row.RowState);
            row.SetModified();
            Assert.Equal(DataRowState.Modified, row.RowState);
        }

        [Fact]
        public void setModified_testRollback()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));

            DataRow row = table.Rows.Add(new object[] { 1, 1 });
            table.AcceptChanges();

            row.SetModified();
            Assert.Equal(row.RowState, DataRowState.Modified);
            Assert.Equal(1, row[0, DataRowVersion.Current]);
            Assert.Equal(1, row[0, DataRowVersion.Original]);
            table.RejectChanges();
            Assert.Equal(row.RowState, DataRowState.Unchanged);
        }
        [Fact]
        public void DataRowExpressionDefaultValueTest()
        {
            DataSet ds = new DataSet();
            DataTable custTable = ds.Tables.Add("CustTable");

            DataColumn column = new DataColumn("units", typeof(int));
            column.AllowDBNull = false;
            column.Caption = "Units";
            column.DefaultValue = 1;
            custTable.Columns.Add(column);

            column = new DataColumn("price", typeof(decimal));
            column.AllowDBNull = false;
            column.Caption = "Price";
            column.DefaultValue = 25;
            custTable.Columns.Add(column);

            column = new DataColumn("total", typeof(string));
            column.Caption = "Total";
            column.Expression = "price*units";
            custTable.Columns.Add(column);

            DataRow row = custTable.NewRow();

            Assert.Equal(DBNull.Value, row["Total"]);
            custTable.Rows.Add(row);

            Assert.Equal("25", row["Total"]);
        }

        private void ColumnChanged(object sender, DataColumnChangeEventArgs e)
        {
            _eventsFired.Add(new EventInfo("ColumnChanged", e));
        }

        private void ColumnChanging(object sender, DataColumnChangeEventArgs e)
        {
            _eventsFired.Add(new EventInfo("ColumnChanging", e));
        }

        private class EventInfo
        {
            public EventInfo(string name, EventArgs args)
            {
                _name = name;
                _args = args;
            }

            public string Name
            {
                get { return _name; }
            }

            public EventArgs Args
            {
                get { return _args; }
            }

            private readonly string _name;
            private readonly EventArgs _args;
        }

        private static bool AssertNotFound(DataRow rc, DataColumn dc, DataRowVersion version)
        {
            try
            {
                object value = rc[dc, version];
                return false;
            }
            catch (VersionNotFoundException)
            {
                return true;
            }
        }

        private class Person
        {
            public Person(string name)
            {
                Name = name;
            }

            public string Name;
            public Address HomeAddress;
        }

        private struct Address
        {
            public Address(string street, int houseNumber)
            {
                Street = street;
                HouseNumber = houseNumber;
            }

            public override bool Equals(object o)
            {
                if (!(o is Address))
                    return false;

                Address address = (Address)o;
                if (address.HouseNumber != HouseNumber)
                    return false;
                if (address.Street != Street)
                    return false;
                return true;
            }

            public override int GetHashCode()
            {
                if (Street == null)
                    return HouseNumber.GetHashCode();
                return (Street.GetHashCode() ^ HouseNumber.GetHashCode());
            }

            public override string ToString()
            {
                if (Street == null)
                    return HouseNumber.ToString(CultureInfo.InvariantCulture);

                return string.Concat(Street, HouseNumber.ToString(CultureInfo.InvariantCulture));
            }

            public string Street;
            public int HouseNumber;
        }
    }
}
