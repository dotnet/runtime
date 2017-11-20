// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using System.Reflection;
using System.Text;
using Microsoft.SqlServer.Server;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class UdtTest2
    {
        private string _connStr = null;

        public UdtTest2()
        {
            _connStr = (new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) { InitialCatalog = "UdtTestDb" }).ConnectionString;
        }

        [CheckConnStrSetupFact]
        public void UDTParams_Early()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.Transaction = conn.BeginTransaction();
                cmd.CommandText = "vicinity"; // select proc
                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter p = cmd.Parameters.Add("@boundary", SqlDbType.Udt);
                p.UdtTypeName = "UdtTestDb.dbo.Point";
                Point pt = new Point()
                {
                    X = 250,
                    Y = 250
                };
                p.Value = pt;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    DataTestUtility.AssertEqualsWithDescription(
                        (new Point(250, 250)).ToString(), p.Value.ToString(),
                        "Unexpected Point value.");
                }
            }
        }

        [CheckConnStrSetupFact]
        public void UDTParams_Binary()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("vicinity", conn))
            {
                conn.Open();
                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter p = cmd.Parameters.Add("@boundary", SqlDbType.VarBinary, 8);
                p.Direction = ParameterDirection.Input;

                byte[] value = new byte[8];
                value[0] = 0xF0;
                value[1] = 0;
                value[2] = 0;
                value[3] = 0;
                value[4] = 0xF0;
                value[5] = 0;
                value[6] = 0;
                value[7] = 0;
                p.Value = new SqlBinary(value);

                DataTestUtility.AssertThrowsWrapper<SqlException>(
                    () => cmd.ExecuteReader(),
                    "Error converting data type varbinary to Point.");
            }
        }

        [CheckConnStrSetupFact]
        public void UDTParams_Invalid2()
        {
            string spInsertCustomer = DataTestUtility.GetUniqueNameForSqlServer("spUdtTest2_InsertCustomer");
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("UdtTest2");

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.Transaction = conn.BeginTransaction();
                cmd.CommandText = "create table " + tableName + " (name nvarchar(30), address Address)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "create proc " + spInsertCustomer + "(@name nvarchar(30), @addr Address OUTPUT)" + " AS insert into " + tableName + " values (@name, @addr)";
                cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = spInsertCustomer;
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter pName = cmd.Parameters.Add("@fname", SqlDbType.NVarChar, 20);
                    SqlParameter p = cmd.Parameters.Add("@addr", SqlDbType.Udt);

                    Address addr = Address.Parse("customer whose name is address");
                    p.UdtTypeName = "UdtTestDb.dbo.Address";
                    p.Value = addr;
                    pName.Value = addr;

                    DataTestUtility.AssertThrowsWrapper<InvalidCastException>(
                        () => cmd.ExecuteReader(),
                        "Failed to convert parameter value from a Address to a String.");
                }
                finally
                {
                    cmd.Transaction.Rollback();
                }
            }
        }

        [CheckConnStrSetupFact]
        public void UDTParams_Invalid()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("vicinity", conn))
            {
                conn.Open();
                cmd.CommandType = CommandType.StoredProcedure;

                SqlParameter p = cmd.Parameters.Add("@boundary", SqlDbType.Udt);
                p.UdtTypeName = "UdtTestDb.dbo.Point";
                p.Value = 32;

                DataTestUtility.AssertThrowsWrapper<ArgumentException>(
                    () => cmd.ExecuteReader(),
                    "Specified type is not registered on the target server. System.Int32");
            }
        }

        [CheckConnStrSetupFact]
        public void UDTParams_TypedNull()
        {
            string spInsertCustomer = DataTestUtility.GetUniqueNameForSqlServer("spUdtTest2_InsertCustomer");
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("UdtTest2_Customer");

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.Transaction = conn.BeginTransaction();
                cmd.CommandText = "create table " + tableName + " (name nvarchar(30), address Address)";
                cmd.ExecuteNonQuery();

                // create proc sp_insert_customer(@name nvarchar(30), @addr Address OUTPUT)
                // AS
                // insert into customers values (@name, @addr)
                cmd.CommandText = "create proc " + spInsertCustomer + " (@name nvarchar(30), @addr Address OUTPUT)" + " AS insert into " + tableName + " values (@name, @addr)";
                cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = spInsertCustomer;
                    cmd.CommandType = CommandType.StoredProcedure;

                    Address addr = Address.Parse("123 baker st || Redmond");
                    SqlParameter pName = cmd.Parameters.Add("@name", SqlDbType.NVarChar, 20);
                    SqlParameter p = cmd.Parameters.Add("@addr", SqlDbType.Udt);

                    p.UdtTypeName = "UdtTestDb.dbo.Address";
                    p.Value = Address.Null;
                    pName.Value = "john";
                    cmd.ExecuteNonQuery();

                    DataTestUtility.AssertEqualsWithDescription(
                        Address.Null.ToString(), p.Value.ToString(),
                        "Unexpected parameter value.");
                }
                finally
                {
                    cmd.Transaction.Rollback();
                }
            }
        }

        [CheckConnStrSetupFact]
        public void UDTParams_NullInput()
        {
            string spInsertCustomer = DataTestUtility.GetUniqueNameForSqlServer("spUdtTest2_InsertCustomer");
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("UdtTest2_Customer");

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.Transaction = conn.BeginTransaction();
                cmd.CommandText = "create table " + tableName + " (name nvarchar(30), address Address)";
                cmd.ExecuteNonQuery();
                cmd.CommandText = "create proc " + spInsertCustomer + "(@name nvarchar(30), @addr Address OUTPUT)" + " AS insert into " + tableName + " values (@name, @addr)";
                cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = spInsertCustomer;
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter pName = cmd.Parameters.Add("@name", SqlDbType.NVarChar, 20);
                    SqlParameter p = cmd.Parameters.Add("@addr", SqlDbType.Udt);

                    p.UdtTypeName = "UdtTestDb.dbo.Address";
                    p.Value = null;
                    pName.Value = "john";

                    string spInsertCustomerNoBrackets = spInsertCustomer;
                    if (spInsertCustomer.StartsWith("[") && spInsertCustomer.EndsWith("]"))
                        spInsertCustomerNoBrackets = spInsertCustomer.Substring(1, spInsertCustomer.Length - 2);
                    string errorMsg = "Procedure or function '" + spInsertCustomerNoBrackets + "' expects parameter '@addr', which was not supplied.";

                    DataTestUtility.AssertThrowsWrapper<SqlException>(
                        () => cmd.ExecuteNonQuery(),
                        errorMsg);
                }
                finally
                {
                    cmd.Transaction.Rollback();
                }
            }
        }

        [CheckConnStrSetupFact]
        public void UDTParams_InputOutput()
        {
            string spInsertCity = DataTestUtility.GetUniqueNameForSqlServer("spUdtTest2_InsertCity");
            string tableName = DataTestUtility.GetUniqueNameForSqlServer("UdtTest2");

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                SqlTransaction tx = conn.BeginTransaction();
                SqlCommand cmd = conn.CreateCommand();

                cmd.Transaction = tx;

                // create the table
                cmd.CommandText = "create table " + tableName + " (name sysname,location Point)";
                cmd.ExecuteNonQuery();

                // create sp
                cmd.CommandText = "create proc " + spInsertCity + "(@name sysname, @location Point OUTPUT)" + " AS insert into " + tableName + " values (@name, @location)";
                cmd.ExecuteNonQuery();
                try
                {
                    cmd.CommandText = spInsertCity;
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter pName = cmd.Parameters.Add("@name", SqlDbType.NVarChar, 20);
                    SqlParameter p = cmd.Parameters.Add("@location", SqlDbType.Udt);

                    Point pt = new Point(100, 100);
                    p.UdtTypeName = "Point";
                    p.Direction = ParameterDirection.InputOutput;
                    p.Value = pt;
                    pName.Value = "newcity";

                    cmd.ExecuteNonQuery();
                    DataTestUtility.AssertEqualsWithDescription(
                        "141.42135623731", ((Point)(p.Value)).Distance().ToString(),
                        "Unexpected distance value.");
                    DataTestUtility.AssertEqualsWithDescription(
                        "141.42135623731", ((Point)(p.Value)).Distance().ToString(),
                        "Unexpected distance value after reading out param again.");

                    cmd.Parameters.Clear();
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "select * from " + tableName;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        string expectedValue = "   newcity, p.X = 100, p.Y = 100, p.Distance() = 141.42135623731" + Environment.NewLine;
                        DataTestUtility.AssertEqualsWithDescription(
                            expectedValue, UdtTestHelpers.DumpReaderString(reader, false),
                            "Unexpected reader dump string.");
                    }
                }
                finally
                {
                    tx.Rollback();
                }
            }
        }

        [CheckConnStrSetupFact]
        public void UDTFields_WrongType()
        {
            using (SqlConnection cn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("select name,location from cities order by name", cn))
            {
                cn.Open();
                cmd.CommandType = CommandType.Text;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();

                    DataTestUtility.AssertEqualsWithDescription(
                        "beaverton", reader.GetValue(0),
                        "Unexpected reader value.");
                    DataTestUtility.AssertEqualsWithDescription(
                        "14.8660687473185", ((Point)reader.GetValue(1)).Distance().ToString(),
                        "Unexpected distance value.");

                    reader.Read();

                    // retrieve the UDT as a string
                    DataTestUtility.AssertThrowsWrapper<InvalidCastException>(
                        () => reader.GetString(1),
                        "Unable to cast object of type 'System.Byte[]' to type 'System.String'.");
                }
            }
        }

        [CheckConnStrSetupFact]
        public void UDT_DataSetFill()
        {
            using (SqlConnection cn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("select * from cities", cn))
            using (SqlDataAdapter adapter = new SqlDataAdapter("select * from cities", cn))
            {
                cn.Open();

                cmd.CommandType = CommandType.Text;
                adapter.SelectCommand = cmd;

                DataSet ds = new DataSet("newset");

                adapter.Fill(ds);
                DataTestUtility.AssertEqualsWithDescription(
                    1, ds.Tables.Count,
                    "Unexpected Tables count.");
                DataTestUtility.AssertEqualsWithDescription(
                    typeof(Point), ds.Tables[0].Columns[1].DataType,
                    "Unexpected DataType.");
            }
        }

        [CheckConnStrSetupFact]
        public void Reader_PointEarly()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("select name, location from cities", conn))
            {
                conn.Open();

                string expectedReaderValues =
                    "ColumnName[0] = name" + Environment.NewLine +
                    "DataType[0] = nvarchar" + Environment.NewLine +
                    "FieldType[0] = System.String" + Environment.NewLine +
                    "ColumnName[1] = location" + Environment.NewLine +
                    "DataType[1] = UdtTestDb.dbo.Point" + Environment.NewLine +
                    "FieldType[1] = Point" + Environment.NewLine +
                    "   redmond, p.X =   3, p.Y =   3, p.Distance() = 5" + Environment.NewLine +
                    "  bellevue, p.X =   6, p.Y =   6, p.Distance() = 10" + Environment.NewLine +
                    "   seattle, p.X =  10, p.Y =  10, p.Distance() = 14.8660687473185" + Environment.NewLine +
                    "  portland, p.X =  20, p.Y =  20, p.Distance() = 25" + Environment.NewLine +
                    "        LA, p.X =   3, p.Y =   3, p.Distance() = 5" + Environment.NewLine +
                    "       SFO, p.X =   6, p.Y =   6, p.Distance() = 10" + Environment.NewLine +
                    " beaverton, p.X =  10, p.Y =  10, p.Distance() = 14.8660687473185" + Environment.NewLine +
                    "  new york, p.X =  20, p.Y =  20, p.Distance() = 25" + Environment.NewLine +
                    "     yukon, p.X =  20, p.Y =  20, p.Distance() = 32.0156211871642" + Environment.NewLine;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    DataTestUtility.AssertEqualsWithDescription(
                        expectedReaderValues, UdtTestHelpers.DumpReaderString(reader),
                        "Unexpected reader values.");
                }
            }
        }

        [CheckConnStrSetupFact]
        public void Reader_LineEarly()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("select * from lines", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    Line l = null;
                    Point p = null;
                    int x = 0, y = 0;
                    double length = 0;

                    string expectedReaderValues =
                        "ids (int);pos (UdtTestDb.dbo.Line);";

                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        builder.Append(reader.GetName(i) + " (" + reader.GetDataTypeName(i) + ");");
                    }
                    DataTestUtility.AssertEqualsWithDescription(
                        expectedReaderValues, builder.ToString(),
                        "Unexpected reader values.");

                    string expectedLineValues =
                        "1, IsNull = False, Length = 2.82842712474619" + Environment.NewLine +
                        "2, IsNull = False, Length = 2.82842712474619" + Environment.NewLine +
                        "3, IsNull = False, Length = 9.8488578017961" + Environment.NewLine +
                        "4, IsNull = False, Length = 214.107449660212" + Environment.NewLine +
                        "5, IsNull = False, Length = 2.82842712474619" + Environment.NewLine +
                        "6, IsNull = False, Length = 2.82842712474619" + Environment.NewLine +
                        "7, IsNull = False, Length = 9.8488578017961" + Environment.NewLine +
                        "8, IsNull = False, Length = 214.107449660212" + Environment.NewLine;

                    builder = new StringBuilder();
                    while (reader.Read())
                    {
                        builder.Append(reader.GetValue(0).ToString() + ", ");
                        l = (Line)reader.GetValue(1);
                        if (!l.IsNull)
                        {
                            p = l.Start;
                            x = p.X;
                            y = p.Y;
                            length = l.Length();
                        }

                        builder.Append("IsNull = " + l.IsNull + ", ");
                        builder.Append("Length = " + length);
                        builder.AppendLine();
                    }
                    DataTestUtility.AssertEqualsWithDescription(
                        expectedLineValues, builder.ToString(),
                        "Unexpected Line values.");
                }
            }
        }

        [CheckConnStrSetupFact]
        public void Reader_PointLate()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("select name, location from cities", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    string expectedReaderValues =
                        "ColumnName[0] = name" + Environment.NewLine +
                        "DataType[0] = nvarchar" + Environment.NewLine +
                        "FieldType[0] = System.String" + Environment.NewLine +
                        "ColumnName[1] = location" + Environment.NewLine +
                        "DataType[1] = UdtTestDb.dbo.Point" + Environment.NewLine +
                        "FieldType[1] = Point" + Environment.NewLine +
                        "   redmond, p.X =   3, p.Y =   3, p.Distance() = 5" + Environment.NewLine +
                        "  bellevue, p.X =   6, p.Y =   6, p.Distance() = 10" + Environment.NewLine +
                        "   seattle, p.X =  10, p.Y =  10, p.Distance() = 14.8660687473185" + Environment.NewLine +
                        "  portland, p.X =  20, p.Y =  20, p.Distance() = 25" + Environment.NewLine +
                        "        LA, p.X =   3, p.Y =   3, p.Distance() = 5" + Environment.NewLine +
                        "       SFO, p.X =   6, p.Y =   6, p.Distance() = 10" + Environment.NewLine +
                        " beaverton, p.X =  10, p.Y =  10, p.Distance() = 14.8660687473185" + Environment.NewLine +
                        "  new york, p.X =  20, p.Y =  20, p.Distance() = 25" + Environment.NewLine +
                        "     yukon, p.X =  20, p.Y =  20, p.Distance() = 32.0156211871642" + Environment.NewLine;

                    DataTestUtility.AssertEqualsWithDescription(
                    expectedReaderValues, UdtTestHelpers.DumpReaderString(reader),
                    "Unexpected reader values.");
                }
            }
        }

        [CheckConnStrSetupFact]
        public void Reader_CircleLate()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("select * from circles", conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    string expectedReaderValues =
                        "ColumnName[0] = num" + Environment.NewLine +
                        "DataType[0] = int" + Environment.NewLine +
                        "FieldType[0] = System.Int32" + Environment.NewLine +
                        "ColumnName[1] = def" + Environment.NewLine +
                        "DataType[1] = UdtTestDb.dbo.Circle" + Environment.NewLine +
                        "FieldType[1] = Circle" + Environment.NewLine +
                        "         1, Center = 1,2" + Environment.NewLine +
                        "         2, Center = 3,4" + Environment.NewLine +
                        "         3, Center = 11,23" + Environment.NewLine +
                        "         4, Center = 444,555" + Environment.NewLine +
                        "         5, Center = 1,2" + Environment.NewLine +
                        "         6, Center = 3,4" + Environment.NewLine +
                        "         7, Center = 11,23" + Environment.NewLine +
                        "         8, Center = 444,245" + Environment.NewLine;

                    DataTestUtility.AssertEqualsWithDescription(
                        expectedReaderValues, UdtTestHelpers.DumpReaderString(reader),
                        "Unexpected reader values.");
                }
            }
        }

        [CheckConnStrSetupFact]
        public void TestSchemaTable()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand("select * from lines", conn))
            {
                conn.Open();
                cmd.CommandType = CommandType.Text;

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    DataTable t = reader.GetSchemaTable();

                    string expectedSchemaTableValues =
                        "ids, 0, 4, 10, 255, False, , , , ids, , , System.Int32, True, 8, , , False, False, False, , False, False, System.Data.SqlTypes.SqlInt32, int, , , , , 8, False, " + Environment.NewLine +
                        "pos, 1, 20, 255, 255, False, , , , pos, , , Line, True, 29, , , False, False, False, , False, False, Line, UdtTestDb.dbo.Line, , , , Line, Shapes, Version=1.2.0.0, Culture=neutral, PublicKeyToken=a3e3aa32e6a16344, 29, False, " + Environment.NewLine;

                    StringBuilder builder = new StringBuilder();
                    foreach (DataRow row in t.Rows)
                    {
                        foreach (DataColumn col in t.Columns)
                            builder.Append(row[col] + ", ");

                        builder.AppendLine();
                    }
                    DataTestUtility.AssertEqualsWithDescription(
                        expectedSchemaTableValues, builder.ToString(),
                        "Unexpected DataTable values from GetSchemaTable.");

                    string expectedReaderValues =
                        "ids1" + Environment.NewLine +
                        "pos1,2,3,4" + Environment.NewLine +
                        "ids2" + Environment.NewLine +
                        "pos3,4,5,6" + Environment.NewLine +
                        "ids3" + Environment.NewLine +
                        "pos11,23,15,32" + Environment.NewLine +
                        "ids4" + Environment.NewLine +
                        "pos444,555,245,634" + Environment.NewLine +
                        "ids5" + Environment.NewLine +
                        "pos1,2,3,4" + Environment.NewLine +
                        "ids6" + Environment.NewLine +
                        "pos3,4,5,6" + Environment.NewLine +
                        "ids7" + Environment.NewLine +
                        "pos11,23,15,32" + Environment.NewLine +
                        "ids8" + Environment.NewLine +
                        "pos444,555,245,634" + Environment.NewLine;
                    builder = new StringBuilder();
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            builder.Append(reader.GetName(i) + reader.GetValue(i).ToString());
                            builder.AppendLine();
                        }
                    }
                    DataTestUtility.AssertEqualsWithDescription(
                        expectedReaderValues, builder.ToString(),
                        "Unexpected Reader values.");
                }
            }
        }

        [CheckConnStrSetupFact]
        public void TestSqlUserDefinedAggregateAttributeMaxByteSize()
        {
            Func<int, SqlUserDefinedAggregateAttribute> create
                    = (size) => new SqlUserDefinedAggregateAttribute(Format.UserDefined) { MaxByteSize = size };

            SqlUserDefinedAggregateAttribute attribute1 = create(-1);
            SqlUserDefinedAggregateAttribute attribute2 = create(0);
            SqlUserDefinedAggregateAttribute attribute3 = create(SqlUserDefinedAggregateAttribute.MaxByteSizeValue);

            string udtError = SystemDataResourceManager.Instance.SQLUDT_MaxByteSizeValue;
            string errorMessage = (new ArgumentOutOfRangeException("MaxByteSize", 8001, udtError)).Message;

            DataTestUtility.AssertThrowsWrapper<ArgumentOutOfRangeException>(
                () => create(SqlUserDefinedAggregateAttribute.MaxByteSizeValue + 1),
                errorMessage);

            errorMessage = (new ArgumentOutOfRangeException("MaxByteSize", -2, udtError)).Message;
            DataTestUtility.AssertThrowsWrapper<ArgumentOutOfRangeException>(
                () => create(-2),
                errorMessage);
        }
    }
}

