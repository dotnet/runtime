// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace Stress.Data
{
    /// <summary>
    /// Base class to generate utility objects required for stress tests to run. For example: connection strings, command texts, 
    /// data tables and views, and other information
    /// </summary>
    public abstract class DataStressFactory : IDisposable
    {
        // This is the maximum number of rows, stress will operate on
        public const int Depth = 100;

        // A string value to be used for scalar data retrieval while constructing
        // a select statement that retrieves multiple result sets.
        public static readonly string LargeStringParam = new string('p', 2000);

        // A temp table that when create puts the server session into a non-recoverable state until dropped.
        private static readonly string s_tempTableName = string.Format("#stress_{0}", Guid.NewGuid().ToString("N"));

        // The languages used for "SET LANGUAGE [language]" statements that modify the server session state.  Let's
        // keep error message readable so we're only using english languages.
        private static string[] s_languages = new string[]
        {
            "English",
            "British English",
        };

        public DbProviderFactory DbFactory { get; private set; }

        protected DataStressFactory(DbProviderFactory factory)
        {
            DataStressErrors.Assert(factory != null, "Argument to DataStressFactory constructor is null");
            this.DbFactory = factory;
        }


        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public abstract string GetParameterName(string pName);


        public abstract bool PrimaryKeyValueIsRequired
        {
            get;
        }

        [Flags]
        public enum SelectStatementOptions
        {
            UseNOLOCK = 0x1,

            // keep last
            Default = 0
        }

        #region PoolingStressMode

        public enum PoolingStressMode
        {
            RandomizeConnectionStrings,    // Use many different connection strings with the same identity, which will result in many DbConnectionPoolGroups each containing one DbConnectionPool
        }

        protected PoolingStressMode CurrentPoolingStressMode
        {
            get;
            private set;
        }

        #endregion


        /// <summary>
        /// Creates a new connection and initializes it with random connection string generated from the factory's source
        /// Note: if rnd is null, create a connection with minimal string required to connect to the target database        
        /// </summary>
        /// <param name="rnd">Randomizes Connection Pool enablement, the application Name to randomize connection pool</param>
        /// <param name="options"></param>
        /// <returns></returns>
        public DataStressConnection CreateConnection(Random rnd = null, ConnectionStringOptions options = ConnectionStringOptions.Default)
        {
            // Determine connection options (connection string, identity, etc)
            string connectionString = CreateBaseConnectionString(rnd, options);
            bool clearPoolBeforeClose = false;

            if (rnd != null)
            {
                // Connection string and/or identity are randomized

                // We implement this using the Application Name field in the connection string since this field
                // should not affect behaviour other than connection pooling, since all connections in a pool 
                // must have the exact same connection string (including Application Name)

                if (rnd.NextBool(.1))
                {
                    // Disable pooling
                    connectionString = connectionString + ";Pooling=false;";
                }
                else if (rnd.NextBool(0.001))
                {
                    // Use a unique Application Name to get a new connection from a new pool. We do this in order to 
                    // stress the code that creates/deletes pools.
                    connectionString = string.Format("{0}; Pooling=true; Application Name=\"{1}\";", connectionString, GetRandomApplicationName());

                    // Tell DataStressConnection to call SqlConnection.ClearPool when closing the connection. This ensures
                    // we do not keep a large number of connections in the pool that we will never use again.
                    clearPoolBeforeClose = true;
                }
                else
                {
                    switch (CurrentPoolingStressMode)
                    {
                        case PoolingStressMode.RandomizeConnectionStrings:
                            // Use one of the pre-generated Application Names in order to get a pooled connection with a randomized connection string
                            connectionString = string.Format("{0}; Pooling=true; Application Name=\"{1}\";", connectionString, _applicationNames[rnd.Next(_applicationNames.Count)]);
                            break;
                        default:
                            throw DataStressErrors.UnhandledCaseError(CurrentPoolingStressMode);
                    }
                }
            }

            // All options have been determined, now create
            DbConnection con = DbFactory.CreateConnection();
            con.ConnectionString = connectionString;
            return new DataStressConnection(con, clearPoolBeforeClose);
        }

        [Flags]
        public enum ConnectionStringOptions
        {
            Default = 0,

            // by default, MARS is disabled
            EnableMars = 0x2,

            // by default, MultiSubnetFailover is enabled
            DisableMultiSubnetFailover = 0x8
        }

        /// <summary>
        /// Creates a new connection string.
        /// Note: if rnd is null, create minimal connection string required to connect to the target database (used during setup)
        /// Otherwise, string is randomized to enable multiple pools.
        /// </summary>
        public abstract string CreateBaseConnectionString(Random rnd, ConnectionStringOptions options);

        protected virtual int GetNumDifferentApplicationNames()
        {
            return DataStressSettings.Instance.NumberOfConnectionPools;
        }

        private string GetRandomApplicationName()
        {
            return Guid.NewGuid().ToString();
        }


        /// <summary>
        /// Returns index of a random table
        /// This will be used to narrow down memory leaks
        /// related to specific tables.
        /// </summary>
        public TableMetadata GetRandomTable(Random rnd)
        {
            return TableMetadataList[rnd.Next(TableMetadataList.Count)];
        }

        /// <summary>
        /// Returns a random command object
        /// </summary>
        public DbCommand GetCommand(Random rnd, TableMetadata table, DataStressConnection conn, bool query, bool isXml = false)
        {
            if (query)
            {
                return GetSelectCommand(rnd, table, conn, isXml);
            }
            else
            {
                // make sure arguments are correct
                DataStressErrors.Assert(!isXml, "wrong usage of GetCommand: cannot create command with FOR XML that is not query");

                int select = rnd.Next(4);
                switch (select)
                {
                    case 0:
                        return GetUpdateCommand(rnd, table, conn);
                    case 1:
                        return GetInsertCommand(rnd, table, conn);
                    case 2:
                        return GetDeleteCommand(rnd, table, conn);
                    default:
                        return GetSelectCommand(rnd, table, conn);
                }
            }
        }

        private DbCommand CreateCommand(Random rnd, DataStressConnection conn)
        {
            DbCommand cmd;
            if (conn == null)
            {
                cmd = DbFactory.CreateCommand();
            }
            else
            {
                cmd = conn.CreateCommand();
            }

            if (rnd != null)
            {
                cmd.CommandTimeout = rnd.NextBool() ? 30 : 600;
            }

            return cmd;
        }

        /// <summary>
        /// Returns a random SELECT command
        /// </summary>
        public DbCommand GetSelectCommand(Random rnd, TableMetadata tableMetadata, DataStressConnection conn, bool isXml = false)
        {
            DbCommand com = CreateCommand(rnd, conn);
            StringBuilder cmdText = new StringBuilder();
            cmdText.Append(GetSelectCommandForMultipleRows(rnd, com, tableMetadata, isXml));

            // 33% of the time, we also want to add another batch to the select command to allow for
            // multiple result sets.
            if ((!isXml) && (rnd.Next(0, 3) == 0))
            {
                cmdText.Append(";").Append(GetSelectCommandForScalarValue(com));
            }

            if ((!isXml) && ShouldModifySession(rnd))
            {
                cmdText.Append(";").Append(GetRandomSessionModificationStatement(rnd));
            }

            com.CommandText = cmdText.ToString();
            return com;
        }

        /// <summary>
        /// Returns a SELECT command that retrieves data from a table
        /// </summary>
        private string GetSelectCommandForMultipleRows(Random rnd, DbCommand com, TableMetadata inputTable, bool isXml)
        {
            int rowcount = rnd.Next(Depth);

            StringBuilder cmdText = new StringBuilder();
            cmdText.Append("SELECT TOP ");
            cmdText.Append(rowcount); //Jonfo added this to prevent table scan of 75k row tables
            cmdText.Append(" PrimaryKey");

            List<TableColumn> columns = inputTable.Columns;
            int colindex = rnd.Next(0, columns.Count);

            for (int i = 0; i <= colindex; i++)
            {
                if (columns[i].ColumnName == "PrimaryKey") continue;
                cmdText.Append(", ");
                cmdText.Append(columns[i].ColumnName);
            }

            cmdText.Append(" FROM \"");
            cmdText.Append(inputTable.TableName);
            cmdText.Append("\" WITH(NOLOCK) WHERE PrimaryKey ");

            // We randomly pick an operator from '>' or '=' to allow for randomization
            // of possible rows returned by this query. This approach *may* help 
            // in reducing the likelihood of multiple threads accessing same rows.
            // If multiple threads access same rows, there may be locking issues
            // which may be avoided because of this randomization.
            string op = rnd.NextBool() ? ">" : "=";
            cmdText.Append(op).Append(" ");

            string pName = GetParameterName("P0");
            cmdText.Append(pName);

            DbParameter param = DbFactory.CreateParameter();
            param.ParameterName = pName;
            param.Value = GetRandomPK(rnd, inputTable);
            param.DbType = DbType.Int32;
            com.Parameters.Add(param);

            return cmdText.ToString();
        }

        /// <summary>
        /// Returns a SELECT command that returns a single string parameter value.
        /// </summary>
        private string GetSelectCommandForScalarValue(DbCommand com)
        {
            string pName = GetParameterName("P1");
            StringBuilder cmdText = new StringBuilder();

            cmdText.Append("SELECT ").Append(pName);

            DbParameter param = DbFactory.CreateParameter();
            param.ParameterName = pName;
            param.Value = LargeStringParam;
            param.Size = LargeStringParam.Length;
            param.DbType = DbType.String;
            com.Parameters.Add(param);

            return cmdText.ToString();
        }

        /// <summary>
        /// Returns a random existing Primary Key value
        /// </summary>
        private int GetRandomPK(Random rnd, TableMetadata table)
        {
            using (DataStressConnection conn = CreateConnection())
            {
                conn.Open();

                // This technique to get a random row comes from http://www.4guysfromrolla.com/webtech/042606-1.shtml
                // When you set rowcount and then select into a scalar value, then the query is optimised so that
                // just the last value is selected. So if n = ROWCOUNT then the query returns the n'th row.

                int rowNumber = rnd.Next(Depth);

                DbCommand com = conn.CreateCommand();
                string cmdText = string.Format(
                    @"SET ROWCOUNT {0};
                          DECLARE @PK INT;
                          SELECT @PK = PrimaryKey FROM {1} WITH(NOLOCK)
                          SELECT @PK", rowNumber, table.TableName);

                com.CommandText = cmdText;

                object result = com.ExecuteScalarSyncOrAsync(CancellationToken.None, rnd).Result;
                if (result == DBNull.Value)
                {
                    throw DataStressErrors.TestError(string.Format("Table {0} returned DBNull for primary key", table.TableName));
                }
                else
                {
                    int primaryKey = (int)result;
                    return primaryKey;
                }
            }
        }

        private DbParameter CreateRandomParameter(Random rnd, string prefix, TableColumn column)
        {
            DbParameter param = DbFactory.CreateParameter();

            param.ParameterName = GetParameterName(prefix);

            param.Value = GetRandomData(rnd, column);

            return param;
        }

        /// <summary>
        /// Returns a random UPDATE command
        /// </summary>  
        public DbCommand GetUpdateCommand(Random rnd, TableMetadata table, DataStressConnection conn)
        {
            DbCommand com = CreateCommand(rnd, conn);

            StringBuilder cmdText = new StringBuilder();
            cmdText.Append("UPDATE \"");
            cmdText.Append(table.TableName);
            cmdText.Append("\" SET ");

            List<TableColumn> columns = table.Columns;
            int numColumns = rnd.Next(2, columns.Count);
            bool mostlyNull = rnd.NextBool(0.1); // 10% of rows have 90% chance of each column being null, in order to test nbcrow

            for (int i = 0; i < numColumns; i++)
            {
                if (columns[i].ColumnName == "PrimaryKey") continue;
                if (columns[i].ColumnName.ToUpper() == "TIMESTAMP_FLD") continue;

                if (i > 1) cmdText.Append(", ");
                cmdText.Append(columns[i].ColumnName);
                cmdText.Append(" = ");

                if (mostlyNull && rnd.NextBool(0.9))
                {
                    cmdText.Append("NULL");
                }
                else
                {
                    DbParameter param = CreateRandomParameter(rnd, string.Format("P{0}", (i + 1)), columns[i]);
                    cmdText.Append(param.ParameterName);
                    com.Parameters.Add(param);
                }
            }

            cmdText.Append(" WHERE PrimaryKey = ");
            string pName = GetParameterName("P0");
            cmdText.Append(pName);
            DbParameter keyParam = DbFactory.CreateParameter();
            keyParam.ParameterName = pName;
            keyParam.Value = GetRandomPK(rnd, table);
            com.Parameters.Add(keyParam);

            if (ShouldModifySession(rnd))
            {
                cmdText.Append(";").Append(GetRandomSessionModificationStatement(rnd));
            }

            com.CommandText = cmdText.ToString(); ;
            return com;
        }

        /// <summary>
        /// Returns a random INSERT command
        /// </summary> 
        public DbCommand GetInsertCommand(Random rnd, TableMetadata table, DataStressConnection conn)
        {
            DbCommand com = CreateCommand(rnd, conn);

            StringBuilder cmdText = new StringBuilder();
            cmdText.Append("INSERT INTO \"");
            cmdText.Append(table.TableName);
            cmdText.Append("\" (");

            StringBuilder valuesText = new StringBuilder();
            valuesText.Append(") VALUES (");

            List<TableColumn> columns = table.Columns;
            int numColumns = rnd.Next(2, columns.Count);
            bool mostlyNull = rnd.NextBool(0.1); // 10% of rows have 90% chance of each column being null, in order to test nbcrow

            for (int i = 0; i < numColumns; i++)
            {
                if (columns[i].ColumnName.ToUpper() == "PRIMARYKEY") continue;

                if (i > 1)
                {
                    cmdText.Append(", ");
                    valuesText.Append(", ");
                }

                cmdText.Append(columns[i].ColumnName);

                if (columns[i].ColumnName.ToUpper() == "TIMESTAMP_FLD")
                {
                    valuesText.Append("DEFAULT"); // Cannot insert an explicit value in a timestamp field
                }
                else if (mostlyNull && rnd.NextBool(0.9))
                {
                    valuesText.Append("NULL");
                }
                else
                {
                    DbParameter param = CreateRandomParameter(rnd, string.Format("P{0}", i + 1), columns[i]);

                    valuesText.Append(param.ParameterName);
                    com.Parameters.Add(param);
                }
            }

            // To deal databases that do not support auto-incremented columns (Oracle?)
            // if (!columns["PrimaryKey"].AutoIncrement)
            if (PrimaryKeyValueIsRequired)
            {
                DbParameter param = CreateRandomParameter(rnd, "P0", table.GetColumn("PrimaryKey"));
                cmdText.Append(", PrimaryKey");
                valuesText.Append(", ");
                valuesText.Append(param.ParameterName);
                com.Parameters.Add(param);
            }

            valuesText.Append(")");
            cmdText.Append(valuesText);

            if (ShouldModifySession(rnd))
            {
                cmdText.Append(";").Append(GetRandomSessionModificationStatement(rnd));
            }

            com.CommandText = cmdText.ToString();
            return com;
        }

        /// <summary>
        /// Returns a random DELETE command
        /// </summary>    
        public DbCommand GetDeleteCommand(Random rnd, TableMetadata table, DataStressConnection conn)
        {
            DbCommand com = CreateCommand(rnd, conn);

            StringBuilder cmdText = new StringBuilder();
            cmdText.Append("DELETE FROM \"");

            List<TableColumn> columns = table.Columns;
            string pName = GetParameterName("P0");
            cmdText.Append(table.TableName);
            cmdText.Append("\" WHERE PrimaryKey = ");
            cmdText.Append(pName);

            DbParameter param = DbFactory.CreateParameter();
            param.ParameterName = pName;
            param.Value = GetRandomPK(rnd, table);
            com.Parameters.Add(param);

            if (ShouldModifySession(rnd))
            {
                cmdText.Append(";").Append(GetRandomSessionModificationStatement(rnd));
            }

            com.CommandText = cmdText.ToString();
            return com;
        }

        public bool ShouldModifySession(Random rnd)
        {
            // 33% of the time, we want to modify the user session on the server
            return rnd.NextBool(.33);
        }

        /// <summary>
        /// Returns a random statement that will modify the session on the server.
        /// </summary>
        public string GetRandomSessionModificationStatement(Random rnd)
        {
            string sessionStmt = null;
            int select = rnd.Next(3);
            switch (select)
            {
                case 0:
                    // Create a SET CONTEXT_INFO statement using a hex string of random data 
                    StringBuilder sb = new StringBuilder("0x");
                    int count = rnd.Next(1, 129);
                    for (int i = 0; i < count; i++)
                    {
                        sb.AppendFormat("{0:x2}", (byte)rnd.Next(0, (int)(byte.MaxValue + 1)));
                    }
                    string contextInfoData = sb.ToString();
                    sessionStmt = string.Format("SET CONTEXT_INFO {0}", contextInfoData);
                    break;

                case 1:
                    // Create or drop the temp table
                    sessionStmt = string.Format("IF OBJECT_ID('tempdb..{0}') IS NULL CREATE TABLE {0}(id INT) ELSE DROP TABLE {0}", s_tempTableName);
                    break;

                default:
                    // Create a SET LANGUAGE statement 
                    sessionStmt = string.Format("SET LANGUAGE N'{0}'", s_languages[rnd.Next(s_languages.Length)]);
                    break;
            }
            return sessionStmt;
        }

        /// <summary>
        /// Returns random data
        /// </summary>
        public object GetRandomData(Random rnd, TableColumn column)
        {
            int length = column.MaxLength;
            int maxTargetLength = (length > 255 || length == -1) ? 255 : length;

            DbType dbType = GetDbType(column);
            return GetRandomData(rnd, dbType, maxTargetLength);
        }

        private DbType GetDbType(TableColumn column)
        {
            switch (column.ColumnName)
            {
                case "bit_FLD": return DbType.Boolean;
                case "tinyint_FLD": return DbType.Byte;
                case "smallint_FLD": return DbType.Int16;
                case "int_FLD": return DbType.Int32;
                case "PrimaryKey": return DbType.Int32;
                case "bigint_FLD": return DbType.Int64;
                case "real_FLD": return DbType.Single;
                case "float_FLD": return DbType.Double;
                case "smallmoney_FLD": return DbType.Decimal;
                case "money_FLD": return DbType.Decimal;
                case "decimal_FLD": return DbType.Decimal;
                case "numeric_FLD": return DbType.Decimal;
                case "datetime_FLD": return DbType.DateTime;
                case "smalldatetime_FLD": return DbType.DateTime;
                case "datetime2_FLD": return DbType.DateTime2;
                case "timestamp_FLD": return DbType.Binary;
                case "date_FLD": return DbType.Date;
                case "time_FLD": return DbType.Time;
                case "datetimeoffset_FLD": return DbType.DateTimeOffset;
                case "uniqueidentifier_FLD": return DbType.Guid;
                case "sql_variant_FLD": return DbType.Object;
                case "image_FLD": return DbType.Binary;
                case "varbinary_FLD": return DbType.Binary;
                case "binary_FLD": return DbType.Binary;
                case "char_FLD": return DbType.String;
                case "varchar_FLD": return DbType.String;
                case "text_FLD": return DbType.String;
                case "ntext_FLD": return DbType.String;
                case "nvarchar_FLD": return DbType.String;
                case "nchar_FLD": return DbType.String;
                case "nvarcharmax_FLD": return DbType.String;
                case "varbinarymax_FLD": return DbType.Binary;
                case "varcharmax_FLD": return DbType.String;
                case "xml_FLD": return DbType.Xml;
                default: throw DataStressErrors.UnhandledCaseError(column.ColumnName);
            }
        }

        protected virtual object GetRandomData(Random rnd, DbType dbType, int maxLength)
        {
            byte[] buffer;
            switch (dbType)
            {
                case DbType.Boolean:
                    return (rnd.Next(2) == 0 ? false : true);
                case DbType.Byte:
                    return rnd.Next(byte.MinValue, byte.MaxValue + 1);
                case DbType.Int16:
                    return rnd.Next(short.MinValue, short.MaxValue + 1);
                case DbType.Int32:
                    return (rnd.Next(2) == 0 ? int.MaxValue / rnd.Next(1, 3) : int.MinValue / rnd.Next(1, 3));
                case DbType.Int64:
                    return (rnd.Next(2) == 0 ? long.MaxValue / rnd.Next(1, 3) : long.MinValue / rnd.Next(1, 3));
                case DbType.Single:
                    return rnd.NextDouble() * (rnd.Next(2) == 0 ? float.MaxValue : float.MinValue);
                case DbType.Double:
                    return rnd.NextDouble() * (rnd.Next(2) == 0 ? double.MaxValue : double.MinValue);
                case DbType.Decimal:
                    return rnd.Next(short.MinValue, short.MaxValue + 1);
                case DbType.DateTime:
                case DbType.DateTime2:
                    return DateTime.Now;
                case DbType.Date:
                    return DateTime.Now.Date;
                case DbType.Time:
                    return DateTime.Now.TimeOfDay.ToString("c");
                case DbType.DateTimeOffset:
                    return DateTimeOffset.Now;
                case DbType.Guid:
                    buffer = new byte[16];
                    rnd.NextBytes(buffer);
                    return (new Guid(buffer));
                case DbType.Object:
                case DbType.Binary:
                    rnd.NextBytes(buffer = new byte[rnd.Next(1, maxLength)]);
                    return buffer;
                case DbType.String:
                case DbType.Xml:
                    string openTag = "<Data>";
                    string closeTag = "</Data>";
                    int tagLength = openTag.Length + closeTag.Length;

                    if (tagLength > maxLength)
                    {
                        // Case (1): tagLength > maxTargetLength
                        return "";
                    }
                    else
                    {
                        StringBuilder builder = new StringBuilder(maxLength);

                        builder.Append(openTag);

                        // The data is just a repeat of one character because to the managed provider
                        // it is only really the length that matters, not the content of the data
                        char characterToUse = (char)rnd.Next((int)'@', (int)'~');  // Choosing random characters in this range to avoid special 
                                                                                   // xml chars like '<' or '&'
                        int numRepeats = rnd.Next(0, maxLength - tagLength); // Case (2): tagLength == maxTargetLength
                                                                             // Case (3): tagLength < maxTargetLength <-- most common
                        builder.Append(characterToUse, numRepeats);

                        builder.Append(closeTag);

                        DataStressErrors.Assert(builder.Length <= maxLength, "Incorrect length of randomly generated string");

                        return builder.ToString();
                    }
                default:
                    throw DataStressErrors.UnhandledCaseError(dbType);
            }
        }

        #region Table information to be used by stress

        // method used to create stress tables in the database
        protected void BuildUserTables(List<TableMetadata> TableMetadataList)
        {
            string CreateTable1 =
            "CREATE TABLE stress_test_table_1 (PrimaryKey int identity(1,1) primary key, int_FLD int, smallint_FLD smallint, real_FLD real, float_FLD float, decimal_FLD decimal(28,4), " +
            "smallmoney_FLD smallmoney, bit_FLD bit, tinyint_FLD tinyint, uniqueidentifier_FLD uniqueidentifier, varbinary_FLD varbinary(756), binary_FLD binary(756), " +
            "image_FLD image, varbinarymax_FLD varbinary(max), timestamp_FLD timestamp, char_FLD char(756), text_FLD text, varcharmax_FLD varchar(max), " +
            "varchar_FLD varchar(756), nchar_FLD nchar(756), ntext_FLD ntext, nvarcharmax_FLD nvarchar(max), nvarchar_FLD nvarchar(756), datetime_FLD datetime, " +
            "smalldatetime_FLD smalldatetime);" +
            "CREATE UNIQUE INDEX stress_test_table_1 on stress_test_table_1 ( PrimaryKey );" +
            "insert into stress_test_table_1(int_FLD, smallint_FLD, real_FLD, float_FLD, decimal_FLD, " +
            "smallmoney_FLD, bit_FLD, tinyint_FLD, uniqueidentifier_FLD, varbinary_FLD, binary_FLD, " +
            "image_FLD, varbinarymax_FLD, char_FLD, text_FLD, varcharmax_FLD, " +
            "varchar_FLD, nchar_FLD, ntext_FLD, nvarcharmax_FLD, nvarchar_FLD, datetime_FLD, " +
            "smalldatetime_FLD) values ( 0, 0, 0, 0, 0, $0, 0, 0, '00000000-0000-0000-0000-000000000000', " +
            "0x00, 0x00, 0x00, 0x00, '0', '0', '0', '0', N'0', N'0', N'0', N'0', '01/11/2000 12:54:01', '01/11/2000 12:54:00' );"
            ;

            string CreateTable2 =
            "CREATE TABLE stress_test_table_2 (PrimaryKey int identity(1,1) primary key, bigint_FLD bigint, money_FLD money, numeric_FLD numeric, " +
            "time_FLD time, date_FLD date, datetimeoffset_FLD datetimeoffset, sql_variant_FLD sql_variant, " +
            "datetime2_FLD datetime2, xml_FLD xml);" +
            "CREATE UNIQUE INDEX stress_test_table_2 on stress_test_table_2 ( PrimaryKey );" +
            "insert into stress_test_table_2(bigint_FLD, money_FLD, numeric_FLD, " +
            "time_FLD, date_FLD, datetimeoffset_FLD, sql_variant_FLD, " +
            "datetime2_FLD, xml_FLD) values ( 0, $0, 0, '01/11/2015 12:54:01', '01/11/2015 12:54:01', '01/11/2000 12:54:01 -08:00', 0, '01/11/2000 12:54:01', '0' );"
            ;

            if (TableMetadataList == null)
            {
                TableMetadataList = new List<TableMetadata>();
            }

            List<TableColumn> tableColumns1 = new List<TableColumn>();
            tableColumns1.Add(new TableColumn("PrimaryKey", -1));
            tableColumns1.Add(new TableColumn("int_FLD", -1));
            tableColumns1.Add(new TableColumn("smallint_FLD", -1));
            tableColumns1.Add(new TableColumn("real_FLD", -1));
            tableColumns1.Add(new TableColumn("float_FLD", -1));
            tableColumns1.Add(new TableColumn("decimal_FLD", -1));
            tableColumns1.Add(new TableColumn("smallmoney_FLD", -1));
            tableColumns1.Add(new TableColumn("bit_FLD", -1));
            tableColumns1.Add(new TableColumn("tinyint_FLD", -1));
            tableColumns1.Add(new TableColumn("uniqueidentifier_FLD", -1));
            tableColumns1.Add(new TableColumn("varbinary_FLD", 756));
            tableColumns1.Add(new TableColumn("binary_FLD", 756));
            tableColumns1.Add(new TableColumn("image_FLD", -1));
            tableColumns1.Add(new TableColumn("varbinarymax_FLD", -1));
            tableColumns1.Add(new TableColumn("timestamp_FLD", -1));
            tableColumns1.Add(new TableColumn("char_FLD", -1));
            tableColumns1.Add(new TableColumn("text_FLD", -1));
            tableColumns1.Add(new TableColumn("varcharmax_FLD", -1));
            tableColumns1.Add(new TableColumn("varchar_FLD", 756));
            tableColumns1.Add(new TableColumn("nchar_FLD", 756));
            tableColumns1.Add(new TableColumn("ntext_FLD", -1));
            tableColumns1.Add(new TableColumn("nvarcharmax_FLD", -1));
            tableColumns1.Add(new TableColumn("nvarchar_FLD", 756));
            tableColumns1.Add(new TableColumn("datetime_FLD", -1));
            tableColumns1.Add(new TableColumn("smalldatetime_FLD", -1));
            TableMetadata tableMeta1 = new TableMetadata("stress_test_table_1", tableColumns1);
            TableMetadataList.Add(tableMeta1);

            List<TableColumn> tableColumns2 = new List<TableColumn>();
            tableColumns2.Add(new TableColumn("PrimaryKey", -1));
            tableColumns2.Add(new TableColumn("bigint_FLD", -1));
            tableColumns2.Add(new TableColumn("money_FLD", -1));
            tableColumns2.Add(new TableColumn("numeric_FLD", -1));
            tableColumns2.Add(new TableColumn("time_FLD", -1));
            tableColumns2.Add(new TableColumn("date_FLD", -1));
            tableColumns2.Add(new TableColumn("datetimeoffset_FLD", -1));
            tableColumns2.Add(new TableColumn("sql_variant_FLD", -1));
            tableColumns2.Add(new TableColumn("datetime2_FLD", -1));
            tableColumns2.Add(new TableColumn("xml_FLD", -1));
            TableMetadata tableMeta2 = new TableMetadata("stress_test_table_2", tableColumns2);
            TableMetadataList.Add(tableMeta2);

            using (DataStressConnection conn = CreateConnection(null))
            {
                conn.Open();
                using (DbCommand com = conn.CreateCommand())
                {
                    try
                    {
                        com.CommandText = CreateTable1;
                        com.ExecuteNonQuery();
                    }
                    catch (DbException de)
                    {
                        // This can be improved by doing a Drop Table if exists.
                        if (de.Message.Contains("There is already an object named \'" + tableMeta1.TableName + "\' in the database."))
                        {
                            CleanupUserTables(tableMeta1);
                            com.ExecuteNonQuery();
                        }
                        else
                        {
                            throw de;
                        }
                    }

                    try
                    {
                        com.CommandText = CreateTable2;
                        com.ExecuteNonQuery();
                    }
                    catch (DbException de)
                    {
                        // This can be improved by doing a Drop Table if exists in the query itself.
                        if (de.Message.Contains("There is already an object named \'" + tableMeta2.TableName + "\' in the database."))
                        {
                            CleanupUserTables(tableMeta2);
                            com.ExecuteNonQuery();
                        }
                        else
                        {
                            throw de;
                        }
                    }

                    for (int i = 0; i < Depth; i++)
                    {
                        TrackedRandom randomInstance = new TrackedRandom();
                        randomInstance.Mark();

                        DbCommand comInsert1 = GetInsertCommand(randomInstance, tableMeta1, conn);
                        comInsert1.ExecuteNonQuery();

                        DbCommand comInsert2 = GetInsertCommand(randomInstance, tableMeta2, conn);
                        comInsert2.ExecuteNonQuery();
                    }
                }
            }
        }

        // method used to delete stress tables in the database
        protected void CleanupUserTables(TableMetadata tableMetadata)
        {
            string DropTable = "drop TABLE " + tableMetadata.TableName + ";";

            using (DataStressConnection conn = CreateConnection(null))
            {
                conn.Open();
                using (DbCommand com = conn.CreateCommand())
                {
                    try
                    {
                        com.CommandText = DropTable;
                        com.ExecuteNonQuery();
                    }
                    catch (Exception) { }
                }
            }
        }

        public List<TableMetadata> TableMetadataList
        {
            get;
            private set;
        }

        public class TableMetadata
        {
            private string _tableName;
            private List<TableColumn> _columns = new List<TableColumn>();

            public TableMetadata(string tbleName, List<TableColumn> cols)
            {
                _tableName = tbleName;
                _columns = cols;
            }

            public string TableName
            {
                get { return _tableName; }
            }

            public List<TableColumn> Columns
            {
                get { return _columns; }
            }

            public TableColumn GetColumn(string colName)
            {
                foreach (TableColumn column in _columns)
                {
                    if (column.ColumnName.Equals(colName))
                    {
                        return column;
                    }
                }
                return null;
            }
        }

        public class TableColumn
        {
            private string _columnName;
            private int _maxLength;

            public TableColumn(string colName, int maxLen)
            {
                _columnName = colName;
                _maxLength = maxLen;
            }

            public string ColumnName
            {
                get { return _columnName; }
            }

            public int MaxLength
            {
                get { return _maxLength; }
            }
        }

        private List<string> _applicationNames;

        /// <summary>
        /// Gets schema of all tables from the back-end database and fills
        /// the m_Tables DataSet with this schema. This DataSet is used to
        /// generate random command text for tests.
        /// </summary>
        public void InitializeSharedData(DataSource source)
        {
            Trace.WriteLine("Creating shared objects", this.ToString());

            // Initialize m_sharedDataSet
            TableMetadataList = new List<TableMetadata>();
            BuildUserTables(TableMetadataList);

            // Initialize m_applicationNames
            _applicationNames = new List<string>();
            for (int i = 0; i < GetNumDifferentApplicationNames(); i++)
            {
                _applicationNames.Add(GetRandomApplicationName());
            }

            // Initialize CurrentPoolingStressMode
            CurrentPoolingStressMode = PoolingStressMode.RandomizeConnectionStrings;


            Trace.WriteLine("Finished creating shared objects", this.ToString());
        }

        public void CleanupSharedData()
        {
            foreach (TableMetadata meta in TableMetadataList)
            {
                CleanupUserTables(meta);
            }
            TableMetadataList = null;
        }

        #endregion
    }
}