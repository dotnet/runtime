// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) Copyright 2002 Franklin Wise
// (C) Copyright 2002 Rodrigo Moya
// (C) Copyright 2003 Daniel Morgan
// (C) Copyright 2003 Martin Willemoes Hansen
// (C) Copyright 2011 Xamarin Inc

//
// Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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

using System.ComponentModel;
using System.Data.SqlTypes;

using Xunit;

namespace System.Data.Tests
{
    public class DataColumnTest
    {
        private DataTable _tbl;

        public DataColumnTest()
        {
            _tbl = new DataTable();
        }

        [Fact]
        public void Ctor()
        {
            string colName = "ColName";
            DataColumn col = new DataColumn();

            //These should all ctor without an exception
            col = new DataColumn(colName);
            col = new DataColumn(colName, typeof(int));
            col = new DataColumn(colName, typeof(int), null);
            col = new DataColumn(colName, typeof(int), null, MappingType.Attribute);
        }

        [Fact]
        public void Constructor3_DataType_Null()
        {
            try
            {
                new DataColumn("ColName", null);
                Assert.False(true);
            }
            catch (ArgumentNullException ex)
            {
                Assert.Equal(typeof(ArgumentNullException), ex.GetType());
                Assert.Null(ex.InnerException);
                // Never premise English.
                //				Assert.NotNull (ex.Message);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("dataType", ex.ParamName);
            }
        }

        [Fact]
        public void AllowDBNull()
        {
            DataColumn col = new DataColumn("NullCheck", typeof(int));
            _tbl.Columns.Add(col);
            col.AllowDBNull = true;
            _tbl.Rows.Add(_tbl.NewRow());
            _tbl.Rows[0]["NullCheck"] = DBNull.Value;
            try
            {
                col.AllowDBNull = false;
                Assert.False(true);
            }
            catch (DataException)
            {
            }
        }

        [Fact]
        public void AllowDBNull1()
        {
            DataTable tbl = _tbl;
            tbl.Columns.Add("id", typeof(int));
            tbl.Columns.Add("name", typeof(string));
            tbl.PrimaryKey = new DataColumn[] { tbl.Columns["id"] };
            tbl.Rows.Add(new object[] { 1, "RowState 1" });
            tbl.Rows.Add(new object[] { 2, "RowState 2" });
            tbl.Rows.Add(new object[] { 3, "RowState 3" });
            tbl.AcceptChanges();
            // Update Table with following changes: Row0 unmodified, 
            // Row1 modified, Row2 deleted, Row3 added, Row4 not-present.
            tbl.Rows[1]["name"] = "Modify 2";
            tbl.Rows[2].Delete();

            DataColumn col = tbl.Columns["name"];
            col.AllowDBNull = true;
            col.AllowDBNull = false;

            Assert.False(col.AllowDBNull);
        }

        [Fact]
        public void AutoIncrement()
        {
            DataColumn col = new DataColumn("Auto", typeof(string));
            col.AutoIncrement = true;

            //Check for Correct Default Values
            Assert.Equal(0L, col.AutoIncrementSeed);
            Assert.Equal(1L, col.AutoIncrementStep);

            //Check for auto type convert
            Assert.Equal(typeof(int), col.DataType);
        }

        [Fact]
        public void AutoIncrementExceptions()
        {
            DataColumn col = new DataColumn();
            col.Expression = "SomeExpression";

            //if computed column exception is thrown
            try
            {
                col.AutoIncrement = true;
                Assert.False(true);
            }
            catch (ArgumentException)
            {
            }
        }

        [Fact]
        public void Caption()
        {
            DataColumn col = new DataColumn("ColName");
            //Caption not set at this point
            Assert.Equal(col.ColumnName, col.Caption);

            //Set caption
            col.Caption = "MyCaption";
            Assert.Equal("MyCaption", col.Caption);

            //Clear caption
            col.Caption = null;
            Assert.Equal(string.Empty, col.Caption);
        }

        [Fact]
        public void DateTimeMode_Valid()
        {
            DataColumn col = new DataColumn("birthdate", typeof(DateTime));
            col.DateTimeMode = DataSetDateTime.Local;
            Assert.Equal(DataSetDateTime.Local, col.DateTimeMode);
            col.DateTimeMode = DataSetDateTime.Unspecified;
            Assert.Equal(DataSetDateTime.Unspecified, col.DateTimeMode);
            col.DateTimeMode = DataSetDateTime.Utc;
            Assert.Equal(DataSetDateTime.Utc, col.DateTimeMode);
        }

        [Fact]
        public void DateTime_DataType_Invalid()
        {
            DataColumn col = new DataColumn("birthdate", typeof(int));
            try
            {
                col.DateTimeMode = DataSetDateTime.Local;
                Assert.False(true);
            }
            catch (InvalidOperationException ex)
            {
                // The DateTimeMode can be set only on DataColumns
                // of type DateTime
                Assert.Equal(typeof(InvalidOperationException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("DateTimeMode") != -1);
                Assert.True(ex.Message.IndexOf("DateTime") != -1);
            }
        }

        [Fact]
        public void DateTimeMode_Invalid()
        {
            DataColumn col = new DataColumn("birthdate", typeof(DateTime));
            try
            {
                col.DateTimeMode = (DataSetDateTime)666;
                Assert.False(true);
            }
            catch (InvalidEnumArgumentException ex)
            {
                // The DataSetDateTime enumeration value, 666, is invalid
                Assert.Equal(typeof(InvalidEnumArgumentException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("DataSetDateTime") != -1);
                Assert.True(ex.Message.IndexOf("666") != -1);
                Assert.Null(ex.ParamName);
            }
        }

        [Fact]
        public void ForColumnNameException()
        {
            DataColumn col = new DataColumn();
            DataColumn col2 = new DataColumn();
            DataColumn col3 = new DataColumn();
            DataColumn col4 = new DataColumn();

            col.ColumnName = "abc";
            Assert.Equal("abc", col.ColumnName);

            _tbl.Columns.Add(col);

            //Duplicate name exception
            try
            {
                col2.ColumnName = "abc";
                _tbl.Columns.Add(col2);
                Assert.Equal("abc", col2.ColumnName);
                Assert.False(true);
            }
            catch (DuplicateNameException)
            {
            }
            // Make sure case matters in duplicate checks
            col3.ColumnName = "ABC";
            _tbl.Columns.Add(col3);
        }

        [Fact]
        public void DefaultValue()
        {
            DataTable tbl = new DataTable();
            tbl.Columns.Add("MyCol", typeof(int));

            //Set default Value if Autoincrement is true
            tbl.Columns[0].AutoIncrement = true;
            try
            {
                tbl.Columns[0].DefaultValue = 2;
                Assert.False(true);
            }
            catch (ArgumentException)
            {
            }

            tbl.Columns[0].AutoIncrement = false;

            //Set default value to an incompatible datatype
            try
            {
                tbl.Columns[0].DefaultValue = "hello";
                Assert.False(true);
            }
            catch (FormatException)
            {
            }
        }

        [Fact]
        public void Defaults1()
        {
            //Check for defaults - ColumnName not set at the beginning
            DataTable table = new DataTable();
            DataColumn column = new DataColumn();

            Assert.Equal(string.Empty, column.ColumnName);
            Assert.Equal(typeof(string), column.DataType);

            table.Columns.Add(column);

            Assert.Equal("Column1", table.Columns[0].ColumnName);
            Assert.Equal(typeof(string), table.Columns[0].DataType);

            DataRow row = table.NewRow();
            table.Rows.Add(row);
            DataRow dataRow = table.Rows[0];

            object v = dataRow.ItemArray[0];
            Assert.Equal(typeof(DBNull), v.GetType());
            Assert.Equal(DBNull.Value, v);
        }

        [Fact]
        public void Defaults2()
        {
            //Check for defaults - ColumnName set at the beginning
            string blah = "Blah";
            //Check for defaults - ColumnName not set at the beginning
            DataTable table = new DataTable();
            DataColumn column = new DataColumn(blah);

            Assert.Equal(blah, column.ColumnName);
            Assert.Equal(typeof(string), column.DataType);

            table.Columns.Add(column);

            Assert.Equal(blah, table.Columns[0].ColumnName);
            Assert.Equal(typeof(string), table.Columns[0].DataType);

            DataRow row = table.NewRow();
            table.Rows.Add(row);
            DataRow dataRow = table.Rows[0];

            object v = dataRow.ItemArray[0];
            Assert.Equal(typeof(DBNull), v.GetType());
            Assert.Equal(DBNull.Value, v);
        }

        [Fact]
        public void Defaults3()
        {
            DataColumn col = new DataColumn("foo", typeof(SqlBoolean));
            Assert.Equal(SqlBoolean.Null, col.DefaultValue);
            col.DefaultValue = SqlBoolean.True;
            // FIXME: not working yet
            //col.DefaultValue = true;
            //Assert.Equal (SqlBoolean.True, col.DefaultValue);
            col.DefaultValue = DBNull.Value;
            Assert.Equal(SqlBoolean.Null, col.DefaultValue);
        }

        [Fact]
        public void ChangeTypeAfterSettingDefaultValue()
        {
            Assert.Throws<DataException>(() =>
           {
               DataColumn col = new DataColumn("foo", typeof(SqlBoolean));
               col.DefaultValue = true;
               col.DataType = typeof(int);
           });
        }

        [Fact]
        public void ExpressionSubstringlimits()
        {
            DataTable t = new DataTable();
            t.Columns.Add("aaa");
            t.Rows.Add(new object[] { "xxx" });
            DataColumn c = t.Columns.Add("bbb");
            try
            {
                c.Expression = "SUBSTRING(aaa, 6000000000000000, 2)";
                Assert.False(true);
            }
            catch (OverflowException)
            {
            }
        }

        [Fact]
        public void ExpressionFunctions()
        {
            DataTable T = new DataTable("test");
            DataColumn C = new DataColumn("name");
            T.Columns.Add(C);
            C = new DataColumn("age");
            C.DataType = typeof(int);
            T.Columns.Add(C);
            C = new DataColumn("id");
            C.Expression = "substring (name, 1, 3) + len (name) + age";
            T.Columns.Add(C);

            DataSet Set = new DataSet("TestSet");
            Set.Tables.Add(T);

            DataRow Row = null;
            for (int i = 0; i < 100; i++)
            {
                Row = T.NewRow();
                Row[0] = "human" + i;
                Row[1] = i;
                T.Rows.Add(Row);
            }

            Row = T.NewRow();
            Row[0] = "h*an";
            Row[1] = DBNull.Value;
            T.Rows.Add(Row);

            Assert.Equal("hum710", T.Rows[10][2]);
            Assert.Equal("hum64", T.Rows[4][2]);
            C = T.Columns[2];
            C.Expression = "isnull (age, 'succ[[]]ess')";
            Assert.Equal("succ[[]]ess", T.Rows[100][2]);

            C.Expression = "iif (age = 24, 'hurrey', 'boo')";
            Assert.Equal("boo", T.Rows[50][2]);
            Assert.Equal("hurrey", T.Rows[24][2]);

            C.Expression = "convert (age, 'System.Boolean')";
            Assert.Equal(bool.TrueString, T.Rows[50][2]);
            Assert.Equal(bool.FalseString, T.Rows[0][2]);

            //
            // Exceptions
            //

            try
            {
                // The expression contains undefined function call iff().
                C.Expression = "iff (age = 24, 'hurrey', 'boo')";
                Assert.False(true);
            }
            catch (EvaluateException)
            {
            }
            catch (SyntaxErrorException)
            {
            }

            //The following two cases fail on mono. MS.net evaluates the expression
            //immediatly upon assignment. We don't do this yet hence we don't throw
            //an exception at this point.
            try
            {
                C.Expression = "iif (nimi = 24, 'hurrey', 'boo')";
                Assert.False(true);
            }
            catch (EvaluateException e)
            {
                Assert.Equal(typeof(EvaluateException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Cannot find column [nimi].", e.Message);
            }

            try
            {
                C.Expression = "iif (name = 24, 'hurrey', 'boo')";
                Assert.False(true);
            }
            catch (EvaluateException e)
            {
                Assert.Equal(typeof(EvaluateException), e.GetType());
                //AssertEquals ("DC41", "Cannot perform '=' operation on System.String and System.Int32.", e.Message);
            }

            try
            {
                C.Expression = "convert (age, Boolean)";
                Assert.False(true);
            }
            catch (EvaluateException e)
            {
                Assert.Equal(typeof(EvaluateException), e.GetType());
                // Never premise English.
                //Assert.Equal ("Invalid type name 'Boolean'.", e.Message);
            }
        }

        [Fact]
        public void ExpressionAggregates()
        {
            DataTable T = new DataTable("test");
            DataTable T2 = new DataTable("test2");

            DataColumn C = new DataColumn("name");
            T.Columns.Add(C);
            C = new DataColumn("age");
            C.DataType = typeof(int);
            T.Columns.Add(C);
            C = new DataColumn("childname");
            T.Columns.Add(C);

            C = new DataColumn("expression");
            T.Columns.Add(C);

            DataSet Set = new DataSet("TestSet");
            Set.Tables.Add(T);
            Set.Tables.Add(T2);

            DataRow Row = null;
            for (int i = 0; i < 100; i++)
            {
                Row = T.NewRow();
                Row[0] = "human" + i;
                Row[1] = i;
                Row[2] = "child" + i;
                T.Rows.Add(Row);
            }

            Row = T.NewRow();
            Row[0] = "h*an";
            Row[1] = DBNull.Value;
            T.Rows.Add(Row);

            C = new DataColumn("name");
            T2.Columns.Add(C);
            C = new DataColumn("age");
            C.DataType = typeof(int);
            T2.Columns.Add(C);

            for (int i = 0; i < 100; i++)
            {
                Row = T2.NewRow();
                Row[0] = "child" + i;
                Row[1] = i;
                T2.Rows.Add(Row);
                Row = T2.NewRow();
                Row[0] = "child" + i;
                Row[1] = i - 2;
                T2.Rows.Add(Row);
            }

            DataRelation Rel = new DataRelation("Rel", T.Columns[2], T2.Columns[0]);
            Set.Relations.Add(Rel);

            C = T.Columns[3];
            C.Expression = "Sum (Child.age)";
            Assert.Equal("-2", T.Rows[0][3]);
            Assert.Equal("98", T.Rows[50][3]);

            C.Expression = "Count (Child.age)";
            Assert.Equal("2", T.Rows[0][3]);
            Assert.Equal("2", T.Rows[60][3]);

            C.Expression = "Avg (Child.age)";
            Assert.Equal("-1", T.Rows[0][3]);
            Assert.Equal("59", T.Rows[60][3]);

            C.Expression = "Min (Child.age)";
            Assert.Equal("-2", T.Rows[0][3]);
            Assert.Equal("58", T.Rows[60][3]);

            C.Expression = "Max (Child.age)";
            Assert.Equal("0", T.Rows[0][3]);
            Assert.Equal("60", T.Rows[60][3]);

            C.Expression = "stdev (Child.age)";
            Assert.Equal((1.4142135623731).ToString(T.Locale), T.Rows[0][3]);
            Assert.Equal((1.4142135623731).ToString(T.Locale), T.Rows[60][3]);

            C.Expression = "var (Child.age)";
            Assert.Equal("2", T.Rows[0][3]);
            Assert.Equal("2", T.Rows[60][3]);
        }

        [Fact]
        public void ExpressionOperator()
        {
            DataTable T = new DataTable("test");
            DataColumn C = new DataColumn("name");
            T.Columns.Add(C);
            C = new DataColumn("age");
            C.DataType = typeof(int);
            T.Columns.Add(C);
            C = new DataColumn("id");
            C.Expression = "substring (name, 1, 3) + len (name) + age";
            T.Columns.Add(C);

            DataSet Set = new DataSet("TestSet");
            Set.Tables.Add(T);

            DataRow Row = null;
            for (int i = 0; i < 100; i++)
            {
                Row = T.NewRow();
                Row[0] = "human" + i;
                Row[1] = i;
                T.Rows.Add(Row);
            }

            Row = T.NewRow();
            Row[0] = "h*an";
            Row[1] = DBNull.Value;
            T.Rows.Add(Row);

            C = T.Columns[2];
            C.Expression = "age + 4";
            Assert.Equal("68", T.Rows[64][2]);

            C.Expression = "age - 4";
            Assert.Equal("60", T.Rows[64][2]);

            C.Expression = "age * 4";
            Assert.Equal("256", T.Rows[64][2]);

            C.Expression = "age / 4";
            Assert.Equal("16", T.Rows[64][2]);

            C.Expression = "age % 5";
            Assert.Equal("4", T.Rows[64][2]);

            C.Expression = "age in (5, 10, 15, 20, 25)";
            Assert.Equal("False", T.Rows[64][2]);
            Assert.Equal("True", T.Rows[25][2]);

            C.Expression = "name like 'human1%'";
            Assert.Equal("True", T.Rows[1][2]);
            Assert.Equal("False", T.Rows[25][2]);

            C.Expression = "age < 4";
            Assert.Equal("False", T.Rows[4][2]);
            Assert.Equal("True", T.Rows[3][2]);

            C.Expression = "age <= 4";
            Assert.Equal("True", T.Rows[4][2]);
            Assert.Equal("False", T.Rows[5][2]);

            C.Expression = "age > 4";
            Assert.Equal("False", T.Rows[4][2]);
            Assert.Equal("True", T.Rows[5][2]);

            C.Expression = "age >= 4";
            Assert.Equal("True", T.Rows[4][2]);
            Assert.Equal("False", T.Rows[1][2]);

            C.Expression = "age = 4";
            Assert.Equal("True", T.Rows[4][2]);
            Assert.Equal("False", T.Rows[1][2]);

            C.Expression = "age <> 4";
            Assert.Equal("False", T.Rows[4][2]);
            Assert.Equal("True", T.Rows[1][2]);
        }

        [Fact]
        public void SetMaxLengthException()
        {
            // Setting MaxLength on SimpleContent -> exception
            DataSet ds = new DataSet("Example");
            ds.Tables.Add("MyType");
            ds.Tables["MyType"].Columns.Add(new DataColumn("Desc",
                typeof(string), "", MappingType.SimpleContent));
            try
            {
                ds.Tables["MyType"].Columns["Desc"].MaxLength = 32;
                Assert.False(true);
            }
            catch (ArgumentException)
            {
            }
        }

        [Fact]
        public void SetMaxLengthNegativeValue()
        {
            // however setting MaxLength on SimpleContent is OK
            DataSet ds = new DataSet("Example");
            ds.Tables.Add("MyType");
            ds.Tables["MyType"].Columns.Add(
                new DataColumn("Desc", typeof(string), "", MappingType.SimpleContent));
            ds.Tables["MyType"].Columns["Desc"].MaxLength = -1;
        }

        [Fact]
        public void AdditionToConstraintCollectionTest()
        {
            DataTable myTable = new DataTable("myTable");
            DataColumn idCol = new DataColumn("id", typeof(int));
            idCol.Unique = true;
            myTable.Columns.Add(idCol);
            ConstraintCollection cc = myTable.Constraints;
            //cc just contains a single UniqueConstraint object.
            UniqueConstraint uc = cc[0] as UniqueConstraint;
            Assert.Equal("id", uc.Columns[0].ColumnName);
        }

        [Fact]
        public void CalcStatisticalFunction_SingleElement()
        {
            DataTable table = new DataTable();
            table.Columns.Add("test", typeof(int));

            table.Rows.Add(new object[] { 0 });
            table.Columns.Add("result_var", typeof(double), "var(test)");
            table.Columns.Add("result_stdev", typeof(double), "stdev(test)");

            // Check DBNull.Value is set as the result 
            Assert.Equal(typeof(DBNull), (table.Rows[0]["result_var"]).GetType());
            Assert.Equal(typeof(DBNull), (table.Rows[0]["result_stdev"]).GetType());
        }

        [Fact]
        public void Aggregation_CheckIfChangesDynamically()
        {
            DataTable table = new DataTable();

            table.Columns.Add("test", typeof(int));
            table.Columns.Add("result_count", typeof(int), "count(test)");
            table.Columns.Add("result_sum", typeof(int), "sum(test)");
            table.Columns.Add("result_avg", typeof(int), "avg(test)");
            table.Columns.Add("result_max", typeof(int), "max(test)");
            table.Columns.Add("result_min", typeof(int), "min(test)");
            table.Columns.Add("result_var", typeof(double), "var(test)");
            table.Columns.Add("result_stdev", typeof(double), "stdev(test)");

            // Adding the rows after all the expression columns are added
            table.Rows.Add(new object[] { 0 });
            Assert.Equal(1, table.Rows[0]["result_count"]);
            Assert.Equal(0, table.Rows[0]["result_sum"]);
            Assert.Equal(0, table.Rows[0]["result_avg"]);
            Assert.Equal(0, table.Rows[0]["result_max"]);
            Assert.Equal(0, table.Rows[0]["result_min"]);
            Assert.Equal(DBNull.Value, table.Rows[0]["result_var"]);
            Assert.Equal(DBNull.Value, table.Rows[0]["result_stdev"]);

            table.Rows.Add(new object[] { 1 });
            table.Rows.Add(new object[] { -2 });

            // Check if the aggregate columns are updated correctly
            Assert.Equal(3, table.Rows[0]["result_count"]);
            Assert.Equal(-1, table.Rows[0]["result_sum"]);
            Assert.Equal(0, table.Rows[0]["result_avg"]);
            Assert.Equal(1, table.Rows[0]["result_max"]);
            Assert.Equal(-2, table.Rows[0]["result_min"]);
            Assert.Equal((7.0 / 3), table.Rows[0]["result_var"]);
            Assert.Equal(Math.Sqrt(7.0 / 3), table.Rows[0]["result_stdev"]);
        }

        [Fact]
        public void Aggregation_CheckIfChangesDynamically_ChildTable()
        {
            DataSet ds = new DataSet();

            DataTable table = new DataTable();
            DataTable table2 = new DataTable();
            ds.Tables.Add(table);
            ds.Tables.Add(table2);

            table.Columns.Add("test", typeof(int));
            table2.Columns.Add("test", typeof(int));
            table2.Columns.Add("val", typeof(int));
            DataRelation rel = new DataRelation("rel", table.Columns[0], table2.Columns[0]);
            ds.Relations.Add(rel);

            table.Columns.Add("result_count", typeof(int), "count(child.test)");
            table.Columns.Add("result_sum", typeof(int), "sum(child.test)");
            table.Columns.Add("result_avg", typeof(int), "avg(child.test)");
            table.Columns.Add("result_max", typeof(int), "max(child.test)");
            table.Columns.Add("result_min", typeof(int), "min(child.test)");
            table.Columns.Add("result_var", typeof(double), "var(child.test)");
            table.Columns.Add("result_stdev", typeof(double), "stdev(child.test)");

            table.Rows.Add(new object[] { 1 });
            table.Rows.Add(new object[] { 2 });
            // Add rows to the child table
            for (int j = 0; j < 10; j++)
                table2.Rows.Add(new object[] { 1, j });

            // Check the values for the expression columns in parent table 
            Assert.Equal(10, table.Rows[0]["result_count"]);
            Assert.Equal(0, table.Rows[1]["result_count"]);

            Assert.Equal(10, table.Rows[0]["result_sum"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["result_sum"]);

            Assert.Equal(1, table.Rows[0]["result_avg"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["result_avg"]);

            Assert.Equal(1, table.Rows[0]["result_max"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["result_max"]);

            Assert.Equal(1, table.Rows[0]["result_min"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["result_min"]);

            Assert.Equal(0.0, table.Rows[0]["result_var"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["result_var"]);

            Assert.Equal(0.0, table.Rows[0]["result_stdev"]);
            Assert.Equal(DBNull.Value, table.Rows[1]["result_stdev"]);
        }

        [Fact]
        public void Aggregation_TestForSyntaxErrors()
        {
            string error = "Aggregation functions cannot be called on Singular(Parent) Columns";
            DataSet ds = new DataSet();
            DataTable table1 = new DataTable();
            DataTable table2 = new DataTable();
            DataTable table3 = new DataTable();

            table1.Columns.Add("test", typeof(int));
            table2.Columns.Add("test", typeof(int));
            table3.Columns.Add("test", typeof(int));

            ds.Tables.Add(table1);
            ds.Tables.Add(table2);
            ds.Tables.Add(table3);

            DataRelation rel1 = new DataRelation("rel1", table1.Columns[0], table2.Columns[0]);
            DataRelation rel2 = new DataRelation("rel2", table2.Columns[0], table3.Columns[0]);

            ds.Relations.Add(rel1);
            ds.Relations.Add(rel2);

            error = "Aggregation Functions cannot be called on Columns Returning Single Row (Parent Column)";
            try
            {
                table2.Columns.Add("result", typeof(int), "count(parent.test)");
                Assert.False(true);
            }
            catch (SyntaxErrorException)
            {
            }

            error = "Numerical or Functions cannot be called on Columns Returning Multiple Rows (Child Column)";
            // Check arithematic operator
            try
            {
                table2.Columns.Add("result", typeof(int), "10*(child.test)");
                Assert.False(true);
            }
            catch (SyntaxErrorException)
            {
            }

            // Check rel operator
            try
            {
                table2.Columns.Add("result", typeof(int), "(child.test) > 10");
                Assert.False(true);
            }
            catch (SyntaxErrorException)
            {
            }

            // Check predicates 
            try
            {
                table2.Columns.Add("result", typeof(int), "(child.test) IN (1,2,3)");
                Assert.False(true);
            }
            catch (SyntaxErrorException)
            {
            }

            try
            {
                table2.Columns.Add("result", typeof(int), "(child.test) LIKE 1");
                Assert.False(true);
            }
            catch (SyntaxErrorException)
            {
            }

            try
            {
                table2.Columns.Add("result", typeof(int), "(child.test) IS null");
                Assert.False(true);
            }
            catch (SyntaxErrorException)
            {
            }

            // Check Calc Functions
            try
            {
                table2.Columns.Add("result", typeof(int), "isnull(child.test,10)");
                Assert.False(true);
            }
            catch (SyntaxErrorException)
            {
            }
        }

        [Fact]
        public void CheckValuesAfterRemovedFromCollection()
        {
            DataTable table = new DataTable("table1");
            DataColumn col1 = new DataColumn("col1", typeof(int));
            DataColumn col2 = new DataColumn("col2", typeof(int));

            Assert.Equal(-1, col1.Ordinal);
            Assert.Null(col1.Table);

            table.Columns.Add(col1);
            table.Columns.Add(col2);
            Assert.Equal(0, col1.Ordinal);
            Assert.Equal(table, col1.Table);

            table.Columns.RemoveAt(0);
            Assert.Equal(-1, col1.Ordinal);
            Assert.Null(col1.Table);

            table.Columns.Clear();
            Assert.Equal(-1, col2.Ordinal);
            Assert.Null(col2.Table);
        }

        [Fact]
        public void B565616_NonIConvertibleTypeTest()
        {
            try
            {
                DataTable dt = new DataTable();
                Guid id = Guid.NewGuid();
                dt.Columns.Add("ID", typeof(string));
                DataRow row = dt.NewRow();
                row["ID"] = id;
                Assert.Equal(id.ToString(), row["ID"]);
            }
            catch (InvalidCastException ex)
            {
                Assert.False(true);
            }
        }

        [Fact]
        public void B623451_SetOrdinalTest()
        {
            try
            {
                DataTable t = new DataTable();
                t.Columns.Add("one");
                t.Columns.Add("two");
                t.Columns.Add("three");
                Assert.Equal("one", t.Columns[0].ColumnName);
                Assert.Equal("two", t.Columns[1].ColumnName);
                Assert.Equal("three", t.Columns[2].ColumnName);

                t.Columns["three"].SetOrdinal(0);
                Assert.Equal("three", t.Columns[0].ColumnName);
                Assert.Equal("one", t.Columns[1].ColumnName);
                Assert.Equal("two", t.Columns[2].ColumnName);

                t.Columns["three"].SetOrdinal(1);
                Assert.Equal("one", t.Columns[0].ColumnName);
                Assert.Equal("three", t.Columns[1].ColumnName);
                Assert.Equal("two", t.Columns[2].ColumnName);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Assert.False(true);
            }
        }

        [Fact]
        public void Xamarin665()
        {
            var t = new DataTable();
            var c1 = t.Columns.Add("c1");
            var c2 = t.Columns.Add("c2");
            c2.Expression = "TRIM(ISNULL(c1,' '))";
            c2.Expression = "SUBSTRING(ISNULL(c1,' '), 1, 10)";
        }

        private DataColumn MakeColumn(string col, string test)
        {
            return new DataColumn()
            {
                ColumnName = col,
                Expression = test
            };
        }

#if false
// Check Windows output for the row [0] value
		[Fact]
		public void NullStrings ()
		{
			var a = MakeColumn ("nullbar", "null+'bar'");
			var b = MakeColumn ("barnull", "'bar'+null");
			var c = MakeColumn ("foobar", "'foo'+'bar'");

		        var table = new DataTable();
		        
		        table.Columns.Add(a);
		        table.Columns.Add(b);
		        table.Columns.Add(c);
		
		        var row = table.NewRow();
		        table.Rows.Add(row);
			Assert.Equal (row [0], DBNull.Value);
			Assert.Equal (row [1], DBNull.Value);
			Assert.Equal (row [2], "foobar");
		}
#endif
    }
}
