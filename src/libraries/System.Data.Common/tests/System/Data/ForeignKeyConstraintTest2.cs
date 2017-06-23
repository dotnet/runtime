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

using Xunit;


namespace System.Data.Tests
{
    public class ForeignKeyConstraintTest2
    {
        [Fact]
        public void Columns()
        {
            //int RowCount;
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);

            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            // Columns
            Assert.Equal(dtChild.Columns[0], fc.Columns[0]);

            // Columns count
            Assert.Equal(1, fc.Columns.Length);
        }

        [Fact]
        public void Equals()
        {
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);
            dtParent.PrimaryKey = new DataColumn[] { dtParent.Columns[0] };
            ds.EnforceConstraints = true;

            ForeignKeyConstraint fc1, fc2;
            fc1 = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            fc2 = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[1]);
            // different columnn
            Assert.Equal(false, fc1.Equals(fc2));

            //Two System.Data.ForeignKeyConstraint are equal if they constrain the same columns.
            // same column
            fc2 = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);
            Assert.Equal(true, fc1.Equals(fc2));
        }

        [Fact]
        public void RelatedColumns()
        {
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);
            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            // RelatedColumns
            Assert.Equal(new DataColumn[] { dtParent.Columns[0] }, fc.RelatedColumns);
        }

        [Fact]
        public void RelatedTable()
        {
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);
            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            // RelatedTable
            Assert.Equal(dtParent, fc.RelatedTable);
        }

        [Fact]
        public void Table()
        {
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);
            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            // Table
            Assert.Equal(dtChild, fc.Table);
        }

        [Fact]
        public new void ToString()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();

            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            // ToString - default
            Assert.Equal(string.Empty, fc.ToString());

            fc = new ForeignKeyConstraint("myConstraint", dtParent.Columns[0], dtChild.Columns[0]);
            // Tostring - Constraint name
            Assert.Equal("myConstraint", fc.ToString());
        }

        [Fact]
        public void acceptRejectRule()
        {
            DataSet ds = getNewDataSet();

            ForeignKeyConstraint fc = new ForeignKeyConstraint(ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]);
            fc.AcceptRejectRule = AcceptRejectRule.Cascade;
            ds.Tables[1].Constraints.Add(fc);

            //Update the parent 

            ds.Tables[0].Rows[0]["ParentId"] = 777;
            Assert.Equal(true, ds.Tables[1].Select("ParentId=777").Length > 0);
            ds.Tables[0].RejectChanges();
            Assert.Equal(0, ds.Tables[1].Select("ParentId=777").Length);
        }
        private DataSet getNewDataSet()
        {
            DataSet ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            ds1.Tables.Add(DataProvider.CreateChildDataTable());
            //	ds1.Tables.Add(DataProvider.CreateChildDataTable());
            ds1.Tables[0].PrimaryKey = new DataColumn[] { ds1.Tables[0].Columns[0] };

            return ds1;
        }

        [Fact]
        public void constraintName()
        {
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);

            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            // default 
            Assert.Equal(string.Empty, fc.ConstraintName);

            fc.ConstraintName = "myConstraint";

            // set/get 
            Assert.Equal("myConstraint", fc.ConstraintName);
        }

        [Fact]
        public void ctor_ParentColChildCol()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            var ds = new DataSet();
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            ForeignKeyConstraint fc = null;

            // Ctor ArgumentException
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                fc = new ForeignKeyConstraint(new DataColumn[] { dtParent.Columns[0] }, new DataColumn[] { dtChild.Columns[0], dtChild.Columns[1] });
            });

            fc = new ForeignKeyConstraint(new DataColumn[] { dtParent.Columns[0], dtParent.Columns[1] }, new DataColumn[] { dtChild.Columns[0], dtChild.Columns[2] });

            // Add constraint to table - ArgumentException
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                dtChild.Constraints.Add(fc);
            });

            // Child Table Constraints Count - two columnns
            Assert.Equal(0, dtChild.Constraints.Count);

            // Parent Table Constraints Count - two columnns
            Assert.Equal(1, dtParent.Constraints.Count);

            // DataSet relations Count
            Assert.Equal(0, ds.Relations.Count);

            dtParent.Constraints.Clear();
            dtChild.Constraints.Clear();

            fc = new ForeignKeyConstraint(new DataColumn[] { dtParent.Columns[0] }, new DataColumn[] { dtChild.Columns[0] });
            // Ctor
            Assert.Equal(false, fc == null);

            // Child Table Constraints Count
            Assert.Equal(0, dtChild.Constraints.Count);

            // Parent Table Constraints Count
            Assert.Equal(0, dtParent.Constraints.Count);

            // DataSet relations Count
            Assert.Equal(0, ds.Relations.Count);

            dtChild.Constraints.Add(fc);

            // Child Table Constraints Count, Add
            Assert.Equal(1, dtChild.Constraints.Count);

            // Parent Table Constraints Count, Add
            Assert.Equal(1, dtParent.Constraints.Count);

            // DataSet relations Count, Add
            Assert.Equal(0, ds.Relations.Count);

            // Parent Table Constraints type
            Assert.Equal(typeof(UniqueConstraint), dtParent.Constraints[0].GetType());

            // Parent Table Constraints type
            Assert.Equal(typeof(ForeignKeyConstraint), dtChild.Constraints[0].GetType());

            // Parent Table Primary key
            Assert.Equal(0, dtParent.PrimaryKey.Length);

            dtChild.Constraints.Clear();
            dtParent.Constraints.Clear();
            ds.Relations.Add(new DataRelation("myRelation", dtParent.Columns[0], dtChild.Columns[0]));

            // Relation - Child Table Constraints Count
            Assert.Equal(1, dtChild.Constraints.Count);

            // Relation - Parent Table Constraints Count
            Assert.Equal(1, dtParent.Constraints.Count);

            // Relation - Parent Table Constraints type
            Assert.Equal(typeof(UniqueConstraint), dtParent.Constraints[0].GetType());

            // Relation - Parent Table Constraints type
            Assert.Equal(typeof(ForeignKeyConstraint), dtChild.Constraints[0].GetType());

            // Relation - Parent Table Primary key
            Assert.Equal(0, dtParent.PrimaryKey.Length);
        }

        [Fact]
        public void ctor_NameParentColChildCol()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();

            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint("myForeignKey", dtParent.Columns[0], dtChild.Columns[0]);

            // Ctor
            Assert.Equal(false, fc == null);

            // Ctor - name
            Assert.Equal("myForeignKey", fc.ConstraintName);
        }

        [Fact]
        public void ctor_NameParentColsChildCols()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();

            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint("myForeignKey", new DataColumn[] { dtParent.Columns[0] }, new DataColumn[] { dtChild.Columns[0] });

            // Ctor
            Assert.Equal(false, fc == null);

            // Ctor - name
            Assert.Equal("myForeignKey", fc.ConstraintName);
        }

        [Fact]
        public void deleteRule()
        {
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            ds.Tables.Add(dtParent);
            ds.Tables.Add(dtChild);
            dtParent.PrimaryKey = new DataColumn[] { dtParent.Columns[0] };
            ds.EnforceConstraints = true;

            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            //checking default
            // Default
            Assert.Equal(Rule.Cascade, fc.DeleteRule);

            //checking set/get
            foreach (Rule rule in Enum.GetValues(typeof(Rule)))
            {
                // Set/Get - rule
                fc.DeleteRule = rule;
                Assert.Equal(rule, fc.DeleteRule);
            }

            dtChild.Constraints.Add(fc);

            //checking delete rule

            // Rule = None, Delete Exception
            fc.DeleteRule = Rule.None;
            //Exception = "Cannot delete this row because constraints are enforced on relation Constraint1, and deleting this row will strand child rows."
            Assert.Throws<InvalidConstraintException>(() =>
            {
                dtParent.Rows.Find(1).Delete();
            });

            // Rule = None, Delete succeed
            fc.DeleteRule = Rule.None;
            foreach (DataRow dr in dtChild.Select("ParentId = 1"))
                dr.Delete();
            dtParent.Rows.Find(1).Delete();
            Assert.Equal(0, dtParent.Select("ParentId=1").Length);

            // Rule = Cascade
            fc.DeleteRule = Rule.Cascade;
            dtParent.Rows.Find(2).Delete();
            Assert.Equal(0, dtChild.Select("ParentId=2").Length);

            // Rule = SetNull
            DataSet ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            ds1.Tables.Add(DataProvider.CreateChildDataTable());

            ForeignKeyConstraint fc1 = new ForeignKeyConstraint(ds1.Tables[0].Columns[0], ds1.Tables[1].Columns[1]);
            fc1.DeleteRule = Rule.SetNull;
            ds1.Tables[1].Constraints.Add(fc1);

            Assert.Equal(0, ds1.Tables[1].Select("ChildId is null").Length);

            ds1.Tables[0].PrimaryKey = new DataColumn[] { ds1.Tables[0].Columns[0] };
            ds1.Tables[0].Rows.Find(3).Delete();

            ds1.Tables[0].AcceptChanges();
            ds1.Tables[1].AcceptChanges();

            DataRow[] arr = ds1.Tables[1].Select("ChildId is null");

            /*foreach (DataRow dr in arr)
					{
						Assert.Equal(null, dr["ChildId"]);
					}*/

            Assert.Equal(4, arr.Length);

            // Rule = SetDefault
            //fc.DeleteRule = Rule.SetDefault;
            ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            ds1.Tables.Add(DataProvider.CreateChildDataTable());

            fc1 = new ForeignKeyConstraint(ds1.Tables[0].Columns[0], ds1.Tables[1].Columns[1]);
            fc1.DeleteRule = Rule.SetDefault;
            ds1.Tables[1].Constraints.Add(fc1);
            ds1.Tables[1].Columns[1].DefaultValue = "777";

            //Add new row  --> in order to apply the forigen key rules
            DataRow dr2 = ds1.Tables[0].NewRow();
            dr2["ParentId"] = 777;
            ds1.Tables[0].Rows.Add(dr2);

            ds1.Tables[0].PrimaryKey = new DataColumn[] { ds1.Tables[0].Columns[0] };
            ds1.Tables[0].Rows.Find(3).Delete();
            Assert.Equal(4, ds1.Tables[1].Select("ChildId=777").Length);
        }

        [Fact]
        public void extendedProperties()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateParentDataTable();

            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            PropertyCollection pc = fc.ExtendedProperties;

            // Checking ExtendedProperties default 
            Assert.Equal(true, fc != null);

            // Checking ExtendedProperties count 
            Assert.Equal(0, pc.Count);
        }

        [Fact]
        public void ctor_DclmDclm()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();
            var ds = new DataSet();
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            ForeignKeyConstraint fc = null;
            fc = new ForeignKeyConstraint(dtParent.Columns[0], dtChild.Columns[0]);

            Assert.False(fc == null);

            Assert.Equal(0, dtChild.Constraints.Count);

            Assert.Equal(0, dtParent.Constraints.Count);

            Assert.Equal(0, ds.Relations.Count);

            dtChild.Constraints.Add(fc);

            Assert.Equal(1, dtChild.Constraints.Count);

            Assert.Equal(1, dtParent.Constraints.Count);

            Assert.Equal(0, ds.Relations.Count);

            Assert.Equal(typeof(UniqueConstraint), dtParent.Constraints[0].GetType());

            Assert.Equal(typeof(ForeignKeyConstraint), dtChild.Constraints[0].GetType());

            Assert.Equal(0, dtParent.PrimaryKey.Length);

            dtChild.Constraints.Clear();
            dtParent.Constraints.Clear();
            ds.Relations.Add(new DataRelation("myRelation", dtParent.Columns[0], dtChild.Columns[0]));

            Assert.Equal(1, dtChild.Constraints.Count);

            Assert.Equal(1, dtParent.Constraints.Count);

            Assert.Equal(typeof(UniqueConstraint), dtParent.Constraints[0].GetType());

            Assert.Equal(typeof(ForeignKeyConstraint), dtChild.Constraints[0].GetType());

            Assert.Equal(0, dtParent.PrimaryKey.Length);
        }

        [Fact]
        public void ctor_DclmDclm1()
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                ForeignKeyConstraint fc = new ForeignKeyConstraint(null, (DataColumn)null);
            });
        }

        [Fact]
        public void ctor_DclmDclm2()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                var ds = new DataSet();
                ds.Tables.Add(DataProvider.CreateParentDataTable());
                ds.Tables.Add(DataProvider.CreateChildDataTable());
                ds.Tables["Parent"].Columns["ParentId"].Expression = "2";

                ForeignKeyConstraint fc = new ForeignKeyConstraint(ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]);
            });
        }

        [Fact]
        public void ctor_DclmDclm3()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                var ds = new DataSet();
                ds.Tables.Add(DataProvider.CreateParentDataTable());
                ds.Tables.Add(DataProvider.CreateChildDataTable());
                ds.Tables["Child"].Columns["ParentId"].Expression = "2";

                ForeignKeyConstraint fc = new ForeignKeyConstraint(ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]);
            });
        }

        [Fact]
        public void UpdateRule1()
        {
            DataSet ds = GetNewDataSet();
            ForeignKeyConstraint fc = new ForeignKeyConstraint(ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]);
            fc.UpdateRule = Rule.Cascade;
            ds.Tables[1].Constraints.Add(fc);

            //Changing parent row

            ds.Tables[0].Rows.Find(1)["ParentId"] = 8;

            ds.Tables[0].AcceptChanges();
            ds.Tables[1].AcceptChanges();
            //Checking the table

            Assert.True(ds.Tables[1].Select("ParentId=8").Length > 0);
        }

        [Fact]
        public void UpdateRule2()
        {
            Assert.Throws<ConstraintException>(() =>
            {
                DataSet ds = GetNewDataSet();
                ds.Tables[0].PrimaryKey = null;
                ForeignKeyConstraint fc = new ForeignKeyConstraint(ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]);
                fc.UpdateRule = Rule.None;
                ds.Tables[1].Constraints.Add(fc);

                //Changing parent row

                ds.Tables[0].Rows[0]["ParentId"] = 5;

                /*ds.Tables[0].AcceptChanges();
                ds.Tables[1].AcceptChanges();
                //Checking the table
                Compare(ds.Tables[1].Select("ParentId=8").Length ,0);*/
            });
        }

        [Fact]
        public void UpdateRule3()
        {
            DataSet ds = GetNewDataSet();
            ForeignKeyConstraint fc = new ForeignKeyConstraint(ds.Tables[0].Columns[0], ds.Tables[1].Columns[1]);
            fc.UpdateRule = Rule.SetDefault;
            ds.Tables[1].Constraints.Add(fc);

            //Changing parent row

            ds.Tables[1].Columns[1].DefaultValue = "777";

            //Add new row  --> in order to apply the forigen key rules
            DataRow dr = ds.Tables[0].NewRow();
            dr["ParentId"] = 777;
            ds.Tables[0].Rows.Add(dr);


            ds.Tables[0].Rows.Find(1)["ParentId"] = 8;

            ds.Tables[0].AcceptChanges();
            ds.Tables[1].AcceptChanges();
            //Checking the table

            Assert.True(ds.Tables[1].Select("ChildId=777").Length > 0);
        }

        [Fact]
        public void UpdateRule4()
        {
            DataSet ds = GetNewDataSet();
            ForeignKeyConstraint fc = new ForeignKeyConstraint(ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]);
            fc.UpdateRule = Rule.SetNull;
            ds.Tables[1].Constraints.Add(fc);

            //Changing parent row

            ds.Tables[0].Rows.Find(1)["ParentId"] = 8;

            ds.Tables[0].AcceptChanges();
            ds.Tables[1].AcceptChanges();
            //Checking the table

            Assert.True(ds.Tables[1].Select("ParentId is null").Length > 0);
        }

        private DataSet GetNewDataSet()
        {
            DataSet ds1 = new DataSet();
            ds1.Tables.Add(DataProvider.CreateParentDataTable());
            ds1.Tables.Add(DataProvider.CreateChildDataTable());
            ds1.Tables[0].PrimaryKey = new DataColumn[] { ds1.Tables[0].Columns[0] };

            return ds1;
        }
        [Fact]
        public void ForeignConstraint_DateTimeModeTest()
        {
            DataTable t1 = new DataTable("t1");
            t1.Columns.Add("col", typeof(DateTime));

            DataTable t2 = new DataTable("t2");
            t2.Columns.Add("col", typeof(DateTime));
            t2.Columns[0].DateTimeMode = DataSetDateTime.Unspecified;

            // DataColumn type shud match, and no exception shud be raised 
            t2.Constraints.Add("fk", t1.Columns[0], t2.Columns[0]);

            t2.Constraints.Clear();
            t2.Columns[0].DateTimeMode = DataSetDateTime.Local;
            try
            {
                // DataColumn type shud not match, and exception shud be raised 
                t2.Constraints.Add("fk", t1.Columns[0], t2.Columns[0]);
                Assert.False(true);
            }
            catch (InvalidOperationException e) { }
        }

        [Fact]
        public void ParentChildSameColumn()
        {
            DataTable dataTable = new DataTable("Menu");
            DataColumn colID = dataTable.Columns.Add("ID", typeof(int));
            DataColumn colCulture = dataTable.Columns.Add("Culture", typeof(string));
            dataTable.Columns.Add("Name", typeof(string));
            DataColumn colParentID = dataTable.Columns.Add("ParentID", typeof(int));

            // table PK (ID, Culture)
            dataTable.Constraints.Add(new UniqueConstraint(
                "MenuPK",
                new DataColumn[] { colID, colCulture },
                true));

            // add a FK referencing the same table: (ID, Culture) <- (ParentID, Culture)
            ForeignKeyConstraint fkc = new ForeignKeyConstraint(
                "MenuParentFK",
                new DataColumn[] { colID, colCulture },
                new DataColumn[] { colParentID, colCulture });

            dataTable.Constraints.Add(fkc);
        }
    }
}
