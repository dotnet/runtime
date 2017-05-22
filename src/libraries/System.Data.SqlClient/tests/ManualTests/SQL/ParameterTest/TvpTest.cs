// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Transactions;

using Microsoft.SqlServer.Server;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class TvpTest
    {
        private const string TvpName = "@tvp";
        private static readonly IList<SteAttributeKey> BoundariesTestKeys = new List<SteAttributeKey>(
                new SteAttributeKey[] {
                    SteAttributeKey.SqlDbType,
                    SteAttributeKey.MultiValued,
                    SteAttributeKey.MaxLength,
                    SteAttributeKey.Precision,
                    SteAttributeKey.Scale,
                    SteAttributeKey.LocaleId,
                    SteAttributeKey.CompareOptions,
                    SteAttributeKey.TypeName,
                    SteAttributeKey.Type,
                    SteAttributeKey.Fields,
                    SteAttributeKey.Value
                }).AsReadOnly();

        // data value and server consts
        private string _connStr;

        [CheckConnStrSetupFact]
        public void TestMain()
        {
            Assert.True(RunTestCoreAndCompareWithBaseline());
        }

        public TvpTest()
        {
            _connStr = DataTestUtility.TcpConnStr;
        }

        private void RunTest()
        {
            Console.WriteLine("Starting test \'TvpTest\'");
            StreamInputParam.Run(_connStr);
            ColumnBoundariesTest();
            QueryHintsTest();
            SqlVariantParam.SendAllSqlTypesInsideVariant(_connStr);
            DateTimeVariantTest.TestAllDateTimeWithDataTypeAndVariant(_connStr);
            OutputParameter.Run(_connStr);
        }

        private bool RunTestCoreAndCompareWithBaseline()
        {
            string outputPath = "SqlParameterTest.out";
#if DEBUG
            string baselinePath = "SqlParameterTest_DebugMode.bsl";
#else
            string baselinePath = "SqlParameterTest_ReleaseMode.bsl";
#endif

            var fstream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
            var swriter = new StreamWriter(fstream, Encoding.UTF8);
            // Convert all string writes of '\n' to '\r\n' so output files can be 'text' not 'binary'
            var twriter = new CarriageReturnLineFeedReplacer(swriter);
            Console.SetOut(twriter); // "redirect" Console.Out

            // Run Test
            RunTest();

            Console.Out.Flush();
            Console.Out.Dispose();

            // Recover the standard output stream
            StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput());
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);

            // Compare output file
            var comparisonResult = FindDiffFromBaseline(baselinePath, outputPath);

            if (string.IsNullOrEmpty(comparisonResult))
            {
                return true;
            }

            Console.WriteLine("Test Failed!");
            Console.WriteLine("Please compare baseline : {0} with output :{1}", Path.GetFullPath(baselinePath), Path.GetFullPath(outputPath));
            Console.WriteLine("Comparison Results : ");
            Console.WriteLine(comparisonResult);
            return false;
        }

        private string FindDiffFromBaseline(string baselinePath, string outputPath)
        {
            var expectedLines = File.ReadAllLines(baselinePath);
            var outputLines = File.ReadAllLines(outputPath);

            var comparisonSb = new StringBuilder();

            // Start compare results
            var expectedLength = expectedLines.Length;
            var outputLength = outputLines.Length;
            var findDiffLength = Math.Min(expectedLength, outputLength);

            // Find diff for each lines
            for (var lineNo = 0; lineNo < findDiffLength; lineNo++)
            {
                if (!expectedLines[lineNo].Equals(outputLines[lineNo]))
                {
                    comparisonSb.AppendFormat("** DIFF at line {0} \n", lineNo);
                    comparisonSb.AppendFormat("A : {0} \n", outputLines[lineNo]);
                    comparisonSb.AppendFormat("E : {0} \n", expectedLines[lineNo]);
                }
            }

            var startIndex = findDiffLength - 1;
            if (startIndex < 0)
                startIndex = 0;

            if (findDiffLength < expectedLength)
            {
                comparisonSb.AppendFormat("** MISSING \n");
                for (var lineNo = startIndex; lineNo < expectedLength; lineNo++)
                {
                    comparisonSb.AppendFormat("{0} : {1}", lineNo, expectedLines[lineNo]);
                }
            }
            if (findDiffLength < outputLength)
            {
                comparisonSb.AppendFormat("** EXTRA \n");
                for (var lineNo = startIndex; lineNo < outputLength; lineNo++)
                {
                    comparisonSb.AppendFormat("{0} : {1}", lineNo, outputLines[lineNo]);
                }
            }

            return comparisonSb.ToString();
        }

        private sealed class CarriageReturnLineFeedReplacer : TextWriter
        {
            private TextWriter _output;
            private int _lineFeedCount;
            private bool _hasCarriageReturn;

            internal CarriageReturnLineFeedReplacer(TextWriter output)
            {
                if (output == null)
                    throw new ArgumentNullException("output");

                _output = output;
            }

            public int LineFeedCount
            {
                get { return _lineFeedCount; }
            }

            public override Encoding Encoding
            {
                get { return _output.Encoding; }
            }

            public override IFormatProvider FormatProvider
            {
                get { return _output.FormatProvider; }
            }

            public override string NewLine
            {
                get { return _output.NewLine; }
                set { _output.NewLine = value; }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    ((IDisposable)_output).Dispose();
                }
                _output = null;
            }

            public override void Flush()
            {
                _output.Flush();
            }

            public override void Write(char value)
            {
                if ('\n' == value)
                {
                    _lineFeedCount++;
                    if (!_hasCarriageReturn)
                    {   // X'\n'Y -> X'\r\n'Y
                        _output.Write('\r');
                    }
                }
                _hasCarriageReturn = '\r' == value;
                _output.Write(value);
            }
        }

#region Main test methods
        private void ColumnBoundariesTest()
        {
            IEnumerator<StePermutation> boundsMD = SteStructuredTypeBoundaries.AllColumnTypesExceptUdts.GetEnumerator(
                        BoundariesTestKeys);

            TestTVPPermutations(SteStructuredTypeBoundaries.AllColumnTypesExceptUdts, false);
            //Console.WriteLine("+++++++++++  UDT TVP tests ++++++++++++++");
            //TestTVPPermutations(SteStructuredTypeBoundaries.UdtsOnly, true);
        }

        private void TestTVPPermutations(SteStructuredTypeBoundaries bounds, bool runOnlyDataRecordTest)
        {
            IEnumerator<StePermutation> boundsMD = bounds.GetEnumerator(BoundariesTestKeys);

            object[][] baseValues = SteStructuredTypeBoundaries.GetSeparateValues(boundsMD);
            IList<DataTable> dtList = GenerateDataTables(baseValues);

            TransactionOptions opts = new TransactionOptions();
            opts.IsolationLevel = System.Transactions.IsolationLevel.ReadCommitted;

            // for each unique pattern of metadata
            int iter = 0;
            while (boundsMD.MoveNext())
            {
                Console.WriteLine("+++++++ Iteration {0} ++++++++", iter);
                StePermutation tvpPerm = boundsMD.Current;

                // Set up base command
                SqlCommand cmd;
                SqlParameter param;
                cmd = new SqlCommand(GetProcName(tvpPerm));
                cmd.CommandType = CommandType.StoredProcedure;
                param = cmd.Parameters.Add(TvpName, SqlDbType.Structured);
                param.TypeName = GetTypeName(tvpPerm);

                // set up the server
                try
                {
                    CreateServerObjects(tvpPerm);
                }
                catch (SqlException se)
                {
                    Console.WriteLine("SqlException creating objects: {0}", se.Number);
                    DropServerObjects(tvpPerm);
                    iter++;
                    continue;
                }

                // Send list of SqlDataRecords as value
                Console.WriteLine("------IEnumerable<SqlDataRecord>---------");
                try
                {
                    param.Value = CreateListOfRecords(tvpPerm, baseValues);
                    ExecuteAndVerify(cmd, tvpPerm, baseValues, null);
                }
                catch (ArgumentException ae)
                {
                    // some argument exceptions expected and should be swallowed
                    Console.WriteLine("Argument exception in value setup: {0}", ae.Message);
                }

                if (!runOnlyDataRecordTest)
                {
                    // send DbDataReader
                    Console.WriteLine("------DbDataReader---------");
                    try
                    {
                        param.Value = new TvpRestartableReader(CreateListOfRecords(tvpPerm, baseValues));
                        ExecuteAndVerify(cmd, tvpPerm, baseValues, null);
                    }
                    catch (ArgumentException ae)
                    {
                        // some argument exceptions expected and should be swallowed
                        Console.WriteLine("Argument exception in value setup: {0}", ae.Message);
                    }

                    // send datasets
                    Console.WriteLine("------DataTables---------");
                    foreach (DataTable d in dtList)
                    {
                        param.Value = d;
                        ExecuteAndVerify(cmd, tvpPerm, null, d);
                    }
                }

                // And clean up
                DropServerObjects(tvpPerm);

                iter++;
            }
        }

        public void QueryHintsTest()
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                Guid randomizer = Guid.NewGuid();
                string typeName = String.Format("dbo.[QHint_{0}]", randomizer);
                string procName = String.Format("dbo.[QHint_Proc_{0}]", randomizer);
                string createTypeSql = String.Format(
                        "CREATE TYPE {0} AS TABLE("
                            + " c1 Int DEFAULT -1,"
                            + " c2 NVarChar(40) DEFAULT N'DEFUALT',"
                            + " c3 DateTime DEFAULT '1/1/2006',"
                            + " c4 Int DEFAULT -1)",
                            typeName);
                string createProcSql = String.Format(
                        "CREATE PROC {0}(@tvp {1} READONLY) AS SELECT TOP(2) * FROM @tvp ORDER BY c1", procName, typeName);
                string dropSql = String.Format("DROP PROC {0}; DROP TYPE {1}", procName, typeName);

                try
                {
                    SqlCommand cmd = new SqlCommand(createTypeSql, conn);
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = createProcSql;
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = procName;
                    cmd.CommandType = CommandType.StoredProcedure;
                    SqlParameter param = cmd.Parameters.Add("@tvp", SqlDbType.Structured);

                    SqlMetaData[] columnMetadata;
                    List<SqlDataRecord> rows = new List<SqlDataRecord>();
                    SqlDataRecord record;

                    Console.WriteLine("------- Sort order + uniqueness #1: simple -------");
                    columnMetadata = new SqlMetaData[] {
                            new SqlMetaData("", SqlDbType.Int, false, true, SortOrder.Ascending, 0),
                            new SqlMetaData("", SqlDbType.NVarChar, 40, false, true, SortOrder.Descending, 1),
                            new SqlMetaData("", SqlDbType.DateTime, false, true, SortOrder.Ascending, 2),
                            new SqlMetaData("", SqlDbType.Int, false, true, SortOrder.Descending, 3),
                        };

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(0, "Z-value", DateTime.Parse("03/01/2000"), 5);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(1, "Y-value", DateTime.Parse("02/01/2000"), 6);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(1, "X-value", DateTime.Parse("01/01/2000"), 7);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(1, "X-value", DateTime.Parse("04/01/2000"), 8);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(1, "X-value", DateTime.Parse("04/01/2000"), 4);
                    rows.Add(record);

                    param.Value = rows;
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        WriteReader(rdr);
                    }
                    rows.Clear();

                    Console.WriteLine("------- Sort order + uniqueness #2: mixed order -------");
                    columnMetadata = new SqlMetaData[] {
                            new SqlMetaData("", SqlDbType.Int, false, true, SortOrder.Descending, 3),
                            new SqlMetaData("", SqlDbType.NVarChar, 40, false, true, SortOrder.Descending, 0),
                            new SqlMetaData("", SqlDbType.DateTime, false, true, SortOrder.Ascending, 2),
                            new SqlMetaData("", SqlDbType.Int, false, true, SortOrder.Ascending, 1),
                        };

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Z-value", DateTime.Parse("01/01/2000"), 1);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Z-value", DateTime.Parse("01/01/2000"), 2);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Y-value", DateTime.Parse("01/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Y-value", DateTime.Parse("02/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(5, "X-value", DateTime.Parse("03/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(4, "X-value", DateTime.Parse("01/01/2000"), 3);
                    rows.Add(record);

                    param.Value = rows;
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        WriteReader(rdr);
                    }
                    rows.Clear();

                    Console.WriteLine("------- default column #1: outer subset -------");
                    columnMetadata = new SqlMetaData[] {
                            new SqlMetaData("", SqlDbType.Int, true, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.NVarChar, 40, false, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.DateTime, false, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.Int, true, false, SortOrder.Unspecified, -1),
                        };

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Z-value", DateTime.Parse("01/01/2000"), 1);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Z-value", DateTime.Parse("01/01/2000"), 2);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Y-value", DateTime.Parse("01/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Y-value", DateTime.Parse("02/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(5, "X-value", DateTime.Parse("03/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(4, "X-value", DateTime.Parse("01/01/2000"), 3);
                    rows.Add(record);

                    param.Value = rows;
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        WriteReader(rdr);
                    }
                    rows.Clear();

                    Console.WriteLine("------- default column #1: middle subset -------");
                    columnMetadata = new SqlMetaData[] {
                            new SqlMetaData("", SqlDbType.Int, false, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.NVarChar, 40, true, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.DateTime, true, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.Int, false, false, SortOrder.Unspecified, -1),
                        };

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Z-value", DateTime.Parse("01/01/2000"), 1);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Z-value", DateTime.Parse("01/01/2000"), 2);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Y-value", DateTime.Parse("01/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Y-value", DateTime.Parse("02/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(5, "X-value", DateTime.Parse("03/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(4, "X-value", DateTime.Parse("01/01/2000"), 3);
                    rows.Add(record);

                    param.Value = rows;
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        WriteReader(rdr);
                    }
                    rows.Clear();

                    Console.WriteLine("------- default column #1: all -------");
                    columnMetadata = new SqlMetaData[] {
                            new SqlMetaData("", SqlDbType.Int, true, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.NVarChar, 40, true, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.DateTime, true, false, SortOrder.Unspecified, -1),
                            new SqlMetaData("", SqlDbType.Int, true, false, SortOrder.Unspecified, -1),
                        };

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Z-value", DateTime.Parse("01/01/2000"), 1);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Z-value", DateTime.Parse("01/01/2000"), 2);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Y-value", DateTime.Parse("01/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(6, "Y-value", DateTime.Parse("02/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(5, "X-value", DateTime.Parse("03/01/2000"), 3);
                    rows.Add(record);

                    record = new SqlDataRecord(columnMetadata);
                    record.SetValues(4, "X-value", DateTime.Parse("01/01/2000"), 3);
                    rows.Add(record);

                    param.Value = rows;
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        WriteReader(rdr);
                    }
                    rows.Clear();

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    SqlCommand cmd = new SqlCommand(dropSql, conn);
                    cmd.ExecuteNonQuery();
                }

            }
        }

#endregion

#region Utility Methods

        private bool AllowableDifference(string source, object result, StePermutation metadata)
        {
            object value;
            bool returnValue = false;

            // turn result into a string
            string resultStr = null;
            if (result.GetType() == typeof(string))
            {
                resultStr = (string)result;
            }
            else if (result.GetType() == typeof(char[]))
            {
                resultStr = new string((char[])result);
            }
            else if (result.GetType() == typeof(SqlChars))
            {
                resultStr = new string(((SqlChars)result).Value);
            }

            if (resultStr != null)
            {
                if (source.Equals(resultStr))
                {
                    returnValue = true;
                }
                else if (metadata.TryGetValue(SteAttributeKey.MaxLength, out value) && value != SteTypeBoundaries.s_doNotUseMarker)
                {
                    int maxLength = (int)value;

                    if (maxLength < source.Length &&
                            source.Substring(0, maxLength).Equals(resultStr))
                    {
                        returnValue = true;
                    }
                    // Check for length extension due to fixed-length type
                    else if (maxLength > source.Length &&
                                resultStr.Length == maxLength &&
                                metadata.TryGetValue(SteAttributeKey.SqlDbType, out value) &&
                                value != SteTypeBoundaries.s_doNotUseMarker &&
                                (SqlDbType.Char == ((SqlDbType)value) ||
                                  SqlDbType.NChar == ((SqlDbType)value)))
                    {
                        returnValue = true;
                    }
                }
            }

            return returnValue;
        }

        private bool AllowableDifference(byte[] source, object result, StePermutation metadata)
        {
            object value;
            bool returnValue = false;

            // turn result into byte array
            byte[] resultBytes = null;
            if (result.GetType() == typeof(byte[]))
            {
                resultBytes = (byte[])result;
            }
            else if (result.GetType() == typeof(SqlBytes))
            {
                resultBytes = ((SqlBytes)result).Value;
            }

            if (resultBytes != null)
            {
                if (source.Equals(resultBytes) || resultBytes.Length == source.Length)
                {
                    returnValue = true;
                }
                else if (metadata.TryGetValue(SteAttributeKey.MaxLength, out value) && value != SteTypeBoundaries.s_doNotUseMarker)
                {
                    int maxLength = (int)value;

                    // allowable max-length adjustments
                    if (maxLength == resultBytes.Length)
                    {  // a bit optimistic, but what the heck.
                        // truncation
                        if (maxLength <= source.Length)
                        {
                            returnValue = true;
                        }
                        // Check for length extension due to fixed-length type
                        else if (metadata.TryGetValue(SteAttributeKey.SqlDbType, out value) && value != SteTypeBoundaries.s_doNotUseMarker &&
                                (SqlDbType.Binary == ((SqlDbType)value)))
                        {
                            returnValue = true;
                        }
                    }
                }
            }

            return returnValue;
        }

        private bool AllowableDifference(SqlDecimal source, object result, StePermutation metadata)
        {
            object value;
            object value2;
            bool returnValue = false;

            // turn result into SqlDecimal
            SqlDecimal resultValue = SqlDecimal.Null;
            if (result.GetType() == typeof(SqlDecimal))
            {
                resultValue = (SqlDecimal)result;
            }
            else if (result.GetType() == typeof(Decimal))
            {
                resultValue = new SqlDecimal((Decimal)result);
            }
            else if (result.GetType() == typeof(SqlMoney))
            {
                resultValue = new SqlDecimal(((SqlMoney)result).Value);
            }

            if (!resultValue.IsNull)
            {
                if (source.Equals(resultValue))
                {
                    returnValue = true;
                }
                else if (metadata.TryGetValue(SteAttributeKey.SqlDbType, out value) &&
                        SteTypeBoundaries.s_doNotUseMarker != value &&
                        (SqlDbType.SmallMoney == (SqlDbType)value ||
                         SqlDbType.Money == (SqlDbType)value))
                {
                    // Some server conversions seem to lose the decimal places
                    // TODO: Investigate and validate that this is acceptable!
                    SqlDecimal tmp = SqlDecimal.ConvertToPrecScale(source, source.Precision, 0);
                    if (tmp.Equals(resultValue))
                    {
                        returnValue = true;
                    }
                    else
                    {
                        tmp = SqlDecimal.ConvertToPrecScale(resultValue, resultValue.Precision, 0);
                        returnValue = tmp.Equals(source);
                    }
                }
                // check if value was altered by precision/scale conversion
                else if (metadata.TryGetValue(SteAttributeKey.SqlDbType, out value) &&
                        SteTypeBoundaries.s_doNotUseMarker != value &&
                        SqlDbType.Decimal == (SqlDbType)value)
                {
                    if (metadata.TryGetValue(SteAttributeKey.Scale, out value) &&
                            metadata.TryGetValue(SteAttributeKey.Precision, out value2) &&
                            SteTypeBoundaries.s_doNotUseMarker != value &&
                            SteTypeBoundaries.s_doNotUseMarker != value2)
                    {
                        SqlDecimal tmp = SqlDecimal.ConvertToPrecScale(source, (byte)value2, (byte)value);

                        returnValue = tmp.Equals(resultValue);
                    }

                    // check if value was changed to 1 by the restartable reader
                    //   due to exceeding size limits of System.Decimal
                    if (resultValue == (SqlDecimal)1M)
                    {
                        try
                        {
                            Decimal dummy = source.Value;
                        }
                        catch (OverflowException)
                        {
                            returnValue = true;
                        }
                    }
                }
            }

            return returnValue;
        }


        private bool CompareValue(object result, object source, StePermutation metadata)
        {
            bool isMatch = false;
            if (!IsNull(source))
            {
                if (!IsNull(result))
                {
                    if (source.Equals(result) || result.Equals(source))
                    {
                        isMatch = true;
                    }
                    else
                    {
                        switch (Type.GetTypeCode(source.GetType()))
                        {
                            case TypeCode.String:
                                isMatch = AllowableDifference((string)source, result, metadata);
                                break;
                            case TypeCode.Object:
                                {
                                    if (source is char[])
                                    {
                                        source = new String((char[])source);
                                        isMatch = AllowableDifference((string)source, result, metadata);
                                    }
                                    else if (source is byte[])
                                    {
                                        isMatch = AllowableDifference((byte[])source, result, metadata);
                                    }
                                    else if (source is SqlBytes)
                                    {
                                        isMatch = AllowableDifference(((SqlBytes)source).Value, result, metadata);
                                    }
                                    else if (source is SqlChars)
                                    {
                                        source = new string(((SqlChars)source).Value);
                                        isMatch = AllowableDifference((string)source, result, metadata);
                                    }
                                    else if (source is SqlInt64 && result is Int64)
                                    {
                                        isMatch = result.Equals(((SqlInt64)source).Value);
                                    }
                                    else if (source is SqlInt32 && result is Int32)
                                    {
                                        isMatch = result.Equals(((SqlInt32)source).Value);
                                    }
                                    else if (source is SqlInt16 && result is Int16)
                                    {
                                        isMatch = result.Equals(((SqlInt16)source).Value);
                                    }
                                    else if (source is SqlSingle && result is Single)
                                    {
                                        isMatch = result.Equals(((SqlSingle)source).Value);
                                    }
                                    else if (source is SqlDouble && result is Double)
                                    {
                                        isMatch = result.Equals(((SqlDouble)source).Value);
                                    }
                                    else if (source is SqlDateTime && result is DateTime)
                                    {
                                        isMatch = result.Equals(((SqlDateTime)source).Value);
                                    }
                                    else if (source is SqlMoney)
                                    {
                                        isMatch = AllowableDifference(new SqlDecimal(((SqlMoney)source).Value), result, metadata);
                                    }
                                    else if (source is SqlDecimal)
                                    {
                                        isMatch = AllowableDifference((SqlDecimal)source, result, metadata);
                                    }
                                }
                                break;
                            case TypeCode.Decimal:
                                if (result is SqlDecimal || result is Decimal || result is SqlMoney)
                                {
                                    isMatch = AllowableDifference(new SqlDecimal((Decimal)source), result, metadata);
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            else
            {
                if (IsNull(result))
                {
                    isMatch = true;
                }
            }

            if (!isMatch)
            {
                ReportMismatch(source, result, metadata);
            }
            return isMatch;
        }

        private IList<SqlDataRecord> CreateListOfRecords(StePermutation tvpPerm, object[][] baseValues)
        {
            IList<StePermutation> fields = GetFields(tvpPerm);
            SqlMetaData[] fieldMetadata = new SqlMetaData[fields.Count];
            int i = 0;
            foreach (StePermutation perm in fields)
            {
                fieldMetadata[i] = PermToSqlMetaData(perm);
                i++;
            }

            List<SqlDataRecord> records = new List<SqlDataRecord>(baseValues.Length);
            for (int rowOrd = 0; rowOrd < baseValues.Length; rowOrd++)
            {
                object[] row = baseValues[rowOrd];
                SqlDataRecord rec = new SqlDataRecord(fieldMetadata);
                records.Add(rec); // Call SetValue *after* Add to ensure record is put in list
                for (int colOrd = 0; colOrd < row.Length; colOrd++)
                {
                    // Set value in try-catch to prevent some errors from aborting run.
                    try
                    {
                        rec.SetValue(colOrd, row[colOrd]);
                    }
                    catch (OverflowException oe)
                    {
                        Console.WriteLine("Failed Row[{0}]Col[{1}] = {2}: {3}", rowOrd, colOrd, row[colOrd], oe.Message);
                    }
                    catch (ArgumentException ae)
                    {
                        Console.WriteLine("Failed Row[{0}]Col[{1}] = {2}: {3}", rowOrd, colOrd, row[colOrd], ae.Message);
                    }
                }
            }

            return records;
        }

        private DataTable CreateNewTable(object[] row, ref Type[] lastRowTypes)
        {
            DataTable dt = new DataTable();
            for (int i = 0; i < row.Length; i++)
            {
                object value = row[i];
                Type t;
                if ((null == value || DBNull.Value == value))
                {
                    if (lastRowTypes[i] == null)
                    {
                        return null;
                    }
                    else
                    {
                        t = lastRowTypes[i];
                    }
                }
                else
                {
                    t = value.GetType();
                }

                dt.Columns.Add(new DataColumn("Col" + i + "_" + t.Name, t));
                lastRowTypes[i] = t;
            }

            return dt;
        }

        // create table type and proc that uses that type at the server
        private void CreateServerObjects(StePermutation tvpPerm)
        {

            // Create the table type tsql
            StringBuilder tsql = new StringBuilder();
            tsql.Append("CREATE TYPE ");
            tsql.Append(GetTypeName(tvpPerm));
            tsql.Append(" AS TABLE(");
            bool addSeparator = false;
            int colOrdinal = 1;
            foreach (StePermutation perm in GetFields(tvpPerm))
            {
                if (addSeparator)
                {
                    tsql.Append(", ");
                }
                else
                {
                    addSeparator = true;
                }

                // column name
                tsql.Append("column");
                tsql.Append(colOrdinal);
                tsql.Append(" ");

                // column type
                SqlDbType dbType = (SqlDbType)perm[SteAttributeKey.SqlDbType];
                switch (dbType)
                {
                    case SqlDbType.BigInt:
                        tsql.Append("Bigint");
                        break;
                    case SqlDbType.Binary:
                        tsql.Append("Binary(");
                        object maxLenObj = perm[SteAttributeKey.MaxLength];
                        int maxLen;
                        if (maxLenObj == SteTypeBoundaries.s_doNotUseMarker)
                        {
                            maxLen = 8000;
                        }
                        else
                        {
                            maxLen = (int)maxLenObj;
                        }
                        tsql.Append(maxLen);
                        tsql.Append(")");
                        break;
                    case SqlDbType.Bit:
                        tsql.Append("Bit");
                        break;
                    case SqlDbType.Char:
                        tsql.Append("Char(");
                        tsql.Append(perm[SteAttributeKey.MaxLength]);
                        tsql.Append(")");
                        break;
                    case SqlDbType.DateTime:
                        tsql.Append("DateTime");
                        break;
                    case SqlDbType.Decimal:
                        tsql.Append("Decimal(");
                        tsql.Append(perm[SteAttributeKey.Precision]);
                        tsql.Append(", ");
                        tsql.Append(perm[SteAttributeKey.Scale]);
                        tsql.Append(")");
                        break;
                    case SqlDbType.Float:
                        tsql.Append("Float");
                        break;
                    case SqlDbType.Image:
                        tsql.Append("Image");
                        break;
                    case SqlDbType.Int:
                        tsql.Append("Int");
                        break;
                    case SqlDbType.Money:
                        tsql.Append("Money");
                        break;
                    case SqlDbType.NChar:
                        tsql.Append("NChar(");
                        tsql.Append(perm[SteAttributeKey.MaxLength]);
                        tsql.Append(")");
                        break;
                    case SqlDbType.NText:
                        tsql.Append("NText");
                        break;
                    case SqlDbType.NVarChar:
                        tsql.Append("NVarChar(");
                        tsql.Append(perm[SteAttributeKey.MaxLength]);
                        tsql.Append(")");
                        break;
                    case SqlDbType.Real:
                        tsql.Append("Real");
                        break;
                    case SqlDbType.UniqueIdentifier:
                        tsql.Append("UniqueIdentifier");
                        break;
                    case SqlDbType.SmallDateTime:
                        tsql.Append("SmallDateTime");
                        break;
                    case SqlDbType.SmallInt:
                        tsql.Append("SmallInt");
                        break;
                    case SqlDbType.SmallMoney:
                        tsql.Append("SmallMoney");
                        break;
                    case SqlDbType.Text:
                        tsql.Append("Text");
                        break;
                    case SqlDbType.Timestamp:
                        tsql.Append("Timestamp");
                        break;
                    case SqlDbType.TinyInt:
                        tsql.Append("TinyInt");
                        break;
                    case SqlDbType.VarBinary:
                        tsql.Append("VarBinary(");
                        tsql.Append(perm[SteAttributeKey.MaxLength]);
                        tsql.Append(")");
                        break;
                    case SqlDbType.VarChar:
                        tsql.Append("VarChar(");
                        tsql.Append(perm[SteAttributeKey.MaxLength]);
                        tsql.Append(")");
                        break;
                    case SqlDbType.Variant:
                        tsql.Append("Variant");
                        break;
                    case SqlDbType.Xml:
                        tsql.Append("Xml");
                        break;
                    case SqlDbType.Udt:
                        string typeName = (string)perm[SteAttributeKey.TypeName];
                        tsql.Append(typeName);
                        break;
                    case SqlDbType.Structured:
                        throw new NotSupportedException("Not supported");
                }

                colOrdinal++;
            }

            tsql.Append(")");

            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                // execute it to create the type
                SqlCommand cmd = new SqlCommand(tsql.ToString(), conn);
                cmd.ExecuteNonQuery();

                // and create the proc that uses the type            
                cmd.CommandText = String.Format("CREATE PROC {0}(@tvp {1} READONLY) AS SELECT * FROM @tvp order by {2}",
                        GetProcName(tvpPerm), GetTypeName(tvpPerm), colOrdinal - 1);
                cmd.ExecuteNonQuery();
            }
        }

        private bool DoesRowMatchMetadata(object[] row, DataTable table)
        {
            bool result = true;
            if (row.Length != table.Columns.Count)
            {
                result = false;
            }
            else
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (null != row[i] && DBNull.Value != row[i] && row[i].GetType() != table.Columns[i].DataType)
                    {
                        result = false;
                    }
                }
            }
            return result;
        }

        private void DropServerObjects(StePermutation tvpPerm)
        {
            string dropText = "DROP PROC " + GetProcName(tvpPerm) + "; DROP TYPE " + GetTypeName(tvpPerm);
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(dropText, conn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException e)
                {
                    Console.WriteLine("SqlException dropping objects: {0}", e.Number);
                }
            }
        }

        private void ExecuteAndVerify(SqlCommand cmd, StePermutation tvpPerm, object[][] objValues, DataTable dtValues)
        {
            using (SqlConnection conn = new SqlConnection(_connStr))
            {
                conn.Open();
                cmd.Connection = conn;
                //                cmd.Transaction = conn.BeginTransaction();

                // and run the command
                try
                {
                    using (SqlDataReader rdr = cmd.ExecuteReader())
                    {
                        VerifyColumnBoundaries(rdr, GetFields(tvpPerm), objValues, dtValues);
                    }
                }
                catch (SqlException se)
                {
                    Console.WriteLine("SqlException: {0}", se.Message);
                }
                catch (InvalidOperationException ioe)
                {
                    Console.WriteLine("InvalidOp: {0}", ioe.Message);
                }
                catch (ArgumentException ae)
                {
                    Console.WriteLine("ArgumentException: {0}", ae.Message);
                }

                // And clean up. If an error is thrown, the connection being recycled 
                //  will roll back the transaction
                if (null != cmd.Transaction)
                {
                    //                    cmd.Transaction.Rollback();
                }
            }
        }

        private IList<DataTable> GenerateDataTables(object[][] values)
        {
            List<DataTable> dtList = new List<DataTable>();
            Type[] valueTypes = new Type[values[0].Length];
            foreach (object[] row in values)
            {
                DataTable targetTable = null;
                if (0 < dtList.Count)
                {
                    // shortcut for matching last table (most common scenario)
                    if (DoesRowMatchMetadata(row, dtList[dtList.Count - 1]))
                    {
                        targetTable = dtList[dtList.Count - 1];
                    }
                    else
                    {
                        foreach (DataTable candidate in dtList)
                        {
                            if (DoesRowMatchMetadata(row, candidate))
                            {
                                targetTable = candidate;
                                break;
                            }
                        }
                    }
                }

                if (null == targetTable)
                {
                    targetTable = CreateNewTable(row, ref valueTypes);
                    if (null != targetTable)
                    {
                        dtList.Add(targetTable);
                    }
                }

                if (null != targetTable)
                {
                    targetTable.Rows.Add(row);
                }
            }

            return dtList;
        }

        private IList<StePermutation> GetFields(StePermutation tvpPerm)
        {
            return (IList<StePermutation>)tvpPerm[SteAttributeKey.Fields];
        }

        private string GetProcName(StePermutation tvpPerm)
        {
            return "dbo.[Proc_" + (string)tvpPerm[SteAttributeKey.TypeName] + "]";
        }

        private string GetTypeName(StePermutation tvpPerm)
        {
            return "dbo.[" + (string)tvpPerm[SteAttributeKey.TypeName] + "]";
        }

        private bool IsNull(object value)
        {
            return null == value ||
                    DBNull.Value == value ||
                    (value is INullable &&
                     ((INullable)value).IsNull);
        }

        private SqlMetaData PermToSqlMetaData(StePermutation perm)
        {
            object attr;
            SqlDbType sqlDbType;
            int maxLength = 0;
            byte precision = 0;
            byte scale = 0;
            string typeName = null;
            Type type = null;
            long localeId = 0;
            SqlCompareOptions opts = SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth;
            if (perm.TryGetValue(SteAttributeKey.SqlDbType, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                sqlDbType = (SqlDbType)attr;
            }
            else
            {
                throw new InvalidOperationException("PermToSqlMetaData: No SqlDbType available!");
            }

            if (perm.TryGetValue(SteAttributeKey.MaxLength, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                maxLength = (int)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.Precision, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                precision = (byte)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.Scale, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                scale = (byte)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.LocaleId, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                localeId = (int)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.CompareOptions, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                opts = (SqlCompareOptions)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.TypeName, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                typeName = (string)attr;
            }

            if (perm.TryGetValue(SteAttributeKey.Type, out attr) && (attr != SteTypeBoundaries.s_doNotUseMarker))
            {
                type = (Type)attr;
            }

            //if (SqlDbType.Udt == sqlDbType)
            //{
            //    return new SqlMetaData("", sqlDbType, type, typeName);
            //}
            //else
            //{
            return new SqlMetaData("", sqlDbType, maxLength, precision, scale, localeId, opts, type);
            //}
        }

        private void ReportMismatch(object source, object result, StePermutation perm)
        {
            if (null == source)
            {
                source = "(null)";
            }
            if (null == result)
            {
                result = "(null)";
            }
            Console.WriteLine("Mismatch: Source = {0}, result = {1}, metadata={2}", source, result, perm.ToString());
        }

        private void VerifyColumnBoundaries(SqlDataReader rdr, IList<StePermutation> fieldMetaData, object[][] values, DataTable dt)
        {
            int rowOrd = 0;
            int matches = 0;
            while (rdr.Read())
            {
                for (int columnOrd = 0; columnOrd < rdr.FieldCount; columnOrd++)
                {
                    object value;
                    // Special case to handle decimal values that may be too large for GetValue
                    if (!rdr.IsDBNull(columnOrd) && rdr.GetFieldType(columnOrd) == typeof(Decimal))
                    {
                        value = rdr.GetSqlValue(columnOrd);
                    }
                    else
                    {
                        value = rdr.GetValue(columnOrd);
                    }
                    if (null != values)
                    {
                        if (CompareValue(value, values[rowOrd][columnOrd], fieldMetaData[columnOrd]))
                        {
                            matches++;
                        }
                        else
                        {
                            Console.WriteLine("   Row={0}, Column={1}", rowOrd, columnOrd);
                        }
                    }
                    else
                    {
                        if (CompareValue(value, dt.Rows[rowOrd][columnOrd], fieldMetaData[columnOrd]))
                        {
                            matches++;
                        }
                        else
                        {
                            Console.WriteLine("   Row={0}, Column={1}", rowOrd, columnOrd);
                        }
                    }
                }
                rowOrd++;
            }

            Console.WriteLine("Matches = {0}", matches);
        }

        private void WriteReader(SqlDataReader rdr)
        {
            int colCount = rdr.FieldCount;

            do
            {
                Console.WriteLine("-------------");
                while (rdr.Read())
                {
                    for (int i = 0; i < colCount; i++)
                    {
                        Console.Write("{0}  ", rdr.GetValue(i));
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
                Console.WriteLine("-------------");
            }
            while (rdr.NextResult());
        }

        private void DumpSqlParam(SqlParameter param)
        {
            Console.WriteLine("Parameter {0}", param.ParameterName);
            Console.WriteLine("  IsNullable: {0}", param.IsNullable);
            Console.WriteLine("  LocaleId: {0}", param.LocaleId);
            Console.WriteLine("  Offset: {0}", param.Offset);
            Console.WriteLine("  CompareInfo: {0}", param.CompareInfo);
            Console.WriteLine("  DbType: {0}", param.DbType);
            Console.WriteLine("  Direction: {0}", param.Direction);
            Console.WriteLine("  Precision: {0}", param.Precision);
            Console.WriteLine("  Scale: {0}", param.Scale);
            Console.WriteLine("  Size: {0}", param.Size);
            Console.WriteLine("  SqlDbType: {0}", param.SqlDbType);
            Console.WriteLine("  TypeName: {0}", param.TypeName);
            //Console.WriteLine("  UdtTypeName: {0}", param.UdtTypeName);
            Console.WriteLine("  XmlSchemaCollectionDatabase: {0}", param.XmlSchemaCollectionDatabase);
            Console.WriteLine("  XmlSchemaCollectionName: {0}", param.XmlSchemaCollectionName);
            Console.WriteLine("  XmlSchemaCollectionSchema: {0}", param.XmlSchemaCollectionOwningSchema);
        }


#endregion
    }

    internal class TvpRestartableReader : DbDataReader
    {
        private IList<SqlDataRecord> _sourceData;
        int _currentRow;

        internal TvpRestartableReader(IList<SqlDataRecord> source) : base()
        {
            _sourceData = source;
            Restart();
        }

        public void Restart()
        {
            _currentRow = -1;
        }

        override public int Depth
        {
            get { return 0; }
        }

        override public int FieldCount
        {
            get { return _sourceData[_currentRow].FieldCount; }
        }

        override public bool HasRows
        {
            get { return _sourceData.Count > 0; }
        }

        override public bool IsClosed
        {
            get { return false; }
        }

        override public int RecordsAffected
        {
            get { return 0; }
        }

        override public object this[int ordinal]
        {
            get { return GetValue(ordinal); }
        }

        override public object this[string name]
        {
            get { return GetValue(GetOrdinal(name)); }
        }

        override public void Close()
        {
            _currentRow = _sourceData.Count;
        }

        override public string GetDataTypeName(int ordinal)
        {
            return _sourceData[_currentRow].GetDataTypeName(ordinal);
        }

        override public IEnumerator GetEnumerator()
        {
            return _sourceData.GetEnumerator();
        }

        override public Type GetFieldType(int ordinal)
        {
            return _sourceData[_currentRow].GetFieldType(ordinal);
        }

        override public string GetName(int ordinal)
        {
            return _sourceData[_currentRow].GetName(ordinal);
        }

        override public int GetOrdinal(string name)
        {
            return _sourceData[_currentRow].GetOrdinal(name);
        }

        override public DataTable GetSchemaTable()
        {
            SqlDataRecord rec = _sourceData[0];

            DataTable schemaTable = new DataTable();
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.ColumnName, typeof(System.String)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(System.Int32)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.ColumnSize, typeof(System.Int32)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.NumericPrecision, typeof(System.Int16)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.NumericScale, typeof(System.Int16)));

            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.DataType, typeof(System.Type)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableOptionalColumn.ProviderSpecificDataType, typeof(System.Type)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.NonVersionedProviderType, typeof(System.Int32)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.ProviderType, typeof(System.Int32)));

            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.IsLong, typeof(System.Boolean)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.AllowDBNull, typeof(System.Boolean)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableOptionalColumn.IsReadOnly, typeof(System.Boolean)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableOptionalColumn.IsRowVersion, typeof(System.Boolean)));

            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.IsUnique, typeof(System.Boolean)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.IsKey, typeof(System.Boolean)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(System.Boolean)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableOptionalColumn.IsHidden, typeof(System.Boolean)));

            schemaTable.Columns.Add(new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(System.String)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(System.String)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.BaseTableName, typeof(System.String)));
            schemaTable.Columns.Add(new DataColumn(SchemaTableColumn.BaseColumnName, typeof(System.String)));

            for (int i = 0; i < rec.FieldCount; i++)
            {
                DataRow row = schemaTable.NewRow();
                SqlMetaData md = rec.GetSqlMetaData(i);
                row[SchemaTableColumn.ColumnName] = md.Name;
                row[SchemaTableColumn.ColumnOrdinal] = i;
                row[SchemaTableColumn.ColumnSize] = md.MaxLength;
                row[SchemaTableColumn.NumericPrecision] = md.Precision;
                row[SchemaTableColumn.NumericScale] = md.Scale;
                row[SchemaTableColumn.DataType] = rec.GetFieldType(i);
                row[SchemaTableOptionalColumn.ProviderSpecificDataType] = rec.GetFieldType(i);
                row[SchemaTableColumn.NonVersionedProviderType] = (int)md.SqlDbType;
                row[SchemaTableColumn.ProviderType] = (int)md.SqlDbType;
                row[SchemaTableColumn.IsLong] = md.MaxLength == SqlMetaData.Max || md.MaxLength > 8000;
                row[SchemaTableColumn.AllowDBNull] = true;
                row[SchemaTableOptionalColumn.IsReadOnly] = true;
                row[SchemaTableOptionalColumn.IsRowVersion] = md.SqlDbType == SqlDbType.Timestamp;
                row[SchemaTableColumn.IsUnique] = false;
                row[SchemaTableColumn.IsKey] = false;
                row[SchemaTableOptionalColumn.IsAutoIncrement] = false;
                row[SchemaTableOptionalColumn.IsHidden] = false;
                row[SchemaTableOptionalColumn.BaseCatalogName] = null;
                row[SchemaTableColumn.BaseSchemaName] = null;
                row[SchemaTableColumn.BaseTableName] = null;
                row[SchemaTableColumn.BaseColumnName] = md.Name;
                schemaTable.Rows.Add(row);
            }

            return schemaTable;
        }

        override public bool GetBoolean(int ordinal)
        {
            return _sourceData[_currentRow].GetBoolean(ordinal);
        }

        override public byte GetByte(int ordinal)
        {
            return _sourceData[_currentRow].GetByte(ordinal);
        }

        override public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return _sourceData[_currentRow].GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        override public char GetChar(int ordinal)
        {
            return _sourceData[_currentRow].GetChar(ordinal);
        }

        override public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return _sourceData[_currentRow].GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }

        override public DateTime GetDateTime(int ordinal)
        {
            return _sourceData[_currentRow].GetDateTime(ordinal);
        }

        override public Decimal GetDecimal(int ordinal)
        {
            // DataRecord may have illegal values for Decimal...
            Decimal result;
            try
            {
                result = _sourceData[_currentRow].GetDecimal(ordinal);
            }
            catch (OverflowException)
            {
                result = (Decimal)1;
            }
            return result;
        }

        override public double GetDouble(int ordinal)
        {
            return _sourceData[_currentRow].GetDouble(ordinal);
        }

        override public float GetFloat(int ordinal)
        {
            return _sourceData[_currentRow].GetFloat(ordinal);
        }

        override public Guid GetGuid(int ordinal)
        {
            return _sourceData[_currentRow].GetGuid(ordinal);
        }

        override public Int16 GetInt16(int ordinal)
        {
            return _sourceData[_currentRow].GetInt16(ordinal);
        }

        override public Int32 GetInt32(int ordinal)
        {
            return _sourceData[_currentRow].GetInt32(ordinal);
        }

        override public Int64 GetInt64(int ordinal)
        {
            return _sourceData[_currentRow].GetInt64(ordinal);
        }

        override public String GetString(int ordinal)
        {
            return _sourceData[_currentRow].GetString(ordinal);
        }

        override public Object GetValue(int ordinal)
        {
            return _sourceData[_currentRow].GetValue(ordinal);
        }

        override public int GetValues(object[] values)
        {
            return _sourceData[_currentRow].GetValues(values);
        }

        override public bool IsDBNull(int ordinal)
        {
            return _sourceData[_currentRow].IsDBNull(ordinal);
        }

        override public bool NextResult()
        {
            Close();
            return false;
        }

        override public bool Read()
        {
            _currentRow++;

            return _currentRow < _sourceData.Count;
        }
    }

}