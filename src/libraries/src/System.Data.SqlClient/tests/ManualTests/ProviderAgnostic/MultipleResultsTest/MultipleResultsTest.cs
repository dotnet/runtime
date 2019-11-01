// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class MultipleResultsTest
    {
        private StringBuilder _globalBuilder = new StringBuilder();
        private StringBuilder _outputBuilder;
        private string[] _outputFilter;

        [ConditionalFact(typeof(DataTestUtility),nameof(DataTestUtility.AreConnStringsSetup))]
        public void TestMain()
        {
            Assert.True(RunTestCoreAndCompareWithBaseline());
        }

        private void RunTest()
        {
            MultipleErrorHandling(new SqlConnection((new SqlConnectionStringBuilder(DataTestUtility.TcpConnStr) { MultipleActiveResultSets = true }).ConnectionString));
        }

        private void MultipleErrorHandling(DbConnection connection)
        {
            try
            {
                Console.WriteLine("MultipleErrorHandling {0}", connection.GetType().Name);
                Type expectedException = null;
                if (connection is SqlConnection)
                {
                    ((SqlConnection)connection).InfoMessage += delegate (object sender, SqlInfoMessageEventArgs args)
                    {
                        Console.WriteLine("*** SQL CONNECTION INFO MESSAGE : {0} ****", args.Message);
                    };
                    expectedException = typeof(SqlException);
                }
                connection.Open();

                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText =
                        "PRINT N'0';\n" +
                        "SELECT num = 1, str = 'ABC';\n" +
                        "PRINT N'1';\n" +
                        "RAISERROR('Error 1', 15, 1);\n" +
                        "PRINT N'3';\n" +
                        "SELECT num = 2, str = 'ABC';\n" +
                        "PRINT N'4';\n" +
                        "RAISERROR('Error 2', 15, 1);\n" +
                        "PRINT N'5';\n" +
                        "SELECT num = 3, str = 'ABC';\n" +
                        "PRINT N'6';\n" +
                        "RAISERROR('Error 3', 15, 1);\n" +
                        "PRINT N'7';\n" +
                        "SELECT num = 4, str = 'ABC';\n" +
                        "PRINT N'8';\n" +
                        "RAISERROR('Error 4', 15, 1);\n" +
                        "PRINT N'9';\n" +
                        "SELECT num = 5, str = 'ABC';\n" +
                        "PRINT N'10';\n" +
                        "RAISERROR('Error 5', 15, 1);\n" +
                        "PRINT N'11';\n";

                    try
                    {
                        Console.WriteLine("**** ExecuteNonQuery *****");
                        command.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        PrintException(expectedException, e);
                    }

                    try
                    {
                        Console.WriteLine("**** ExecuteScalar ****");
                        command.ExecuteScalar();
                    }
                    catch (Exception e)
                    {
                        PrintException(expectedException, e);
                    }

                    try
                    {
                        Console.WriteLine("**** ExecuteReader ****");
                        using (DbDataReader reader = command.ExecuteReader())
                        {
                            bool moreResults = true;
                            do
                            {
                                try
                                {
                                    Console.WriteLine("NextResult");
                                    moreResults = reader.NextResult();
                                }
                                catch (Exception e)
                                {
                                    PrintException(expectedException, e);
                                }
                            } while (moreResults);
                        }
                    }
                    catch (Exception e)
                    {
                        PrintException(null, e);
                    }
                }
            }
            catch (Exception e)
            {
                PrintException(null, e);
            }
            try
            {
                connection.Dispose();
            }
            catch (Exception e)
            {
                PrintException(null, e);
            }
        }

        private bool RunTestCoreAndCompareWithBaseline()
        {
            string outputPath = "MultipleResultsTest.out";
            string baselinePath = "MultipleResultsTest.bsl";

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

        private void PrintException(Type expected, Exception e, params string[] values)
        {
            try
            {
                Debug.Assert(null != e, "PrintException: null exception");

                _globalBuilder.Length = 0;
                _globalBuilder.Append(e.GetType().Name).Append(": ");

                if (e is COMException)
                {
                    _globalBuilder.Append("0x").Append((((COMException)e).HResult).ToString("X8"));
                    if (expected != e.GetType())
                    {
                        _globalBuilder.Append(": ").Append(e.ToString());
                    }
                }
                else
                {
                    _globalBuilder.Append(e.Message);
                }
                AssemblyFilter(_globalBuilder);
                Console.WriteLine(_globalBuilder.ToString());

                if (expected != e.GetType())
                {
                    Console.WriteLine(e.StackTrace);
                }
                if (null != values)
                {
                    foreach (string value in values)
                    {
                        Console.WriteLine(value);
                    }
                }
                if (null != e.InnerException)
                {
                    PrintException(e.InnerException.GetType(), e.InnerException);
                }
                Console.Out.Flush();
            }
            catch (Exception f)
            {
                Console.WriteLine(f);
            }
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
            if (startIndex < 0) startIndex = 0;

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

        private string AssemblyFilter(StreamWriter writer)
        {
            if (null == _outputBuilder)
            {
                _outputBuilder = new StringBuilder();
            }
            _outputBuilder.Length = 0;

            byte[] utf8 = ((MemoryStream)writer.BaseStream).ToArray();
            string value = System.Text.Encoding.UTF8.GetString(utf8, 3, utf8.Length - 3); // skip 0xEF, 0xBB, 0xBF
            _outputBuilder.Append(value);
            AssemblyFilter(_outputBuilder);
            return _outputBuilder.ToString();
        }

        private void AssemblyFilter(StringBuilder builder)
        {
            string[] filter = _outputFilter;
            if (null == filter)
            {
                filter = new string[5];
                string tmp = typeof(System.Guid).AssemblyQualifiedName;
                filter[0] = tmp.Substring(tmp.IndexOf(','));
                filter[1] = filter[0].Replace("mscorlib", "System");
                filter[2] = filter[0].Replace("mscorlib", "System.Data");
                filter[3] = filter[0].Replace("mscorlib", "System.Data.OracleClient");
                filter[4] = filter[0].Replace("mscorlib", "System.Xml");
                _outputFilter = filter;
            }

            for (int i = 0; i < filter.Length; ++i)
            {
                builder.Replace(filter[i], "");
            }
        }

        /// <summary>
        ///  special wrapper for the text writer to replace single "\n" with "\n"
        /// </summary>
        private sealed class CarriageReturnLineFeedReplacer : TextWriter
        {
            private TextWriter _output;
            private int _lineFeedCount;
            private bool _hasCarriageReturn;

            internal CarriageReturnLineFeedReplacer(TextWriter output)
            {
                if (output == null)
                    throw new ArgumentNullException(nameof(output));

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
    }
}
