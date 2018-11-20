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
using System.Collections.Generic;
using System.Text.RegularExpressions;


using Xunit;

namespace System.Data.Tests
{
    public class DataTableTest2
    {
        private bool _EventTriggered;
        private bool _eventRaised;
        private bool _eventValues;

        private class ProtectedTestClass : DataTable
        {
            public ProtectedTestClass()
            {
                Columns.Add("Id", typeof(int));
                Columns.Add("Value", typeof(string));
                Rows.Add(new object[] { 1, "one" });
                Rows.Add(new object[] { 2, "two" });
                AcceptChanges();
            }

            public void OnColumnChanged_Test()
            {
                OnColumnChanged(new DataColumnChangeEventArgs(
                    Rows[0], Columns["Value"],
                    "NewValue"));
            }

            public void OnColumnChanging_Test()
            {
                OnColumnChanging(new DataColumnChangeEventArgs(
                    Rows[0], Columns["Value"],
                    "NewValue"));
            }

            public void OnRemoveColumn_Test()
            {
                OnRemoveColumn(Columns[0]);
            }

            public DataTable CreateInstance_Test()
            {
                return CreateInstance();
            }

            public void OnRowChanged_Test(DataRowAction drAction)
            {
                base.OnRowChanged(new DataRowChangeEventArgs(Rows[0], drAction));
            }

            public void OnRowChanging_Test(DataRowAction drAction)
            {
                base.OnRowChanging(new DataRowChangeEventArgs(Rows[0], drAction));
            }

            public void OnRowDeleted_Test(DataRowAction drAction)
            {
                base.OnRowDeleted(new DataRowChangeEventArgs(Rows[0], drAction));
            }

            public void OnRowDeleting_Test(DataRowAction drAction)
            {
                base.OnRowDeleting(new DataRowChangeEventArgs(Rows[0], drAction));
            }
        }

        [Fact]
        public void AcceptChanges()
        {
            string sNewValue = "NewValue";
            DataRow drModified, drDeleted, drAdded;
            DataTable dt = DataProvider.CreateParentDataTable();

            drModified = dt.Rows[0];
            drModified[1] = sNewValue; //DataRowState = Modified, DataRowVersion = Proposed

            drDeleted = dt.Rows[1];
            drDeleted.Delete(); //DataRowState =  Deleted

            drAdded = dt.NewRow();
            dt.Rows.Add(drAdded); //DataRowState =  Added

            dt.AcceptChanges();

            // AcceptChanges - Unchanged1
            Assert.Equal(DataRowState.Unchanged, drModified.RowState);

            // AcceptChanges - Current
            Assert.Equal(sNewValue, drModified[1, DataRowVersion.Current]);

            // AcceptChanges - Unchanged2
            Assert.Equal(DataRowState.Unchanged, drAdded.RowState);

            // AcceptChanges - Detached
            Assert.Equal(DataRowState.Detached, drDeleted.RowState);
        }

        [Fact]
        public void ChildRelations()
        {
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            DataRelationCollection drlCollection;
            DataRelation drl = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);

            // Checking ChildRelations - default value
            //Check default
            drlCollection = dtParent.ChildRelations;
            Assert.Equal(0, drlCollection.Count);

            ds.Relations.Add(drl);
            drlCollection = dtParent.ChildRelations;

            // Checking ChildRelations Count
            Assert.Equal(1, drlCollection.Count);

            // Checking ChildRelations Value
            Assert.Equal(drl, drlCollection[0]);
        }

        [Fact]
        public void Clear()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Clear();
            Assert.Equal(0, dt.Rows.Count);
        }

        [Fact]
        public void Clone()
        {
            DataTable dt1, dt2 = DataProvider.CreateParentDataTable();
            dt2.Constraints.Add("Unique", dt2.Columns[0], true);
            dt2.Columns[0].DefaultValue = 7;

            dt1 = dt2.Clone();

            for (int i = 0; i < dt2.Constraints.Count; i++)
            {
                // Clone - Constraints[{0}],i)
                Assert.Equal(dt2.Constraints[i].ConstraintName,
                    dt1.Constraints[i].ConstraintName);
            }

            for (int i = 0; i < dt2.Columns.Count; i++)
            {
                // Clone - Columns[{0}].ColumnName,i)
                Assert.Equal(dt2.Columns[i].ColumnName, dt1.Columns[i].ColumnName);

                // Clone - Columns[{0}].DataType,i)
                Assert.Equal(dt2.Columns[i].DataType, dt1.Columns[i].DataType);
            }
        }

        [Fact]
        public void ColumnChanged()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.ColumnChanged += new DataColumnChangeEventHandler(Column_Changed);

            _EventTriggered = false;
            // ColumnChanged - EventTriggered
            dt.Rows[0][1] = "NewValue";
            Assert.True(_EventTriggered);

            _EventTriggered = false;
            dt.ColumnChanged -= new DataColumnChangeEventHandler(Column_Changed);
            // ColumnChanged - NO EventTriggered
            dt.Rows[0][1] = "VeryNewValue";
            Assert.False(_EventTriggered);
        }

        private void Column_Changed(object sender, DataColumnChangeEventArgs e)
        {
            _EventTriggered = true;
        }

        [Fact]
        public void ColumnChanging()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.ColumnChanging += new DataColumnChangeEventHandler(Column_Changeding);

            _EventTriggered = false;
            // ColumnChanged - EventTriggered
            dt.Rows[0][1] = "NewValue";
            Assert.True(_EventTriggered);

            _EventTriggered = false;
            dt.ColumnChanging -= new DataColumnChangeEventHandler(Column_Changeding);
            // ColumnChanged - NO EventTriggered
            dt.Rows[0][1] = "VeryNewValue";
            Assert.False(_EventTriggered);
        }

        private void Column_Changeding(object sender, DataColumnChangeEventArgs e)
        {
            _EventTriggered = true;
        }

        [Fact]
        public void Columns()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataColumnCollection dcl = dtParent.Columns;

            Assert.NotNull(dcl);
            Assert.Equal(6, dcl.Count);
            dtParent.Columns.Add(new DataColumn("Test"));
            Assert.Equal(7, dcl.Count);
            DataColumn tmp = dtParent.Columns["TEST"];
            Assert.Equal(dtParent.Columns["Test"], tmp);
            dtParent.Columns.Add(new DataColumn("test"));
            Assert.Equal(8, dcl.Count);

            try
            {
                tmp = dtParent.Columns["TEST"];
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // The given name 'TEST' matches at least two
                // names in the collection object with different
                // cases, but does not match either of them with
                // the same case
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "TEST" + @"[\p{Pf}\p{Po}]", ex.Message);

                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void Compute()
        {
            DataTable dt = DataProvider.CreateChildDataTable();

            //Get expected
            DataRow[] drArr = dt.Select("ParentId=1");
            long iExSum = 0;
            foreach (DataRow dr in drArr)
                iExSum += (int)dr["ChildId"];
            object objCompute = null;
            // Compute - sum values
            objCompute = dt.Compute("Sum(ChildId)", "ParentId=1");
            Assert.Equal(long.Parse(objCompute.ToString()), long.Parse(iExSum.ToString()));

            // Compute - sum type
            Assert.Equal(typeof(long), objCompute.GetType());

            //get expected
            double iExAvg = 0;
            drArr = dt.Select("ParentId=5");
            foreach (DataRow dr in drArr)
                iExAvg += (double)dr["ChildDouble"];
            iExAvg = iExAvg / drArr.Length;

            // Compute - Avg value
            objCompute = dt.Compute("Avg(ChildDouble)", "ParentId=5");
            Assert.Equal(double.Parse(objCompute.ToString()), double.Parse(iExAvg.ToString()));

            // Compute - Avg type
            Assert.Equal(typeof(double), objCompute.GetType());
        }

        [Fact]
        public void Constraints()
        {
            DataTable dtParent;
            ConstraintCollection consColl;
            dtParent = DataProvider.CreateParentDataTable();

            consColl = dtParent.Constraints;
            // Checking Constraints  != null 
            Assert.NotNull(consColl);

            // Checking Constraints Count
            Assert.Equal(0, consColl.Count);

            // Checking Constraints Count
            //Add primary key
            dtParent.PrimaryKey = new DataColumn[] { dtParent.Columns[0] };
            Assert.Equal(1, consColl.Count);
        }

        [Fact]
        public void Copy()
        {
            DataTable dt1, dt2 = DataProvider.CreateParentDataTable();
            dt2.Constraints.Add("Unique", dt2.Columns[0], true);
            dt2.Columns[0].DefaultValue = 7;

            dt1 = dt2.Copy();

            for (int i = 0; i < dt2.Constraints.Count; i++)
            {
                // Copy - Constraints[{0}],i)
                Assert.Equal(dt2.Constraints[i].ConstraintName,
                    dt1.Constraints[i].ConstraintName);
            }

            for (int i = 0; i < dt2.Columns.Count; i++)
            {
                // Copy - Columns[{0}].ColumnName,i)
                Assert.Equal(dt2.Columns[i].ColumnName, dt1.Columns[i].ColumnName);

                // Copy - Columns[{0}].DataType,i)
                Assert.Equal(dt2.Columns[i].DataType, dt1.Columns[i].DataType);
            }

            DataRow[] drArr1, drArr2;
            drArr1 = dt1.Select(string.Empty);
            drArr2 = dt2.Select(string.Empty);
            for (int i = 0; i < drArr1.Length; i++)
            {
                // Copy - Data [ParentId]{0} ,i)
                Assert.Equal(drArr2[i]["ParentId"], drArr1[i]["ParentId"]);
                // Copy - Data [String1]{0} ,i)
                Assert.Equal(drArr2[i]["String1"], drArr1[i]["String1"]);
                // Copy - Data [String2]{0} ,i)
                Assert.Equal(drArr2[i]["String2"], drArr1[i]["String2"]);
            }
        }

        [Fact]
        public void CreateInstance()
        {
            // CreateInstance
            ProtectedTestClass C = new ProtectedTestClass();
            DataTable dt = C.CreateInstance_Test();
            Assert.NotNull(dt);
        }

        [Fact]
        public void DataSet()
        {
            DataTable dtParent;
            DataSet ds;
            dtParent = DataProvider.CreateParentDataTable();

            ds = dtParent.DataSet;

            // Checking DataSet == null
            Assert.Null(ds);

            // Checking DataSet != null
            ds = new DataSet("MyDataSet");
            ds.Tables.Add(dtParent);
            Assert.NotNull(dtParent.DataSet);

            // Checking DataSet Name
            Assert.Equal("MyDataSet", dtParent.DataSet.DataSetName);
        }

        [Fact]
        public void DefaultView()
        {
            DataTable dtParent;
            DataView dv;
            dtParent = DataProvider.CreateParentDataTable();
            dv = dtParent.DefaultView;
            Assert.NotNull(dv);
        }

        [Fact]
        public void EndLoadData()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Columns[0].AllowDBNull = false;

            // EndLoadData
            dt.BeginLoadData();
            dt.LoadDataRow(new object[] { null, "A", "B" }, false);

            try
            {
                dt.EndLoadData();
                Assert.False(true);
            }
            catch (ConstraintException ex)
            {
                // Failed to enable constraints. One or more rows
                // contain values violating non-null, unique, or
                // foreign-key constraints
                Assert.Equal(typeof(ConstraintException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact] // LoadDataRow (Object [], Boolean)
        public void LoadDataRow1_Column_ReadOnly()
        {
            DataTable dt = new DataTable("myTable");
            DataColumn dcId = new DataColumn("Id", typeof(int));
            dcId.ReadOnly = true;
            dt.Columns.Add(dcId);
            DataColumn dcName = new DataColumn("Name", typeof(string));
            dcName.ReadOnly = true;
            dt.Columns.Add(dcName);
            DataColumn dcPassword = new DataColumn("Password", typeof(string));
            dt.Columns.Add(dcPassword);
            dt.PrimaryKey = new DataColumn[] { dcId };

            dt.Rows.Add(new object[] { 5, "Mono", "guess" });
            dt.AcceptChanges();
            dt.LoadDataRow(new object[] { 5, "SysData", "what" }, true);

            Assert.Equal(1, dt.Rows.Count);
            DataRow row = dt.Rows.Find(5);
            Assert.NotNull(row);
            Assert.Equal(5, row[dcId]);
            Assert.Equal("SysData", row[dcName]);
            Assert.Equal("what", row[dcPassword]);
            Assert.Equal(DataRowState.Unchanged, row.RowState);
        }

        [Fact]
        public void LoadDataRow_DuplicateValues()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));

            table.PrimaryKey = new DataColumn[] { table.Columns[0] };

            table.BeginLoadData();
            table.LoadDataRow(new object[] { 1, 1 }, false);
            table.LoadDataRow(new object[] { 1, 10 }, false);

            try
            {
                table.EndLoadData();
                Assert.False(true);
            }
            catch (ConstraintException ex)
            {
                // Failed to enable constraints. One or more rows
                // contain values violating non-null, unique, or
                // foreign-key constraints
                Assert.Equal(typeof(ConstraintException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void LoadDataRow_WithoutBeginLoadData()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));

            table.PrimaryKey = new DataColumn[] { table.Columns[0] };
            table.Rows.Add(new object[] { 1, 1 });
            table.AcceptChanges();

            table.LoadDataRow(new object[] { 10, 1 }, false);
            DataRow row = table.Rows.Find(10);
            Assert.NotNull(row);
            Assert.Equal(1, row[1]);
            Assert.Equal(DataRowState.Added, row.RowState);
            table.AcceptChanges();

            table.LoadDataRow(new object[] { 10, 2 }, true);
            row = table.Rows.Find(10);
            Assert.NotNull(row);
            Assert.Equal(2, row[1]);
            Assert.Equal(DataRowState.Unchanged, row.RowState);

            table.LoadDataRow(new object[] { 1, 2 }, false);
            row = table.Rows.Find(1);
            Assert.NotNull(row);
            Assert.Equal(2, row[1]);
            Assert.Equal(DataRowState.Modified, table.Rows.Find(1).RowState);

            table.LoadDataRow(new object[] { 1, 3 }, true);
            row = table.Rows.Find(1);
            Assert.NotNull(row);
            Assert.Equal(3, row[1]);
            Assert.Equal(DataRowState.Unchanged, table.Rows.Find(1).RowState);
        }

        [Fact]
        public void EndLoadData_MergeDuplcateValues()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));

            table.PrimaryKey = new DataColumn[] { table.Columns[0] };
            table.Rows.Add(new object[] { 1, 500 });
            table.AcceptChanges();

            table.BeginLoadData();
            table.LoadDataRow(new object[] { 1, 1 }, false);
            table.LoadDataRow(new object[] { 1, 10 }, false);
            table.LoadDataRow(new object[] { 1, 100 }, false);
            table.EndLoadData();

            Assert.Equal(1, table.Rows.Count);
            Assert.Equal(100, table.Rows[0][1]);
        }

        [Fact]
        public void GetChanges()
        {
            DataTable dt1, dt2 = DataProvider.CreateParentDataTable();
            dt2.Constraints.Add("Unique", dt2.Columns[0], true);
            dt2.Columns[0].DefaultValue = 7;

            //make some changes
            dt2.Rows[0].Delete();
            dt2.Rows[1].Delete();
            dt2.Rows[2].Delete();
            dt2.Rows[3].Delete();

            dt1 = dt2.GetChanges();

            for (int i = 0; i < dt2.Constraints.Count; i++)
            {
                // GetChanges - Constraints[{0}],i)
                Assert.Equal(dt2.Constraints[i].ConstraintName, dt1.Constraints[i].ConstraintName);
            }

            for (int i = 0; i < dt2.Columns.Count; i++)
            {
                // GetChanges - Columns[{0}].ColumnName,i)
                Assert.Equal(dt2.Columns[i].ColumnName, dt1.Columns[i].ColumnName);

                // GetChanges - Columns[{0}].DataType,i)
                Assert.Equal(dt2.Columns[i].DataType, dt1.Columns[i].DataType);
            }

            DataRow[] drArr1, drArr2;

            drArr1 = dt1.Select(string.Empty, string.Empty, DataViewRowState.Deleted);
            drArr2 = dt2.Select(string.Empty, string.Empty, DataViewRowState.Deleted);

            for (int i = 0; i < drArr1.Length; i++)
            {
                // GetChanges - Data [ParentId]{0} ,i)
                Assert.Equal(drArr1[i]["ParentId", DataRowVersion.Original], drArr2[i]["ParentId", DataRowVersion.Original]);
                // GetChanges - Data [String1]{0} ,i)
                Assert.Equal(drArr1[i]["String1", DataRowVersion.Original], drArr2[i]["String1", DataRowVersion.Original]);
                // GetChanges - Data [String2]{0} ,i)
                Assert.Equal(drArr1[i]["String2", DataRowVersion.Original], drArr2[i]["String2", DataRowVersion.Original]);
            }
        }

        [Fact]
        public void GetChanges_ByDataRowState()
        {
            DataTable dt1, dt2 = DataProvider.CreateParentDataTable();
            dt2.Constraints.Add("Unique", dt2.Columns[0], true);
            dt2.Columns[0].DefaultValue = 7;

            //make some changes
            dt2.Rows[0].Delete(); //DataRowState.Deleted
            dt2.Rows[1].Delete(); //DataRowState.Deleted
            dt2.Rows[2].BeginEdit();
            dt2.Rows[2]["String1"] = "Changed"; //DataRowState.Modified
            dt2.Rows[2].EndEdit();

            dt2.Rows.Add(new object[] { "99", "Temp1", "Temp2" }); //DataRowState.Added

            // *********** Checking GetChanges - DataRowState.Deleted ************
            dt1 = null;
            dt1 = dt2.GetChanges(DataRowState.Deleted);
            CheckTableSchema(dt1, dt2, DataRowState.Deleted.ToString());
            DataRow[] drArr1, drArr2;
            drArr1 = dt1.Select(string.Empty, string.Empty, DataViewRowState.Deleted);
            drArr2 = dt2.Select(string.Empty, string.Empty, DataViewRowState.Deleted);

            for (int i = 0; i < drArr1.Length; i++)
            {
                // GetChanges(Deleted) - Data [ParentId]{0} ,i)
                Assert.Equal(drArr1[i]["ParentId", DataRowVersion.Original], drArr2[i]["ParentId", DataRowVersion.Original]);
                // GetChanges(Deleted) - Data [String1]{0} ,i)
                Assert.Equal(drArr1[i]["String1", DataRowVersion.Original], drArr2[i]["String1", DataRowVersion.Original]);
                // GetChanges(Deleted) - Data [String2]{0} ,i)
                Assert.Equal(drArr1[i]["String2", DataRowVersion.Original], drArr2[i]["String2", DataRowVersion.Original]);
            }

            // *********** Checking GetChanges - DataRowState.Modified ************
            dt1 = null;
            dt1 = dt2.GetChanges(DataRowState.Modified);
            CheckTableSchema(dt1, dt2, DataRowState.Modified.ToString());
            drArr1 = dt1.Select(string.Empty, string.Empty);
            drArr2 = dt2.Select(string.Empty, string.Empty, DataViewRowState.ModifiedCurrent);

            for (int i = 0; i < drArr1.Length; i++)
            {
                // GetChanges(Modified) - Data [ParentId]{0} ,i)
                Assert.Equal(drArr2[i]["ParentId"], drArr1[i]["ParentId"]);
                // GetChanges(Modified) - Data [String1]{0} ,i)
                Assert.Equal(drArr2[i]["String1"], drArr1[i]["String1"]);
                // GetChanges(Modified) - Data [String2]{0} ,i)
                Assert.Equal(drArr2[i]["String2"], drArr1[i]["String2"]);
            }

            // *********** Checking GetChanges - DataRowState.Added ************
            dt1 = null;
            dt1 = dt2.GetChanges(DataRowState.Added);
            CheckTableSchema(dt1, dt2, DataRowState.Added.ToString());
            drArr1 = dt1.Select(string.Empty, string.Empty);
            drArr2 = dt2.Select(string.Empty, string.Empty, DataViewRowState.Added);

            for (int i = 0; i < drArr1.Length; i++)
            {
                // GetChanges(Added) - Data [ParentId]{0} ,i)
                Assert.Equal(drArr2[i]["ParentId"], drArr1[i]["ParentId"]);
                // GetChanges(Added) - Data [String1]{0} ,i)
                Assert.Equal(drArr2[i]["String1"], drArr1[i]["String1"]);
                // GetChanges(Added) - Data [String2]{0} ,i)
                Assert.Equal(drArr2[i]["String2"], drArr1[i]["String2"]);
            }

            // *********** Checking GetChanges - DataRowState.Unchanged  ************
            dt1 = null;
            dt1 = dt2.GetChanges(DataRowState.Unchanged);
            CheckTableSchema(dt1, dt2, DataRowState.Unchanged.ToString());
            drArr1 = dt1.Select(string.Empty, string.Empty);
            drArr2 = dt2.Select(string.Empty, string.Empty, DataViewRowState.Unchanged);

            for (int i = 0; i < drArr1.Length; i++)
            {
                // GetChanges(Unchanged) - Data [ParentId]{0} ,i)
                Assert.Equal(drArr2[i]["ParentId"], drArr1[i]["ParentId"]);
                // GetChanges(Unchanged) - Data [String1]{0} ,i)
                Assert.Equal(drArr2[i]["String1"], drArr1[i]["String1"]);
                // GetChanges(Unchanged) - Data [String2]{0} ,i)
                Assert.Equal(drArr2[i]["String2"], drArr1[i]["String2"]);
            }
        }

        private void CheckTableSchema(DataTable dt1, DataTable dt2, string Description)
        {
            for (int i = 0; i < dt2.Constraints.Count; i++)
            {
                // GetChanges - Constraints[{0}] - {1},i,Description)
                Assert.Equal(dt2.Constraints[i].ConstraintName,
                    dt1.Constraints[i].ConstraintName);
            }

            for (int i = 0; i < dt2.Columns.Count; i++)
            {
                // GetChanges - Columns[{0}].ColumnName - {1},i,Description)
                Assert.Equal(dt2.Columns[i].ColumnName, dt1.Columns[i].ColumnName);

                // GetChanges - Columns[{0}].DataType {1},i,Description)
                Assert.Equal(dt2.Columns[i].DataType, dt1.Columns[i].DataType);
            }
        }

        [Fact]
        public void GetErrors()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataRow[] drArr = new DataRow[3];
            drArr[0] = dt.Rows[0];
            drArr[1] = dt.Rows[2];
            drArr[2] = dt.Rows[5];

            drArr[0].RowError = "Error1";
            drArr[1].RowError = "Error2";
            drArr[2].RowError = "Error3";

            // GetErrors
            Assert.Equal(dt.GetErrors(), drArr);
        }

        [Fact]
        public new void GetHashCode()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            int iHashCode;
            iHashCode = dt.GetHashCode();

            for (int i = 0; i < 10; i++)
            {
                // HashCode - i= + i.ToString()
                Assert.Equal(dt.GetHashCode(), iHashCode);
            }
        }

        [Fact]
        public new void GetType()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            Type tmpType = typeof(DataTable);

            // GetType
            Assert.Equal(tmpType, dt.GetType());
        }

        [Fact]
        public void HasErrors()
        {
            DataTable dtParent;
            dtParent = DataProvider.CreateParentDataTable();

            // Checking HasErrors default 
            Assert.Equal(false, dtParent.HasErrors);

            // Checking HasErrors Get 
            dtParent.Rows[0].RowError = "Error on row 0";
            dtParent.Rows[2].RowError = "Error on row 2";
            Assert.Equal(true, dtParent.HasErrors);
        }

        [Fact]
        public void ImportRow()
        {
            DataTable dt1, dt2;
            dt1 = DataProvider.CreateParentDataTable();
            dt2 = DataProvider.CreateParentDataTable();
            DataRow dr = dt2.NewRow();
            dr.ItemArray = new object[] { 99, string.Empty, string.Empty };
            dt2.Rows.Add(dr);

            // ImportRow - Values
            dt1.ImportRow(dr);
            Assert.Equal(dr.ItemArray, dt1.Rows[dt1.Rows.Count - 1].ItemArray);

            // ImportRow - DataRowState
            Assert.Equal(dr.RowState, dt1.Rows[dt1.Rows.Count - 1].RowState);
        }

        [Fact]
        public void LoadDataRow()
        {
            DataTable dt;
            DataRow dr;
            dt = DataProvider.CreateParentDataTable();
            dt.PrimaryKey = new DataColumn[] { dt.Columns[0] }; //add ParentId as Primary Key
            dt.Columns["String1"].DefaultValue = "Default";

            dr = dt.Select("ParentId=1")[0];

            //Update existing row without accept changes
            dt.BeginLoadData();
            dt.LoadDataRow(new object[] { 1, null, "Changed" }, false);
            dt.EndLoadData();

            // LoadDataRow(update1) - check column String1
            Assert.Equal(dr["String1"], dt.Columns["String1"].DefaultValue);

            // LoadDataRow(update1) - check column String2
            Assert.Equal(dr["String2"], "Changed");

            // LoadDataRow(update1) - check row state
            Assert.Equal(DataRowState.Modified, dr.RowState);

            //Update existing row with accept changes
            dr = dt.Select("ParentId=2")[0];

            dt.BeginLoadData();
            dt.LoadDataRow(new object[] { 2, null, "Changed" }, true);
            dt.EndLoadData();

            // LoadDataRow(update2) - check row state
            Assert.Equal(DataRowState.Unchanged, dr.RowState);

            //Add New row without accept changes
            dt.BeginLoadData();
            dt.LoadDataRow(new object[] { 99, null, "Changed" }, false);
            dt.EndLoadData();

            // LoadDataRow(insert1) - check column String2
            dr = dt.Select("ParentId=99")[0];
            Assert.Equal("Changed", dr["String2"]);

            // LoadDataRow(insert1) - check row state
            Assert.Equal(DataRowState.Added, dr.RowState);

            //Add New row with accept changes
            dt.BeginLoadData();
            dt.LoadDataRow(new object[] { 100, null, "Changed" }, true);
            dt.EndLoadData();

            // LoadDataRow(insert2) - check row and values
            dr = dt.Select("ParentId=100")[0];
            Assert.Equal("Changed", dr["String2"]);

            // LoadDataRow(insert2) - check row state
            Assert.Equal(DataRowState.Unchanged, dr.RowState);
        }

        [Fact]
        public void Locale()
        {
            DataTable dtParent;
            DataSet ds = new DataSet("MyDataSet");

            dtParent = DataProvider.CreateParentDataTable();
            ds.Tables.Add(dtParent);
            CultureInfo culInfo = CultureInfo.CurrentCulture;

            // Checking Locale default from system
            Assert.Equal(culInfo, dtParent.Locale);

            // Checking Locale default from dataset
            culInfo = new CultureInfo("fr");
            ds.Locale = culInfo;
            Assert.Equal(culInfo, dtParent.Locale);

            // Checking Locale get/set
            culInfo = new CultureInfo("nl-BE");
            dtParent.Locale = culInfo;
            Assert.Equal(culInfo, dtParent.Locale);
        }

        [Fact]
        public void MinimumCapacity()
        {
            //				i get default=50, according to MSDN the value should be 25 
            //				// Checking MinimumCapacity default = 25 
            //				Assert.Equal(25, dtParent.MinimumCapacity);
            //				EndCase(null);
            DataTable dt = new DataTable();

            // Checking MinimumCapacity get/set int.MaxValue 
            dt.MinimumCapacity = int.MaxValue;
            Assert.Equal(int.MaxValue, dt.MinimumCapacity);

            // Checking MinimumCapacity get/set 0
            dt.MinimumCapacity = 0;
            Assert.Equal(0, dt.MinimumCapacity);

            //				// Checking MinimumCapacity get/set int.MinValue 
            //				dtParent.MinimumCapacity = int.MinValue;
            //				Assert.Equal(int.MinValue, dtParent.MinimumCapacity);
            //				EndCase(null);
        }

        [Fact]
        public void Namespace()
        {
            DataTable dtParent = new DataTable();

            // Checking Namespace default
            Assert.Equal(string.Empty, dtParent.Namespace);

            // Checking Namespace set/get
            string s = "MyNamespace";
            dtParent.Namespace = s;
            Assert.Equal(s, dtParent.Namespace);
        }

        [Fact]
        public void NewRow()
        {
            DataTable dt;
            DataRow dr;
            dt = DataProvider.CreateParentDataTable();

            // NewRow
            dr = dt.NewRow();
            Assert.NotNull(dr);
        }

        [Fact]
        public void OnColumnChanged()
        {
            ProtectedTestClass dt = new ProtectedTestClass();

            _eventRaised = false;
            dt.OnColumnChanged_Test();
            // OnColumnChanged Event 1
            Assert.Equal(false, _eventRaised);
            _eventRaised = false;
            _eventValues = false;
            dt.ColumnChanged += new DataColumnChangeEventHandler(OnColumnChanged_Handler);
            dt.OnColumnChanged_Test();
            // OnColumnChanged Event 2
            Assert.Equal(true, _eventRaised);
            // OnColumnChanged Values
            Assert.Equal(true, _eventValues);
            dt.ColumnChanged -= new DataColumnChangeEventHandler(OnColumnChanged_Handler);
        }

        private void OnColumnChanged_Handler(object sender, DataColumnChangeEventArgs e)
        {
            DataTable dt = (DataTable)sender;
            if ((e.Column.Equals(dt.Columns["Value"])) && (e.Row.Equals(dt.Rows[0])) && (e.ProposedValue.Equals("NewValue")))
            {
                _eventValues = true;
            }
            else
            {
                _eventValues = false;
            }
            _eventRaised = true;
        }

        [Fact]
        public void OnColumnChanging()
        {
            ProtectedTestClass dt = new ProtectedTestClass();

            _eventRaised = false;
            dt.OnColumnChanging_Test();
            // OnColumnChanging Event 1
            Assert.Equal(false, _eventRaised);
            _eventRaised = false;
            _eventValues = false;
            dt.ColumnChanging += new DataColumnChangeEventHandler(OnColumnChanging_Handler);
            dt.OnColumnChanging_Test();
            // OnColumnChanging Event 2
            Assert.Equal(true, _eventRaised);
            // OnColumnChanging Values
            Assert.Equal(true, _eventValues);
            dt.ColumnChanging -= new DataColumnChangeEventHandler(OnColumnChanging_Handler);
        }

        private void OnColumnChanging_Handler(object sender, DataColumnChangeEventArgs e)
        {
            DataTable dt = (DataTable)sender;
            if ((e.Column.Equals(dt.Columns["Value"])) && (e.Row.Equals(dt.Rows[0])) && (e.ProposedValue.Equals("NewValue")))
            {
                _eventValues = true;
            }
            else
            {
                _eventValues = false;
            }
            _eventRaised = true;
        }

        [Fact]
        public void OnRemoveColumn()
        {
            ProtectedTestClass dt = new ProtectedTestClass();
            dt.OnRemoveColumn_Test();
        }

        [Fact]
        public void ParentRelations()
        {
            DataTable dtChild, dtParent;
            var ds = new DataSet();
            //Create tables
            dtChild = DataProvider.CreateChildDataTable();
            dtParent = DataProvider.CreateParentDataTable();
            //Add tables to dataset
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            DataRelationCollection drlCollection;
            DataRelation drl = new DataRelation("Parent-Child", dtParent.Columns["ParentId"], dtChild.Columns["ParentId"]);

            // Checking ParentRelations - default value
            //Check default
            drlCollection = dtChild.ParentRelations;
            Assert.Equal(0, drlCollection.Count);

            ds.Relations.Add(drl);
            drlCollection = dtChild.ParentRelations;

            // Checking ParentRelations Count
            Assert.Equal(1, drlCollection.Count);

            // Checking ParentRelations Value
            Assert.Equal(drl, drlCollection[0]);
        }

        [Fact]
        public void Prefix()
        {
            DataTable dtParent = new DataTable();

            // Checking Prefix default
            Assert.Equal(string.Empty, dtParent.Prefix);

            // Checking Prefix set/get
            string s = "MyPrefix";
            dtParent.Prefix = s;
            Assert.Equal(s, dtParent.Prefix);
        }

        [Fact]
        public void RejectChanges()
        {
            string sNewValue = "NewValue";
            DataRow drModified, drDeleted, drAdded;
            DataTable dt = DataProvider.CreateParentDataTable();

            drModified = dt.Rows[0];
            drModified[1] = sNewValue; //DataRowState = Modified, DataRowVersion = Proposed

            drDeleted = dt.Rows[1];
            drDeleted.Delete(); //DataRowState =  Deleted

            drAdded = dt.NewRow();
            dt.Rows.Add(drAdded); //DataRowState =  Added

            dt.RejectChanges();

            // RejectChanges - Unchanged1
            Assert.Equal(DataRowState.Unchanged, drModified.RowState);

            // RejectChanges - Unchanged2
            Assert.Equal(DataRowState.Detached, drAdded.RowState);

            // RejectChanges - Detached
            Assert.Equal(DataRowState.Unchanged, drDeleted.RowState);
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

            dt2.Reset();

            // Reset - ParentRelations
            Assert.Equal(0, dt2.ParentRelations.Count);
            // Reset - Constraints
            Assert.Equal(0, dt2.Constraints.Count);
            // Reset - Rows
            Assert.Equal(0, dt2.Rows.Count);
            // Reset - Columns
            Assert.Equal(0, dt2.Columns.Count);
        }

        [Fact]
        public void RowChanged()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.RowChanged += new DataRowChangeEventHandler(Row_Changed);

            _EventTriggered = false;
            // RowChanged - 1
            dt.Rows[0][1] = "NewValue";
            Assert.Equal(true, _EventTriggered);

            _EventTriggered = false;
            // RowChanged - 2
            dt.Rows[0].BeginEdit();
            dt.Rows[0][1] = "NewValue";
            Assert.Equal(false, _EventTriggered);

            _EventTriggered = false;
            // RowChanged - 3
            dt.Rows[0].EndEdit();
            Assert.Equal(true, _EventTriggered);

            _EventTriggered = false;
            dt.RowChanged -= new DataRowChangeEventHandler(Row_Changed);
            // RowChanged - 4
            dt.Rows[0][1] = "NewValue A";
            Assert.Equal(false, _EventTriggered);
        }

        private void Row_Changed(object sender, DataRowChangeEventArgs e)
        {
            _EventTriggered = true;
        }

        [Fact]
        public void RowChanging()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.RowChanging += new DataRowChangeEventHandler(Row_Changing);

            _EventTriggered = false;
            // RowChanging - 1
            dt.Rows[0][1] = "NewValue";
            Assert.Equal(true, _EventTriggered);

            _EventTriggered = false;
            // RowChanging - 2
            dt.Rows[0].BeginEdit();
            dt.Rows[0][1] = "NewValue";
            Assert.Equal(false, _EventTriggered);

            _EventTriggered = false;
            // RowChanging - 3
            dt.Rows[0].EndEdit();
            Assert.Equal(true, _EventTriggered);

            _EventTriggered = false;
            dt.RowChanging -= new DataRowChangeEventHandler(Row_Changing);
            // RowChanging - 4
            dt.Rows[0][1] = "NewValue A";
            Assert.Equal(false, _EventTriggered);
        }

        private void Row_Changing(object sender, DataRowChangeEventArgs e)
        {
            _EventTriggered = true;
        }

        [Fact]
        public void RowDeleted()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.RowDeleted += new DataRowChangeEventHandler(Row_Deleted);

            _EventTriggered = false;
            // RowDeleted - 1
            dt.Rows[0].Delete();
            Assert.True(_EventTriggered);

            _EventTriggered = false;
            dt.RowDeleted -= new DataRowChangeEventHandler(Row_Deleted);
            // RowDeleted - 2
            dt.Rows[1].Delete();
            Assert.False(_EventTriggered);
        }

        private void Row_Deleted(object sender, DataRowChangeEventArgs e)
        {
            _EventTriggered = true;
        }

        [Fact]
        public void RowDeleting()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            dt.RowDeleting += new DataRowChangeEventHandler(Row_Deleting);

            _EventTriggered = false;
            // RowDeleting - 1
            dt.Rows[0].Delete();
            Assert.True(_EventTriggered);

            _EventTriggered = false;
            dt.RowDeleting -= new DataRowChangeEventHandler(Row_Deleting);
            // RowDeleting - 2
            dt.Rows[1].Delete();
            Assert.False(_EventTriggered);
        }

        private void Row_Deleting(object sender, DataRowChangeEventArgs e)
        {
            _EventTriggered = true;
        }

        [Fact]
        public void Rows()
        {
            DataTable dtParent;
            dtParent = DataProvider.CreateParentDataTable();

            // Checking Rows
            Assert.NotNull(dtParent.Rows);

            // Checking rows count
            Assert.True(dtParent.Rows.Count > 0);
        }

        [Fact]
        public void Select()
        {
            DataTable dt = DataProvider.CreateParentDataTable();

            DataRow[] drSelect = dt.Select();
            DataRow[] drResult = new DataRow[dt.Rows.Count];
            dt.Rows.CopyTo(drResult, 0);

            // Select
            Assert.Equal(drResult, drSelect);
        }

        [Fact]
        public void Select_ByFilter()
        {
            var ds = new DataSet();
            ds.Tables.Add(DataProvider.CreateParentDataTable());

            DataTable dt = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dt);
            DataRow[] drSelect = null;
            var al = new List<DataRow>();

            //add column with special name
            DataColumn dc = new DataColumn("Column#", typeof(int));
            dc.DefaultValue = -1;
            dt.Columns.Add(dc);
            //put some values
            dt.Rows[0][dc] = 100;
            dt.Rows[1][dc] = 200;
            dt.Rows[2][dc] = 300;
            dt.Rows[4][dc] = -400;

            //for trim function
            dt.Rows[0]["String1"] = dt.Rows[0]["String1"] + "   \t\n ";
            dt.Rows[0]["String1"] = "   \t\n " + dt.Rows[0]["String1"];
            dt.Rows[0]["String1"] = dt.Rows[0]["String1"] + "    ";

            ds.Tables[0].Rows[0]["ParentBool"] = DBNull.Value;
            ds.Tables[0].Rows[2]["ParentBool"] = DBNull.Value;
            ds.Tables[0].Rows[3]["ParentBool"] = DBNull.Value;

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
            {
                if ((int)dr["ChildId"] == 1)
                    al.Add(dr);
            }
            // Select_S - ChildId=1
            drSelect = dt.Select("ChildId=1");
            Assert.Equal(al.ToArray(), drSelect);

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
            {
                if ((int)dr["ChildId"] == 1)
                    al.Add(dr);
            }
            // Select_S - ChildId='1'
            drSelect = dt.Select("ChildId='1'");
            Assert.Equal(al.ToArray(), drSelect);
            //-------------------------------------------------------------
            // Select_S - ChildId= '1'  (whitespace in filter string.
            drSelect = dt.Select("ChildId= '1'");
            Assert.Equal(al.ToArray(), drSelect);
            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if (dr["String1"].ToString() == "1-String1")
                    al.Add(dr);
            // Select_S - String1='1-String1'
            drSelect = dt.Select("String1='1-String1'");
            Assert.Equal(al.ToArray(), drSelect);

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if ((int)dr["ChildId"] == 1 && dr["String1"].ToString() == "1-String1")
                    al.Add(dr);
            // Select_S - ChildId=1 and String1='1-String1'
            drSelect = dt.Select("ChildId=1 and String1='1-String1'");
            Assert.Equal(al.ToArray(), drSelect);

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if ((int)dr["ChildId"] + (int)dr["ParentId"] >= 4)
                    al.Add(dr);
            // Select_S - ChildId+ParentId >= 4
            drSelect = dt.Select("ChildId+ParentId >= 4");
            CompareUnSorted(drSelect, al.ToArray());

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
            {
                if ((((int)dr["ChildId"] - (int)dr["ParentId"]) * -1) != 0)
                    al.Add(dr);
            }
            // Select_S - ChildId-ParentId) * -1  <> 0
            drSelect = dt.Select("(ChildId-ParentId) * -1  <> 0");
            CompareUnSorted(drSelect, al.ToArray());

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if ((double)dr["ChildDouble"] < ((int)dr["ParentId"]) % 4)
                    al.Add(dr);
            // Select_S - ChildDouble < ParentId % 4
            drSelect = dt.Select("ChildDouble < ParentId % 4");
            CompareUnSorted(drSelect, al.ToArray());

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if ((double)dr["ChildDouble"] == 10 || (double)dr["ChildDouble"] == 20 || (double)dr["ChildDouble"] == 25)
                    al.Add(dr);
            // Select_S - ChildDouble in (10,20,25)
            drSelect = dt.Select("ChildDouble in (10,20,25)");
            CompareUnSorted(drSelect, al.ToArray());

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if (dr["String2"].ToString().IndexOf("1-S") >= 0)
                    al.Add(dr);
            // Select_S - String2 like '%1-S%'
            drSelect = dt.Select("String2 like '%1-S%'");
            Assert.Equal(al.ToArray(), drSelect);

            //-------------------------------------------------------------
            //If a column name contains one of the above characters,(ex. #\/=><+-*%&|^'" and so on) the name must be wrapped in brackets. For example to use a column named "Column#" in an expression, you would write "[Column#]":
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if ((int)dr["Column#"] <= 0)
                    al.Add(dr);
            // Select_S - [Column#] <= 0 
            drSelect = dt.Select("[Column#] <= 0 ");
            CompareUnSorted(drSelect, al.ToArray());
            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if ((int)dr["Column#"] <= 0)
                    al.Add(dr);
            // Select_S - [Column#] <= 0
            drSelect = dt.Select("[Column#] <= 0");
            CompareUnSorted(drSelect, al.ToArray());

            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if (((DateTime)dr["ChildDateTime"]).CompareTo(new DateTime(2000, 12, 12)) > 0)
                    al.Add(dr);
            // Select_S - ChildDateTime > #12/12/2000# 
            drSelect = dt.Select("ChildDateTime > #12/12/2000# ");
            CompareUnSorted(drSelect, al.ToArray());

            //-------------------------------------------------------------

            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if (((DateTime)dr["ChildDateTime"]).CompareTo(new DateTime(1999, 1, 12, 12, 06, 30)) > 0)
                    al.Add(dr);
            // Select_S - ChildDateTime > #1/12/1999 12:06:30 PM#  
            drSelect = dt.Select("ChildDateTime > #1/12/1999 12:06:30 PM#  ");
            CompareUnSorted(drSelect, al.ToArray());

            //-------------------------------------------------------------

            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if (((DateTime)dr["ChildDateTime"]).CompareTo(new DateTime(2005, 12, 03, 17, 06, 30)) >= 0 || ((DateTime)dr["ChildDateTime"]).CompareTo(new DateTime(1980, 11, 03)) <= 0)
                    al.Add(dr);
            // Select_S - ChildDateTime >= #12/3/2005 5:06:30 PM# or ChildDateTime <= #11/3/1980#  
            drSelect = dt.Select("ChildDateTime >= #12/3/2005 5:06:30 PM# or ChildDateTime <= #11/3/1980#  ");
            CompareUnSorted(drSelect, al.ToArray());

#if LATER
			//-------------------------------------------------------------
			al.Clear();
			foreach (DataRow dr in dt.Rows)
				if (dr["ChildDouble"].ToString().Length > 10)
					al.Add(dr);
				// Select_S - Len(Convert(ChildDouble,'System.String')) > 10
				drSelect = dt.Select ("Len(Convert(ChildDouble,'System.String')) > 10");
				Assert.Equal (al.ToArray(), drSelect);
#endif
            //-------------------------------------------------------------
            al.Clear();
            foreach (DataRow dr in dt.Rows)
                if (dr["String1"].ToString().Trim().Substring(0, 2) == "1-")
                    al.Add(dr);
            // Select_S - SubString(Trim(String1),1,2) = '1-'
            drSelect = dt.Select("SubString(Trim(String1),1,2) = '1-'");
            Assert.Equal(al.ToArray(), drSelect);
            //-------------------------------------------------------------
            /*
			al.Clear();
			foreach (DataRow dr in ds.Tables[0].Rows)
				if (dr.IsNull("ParentBool") || (bool)dr["ParentBool"])
					al.Add(dr);
				// Select_S - IsNull(ParentBool,true)
				drSelect = ds.Tables[0].Select("IsNull(ParentBool,true) ");
				Assert.Equal (al.ToArray(), drSelect);
			*/
            //-------------------------------------------------------------
            al.Clear();
            // Select_S - Relation not exists, Exception
            try
            {
                drSelect = dt.Select("Parent.ParentId = ChildId");
                Assert.False(true);
            }
            catch (IndexOutOfRangeException ex)
            {
                // Cannot find relation 0
                Assert.Equal(typeof(IndexOutOfRangeException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
            //-------------------------------------------------------------
            al.Clear();
            ds.Relations.Add(new DataRelation("ParentChild", ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]));
            foreach (DataRow dr in dt.Rows)
                if ((int)dr["ChildId"] == (int)dr.GetParentRow("ParentChild")["ParentId"])
                    al.Add(dr);
            // Select_S - Parent.ParentId = ChildId
            drSelect = dt.Select("Parent.ParentId = ChildId");
            Assert.Equal(al.ToArray(), drSelect);
        }

        private void CompareUnSorted(Array a, Array b)
        {
            string msg = string.Format("Failed while comparing(Array a ={0} ({1}), Array b = {2} ({3}))]", a.ToString(), a.GetType().FullName, b.ToString(), b.GetType().FullName);
            foreach (object item in a)
            {
                if (Array.IndexOf(b, item) < 0) //b does not contain the current item.
                    Assert.False(true);
            }

            foreach (object item in b)
            {
                if (Array.IndexOf(a, item) < 0) //a does not contain the current item.
                    Assert.False(true);
            }
        }

        [Fact]
        public void Select_ByFilterDataViewRowState()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataRow[] drSelect, drResult;

            dt.Rows[0].Delete();
            dt.Rows[1]["ParentId"] = 1;
            dt.Rows[2]["ParentId"] = 1;
            dt.Rows[3].Delete();
            dt.Rows.Add(new object[] { 1, "A", "B" });
            dt.Rows.Add(new object[] { 1, "C", "D" });
            dt.Rows.Add(new object[] { 1, "E", "F" });

            drSelect = dt.Select("ParentId=1", string.Empty, DataViewRowState.Added);
            drResult = GetResultRows(dt, DataRowState.Added);
            // Select_SSD DataViewRowState.Added
            Assert.Equal(drResult, drSelect);

            drSelect = dt.Select("ParentId=1", string.Empty, DataViewRowState.CurrentRows);
            drResult = GetResultRows(dt, DataRowState.Unchanged | DataRowState.Added | DataRowState.Modified);
            // Select_SSD DataViewRowState.CurrentRows
            Assert.Equal(drResult, drSelect);

            drSelect = dt.Select("ParentId=1", string.Empty, DataViewRowState.Deleted);
            drResult = GetResultRows(dt, DataRowState.Deleted);
            // Select_SSD DataViewRowState.Deleted
            Assert.Equal(drResult, drSelect);

            drSelect = dt.Select("ParentId=1", string.Empty, DataViewRowState.ModifiedCurrent | DataViewRowState.ModifiedOriginal);
            drResult = GetResultRows(dt, DataRowState.Modified);
            // Select_SSD ModifiedCurrent or ModifiedOriginal
            Assert.Equal(drResult, drSelect);
        }

        private DataRow[] GetResultRows(DataTable dt, DataRowState State)
        {
            ArrayList al = new ArrayList();
            DataRowVersion drVer = DataRowVersion.Current;

            //From MSDN -	The row the default version for the current DataRowState.
            //				For a DataRowState value of Added, Modified or Current, 
            //				the default version is Current. 
            //				For a DataRowState of Deleted, the version is Original.
            //				For a DataRowState value of Detached, the version is Proposed.

            if (((State & DataRowState.Added) > 0)
                | ((State & DataRowState.Modified) > 0)
                | ((State & DataRowState.Unchanged) > 0))
                drVer = DataRowVersion.Current;
            if ((State & DataRowState.Deleted) > 0
                | (State & DataRowState.Detached) > 0)
                drVer = DataRowVersion.Original;

            foreach (DataRow dr in dt.Rows)
            {
                if (dr.HasVersion(drVer) && ((int)dr["ParentId", drVer] == 1) && ((dr.RowState & State) > 0))
                    al.Add(dr);
            }
            DataRow[] result = (DataRow[])al.ToArray((typeof(DataRow)));
            return result;
        }

        [Fact]
        public void TableName()
        {
            DataTable dtParent = new DataTable();

            // Checking TableName default
            Assert.Equal(string.Empty, dtParent.TableName);

            // Checking TableName set/get
            string s = "MyTable";
            dtParent.TableName = s;
            Assert.Equal(s, dtParent.TableName);
        }

        [Fact]
        public new void ToString()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.DisplayExpression = dt.Columns[0].ColumnName;

            string sToString = dt.TableName + " + " + dt.DisplayExpression;
            Assert.Equal(sToString, dt.ToString());
        }

        [Fact]
        public void CaseSensitive()
        {
            DataTable dtParent = new DataTable();

            // Checking default
            Assert.False(dtParent.CaseSensitive);

            // Checking set/get
            dtParent.CaseSensitive = true;
            Assert.True(dtParent.CaseSensitive);
        }

        [Fact]
        public void ctor()
        {
            DataTable dt = new DataTable();
            Assert.NotNull(dt);
        }

        [Fact]
        public void ctor_ByName()
        {
            string sName = "MyName";

            DataTable dt = new DataTable(sName);

            Assert.NotNull(dt);
            Assert.Equal(sName, dt.TableName);
        }

        [Fact]
        public void DisplayExpression()
        {
            DataTable dtParent;
            dtParent = DataProvider.CreateParentDataTable();

            // Checking DisplayExpression default 
            Assert.Equal(string.Empty, dtParent.DisplayExpression);

            // Checking DisplayExpression Set/Get 
            dtParent.DisplayExpression = dtParent.Columns[0].ColumnName;
            Assert.Equal(dtParent.Columns[0].ColumnName, dtParent.DisplayExpression);
        }

        [Fact]
        public void ExtendedProperties()
        {
            DataTable dtParent;
            PropertyCollection pc;
            dtParent = DataProvider.CreateParentDataTable();

            pc = dtParent.ExtendedProperties;

            // Checking ExtendedProperties default
            Assert.NotNull(pc);

            // Checking ExtendedProperties count
            Assert.Equal(0, pc.Count);
        }

        [Fact]
        public void Compute_WithoutSchemaData_Test()
        {
            DataSet ds = new DataSet("TestData");
            DataTable table = ds.Tables.Add("TestTable");

            table.Columns.Add("Id");
            table.Columns.Add("Value");

            table.Rows.Add(new object[] { "1", "4.5" });
            table.Rows.Add(new object[] { "2", "7.5" });
            table.Rows.Add(new object[] { "3", "2.5" });
            table.Rows.Add(new object[] { "4", "3.5" });

            Assert.Equal("1", table.Compute("Min(Id)", string.Empty));
            Assert.Equal("4", table.Compute("Max(Id)", string.Empty));
            Assert.Equal("2.5", table.Compute("Min(Value)", string.Empty));
            Assert.Equal("7.5", table.Compute("Max(Value)", string.Empty));
        }

        [Fact]
        public void BeginLoadData()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            dt.Columns[0].AllowDBNull = false;

            try
            {
                //if BeginLoadData has not been called, an exception will be throw
                dt.LoadDataRow(new object[] { null, "A", "B" }, false);
                Assert.False(true);
            }
            catch (NoNullAllowedException ex)
            {
                // Column 'ParentId' does not allow nulls
                Assert.Equal(typeof(NoNullAllowedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + "ParentId" + @"[\p{Pf}\p{Po}]", ex.Message);
            }

            DataTable dt1 = DataProvider.CreateUniqueConstraint();

            dt1.BeginLoadData();

            DataRow dr = dt1.NewRow();
            dr[0] = 3;
            dt1.Rows.Add(dr);

            try
            {
                dt1.EndLoadData();
                Assert.False(true);
            }
            catch (ConstraintException ex)
            {
                // Failed to enable constraints. One or more rows
                // contain values violating non-null, unique, or
                // foreign-key constraints
                Assert.Equal(typeof(ConstraintException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                Assert.Equal(2, dt1.GetErrors().Length);
                Assert.True(dt1.GetErrors()[0].RowError.Length > 10);
                Assert.True(dt1.GetErrors()[1].RowError.Length > 10);
            }

            DataSet ds = DataProvider.CreateForeignConstraint();
            ds.Tables[0].BeginLoadData();
            ds.Tables[0].Rows[0][0] = 10; //Foreign constraint violation

            try
            {
                ds.Tables[0].EndLoadData();
                Assert.False(true);
            }
            catch (ConstraintException ex)
            {
                // Failed to enable constraints. One or more
                // rows contain values violating non-null,
                // unique, or foreign-key constraints
                Assert.Equal(typeof(ConstraintException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);

                Assert.Equal(3, ds.Tables[1].GetErrors().Length);
                for (int index = 0; index < 3; index++)
                    Assert.True(ds.Tables[1].GetErrors()[index].RowError.Length > 10);
            }
        }

        private DataRowAction _drExpectedAction;

        [Fact]
        public void OnRowChanged()
        {
            ProtectedTestClass dt = new ProtectedTestClass();

            _eventRaised = false;
            dt.OnRowChanged_Test(DataRowAction.Nothing);

            Assert.False(_eventRaised);

            dt.RowChanged += new DataRowChangeEventHandler(OnRowChanged_Handler);
            foreach (int i in Enum.GetValues(typeof(DataRowAction)))
            {
                _eventRaised = false;
                _eventValues = false;
                _drExpectedAction = (DataRowAction)i;
                dt.OnRowChanged_Test(_drExpectedAction);

                Assert.True(_eventRaised);
                Assert.True(_eventValues);
            }
            dt.RowChanged -= new DataRowChangeEventHandler(OnRowChanged_Handler);
        }

        private void OnRowChanged_Handler(object sender, DataRowChangeEventArgs e)
        {
            DataTable dt = (DataTable)sender;
            if (dt.Rows[0].Equals(e.Row) && e.Action == _drExpectedAction)
                _eventValues = true;
            _eventRaised = true;
        }

        [Fact]
        public void OnRowChanging()
        {
            ProtectedTestClass dt = new ProtectedTestClass();

            _eventRaised = false;
            dt.OnRowChanging_Test(DataRowAction.Nothing);

            Assert.False(_eventRaised);

            dt.RowChanging += new DataRowChangeEventHandler(OnRowChanging_Handler);
            foreach (int i in Enum.GetValues(typeof(DataRowAction)))
            {
                _eventRaised = false;
                _eventValues = false;
                _drExpectedAction = (DataRowAction)i;
                dt.OnRowChanging_Test(_drExpectedAction);

                Assert.True(_eventRaised);
                Assert.True(_eventValues);
            }
            dt.RowChanging -= new DataRowChangeEventHandler(OnRowChanging_Handler);
        }

        private void OnRowChanging_Handler(object sender, DataRowChangeEventArgs e)
        {
            DataTable dt = (DataTable)sender;
            if (dt.Rows[0].Equals(e.Row) && e.Action == _drExpectedAction)
                _eventValues = true;
            _eventRaised = true;
        }

        [Fact]
        public void OnRowDeleted()
        {
            ProtectedTestClass dt = new ProtectedTestClass();

            _eventRaised = false;
            dt.OnRowDeleted_Test(DataRowAction.Nothing);

            Assert.False(_eventRaised);

            dt.RowDeleted += new DataRowChangeEventHandler(OnRowDeleted_Handler);
            foreach (int i in Enum.GetValues(typeof(DataRowAction)))
            {
                _eventRaised = false;
                _eventValues = false;
                _drExpectedAction = (DataRowAction)i;
                dt.OnRowDeleted_Test(_drExpectedAction);

                Assert.True(_eventRaised);
                Assert.True(_eventValues);
            }
            dt.RowDeleted -= new DataRowChangeEventHandler(OnRowDeleted_Handler);
        }

        private void OnRowDeleted_Handler(object sender, DataRowChangeEventArgs e)
        {
            DataTable dt = (DataTable)sender;
            if (dt.Rows[0].Equals(e.Row) && e.Action == _drExpectedAction)
                _eventValues = true;
            _eventRaised = true;
        }

        [Fact]
        public void OnRowDeleting()
        {
            ProtectedTestClass dt = new ProtectedTestClass();

            _eventRaised = false;
            dt.OnRowDeleting_Test(DataRowAction.Nothing);

            Assert.False(_eventRaised);

            dt.RowDeleting += new DataRowChangeEventHandler(OnRowDeleting_Handler);
            foreach (int i in Enum.GetValues(typeof(DataRowAction)))
            {
                _eventRaised = false;
                _eventValues = false;
                _drExpectedAction = (DataRowAction)i;
                dt.OnRowDeleting_Test(_drExpectedAction);

                Assert.True(_eventRaised);
                Assert.True(_eventValues);
            }
            dt.RowDeleting -= new DataRowChangeEventHandler(OnRowDeleting_Handler);
        }

        [Fact]
        public void BeginInit_PrimaryKey_1()
        {
            DataTable table = new DataTable();
            DataColumn col = table.Columns.Add("col", typeof(int));
            table.PrimaryKey = new DataColumn[] { col };
            table.AcceptChanges();
            Assert.Equal(1, table.PrimaryKey.Length);

            table.BeginInit();
            DataColumn col2 = new DataColumn("col2", typeof(int));
            table.Columns.AddRange(new DataColumn[] { col2 });
            table.PrimaryKey = new DataColumn[] { col2 };
            table.EndInit();
            Assert.Equal(1, table.PrimaryKey.Length);
            Assert.Equal("col2", table.PrimaryKey[0].ColumnName);
        }

        [Fact]
        public void BeginInit_PrimaryKey_2()
        {
            DataTable table = new DataTable();
            DataColumn col = table.Columns.Add("col", typeof(int));
            table.PrimaryKey = new DataColumn[] { col };
            table.AcceptChanges();

            // ms.net behavior.	
            table.BeginInit();
            DataColumn col1 = new DataColumn("col1", typeof(int));
            table.Columns.AddRange(new DataColumn[] { col1 });
            UniqueConstraint uc = new UniqueConstraint(string.Empty, new string[] { "col1" }, true);
            table.Constraints.AddRange(new Constraint[] { uc });

            try
            {
                table.EndInit();
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                // Cannot add primary key constraint since primary
                // key is already set for the table
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void BeginInit_PrimaryKey_3()
        {
            DataTable table = new DataTable();
            DataColumn col1 = table.Columns.Add("col1", typeof(int));
            DataColumn col2 = table.Columns.Add("col2", typeof(int));

            // ms.net behavior
            table.BeginInit();
            UniqueConstraint uc = new UniqueConstraint(string.Empty, new string[] { "col1" }, true);
            table.Constraints.AddRange(new Constraint[] { uc });
            table.PrimaryKey = new DataColumn[] { col2 };
            table.EndInit();

            Assert.Equal("col1", table.PrimaryKey[0].ColumnName);
        }

        [Fact]
        public void PrimaryKey_OnFailing()
        {
            DataTable table = new DataTable();
            DataColumn col1 = table.Columns.Add("col1", typeof(int));
            table.PrimaryKey = new DataColumn[] { col1 };

            try
            {
                table.PrimaryKey = new DataColumn[] { new DataColumn("col2", typeof(int)) };
                Assert.False(true);
            }
            catch (ArgumentException ex)
            {
                //  Column must belong to a table
                Assert.Equal(typeof(ArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }

            Assert.Equal("col1", table.PrimaryKey[0].ColumnName);
        }

        [Fact]
        public void BeginInit_Cols_Constraints()
        {
            DataTable table = new DataTable();

            // if both cols and constraints are added after BeginInit, the cols
            // should be added, before the constraints are added/validated
            table.BeginInit();
            DataColumn col1 = new DataColumn("col1", typeof(int));
            table.Columns.AddRange(new DataColumn[] { col1 });
            UniqueConstraint uc = new UniqueConstraint(string.Empty, new string[] { "col1" }, false);
            table.Constraints.AddRange(new Constraint[] { uc });
            // no exception shud be thrown
            table.EndInit();

            Assert.Equal(1, table.Constraints.Count);
        }

        [Fact]
        public void LoadDataRow_ExistingData()
        {
            var ds = new DataSet();
            DataTable table = ds.Tables.Add();

            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));
            table.PrimaryKey = new DataColumn[] { table.Columns[0] };

            table.BeginLoadData();
            table.LoadDataRow(new object[] { 1, 10 }, true);
            table.LoadDataRow(new object[] { 2, 10 }, true);
            table.LoadDataRow(new object[] { 3, 10 }, true);
            table.LoadDataRow(new object[] { 4, 10 }, true);
            table.EndLoadData();

            Assert.Equal(4, table.Rows.Count);
            Assert.Equal(10, table.Rows[0][1]);
            Assert.Equal(10, table.Rows[1][1]);
            Assert.Equal(10, table.Rows[2][1]);
            Assert.Equal(10, table.Rows[3][1]);

            table.BeginLoadData();
            table.LoadDataRow(new object[] { 1, 100 }, true);
            table.LoadDataRow(new object[] { 2, 100 }, true);
            table.LoadDataRow(new object[] { 3, 100 }, true);
            table.LoadDataRow(new object[] { 4, 100 }, true);
            table.EndLoadData();

            Assert.Equal(4, table.Rows.Count);
            Assert.Equal(100, table.Rows[0][1]);
            Assert.Equal(100, table.Rows[1][1]);
            Assert.Equal(100, table.Rows[2][1]);
            Assert.Equal(100, table.Rows[3][1]);
        }

        [Fact]
        public void LoadDataRow_DefaultValueError()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));
            table.PrimaryKey = new DataColumn[] { table.Columns[0] };

            table.BeginLoadData();
            // should not throw exception
            table.LoadDataRow(new object[] { 1 }, true);
            table.EndLoadData();

            Assert.Equal(1, table.Rows[0][0]);
            Assert.Equal(DBNull.Value, table.Rows[0][1]);
        }

        [Fact]
        public void RejectChanges_CheckIndex()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.PrimaryKey = new DataColumn[] { table.Columns[0] };

            table.Rows.Add(new object[] { 1 });
            table.AcceptChanges();

            table.Rows[0][0] = 10;
            table.RejectChanges();

            Assert.NotNull(table.Rows.Find(1));
        }

        private void OnRowDeleting_Handler(object sender, DataRowChangeEventArgs e)
        {
            DataTable dt = (DataTable)sender;
            if (dt.Rows[0].Equals(e.Row) && e.Action == _drExpectedAction)
                _eventValues = true;
            _eventRaised = true;
        }

        [Fact]
        public void Select_StringString()
        {
            DataTable dt = DataProvider.CreateChildDataTable();

            DataRow[] drSelect;
            List<DataRow> al;

            //add some rows
            dt.Rows.Add(new object[] { 99, 88, "bla", "wowww" });
            dt.Rows.Add(new object[] { 999, 888, string.Empty, "woowww" });

            //get excepted resault
            al = new List<DataRow>();
            foreach (DataRow dr in dt.Rows)
            {
                if ((int)dr["ChildId"] == 1)
                    al.Add(dr);
            }
            //al.Reverse();
            al.Sort(new DataRowsComparer("ParentId", "Desc"));

            drSelect = dt.Select("ChildId=1", "ParentId Desc");
            Assert.Equal(al.ToArray(), drSelect);

            //get excepted resault
            al = new List<DataRow>();
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["String1"].ToString() == "1-String1")
                    al.Add(dr);
            }
            //al.Reverse();
            al.Sort(new DataRowsComparer("ParentId", "Desc"));

            drSelect = dt.Select("String1='1-String1'", "ParentId Desc");
            Assert.Equal(al.ToArray(), drSelect);

            //get excepted resault
            al = new List<DataRow>();
            foreach (DataRow dr in dt.Rows)
            {
                if ((int)dr["ChildId"] == 1 && dr["String1"].ToString() == "1-String1")
                    al.Add(dr);
            }
            //al.Reverse();
            al.Sort(new DataRowsComparer("ParentId", "Desc"));

            drSelect = dt.Select("ChildId=1 and String1='1-String1'", "ParentId Desc");
            Assert.Equal(al.ToArray(), drSelect);


            //get excepted resault
            al = new List<DataRow>();
            foreach (DataRow dr in dt.Rows)
            {
                if (dr["String1"].ToString().Length < 4)
                    al.Add(dr);
            }
            //al.Reverse();
            al.Sort(new DataRowsComparer("ParentId", "Desc"));

            drSelect = dt.Select("Len(String1) < 4 ", "ParentId Desc");
            Assert.Equal(al.ToArray(), drSelect);
        }

        [Fact]
        public void Select_StringString_3()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            //Select - parse sort string checking 1");
            try
            {
                dt.Select(dt.Columns[0].ColumnName, dt.Columns[0].ColumnName + "1");
                Assert.False(true);
            }
            catch (IndexOutOfRangeException ex)
            {
                // Cannot find column ParentId1
                Assert.Equal(typeof(IndexOutOfRangeException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("ParentId1") != -1);
            }
        }

        [Fact]
        public void Select_NonBooleanFilter()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));

            DataRow[] row = table.Select("col1*10");
            Assert.Equal(0, row.Length);

            // No exception shud be thrown 
            // The filter created earlier (if cached), will raise an EvaluateException
            // and so shouldn't be cached
            for (int i = 0; i < 5; ++i)
                table.Rows.Add(new object[] { i });

            try
            {
                table.Select("col1*10");
                Assert.False(true);
            }
            catch (EvaluateException ex)
            {
                // Filter expression 'col1*10' does not evaluate
                // to a Boolean term
                Assert.Equal(typeof(EvaluateException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                //Assert.True (ex.Message.IndexOf ("'col1*10'") != -1);
                //Assert.True (ex.Message.IndexOf ("Boolean") != -1);
            }
        }

        [Fact]
        public void Select_BoolColumn()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(bool));

            for (int i = 0; i < 5; i++)
                table.Rows.Add(new object[] { i });

            DataRow[] result;
            try
            {
                result = table.Select("col1");
                Assert.False(true);
            }
            catch (EvaluateException ex)
            {
                // Filter expression 'col1' does not evaluate to
                // a Boolean term
                Assert.Equal(typeof(EvaluateException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                //Assert.True (ex.Message.IndexOf ("'col1'") != -1);
                //Assert.True (ex.Message.IndexOf ("Boolean") != -1);
            }

            //col2 is a boolean expression, and a null value translates to
            //false.
            result = table.Select("col2");
            Assert.Equal(0, result.Length);
        }

        [Fact]
        public void Select_OrderOfRows()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(int));
            table.Columns.Add("col2", typeof(int));

            for (int i = 0; i < 10; i++)
                table.Rows.Add(new object[] { 10 - i, i });
            DataRow[] result = table.Select("col1 > 5");

            Assert.Equal(6, result[0][0]);
        }

        [Fact]
        public void DataTable_Clone()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col1", typeof(DateTime));
            table.Columns[0].DateTimeMode = DataSetDateTime.Local;
            DataTable table1 = table.Clone();
            Assert.Equal(DataSetDateTime.Local, table1.Columns[0].DateTimeMode);

            //Test any other new prop in 2.0
        }

        [Fact]
        public void Merge_SchemaTest()
        {
            DataTable table1 = new DataTable("t1");
            table1.Columns.Add("col1", typeof(int));

            DataTable table2 = new DataTable("t2");
            table2.Columns.Add("col2", typeof(int));

            DataTable table3;

            table3 = table1.Clone();
            table3.Merge(table2);
            Assert.Equal(2, table3.Columns.Count);
            table3 = table1.Clone();
            table3.Merge(table2, false, MissingSchemaAction.Ignore);
            Assert.Equal(1, table3.Columns.Count);

            // source constraints are ignored
            table2.Constraints.Add("uc", table2.Columns[0], false);
            table3 = table1.Clone();
            table3.Merge(table2);
            Assert.Equal(0, table3.Constraints.Count);

            // source PK is merged depending on MissingSchemaAction
            table2.PrimaryKey = new DataColumn[] { table2.Columns[0] };
            table3 = table1.Clone();
            table3.Merge(table2);
            Assert.Equal(1, table3.Constraints.Count);
            table3 = table1.Clone();
            table3.Merge(table2, false, MissingSchemaAction.Ignore);
            Assert.Equal(0, table3.Constraints.Count);

            //FIXME : If both source and target have PK, then 
            // shud be the exception raised when schema is merged? 
            // ms.net throws a nullreference exception.
            // If any data is merged, exception is anyways raised.
            /*
			table1.PrimaryKey = new DataColumn[] {table1.Columns[0]};
			table3 = table1.Clone ();
			try {
				table3.Merge(table2);
Assert.False(true);
			} catch (DataException e) {}
			*/

            table3.Merge(table2, false, MissingSchemaAction.Ignore);
            table1.PrimaryKey = null;

            // DateTime columns, DataType match only if DateTimeMode matches
            table1.Columns.Add("col_datetime", typeof(DateTime));
            table2.Columns.Add("col_datetime", typeof(DateTime));
            table1.Columns["col_datetime"].DateTimeMode = DataSetDateTime.Local;
            table2.Columns["col_datetime"].DateTimeMode = DataSetDateTime.Unspecified;

            table3 = table1.Clone();
            try
            {
                table3.Merge(table2);
                Assert.False(true);
            }
            catch (DataException)
            {
                // <target>.col_datetime and <source>.col_datetime
                // have conflicting properties: DataType property mismatch.
            }

            table1.Columns["col_datetime"].DateTimeMode = DataSetDateTime.Unspecified;
            table2.Columns["col_datetime"].DateTimeMode = DataSetDateTime.UnspecifiedLocal;
            table3 = table1.Clone();
            table3.Merge(table2);
            Assert.Equal(DataSetDateTime.Unspecified, table3.Columns["col_datetime"].DateTimeMode);
        }

        [Fact]
        public void Merge_TestData()
        {
            DataTable t1 = new DataTable("t1");
            DataTable t2 = new DataTable("t2");

            t1.Columns.Add("c1", typeof(int));
            t1.Columns.Add("c2", typeof(int));
            t2.Columns.Add("c1", typeof(int));
            t2.Columns.Add("c2", typeof(int));

            t1.Rows.Add(new object[] { 1, 1 });
            t1.Rows.Add(new object[] { 2, 2 });

            t2.Rows.Add(new object[] { 1, 5 });
            t2.Rows.Add(new object[] { 1, 10 });

            DataTable t3 = t1.Copy();
            // When primary key is not defined, rows are not merged.
            t3.Merge(t2);
            Assert.Equal(4, t3.Rows.Count);

            t1.PrimaryKey = new DataColumn[] { t1.Columns[0] };
            t3 = t1.Copy();
            t3.Merge(t2);
            Assert.Equal(2, t3.Rows.Count);
            Assert.Equal(10, t3.Rows[0][1]);

            t3 = t1.Copy();
            t3.Merge(t2, true);
            Assert.Equal(2, t3.Rows.Count);
            Assert.Equal(1, t3.Rows[0][1]);
        }

        internal class DataRowsComparer : IComparer<DataRow>
        {
            private string _columnName;
            private string _direction;

            public DataRowsComparer(string columnName, string direction)
            {
                _columnName = columnName;
                if (direction.ToLower() != "asc" && direction.ToLower() != "desc")
                    throw new ArgumentException("Direction can only be one of: 'asc' or 'desc'");
                _direction = direction;
            }

            public int Compare(DataRow drX, DataRow drY)
            {
                object objX = drX[_columnName];
                object objY = drY[_columnName];

                int compareResult = Comparer.Default.Compare(objX, objY);

                //If we are comparing desc we need to reverse the result.
                if (_direction.ToLower() == "desc")
                    compareResult = -compareResult;

                return compareResult;
            }
        }
    }
}
