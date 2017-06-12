// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using System.Globalization;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class AdapterTest
    {
        private char[] _appendNewLineIndentBuffer = new char[0];

        // data value and server consts
        private const string MagicName = "Magic";
        private string _tempTable;
        private string _tempKey;

        // data type
        private decimal _c_numeric_val;
        private long _c_bigint_val;
        private byte[] _c_unique_val;
        private Guid _c_guid_val;
        private byte[] _c_varbinary_val;
        private byte[] _c_binary_val;
        private decimal _c_money_val;
        private decimal _c_smallmoney_val;
        private DateTime _c_datetime_val;
        private DateTime _c_smalldatetime_val;
        private string _c_nvarchar_val;
        private string _c_nchar_val;
        private string _c_varchar_val;
        private string _c_char_val;
        private int _c_int_val;
        private short _c_smallint_val;
        private byte _c_tinyint_val;
        private bool _c_bit_val;
        private double _c_float_val;
        private float _c_real_val;

        private object[] _values;

        public AdapterTest()
        {
            // create random name for temp tables
            _tempTable = Environment.MachineName + "_" + Guid.NewGuid().ToString();
            _tempTable = _tempTable.Replace('-', '_');

            _tempKey = "employee_id_key_" + Environment.TickCount.ToString() + Guid.NewGuid().ToString();
            _tempKey = _tempKey.Replace('-', '_');

            InitDataValues();
        }

        [CheckConnStrSetupFact]
        public void SimpleFillTest()
        {
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country FROM Employees", conn))
            {
                DataSet employeesSet = new DataSet();
                DataTestUtility.AssertEqualsWithDescription(0, employeesSet.Tables.Count, "Unexpected tables count before fill.");
                adapter.Fill(employeesSet, "Employees");

                DataTestUtility.AssertEqualsWithDescription(1, employeesSet.Tables.Count, "Unexpected tables count after fill.");
                DataTestUtility.AssertEqualsWithDescription("Employees", employeesSet.Tables[0].TableName, "Unexpected table name.");

                DataTestUtility.AssertEqualsWithDescription(9, employeesSet.Tables["Employees"].Columns.Count, "Unexpected columns count.");
                employeesSet.Tables["Employees"].Columns.Remove("LastName");
                employeesSet.Tables["Employees"].Columns.Remove("FirstName");
                employeesSet.Tables["Employees"].Columns.Remove("Title");
                DataTestUtility.AssertEqualsWithDescription(6, employeesSet.Tables["Employees"].Columns.Count, "Unexpected columns count after column removal.");
            }
        }

        [CheckConnStrSetupFact]
        public void PrepUnprepTest()
        {
            // share the connection
            using (SqlCommand cmd = new SqlCommand("select * from shippers", new SqlConnection(DataTestUtility.TcpConnStr)))
            using (SqlDataAdapter sqlAdapter = new SqlDataAdapter())
            {
                cmd.Connection.Open();

                DataSet dataSet = new DataSet();
                sqlAdapter.TableMappings.Add("Table", "shippers");

                cmd.CommandText = "Select * from shippers";
                sqlAdapter.SelectCommand = cmd;
                sqlAdapter.Fill(dataSet);

                DataTestUtility.AssertEqualsWithDescription(
                    3, dataSet.Tables[0].Rows.Count,
                    "Exec1: Unexpected number of shipper rows.");

                dataSet.Reset();
                sqlAdapter.Fill(dataSet);

                DataTestUtility.AssertEqualsWithDescription(
                    3, dataSet.Tables[0].Rows.Count,
                    "Exec2: Unexpected number of shipper rows.");

                dataSet.Reset();
                cmd.CommandText = "select * from shippers where shipperId < 3";
                sqlAdapter.Fill(dataSet);

                DataTestUtility.AssertEqualsWithDescription(
                    2, dataSet.Tables[0].Rows.Count,
                    "Exec3: Unexpected number of shipper rows.");

                dataSet.Reset();

                sqlAdapter.Fill(dataSet);
                DataTestUtility.AssertEqualsWithDescription(
                    2, dataSet.Tables[0].Rows.Count,
                    "Exec4: Unexpected number of shipper rows.");

                cmd.CommandText = "select * from shippers";
                cmd.Prepare();

                int i = 0;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    DataTestUtility.AssertEqualsWithDescription(3, reader.FieldCount, "Unexpected FieldCount.");

                    while (reader.Read())
                    {
                        i++;
                    }
                }
                DataTestUtility.AssertEqualsWithDescription(3, i, "Unexpected read count.");

                cmd.CommandText = "select * from orders where orderid < @p1";
                cmd.Parameters.AddWithValue("@p1", 10250);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    DataTestUtility.AssertEqualsWithDescription(14, reader.FieldCount, "Unexpected FieldCount.");

                    i = 0;
                    while (reader.Read())
                    {
                        i++;
                    }
                }
                DataTestUtility.AssertEqualsWithDescription(2, i, "Unexpected read count.");

                cmd.Parameters["@p1"].Value = 10249;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    DataTestUtility.AssertEqualsWithDescription(14, reader.FieldCount, "Unexpected FieldCount.");

                    i = 0;
                    while (reader.Read())
                    {
                        i++;
                    }
                }
                DataTestUtility.AssertEqualsWithDescription(1, i, "Unexpected read count.");
            }
        }

        [CheckConnStrSetupFact]
        public void SqlVariantTest()
        {
            try
            {
                ExecuteNonQueryCommand("CREATE TABLE shiloh_types (c0_bigint bigint, c1_variant sql_variant)");

                // good test for null values and unicode strings
                using (SqlCommand cmd = new SqlCommand(null, new SqlConnection(DataTestUtility.TcpConnStr)))
                using (SqlDataAdapter sqlAdapter = new SqlDataAdapter())
                {
                    cmd.Connection.Open();

                    // the ORDER BY clause tests that we correctly ignore the ORDER token
                    cmd.CommandText = "select * from shiloh_types";
                    sqlAdapter.SelectCommand = cmd;
                    sqlAdapter.TableMappings.Add("Shiloh_Types", "rowset");

                    // insert
                    sqlAdapter.InsertCommand = new SqlCommand()
                    {
                        CommandText = "INSERT INTO Shiloh_Types(c0_bigint, c1_variant) " +
                        "VALUES (@bigint, @variant)"
                    };
                    SqlParameter p = sqlAdapter.InsertCommand.Parameters.Add(new SqlParameter("@bigint", SqlDbType.BigInt));
                    p.SourceColumn = "c0_bigint";
                    p = sqlAdapter.InsertCommand.Parameters.Add(new SqlParameter("@variant", SqlDbType.Variant));
                    p.SourceColumn = "c1_variant";
                    sqlAdapter.InsertCommand.Connection = cmd.Connection;

                    DataSet dataSet = new DataSet();
                    sqlAdapter.FillSchema(dataSet, SchemaType.Mapped, "Shiloh_Types");

                    DataRow datarow = null;
                    for (int i = 0; i < _values.Length; i++)
                    {
                        // add each variant type
                        datarow = dataSet.Tables[0].NewRow();
                        datarow.ItemArray = new object[] { 1, _values[i] };
                        datarow.Table.Rows.Add(datarow);
                    }

                    sqlAdapter.Update(dataSet, "Shiloh_Types");

                    // now reload and make sure we got the values we wrote out
                    dataSet.Reset();
                    sqlAdapter.Fill(dataSet, "Shiloh_Types");

                    DataColumnCollection cols = dataSet.Tables[0].Columns;
                    DataRowCollection rows = dataSet.Tables[0].Rows;

                    Assert.True(rows.Count == _values.Length, "FAILED:  SqlVariant didn't update all the rows!");

                    for (int i = 0; i < rows.Count; i++)
                    {
                        DataRow row = rows[i];
                        object value = row[1];

                        if (_values[i].GetType() == typeof(byte[]) || _values[i].GetType() == typeof(Guid))
                        {
                            byte[] bsrc;
                            byte[] bdst;

                            if (_values[i].GetType() == typeof(Guid))
                            {
                                bsrc = ((Guid)value).ToByteArray();
                                bdst = ((Guid)(_values[i])).ToByteArray();
                            }
                            else
                            {
                                bsrc = (byte[])value;
                                bdst = (byte[])_values[i];
                            }

                            Assert.True(ByteArraysEqual(bsrc, bdst), "FAILED: Byte arrays are unequal");
                        }
                        else if (_values[i].GetType() == typeof(bool))
                        {
                            Assert.True(Convert.ToBoolean(value) == (bool)_values[i], "FAILED:  " + DBConvertToString(value) + " is not equal to " + DBConvertToString(_values[i]));
                        }
                        else
                        {
                            Assert.True(value.Equals(_values[i]), "FAILED:  " + DBConvertToString(value) + " is not equal to " + DBConvertToString(_values[i]));
                        }
                    }
                }
            }
            finally
            {
                ExecuteNonQueryCommand("DROP TABLE shiloh_types");
            }
        }

        [CheckConnStrSetupFact]
        public void ParameterTest_AllTypes()
        {
            string spCreateAllTypes =
                "CREATE PROCEDURE sp_alltypes " +
                "@Cnumeric numeric(10,2) OUTPUT, " +
                "@Cunique uniqueidentifier OUTPUT, " +
                "@Cnvarchar nvarchar(10) OUTPUT, " +
                "@Cnchar nchar(10) OUTPUT, " +
                "@Cbit bit OUTPUT, " +
                "@Ctinyint tinyint OUTPUT, " +
                "@Cvarbinary varbinary(16) OUTPUT, " +
                "@Cbinary binary(16) OUTPUT, " +
                "@Cchar char(10) OUTPUT, " +
                "@Cmoney money OUTPUT, " +
                "@Csmallmoney smallmoney OUTPUT, " +
                "@Cint int OUTPUT, " +
                "@Csmallint smallint OUTPUT, " +
                "@Cfloat float OUTPUT, " +
                "@Creal real OUTPUT, " +
                "@Cdatetime datetime OUTPUT, " +
                "@Csmalldatetime smalldatetime OUTPUT, " +
                "@Cvarchar varchar(10) OUTPUT " +
                "AS SELECT " +
                "@Cnumeric=@Cnumeric, " +
                "@Cunique=@Cunique, " +
                "@Cnvarchar=@Cnvarchar, " +
                "@Cnchar=@Cnchar, " +
                "@Cbit=@Cbit, " +
                "@Ctinyint=@Ctinyint, " +
                "@Cvarbinary=@Cvarbinary, " +
                "@Cbinary=@Cbinary, " +
                "@Cchar=@Cchar, " +
                "@Cmoney=@Cmoney, " +
                "@Csmallmoney=@Csmallmoney, " +
                "@Cint=@Cint, " +
                "@Csmallint=@Csmallint, " +
                "@Cfloat=@Cfloat, " +
                "@Creal=@Creal, " +
                "@Cdatetime=@Cdatetime, " +
                "@Csmalldatetime=@Csmalldatetime, " +
                "@Cvarchar=@Cvarchar " +
                "RETURN(42)";

            string spDropAllTypes = "DROP PROCEDURE sp_alltypes";
            bool dropSP = false;

            try
            {
                ExecuteNonQueryCommand(spCreateAllTypes);
                dropSP = true;

                using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmd = new SqlCommand("sp_allTypes", conn))
                using (SqlDataAdapter sqlAdapter = new SqlDataAdapter())
                {
                    conn.Open();

                    SqlParameter param = cmd.Parameters.Add(new SqlParameter("@Cnumeric", SqlDbType.Decimal));
                    param.Precision = 10;
                    param.Scale = 2;
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[0].Value = _c_numeric_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Cunique", SqlDbType.UniqueIdentifier));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[1].Value = _c_guid_val;

                    cmd.Parameters.Add(new SqlParameter("@Cnvarchar", SqlDbType.NVarChar, 10));
                    cmd.Parameters[2].Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[2].Value = _c_nvarchar_val;

                    cmd.Parameters.Add(new SqlParameter("@Cnchar", SqlDbType.NChar, 10));
                    cmd.Parameters[3].Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[3].Value = _c_nchar_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Cbit", SqlDbType.Bit));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[4].Value = _c_bit_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Ctinyint", SqlDbType.TinyInt));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[5].Value = _c_tinyint_val;

                    cmd.Parameters.Add(new SqlParameter("@Cvarbinary", SqlDbType.VarBinary, 16));
                    cmd.Parameters[6].Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[6].Value = _c_varbinary_val;

                    cmd.Parameters.Add(new SqlParameter("@Cbinary", SqlDbType.Binary, 16));
                    cmd.Parameters[7].Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[7].Value = _c_binary_val;

                    cmd.Parameters.Add(new SqlParameter("@Cchar", SqlDbType.Char, 10));
                    cmd.Parameters[8].Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[8].Value = _c_char_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Cmoney", SqlDbType.Money));
                    param.Direction = ParameterDirection.InputOutput;
                    param.Scale = 4;
                    cmd.Parameters[9].Value = _c_money_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Csmallmoney", SqlDbType.SmallMoney));
                    param.Direction = ParameterDirection.InputOutput;
                    param.Scale = 4;
                    cmd.Parameters[10].Value = _c_smallmoney_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Cint", SqlDbType.Int));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[11].Value = _c_int_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Csmallint", SqlDbType.SmallInt));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[12].Value = _c_smallint_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Cfloat", SqlDbType.Float));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[13].Value = _c_float_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Creal", SqlDbType.Real));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[14].Value = _c_real_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Cdatetime", SqlDbType.DateTime));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[15].Value = _c_datetime_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Csmalldatetime", SqlDbType.SmallDateTime));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[16].Value = _c_smalldatetime_val;

                    param = cmd.Parameters.Add(new SqlParameter("@Cvarchar", SqlDbType.VarChar, 10));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[17].Value = _c_varchar_val;

                    param = cmd.Parameters.Add(new SqlParameter("@return", SqlDbType.Int));
                    param.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters[18].Value = 17; // will be overwritten

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.ExecuteNonQuery();

                    string[] expectedStringValues =
                    {
                        "@Cnumeric : Decimal<42424242.42>",
                        null,
                        "@Cnvarchar : String:10<1234567890>",
                        "@Cnchar : String:10<1234567890>",
                        "@Cbit : Boolean<True>",
                        "@Ctinyint : Byte<255>",
                        null,
                        null,
                        "@Cchar : String:10<1234567890>",
                        null,
                        null,
                        "@Cint : Int32<-1>",
                        "@Csmallint : Int16<-1>",
                        "@Cfloat : Double<12345678.2>",
                        "@Creal : Single<12345.1>",
                        null,
                        null,
                        "@Cvarchar : String:10<1234567890>",
                        "@return : Int32<42>"
                    };

                    for (int i = 0; i < cmd.Parameters.Count; i++)
                    {
                        param = cmd.Parameters[i];
                        switch (param.SqlDbType)
                        {
                            case SqlDbType.Binary:
                                Assert.True(ByteArraysEqual(_c_binary_val, (byte[])param.Value), "FAILED: sp_alltypes, Binary parameter");
                                break;
                            case SqlDbType.VarBinary:
                                Assert.True(ByteArraysEqual(_c_varbinary_val, (byte[])param.Value), "FAILED: sp_alltypes, VarBinary parameter");
                                break;
                            case SqlDbType.UniqueIdentifier:
                                DataTestUtility.AssertEqualsWithDescription(_c_guid_val, (Guid)param.Value, "FAILED: sp_alltypes, UniqueIdentifier parameter");
                                break;
                            case SqlDbType.DateTime:
                                Assert.True(0 == DateTime.Compare((DateTime)param.Value, _c_datetime_val), "FAILED: sp_alltypes, DateTime parameter");
                                break;
                            case SqlDbType.SmallDateTime:
                                Assert.True(0 == DateTime.Compare((DateTime)param.Value, _c_smalldatetime_val), "FAILED: sp_alltypes, SmallDateTime parameter");
                                break;
                            case SqlDbType.Money:
                                Assert.True(
                                    0 == decimal.Compare((decimal)param.Value, _c_money_val),
                                    string.Format("FAILED: sp_alltypes, Money parameter. Expected: {0}. Actual: {1}.", _c_money_val, (decimal)param.Value));
                                break;
                            case SqlDbType.SmallMoney:
                                Assert.True(
                                    0 == decimal.Compare((decimal)param.Value, _c_smallmoney_val),
                                    string.Format("FAILED: sp_alltypes, SmallMoney parameter. Expected: {0}. Actual: {1}.", _c_smallmoney_val, (decimal)param.Value));
                                break;
                            default:
                                string actualValue = param.ParameterName + " : " + DBConvertToString(cmd.Parameters[i].Value);
                                DataTestUtility.AssertEqualsWithDescription(actualValue, expectedStringValues[i], "Unexpected parameter value.");
                                break;
                        }
                    }
                }
            }
            finally
            {
                if (dropSP)
                {
                    ExecuteNonQueryCommand(spDropAllTypes);
                }
            }
        }

        [CheckConnStrSetupFact]
        public void ParameterTest_InOut()
        {
            // input, output
            string spCreateInOut =
                "CREATE PROCEDURE sp_test @in int, @inout int OUTPUT, @out nvarchar(8) OUTPUT " +
                "AS SELECT @inout = (@in + @inout), @out = 'Success!' " +
                "SELECT * From shippers where ShipperID = @in " +
                "RETURN(42)";

            string spDropInOut = "DROP PROCEDURE sp_test";
            bool dropSP = false;

            try
            {
                ExecuteNonQueryCommand(spCreateInOut);
                dropSP = true;

                using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmd = new SqlCommand("sp_test", conn))
                using (SqlDataAdapter sqlAdapter = new SqlDataAdapter())
                {
                    conn.Open();

                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@in", SqlDbType.Int));
                    cmd.Parameters[0].Value = 2;

                    SqlParameter param = cmd.Parameters.Add(new SqlParameter("@inout", SqlDbType.Int));
                    param.Direction = ParameterDirection.InputOutput;
                    cmd.Parameters[1].Value = 1998;

                    param = cmd.Parameters.Add(new SqlParameter("@out", SqlDbType.NVarChar, 8));
                    param.Direction = ParameterDirection.Output;

                    param = cmd.Parameters.Add(new SqlParameter("@ret", SqlDbType.Int));
                    param.Direction = ParameterDirection.ReturnValue;

                    DataSet dataSet = new DataSet();
                    sqlAdapter.TableMappings.Add("Table", "shipper");
                    sqlAdapter.SelectCommand = cmd;
                    sqlAdapter.Fill(dataSet);

                    // check our ouput and return value params
                    Assert.True(VerifyOutputParams(cmd.Parameters), "FAILED: InputOutput parameter test with returned rows and bound return value!");

                    Assert.True(1 == dataSet.Tables[0].Rows.Count, "FAILED:  Expected 1 row to be loaded in the dataSet!");

                    DataRow row = dataSet.Tables[0].Rows[0];
                    Assert.True((int)row["ShipperId"] == 2, "FAILED:  ShipperId column should be 2, not " + DBConvertToString(row["ShipperId"]));

                    // remember to reset params
                    cmd.Parameters[0].Value = 2;
                    cmd.Parameters[1].Value = 1998;
                    cmd.Parameters[2].Value = Convert.DBNull;
                    cmd.Parameters[3].Value = Convert.DBNull;

                    // now exec the same thing without a data set
                    cmd.ExecuteNonQuery();

                    // check our ouput and return value params
                    Assert.True(VerifyOutputParams(cmd.Parameters), "FAILED: InputOutput parameter test with no returned rows and bound return value!");

                    // now unbind the return value
                    cmd.Parameters.RemoveAt(3);

                    // remember to reset input params
                    cmd.Parameters[0].Value = 1; // use 1, just for the heck of it
                    cmd.Parameters[1].Value = 1999;
                    cmd.Parameters[2].Value = Convert.DBNull;

                    dataSet.Reset();

                    sqlAdapter.Fill(dataSet);

                    // verify the ouptut parameter
                    Assert.True(
                        ((int)cmd.Parameters[1].Value == 2000) &&
                        (0 == string.Compare(cmd.Parameters[2].Value.ToString(), "Success!", false, CultureInfo.InvariantCulture)),
                        "FAILED:  unbound return value case, output param is not correct!");

                    Assert.True(1 == dataSet.Tables[0].Rows.Count, "FAILED:  Expected 1 row to be loaded in the dataSet!");

                    row = dataSet.Tables[0].Rows[0];
                    Assert.True((int)row["ShipperId"] == 1, "FAILED:  ShipperId column should be 1, not " + DBConvertToString(row["ShipperId"]));
                }
            }
            finally
            {
                if (dropSP)
                {
                    ExecuteNonQueryCommand(spDropInOut);
                }
            }
        }

        [CheckConnStrSetupFact]
        public void UpdateTest()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmd = conn.CreateCommand())
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                using (SqlDataAdapter adapterVerify = new SqlDataAdapter())
                {
                    conn.Open();

                    cmd.CommandText = string.Format("SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country into {0} from Employees where EmployeeID < 3", _tempTable);
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "alter table " + _tempTable + " add constraint " + _tempKey + " primary key (EmployeeID)";
                    cmd.ExecuteNonQuery();

                    PrepareUpdateCommands(adapter, conn, _tempTable);

                    adapter.SelectCommand = new SqlCommand(string.Format("SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country from {0} where EmployeeID < 3", _tempTable), conn);
                    adapterVerify.SelectCommand = new SqlCommand("SELECT LastName, FirstName FROM " + _tempTable + " where FirstName='" + MagicName + "'", conn);

                    adapter.TableMappings.Add(_tempTable, "rowset");
                    adapterVerify.TableMappings.Add(_tempTable, "rowset");

                    DataSet dataSet = new DataSet();
                    VerifyFillSchemaResult(adapter.FillSchema(dataSet, SchemaType.Mapped, _tempTable), new string[] { "rowset" });

                    // FillSchema
                    dataSet.Tables["rowset"].PrimaryKey = new DataColumn[] { dataSet.Tables["rowset"].Columns["EmployeeID"] };
                    adapter.Fill(dataSet, _tempTable);

                    // Fill from Database
                    Assert.True(dataSet.Tables[0].Rows.Count == 2, "FAILED:  Fill after FillSchema should populate the dataSet with two rows!");

                    dataSet.AcceptChanges();

                    // Verify that set is empty
                    DataSet dataSetVerify = new DataSet();
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    // Insert
                    DataRow datarow = dataSet.Tables["rowset"].NewRow();
                    datarow.ItemArray = new object[] { "11", "The Original", MagicName, "Engineer", "One Microsoft Way", "Redmond", "WA", "98052", "USA" };
                    datarow.Table.Rows.Add(datarow);

                    adapter.Update(dataSet, _tempTable);

                    // Verify that set has one 'Magic' entry
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    dataSet.AcceptChanges();

                    // Update
                    datarow = dataSet.Tables["rowset"].Rows.Find("11");
                    datarow.BeginEdit();
                    datarow.ItemArray = new object[] { "11", "The New and Improved", MagicName, "reenignE", "Yaw Tfosorcim Eno", "Dnomder", "WA", "52098", "ASU" };
                    datarow.EndEdit();

                    adapter.Update(dataSet, _tempTable);

                    // Verify that set has updated 'Magic' entry
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    dataSet.AcceptChanges();

                    // Delete
                    dataSet.Tables["rowset"].Rows.Find("11").Delete();
                    adapter.Update(dataSet, _tempTable);

                    // Verify that set is empty
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    dataSet.AcceptChanges();
                }
            }
            finally
            {
                ExecuteNonQueryCommand("DROP TABLE " + _tempTable);
            }
        }

        // these next texts verify that 'bulk' operations work.  If each command type modifies more than three rows, then we do a Prep/Exec instead of
        // adhoc ExecuteSql.
        [CheckConnStrSetupFact]
        public void BulkUpdateTest()
        {
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlCommand cmd = conn.CreateCommand())
            using (SqlDataAdapter adapter = new SqlDataAdapter())
            using (SqlDataAdapter adapterVerify = new SqlDataAdapter())
            {
                try
                {
                    conn.Open();

                    cmd.CommandText = "SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country into " + _tempTable + " from Employees where EmployeeID < 3";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "alter table " + _tempTable + " add constraint " + _tempKey + " primary key (EmployeeID)";
                    cmd.ExecuteNonQuery();

                    PrepareUpdateCommands(adapter, conn, _tempTable);

                    adapter.SelectCommand = new SqlCommand("SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country FROM " + _tempTable + " WHERE EmployeeID < 3", conn);
                    adapterVerify.SelectCommand = new SqlCommand("SELECT LastName, FirstName FROM " + _tempTable + " where FirstName='" + MagicName + "'", conn);

                    adapter.TableMappings.Add(_tempTable, "rowset");
                    adapterVerify.TableMappings.Add(_tempTable, "rowset");

                    DataSet dataSet = new DataSet();
                    adapter.FillSchema(dataSet, SchemaType.Mapped, _tempTable);

                    dataSet.Tables["rowset"].PrimaryKey = new DataColumn[] { dataSet.Tables["rowset"].Columns["EmployeeID"] };
                    adapter.Fill(dataSet, _tempTable);
                    dataSet.AcceptChanges();

                    // Verify that set is empty
                    DataSet dataSetVerify = new DataSet();
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    // Bulk Insert  (10 records)
                    DataRow datarow = null;
                    const int cOps = 5;
                    for (int i = 0; i < cOps * 2; i++)
                    {
                        datarow = dataSet.Tables["rowset"].NewRow();
                        string sid = "99999000" + i.ToString();
                        datarow.ItemArray = new object[] { sid, "Bulk Insert" + i.ToString(), MagicName, "Engineer", "One Microsoft Way", "Redmond", "WA", "98052", "USA" };
                        datarow.Table.Rows.Add(datarow);
                    }
                    adapter.Update(dataSet, _tempTable);

                    // Verify that set has 10 'Magic' entries
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 10, _tempTable);
                    dataSet.AcceptChanges();

                    // Bulk Update (first 5)
                    for (int i = 0; i < cOps; i++)
                    {
                        string sid = "99999000" + i.ToString();
                        datarow = dataSet.Tables["rowset"].Rows.Find(sid);
                        datarow.BeginEdit();
                        datarow.ItemArray = new object[] { sid, "Bulk Update" + i.ToString(), MagicName, "reenignE", "Yaw Tfosorcim Eno", "Dnomder", "WA", "52098", "ASU" };
                        datarow.EndEdit();
                    }

                    // Bulk Delete (last 5)
                    for (int i = cOps; i < cOps * 2; i++)
                    {
                        string sid = "99999000" + i.ToString();
                        dataSet.Tables["rowset"].Rows.Find(sid).Delete();
                    }

                    // now update the dataSet with the insert and delete changes
                    adapter.Update(dataSet, _tempTable);

                    // Verify that set has 5 'Magic' updated entries
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 5, _tempTable);
                    dataSet.AcceptChanges();

                    // clean up the remaining 5 rows
                    for (int i = 0; i < cOps; i++)
                    {
                        string sid = "99999000" + i.ToString();
                        dataSet.Tables["rowset"].Rows.Find(sid).Delete();
                    }
                    adapter.Update(dataSet, _tempTable);

                    // Verify that set has no entries
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);
                    dataSet.AcceptChanges();
                }
                finally
                {
                    ExecuteNonQueryCommand("DROP TABLE " + _tempTable);
                }
            }
        }

        // Makes sure that we can refresh an identity column in the dataSet
        // for a newly inserted row
        [CheckConnStrSetupFact]
        public void UpdateRefreshTest()
        {
            string createIdentTable =
                "CREATE TABLE ident(id int IDENTITY," +
                "LastName nvarchar(50) NULL," +
                "Firstname nvarchar(50) NULL)";

            string spCreateInsert =
                "CREATE PROCEDURE sp_insert" + _tempTable +
                "(@FirstName nvarchar(50), @LastName nvarchar(50), @id int OUTPUT) " +
                "AS INSERT INTO " + _tempTable + " (FirstName, LastName) " +
                "VALUES (@FirstName, @LastName); " +
                "SELECT @id=@@IDENTITY";

            string spDropInsert = "DROP PROCEDURE sp_insert" + _tempTable;
            bool dropSP = false;
            try
            {
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmd = new SqlCommand(null, conn))
                using (SqlCommand temp = new SqlCommand("SELECT id, LastName, FirstName into " + _tempTable + " from ident", conn))
                using (SqlCommand tableClean = new SqlCommand("", conn))
                {
                    ExecuteNonQueryCommand(createIdentTable);

                    adapter.InsertCommand = new SqlCommand()
                    {
                        CommandText = "sp_insert" + _tempTable,
                        CommandType = CommandType.StoredProcedure
                    };
                    adapter.InsertCommand.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 50, "FirstName"));
                    adapter.InsertCommand.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 50, "LastName"));
                    SqlParameter param = adapter.InsertCommand.Parameters.Add(new SqlParameter("@id", SqlDbType.Int));
                    param.SourceColumn = "id";
                    param.Direction = ParameterDirection.Output;

                    adapter.InsertCommand.Parameters.Add(new SqlParameter("@badapple", SqlDbType.NVarChar, 50, "LastName"));
                    adapter.RowUpdating += new SqlRowUpdatingEventHandler(RowUpdating_UpdateRefreshTest);
                    adapter.RowUpdated += new SqlRowUpdatedEventHandler(RowUpdated_UpdateRefreshTest);

                    adapter.InsertCommand.Connection = conn;
                    conn.Open();

                    temp.ExecuteNonQuery();

                    // start clean
                    tableClean.CommandText = "delete " + _tempTable;
                    tableClean.ExecuteNonQuery();
                    tableClean.CommandText = spCreateInsert;
                    tableClean.ExecuteNonQuery();

                    dropSP = true;

                    DataSet ds = new DataSet();
                    adapter.TableMappings.Add("Table", _tempTable);
                    cmd.CommandText = "select * from " + _tempTable;
                    adapter.SelectCommand = cmd;
                    adapter.Fill(ds, "Table");

                    // Insert
                    DataRow row1 = ds.Tables[_tempTable].NewRow();
                    row1.ItemArray = new object[] { 0, "Bond", "James" };
                    row1.Table.Rows.Add(row1);

                    DataRow row2 = ds.Tables[_tempTable].NewRow();
                    row2.ItemArray = new object[] { 0, "Lee", "Bruce" };
                    row2.Table.Rows.Add(row2);

                    Assert.True((int)row1["id"] == 0 && (int)row2["id"] == 0, "FAILED:  UpdateRefresh should not have values for identity columns");

                    adapter.Update(ds, "Table");

                    // should have values now
                    int i1 = (int)row1["id"];
                    int i2 = (int)row2["id"];

                    Assert.True(
                        (i1 != 0) && (i2 != 0) && (i2 == (i1 + 1)),
                        string.Format("FAILED:  UpdateRefresh, i2 should equal (i1 + 1). i1: {0}. i2: {1}.", i1, i2));
                }
            }
            finally
            {
                if (dropSP)
                {
                    ExecuteNonQueryCommand(spDropInsert);
                    ExecuteNonQueryCommand("DROP TABLE " + _tempTable);
                }
                ExecuteNonQueryCommand("DROP TABLE ident");
            }
        }

        [CheckConnStrSetupFact]
        public void UpdateNullTest()
        {
            string createTable = "CREATE TABLE varbin(cvarbin VARBINARY(7000), cimage IMAGE)";

            string createSP =
                "CREATE PROCEDURE sp_insertvarbin (@val_cvarbin VARBINARY(7000), @val_cimage IMAGE)" +
                "AS INSERT INTO varbin (cvarbin, cimage)" +
                "VALUES (@val_cvarbin, @val_cimage)";
            bool dropSP = false;

            try
            {
                ExecuteNonQueryCommand(createTable);
                ExecuteNonQueryCommand(createSP);
                dropSP = true;

                using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmdInsert = new SqlCommand("sp_insertvarbin", conn))
                using (SqlCommand cmdSelect = new SqlCommand("select * from varbin", conn))
                using (SqlCommand tableClean = new SqlCommand("delete varbin", conn))
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    conn.Open();

                    cmdInsert.CommandType = CommandType.StoredProcedure;
                    SqlParameter p1 = cmdInsert.Parameters.Add(new SqlParameter("@val_cvarbin", SqlDbType.VarBinary, 7000));
                    SqlParameter p2 = cmdInsert.Parameters.Add(new SqlParameter("@val_cimage", SqlDbType.Image, 8000));

                    tableClean.ExecuteNonQuery();
                    p1.Value = Convert.DBNull;
                    p2.Value = Convert.DBNull;
                    int rowsAffected = cmdInsert.ExecuteNonQuery();
                    DataTestUtility.AssertEqualsWithDescription(1, rowsAffected, "Unexpected number of rows inserted.");

                    DataSet ds = new DataSet();
                    adapter.SelectCommand = cmdSelect;
                    adapter.Fill(ds, "goofy");
                    // should have 1 row in table (with two null entries)
                    DataTestUtility.AssertEqualsWithDescription(1, ds.Tables[0].Rows.Count, "Unexpected rows count.");
                    DataTestUtility.AssertEqualsWithDescription(DBNull.Value, ds.Tables[0].Rows[0][0], "Unexpected value.");
                    DataTestUtility.AssertEqualsWithDescription(DBNull.Value, ds.Tables[0].Rows[0][1], "Unexpected value.");
                }
            }
            finally
            {
                if (dropSP)
                {
                    ExecuteNonQueryCommand("DROP PROCEDURE sp_insertvarbin");
                }
                ExecuteNonQueryCommand("DROP TABLE varbin");
            }
        }

        [CheckConnStrSetupFact]
        public void UpdateOffsetTest()
        {
            string createTable = "CREATE TABLE varbin(cvarbin VARBINARY(7000), cimage IMAGE)";

            string createSP =
                "CREATE PROCEDURE sp_insertvarbin (@val_cvarbin VARBINARY(7000), @val_cimage IMAGE)" +
                "AS INSERT INTO varbin (cvarbin, cimage)" +
                "VALUES (@val_cvarbin, @val_cimage)";
            bool dropSP = false;

            try
            {
                ExecuteNonQueryCommand(createTable);
                ExecuteNonQueryCommand(createSP);
                dropSP = true;

                using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmdInsert = new SqlCommand("sp_insertvarbin", conn))
                using (SqlCommand cmdSelect = new SqlCommand("select * from varbin", conn))
                using (SqlCommand tableClean = new SqlCommand("delete varbin", conn))
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    conn.Open();

                    cmdInsert.CommandType = CommandType.StoredProcedure;
                    SqlParameter p1 = cmdInsert.Parameters.Add(new SqlParameter("@val_cvarbin", SqlDbType.VarBinary, 7000));
                    SqlParameter p2 = cmdInsert.Parameters.Add(new SqlParameter("@val_cimage", SqlDbType.Image, 7000));

                    tableClean.ExecuteNonQuery();

                    byte[] b = new byte[7];
                    b[0] = 0x01;
                    b[1] = 0x02;
                    b[2] = 0x03;
                    b[3] = 0x04;
                    b[4] = 0x05;
                    b[5] = 0x06;
                    b[6] = 0x07;
                    p1.Value = b;
                    p1.Size = 4;

                    p2.Value = b;
                    p2.Size = 3;
                    p2.Offset = 4;
                    int rowsAffected = cmdInsert.ExecuteNonQuery();

                    DataSet ds = new DataSet();
                    adapter.SelectCommand = cmdSelect;
                    adapter.Fill(ds, "goofy");

                    byte[] expectedBytes1 = { 0x01, 0x02, 0x03, 0x04 };
                    byte[] val = (byte[])(ds.Tables[0].Rows[0][0]);
                    Assert.True(ByteArraysEqual(expectedBytes1, val), "FAILED: Test 1: Unequal byte arrays.");

                    byte[] expectedBytes2 = { 0x05, 0x06, 0x07 };
                    val = (byte[])(ds.Tables[0].Rows[0][1]);
                    Assert.True(ByteArraysEqual(expectedBytes2, val), "FAILED: Test 2: Unequal byte arrays.");
                }
            }
            finally
            {
                if (dropSP)
                {
                    ExecuteNonQueryCommand("DROP PROCEDURE sp_insertvarbin");
                }
                ExecuteNonQueryCommand("DROP TABLE varbin");
            }
        }

        [CheckConnStrSetupFact]
        public void SelectAllTest()
        {
            // Test exceptions
            using (SqlDataAdapter sqlAdapter = new SqlDataAdapter(new SqlCommand("select * from orders", new SqlConnection(DataTestUtility.TcpConnStr))))
            {
                DataSet dataset = new DataSet();
                sqlAdapter.TableMappings.Add("Table", "orders");
                sqlAdapter.Fill(dataset);
            }
        }

        // AutoGen test
        [CheckConnStrSetupFact]
        public void AutoGenUpdateTest()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmd = conn.CreateCommand())
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                using (SqlDataAdapter adapterVerify = new SqlDataAdapter())
                {
                    conn.Open();

                    cmd.CommandText = string.Format("SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country into {0} from Employees where EmployeeID < 3", _tempTable);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "alter table " + _tempTable + " add constraint " + _tempKey + " primary key (EmployeeID)";
                    cmd.ExecuteNonQuery();

                    adapter.SelectCommand = new SqlCommand(string.Format("SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country from {0} where EmployeeID < 3", _tempTable), conn);
                    adapterVerify.SelectCommand = new SqlCommand("SELECT LastName, FirstName FROM " + _tempTable + " where FirstName='" + MagicName + "'", conn);

                    adapter.TableMappings.Add(_tempTable, "rowset");
                    adapterVerify.TableMappings.Add(_tempTable, "rowset");

                    // FillSchema
                    DataSet dataSet = new DataSet();
                    VerifyFillSchemaResult(adapter.FillSchema(dataSet, SchemaType.Mapped, _tempTable), new string[] { "rowset" });

                    adapter.Fill(dataSet, _tempTable);

                    // Fill from Database
                    Assert.True(dataSet.Tables[0].Rows.Count == 2, "FAILED:  Fill after FillSchema should populate the dataSet with two rows!");

                    // Verify that set is empty
                    DataSet dataSetVerify = new DataSet();
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

                    // Insert
                    DataRow datarow = dataSet.Tables["rowset"].NewRow();
                    datarow.ItemArray = new object[] { "11", "The Original", MagicName, "Engineer", "One Microsoft Way", "Redmond", "WA", "98052", "USA" };
                    datarow.Table.Rows.Add(datarow);
                    adapter.Update(dataSet, _tempTable);

                    // Verify that set has one 'Magic' entry
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    // Update
                    datarow = dataSet.Tables["rowset"].Rows.Find("11");
                    datarow.BeginEdit();
                    datarow.ItemArray = new object[] { "11", "The New and Improved", MagicName, "reenignE", "Yaw Tfosorcim Eno", "Dnomder", "WA", "52098", "ASU" };
                    datarow.EndEdit();
                    adapter.Update(dataSet, _tempTable);

                    // Verify that set has updated 'Magic' entry
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    // Delete
                    dataSet.Tables["rowset"].Rows.Find("11").Delete();
                    adapter.Update(dataSet, _tempTable);

                    // Verify that set is empty
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);
                }
            }
            finally
            {
                ExecuteNonQueryCommand("DROP TABLE " + _tempTable);
            }
        }

        [CheckConnStrSetupFact]
        public void AutoGenErrorTest()
        {
            string createIdentTable =
                "CREATE TABLE ident(id int IDENTITY," +
                "LastName nvarchar(50) NULL," +
                "Firstname nvarchar(50) NULL)";

            try
            {
                using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmd = new SqlCommand("SELECT * into " + _tempTable + " from ident", conn))
                using (SqlDataAdapter adapter = new SqlDataAdapter())
                {
                    ExecuteNonQueryCommand(createIdentTable);

                    conn.Open();
                    adapter.SelectCommand = new SqlCommand("select * from " + _tempTable, conn);

                    cmd.ExecuteNonQuery();

                    // start clean
                    DataSet ds = new DataSet();
                    adapter.Fill(ds, _tempTable);

                    // Insert
                    DataRow row1 = ds.Tables[_tempTable].NewRow();
                    row1.ItemArray = new object[] { 0, "Bond", "James" };
                    row1.Table.Rows.Add(row1);

                    // table has no key so we should get an error here when we try to autogen the delete command (note that all three
                    // update command types are generated here despite the fact that we are just doing an insert)
                    SqlCommandBuilder builder = new SqlCommandBuilder(adapter);
                    adapter.Update(ds, _tempTable);
                }
            }
            finally
            {
                ExecuteNonQueryCommand("DROP TABLE ident");
            }
        }

        // These next tests verify that 'bulk' operations work. If each command type modifies more than three rows, then we do a Prep/Exec instead of
        // adhoc ExecuteSql.
        [CheckConnStrSetupFact]
        public void AutoGenBulkUpdateTest()
        {
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlCommand cmd = conn.CreateCommand())
            using (SqlDataAdapter adapter = new SqlDataAdapter())
            using (SqlDataAdapter adapterVerify = new SqlDataAdapter())
            {
                try
                {
                    conn.Open();

                    cmd.CommandText = "SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country into " + _tempTable + " from Employees where EmployeeID < 3";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "alter table " + _tempTable + " add constraint " + _tempKey + " primary key (EmployeeID)";
                    cmd.ExecuteNonQuery();

                    adapter.SelectCommand = new SqlCommand("SELECT EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country FROM " + _tempTable + " WHERE EmployeeID < 3", conn);
                    adapterVerify.SelectCommand = new SqlCommand("SELECT LastName, FirstName FROM " + _tempTable + " where FirstName='" + MagicName + "'", conn);

                    adapter.TableMappings.Add(_tempTable, "rowset");
                    adapterVerify.TableMappings.Add(_tempTable, "rowset");

                    DataSet dataSet = new DataSet();
                    adapter.FillSchema(dataSet, SchemaType.Mapped, _tempTable);

                    dataSet.Tables["rowset"].PrimaryKey = new DataColumn[] { dataSet.Tables["rowset"].Columns["EmployeeID"] };
                    adapter.Fill(dataSet, _tempTable);

                    // Verify that set is empty
                    DataSet dataSetVerify = new DataSet();
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);

                    SqlCommandBuilder builder = new SqlCommandBuilder(adapter);

                    // Bulk Insert  (10 records)
                    DataRow datarow = null;
                    const int cOps = 5;
                    for (int i = 0; i < cOps * 2; i++)
                    {
                        datarow = dataSet.Tables["rowset"].NewRow();
                        string sid = "99999000" + i.ToString();
                        datarow.ItemArray = new object[] { sid, "Bulk Insert" + i.ToString(), MagicName, "Engineer", "One Microsoft Way", "Redmond", "WA", "98052", "USA" };
                        datarow.Table.Rows.Add(datarow);
                    }
                    adapter.Update(dataSet, _tempTable);
                    // Verify that set has 10 'Magic' entries
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 10, _tempTable);
                    dataSet.AcceptChanges();

                    // Bulk Update (first 5)
                    for (int i = 0; i < cOps; i++)
                    {
                        string sid = "99999000" + i.ToString();
                        datarow = dataSet.Tables["rowset"].Rows.Find(sid);
                        datarow.BeginEdit();
                        datarow.ItemArray = new object[] { sid, "Bulk Update" + i.ToString(), MagicName, "reenignE", "Yaw Tfosorcim Eno", "Dnomder", "WA", "52098", "ASU" };
                        datarow.EndEdit();
                    }

                    // Bulk Delete (last 5)
                    for (int i = cOps; i < cOps * 2; i++)
                    {
                        string sid = "99999000" + i.ToString();
                        dataSet.Tables["rowset"].Rows.Find(sid).Delete();
                    }

                    // now update the dataSet with the insert and delete changes
                    adapter.Update(dataSet, _tempTable);
                    // Verify that set has 5 'Magic' updated entries
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 5, _tempTable);

                    // clean up the remaining 5 rows
                    for (int i = 0; i < cOps; i++)
                    {
                        string sid = "99999000" + i.ToString();
                        dataSet.Tables["rowset"].Rows.Find(sid).Delete();
                    }
                    adapter.Update(dataSet, _tempTable);
                    // Verify that set has no entries
                    VerifyUpdateRow(adapterVerify, dataSetVerify, 0, _tempTable);
                }
                finally
                {
                    ExecuteNonQueryCommand("DROP TABLE " + _tempTable);
                }
            }
        }

        [CheckConnStrSetupFact]
        public void TestDeriveParameters()
        {
            string spEmployeeSales =
            "create procedure [dbo].[Test_EmployeeSalesByCountry] " +
            "@Beginning_Date DateTime, @Ending_Date DateTime AS " +
            "SELECT Employees.Country, Employees.LastName, Employees.FirstName, Orders.ShippedDate, Orders.OrderID, \"Order Subtotals\".Subtotal AS SaleAmount " +
            "FROM Employees INNER JOIN " +
                "(Orders INNER JOIN \"Order Subtotals\" ON Orders.OrderID = \"Order Subtotals\".OrderID) " +
                "ON Employees.EmployeeID = Orders.EmployeeID " +
            "WHERE Orders.ShippedDate Between @Beginning_Date And @Ending_Date";
            string dropSpEmployeeSales = "drop procedure [dbo].[Test_EmployeeSalesByCountry]";

            string expectedParamResults =
                "\"@RETURN_VALUE\" AS Int32 OF Int FOR Current \"\" " +
                "0, 0, 0, ReturnValue, DEFAULT; " +
                "\"@Beginning_Date\" AS DateTime OF DateTime FOR Current \"\" " +
                "0, 0, 0, Input, DEFAULT; " +
                "\"@Ending_Date\" AS DateTime OF DateTime FOR Current \"\" " +
                "0, 0, 0, Input, DEFAULT; ";

            try
            {
                ExecuteNonQueryCommand(spEmployeeSales);

                using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
                using (SqlCommand cmd = new SqlCommand("Test_EmployeeSalesByCountry", connection))
                {
                    string errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_DeriveParametersNotSupported, "SqlCommand", cmd.CommandType);
                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(
                        () => SqlCommandBuilder.DeriveParameters(cmd),
                        errorMessage);

                    errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_OpenConnectionRequired, "DeriveParameters", "");
                    cmd.CommandType = CommandType.StoredProcedure;
                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(
                        () => SqlCommandBuilder.DeriveParameters(cmd),
                        errorMessage);

                    connection.Open();

                    SqlCommandBuilder.DeriveParameters(cmd);
                    CheckParameters(cmd, expectedParamResults);

                    cmd.CommandText = "Test_EmployeeSalesBy";
                    errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_NoStoredProcedureExists, cmd.CommandText);
                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(
                        () => SqlCommandBuilder.DeriveParameters(cmd),
                        errorMessage);

                    cmd.CommandText = "Test_EmployeeSalesByCountry";
                    SqlCommandBuilder.DeriveParameters(cmd);
                    CheckParameters(cmd, expectedParamResults);
                }
            }
            finally
            {
                ExecuteNonQueryCommand(dropSpEmployeeSales);
            }
        }

        #region Utility_Methods
        private void CheckParameters(SqlCommand cmd, string expectedResults)
        {
            Debug.Assert(null != cmd, "DumpParameters: null SqlCommand");

            string actualResults = "";
            StringBuilder builder = new StringBuilder();
            foreach (SqlParameter p in cmd.Parameters)
            {
                byte precision = p.Precision;
                byte scale = p.Scale;
                builder.Append("\"" + p.ParameterName + "\" AS " + p.DbType.ToString("G") + " OF " + p.SqlDbType.ToString("G") + " FOR " + p.SourceVersion.ToString("G") + " \"" + p.SourceColumn + "\" ");
                builder.Append(p.Size.ToString() + ", " + precision.ToString() + ", " + scale.ToString() + ", " + p.Direction.ToString("G") + ", " + DBConvertToString(p.Value) + "; ");
            }
            actualResults = builder.ToString();

            DataTestUtility.AssertEqualsWithDescription(expectedResults, actualResults, "Unexpected Parameter results.");
        }

        private bool ByteArraysEqual(byte[] expectedBytes, byte[] actualBytes)
        {
            DataTestUtility.AssertEqualsWithDescription(
                expectedBytes.Length, actualBytes.Length,
                "Unexpected array length.");

            for (int i = 0; i < expectedBytes.Length; i++)
            {
                DataTestUtility.AssertEqualsWithDescription(
                    expectedBytes[i], actualBytes[i],
                    "Unexpected byte value.");
            }

            return true;
        }

        private void InitDataValues()
        {
            _c_numeric_val = new decimal(42424242.42);
            _c_unique_val = new byte[16];
            _c_unique_val[0] = 0xba;
            _c_unique_val[1] = 0xad;
            _c_unique_val[2] = 0xf0;
            _c_unique_val[3] = 0x0d;
            _c_unique_val[4] = 0xba;
            _c_unique_val[5] = 0xad;
            _c_unique_val[6] = 0xf0;
            _c_unique_val[7] = 0x0d;
            _c_unique_val[8] = 0xba;
            _c_unique_val[9] = 0xad;
            _c_unique_val[10] = 0xf0;
            _c_unique_val[11] = 0x0d;
            _c_unique_val[12] = 0xba;
            _c_unique_val[13] = 0xad;
            _c_unique_val[14] = 0xf0;
            _c_unique_val[15] = 0x0d;
            _c_guid_val = new Guid(_c_unique_val);
            _c_varbinary_val = _c_unique_val;
            _c_binary_val = _c_unique_val;
            _c_money_val = new decimal((double)123456789.99);
            _c_smallmoney_val = new decimal((double)-6543.21);
            _c_datetime_val = new DateTime(1971, 7, 20, 23, 59, 59);
            _c_smalldatetime_val = new DateTime(1971, 7, 20, 23, 59, 0);
            _c_nvarchar_val = "1234567890";
            _c_nchar_val = _c_nvarchar_val;
            _c_varchar_val = _c_nvarchar_val;
            _c_char_val = _c_nvarchar_val;
            _c_int_val = unchecked((int)0xffffffff);
            _c_smallint_val = unchecked((short)0xffff);
            _c_tinyint_val = 0xff;
            _c_bigint_val = 0x11ffffff;
            _c_bit_val = true;
            _c_float_val = (double)12345678.2;
            _c_real_val = (float)12345.1;

            _values = new object[18];
            _values[0] = _c_numeric_val;
            _values[1] = _c_smalldatetime_val;
            _values[2] = _c_guid_val;
            _values[3] = _c_varbinary_val;
            _values[4] = _c_binary_val;
            _values[5] = _c_money_val;
            _values[6] = _c_smallmoney_val;
            _values[7] = _c_nvarchar_val;
            _values[8] = _c_varchar_val;
            _values[9] = _c_char_val;
            _values[10] = _c_int_val;
            _values[11] = _c_smallint_val;
            _values[12] = _c_tinyint_val;
            _values[13] = _c_bigint_val;
            _values[14] = _c_bit_val;
            _values[15] = _c_float_val;
            _values[16] = _c_real_val;
            _values[17] = _c_datetime_val;
        }

        private void VerifyFillSchemaResult(DataTable[] tables, string[] expectedTableNames)
        {
            DataTestUtility.AssertEqualsWithDescription(expectedTableNames.Length, tables.Length, "Unequal number of tables.");
            for (int i = 0; i < tables.Length; i++)
            {
                DataTestUtility.AssertEqualsWithDescription(expectedTableNames[i], tables[i].TableName, "Unexpected DataTable TableName.");
            }
        }

        // Prepares the Insert, Update, and Delete command to test updating against Northwind.Employees
        private void PrepareUpdateCommands(SqlDataAdapter adapter, SqlConnection conn, string table)
        {
            // insert
            adapter.InsertCommand = new SqlCommand()
            {
                CommandText = "INSERT INTO " + table + "(EmployeeID, LastName, FirstName, Title, Address, City, Region, PostalCode, Country) " +
                "VALUES (@EmployeeID, @LastName, @FirstName, @Title, @Address, @City, @Region, @PostalCode, @Country)"
            };
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@EmployeeID", SqlDbType.Int, 0, "EmployeeID"));
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 20, "LastName"));
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 10, "FirstName"));
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@Title", SqlDbType.NVarChar, 30, "Title"));
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 60, "Address"));
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 15, "City"));
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@Region", SqlDbType.NVarChar, 15, "Region"));
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@PostalCode", SqlDbType.NVarChar, 10, "PostalCode"));
            adapter.InsertCommand.Parameters.Add(new SqlParameter("@Country", SqlDbType.NVarChar, 15, "Country"));

            adapter.InsertCommand.Connection = conn;

            // update
            adapter.UpdateCommand = new SqlCommand()
            {
                CommandText = "UPDATE " + table + " SET " +
                "EmployeeID = @EmployeeID, LastName = @LastName, FirstName = @FirstName, Title = @Title, Address = @Address, City = @City, Region = @Region, " +
                "PostalCode = @PostalCode, Country = @Country WHERE (EmployeeID = @OldEmployeeID)"
            };
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@EmployeeID", SqlDbType.Int, 0, "EmployeeID"));
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@LastName", SqlDbType.NVarChar, 20, "LastName"));
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@FirstName", SqlDbType.NVarChar, 10, "FirstName"));
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@Title", SqlDbType.NVarChar, 30, "Title"));
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@Address", SqlDbType.NVarChar, 60, "Address"));
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@City", SqlDbType.NVarChar, 15, "City"));
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@Region", SqlDbType.NVarChar, 15, "Region"));
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@PostalCode", SqlDbType.NVarChar, 10, "PostalCode"));
            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@Country", SqlDbType.NVarChar, 15, "Country"));

            adapter.UpdateCommand.Parameters.Add(new SqlParameter("@OldEmployeeID", SqlDbType.Int, 0, "EmployeeID")).SourceVersion = DataRowVersion.Original;
            adapter.UpdateCommand.Connection = conn;

            //
            // delete
            //
            adapter.DeleteCommand = new SqlCommand()
            {
                CommandText = "DELETE FROM " + table + " WHERE (EmployeeID = @EmployeeID)"
            };
            adapter.DeleteCommand.Parameters.Add(new SqlParameter("@EmployeeID", SqlDbType.Int, 0, "EmployeeID"));
            adapter.DeleteCommand.Connection = conn;
        }

        private void RowUpdating_UpdateRefreshTest(object sender, SqlRowUpdatingEventArgs e)
        {
            // make sure that we always get a cloned command back (which means that it should always have the badapple parameter!)
            e.Command = (SqlCommand)((ICloneable)e.Command).Clone();
            DataTestUtility.AssertEqualsWithDescription("sp_insert", e.Command.CommandText.Substring(0, 9), "Unexpected command name.");
            e.Command.Parameters.RemoveAt("@badapple");
        }

        private void RowUpdated_UpdateRefreshTest(object sender, SqlRowUpdatedEventArgs e)
        {
            DataTestUtility.AssertEqualsWithDescription("sp_insert", e.Command.CommandText.Substring(0, 9), "Unexpected command name.");
        }

        private void VerifyUpdateRow(SqlDataAdapter sa, DataSet ds, int cRows, string table)
        {
            ds.Reset();
            sa.Fill(ds, table);

            // don't dump out all the data, just get the row count
            if (cRows > 0)
            {
                Assert.True(ds.Tables[0].Rows.Count == cRows, "FAILED:  expected " + cRows.ToString() + " rows but got " + ds.Tables[0].Rows.Count.ToString());
            }

            ds.Reset();
        }

        private bool VerifyOutputParams(SqlParameterCollection sqlParameters)
        {
            return
                (int)sqlParameters[1].Value == 2000 &&
                (0 == string.Compare((string)sqlParameters[2].Value, "Success!", false, CultureInfo.InvariantCulture)) &&
                (int)sqlParameters[3].Value == 42;
        }

        private string DBConvertToString(object value)
        {
            StringBuilder builder = new StringBuilder();
            WriteObject(builder, value, CultureInfo.InvariantCulture, null, 0, int.MaxValue);
            return builder.ToString();
        }

        private void WriteObject(StringBuilder textBuilder, object value, CultureInfo cultureInfo, Hashtable used, int indent, int recursionLimit)
        {
            if (0 > --recursionLimit)
            {
                return;
            }
            if (null == value)
            {
                textBuilder.Append("DEFAULT");
            }
            else if (Convert.IsDBNull(value))
            {
                textBuilder.Append("ISNULL");
            }
            else
            {
                Type valuetype = value.GetType();

                if ((null != used) && (!valuetype.IsPrimitive))
                {
                    if (used.Contains(value))
                    {
                        textBuilder.Append('#');
                        textBuilder.Append(((int)used[value]).ToString(cultureInfo));
                        return;
                    }
                    else
                    {
                        textBuilder.Append('#');
                        textBuilder.Append(used.Count.ToString(cultureInfo));
                        used.Add(value, used.Count);
                    }
                }
                if ((value is string) || (value is SqlString))
                {
                    if (value is SqlString)
                    {
                        value = ((SqlString)value).Value;
                    }
                    textBuilder.Append(valuetype.Name);
                    textBuilder.Append(":");
                    textBuilder.Append(((string)value).Length);
                    textBuilder.Append("<");
                    textBuilder.Append((string)value);
                    textBuilder.Append(">");
                }
                else if ((value is DateTime) || (value is SqlDateTime))
                {
                    if (value is SqlDateTime)
                    {
                        value = ((SqlDateTime)value).Value;
                    }
                    textBuilder.Append(valuetype.Name);
                    textBuilder.Append("<");
                    textBuilder.Append(((DateTime)value).ToString("s", cultureInfo));
                    textBuilder.Append(">");
                }
                else if ((value is float) || (value is SqlSingle))
                {
                    if (value is SqlSingle)
                    {
                        value = ((SqlSingle)value).Value;
                    }
                    textBuilder.Append(valuetype.Name);
                    textBuilder.Append("<");
                    textBuilder.Append(((float)value).ToString(cultureInfo));
                    textBuilder.Append(">");
                }
                else if ((value is double) || (value is SqlDouble))
                {
                    if (value is SqlDouble)
                    {
                        value = ((SqlDouble)value).Value;
                    }
                    textBuilder.Append(valuetype.Name);
                    textBuilder.Append("<");
                    textBuilder.Append(((double)value).ToString(cultureInfo));
                    textBuilder.Append(">");
                }
                else if ((value is decimal) || (value is SqlDecimal) || (value is SqlMoney))
                {
                    if (value is SqlDecimal)
                    {
                        value = ((SqlDecimal)value).Value;
                    }
                    else if (value is SqlMoney)
                    {
                        value = ((SqlMoney)value).Value;
                    }
                    textBuilder.Append(valuetype.Name);
                    textBuilder.Append("<");
                    textBuilder.Append(((decimal)value).ToString(cultureInfo));
                    textBuilder.Append(">");
                }
                else if (value is INullable && ((INullable)value).IsNull)
                {
                    textBuilder.Append(valuetype.Name);
                    textBuilder.Append(" ISNULL");
                }
                else if (valuetype.IsArray)
                {
                    textBuilder.Append(valuetype.Name);
                    Array array = (Array)value;

                    if (1 < array.Rank)
                    {
                        textBuilder.Append("{");
                    }

                    for (int i = 0; i < array.Rank; ++i)
                    {
                        int count = array.GetUpperBound(i);

                        textBuilder.Append(' ');
                        textBuilder.Append(count - array.GetLowerBound(i) + 1);
                        textBuilder.Append("{ ");
                        for (int k = array.GetLowerBound(i); k <= count; ++k)
                        {
                            AppendNewLineIndent(textBuilder, indent + 1);
                            textBuilder.Append(',');
                            WriteObject(textBuilder, array.GetValue(k), cultureInfo, used, 0, recursionLimit);
                            textBuilder.Append(' ');
                        }
                        AppendNewLineIndent(textBuilder, indent);
                        textBuilder.Append("}");
                    }
                    if (1 < array.Rank)
                    {
                        textBuilder.Append('}');
                    }
                }
                else if (value is ICollection)
                {
                    textBuilder.Append(valuetype.Name);
                    ICollection collection = (ICollection)value;
                    object[] newvalue = new object[collection.Count];
                    collection.CopyTo(newvalue, 0);

                    textBuilder.Append(' ');
                    textBuilder.Append(newvalue.Length);
                    textBuilder.Append('{');
                    for (int k = 0; k < newvalue.Length; ++k)
                    {
                        AppendNewLineIndent(textBuilder, indent + 1);
                        textBuilder.Append(',');
                        WriteObject(textBuilder, newvalue[k], cultureInfo, used, indent + 1, recursionLimit);
                    }
                    AppendNewLineIndent(textBuilder, indent);
                    textBuilder.Append('}');
                }
                else if (value is Type)
                {
                    textBuilder.Append(valuetype.Name);
                    textBuilder.Append('<');
                    textBuilder.Append((value as Type).FullName);
                    textBuilder.Append('>');
                }
                else if (valuetype.IsEnum)
                {
                    textBuilder.Append(valuetype.Name);
                    textBuilder.Append('<');
                    textBuilder.Append(Enum.GetName(valuetype, value));
                    textBuilder.Append('>');
                }
                else
                {
                    string fullName = valuetype.FullName;
                    if ("System.ComponentModel.ExtendedPropertyDescriptor" == fullName)
                    {
                        textBuilder.Append(fullName);
                    }
                    else
                    {
                        FieldInfo[] fields = valuetype.GetFields(BindingFlags.Instance | BindingFlags.Public);
                        PropertyInfo[] properties = valuetype.GetProperties(BindingFlags.Instance | BindingFlags.Public);

                        bool hasinfo = false;
                        if ((null != fields) && (0 < fields.Length))
                        {
                            textBuilder.Append(fullName);
                            fullName = null;

                            Array.Sort(fields, FieldInfoCompare.s_default);
                            for (int i = 0; i < fields.Length; ++i)
                            {
                                FieldInfo field = fields[i];

                                AppendNewLineIndent(textBuilder, indent + 1);
                                textBuilder.Append(field.Name);
                                textBuilder.Append('=');
                                object newvalue = field.GetValue(value);
                                WriteObject(textBuilder, newvalue, cultureInfo, used, indent + 1, recursionLimit);
                            }
                            hasinfo = true;
                        }
                        if ((null != properties) && (0 < properties.Length))
                        {
                            if (null != fullName)
                            {
                                textBuilder.Append(fullName);
                                fullName = null;
                            }

                            Array.Sort(properties, PropertyInfoCompare.s_default);
                            for (int i = 0; i < properties.Length; ++i)
                            {
                                PropertyInfo property = properties[i];
                                if (property.CanRead)
                                {
                                    ParameterInfo[] parameters = property.GetIndexParameters();
                                    if ((null == parameters) || (0 == parameters.Length))
                                    {
                                        AppendNewLineIndent(textBuilder, indent + 1);
                                        textBuilder.Append(property.Name);
                                        textBuilder.Append('=');
                                        object newvalue = null;
                                        bool haveValue = false;
                                        try
                                        {
                                            newvalue = property.GetValue(value, BindingFlags.Public | BindingFlags.GetProperty, null, null, CultureInfo.InvariantCulture);
                                            haveValue = true;
                                        }
                                        catch (TargetInvocationException e)
                                        {
                                            textBuilder.Append(e.InnerException.GetType().Name);
                                            textBuilder.Append(": ");
                                            textBuilder.Append(e.InnerException.Message);
                                        }
                                        if (haveValue)
                                        {
                                            WriteObject(textBuilder, newvalue, cultureInfo, used, indent + 1, recursionLimit);
                                        }
                                    }
                                }
                            }
                            hasinfo = true;
                        }
                        if (!hasinfo)
                        {
                            textBuilder.Append(valuetype.Name);
                            textBuilder.Append('<');
                            MethodInfo method = valuetype.GetMethod("ToString", new Type[] { typeof(IFormatProvider) });
                            if (null != method)
                            {
                                textBuilder.Append((string)method.Invoke(value, new object[] { cultureInfo }));
                            }
                            else
                            {
                                string text = value.ToString();
                                textBuilder.Append(text);
                            }
                            textBuilder.Append('>');
                        }
                    }
                }
            }
        }

        private void ExecuteNonQueryCommand(string cmdText)
        {
            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = cmdText;
                cmd.ExecuteNonQuery();
            }
        }

        private void AppendNewLineIndent(StringBuilder textBuilder, int indent)
        {
            textBuilder.Append(Environment.NewLine);
            char[] buf = _appendNewLineIndentBuffer;
            if (buf.Length < indent * 4)
            {
                buf = new char[indent * 4];
                for (int i = 0; i < buf.Length; ++i)
                {
                    buf[i] = ' ';
                }
                _appendNewLineIndentBuffer = buf;
            }
            textBuilder.Append(buf, 0, indent * 4);
        }

        private class PropertyInfoCompare : IComparer
        {
            internal static PropertyInfoCompare s_default = new PropertyInfoCompare();

            private PropertyInfoCompare()
            {
            }

            public int Compare(object x, object y)
            {
                string propertyInfoName1 = ((PropertyInfo)x).Name;
                string propertyInfoName2 = ((PropertyInfo)y).Name;

                return CultureInfo.InvariantCulture.CompareInfo.Compare(propertyInfoName1, propertyInfoName2, CompareOptions.IgnoreCase);
            }
        }

        private class FieldInfoCompare : IComparer
        {
            internal static FieldInfoCompare s_default = new FieldInfoCompare();

            private FieldInfoCompare()
            {
            }

            public int Compare(object x, object y)
            {
                string fieldInfoName1 = ((FieldInfo)x).Name;
                string fieldInfoName2 = ((FieldInfo)y).Name;

                return CultureInfo.InvariantCulture.CompareInfo.Compare(fieldInfoName1, fieldInfoName2, CompareOptions.IgnoreCase);
            }
        }
        #endregion
    }
}


