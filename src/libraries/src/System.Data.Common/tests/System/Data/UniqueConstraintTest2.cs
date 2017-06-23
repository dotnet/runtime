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
    public class UniqueConstraintTest2
    {
        [Fact]
        public void Columns()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint(dtParent.Columns[0]);

            // Columns 1
            Assert.Equal(1, uc.Columns.Length);

            // Columns 2
            Assert.Equal(dtParent.Columns[0], uc.Columns[0]);
        }

        [Fact]
        public void Equals_O()
        {
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            ds.Tables.Add(dtParent);

            UniqueConstraint uc1, uc2;
            uc1 = new UniqueConstraint(dtParent.Columns[0]);

            uc2 = new UniqueConstraint(dtParent.Columns[1]);
            // different columnn
            Assert.Equal(false, uc1.Equals(uc2));

            //Two System.Data.ForeignKeyConstraint are equal if they constrain the same columns.
            // same column
            uc2 = new UniqueConstraint(dtParent.Columns[0]);
            Assert.Equal(true, uc1.Equals(uc2));
        }

        [Fact]
        public void IsPrimaryKey()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint(dtParent.Columns[0], false);
            dtParent.Constraints.Add(uc);

            // primary key 1
            Assert.Equal(false, uc.IsPrimaryKey);

            dtParent.Constraints.Remove(uc);
            uc = new UniqueConstraint(dtParent.Columns[0], true);
            dtParent.Constraints.Add(uc);

            // primary key 2
            Assert.Equal(true, uc.IsPrimaryKey);
        }

        [Fact]
        public void Table()
        {
            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            ds.Tables.Add(dtParent);
            UniqueConstraint uc = null;
            uc = new UniqueConstraint(dtParent.Columns[0]);

            // Table
            Assert.Equal(dtParent, uc.Table);
        }

        [Fact]
        public new void ToString()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint(dtParent.Columns[0], false);

            // ToString - default
            Assert.Equal(string.Empty, uc.ToString());

            uc = new UniqueConstraint("myConstraint", dtParent.Columns[0], false);
            // Tostring - Constraint name
            Assert.Equal("myConstraint", uc.ToString());
        }

        [Fact]
        public void constraintName()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint(dtParent.Columns[0]);

            // default 
            Assert.Equal(string.Empty, uc.ConstraintName);

            uc.ConstraintName = "myConstraint";

            // set/get 
            Assert.Equal("myConstraint", uc.ConstraintName);
        }

        [Fact]
        public void ctor_DataColumn()
        {
            Exception tmpEx = new Exception();

            var ds = new DataSet();
            DataTable dtParent = DataProvider.CreateParentDataTable();
            ds.Tables.Add(dtParent);
            ds.EnforceConstraints = true;

            UniqueConstraint uc = null;

            // DataColumn.Unique - without constraint
            Assert.Equal(false, dtParent.Columns[0].Unique);

            uc = new UniqueConstraint(dtParent.Columns[0]);

            // Ctor
            Assert.Equal(false, uc == null);

            // DataColumn.Unique - with constraint
            Assert.Equal(false, dtParent.Columns[0].Unique);

            // Ctor - add exisiting column
            dtParent.Rows.Add(new object[] { 99, "str1", "str2" });
            dtParent.Constraints.Add(uc);
            Assert.Throws<ConstraintException>(() => dtParent.Rows.Add(new object[] { 99, "str1", "str2" }));

            DataTable dtChild = DataProvider.CreateChildDataTable();
            uc = new UniqueConstraint(dtChild.Columns[1]);

            //Column[1] is not unique, will throw exception
            // ArgumentException 
            AssertExtensions.Throws<ArgumentException>(null, () => dtChild.Constraints.Add(uc));

            //reset the table
            dtParent = DataProvider.CreateParentDataTable();

            // DataColumn.Unique = true, will add UniqueConstraint
            dtParent.Columns[0].Unique = true;
            Assert.Equal(1, dtParent.Constraints.Count);

            // Check the created UniqueConstraint
            dtParent.Columns[0].Unique = true;
            Assert.Equal(typeof(UniqueConstraint).FullName, dtParent.Constraints[0].GetType().FullName);

            // add UniqueConstarint that don't belong to the table
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                dtParent.Constraints.Add(uc);
            });
        }

        [Fact]
        public void ctor_DataColumnNoPrimary()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint(dtParent.Columns[0], false);
            dtParent.Constraints.Add(uc);

            // Ctor
            Assert.Equal(false, uc == null);

            // primary key 1
            Assert.Equal(0, dtParent.PrimaryKey.Length);

            dtParent.Constraints.Remove(uc);
            uc = new UniqueConstraint(dtParent.Columns[0], true);
            dtParent.Constraints.Add(uc);

            // primary key 2
            Assert.Equal(1, dtParent.PrimaryKey.Length);
        }

        [Fact]
        public void ctor_DataColumns()
        {
            Exception tmpEx = new Exception();
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint(new DataColumn[] { dtParent.Columns[0], dtParent.Columns[1] });

            // Ctor - parent
            Assert.Equal(false, uc == null);

            // Ctor - add exisiting column
            dtParent.Rows.Add(new object[] { 99, "str1", "str2" });
            dtParent.Constraints.Add(uc);
            Assert.Throws<ConstraintException>(() => dtParent.Rows.Add(new object[] { 99, "str1", "str2" }));

            DataTable dtChild = DataProvider.CreateChildDataTable();
            uc = new UniqueConstraint(new DataColumn[] { dtChild.Columns[0], dtChild.Columns[1] });
            dtChild.Constraints.Add(uc);

            // Ctor - child
            Assert.Equal(false, uc == null);

            dtChild.Constraints.Clear();
            uc = new UniqueConstraint(new DataColumn[] { dtChild.Columns[1], dtChild.Columns[2] });

            //target columnn are not unnique, will throw an exception
            // ArgumentException - child
            AssertExtensions.Throws<ArgumentException>(null, () => dtChild.Constraints.Add(uc));
        }

        [Fact]
        public void ctor_DataColumnPrimary()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint(dtParent.Columns[0], false);
            dtParent.Constraints.Add(uc);

            // Ctor
            Assert.Equal(false, uc == null);

            // primary key 1
            Assert.Equal(0, dtParent.PrimaryKey.Length);

            dtParent.Constraints.Remove(uc);
            uc = new UniqueConstraint(dtParent.Columns[0], true);
            dtParent.Constraints.Add(uc);

            // primary key 2
            Assert.Equal(1, dtParent.PrimaryKey.Length);
        }

        [Fact]
        public void ctor_NameDataColumn()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint("myConstraint", dtParent.Columns[0]);

            // Ctor
            Assert.Equal(false, uc == null);

            // Ctor name
            Assert.Equal("myConstraint", uc.ConstraintName);
        }

        [Fact]
        public void ctor_NameDataColumnPrimary()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint("myConstraint", dtParent.Columns[0], false);
            dtParent.Constraints.Add(uc);

            // Ctor
            Assert.Equal(false, uc == null);

            // primary key 1
            Assert.Equal(0, dtParent.PrimaryKey.Length);

            // Ctor name 1
            Assert.Equal("myConstraint", uc.ConstraintName);

            dtParent.Constraints.Remove(uc);
            uc = new UniqueConstraint("myConstraint", dtParent.Columns[0], true);
            dtParent.Constraints.Add(uc);

            // primary key 2
            Assert.Equal(1, dtParent.PrimaryKey.Length);

            // Ctor name 2
            Assert.Equal("myConstraint", uc.ConstraintName);
        }

        [Fact]
        public void ctor_NameDataColumns()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint("myConstraint", new DataColumn[] { dtParent.Columns[0], dtParent.Columns[1] });

            // Ctor
            Assert.Equal(false, uc == null);

            // Ctor name
            Assert.Equal("myConstraint", uc.ConstraintName);
        }

        [Fact]
        public void ctor_NameDataColumnsPrimary()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint("myConstraint", new DataColumn[] { dtParent.Columns[0] }, false);
            dtParent.Constraints.Add(uc);

            // Ctor
            Assert.Equal(false, uc == null);

            // primary key 1
            Assert.Equal(0, dtParent.PrimaryKey.Length);

            // Ctor name 1
            Assert.Equal("myConstraint", uc.ConstraintName);

            dtParent.Constraints.Remove(uc);
            uc = new UniqueConstraint("myConstraint", new DataColumn[] { dtParent.Columns[0] }, true);
            dtParent.Constraints.Add(uc);

            // primary key 2
            Assert.Equal(1, dtParent.PrimaryKey.Length);

            // Ctor name 2
            Assert.Equal("myConstraint", uc.ConstraintName);
        }

        [Fact]
        public void extendedProperties()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();

            UniqueConstraint uc = null;
            uc = new UniqueConstraint(dtParent.Columns[0]);
            PropertyCollection pc = uc.ExtendedProperties;

            // Checking ExtendedProperties default 
            Assert.Equal(true, pc != null);

            // Checking ExtendedProperties count 
            Assert.Equal(0, pc.Count);
        }
    }
}
