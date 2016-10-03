// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Data.SqlTypes;
using System.Text;
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
        public static void TestUdt()
        {
            SqlConnection connection = new SqlConnection(DataTestUtility.TcpConnStr);
            connection.Open();
            Console.WriteLine("Connected...");

            SqlCommand command = connection.CreateCommand();
            command.CommandText = "select cast('0x0000' as varbinary(max)) as col0, hierarchyid::Parse('/1/1/3/') as col1, 2 as col2, geometry::Parse('LINESTRING (100 100, 20 180, 180 180)') as col3";
            SqlDataReader reader = command.ExecuteReader();
            Console.WriteLine("Executed...");

            foreach (DbColumn column in reader.GetColumnSchema())
            {
                Console.WriteLine("Schema for column: {0}", column.ColumnName);
                Console.WriteLine("\tudt: {0}", column.UdtAssemblyQualifiedName);
                Console.WriteLine("\ttype: {0}", column.DataTypeName);
            }

            reader.Read();
            Console.WriteLine("Read...");

            Console.WriteLine();
            Console.WriteLine("Read column 0...");
            SqlBytes sqlBytes = reader.GetSqlBytes(0);
            Console.WriteLine("Value length: {0}", sqlBytes.Length);
            Console.WriteLine("SqlBytes Value is null: {0}", sqlBytes.IsNull);
            Console.WriteLine("SqlBytes Value length: {0}", sqlBytes.Length);
            WriteBytes(sqlBytes.Value);

            Console.WriteLine();
            Console.WriteLine("Read column 1...");
            byte[] bytes = (byte[])reader.GetValue(1);
            Console.WriteLine("Value length: {0}", bytes.Length);
            WriteBytes(bytes);

            Console.WriteLine();
            Console.WriteLine("Read column 2...");
            Console.WriteLine(reader.GetValue(2));

            Console.WriteLine();
            Console.WriteLine("Read column 3...");
            sqlBytes = reader.GetSqlBytes(3);
            Console.WriteLine("Value length: {0}", sqlBytes.Length);
            Console.WriteLine("SqlBytes Value is null: {0}", sqlBytes.IsNull);
            Console.WriteLine("SqlBytes Value length: {0}", sqlBytes.Length);
            WriteBytes(sqlBytes.Value);
        }

        private static void WriteBytes(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                hex.AppendFormat("{0:x2}", b);
            Console.WriteLine("Value: {0}", hex.ToString());
            Console.WriteLine();
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
