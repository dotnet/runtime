// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.SqlTypes;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class DataStreamTest
    {
        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void RunAllTestsForSingleServer_NP()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DataTestUtility.AssertThrowsWrapper<PlatformNotSupportedException>(() => RunAllTestsForSingleServer(DataTestUtility.NpConnStr, true));
            }
            else
            {
                RunAllTestsForSingleServer(DataTestUtility.NpConnStr, true);
            }
        }

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public static void RunAllTestsForSingleServer_TCP()
        {
            RunAllTestsForSingleServer(DataTestUtility.TcpConnStr);
        }

        private static void RunAllTestsForSingleServer(string connectionString, bool usingNamePipes = false)
        {
            RowBuffer(connectionString);
            InvalidRead(connectionString);
            VariantRead(connectionString);
            TypeRead(connectionString);
            SQLTypeRead(connectionString);
            GetValueOfTRead(connectionString);
            MultipleResults(connectionString);
            NumericRead(connectionString);
            TimestampRead(connectionString);
            OrphanReader(connectionString);
            BufferSize(connectionString);
            ExecuteXmlReaderTest(connectionString);
            SequentialAccess(connectionString);
            HasRowsTest(connectionString);
            CloseConnection(connectionString);
            OpenConnection(connectionString);
            SqlCharsBytesTest(connectionString);
            GetStream(connectionString);
            GetTextReader(connectionString);
            GetXmlReader(connectionString);
            ReadStream(connectionString);
            ReadTextReader(connectionString);
            StreamingBlobDataTypes(connectionString);
            OutOfOrderGetChars(connectionString);
            TestXEventsStreaming(connectionString);
            // These tests fail with named pipes, since they try to do DNS lookups on named pipe paths.
            if (!usingNamePipes)
            {
                //if (DataTestUtility.IsUsingNativeSNI()) /* [ActiveIssue(33930)] */
                //{
                //    TimeoutDuringReadAsyncWithClosedReaderTest(connectionString);
                //}
                NonFatalTimeoutDuringRead(connectionString);
            }
        }

        private static void MultipleResults(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query =
                    "select orderid from orders where orderid < @id order by orderid;" +
                    "set no_browsetable on set fmtonly on select * from shippers order by shipperid set fmtonly off;" +
                    "select * from shippers order by shipperid;" +
                    "select * from region order by regionid set no_browsetable off;" +
                    "select lastname from employees order by lastname";

                // Each array in the expectedResults is a separate query result
                string[][] expectedResults =
                {
                    new string[] { "10248", "10249", "10250", "10251", "10252", "10253", "10254" }, // All separate rows
                    new string[] { "" }, // Empty query result
                    new string[]
                    {
                        "1", "Speedy Express", "(503) 555-9831",  // Query Row 1
                        "2", "United Package", "(503) 555-3199",  // Query Row 2
                        "3", "Federal Shipping", "(503) 555-9931" // Query Row 3
                    },
                    new string[]
                    {
                        "1", "Eastern                                           ", // Query Row 1
                        "2", "Western                                           ", // Query Row 2
                        "3", "Northern                                          ", // Query Row 3
                        "4", "Southern                                          "  // Query Row 4
                    },
                    new string[] { "Buchanan", "Callahan", "Davolio", "Dodsworth", "Fuller", "King", "Leverling", "Peacock", "Suyama" } // All separate rows
                };

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int)).Value = 10255;
                    using (SqlDataReader r1 = cmd.ExecuteReader())
                    {
                        int numBatches = 0;
                        do
                        {
                            Assert.True(numBatches < expectedResults.Length, "ERROR: Received more batches than were expected.");
                            object[] values = new object[r1.FieldCount];
                            // Current "column" in expected row is (valuesChecked MOD FieldCount), since 
                            // expected rows for current batch are appended together for easy formatting
                            int valuesChecked = 0;
                            while (r1.Read())
                            {
                                r1.GetValues(values);

                                for (int col = 0; col < values.Length; col++, valuesChecked++)
                                {
                                    Assert.True(valuesChecked < expectedResults[numBatches].Length, "ERROR: Received more results for this batch than was expected");
                                    string expectedVal = expectedResults[numBatches][valuesChecked];
                                    string actualVal = values[col].ToString();

                                    DataTestUtility.AssertEqualsWithDescription(expectedVal, actualVal, "FAILED: Received a different value than expected.");
                                }
                            }
                            numBatches++;
                        } while (r1.NextResult());
                    }
                }
            }
        }

        private static void InvalidRead(string connectionString)
        {
            using (SqlConnection c = new SqlConnection(connectionString))
            {
                c.Open();
                string sqlBatch = "select * from orders where orderid < 10253";
                using (SqlCommand cmd = new SqlCommand(sqlBatch, c))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    string errorMessage = SystemDataResourceManager.Instance.SQL_InvalidRead;
                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetInt32(0), errorMessage);
                }
            }
        }

        private static void VariantRead(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sqlBatch = "select * from orders where orderid < 10253";
                using (SqlCommand cmd = new SqlCommand(sqlBatch, conn))
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    rdr.Read();
                    object v = null;

                    DateTime d;
                    decimal m;
                    string s = null;
                    int i;

                    // read data out of buffer
                    v = rdr.GetValue(0); i = (int)v;
                    v = rdr.GetValue(1); s = v is DBNull ? null : (string)v;
                    v = rdr.GetValue(2); i = (int)v;
                    v = rdr.GetValue(3); d = (DateTime)v;
                    v = rdr.GetValue(4); d = (DateTime)v;
                    v = rdr.GetValue(5); d = (DateTime)v;
                    v = rdr.GetValue(6); i = (int)v;
                    v = rdr.GetValue(7); m = (decimal)v;
                    v = rdr.GetValue(8); s = v is DBNull ? null : (string)v;
                    v = rdr.GetValue(9); s = v is DBNull ? null : (string)v;
                    v = rdr.GetValue(10); s = v is DBNull ? null : (string)v;
                    v = rdr.GetValue(11); s = v is DBNull ? null : (string)v;
                    v = rdr.GetValue(12); s = v is DBNull ? null : (string)v;
                    v = rdr.GetValue(13); s = v is DBNull ? null : (string)v;

                    DataTestUtility.AssertEqualsWithDescription("France", s.ToString(), "FAILED: Received incorrect last value.");
                }
            }
        }

        private static void TypeRead(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sqlBatch = "select * from orders where orderid < 10253";
                using (SqlCommand cmd = new SqlCommand(sqlBatch, conn))
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    rdr.Read();

                    DateTime d;
                    decimal m;
                    string s = null;
                    int i;

                    // read data out of buffer
                    i = rdr.GetInt32(0); //order id
                    s = rdr.GetString(1); //customer id
                    i = rdr.GetInt32(2); // employee id
                    d = rdr.GetDateTime(3); //OrderDate
                    d = rdr.GetDateTime(4); //RequiredDate
                    d = rdr.GetDateTime(5); //ShippedDate;
                    i = rdr.GetInt32(6); //ShipVia;
                    m = rdr.GetDecimal(7); //Freight;
                    s = rdr.GetString(8); //ShipName;
                    s = rdr.GetString(9); //ShipAddres;
                    s = rdr.GetString(10); //ShipCity;
                                           // should get an exception here
                    string errorMessage = SystemDataResourceManager.Instance.SqlMisc_NullValueMessage;
                    DataTestUtility.AssertThrowsWrapper<SqlNullValueException>(() => rdr.GetString(11), errorMessage);

                    s = rdr.GetString(12); //ShipPostalCode;
                    s = rdr.GetString(13); //ShipCountry;
                    DataTestUtility.AssertEqualsWithDescription("France", s.ToString(), "FAILED: Received incorrect last value.");
                }
            }
        }

        private static void GetValueOfTRead(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sqlBatch = "select *, CAST(N'<test>Hello, World</test>' AS XML), CAST(NULL AS XML) from orders where orderid < 10253 and shipregion is null";
                using (SqlCommand cmd = new SqlCommand(sqlBatch, conn))
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    string errorMessage = SystemDataResourceManager.Instance.SqlMisc_NullValueMessage;

                    rdr.Read();
                    // read data out of buffer
                    rdr.GetFieldValue<int>(0); //order id
                    rdr.GetFieldValue<string>(1); //customer id
                    rdr.GetFieldValue<SqlInt32>(2); // employee id
                    rdr.GetFieldValue<DateTime>(3); //OrderDate
                    rdr.GetFieldValue<SqlDateTime>(4); //RequiredDate
                    rdr.GetFieldValue<DateTime>(5); //ShippedDate;
                    rdr.GetFieldValue<int>(6); //ShipVia;
                    rdr.GetFieldValue<decimal>(7); //Freight;
                    rdr.GetFieldValue<string>(8); //ShipName;
                    rdr.GetFieldValue<SqlString>(9); //ShipAddres;
                    rdr.IsDBNull(10);
                    rdr.IsDBNull(10);
                    rdr.GetFieldValue<string>(10); //ShipCity;
                    // should get an exception here
                    DataTestUtility.AssertThrowsWrapper<SqlNullValueException>(() => rdr.GetFieldValue<string>(11), errorMessage);
                    rdr.IsDBNull(11);
                    rdr.GetFieldValue<SqlString>(11);
                    rdr.IsDBNull(11);
                    rdr.IsDBNull(12);
                    rdr.GetChars(12, 0, null, 0, 0);
                    rdr.IsDBNull(12);
                    rdr.GetFieldValue<INullable>(13);//ShipCountry;
                    rdr.GetFieldValue<string>(14);
                    DataTestUtility.AssertThrowsWrapper<SqlNullValueException>(() => rdr.GetFieldValue<string>(15), errorMessage);

                    rdr.Read();
                    // read data out of buffer
                    rdr.GetFieldValueAsync<int>(0).Wait(); //order id
                    rdr.GetFieldValueAsync<string>(1).Wait(); //customer id
                    rdr.GetFieldValueAsync<SqlInt32>(2).Wait(); // employee id
                    rdr.GetFieldValueAsync<DateTime>(3).Wait(); //OrderDate
                    rdr.GetFieldValueAsync<SqlDateTime>(4).Wait(); //RequiredDate
                    rdr.GetFieldValueAsync<DateTime>(5).Wait(); //ShippedDate;
                    rdr.GetFieldValueAsync<int>(6).Wait(); //ShipVia;
                    rdr.GetFieldValueAsync<decimal>(7).Wait(); //Freight;
                    rdr.GetFieldValueAsync<string>(8).Wait(); //ShipName;
                    rdr.GetFieldValueAsync<SqlString>(9).Wait(); //ShipAddres;
                    Assert.False(rdr.IsDBNullAsync(10).Result, "FAILED: IsDBNull was true for a non-null value");
                    rdr.GetFieldValueAsync<string>(10).Wait(); //ShipCity;
                    // should get an exception here
                    DataTestUtility.AssertThrowsWrapper<AggregateException, SqlNullValueException>(() => rdr.GetFieldValueAsync<string>(11).Wait(), innerExceptionMessage: errorMessage);
                    Assert.True(rdr.IsDBNullAsync(11).Result, "FAILED: IsDBNull was false for a null value");

                    rdr.IsDBNullAsync(11).Wait();
                    rdr.GetFieldValueAsync<SqlString>(11).Wait();
                    rdr.IsDBNullAsync(11).Wait();
                    rdr.IsDBNullAsync(12).Wait();
                    rdr.GetChars(12, 0, null, 0, 0);
                    rdr.IsDBNullAsync(12).Wait();
                    rdr.GetFieldValueAsync<INullable>(13).Wait(); //ShipCountry;
                    rdr.GetFieldValue<SqlXml>(14);
                    rdr.GetFieldValue<SqlXml>(15);
                    rdr.GetFieldValue<SqlString>(14);
                    rdr.GetFieldValue<SqlString>(15);
                    rdr.GetFieldValue<XmlReader>(14);
                    rdr.GetFieldValue<XmlReader>(15);
                    rdr.GetFieldValueAsync<XmlReader>(14);
                    rdr.GetFieldValueAsync<XmlReader>(15);

                    rdr.Read();
                    Assert.True(rdr.IsDBNullAsync(11).Result, "FAILED: IsDBNull was false for a null value");
                }
            }
        }

        private static void OutOfOrderGetChars(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                string expectedFirstString = "Hello, World!";
                string expectedSecondString = "Another string";

                // NOTE: Must be non-Plp types (i.e. not MAX sized columns)
                using (SqlCommand cmd = new SqlCommand("SELECT @r, @p", conn))
                {
                    cmd.Parameters.AddWithValue("r", expectedFirstString);
                    cmd.Parameters.AddWithValue("p", expectedSecondString);

                    // NOTE: Command behavior must NOT be sequential
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        char[] data = new char[20];
                        reader.Read();

                        // Read last column - this will read in all intermediate columns
                        reader.GetValue(1);

                        // Read in first column with GetChars
                        // Since we've haven't called GetChars yet, this caches the value of the column into _columnDataChars
                        long charsRead = reader.GetChars(0, 0, data, 0, data.Length);
                        string actualFirstString = new string(data, 0, (int)charsRead);

                        // Now read in the second column
                        charsRead = reader.GetChars(1, 0, data, 0, data.Length);
                        string actualSecondString = new string(data, 0, (int)charsRead);

                        // Validate data
                        DataTestUtility.AssertEqualsWithDescription(expectedFirstString, actualFirstString, "FAILED: First string did not match");
                        DataTestUtility.AssertEqualsWithDescription(expectedSecondString, actualSecondString, "FAILED: Second string did not match");
                    }
                }
            }
        }

        private static void SQLTypeRead(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("select * from orders where orderid < 10253", conn))
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    rdr.Read();

                    SqlDateTime d;
                    SqlMoney m;
                    SqlString s = null;
                    SqlInt32 i;

                    // read data out of buffer
                    i = rdr.GetSqlInt32(0); //order id
                    s = rdr.GetSqlString(1); //customer id
                    i = rdr.GetSqlInt32(2); // employee id
                    d = rdr.GetSqlDateTime(3); //OrderDate
                    d = rdr.GetSqlDateTime(4); //RequiredDate
                    d = rdr.GetSqlDateTime(5); //ShippedDate;
                    i = rdr.GetSqlInt32(6); //ShipVia;
                    m = rdr.GetSqlMoney(7); //Freight;
                    s = rdr.GetSqlString(8); //ShipName;
                    s = rdr.GetSqlString(9); //ShipAddres;
                    s = rdr.GetSqlString(10); //ShipCity;
                    s = rdr.GetSqlString(11); //ShipRegion;
                    s = rdr.GetSqlString(12); //ShipPostalCode;
                    s = rdr.GetSqlString(13); //ShipCountry;
                    DataTestUtility.AssertEqualsWithDescription("France", s.ToString(), "FAILED: Received incorrect last value.");
                }
            }
        }

        private static void RowBuffer(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("select * from orders where orderid < 10253", conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int cRecords = 0;
                    object[] buffer = new object[reader.FieldCount];
                    while (reader.Read())
                    {
                        reader.GetValues(buffer);

                        if (cRecords == 0)
                        {
                            VerifySchema(reader);
                        }

                        VerifyData(reader, buffer);
                        cRecords++;
                    }
                    DataTestUtility.AssertEqualsWithDescription(5, cRecords, "FAILED: Received incorrect number of records");
                }
            }
        }

        private static void TimestampRead(string connectionString)
        {
            string tempTable = "##" + Environment.GetEnvironmentVariable("ComputerName") + Environment.TickCount.ToString();
            tempTable = tempTable.Replace('-', '_');

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmdDefault = new SqlCommand("", conn))
                {
                    cmdDefault.CommandText = "create table " + tempTable + "(c1 int, c2 timestamp)";
                    cmdDefault.ExecuteNonQuery();

                    cmdDefault.CommandText = "insert into " + tempTable + "(c1) values (1)";
                    cmdDefault.ExecuteNonQuery();

                    cmdDefault.CommandText = "select * from " + tempTable;
                    using (SqlDataReader reader = cmdDefault.ExecuteReader())
                    {
                        DataTestUtility.AssertEqualsWithDescription("timestamp", reader.GetDataTypeName(1), "FAILED: Data value did not have correct type");
                        reader.Read();

                        object o = reader[1];

                        // timestamps are really 8-byte binary
                        byte[] b = (byte[])o;
                        DataTestUtility.AssertEqualsWithDescription(8, b.Length, "FAILED: Retrieved byte array had incorrect length");

                        SqlBinary sqlBin = reader.GetSqlBinary(1);
                        b = sqlBin.Value;
                        DataTestUtility.AssertEqualsWithDescription(8, b.Length, "FAILED: Retrieved SqlBinary value had incorrect length");
                    }
                }
            }
        }


        private static void BufferSize(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("select * from orders where orderid<@id", conn))
                {
                    cmd.Parameters.Add(new SqlParameter("@id", SqlDbType.Int)).Value = 10252;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        // smaller buffer
                        object[] buf_small = new object[3];
                        // bigger buffer
                        object[] buf_big = new object[reader.FieldCount + 10];
                        object[] buf = buf_small;
                        while (reader.Read())
                        {
                            // alternate buffers
                            reader.GetValues(buf);
                            string bufEntry1 = (string)buf[1];
                            reader.GetSqlValues(buf);
                            SqlString bufEntry2 = ((SqlString)buf[1]).Value;

                            Assert.True(bufEntry1.Equals(bufEntry2.ToString()),
                                string.Format("FAILED: Should have same value with both buffer entries. Buf2 value: {0}. Buf2 value: {1}", bufEntry1, bufEntry2));

                            buf = (buf == buf_small) ? buf_big : buf_small;
                        }
                    }
                }
            }
        }

        private static void OrphanReader(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("select * from orders where orderid < 10253", conn))
                {
                    object value;
                    SqlDataReader reader;
                    using (reader = cmd.ExecuteReader())
                    {
                        conn.Close();
                        Assert.True(reader.IsClosed, "FAILED: Stream was not closed by connection close (Scenario: No Read)");
                    }

                    string errorMessage;
                    conn.Open();
                    using (reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        conn.Close();

                        errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_DataReaderClosed, "CheckDataIsReady");
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => value = reader[0], errorMessage);
                        Assert.True(reader.IsClosed, "FAILED: Stream was not closed by connection close (Scenario: Read)");
                        conn.Open();
                    }

                    using (reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        value = reader[0];
                        conn.Close();

                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => value = reader[0], errorMessage);
                        Assert.True(reader.IsClosed, "FAILED: Stream was not closed by connection close (Scenario: Read Partial Data)");
                        conn.Open();
                    }

                    using (reader = cmd.ExecuteReader())
                    {
                        do
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    value = reader[i];
                                }
                            }
                        } while (reader.NextResult());

                        conn.Close();
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => value = reader[0], errorMessage);
                        Assert.True(reader.IsClosed, "FAILED: Stream was not closed by connection close (Scenario: Read All Data)");
                    }
                }
            }
        }

        private static void ExecuteXmlReaderTest(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("select employeeId, lastname, firstname from employees for xml auto", conn))
                {
                    XmlReader xr;
                    using (xr = cmd.ExecuteXmlReader())
                    {
                        string[] expectedResults =
                        {
                            "<employees employeeId=\"1\" lastname=\"Davolio\" firstname=\"Nancy\" />",
                            "<employees employeeId=\"2\" lastname=\"Fuller\" firstname=\"Andrew\" />",
                            "<employees employeeId=\"3\" lastname=\"Leverling\" firstname=\"Janet\" />",
                            "<employees employeeId=\"4\" lastname=\"Peacock\" firstname=\"Margaret\" />",
                            "<employees employeeId=\"5\" lastname=\"Buchanan\" firstname=\"Steven\" />",
                            "<employees employeeId=\"6\" lastname=\"Suyama\" firstname=\"Michael\" />",
                            "<employees employeeId=\"7\" lastname=\"King\" firstname=\"Robert\" />",
                            "<employees employeeId=\"8\" lastname=\"Callahan\" firstname=\"Laura\" />",
                            "<employees employeeId=\"9\" lastname=\"Dodsworth\" firstname=\"Anne\" />",
                        };

                        xr.Read();
                        for (int i = 0; !xr.EOF; i++)
                        {
                            Assert.True(i < expectedResults.Length, "ERROR: Received more XML results than expected");

                            string actualResult = xr.ReadOuterXml();
                            DataTestUtility.AssertEqualsWithDescription(expectedResults[i], actualResult, "FAILED: Actual XML results differed from expected value.");
                        }
                    }

                    // use a big result to fill up the pipe and do a partial read
                    cmd.CommandText = "select * from orders for xml auto";
                    string errorMessage;
                    using (xr = cmd.ExecuteXmlReader())
                    {
                        xr.Read();

                        // make sure we get an exception if we try to get another reader
                        errorMessage = SystemDataResourceManager.Instance.ADP_OpenReaderExists;
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => xr = cmd.ExecuteXmlReader(), errorMessage);
                    }

                    // use a big result to fill up the pipe and do a partial read
                    cmd.CommandText = "select * from orders for xml auto";
                    using (xr = cmd.ExecuteXmlReader())
                    {
                        xr.Read();
                        conn.Close();
                        conn.Open();
                    }

                    // read a large xml input, should succeed
                    cmd.CommandText = "select * from orders for xml auto";
                    using (xr = cmd.ExecuteXmlReader())
                    {
                        xr.Read();
                        while (!xr.EOF)
                        {
                            xr.ReadOuterXml();
                        }
                    }

                    // read an empty xml output, should succeed
                    cmd.CommandText = "select * from orders where 0 = 1 for xml auto";
                    using (xr = cmd.ExecuteXmlReader())
                    {
                        xr.Read();
                        while (!xr.EOF)
                        {
                            xr.ReadOuterXml();
                        }
                    }

                    // multiple results
                    cmd.CommandText =
                        "select orderid from orders where orderid < 10253 for xml auto;" +
                        "select customerid from customers where customerid < 'ANTON' for xml auto;" +
                        "select employeeId from employees where employeeid < 3 for xml auto;";
                    using (xr = cmd.ExecuteXmlReader())
                    {
                        string[] expectedResults =
                        {
                            "<orders orderid=\"10248\" />",
                            "<orders orderid=\"10249\" />",
                            "<orders orderid=\"10250\" />",
                            "<orders orderid=\"10251\" />",
                            "<orders orderid=\"10252\" />",
                            "<customers customerid=\"ALFKI\" />",
                            "<customers customerid=\"ANATR\" />",
                            "<employees employeeId=\"1\" />",
                            "<employees employeeId=\"2\" />"
                        };
                        xr.Read();
                        for (int i = 0; !xr.EOF; i++)
                        {
                            Assert.True(i < expectedResults.Length, "ERROR: Received more XML results than expected");

                            string actualResult = xr.ReadOuterXml();
                            DataTestUtility.AssertEqualsWithDescription(expectedResults[i], actualResult, "FAILED: Actual XML results differed from expected value.");
                        }
                    }

                    // multiple columns
                    cmd.CommandText = "select * from customers";
                    errorMessage = SystemDataResourceManager.Instance.SQL_NonXmlResult;
                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => xr = cmd.ExecuteXmlReader(), errorMessage);

                    // non-ntext column
                    cmd.CommandText = "select employeeID from employees";
                    errorMessage = SystemDataResourceManager.Instance.SQL_NonXmlResult;
                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => xr = cmd.ExecuteXmlReader(), errorMessage);
                }
            }
        }

        private static void SequentialAccess(string connectionString)
        {
            SqlDataReader reader;
            string s;
            int size = 4096; // some random chunk size
            byte[] data = new byte[size];
            char[] chars = new char[size];
            long cb = 0;
            long di = 0;
            long cbTotal = 0;
            object o;
            int i;
            SqlBinary sqlbin;

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("select * from orders for xml auto", conn))
                {
                    // Simple reads
                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        do
                        {
                            while (reader.Read())
                            {
                                s = reader.GetString(0);
                                cbTotal += s.Length * 2;
                            }
                        } while (reader.NextResult());
                    }
                    DataTestUtility.AssertEqualsWithDescription((long)536198, cbTotal, "FAILED: cbTotal result did not have expected value");

                    // Simple GetFieldValue<T>
                    cbTotal = 0;
                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        do
                        {
                            while (reader.Read())
                            {
                                s = reader.GetFieldValue<string>(0);
                                cbTotal += s.Length * 2;
                            }
                        } while (reader.NextResult());
                    }
                    DataTestUtility.AssertEqualsWithDescription((long)536198, cbTotal, "FAILED: cbTotal result did not have expected value");

                    // Simple GetFieldValueAsync<T>
                    cbTotal = 0;
                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        do
                        {
                            while (reader.Read())
                            {
                                s = reader.GetFieldValueAsync<string>(0).Result;
                                cbTotal += s.Length * 2;
                            }
                        } while (reader.NextResult());
                    }
                    DataTestUtility.AssertEqualsWithDescription((long)536198, cbTotal, "FAILED: cbTotal result did not have expected value");

                    // test sequential access reading everything
                    cbTotal = 0;
                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        do
                        {
                            while (reader.Read())
                            {
                                do
                                {
                                    cb = reader.GetBytes(0, di, data, 0, size);
                                    di += cb;
                                    cbTotal += cb;
                                    if ((int)cb < size)
                                        break;
                                } while (cb > 0);
                                di = 0;
                            }
                        } while (reader.NextResult());
                    }
                    DataTestUtility.AssertEqualsWithDescription((long)536198, cbTotal, "FAILED: cbTotal result did not have expected value");
                }

                // Test IsDBNull
                using (SqlCommand cmd = new SqlCommand("select city, region from employees where region is null", conn))
                {
                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        reader.Read();
                        Assert.False(reader.IsDBNull(0), "FAILED: IsDBNull is true for non-null value");
                        Assert.True(reader.IsDBNull(1), "FAILED: IsDBNull is false for null value");

                        reader.Read();
                        Assert.False(reader.IsDBNullAsync(0).Result, "FAILED: IsDBNull is true for non-null value");
                        Assert.True(reader.IsDBNullAsync(1).Result, "FAILED: IsDBNull is false for null value");
                    }
                }

                using (SqlCommand cmd = new SqlCommand("select * from employees", conn))
                {
                    // test sequential access with partial reads
                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        int currentCity = 0;
                        int currentNote = 0;
                        string[] expectedCities = { "Seattle", "Tacoma", "Kirkland", "Redmond", "London", "London", "London", "Seattle", "London" };
                        string expectedPhotoBytes = "00 21 00 ff ff ff ff 42 69 74 6d 61 70 ";
                        string[] expectedNotes =
                        {
                            "Education inclu", "Andrew received", "Janet has a BS ", "Margaret holds ",
                            "Steven Buchanan", "Michael is a gr", "Robert King ser", "Laura received ", "Anne has a BA d"
                        };

                        while (reader.Read())
                        {
                            i = reader.GetOrdinal("City");
                            o = reader.GetValue(i);
                            DataTestUtility.AssertEqualsWithDescription(expectedCities[currentCity], o.ToString(), "FAILED: Received unexpected city value.");

                            i = reader.GetOrdinal("photo");
                            cb = reader.GetBytes(i, 13, data, 0, 13);
                            StringBuilder byteBuilder = new StringBuilder();
                            for (int j = 0; j < 13; j++)
                            {
                                byteBuilder.Append(data[j].ToString("x2") + " ");
                            }
                            DataTestUtility.AssertEqualsWithDescription(expectedPhotoBytes, byteBuilder.ToString(), "FAILED: Photo byte array did not contain correct values");

                            i = reader.GetOrdinal("notes");
                            cb = reader.GetChars(i, 0, chars, 0, 15);
                            DataTestUtility.AssertEqualsWithDescription(expectedNotes[currentNote], new string(chars, 0, 15), "FAILED: Received unexpected city value.");

                            currentCity++;
                            currentNote++;
                        }
                    }

                    // test GetSqlBinary special case
                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        reader.Read();

                        i = reader.GetOrdinal("photo");
                        long actualLength = reader.GetBytes(i, 0, null, 0, 0);
                        cb = reader.GetBytes(i, 0, data, 0, 13);
                        sqlbin = reader.GetSqlBinary(i);
                        DataTestUtility.AssertEqualsWithDescription((actualLength - 13), (long)sqlbin.Length, "FAILED: Did not receive expected number of bytes");
                    }

                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        // Tests GetBytes\Chars before GetValue
                        reader.Read();
                        i = reader.GetOrdinal("notes");
                        reader.GetChars(i, 14, chars, 0, 14);
                        string errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_NonSequentialColumnAccess, i, i + 1);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetString(i), errorMessage);

                        // Tests GetValue before GetBytes\Chars
                        reader.Read();
                        i = reader.GetOrdinal("photo");
                        reader.GetSqlBinary(i);
                        errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_NonSequentialColumnAccess, i, i + 1);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetBytes(i, 0, data, 0, 13), errorMessage);

                        i = reader.GetOrdinal("notes");
                        reader.GetString(i);
                        errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_NonSequentialColumnAccess, i, i + 1);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetChars(i, 0, chars, 0, 14), errorMessage);

                        // Tests GetBytes\GetChars re-reading same characters
                        reader.Read();
                        i = reader.GetOrdinal("photo");
                        reader.GetBytes(i, 0, data, 0, 13);
                        errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_NonSeqByteAccess, 0, 13, "GetBytes");
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetBytes(i, 0, data, 0, 13), errorMessage);

                        i = reader.GetOrdinal("notes");
                        reader.GetChars(i, 0, chars, 0, 14);
                        errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_NonSeqByteAccess, 0, 14, "GetChars");
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetChars(i, 0, chars, 0, 14), errorMessage);
                    }

                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        // exception tests
                        reader.Read();
                        object[] sqldata = new object[reader.FieldCount];
                        reader.GetValues(sqldata); // should work

                        int columnToTry = 0;
                        string errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_NonSequentialColumnAccess, columnToTry, sqldata.Length);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetInt32(columnToTry), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetValue(columnToTry), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetFieldValue<int>(columnToTry), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetFieldValue<SqlInt32>(columnToTry), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<AggregateException, InvalidOperationException>(() => reader.GetFieldValueAsync<int>(columnToTry).Wait(), innerExceptionMessage: errorMessage);
                        DataTestUtility.AssertThrowsWrapper<AggregateException, InvalidOperationException>(() => reader.GetFieldValueAsync<SqlInt32>(columnToTry).Wait(), innerExceptionMessage: errorMessage);

                        reader.Read();
                        columnToTry = 17;
                        errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_NonSequentialColumnAccess, columnToTry, sqldata.Length);

                        s = reader.GetString(columnToTry);
                        DataTestUtility.AssertEqualsWithDescription("http://accweb/emmployees/fuller.bmp", s, "FAILED: Did not receive expected string.");
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetInt32(columnToTry), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetValue(columnToTry), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetFieldValue<int>(columnToTry), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetFieldValue<SqlInt32>(columnToTry), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<AggregateException, InvalidOperationException>(() => reader.GetFieldValueAsync<int>(columnToTry).Wait(), innerExceptionMessage: errorMessage);
                        DataTestUtility.AssertThrowsWrapper<AggregateException, InvalidOperationException>(() => reader.GetFieldValueAsync<SqlInt32>(columnToTry).Wait(), innerExceptionMessage: errorMessage);

                        reader.Read();
                        // skip all columns up to photo, and read from it partially
                        i = reader.GetOrdinal("photo");
                        // partially read data (20 bytes from offset 50);
                        cb = reader.GetBytes(i, 50, data, 0, 20);
                        DataTestUtility.AssertEqualsWithDescription((long)20, cb, "FAILED: Did not receive expected number of bytes");
                    }

                    // close connection while in the middle of a read
                    using (reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        reader.Read();
                        i = reader.GetOrdinal("photo");
                        cb = reader.GetBytes(i, 50, data, 0, 1);
                        DataTestUtility.AssertEqualsWithDescription((long)1, cb, "FAILED: Did not receive expected number of bytes");
                        conn.Close();

                        // now try to read one more byte
                        string errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_DataReaderClosed, "GetBytes");
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => cb = reader.GetBytes(i, 51, data, 0, 1), errorMessage);
                        errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_DataReaderClosed, "CheckDataIsReady");
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetValue(i), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetFieldValue<byte[]>(i), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetFieldValue<SqlBinary>(i), errorMessage);
                        errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_DataReaderClosed, "GetFieldValueAsync");
                        DataTestUtility.AssertThrowsWrapper<AggregateException, InvalidOperationException>(() => reader.GetFieldValueAsync<byte[]>(i).Wait(), innerExceptionMessage: errorMessage);
                        DataTestUtility.AssertThrowsWrapper<AggregateException, InvalidOperationException>(() => reader.GetFieldValueAsync<SqlBinary>(i).Wait(), innerExceptionMessage: errorMessage);
                    }
                }
            }
        }

        private static void NumericRead(string connectionString)
        {
            string tempTable = "##" + Environment.GetEnvironmentVariable("ComputerName") + Environment.TickCount.ToString();
            tempTable = tempTable.Replace('-', '_');

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("", conn))
                {
                    cmd.CommandText = "create table " + tempTable + "(c1 sql_variant, c2 numeric(38,23))";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "insert into " + tempTable + " values (convert(numeric(38,23), -123456789012345.67890123456789012345678), convert(numeric(38,23), -123456789012345.67890123456789012345678))";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = "select * from " + tempTable;
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();

                        object o = reader.GetSqlValue(0);
                        SqlDecimal n = reader.GetSqlDecimal(1);

                        Assert.True(o is SqlDecimal, "FAILED: Query result was not a SqlDecimal value");
                        DataTestUtility.AssertEqualsWithDescription("-123456789012345.67890123456789012345678", ((SqlDecimal)o).ToString(), "FAILED: SqlDecimal did not have expected value");
                        DataTestUtility.AssertEqualsWithDescription("-123456789012345.67890123456789012345678", n.ToString(), "FAILED: SqlDecimal did not have expected value");

                        // com+ type coercion should fail
                        // Em
                        object value;
                        string errorMessage = SystemDataResourceManager.Instance.SqlMisc_ConversionOverflowMessage;
                        DataTestUtility.AssertThrowsWrapper<OverflowException>(() => value = reader[0], errorMessage);
                        DataTestUtility.AssertThrowsWrapper<OverflowException>(() => value = reader[1], errorMessage);
                        DataTestUtility.AssertThrowsWrapper<OverflowException>(() => value = reader.GetDecimal(0), errorMessage);
                        DataTestUtility.AssertThrowsWrapper<OverflowException>(() => value = reader.GetDecimal(1), errorMessage);
                    }
                }
            }
        }

        private static void HasRowsTest(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sqlBatch =
                    "select top 10 * from orders;" +
                    "select top 5 * from orders;" +
                    "select top 0 * from orders;";
                using (SqlCommand cmd = new SqlCommand(sqlBatch, conn))
                {
                    SqlDataReader reader;
                    using (reader = cmd.ExecuteReader())
                    {
                        Assert.True(reader.HasRows, "FAILED: Failure #1: HasRows");
                        while (reader.Read())
                        {
                            Assert.True(reader.HasRows, "FAILED: Failure #2: HasRows");
                        }
                        Assert.True(reader.HasRows, "FAILED: Failure #3: HasRows");

                        Assert.True(reader.NextResult(), "FAILED: Failure #3.5: NextResult");

                        Assert.True(reader.HasRows, "FAILED: Failure #4: HasRows");
                        while (reader.Read())
                        {
                            Assert.True(reader.HasRows, "FAILED: Failure #5: HasRows");
                        }
                        Assert.True(reader.HasRows, "FAILED: Failure #6: HasRows");

                        Assert.True(reader.NextResult(), "FAILED: Failure #6.5: NextResult");

                        Assert.False(reader.HasRows, "FAILED: Failure #7: HasRows");
                        while (reader.Read())
                        {
                            Assert.False(reader.HasRows, "FAILED: Failure #8: HasRows");
                        }
                        Assert.False(reader.HasRows, "FAILED: Failure #9: HasRows");

                        Assert.False(reader.NextResult(), "FAILED: Failure #9.5: NextResult");

                        Assert.False(reader.HasRows, "FAILED: Failure #10: HasRows");
                    }

                    bool result;
                    string errorMessage = string.Format(SystemDataResourceManager.Instance.ADP_DataReaderClosed, "HasRows");
                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => result = reader.HasRows, errorMessage);
                }
            }
        }

        private static void SqlCharsBytesTest(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // select with SqlChars	parameter
                SqlCommand cmd;
                SqlDataReader reader;
                using (cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select EmployeeID, FirstName, LastName from Employees where Title = @vm ";

                    (cmd.Parameters.Add("@vm", SqlDbType.VarChar)).Value = new SqlChars("Vice President, Sales");

                    using (reader = cmd.ExecuteReader())
                    {
                        Assert.True(reader.Read(), "FAILED: No results were returned from read()");
                        DataTestUtility.AssertEqualsWithDescription(2, reader.GetInt32(0), "FAILED: GetInt32(0) result did not match expected value");
                        DataTestUtility.AssertEqualsWithDescription("Andrew", reader.GetString(1), "FAILED: GetString(1) result did not match expected value");
                        DataTestUtility.AssertEqualsWithDescription("Fuller", reader.GetString(2), "FAILED: GetString(2) result did not match expected value");
                    }
                }

                // select with SqlBytes	parameter
                using (cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "select EmployeeID, FirstName, LastName from Employees where EmployeeID = 2 and Convert(binary(5), Photo) = @bn ";

                    byte[] barr = new byte[5] { 0x15, 0x1c, 0x2F, 0x00, 0x02 };
                    (cmd.Parameters.Add("@bn", SqlDbType.VarBinary)).Value = new SqlBytes(barr);

                    using (reader = cmd.ExecuteReader())
                    {
                        Assert.True(reader.Read(), "FAILED: No results were returned from read()");
                        DataTestUtility.AssertEqualsWithDescription(2, reader.GetInt32(0), "FAILED: GetInt32(0) result did not match expected value");
                        DataTestUtility.AssertEqualsWithDescription("Andrew", reader.GetString(1), "FAILED: GetString(1) result did not match expected value");
                        DataTestUtility.AssertEqualsWithDescription("Fuller", reader.GetString(2), "FAILED: GetString(2) result did not match expected value");
                    }
                }
            }
        }

        private static void CloseConnection(string connectionString)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand("select * from orders where orderid < 10253", conn))
                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    DataTestUtility.AssertEqualsWithDescription(ConnectionState.Open, conn.State, "FAILED: Connection should be in open state");

                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            reader.GetValue(i);
                        }
                    }
                }

                DataTestUtility.AssertEqualsWithDescription(ConnectionState.Closed, conn.State, "FAILED: Connection should be in closed state after reader close");
            }
        }

        private static void OpenConnection(string connectionString)
        {
            // Isolates OpenConnection behavior for sanity testing on x-plat
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                DataTestUtility.AssertEqualsWithDescription(ConnectionState.Open, conn.State, "FAILED: Connection should be in open state");
            }
        }

        private static void SeqAccessFailureWrapper<TException>(Action action, CommandBehavior behavior) where TException : Exception
        {
            if (behavior == CommandBehavior.SequentialAccess)
                DataTestUtility.AssertThrowsWrapper<TException>(action);
            else
                action();
        }

        private static void GetStream(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT 0x12341234, 0x12341234, 12, CAST(NULL AS VARBINARY(MAX)), 0x12341234, 0x12341234, 0x12341234, CAST(REPLICATE('a', 8000) AS VARBINARY(MAX)), 0x12341234", connection))
                {
                    CommandBehavior[] behaviors = new CommandBehavior[] { CommandBehavior.Default, CommandBehavior.SequentialAccess };
                    foreach (CommandBehavior behavior in behaviors)
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            byte[] buffer = new byte[9000];
                            reader.Read();

                            // Basic success paths
                            reader.GetStream(0);
                            reader.GetStream(1);

                            // Bad values
                            DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => reader.GetStream(2));
                            // Null stream
                            Stream stream = reader.GetStream(3);
                            Assert.False(stream.Read(buffer, 0, buffer.Length) > 0, "FAILED: Read more than 0 bytes from a null stream");

                            // Get column before current column
                            Action action = (() => reader.GetStream(0));
                            SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                            // Two streams on same column
                            reader.GetStream(4);
                            action = (() => reader.GetStream(4));
                            SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                            // GetStream then GetBytes on same column
                            reader.GetStream(5);
                            action = (() => reader.GetBytes(5, 0, null, 0, 0));
                            SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                            action = (() => reader.GetBytes(5, 0, buffer, 0, buffer.Length));
                            SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                            // GetBytes then GetStream on same column
                            reader.GetBytes(6, 0, null, 0, 0);
                            action = (() => reader.GetStream(6));
                            SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                            reader.GetBytes(6, 0, buffer, 0, buffer.Length);
                            SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);
#if DEBUG
                            // GetStream while async is pending
                            Task t = null;
                            using (PendAsyncReadsScope pendScope = new PendAsyncReadsScope(reader))
                            {
                                t = reader.ReadAsync();
                                Assert.False(t.Wait(1), "FAILED: Read completed immediately");
                                DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetStream(8));
                            }
                            t.Wait();

                            // GetStream after Read 
                            DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetStream(0));
#endif
                        }

                        // IsDBNull + GetStream
                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            reader.Read();
                            reader.GetStream(8);
                            // Past column
                            reader.IsDBNull(8);
                        }

                        // IsDBNullAsync + GetStream
                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            reader.Read();
                            reader.GetStream(8);
                            // Past column
                            reader.IsDBNullAsync(8).Wait();
                        }
                    }
#if DEBUG
                    // Test GetStream is non-blocking
                    using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                    {
                        reader.Read();
                        Task t = null;
                        using (PendAsyncReadsScope pendScope = new PendAsyncReadsScope(reader))
                        {
                            t = Task.Factory.StartNew(() => reader.GetStream(8));
                            t.Wait(1000);
                            Assert.True(t.IsCompleted, "FAILED: Failed to get stream within 1 second");
                            t = reader.ReadAsync();
                        }
                        t.Wait();
                    }
#endif
                }
            }
        }

        private static void GetTextReader(string connectionString)
        {
            string[] queryStrings =
            {
                "SELECT N'Hello World', N'Hello World', 12, CAST(NULL AS NVARCHAR(MAX)), N'Hello World', N'Hello World', N'Hello World', CAST(REPLICATE('a', 8000) AS NVARCHAR(MAX)), N'Hello World'",
                "SELECT 'Hello World', 'Hello World', 12, CAST(NULL AS VARCHAR(MAX)), 'Hello World', 'Hello World', 'Hello World', CAST(REPLICATE('a', 8000) AS VARCHAR(MAX)), 'Hello World' COLLATE Latin1_General_CI_AS",
                string.Format("SELECT {0} {1}, {0} {1}, 12, CAST(NULL AS VARCHAR(MAX)), {0} {1}, {0} {1}, {0} {1}, CAST(REPLICATE(('\uFF8A' {1}), 8000) AS VARCHAR(MAX)), {0} {1}", "'\uFF8A\uFF9B\uFF70\uFF9C\uFF70\uFF99\uFF84\uFF9E'", "COLLATE Japanese_CI_AS")
            };

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                foreach (string query in queryStrings)
                {
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        CommandBehavior[] behaviors = new CommandBehavior[] { CommandBehavior.Default, CommandBehavior.SequentialAccess };
                        foreach (CommandBehavior behavior in behaviors)
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                char[] buffer = new char[9000];
                                reader.Read();

                                // Basic success paths
                                reader.GetTextReader(0);
                                reader.GetTextReader(1);

                                // Bad values
                                DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => reader.GetTextReader(2));
                                // Null stream
                                TextReader textReader = reader.GetTextReader(3);
                                Assert.False(textReader.Read(buffer, 0, buffer.Length) > 0, "FAILED: Read more than 0 chars from a null TextReader");

                                // Get column before current column
                                Action action = (() => reader.GetTextReader(0));
                                SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                                // Two TextReaders on same column
                                reader.GetTextReader(4);
                                action = (() => reader.GetTextReader(4));
                                SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                                // GetTextReader then GetBytes on same column
                                reader.GetTextReader(5);
                                action = (() => reader.GetChars(0, 0, null, 0, 0));
                                SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                                action = (() => reader.GetChars(5, 0, buffer, 0, buffer.Length));
                                SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                                // GetBytes then GetTextReader on same column
                                reader.GetChars(6, 0, null, 0, 0);
                                action = (() => reader.GetTextReader(6));
                                SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                                reader.GetChars(6, 0, buffer, 0, buffer.Length);
                                SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);
#if DEBUG
                                // GetTextReader while async is pending
                                Task t = null;
                                using (PendAsyncReadsScope pendScope = new PendAsyncReadsScope(reader))
                                {
                                    t = reader.ReadAsync();
                                    Assert.False(t.IsCompleted, "FAILED: Read completed immediately");
                                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetTextReader(8));
                                }
                                t.Wait();

                                // GetTextReader after Read 
                                DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetTextReader(0));
#endif
                            }

                            // IsDBNull + GetTextReader
                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                reader.Read();
                                reader.GetTextReader(8);
                                // Past column
                                reader.IsDBNull(8);
                            }

                            // IsDBNullAsync + GetTextReader
                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                reader.Read();
                                reader.GetTextReader(8);
                                // Past column
                                reader.IsDBNullAsync(8).Wait();
                            }
                        }
#if DEBUG
                        // Test GetTextReader is non-blocking
                        using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess))
                        {
                            reader.Read();

                            Task t = null;
                            using (PendAsyncReadsScope pendScope = new PendAsyncReadsScope(reader))
                            {
                                t = Task.Factory.StartNew(() => reader.GetTextReader(8));
                                t.Wait(1000);
                                Assert.True(t.IsCompleted, "FAILED: Failed to get TextReader within 1 second");
                                t = reader.ReadAsync();
                            }
                            t.Wait();
                        }
#endif
                    }
                }
            }
        }

        private static void GetXmlReader(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string xml = "CAST('<test><subtest /><subtest>asdfasdfasdf</subtest></test>' AS XML)";
                string queryString = string.Format("SELECT {0}, {0}, 12, CAST(NULL AS XML), {0}, CAST(('<test>' + REPLICATE(CAST('a' AS VARCHAR(MAX)), 10000) + '</test>') AS XML), {0}", xml);
                using (SqlCommand cmd = new SqlCommand(queryString, connection))
                {
                    CommandBehavior[] behaviors = new CommandBehavior[] { CommandBehavior.Default, CommandBehavior.SequentialAccess };
                    foreach (CommandBehavior behavior in behaviors)
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            reader.Read();

                            // Basic success paths
                            reader.GetXmlReader(0);
                            reader.GetXmlReader(1);

                            // Bad values
                            DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => reader.GetXmlReader(2));
                            // Null stream
                            XmlReader xmlReader = reader.GetXmlReader(3);
                            Assert.False(xmlReader.Read(), "FAILED: Successfully read on a null XmlReader");

                            // Get column before current column
                            Action action = (() => reader.GetXmlReader(0));
                            SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);

                            // Two XmlReaders on same column
                            reader.GetXmlReader(4);
                            action = (() => reader.GetXmlReader(4));
                            SeqAccessFailureWrapper<InvalidOperationException>(action, behavior);
#if DEBUG
                            // GetXmlReader while async is pending
                            Task t = null;
                            using (PendAsyncReadsScope pendScope = new PendAsyncReadsScope(reader))
                            {
                                t = reader.ReadAsync();
                                Assert.False(t.IsCompleted, "FAILED: Read completed immediately");
                                DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetXmlReader(6));
                            }
                            t.Wait();

                            // GetXmlReader after Read 
                            DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.GetXmlReader(0));
#endif
                        }
                    }
                }
            }
        }

        private static void ReadStream(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                CommandBehavior[] behaviors = new CommandBehavior[] { CommandBehavior.Default, CommandBehavior.SequentialAccess };
                foreach (CommandBehavior behavior in behaviors)
                {
                    byte[] smallBuffer = new byte[2];
                    byte[] buffer = new byte[16];
                    byte[] largeBuffer = new byte[9000];
                    Stream stream = null;
                    Action action = null;
                    using (SqlCommand cmd = new SqlCommand("SELECT 0x12341234", connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            reader.Read();
                            // Basic case
                            using (stream = reader.GetStream(0))
                            {
                                stream.Read(smallBuffer, 0, smallBuffer.Length);
                                stream.Read(buffer, 2, 2);

                                // Testing stream properties
                                stream.Flush();
                                DataTestUtility.AssertThrowsWrapper<NotSupportedException>(() => stream.SetLength(1));
                                Action<Stream> performOnStream = ((s) => { int i = s.WriteTimeout; });
                                DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => performOnStream(stream));
                                if (behavior == CommandBehavior.SequentialAccess)
                                {
                                    DataTestUtility.AssertThrowsWrapper<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
                                    performOnStream = ((s) => { long i = s.Position; });
                                    DataTestUtility.AssertThrowsWrapper<NotSupportedException>(() => performOnStream(stream));
                                    performOnStream = ((s) => { long i = s.Length; });
                                    DataTestUtility.AssertThrowsWrapper<NotSupportedException>(() => performOnStream(stream));
                                }
                                else
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    long position = stream.Position;
                                    long length = stream.Length;
                                }
                            }

                            // Once Stream is closed
                            DataTestUtility.AssertThrowsWrapper<ObjectDisposedException>(() => stream.Read(buffer, 0, buffer.Length));
                        }

                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            reader.Read();
                            // Reading more than is there, and when there is nothing there
                            stream = reader.GetStream(0);
                            stream.Read(buffer, 0, buffer.Length);
                            stream.Read(buffer, 0, buffer.Length);

                            // Argument exceptions
                            DataTestUtility.AssertThrowsWrapper<ArgumentNullException>(() => stream.Read(null, 0, 1));
                            DataTestUtility.AssertThrowsWrapper<ArgumentOutOfRangeException>(() => stream.Read(buffer, -1, 2));
                            DataTestUtility.AssertThrowsWrapper<ArgumentOutOfRangeException>(() => stream.Read(buffer, 2, -1));
                            DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => stream.Read(buffer, buffer.Length, buffer.Length));
                            DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => stream.Read(buffer, int.MaxValue, int.MaxValue));
                        }

                        // Once Reader is closed
                        action = (() => stream.Read(buffer, 0, buffer.Length));
                        SeqAccessFailureWrapper<ObjectDisposedException>(action, behavior);
                    }

                    using (SqlCommand cmd = new SqlCommand("SELECT 0x12341234, 12", connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            reader.Read();
                            // Read after advancing reader
                            stream = reader.GetStream(0);
                            reader.GetInt32(1);

                            action = (() => stream.Read(buffer, 0, buffer.Length));
                            SeqAccessFailureWrapper<ObjectDisposedException>(action, behavior);
                        }
                    }

                    if (behavior == CommandBehavior.SequentialAccess)
                    {
                        using (SqlCommand cmd = new SqlCommand("SELECT CAST(REPLICATE('a', 8000) AS VARBINARY(MAX)), CAST(REPLICATE('a', 8000) AS VARBINARY(MAX))", connection))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                // 0 byte read
                                reader.Read();
                                stream = reader.GetStream(1);
                                stream.Read(largeBuffer, 0, 0);
                            }
#if DEBUG
                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                reader.Read();
                                stream = reader.GetStream(1);

                                Task t = null;
                                using (PendAsyncReadsScope debugScope = new PendAsyncReadsScope(reader))
                                {
                                    // Read during async
                                    t = stream.ReadAsync(largeBuffer, 0, largeBuffer.Length);
                                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => stream.Read(largeBuffer, 0, largeBuffer.Length));
                                    DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.Read());
                                }
                                t.Wait();
                            }
                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                reader.Read();
                                stream = reader.GetStream(0);
                                stream.ReadTimeout = 20;
                                Task t = null;

                                using (PendAsyncReadsScope debugScope = new PendAsyncReadsScope(reader))
                                {
                                    // Timeout
                                    t = stream.ReadAsync(largeBuffer, 0, largeBuffer.Length);
                                    // Guarantee that timeout occurs:
                                    Thread.Sleep(stream.ReadTimeout * 4);
                                }
                                DataTestUtility.AssertThrowsWrapper<AggregateException, IOException>(() => t.Wait());
                            }

                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                reader.Read();
                                stream = reader.GetStream(1);
                                Task t = null;
                                using (PendAsyncReadsScope debugScope = new PendAsyncReadsScope(reader))
                                {
                                    // Cancellation
                                    CancellationTokenSource tokenSource = new CancellationTokenSource();
                                    t = stream.ReadAsync(largeBuffer, 0, largeBuffer.Length, tokenSource.Token);
                                    tokenSource.Cancel();
                                }
                                DataTestUtility.AssertThrowsWrapper<AggregateException, TaskCanceledException>(() => t.Wait());
                            }

                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                reader.Read();
                                stream = reader.GetStream(0);
                                Task t = null;
                                using (PendAsyncReadsScope debugScope = new PendAsyncReadsScope(reader, errorCode: 11))
                                {
                                    // Error during read
                                    t = stream.ReadAsync(largeBuffer, 0, largeBuffer.Length);
                                }
                                DataTestUtility.AssertThrowsWrapper<AggregateException, IOException, SqlException>(() => t.Wait());
                            }
#endif
                        }
                    }
                }
            }
        }

        private static void ReadTextReader(string connectionString)
        {
            CommandBehavior[] behaviors = new CommandBehavior[] { CommandBehavior.Default, CommandBehavior.SequentialAccess };

            // Some more complex unicode characters, including surrogate pairs
            byte[] unicodeArray = { 0xFF, 0xDB, 0xFD, 0xDF, 0x34, 0xD8, 0xDD, 0xD8, 0x1E, 0xDC, 0x00, 0x6C, 0x00, 0x34 };
            string unicodeString = System.Text.Encoding.Unicode.GetString(unicodeArray, 0, unicodeArray.Length);

            foreach (CommandBehavior behavior in behaviors)
            {
                string[] correctStrings = {
                    "CAST(('Hello world' COLLATE Latin1_General_CI_AS) AS VARCHAR(MAX))",
                    string.Format("CAST(N'{0}Hello world' AS NVARCHAR(MAX))", unicodeString),
                    "CAST((N'\uFF8A\uFF9B\uFF70\uFF9C\uFF70\uFF99\uFF84\uFF9E' COLLATE Japanese_CI_AS) AS VARCHAR(MAX))" };

                foreach (string correctString in correctStrings)
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        char[] smallBuffer = new char[2];
                        char[] buffer = new char[16];
                        char[] largeBuffer = new char[9000];
                        TextReader textReader = null;
                        Action action = null;
                        using (SqlCommand cmd = new SqlCommand(string.Format("SELECT {0}", correctString), connection))
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                reader.Read();
                                // Basic case
                                using (textReader = reader.GetTextReader(0))
                                {
                                    textReader.Read();
                                    textReader.Read(smallBuffer, 0, smallBuffer.Length);
                                    textReader.Read(buffer, 2, 2);
                                    textReader.Peek();
                                    textReader.Read(buffer, 3, 2);
                                    textReader.Peek();
                                    textReader.Read();
                                }

                                // Once Reader is closed
                                DataTestUtility.AssertThrowsWrapper<ObjectDisposedException>(() => textReader.Read(buffer, 0, buffer.Length));
                            }

                            using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                            {
                                reader.Read();
                                // Reading more than is there, and when there is nothing there
                                textReader = reader.GetTextReader(0);
                                textReader.Read(buffer, 0, buffer.Length);
                                textReader.Read(buffer, 0, buffer.Length);
                                textReader.Read();
                                textReader.Peek();

                                // Argument exceptions
                                DataTestUtility.AssertThrowsWrapper<ArgumentNullException>(() => textReader.Read(null, 0, 1));
                                DataTestUtility.AssertThrowsWrapper<ArgumentOutOfRangeException>(() => textReader.Read(buffer, -1, 2));
                                DataTestUtility.AssertThrowsWrapper<ArgumentOutOfRangeException>(() => textReader.Read(buffer, 2, -1));
                                DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => textReader.Read(buffer, buffer.Length, buffer.Length));
                                DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => textReader.Read(buffer, int.MaxValue, int.MaxValue));
                            }

                            // Once Reader is closed
                            action = (() => textReader.Read(buffer, 0, buffer.Length));
                            SeqAccessFailureWrapper<ObjectDisposedException>(action, behavior);
                        }

                        using (SqlCommand cmd = new SqlCommand(string.Format("SELECT {0}, 12", correctString), connection))
                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            reader.Read();
                            // Read after advancing reader
                            textReader = reader.GetTextReader(0);
                            reader.GetInt32(1);

                            action = (() => textReader.Read(buffer, 0, buffer.Length));
                            SeqAccessFailureWrapper<ObjectDisposedException>(action, behavior);
                        }

                        if (behavior == CommandBehavior.SequentialAccess)
                        {
                            using (SqlCommand cmd = new SqlCommand(string.Format("SELECT REPLICATE({0}, 1500), REPLICATE({0}, 1500)", correctString), connection))
                            {
                                using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                                {
                                    // 0 char read
                                    reader.Read();
                                    textReader = reader.GetTextReader(1);
                                    textReader.Read(largeBuffer, 0, 0);
                                }
#if DEBUG
                                using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                                {
                                    reader.Read();
                                    textReader = reader.GetTextReader(1);

                                    Task t = null;
                                    using (PendAsyncReadsScope debugScope = new PendAsyncReadsScope(reader))
                                    {
                                        // Read during async
                                        t = textReader.ReadAsync(largeBuffer, 0, largeBuffer.Length);
                                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => textReader.Read(largeBuffer, 0, largeBuffer.Length));
                                        DataTestUtility.AssertThrowsWrapper<InvalidOperationException>(() => reader.Read());
                                    }
                                    t.Wait();
                                }

                                using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                                {
                                    reader.Read();
                                    textReader = reader.GetTextReader(0);
                                    Task t = null;
                                    using (PendAsyncReadsScope debugScope = new PendAsyncReadsScope(reader, errorCode: 11))
                                    {
                                        // Error during read
                                        t = textReader.ReadAsync(largeBuffer, 0, largeBuffer.Length);
                                    }
                                    DataTestUtility.AssertThrowsWrapper<AggregateException, IOException, SqlException>(() => t.Wait());
                                }
#endif
                            }
                        }
                    }
                }
            }
        }

        private static void StreamingBlobDataTypes(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                CommandBehavior[] behaviors = new CommandBehavior[] { CommandBehavior.Default, CommandBehavior.SequentialAccess };
                foreach (CommandBehavior behavior in behaviors)
                {
                    // GetStream
                    byte[] correctBytes = { 0x12, 0x34, 0x56, 0x78 };
                    string correctBytesAsString = "0x12345678";
                    string queryString = string.Format("SELECT CAST({0} AS BINARY(20)), CAST({0} AS IMAGE), CAST({0} AS VARBINARY(20)), CAST({0} AS VARBINARY(MAX)), CAST({0} AS SQL_VARIANT)", correctBytesAsString);
                    using (SqlCommand cmd = new SqlCommand(queryString, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                    {
                        reader.Read();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            byte[] buffer = new byte[256];
                            Stream stream = reader.GetStream(i);
                            int bytesRead = stream.Read(buffer, 0, buffer.Length);
                            for (int j = 0; j < correctBytes.Length; j++)
                            {
                                DataTestUtility.AssertEqualsWithDescription(correctBytes[j], buffer[j], "FAILED: Bytes do not match");
                            }
                        }
                    }

                    // GetTextReader
                    string[] correctStrings = { "Hello World", "\uFF8A\uFF9B\uFF70\uFF9C\uFF70\uFF99\uFF84\uFF9E" };
                    string[] collations = { "Latin1_General_CI_AS", "Japanese_CI_AS" };

                    for (int j = 0; j < collations.Length; j++)
                    {
                        string substring = string.Format("(N'{0}' COLLATE {1})", correctStrings[j], collations[j]);
                        queryString = string.Format("SELECT CAST({0} AS CHAR(20)), CAST({0} AS NCHAR(20)), CAST({0} AS NTEXT), CAST({0} AS NVARCHAR(20)), CAST({0} AS NVARCHAR(MAX)), CAST({0} AS TEXT), CAST({0} AS NVARCHAR(20)), CAST({0} AS VARCHAR(MAX)), CAST({0} AS SQL_VARIANT)", substring);
                        using (SqlCommand cmd = new SqlCommand(queryString, connection))
                        using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                        {
                            reader.Read();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                char[] buffer = new char[256];
                                TextReader textReader = reader.GetTextReader(i);
                                int charsRead = textReader.Read(buffer, 0, buffer.Length);
                                string stringRead = new string(buffer, 0, charsRead);
                                DataTestUtility.AssertEqualsWithDescription(correctStrings[j], stringRead.TrimEnd(), "FAILED: Strings to not match");
                            }
                        }
                    }

                    // GetXmlReader
                    string correctXml = "<test><subtest>asdf</subtest><subtest /></test>";
                    byte[] correctXmlAsBytes = System.Text.Encoding.Unicode.GetBytes(correctXml);
                    XmlReader correctXmlReader = XmlReader.Create(new StringReader(correctXml), new XmlReaderSettings() { ConformanceLevel = System.Xml.ConformanceLevel.Fragment, CloseInput = true });
                    queryString = string.Format("SELECT CAST(N'{0}' AS XML)", correctXml);
                    using (SqlCommand cmd = new SqlCommand(queryString, connection))
                    using (SqlDataReader reader = cmd.ExecuteReader(behavior))
                    {
                        reader.Read();
                        XmlReader xmlReader = reader.GetXmlReader(0);
                        bool match = true;
                        while ((match) && (xmlReader.Read()))
                        {
                            match = correctXmlReader.Read();
                            if (match)
                            {
                                match &= (correctXmlReader.Name == xmlReader.Name);
                                match &= (correctXmlReader.Value == xmlReader.Value);
                            }
                        }

                        Assert.True(match, "FAILED: XmlReaders do not match");
                    }
                }
            }
        }

        private static void VariantCollationsTest(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = connection;

                    // Setup japanese database
                    cmd.CommandText = "USE master; IF EXISTS (SELECT * FROM sys.databases WHERE name='japaneseCollationTest') DROP DATABASE japaneseCollationTest; CREATE DATABASE japaneseCollationTest COLLATE Japanese_90_BIN;";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "USE japaneseCollationTest; CREATE TABLE japaneseCollationTest.dbo.tVar (c1 SQL_VARIANT);INSERT INTO japaneseCollationTest.dbo.tVar VALUES (CAST(0xA6 AS VARCHAR(2)) COLLATE Japanese_90_bin);";
                    cmd.ExecuteNonQuery();

                    // Select the same string - once using japaneseCollationTest context and the second time using master context
                    cmd.CommandText = "SELECT c1 FROM japaneseCollationTest.dbo.tVar;";
                    connection.ChangeDatabase("japaneseCollationTest");
                    string fromJapaneseDb = (string)cmd.ExecuteScalar();
                    connection.ChangeDatabase("master");
                    string fromMasterDb = (string)cmd.ExecuteScalar();

                    Assert.True(fromJapaneseDb == fromMasterDb, "FAILED: Variant collations strings do not match");

                    // drop japanese database
                    cmd.CommandText = "USE master; DROP DATABASE japaneseCollationTest;";
                }
            }
        }

       private static void TestXEventsStreaming(string connectionString)
        {   
            string sessionName = "xeventStreamTest";
            //Create XEvent
            SetupXevent(connectionString, sessionName);
            Task.Factory.StartNew(() =>
            {
                // Read XEvents
                int streamXeventCount = 3;
                using (SqlConnection xEventsReadConnection = new SqlConnection(connectionString))
                {
                    xEventsReadConnection.Open();
                    string xEventDataStreamCommand ="USE master; " + @"select [type], [data] from sys.fn_MSxe_read_event_stream ('" + sessionName + "',0)";
                    using (SqlCommand cmd = new SqlCommand(xEventDataStreamCommand, xEventsReadConnection))
                    {
                        SqlDataReader reader = cmd.ExecuteReader(System.Data.CommandBehavior.SequentialAccess);
                        for (int i = 0; i < streamXeventCount && reader.Read(); i++)
                        {
                            Int32 colType = reader.GetInt32(0);
                            int cb = (int)reader.GetBytes(1, 0, null, 0, 0);
                            
                            byte[] bytes = new byte[cb];
                            long read = reader.GetBytes(1, 0, bytes, 0, cb);                            

                            // Don't send data on the first read because there is already data in the buffer. 
                            // Don't send data on the last iteration. We will not be reading that data.
                            if (i == 0 || i == streamXeventCount - 1) continue;

                            using (SqlConnection xEventWriteConnection = new SqlConnection(connectionString))
                            {
                                xEventWriteConnection.Open();
                                string xEventWriteCommandText = @"exec sp_trace_generateevent 90, N'Test2'";
                                using (SqlCommand xEventWriteCommand = new SqlCommand(xEventWriteCommandText, xEventWriteConnection))
                                {
                                    xEventWriteCommand.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }).Wait(10000);
            //Delete XEvent 
            DeleteXevent(connectionString, sessionName);
        }

        private static void SetupXevent(string connectionString, string sessionName)
        {            
            string xEventCreateAndStartCommandText = @"CREATE EVENT SESSION [" + sessionName + @"] ON SERVER 
                        ADD EVENT sqlserver.user_event(ACTION(package0.event_sequence))
                        ADD TARGET package0.ring_buffer
                        WITH (
                            MAX_MEMORY=4096 KB,
                            EVENT_RETENTION_MODE=ALLOW_SINGLE_EVENT_LOSS,
                            MAX_DISPATCH_LATENCY=30 SECONDS,
                            MAX_EVENT_SIZE=0 KB,
                            MEMORY_PARTITION_MODE=NONE,
                            TRACK_CAUSALITY=ON,
                            STARTUP_STATE=OFF)
                            
                        ALTER EVENT SESSION [" + sessionName + "] ON SERVER STATE = START ";
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand createXeventSession = new SqlCommand(xEventCreateAndStartCommandText, connection))
                {
                    createXeventSession.ExecuteNonQuery();                    
                }
            }
        }

        private static void DeleteXevent(string connectionString, string sessionName)
        { 
            string deleteXeventSessionCommand = @"IF EXISTS (select * from sys.server_event_sessions where name ='" + sessionName + "') DROP  EVENT SESSION [" + sessionName + "] ON SERVER";
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand deleteXeventSession = new SqlCommand(deleteXeventSessionCommand, connection))
                {
                    deleteXeventSession.ExecuteNonQuery();
                }
            }
            
        }
    
        private static void TimeoutDuringReadAsyncWithClosedReaderTest(string connectionString)
        {
            // Create the proxy
            ProxyServer proxy = ProxyServer.CreateAndStartProxy(connectionString, out connectionString);
            proxy.SimulatedPacketDelay = 100;
            proxy.SimulatedOutDelay = true;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    // Start the command
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT @p, @p, @p, @p, @p", conn))
                    {
                        cmd.CommandTimeout = 1;
                        cmd.Parameters.AddWithValue("p", new string('a', 3000));
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Start reading, and then force a timeout
                            Task<bool> task = reader.ReadAsync();
                            proxy.PauseCopying();
                            // Before the timeout occurs, but after ReadAsync has started waiting for a packet, close the reader
                            Thread.Sleep(200);
                            Task closeTask = Task.Run(() => reader.Dispose());

                            // Wait for the task to see the timeout
                            string errorMessage = SystemDataResourceManager.Instance.SQL_Timeout;
                            DataTestUtility.AssertThrowsWrapper<AggregateException, SqlException>(() => task.Wait(), innerExceptionMessage: errorMessage);
                        }
                    }
                }
                proxy.Stop();
            }
            catch (SqlException ex)
            {
                // In case of error, stop the proxy and dump its logs (hopefully this will help with debugging
                proxy.Stop();
                throw ex;
            }
        }

        private static void NonFatalTimeoutDuringRead(string connectionString)
        {
            // Create the proxy
            ProxyServer proxy = ProxyServer.CreateAndStartProxy(connectionString, out connectionString);
            proxy.SimulatedPacketDelay = 100;
            proxy.SimulatedOutDelay = true;
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    // Start the command
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT @p, @p, @p, @p, @p", conn))
                    {
                        cmd.CommandTimeout = 1;
                        cmd.Parameters.AddWithValue("p", new string('a', 3000));
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Slow down packets and wait on ReadAsync
                            proxy.SimulatedPacketDelay = 1500;
                            reader.ReadAsync().Wait();

                            // Allow proxy to copy at full speed again
                            proxy.SimulatedOutDelay = false;
                            reader.SetDefaultTimeout(30000);

                            // Close will now observe the stored timeout error
                            string errorMessage = SystemDataResourceManager.Instance.SQL_Timeout;
                            DataTestUtility.AssertThrowsWrapper<SqlException>(reader.Dispose, errorMessage);
                        }
                    }
                }
                proxy.Stop();
            }
            catch
            {
                // In case of error, stop the proxy and dump its logs (hopefully this will help with debugging
                proxy.Stop();
                throw;
            }
        }

        internal static void VerifySchema(SqlDataReader reader)
        {
            string[] expectedColNames =
            {
                "OrderID", "CustomerID", "EmployeeID", "OrderDate", "RequiredDate", "ShippedDate", "ShipVia",
                "Freight", "ShipName", "ShipAddress", "ShipCity", "ShipRegion", "ShipPostalCode", "ShipCountry"
            };
            string[] expectedColTypeNames =
            {
                "int", "nchar", "int", "datetime", "datetime", "datetime", "int",
                "money", "nvarchar", "nvarchar", "nvarchar", "nvarchar", "nvarchar", "nvarchar"
            };

            for (int i = 0; i < reader.FieldCount; i++)
            {
                DataTestUtility.AssertEqualsWithDescription(expectedColNames[i], reader.GetName(i), "FAILED: Received incorrect column name in VerifySchema.");
                DataTestUtility.AssertEqualsWithDescription(expectedColTypeNames[i], reader.GetDataTypeName(i), "FAILED: Received incorrect column type name in VerifySchema.");
            }
        }

        private static bool Equal(object v1, object v2)
        {
            if (v1 is DBNull && v2 is DBNull)
            {
                return true;
            }
            return (v1.Equals(v2));
        }

        internal static void VerifyData(SqlDataReader reader, object[] buffer)
        {
            object value = null;

            for (int i = 0; i < reader.FieldCount; i++)
            {
                value = reader.GetValue(i);
                Assert.True(Equal(buffer[i], value),
                    string.Format("FAILED: Buffer value and stream.GetValue(i, v) don't match. Buffer value: {0}. GetValue value: {1}.", buffer[i], value));

                value = reader[i];
                Assert.True(Equal(buffer[i], value),
                    string.Format("FAILED: Buffer value and stream.this[i] don't match. Buffer value: {0}. this[i] value: {1}.", buffer[i], value));

                value = reader[reader.GetName(i)];
                Assert.True(Equal(buffer[i], value),
                    string.Format("FAILED: Buffer value and stream.this[name] don't match. Buffer value: {0}. this[name] value: {1}.", buffer[i], value));
            }
        }
    }
}
