// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

using System.ComponentModel;
using Xunit;

namespace System.Data.Tests
{
    public class DataColumnTest2
    {
        [Fact]
        public void AllowDBNull()
        {
            DataTable dt = new DataTable();
            DataColumn dc;
            dc = new DataColumn("ColName", typeof(int));
            dc.DefaultValue = DBNull.Value;
            dt.Columns.Add(dc);
            dc.AutoIncrement = false;

            // Checking default value (True)
            Assert.True(dc.AllowDBNull);

            // AllowDBNull=true - adding new row with null value
            dt.Rows.Add(dt.NewRow());
            Assert.Equal(DBNull.Value, dt.Rows[0][0]);

            // set AllowDBNull=false
            Assert.Throws<DataException>(() =>
            {
                dc.AllowDBNull = false; //the existing row contains null value
            });

            dt.Rows.Clear();
            dc.AllowDBNull = false;
            // AllowDBNull=false - adding new row with null value
            Assert.Throws<NoNullAllowedException>(() =>
            {
                dt.Rows.Add(dt.NewRow());
            });

            dc.AutoIncrement = true;
            int iRowCount = dt.Rows.Count;
            // AllowDBNull=false,AutoIncrement=true - adding new row with null value
            dt.Rows.Add(dt.NewRow());
            Assert.Equal(dt.Rows.Count, iRowCount + 1);
        }

        [Fact]
        public void AutoIncrement()
        {
            DataColumn dc;
            dc = new DataColumn("ColName", typeof(string));

            // Checking default value (False)
            Assert.False(dc.AutoIncrement);

            //Checking Set
            dc.AutoIncrement = true;
            // Checking Get
            Assert.True(dc.AutoIncrement);
        }

        [Fact]
        public void AutoIncrementSeed()
        {
            DataColumn dc;
            dc = new DataColumn("ColName", typeof(string));

            // Checking default value 0
            Assert.Equal(0, dc.AutoIncrementSeed);

            //Checking Set
            dc.AutoIncrementSeed = long.MaxValue;
            // Checking Get MaxValue
            Assert.Equal(long.MaxValue, dc.AutoIncrementSeed);

            //Checking Set
            dc.AutoIncrementSeed = long.MinValue;
            // Checking Get MinValue
            Assert.Equal(long.MinValue, dc.AutoIncrementSeed);
        }

        [Fact]
        public void AutoIncrementStep()
        {
            DataColumn dc;
            dc = new DataColumn("ColName", typeof(string));
            // Checking default value 1
            Assert.Equal(1, dc.AutoIncrementStep);

            //Checking Set
            dc.AutoIncrementStep = long.MaxValue;
            // Checking Get MaxValue
            Assert.Equal(long.MaxValue, dc.AutoIncrementStep);

            //Checking Set
            dc.AutoIncrementStep = long.MinValue;
            // Checking Get MinValue
            Assert.Equal(long.MinValue, dc.AutoIncrementStep);
        }

        [Fact]
        public void Caption()
        {
            DataColumn dc;
            string sCaption = "NewCaption";
            dc = new DataColumn("ColName", typeof(string));

            //Checking default value ( ColumnName )
            // Checking default value ( ColumnName )
            Assert.Equal(dc.ColumnName, dc.Caption);

            //Checking Set
            dc.Caption = sCaption;
            // Checking Get
            Assert.Equal(sCaption, dc.Caption);
        }

        [Fact]
        public void ColumnName()
        {
            DataColumn dc;
            string sName = "NewName";

            dc = new DataColumn();
            //Checking default value ("")
            // ColumnName default value
            Assert.Equal(string.Empty, dc.ColumnName);

            //Checking Set
            dc.ColumnName = sName;
            //Checking Get
            // ColumnName Get/Set
            Assert.Equal(sName, dc.ColumnName);

            //Special chars (valid chars)
            sName = "~()#\\/=><+-*%&|^'\"[]";
            // ColumnName Special chars
            dc.ColumnName = sName;
            Assert.Equal(sName, dc.ColumnName);
        }

        [Fact]
        public void DataType()
        {
            DataColumn dc;
            dc = new DataColumn();
            Type[] typeArr = { typeof(bool), typeof(byte), typeof(char), typeof(DateTime),
                typeof(decimal), typeof(double), typeof(short), typeof(int),
                typeof(long), typeof(sbyte), typeof(float), typeof(string),
                typeof(TimeSpan), typeof(ushort), typeof(uint), typeof(ulong) };

            //Checking default value (string)
            // GetType - Default
            Assert.Equal(typeof(string), dc.DataType);

            foreach (Type type in typeArr)
            {
                //Checking Set
                dc.DataType = type;
                // Checking GetType " + sType);
                Assert.Equal(type, dc.DataType);
            }
        }

        [Fact]
        public void ExtendedProperties()
        {
            DataColumn dc;
            PropertyCollection pc;
            dc = new DataColumn();

            pc = dc.ExtendedProperties;
            // Checking ExtendedProperties default
            Assert.NotNull(pc);

            // Checking ExtendedProperties count
            Assert.Equal(0, pc.Count);
        }

        [Fact]
        public void TestGetHashCode()
        {
            DataColumn dc1;
            int iHashCode1;
            dc1 = new DataColumn();

            iHashCode1 = dc1.GetHashCode();
            for (int i = 0; i < 10; i++)
            {   // must return the same value each time
                // GetHashCode #" + i.ToString());
                Assert.Equal(dc1.GetHashCode(), iHashCode1);
            }
        }

        [Fact]
        public void MaxLength()
        {
            DataColumn dc;
            dc = new DataColumn("ColName", typeof(string));

            //Checking default value (-1)
            // MaxLength default
            Assert.Equal(-1, dc.MaxLength);

            //Checking Set MaxValue
            dc.MaxLength = int.MaxValue;
            //Checking Get MaxValue
            // MaxLength MaxValue
            Assert.Equal(int.MaxValue, dc.MaxLength);

            //Checking Set MinValue
            dc.MaxLength = int.MinValue;
            //Checking Get MinValue
            // MaxLength MinValue
            Assert.Equal(-1, dc.MaxLength);

            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("col", typeof(string)));
            dt.Columns[0].MaxLength = 5;
            dt.Rows.Add(new object[] { "a" });

            //MaxLength = 5
            AssertExtensions.Throws<ArgumentException>(null, () => dt.Rows[0][0] = "123456");
        }

        [Fact]
        public void Namespace()
        {
            DataColumn dc;
            string sName = "NewName";

            dc = new DataColumn();
            //Checking default value ("")
            // Namespace default
            Assert.Equal(string.Empty, dc.Namespace);

            //Checking Set
            dc.Namespace = sName;
            //Checking Get
            // Namespace Get/Set
            Assert.Equal(sName, dc.Namespace);
        }

        [Fact]
        public void Prefix()
        {
            DataColumn dc;
            string sPrefix = "Prefix";
            dc = new DataColumn("ColName", typeof(string));

            // Prefix Checking default value (string.Empty)
            Assert.Equal(string.Empty, dc.Prefix);

            //Checking Set
            dc.Prefix = sPrefix;
            //Checking Get
            // Prefix Checking Get
            Assert.Equal(sPrefix, dc.Prefix);
        }

        [Fact]
        public void ReadOnly()
        {
            DataColumn dc;
            dc = new DataColumn();

            //Checking default value (false)
            // ReadOnly default
            Assert.False(dc.ReadOnly);

            //Checking Set
            dc.ReadOnly = true;
            //Checking Get
            // ReadOnly Get/Set
            Assert.True(dc.ReadOnly);
        }

        [Fact]
        public void Table()
        {
            DataColumn dc;
            dc = new DataColumn();

            //Checking First Get
            // Table test1
            Assert.Null(dc.Table);

            DataTable dt = new DataTable();
            dt.Columns.Add(dc);

            //Checking Second Get
            // Table test2
            Assert.Equal(dt, dc.Table);
        }

        [Fact]
        public void TestToString()
        {
            DataColumn dc;
            string sColName, sExp;
            dc = new DataColumn();

            //ToString = ColumnName
            sColName = "Test1";
            dc.ColumnName = sColName;
            // ToString - ColumnName
            Assert.Equal(sColName, dc.ToString());

            //TosTring = ColumnName + " + " + Expression
            sExp = "Tax * 1.234";
            dc.Expression = sExp;
            // TosTring=ColumnName + Expression
            Assert.Equal(sColName + " + " + sExp, dc.ToString());
        }

        [Fact]
        public void Unique()
        {
            DataColumn dc;
            dc = new DataColumn();
            //Checking default value (false)

            // Unique default
            Assert.False(dc.Unique);

            //Checking Set
            dc.Unique = true;

            //Checking Get
            // Unique Get/Set
            Assert.True(dc.Unique);
        }

        [Fact]
        public void Unique_PrimaryKey()
        {
            DataTable table = new DataTable("Table1");
            DataColumn col = table.Columns.Add("col1");
            table.PrimaryKey = new DataColumn[] { col };

            Assert.True(col.Unique);

            Assert.Throws<ArgumentException>(() => col.Unique = false);

            Assert.True(col.Unique);
        }

        [Fact]
        public void ctor_ByColumnName()
        {
            DataColumn dc;
            string sName = "ColName";
            dc = new DataColumn(sName);

            // ctor - ColName
            Assert.Equal(sName, dc.ColumnName);
        }

        [Fact]
        public void ctor_ByColumnNameType()
        {
            Type typTest;
            DataColumn dc = null;
            Type[] typeArr = { typeof(bool), typeof(byte), typeof(char), typeof(DateTime),
                typeof(decimal), typeof(double), typeof(short), typeof(int),
                typeof(long), typeof(sbyte), typeof(float), typeof(string),
                typeof(TimeSpan), typeof(ushort), typeof(uint), typeof(ulong) };

            foreach (Type type in typeArr)
            {
                dc = new DataColumn("ColName", type);

                // ctor - ColName
                Assert.Equal(type, dc.DataType);
            }
        }

        [Fact]
        public void ctor_ByColumnNameTypeExpression()
        {
            DataColumn col = new DataColumn("ColName", typeof(string), "Price * 1.18");
            Assert.Equal("ColName", col.ColumnName);
            Assert.Equal(typeof(string), col.DataType);
            Assert.Equal("Price * 1.18", col.Expression);
        }

        [Fact]
        public void ctor_ByColumnNameTypeExpressionMappingType()
        {
            DataColumn dc;
            //Checking constructor for each Enum MappingType
            foreach (int i in Enum.GetValues(typeof(MappingType)))
            {
                dc = new DataColumn("ColName", typeof(string), "Price * 1.18", (MappingType)i);
                // Ctor #" + i.ToString());
            }
        }

        [Fact]
        public void ordinal()
        {
            DataColumn dc;
            dc = new DataColumn("ColName", typeof(string));

            //Checking default value
            // Ordinal default value
            Assert.Equal(-1, dc.Ordinal);

            // needs a DataTable.Columns to test
            DataColumnCollection dcColl;
            DataTable tb = new DataTable();
            dcColl = tb.Columns;
            dcColl.Add();   //0
            dcColl.Add();   //1
            dcColl.Add();   //2
            dcColl.Add(dc); //3

            //Checking Get
            // Ordinal Get
            Assert.Equal(3, dc.Ordinal);
        }

        [Fact]
        public void Expression()
        {
            DataColumn dc;
            string sExpression = "Tax * 0.59";
            dc = new DataColumn("ColName", typeof(string));

            Assert.Equal(string.Empty, dc.Expression);

            dc.Expression = sExpression;

            Assert.Equal(sExpression, dc.Expression);
        }

        [Fact]
        public void Expression_Whitespace()
        {
            DataColumn dc = new DataColumn("ColName", typeof(string));

            string plainWhitespace = "    ";
            string surroundWhitespace = "  'abc'  ";

            Assert.Equal(string.Empty, dc.Expression);

            dc.Expression = plainWhitespace;
            Assert.Equal(string.Empty, dc.Expression);

            dc.Expression = surroundWhitespace;
            Assert.Equal(surroundWhitespace, dc.Expression);
        }

        [Fact]
        public void Expression_Exceptions()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                dt.Columns[0].Unique = true;
                dt.Columns[0].Expression = "sum(" + dt.Columns[0].ColumnName + ")";
            });

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataTable dt1 = DataProvider.CreateParentDataTable();
                dt1.Columns[0].AutoIncrement = true;
                dt1.Columns[0].Expression = "sum(" + dt1.Columns[0].ColumnName + ")";
            });

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataTable dt1 = DataProvider.CreateParentDataTable();
                dt1.Constraints.Add(new UniqueConstraint(dt1.Columns[0], false));
                dt1.Columns[0].Expression = "count(" + dt1.Columns[0].ColumnName + ")";
            });

            Assert.Throws<FormatException>(() =>
            {
                DataTable dt1 = DataProvider.CreateParentDataTable();
                dt1.Columns[0].Expression = "CONVERT(" + dt1.Columns[1].ColumnName + ",'System.Int32')";
            });

            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                DataTable dt1 = DataProvider.CreateParentDataTable();
                dt1.Columns[0].Expression = "CONVERT(" + dt1.Columns[0].ColumnName + ",'System.DateTime')";
            });

            Assert.Throws<InvalidCastException>(() =>
            {
                DataTable dt1 = DataProvider.CreateParentDataTable();
                dt1.Columns[1].Expression = "CONVERT(" + dt1.Columns[0].ColumnName + ",'System.DateTime')";
            });

            Assert.Throws<OverflowException>(() =>
            {
                DataTable dt1 = DataProvider.CreateParentDataTable();
                dt1.Columns[1].Expression = "SUBSTRING(" + dt1.Columns[2].ColumnName + ",60000000000,2)";
            });
        }

        [Fact]
        public void Expression_Simple()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            //Simple expression --> not aggregate
            DataColumn dc = new DataColumn("expr", typeof(decimal));
            dt.Columns.Add(dc);
            dt.Columns["expr"].Expression = dt.Columns[0].ColumnName + "*0.52 +" + dt.Columns[0].ColumnName;

            //Check the values
            //double temp;
            string temp;
            string str;

            foreach (DataRow dr in dt.Rows)
            {
                str = (((int)dr[0]) * 0.52 + ((int)dr[0])).ToString();
                if (str.Length > 3)
                    temp = str.Substring(0, 4);
                else
                    temp = str;

                if (dr["expr"].ToString().Length > 3)
                    str = dr["expr"].ToString().Substring(0, 4);
                else
                    str = dr["expr"].ToString();

                if (str == 7.60m.ToString())
                    str = 7.6.ToString();

                Assert.Equal(temp, str);
            }
        }

        [Fact]
        public void Expression_Aggregate()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            //Simple expression -->  aggregate
            DataColumn dc = new DataColumn("expr", typeof(decimal));
            dt.Columns.Add(dc);
            dt.Columns["expr"].Expression = "sum(" + dt.Columns[0].ColumnName + ") + count(" + dt.Columns[0].ColumnName + ")";
            dt.Columns["expr"].Expression += " + avg(" + dt.Columns[0].ColumnName + ") + Min(" + dt.Columns[0].ColumnName + ")";


            //Check the values
            double temp;
            string str;

            double sum = Convert.ToDouble(dt.Compute("sum(" + dt.Columns[0].ColumnName + ")", string.Empty));
            double count = Convert.ToDouble(dt.Compute("count(" + dt.Columns[0].ColumnName + ")", string.Empty));
            double avg = Convert.ToDouble(dt.Compute("avg(" + dt.Columns[0].ColumnName + ")", string.Empty));
            double min = Convert.ToDouble(dt.Compute("min(" + dt.Columns[0].ColumnName + ")", string.Empty));

            str = (sum + count + avg + min).ToString();
            foreach (DataRow dr in dt.Rows)
            {
                if (str.Length > 3)
                {
                    temp = Convert.ToDouble(str.Substring(0, 4));
                }
                else
                {
                    temp = Convert.ToDouble(str);
                }

                Assert.Equal(temp, Convert.ToDouble(dr["expr"]));
            }
        }

        [Fact]
        public void Expression_AggregateRelation()
        {
            DataTable parent = DataProvider.CreateParentDataTable();
            DataTable child = DataProvider.CreateChildDataTable();
            var ds = new DataSet();
            ds.Tables.Add(parent);
            ds.Tables.Add(child);

            ds.Relations.Add("Relation1", parent.Columns[0], child.Columns[0], false);

            //Create the computed columns

            DataColumn dcComputedParent = new DataColumn("computedParent", typeof(double));
            parent.Columns.Add(dcComputedParent);
            dcComputedParent.Expression = "sum(child(Relation1)." + child.Columns[1].ColumnName + ")";

            double preCalculatedExpression;

            foreach (DataRow dr in parent.Rows)
            {
                object o = child.Compute("sum(" + child.Columns[1].ColumnName + ")",
                    parent.Columns[0].ColumnName + "=" + dr[0]);
                if (o == DBNull.Value)
                {
                    Assert.Equal(dr["computedParent"], o);
                }
                else
                {
                    preCalculatedExpression = Convert.ToDouble(o);
                    Assert.Equal(dr["computedParent"], preCalculatedExpression);
                }
            }

            DataColumn dcComputedChild = new DataColumn("computedChild", typeof(double));
            child.Columns.Add(dcComputedChild);
            dcComputedChild.Expression = "Parent." + parent.Columns[0].ColumnName;

            int index = 0;
            double val;
            foreach (DataRow dr in child.Rows)
            {
                val = Convert.ToDouble(dr.GetParentRow("Relation1")[0]);
                Assert.Equal(dr["computedChild"], val);
                index++;
            }
        }

        [Fact]
        public void Expression_IIF()
        {
            DataTable dt = DataProvider.CreateParentDataTable();
            DataColumn dcComputedParent = new DataColumn("computedCol", typeof(double));
            dcComputedParent.DefaultValue = 25.5;
            dt.Columns.Add(dcComputedParent);
            dcComputedParent.Expression = "IIF(" + dt.Columns[0].ColumnName + ">3" + ",1,2)";

            double val;
            foreach (DataRow dr in dt.Rows)
            {
                val = (int)dr[0] > 3 ? 1 : 2;
                Assert.Equal(val, dr["computedCol"]);
            }
            //Now reset the expression and check that the column got its deafult value

            dcComputedParent.Expression = null;
            foreach (DataRow dr in dt.Rows)
            {
                Assert.Equal(25.5, dr["computedCol"]);
            }
        }

        [Fact]
        public void Expression_ISNULL()
        {
            DataSet ds = new DataSet();

            DataTable ptable = new DataTable();
            ptable.Columns.Add("col1", typeof(int));

            DataTable ctable = new DataTable();
            ctable.Columns.Add("col1", typeof(int));
            ctable.Columns.Add("col2", typeof(int));

            ds.Tables.AddRange(new DataTable[] { ptable, ctable });
            ds.Relations.Add("rel1", ptable.Columns[0], ctable.Columns[0]);

            ptable.Rows.Add(new object[] { 1 });
            ptable.Rows.Add(new object[] { 2 });
            for (int i = 0; i < 5; ++i)
                ctable.Rows.Add(new object[] { 1, i });

            // should not throw exception
            ptable.Columns.Add("col2", typeof(int), "IsNull (Sum (Child (rel1).col2), -1)");

            Assert.Equal(10, ptable.Rows[0][1]);
            Assert.Equal(-1, ptable.Rows[1][1]);
        }

        [Fact]
        public void DateTimeMode_DataType()
        {
            DataColumn col = new DataColumn("col", typeof(int));
            Assert.Equal(DataSetDateTime.UnspecifiedLocal, col.DateTimeMode);
            Assert.Throws<InvalidOperationException>(() => col.DateTimeMode = DataSetDateTime.Local);

            col = new DataColumn("col", typeof(DateTime));
            col.DateTimeMode = DataSetDateTime.Utc;
            Assert.Equal(DataSetDateTime.Utc, col.DateTimeMode);
            col.DataType = typeof(int);
            Assert.Equal(DataSetDateTime.UnspecifiedLocal, col.DateTimeMode);
        }

        [Fact]
        public void DateTimeMode_InvalidValues()
        {
            DataColumn col = new DataColumn("col", typeof(DateTime));
            Assert.Throws<InvalidEnumArgumentException>(() => col.DateTimeMode = (DataSetDateTime)(-1));

            Assert.Throws<InvalidEnumArgumentException>(() => col.DateTimeMode = (DataSetDateTime)5);
        }

        [Fact]
        public void DateTimeMode_RowsAdded()
        {
            DataTable table = new DataTable();
            table.Columns.Add("col", typeof(DateTime));
            table.Rows.Add(new object[] { DateTime.Now });

            Assert.Equal(DataSetDateTime.UnspecifiedLocal, table.Columns[0].DateTimeMode);
            // allowed
            table.Columns[0].DateTimeMode = DataSetDateTime.Unspecified;
            table.Columns[0].DateTimeMode = DataSetDateTime.UnspecifiedLocal;

            Assert.Throws<InvalidOperationException>(() => table.Columns[0].DateTimeMode = DataSetDateTime.Local);

            Assert.Throws<InvalidOperationException>(() => table.Columns[0].DateTimeMode = DataSetDateTime.Utc);
        }

        [Fact]
        public void SetOrdinalTest()
        {
            DataColumn col = new DataColumn("col", typeof(int));
            Assert.Throws<ArgumentException>(() => col.SetOrdinal(2));

            DataTable table = new DataTable();
            DataColumn col1 = table.Columns.Add("col1", typeof(int));
            DataColumn col2 = table.Columns.Add("col2", typeof(int));
            DataColumn col3 = table.Columns.Add("col3", typeof(int));

            Assert.Equal("col1", table.Columns[0].ColumnName);
            Assert.Equal("col3", table.Columns[2].ColumnName);

            table.Columns[0].SetOrdinal(2);
            Assert.Equal("col2", table.Columns[0].ColumnName);
            Assert.Equal("col1", table.Columns[2].ColumnName);

            Assert.Equal(0, col2.Ordinal);
            Assert.Equal(1, col3.Ordinal);
            Assert.Equal(2, col1.Ordinal);

            Assert.Throws<ArgumentOutOfRangeException>(() => table.Columns[0].SetOrdinal(-1));

            Assert.Throws<ArgumentOutOfRangeException>(() => table.Columns[0].SetOrdinal(4));
        }
        [Fact]
        public void Bug672113_MulpleColConstraint()
        {
            DataTable FirstTable = new DataTable("First Table");
            DataColumn col0 = new DataColumn("empno", typeof(int));
            DataColumn col1 = new DataColumn("name", typeof(string));
            DataColumn col2 = new DataColumn("age", typeof(int));
            FirstTable.Columns.Add(col0);
            FirstTable.Columns.Add(col1);
            FirstTable.Columns.Add(col2);
            DataColumn[] primkeys = new DataColumn[2];
            primkeys[0] = FirstTable.Columns[0];
            primkeys[1] = FirstTable.Columns[1];
            FirstTable.Constraints.Add("PRIM1", primkeys, true);

            DataTable SecondTable = new DataTable("Second Table");
            col0 = new DataColumn("field1", typeof(int));
            col1 = new DataColumn("field2", typeof(int));
            col2 = new DataColumn("field3", typeof(int));
            SecondTable.Columns.Add(col0);
            SecondTable.Columns.Add(col1);
            SecondTable.Columns.Add(col2);

            primkeys[0] = SecondTable.Columns[0];
            primkeys[1] = SecondTable.Columns[1];
            SecondTable.Constraints.Add("PRIM2", primkeys, true);

            DataRow row1 = FirstTable.NewRow();
            row1["empno"] = 1;
            row1["name"] = "Test";
            row1["age"] = 32;
            FirstTable.Rows.Add(row1);
            FirstTable.AcceptChanges();
            Assert.Equal(32, FirstTable.Rows[0]["age"]);

            row1 = SecondTable.NewRow();
            row1["field1"] = 10000;
            row1["field2"] = 12000;
            row1["field3"] = 1000;
            SecondTable.Rows.Add(row1);
            SecondTable.AcceptChanges();
            Assert.Equal(12000, SecondTable.Rows[0]["field2"]);
        }
    }
}
