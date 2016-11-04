// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class DDDataTypesTest
    {
        [CheckConnStrSetupFact]
        public static void XmlTest()
        {
            string tempTable = "xml_" + Guid.NewGuid().ToString().Replace('-', '_');
            string initStr = "create table " + tempTable + " (xml_col XML)";
            string insertNormStr = "INSERT " + tempTable + " VALUES('<doc>Hello World</doc>')";
            string insertParamStr = "INSERT " + tempTable + " VALUES(@x)";
            string queryStr = "select * from " + tempTable;

            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                conn.Open();

                SqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = initStr;
                cmd.ExecuteNonQuery();

                try
                {
                    cmd.CommandText = insertNormStr;
                    cmd.ExecuteNonQuery();

                    SqlCommand cmd2 = new SqlCommand(insertParamStr, conn);

                    cmd2.Parameters.Add("@x", SqlDbType.Xml);
                    XmlReader xr = XmlReader.Create("data.xml");
                    cmd2.Parameters[0].Value = new SqlXml(xr);
                    cmd2.ExecuteNonQuery();

                    cmd.CommandText = queryStr;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int currentValue = 0;
                        string[] expectedValues =
                        {
                            "<doc>Hello World</doc>",
                            "<NewDataSet><builtinCLRtypes><colsbyte>1</colsbyte><colbyte>2</colbyte><colint16>-20</colint16><coluint16>40</coluint16><colint32>-300</colint32><coluint32>300</coluint32><colint64>-4000</colint64><coluint64>4000</coluint64><coldecimal>50000.01</coldecimal><coldouble>600000.987</coldouble><colsingle>70000.9</colsingle><colstring>string variable</colstring><colboolean>true</colboolean><coltimespan>P10675199DT2H48M5.4775807S</coltimespan><coldatetime>9999-12-30T23:59:59.9999999-08:00</coldatetime><colGuid>00000001-0002-0003-0405-060708010101</colGuid><colbyteArray>AQIDBAUGBwgJCgsMDQ4PEA==</colbyteArray><colUri>http://www.abc.com/</colUri><colobjectsbyte xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"byte\">1</colobjectsbyte><colobjectbyte xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"unsignedByte\">2</colobjectbyte><colobjectint16 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"short\">-20</colobjectint16><colobjectuint16 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"unsignedShort\">40</colobjectuint16><colobjectint32 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"int\">-300</colobjectint32><colobjectuint32 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"unsignedInt\">300</colobjectuint32><colobjectint64 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"long\">-4000</colobjectint64><colobjectuint64 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"unsignedLong\">4000</colobjectuint64><colobjectdecimal xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"decimal\">50000.01</colobjectdecimal><colobjectdouble xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"double\">600000.987</colobjectdouble><colobjectsingle xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"float\">70000.9</colobjectsingle><colobjectstring xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"string\">string variable</colobjectstring><colobjectboolean xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"boolean\">true</colobjectboolean><colobjecttimespan xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"duration\">P10675199DT2H48M5.4775807S</colobjecttimespan><colobjectdatetime xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"dateTime\">9999-12-30T23:59:59.9999999-08:00</colobjectdatetime><colobjectguid xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" msdata:InstanceType=\"System.Guid\">00000001-0002-0003-0405-060708010101</colobjectguid><colobjectbytearray xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"base64Binary\">AQIDBAUGBwgJCgsMDQ4PEA==</colobjectbytearray><colobjectUri xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"anyURI\">http://www.abc.com/</colobjectUri></builtinCLRtypes><builtinCLRtypes><colbyte>2</colbyte><colint16>-20</colint16><colint32>-300</colint32><coluint32>300</coluint32><coluint64>4000</coluint64><coldecimal>50000.01</coldecimal><coldouble>600000.987</coldouble><colsingle>70000.9</colsingle><colboolean>true</colboolean><colobjectsbyte xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"byte\">11</colobjectsbyte><colobjectbyte xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"unsignedByte\">22</colobjectbyte><colobjectint16 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"short\">-200</colobjectint16><colobjectuint16 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"unsignedShort\">400</colobjectuint16><colobjectint32 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"int\">-3000</colobjectint32><colobjectuint32 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"unsignedInt\">3000</colobjectuint32><colobjectint64 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"long\">-40000</colobjectint64><colobjectuint64 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"unsignedLong\">40000</colobjectuint64><colobjectdecimal xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"decimal\">500000.01</colobjectdecimal><colobjectdouble xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"double\">6000000.987</colobjectdouble><colobjectsingle xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"float\">700000.9</colobjectsingle><colobjectstring xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"string\">string variable 2</colobjectstring><colobjectboolean xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"boolean\">false</colobjectboolean><colobjecttimespan xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"duration\">-P10675199DT2H48M5.4775808S</colobjecttimespan><colobjectdatetime xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"dateTime\">0001-01-01T00:00:00.0000000-08:00</colobjectdatetime><colobjectguid xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" msdata:InstanceType=\"System.Guid\">00000002-0001-0001-0807-060504030201</colobjectguid><colobjectbytearray xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"base64Binary\">EA8ODQwLCgkIBwYFBAMCAQ==</colobjectbytearray></builtinCLRtypes></NewDataSet>"
                        };

                        while (reader.Read())
                        {
                            Assert.True(currentValue < expectedValues.Length, "ERROR: Received more values than expected");

                            SqlXml sx = reader.GetSqlXml(0);
                            xr = sx.CreateReader();
                            xr.Read();

                            DataTestUtility.AssertEqualsWithDescription(expectedValues[currentValue++], xr.ReadOuterXml(), "FAILED: Did not receive expected data");
                        }
                    }
                }
                finally
                {
                    cmd.CommandText = "drop table " + tempTable;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void MaxTypesTest()
        {
            string tempTable = "max_" + Guid.NewGuid().ToString().Replace('-', '_');
            string initStr = "create table " + tempTable + " (col1 varchar(max), col2 nvarchar(max), col3 varbinary(max))";

            string insertNormStr = "INSERT " + tempTable + " VALUES('ASCIASCIASCIASCIASCIASCIThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first row', ";
            insertNormStr += "N'This is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first row', ";
            insertNormStr += "0x010100110011000111000111000011110000111100001111000001111100000111110000011111000001111100000111110000011111000001111100000111110000011111)";

            string insertParamStr = "INSERT " + tempTable + " VALUES(@x, @y, @z)";
            string queryStr = "select * from " + tempTable;

            using (SqlConnection conn = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                conn.Open();

                SqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = initStr;
                cmd.ExecuteNonQuery();

                try
                {
                    cmd.CommandText = insertNormStr;
                    cmd.ExecuteNonQuery();

                    SqlCommand cmd2 = new SqlCommand(insertParamStr, conn);

                    cmd2.Parameters.Add("@x", SqlDbType.VarChar);
                    cmd2.Parameters.Add("@y", SqlDbType.NVarChar);
                    cmd2.Parameters.Add("@z", SqlDbType.VarBinary);
                    cmd2.Parameters[1].Value = "second line, Insert big, Insert Big, This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";
                    cmd2.Parameters[1].Value += "This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row";

                    byte[] bytes = new byte[2];

                    for (int i = 0; i < bytes.Length; ++i)
                    {
                        bytes[i] = 0xad;
                    }
                    cmd2.Parameters[2].Value = bytes;
                    cmd2.Parameters[0].Value = "This is second row ANSI value";
                    cmd2.ExecuteNonQuery();

                    cmd.CommandText = queryStr;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        int currentValue = 0;
                        string[][] expectedValues =
                        {
                            new string[]
                            {
                                "ASCIASCIASCIASCIASCIASCIThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first row",
                                "This is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first rowThis is the first row",
                                "010100110011000111000111000011110000111100001111000001111100000111110000011111000001111100000111110000011111000001111100000111110000011111"
                            },
                            new string[]
                            {
                                "This is second row ANSI value",
                                "second line, Insert big, Insert Big, This is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second rowThis is the second row",
                                "ADAD"
                            }
                        };

                        while (reader.Read())
                        {
                            Assert.True(currentValue < expectedValues.Length, "ERROR: Received more values than expected");

                            char[] stringResult = reader.GetSqlChars(0).Value;
                            DataTestUtility.AssertEqualsWithDescription(expectedValues[currentValue][0], new string(stringResult, 0, stringResult.Length), "FAILED: Did not receive expected data");
                            stringResult = reader.GetSqlChars(1).Value;
                            DataTestUtility.AssertEqualsWithDescription(expectedValues[currentValue][1], new string(stringResult, 0, stringResult.Length), "FAILED: Did not receive expected data");

                            byte[] bb = reader.GetSqlBytes(2).Value;
                            char[] cc = new char[bb.Length * 2];
                            ConvertBinaryToChar(bb, cc);

                            DataTestUtility.AssertEqualsWithDescription(expectedValues[currentValue][2], new string(cc, 0, cc.Length), "FAILED: Did not receive expected data");
                            currentValue++;
                        }
                    }
                }
                finally
                {
                    cmd.CommandText = "drop table " + tempTable;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtDataReaderGetValueThrowsPlatformNotSupportedException()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/') as col0";
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());

                    Assert.Throws<PlatformNotSupportedException>(() =>
                    {
                        object valud = reader[0];
                    });

                    Assert.Throws<PlatformNotSupportedException>(() =>
                    {
                        reader.GetValue(0);
                    });

                    Assert.Throws<PlatformNotSupportedException>(() =>
                    {
                        reader.GetFieldValue<byte[]>(0);
                    });

                    AggregateException exception = Assert.Throws<AggregateException>(() =>
                    {
                        reader.GetFieldValueAsync<byte[]>(0).Wait();
                    });
                    Assert.IsType(typeof(PlatformNotSupportedException), exception.InnerException);

                    Assert.Throws<PlatformNotSupportedException>(() =>
                    {
                        object[] values = new object[1];
                        reader.GetValues(values);
                    });

                    Assert.Throws<PlatformNotSupportedException>(() =>
                    {
                        object[] values = new object[1];
                        reader.GetSqlValues(values);
                    });

                    Assert.Throws<PlatformNotSupportedException>(() =>
                    {
                        reader.GetProviderSpecificFieldType(0);
                    });

                    Assert.Throws<PlatformNotSupportedException>(() =>
                    {
                        reader.GetProviderSpecificValue(0);
                    });

                    Assert.Throws<PlatformNotSupportedException>(() =>
                    {
                        object[] values = new object[1];
                        reader.GetProviderSpecificValues(values);
                    });
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlParameterThrowsPlatformNotSupportedException()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();

                // This command is not executed on the server since we should throw well before we send the query
                command.CommandText = "select @p as col0";

                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@p",
                    SqlDbType = SqlDbType.Udt,
                    Direction = ParameterDirection.Input,
                });

                Assert.Throws<PlatformNotSupportedException>(() =>
                {
                    command.ExecuteReader();
                });


                command.Parameters.Clear();
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@p",
                    SqlDbType = SqlDbType.Udt,
                    Direction = ParameterDirection.InputOutput,
                });

                Assert.Throws<PlatformNotSupportedException>(() =>
                {
                    command.ExecuteReader();
                });


                command.Parameters.Clear();
                command.Parameters.Add(new SqlParameter
                {
                    ParameterName = "@p",
                    SqlDbType = SqlDbType.Udt,
                    Direction = ParameterDirection.Output,
                });

                Assert.Throws<PlatformNotSupportedException>(() =>
                {
                    command.ExecuteReader();
                });
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlCommandExecuteScalarThrowsPlatformNotSupportedException()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();

                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/') as col0";
                Assert.Throws<PlatformNotSupportedException>(() =>
                {
                    object value = command.ExecuteScalar();
                });


                SqlCommand asyncCommand = connection.CreateCommand();
                asyncCommand.CommandText = "select hierarchyid::Parse('/') as col0";
                AggregateException exception = Assert.Throws<AggregateException>(() =>
                {
                    asyncCommand.ExecuteScalarAsync().Wait();
                });
                Assert.IsType(typeof(PlatformNotSupportedException), exception.InnerException);
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtZeroByte()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/') as col0";
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.False(reader.IsDBNull(0));
                    SqlBytes sqlBytes = reader.GetSqlBytes(0);
                    Assert.False(sqlBytes.IsNull, "Expected a zero length byte array");
                    Assert.True(sqlBytes.Length == 0, "Expected a zero length byte array");
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetSqlBytesSequentialAccess()
        {
            TestUdtSqlDataReaderGetSqlBytes(CommandBehavior.SequentialAccess);
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetSqlBytes()
        {
            TestUdtSqlDataReaderGetSqlBytes(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetSqlBytes(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    SqlBytes sqlBytes = null;

                    sqlBytes = reader.GetSqlBytes(0);
                    Assert.Equal("5ade", ToHexString(sqlBytes.Value));

                    sqlBytes = reader.GetSqlBytes(1);
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(sqlBytes.Value));

                    sqlBytes = reader.GetSqlBytes(2);
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(sqlBytes.Value));

                    if (behavior == CommandBehavior.Default)
                    {
                        sqlBytes = reader.GetSqlBytes(0);
                        Assert.Equal("5ade", ToHexString(sqlBytes.Value));
                    }
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetBytesSequentialAccess()
        {
            TestUdtSqlDataReaderGetBytes(CommandBehavior.SequentialAccess);
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetBytes()
        {
            TestUdtSqlDataReaderGetBytes(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetBytes(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    int byteCount = 0;
                    byte[] bytes = null;

                    byteCount = (int) reader.GetBytes(0, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(0, 0, bytes, 0, bytes.Length);
                    Assert.Equal("5ade", ToHexString(bytes));

                    byteCount = (int)reader.GetBytes(1, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(1, 0, bytes, 0, bytes.Length);
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(bytes));

                    byteCount = (int)reader.GetBytes(2, 0, null, 0, 0);
                    Assert.True(byteCount > 0);
                    bytes = new byte[byteCount];
                    reader.GetBytes(2, 0, bytes, 0, bytes.Length);
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(bytes));

                    if (behavior == CommandBehavior.Default)
                    {
                        byteCount = (int)reader.GetBytes(0, 0, null, 0, 0);
                        Assert.True(byteCount > 0);
                        bytes = new byte[byteCount];
                        reader.GetBytes(0, 0, bytes, 0, bytes.Length);
                        Assert.Equal("5ade", ToHexString(bytes));
                    }
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetStreamSequentialAccess()
        {
            TestUdtSqlDataReaderGetStream(CommandBehavior.SequentialAccess);
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSqlDataReaderGetStream()
        {
            TestUdtSqlDataReaderGetStream(CommandBehavior.Default);
        }

        private static void TestUdtSqlDataReaderGetStream(CommandBehavior behavior)
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(behavior))
                {
                    Assert.True(reader.Read());

                    MemoryStream buffer = null;
                    byte[] bytes = null;

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(0))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("5ade", ToHexString(bytes));

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(1))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("0000000001040300000000000000000059400000000000005940000000000000344000000000008066400000000000806640000000000080664001000000010000000001000000ffffffff0000000002", ToHexString(bytes));

                    buffer = new MemoryStream();
                    using (Stream stream = reader.GetStream(2))
                    {
                        stream.CopyTo(buffer);
                    }
                    bytes = buffer.ToArray();
                    Assert.Equal("e610000001148716d9cef7d34740d7a3703d0a975ec08716d9cef7d34740cba145b6f3955ec0", ToHexString(bytes));

                    if (behavior == CommandBehavior.Default)
                    {
                        buffer = new MemoryStream();
                        using (Stream stream = reader.GetStream(0))
                        {
                            stream.CopyTo(buffer);
                        }
                        bytes = buffer.ToArray();
                        Assert.Equal("5ade", ToHexString(bytes));
                    }
                }
            }
        }

        [CheckConnStrSetupFact]
        public static void TestUdtSchemaMetadata()
        {
            using (SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr))
            {
                connection.Open();
                SqlCommand command = connection.CreateCommand();
                command.CommandText = "select hierarchyid::Parse('/1/1/3/') as col0, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col1, geography::Parse('LINESTRING(-122.360 47.656, -122.343 47.656)') as col2";
                using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    ReadOnlyCollection<DbColumn> columns = reader.GetColumnSchema();

                    DbColumn column = null;

                    // Validate Microsoft.SqlServer.Types.SqlHierarchyId, Microsoft.SqlServer.Types, Version=11.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91
                    column = columns[0];
                    Assert.Equal(column.ColumnName, "col0");
                    Assert.Equal(typeof(byte[]), column.DataType);
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlHierarchyId");

                    // Validate Microsoft.SqlServer.Types.SqlGeometry, Microsoft.SqlServer.Types, Version = 11.0.0.0, Culture = neutral, PublicKeyToken = 89845dcd8080cc91
                    column = columns[1];
                    Assert.Equal(column.ColumnName, "col1");
                    Assert.Equal(typeof(byte[]), column.DataType);
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlGeometry");

                    // Validate Microsoft.SqlServer.Types.SqlGeography, Microsoft.SqlServer.Types, Version = 11.0.0.0, Culture = neutral, PublicKeyToken = 89845dcd8080cc91
                    column = columns[2];
                    Assert.Equal(column.ColumnName, "col2");
                    Assert.Equal(typeof(byte[]), column.DataType);
                    Assert.NotNull(column.UdtAssemblyQualifiedName);
                    AssertSqlUdtAssemblyQualifiedName(column.UdtAssemblyQualifiedName, "Microsoft.SqlServer.Types.SqlGeography");
                }
            }
        }

        private static void AssertSqlUdtAssemblyQualifiedName(string assemblyQualifiedName, string expectedType)
        {
            List<string> parts = assemblyQualifiedName.Split(',').Select(x => x.Trim()).ToList();

            string type = parts[0];
            string assembly = parts.Count < 2 ? string.Empty : parts[1];
            string version = parts.Count < 3 ? string.Empty : parts[2];
            string culture = parts.Count < 4 ? string.Empty : parts[3];
            string token = parts.Count < 5 ? string.Empty : parts[4];

            Assert.Equal(expectedType, type);
            Assert.Equal("Microsoft.SqlServer.Types", assembly);
            Assert.True(version.StartsWith("Version"));
            Assert.True(culture.StartsWith("Culture"));
            Assert.True(token.StartsWith("PublicKeyToken"));
        }

        private static string ToHexString(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        private static char localByteToChar(int b)
        {
            char c;

            if ((b & 0xf) < 10)
            {
                c = (char)((b & 0xf) + '0');
            }
            else
            {
                c = (char)((b & 0xf) - 10 + 'A');
            }

            return c;
        }

        private static void ConvertBinaryToChar(byte[] bb, char[] cc)
        {
            for (int i = 0; i < bb.Length; ++i)
            {
                cc[2 * i] = localByteToChar((bb[i] >> 4) & 0xf);
                cc[2 * i + 1] = localByteToChar(bb[i] & 0xf);
            }
        }
    }
}
