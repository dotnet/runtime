// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class StreamInputParam
    {
        private static Random s_rand = new Random(9999);
        private static string s_connStr = null;
        private static bool s_useSP = false;

        internal class CustomStreamException : Exception
        {
        }

        internal class CustomStream : Stream
        {
            byte[] _data;
            bool _sync;
            int _pos = 0;
            int _errorPos = 0;

            Random r = new Random(8888);

            public CustomStream(byte[] data, bool sync, int errorPos)
            {
                _data = data;
                _sync = sync;
                _errorPos = errorPos;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return false; }
            }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            private int ReadInternal(byte[] buffer, int offset, int count)
            {
                Debug.Assert(count > 0, "Count <= 0 ");
                if (_pos == _data.Length)
                {
                    return 0;
                }
                int nRead;
                if (_data.Length == _pos - 1)
                {
                    nRead = 1;
                }
                else
                {
                    nRead = 1 + r.Next(Math.Min(count, _data.Length - _pos) - 1);
                }
                if (_errorPos >= _pos && _errorPos < _pos + nRead)
                    throw new CustomStreamException();
                Array.Copy(_data, _pos, buffer, offset, nRead);
                _pos += nRead;
                return nRead;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                Debug.Assert(_sync, "Custom stream reader: Sync read in non-sync mode");
                return ReadInternal(buffer, offset, count);
            }

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, System.Threading.CancellationToken cancellationToken)
            {
                Debug.Assert(!_sync, "Custom stream reader: Async read in sync mode");
                TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
                tcs.SetResult(ReadInternal(buffer, offset, count));
                return tcs.Task;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                throw new NotImplementedException();
            }
        }

        private static void AssertEqual(byte[] ret, byte[] val, int len)
        {
            if (len > 0)
                len = Math.Min(len, val.Length);
            else
                len = val.Length;
            Debug.Assert(ret != null && ret.Length == len, "Length not equal");
            for (int i = 0; i < len; i++)
                Debug.Assert(val[i] == ret[i], "Data not equal");
        }

        private static void TestStream(int dataLen, bool sync, bool oldTypes, int paramLen, bool addWithValue = false)
        {
            byte[] val = new byte[dataLen];
            s_rand.NextBytes(val);
            TestStreamHelper(val, new MemoryStream(val, false), sync, oldTypes, paramLen, false, addWithValue);
            Console.WriteLine("TestStream (Sync {0} DataLen {1} ParamLen {2} OLD {3} AVW {4}) is OK", sync, dataLen, paramLen, oldTypes, addWithValue);
        }

        private static void TestCustomStream(int dataLen, bool sync, bool oldTypes, int paramLen, bool error, bool addWithValue = false)
        {
            byte[] val = new byte[dataLen];
            s_rand.NextBytes(val);
            TestStreamHelper(val, new CustomStream(val, sync || oldTypes, error ? dataLen / 2 : -1), sync, oldTypes, paramLen, error, addWithValue);
            Console.WriteLine("TestCustomStream (Sync {0} DataLen {1} ParamLen {2} Error {3} OLD {4} AVW {5}) is OK", sync, dataLen, paramLen, error, oldTypes, addWithValue);
        }

        private static void TestStreamHelper(byte[] val, object stream, bool sync, bool oldTypes, int paramLen, bool expectException, bool addWithValue)
        {
            using (SqlConnection conn = new SqlConnection(s_connStr))
            {
                conn.Open();
                (new SqlCommand("create table #blobs (id INT, blob VARBINARY(MAX))", conn)).ExecuteNonQuery();
                try
                {
                    SqlCommand ins;
                    if (!s_useSP)
                    {
                        ins = new SqlCommand("insert into #blobs (id,blob) values (1,@blob)", conn);
                    }
                    else
                    {
                        new SqlCommand("create procedure #myProc (@blob varbinary(MAX)) as begin insert into #blobs values (1, @blob) end", conn).ExecuteNonQuery();
                        ins = new SqlCommand("#myProc", conn);
                        ins.CommandType = CommandType.StoredProcedure;
                    }
                    if (addWithValue)
                    {
                        ins.Parameters.AddWithValue("@blob", stream);
                    }
                    else
                    {
                        ins.Parameters.Add("@blob", oldTypes ? SqlDbType.Image : SqlDbType.VarBinary, paramLen);
                        ins.Parameters["@blob"].Direction = ParameterDirection.Input;
                        ins.Parameters["@blob"].Value = stream;
                    }
                    bool exc = false;
                    if (sync)
                    {
                        try
                        {
                            ins.ExecuteNonQuery();
                        }
                        catch (CustomStreamException)
                        {
                            exc = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            ins.ExecuteNonQueryAsync().Wait();
                        }
                        catch (AggregateException ae)
                        {
                            if (ae.InnerException is CustomStreamException)
                            {
                                exc = true;
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                    Debug.Assert(exc == expectException, "Exception!=Expectation");

                    byte[] back = (new SqlCommand("select blob from #blobs where id=1", conn)).ExecuteScalar() as byte[];
                    if (!expectException)
                    {
                        AssertEqual(back, val, paramLen);
                    }
                }
                finally
                {
                    (new SqlCommand("drop table #blobs", conn)).ExecuteNonQuery();
                    if (s_useSP)
                    {
                        (new SqlCommand("drop procedure #myProc", conn)).ExecuteNonQuery();
                    }
                }
            }
        }

        private static string _xmlstr = null;

        private static string XmlStr
        {
            get
            {
                if (_xmlstr == null)
                {
                    int N = 10000;
                    XmlDocument doc = new XmlDocument();
                    XmlNode root = doc.AppendChild(doc.CreateElement("root"));
                    for (int i = 0; i < N; i++)
                        root.AppendChild(doc.CreateElement("e" + i.ToString()));
                    _xmlstr = doc.OuterXml;
                }
                return _xmlstr;
            }
        }

        private static void TestXml2Text(bool sync, bool oldTypes, int paramLen, bool nvarchar)
        {
            TestTextWrite(XmlStr, XmlReader.Create(new StringReader(XmlStr)), sync, oldTypes, paramLen, nvarchar, false, false);
            Console.WriteLine("TestXml2Text (Sync {0} ParamLen {1} NVARCHAR {2} OLD {3}) is OK", sync, paramLen, nvarchar, oldTypes);
        }

        private static void TestTextReader(int dataLen, bool sync, bool oldTypes, int paramLen, bool nvarchar, bool addWithValue = false)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < dataLen; i++)
                sb.Append((char)('A' + s_rand.Next(20)));
            string s = sb.ToString();
            TestTextWrite(s, new StringReader(s), sync, oldTypes, paramLen, nvarchar, false, addWithValue);
            Console.WriteLine("TestTextReader (Sync {0} DataLen {1} ParamLen {2} NVARCHAR {3} OLD {4} AVW {5}) is OK", sync, dataLen, paramLen, nvarchar, oldTypes, addWithValue);
        }

        private static void TestCustomTextReader(int dataLen, bool sync, bool oldTypes, int paramLen, bool nvarchar, bool error, bool addWithValue = false)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < dataLen; i++)
                sb.Append((char)('A' + s_rand.Next(20)));
            string s = sb.ToString();
            TestTextWrite(s, new StreamReader(
                                    new CustomStream(Encoding.Unicode.GetBytes(s), sync || oldTypes, error ? dataLen : -1),
                                    Encoding.Unicode),
                                    sync, oldTypes, paramLen, nvarchar, error, addWithValue);
            Console.WriteLine("TestCustomTextReader (Sync {0} DataLen {1} ParamLen {2} NVARCHAR {3} Error {4} OLD {5} AVW {6}) is OK", sync, dataLen, paramLen, nvarchar, error, oldTypes, addWithValue);
        }

        private static void TestTextWrite(string s, object reader, bool sync, bool oldTypes, int paramLen, bool nvarchar, bool expectException, bool addWithValue)
        {
            using (SqlConnection conn = new SqlConnection(s_connStr))
            {
                conn.Open();

                (new SqlCommand(string.Format("create table #blobs (id INT, blob {0}(MAX))", nvarchar ? "NVARCHAR" : "VARCHAR"), conn)).ExecuteNonQuery();
                try
                {
                    SqlCommand ins;
                    if (!s_useSP)
                    {
                        ins = new SqlCommand("insert into #blobs (id,blob) values (1,@blob)", conn);
                    }
                    else
                    {
                        new SqlCommand(
                            string.Format("create procedure #myProc (@blob {0}(MAX)) as begin insert into #blobs values (1, @blob) end", nvarchar ? "NVARCHAR" : "VARCHAR"),
                            conn).ExecuteNonQuery();
                        ins = new SqlCommand("#myProc", conn);
                        ins.CommandType = CommandType.StoredProcedure;
                    }
                    if (addWithValue)
                    {
                        ins.Parameters.AddWithValue("@blob", reader);
                    }
                    else
                    {
                        ins.Parameters.Add("@blob", nvarchar ?
                                                       (oldTypes ? SqlDbType.NText : SqlDbType.NVarChar) :
                                                       (oldTypes ? SqlDbType.Text : SqlDbType.VarChar), paramLen);
                        ins.Parameters["@blob"].Direction = ParameterDirection.Input;
                        ins.Parameters["@blob"].Value = reader;
                    }

                    bool exc = false;
                    if (sync)
                    {
                        try
                        {
                            ins.ExecuteNonQuery();
                        }
                        catch (CustomStreamException)
                        {
                            exc = true;
                        }
                    }
                    else
                    {
                        try
                        {
                            ins.ExecuteNonQueryAsync().Wait();
                        }
                        catch (AggregateException ae)
                        {
                            if (ae.InnerException is CustomStreamException)
                            {
                                exc = true;
                            }
                            else
                            {
                                throw;
                            }
                        }
                    }
                    Debug.Assert(exc == expectException, "Exception!=Expectation");

                    string back = (new SqlCommand("select blob from #blobs where id=1", conn)).ExecuteScalar() as string;
                    if (paramLen > 0)
                    {
                        s = s.Substring(0, Math.Min(paramLen, s.Length));
                    }
                    if (!expectException)
                    {
                        Debug.Assert(back == s, "Strings are not equal");
                    }
                }
                finally
                {
                    (new SqlCommand("drop table #blobs", conn)).ExecuteNonQuery();
                    if (s_useSP)
                    {
                        (new SqlCommand("drop procedure #myProc", conn)).ExecuteNonQuery();
                    }
                }
            }
        }

        private static void TestXML(bool sync, bool lengthLimited, bool addWithValue = false)
        {

            using (SqlConnection conn = new SqlConnection(s_connStr))
            {
                conn.Open();

                (new SqlCommand("create table #blobs (id INT, blob XML)", conn)).ExecuteNonQuery();
                try
                {
                    SqlCommand ins;
                    if (!s_useSP)
                    {
                        ins = new SqlCommand("insert into #blobs (id,blob) values (1,@blob)", conn);
                    }
                    else
                    {
                        new SqlCommand("create procedure #myProc (@blob XML) as begin insert into #blobs values (1, @blob) end", conn).ExecuteNonQuery();
                        ins = new SqlCommand("#myProc", conn);
                        ins.CommandType = CommandType.StoredProcedure;
                    }

                    StringBuilder comment = new StringBuilder();
                    if (lengthLimited)
                    {
                        comment.Append("<!-- ");
                        int N = s_rand.Next(100);
                        for (int i = 0; i < N; i++)
                            comment.Append(i.ToString());
                        comment.Append("-->");
                    }
                    XmlReader reader = XmlReader.Create(new StringReader(XmlStr + comment.ToString()));

                    if (addWithValue)
                    {
                        ins.Parameters.AddWithValue("@blob", reader);
                    }
                    else
                    {
                        ins.Parameters.Add("@blob", SqlDbType.Xml, lengthLimited ? XmlStr.Length : -1);
                        ins.Parameters["@blob"].Direction = ParameterDirection.Input;
                        ins.Parameters["@blob"].Value = reader;
                    }
                    if (sync)
                    {
                        ins.ExecuteNonQuery();
                    }
                    else
                    {
                        ins.ExecuteNonQueryAsync().Wait();
                    }
                    string back = (new SqlCommand("select blob from #blobs where id=1", conn)).ExecuteScalar() as string;
                    Debug.Assert(back == XmlStr, "String!=xml");
                    if (back != XmlStr)
                    {
                        Console.WriteLine("[{0}]", back);
                        Console.WriteLine("[{0}]", XmlStr);
                    }
                }
                finally
                {
                    (new SqlCommand("drop table #blobs", conn)).ExecuteNonQuery();
                    if (s_useSP)
                    {
                        (new SqlCommand("drop procedure #myProc", conn)).ExecuteNonQuery();
                    }
                }
                Console.WriteLine("TestXml (Sync {0} LimitLength {1} ) is OK", sync, lengthLimited);
            }
        }

        private static void ImmediateCancelBin()
        {
            Console.WriteLine("Test immediate cancel for binary stream");
            CancellationTokenSource cts = new CancellationTokenSource();
            using (SqlConnection conn = new SqlConnection(s_connStr))
            {
                conn.OpenAsync().Wait();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "create table #blobs (Id int, blob varbinary(max))";
                    cmd.ExecuteNonQuery();
                    Random rand = new Random(10000);
                    int dataSize = 10000000;
                    byte[] data = new byte[dataSize];
                    rand.NextBytes(data);
                    MemoryStream ms = new MemoryStream(data, false);
                    cmd.CommandText = "insert into #blobs (Id, blob) values (1, @blob)";
                    cmd.Parameters.Add("@blob", SqlDbType.VarBinary, dataSize);
                    cmd.Parameters["@blob"].Direction = ParameterDirection.Input;
                    cmd.Parameters["@blob"].Value = ms;
                    Task t = cmd.ExecuteNonQueryAsync(cts.Token);
                    if (!t.IsCompleted)
                        cts.Cancel();
                    try
                    {
                        t.Wait();
                        Console.WriteLine("FAIL: Expected AggregateException on Task wait for Cancelled Task!");
                        Console.WriteLine("t.Status: " + t.Status);
                    }
                    catch (AggregateException ae)
                    {
                        if (ae.InnerException is InvalidOperationException)
                        {
                            Console.WriteLine("PASS: Task is cancelled");
                        }
                        else
                        {
                            throw ae.InnerException;
                        }
                    }
                    finally
                    {
                        ms.Close();
                    }
                }
            }
        }

        private static void ImmediateCancelText()
        {
            Console.WriteLine("Test immediate cancel for text stream");
            CancellationTokenSource cts = new CancellationTokenSource();
            using (SqlConnection conn = new SqlConnection(s_connStr))
            {
                conn.OpenAsync().Wait();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "create table #blobs (Id int, blob varchar(max))";
                    cmd.ExecuteNonQuery();
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < 1000000; i++)
                        sb.Append(i);
                    cmd.CommandText = "insert into #blobs (Id, blob) values (1, @blob)";
                    cmd.Parameters.Add("@blob", SqlDbType.VarChar, -1);
                    cmd.Parameters["@blob"].Direction = ParameterDirection.Input;
                    cmd.Parameters["@blob"].Value = new StringReader(sb.ToString());
                    Task t = cmd.ExecuteNonQueryAsync(cts.Token);
                    if (!t.IsCompleted)
                        cts.Cancel();
                    try
                    {
                        t.Wait();
                        Console.WriteLine("FAIL: Expected AggregateException on Task wait for Cancelled Task!");
                        Console.WriteLine("t.Status: " + t.Status);
                    }
                    catch (AggregateException ae)
                    {
                        if (ae.InnerException is InvalidOperationException)
                        {
                            Console.WriteLine("PASS: Task is cancelled");
                        }
                        else
                        {
                            throw ae.InnerException;
                        }
                    }
                }
            }
        }

        private static void ImmediateCancelXml()
        {
            Console.WriteLine("Test immediate cancel for xml stream");
            CancellationTokenSource cts = new CancellationTokenSource();
            using (SqlConnection conn = new SqlConnection(s_connStr))
            {
                conn.OpenAsync().Wait();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "create table #blobs (Id int, blob xml)";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "insert into #blobs (Id, blob) values (1, @blob)";
                    cmd.Parameters.Add("@blob", SqlDbType.Xml, -1);
                    cmd.Parameters["@blob"].Direction = ParameterDirection.Input;
                    cmd.Parameters["@blob"].Value = XmlReader.Create(new StringReader(XmlStr));
                    Task t = cmd.ExecuteNonQueryAsync(cts.Token);
                    if (!t.IsCompleted)
                        cts.Cancel();
                    try
                    {
                        t.Wait();
                        Console.WriteLine("FAIL: Expected AggregateException on Task wait for Cancelled Task!");
                        Console.WriteLine("t.Status: " + t.Status);
                    }
                    catch (AggregateException ae)
                    {
                        if (ae.InnerException is InvalidOperationException)
                        {
                            Console.WriteLine("PASS: Task is cancelled");
                        }
                        else
                        {
                            throw ae.InnerException;
                        }
                    }
                }
            }
        }

        private static void PrepareCommand()
        {
            Console.Write("Test command preparation ");
            using (SqlConnection conn = new SqlConnection(s_connStr))
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "create table #blobs (Id int, blob varbinary(max))";
                    cmd.ExecuteNonQuery();
                    Random rand = new Random(10000);
                    int dataSize = 100000;
                    byte[] data = new byte[dataSize];
                    rand.NextBytes(data);
                    MemoryStream ms = new MemoryStream(data, false);

                    cmd.CommandText = "insert into #blobs (Id, blob) values (1, @blob)";

                    cmd.Parameters.Add("@blob", SqlDbType.VarBinary, dataSize);
                    cmd.Parameters["@blob"].Direction = ParameterDirection.Input;
                    cmd.Parameters["@blob"].Value = ms;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                    conn.Close();
                    ms.Close();
                }
            }
            Console.WriteLine("PASS");
        }

        private static void CommandReuse()
        {
            foreach (var func in new Func<SqlCommand, CancellationToken, Task>[] {
                    (cmd,token) => cmd.ExecuteNonQueryAsync(token),
                    (cmd,token) => cmd.ExecuteReaderAsync(token),
                    (cmd,token) => cmd.ExecuteXmlReaderAsync(token)
            })
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                using (SqlConnection conn = new SqlConnection(s_connStr))
                {
                    conn.OpenAsync().Wait();
                    Console.WriteLine("Test reuse of command after cancel");
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = "create table #blobs (Id int, blob varbinary(max))";
                        cmd.ExecuteNonQuery();
                        Random rand = new Random(10000);
                        int dataSize = 100000;
                        byte[] binarydata = new byte[dataSize];
                        rand.NextBytes(binarydata);
                        MemoryStream ms = new MemoryStream(binarydata, false);
                        cmd.CommandText = "insert into #blobs (Id, blob) values (1, @blob)";

                        cmd.Parameters.Add("@blob", SqlDbType.VarBinary, dataSize);
                        cmd.Parameters["@blob"].Direction = ParameterDirection.Input;
                        cmd.Parameters["@blob"].Value = ms;

                        Task t = func(cmd, cts.Token);
                        if (!t.IsCompleted)
                        {
                            cts.Cancel();
                        }

                        try
                        {
                            t.Wait();
                            throw new Exception("Expected AggregateException on Task wait for Cancelled Task!");
                        }
                        catch (AggregateException ae)
                        {
                            if(!ae.InnerException.Message.Contains("Operation cancelled by user."))
                            {
                                Console.WriteLine("Unexpected exception message: " + ae.InnerException.Message);
                            }
                        }
                        finally
                        {
                            ms.Close();
                        }
                        cmd.Parameters.Clear();
                        cmd.CommandText = "select 'PASS'";
                        Console.WriteLine(cmd.ExecuteScalar());
                    }
                }
            }
        }

        internal static void Run(string connection)
        {
            s_connStr = connection;
            s_useSP = false;
            Console.WriteLine("Starting Test using AsyncDebugScope");
#if DEBUG
            do
            {
                Console.WriteLine("Using stored procedure {0}", s_useSP);
                bool oldTypes = false;
                do
                {
                    using (AsyncDebugScope debugScope = new AsyncDebugScope())
                    {
                        for (int run = 0; run < 2; run++)
                        {
                            bool sync = (run == 0);
                            if (run == 2)
                            {
                                debugScope.ForceAsyncWriteDelay = 1;
                            }

                            TestStream(100000, sync, oldTypes, -1);

                            TestStream(10000, sync, oldTypes, 9000);
                            TestStream(10000, sync, oldTypes, 1000);
                            TestStream(500, sync, oldTypes, 9000);
                            TestStream(500, sync, oldTypes, 1000);
                            TestStream(0, sync, oldTypes, -1);
                            TestStream(0, sync, oldTypes, 9000);
                            TestStream(0, sync, oldTypes, 1000);
                            TestStream(10000, sync, oldTypes, 0);

                            TestCustomStream(10000, sync, oldTypes, -1, error: false);
                            TestCustomStream(10000, sync, oldTypes, 9000, error: false);
                            TestCustomStream(10000, sync, oldTypes, 1000, error: false);
                            TestCustomStream(10000, sync, oldTypes, 0, error: false);
                            TestCustomStream(10000, sync, oldTypes, -1, error: true);

                            bool nvarchar = false;
                            do
                            {

                                TestTextReader(100000, sync, oldTypes, -1, nvarchar);

                                TestTextReader(10000, sync, oldTypes, 9000, nvarchar);
                                TestTextReader(10000, sync, oldTypes, 1000, nvarchar);
                                TestTextReader(500, sync, oldTypes, 9000, nvarchar);
                                TestTextReader(500, sync, oldTypes, 1000, nvarchar);
                                TestTextReader(0, sync, oldTypes, -1, nvarchar);
                                TestTextReader(0, sync, oldTypes, 9000, nvarchar);
                                TestTextReader(0, sync, oldTypes, 1000, nvarchar);
                                TestTextReader(10000, sync, oldTypes, 0, nvarchar);

                                TestXml2Text(sync, oldTypes, -1, nvarchar);
                                TestXml2Text(sync, oldTypes, 1000000, nvarchar);
                                TestXml2Text(sync, oldTypes, 9000, nvarchar);
                                TestXml2Text(sync, oldTypes, 1000, nvarchar);
                                TestXml2Text(sync, oldTypes, 0, nvarchar);

                                TestCustomTextReader(10000, sync, oldTypes, -1, nvarchar, error: false);
                                TestCustomTextReader(10000, sync, oldTypes, 9000, nvarchar, error: false);
                                TestCustomTextReader(10000, sync, oldTypes, 1000, nvarchar, error: false);
                                TestCustomTextReader(10000, sync, oldTypes, 0, nvarchar, error: false);
                                TestCustomTextReader(10000, sync, oldTypes, -1, nvarchar, error: true);


                                nvarchar = !nvarchar;
                            } while (nvarchar);

                            if (!oldTypes)
                            {
                                TestXML(sync, true);
                                TestXML(sync, false);

                                TestStream(100000, sync, oldTypes, -1, addWithValue: true);
                                TestStream(0, sync, oldTypes, -1, addWithValue: true);

                                TestCustomStream(10000, sync, oldTypes, -1, error: false, addWithValue: true);
                                TestCustomStream(10000, sync, oldTypes, -1, error: true, addWithValue: true);

                                TestTextReader(100000, sync, oldTypes, -1, nvarchar: true, addWithValue: true);
                                TestTextReader(0, sync, oldTypes, -1, nvarchar: true, addWithValue: true);

                                TestCustomTextReader(10000, sync, oldTypes, -1, nvarchar: true, error: false, addWithValue: true);
                                TestCustomTextReader(10000, sync, oldTypes, -1, nvarchar: true, error: true, addWithValue: true);

                                TestXML(sync, lengthLimited: false, addWithValue: true);
                            }
                        }
                    }

                    oldTypes = !oldTypes;
                } while (oldTypes);
                s_useSP = !s_useSP;
            } while (s_useSP);
#else
            Console.WriteLine("Tests using AsyncDebugScope are only supported in Debug mode!");
#endif
            Console.WriteLine("Finished Test using AsyncDebugScope");

            ImmediateCancelBin();
            ImmediateCancelText();
            ImmediateCancelXml();
            PrepareCommand();
            CommandReuse();
        }
    }
}