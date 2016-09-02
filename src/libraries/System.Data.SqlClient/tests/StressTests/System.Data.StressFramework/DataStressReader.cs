// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stress.Data
{
    public class DataStressReader : IDisposable
    {
        #region Type method mapping

        private static Dictionary<Type, Func<DataStressReader, int, CancellationToken, Random, Task<object>>> s_sqlTypes;
        private static Dictionary<Type, Func<DataStressReader, int, CancellationToken, Random, Task<object>>> s_clrTypes;

        static DataStressReader()
        {
            InitSqlTypes();
            InitClrTypes();
        }

        private static void InitSqlTypes()
        {
            s_sqlTypes = new Dictionary<Type, Func<DataStressReader, int, CancellationToken, Random, Task<object>>>();

            s_sqlTypes.Add(typeof(SqlBinary), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlBinary>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlBoolean), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlBoolean>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlByte), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlByte>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlBytes), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlBytes>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlChars), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlChars>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlDateTime), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlDateTime>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlDecimal), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlDecimal>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlDouble), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlDouble>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlGuid), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlGuid>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlInt16), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlInt16>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlInt32), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlInt32>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlInt64), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlInt64>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlMoney), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlMoney>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlSingle), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlSingle>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlString), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlString>(ordinal, token, rnd));
            s_sqlTypes.Add(typeof(SqlXml), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<SqlXml>(ordinal, token, rnd));
        }

        private static void InitClrTypes()
        {
            s_clrTypes = new Dictionary<Type, Func<DataStressReader, int, CancellationToken, Random, Task<object>>>();

            s_clrTypes.Add(typeof(Boolean), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Boolean>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Byte), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Byte>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Int16), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Int16>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Int32), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Int32>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Int64), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Int64>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Single), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Single>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Double), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Double>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(String), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<String>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Char), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Char>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Decimal), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Decimal>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(Guid), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<Guid>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(DateTime), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<DateTime>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(TimeSpan), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<TimeSpan>(ordinal, token, rnd));
            s_clrTypes.Add(typeof(DateTimeOffset), (reader, ordinal, token, rnd) => reader.GetFieldValueSyncOrAsync<DateTimeOffset>(ordinal, token, rnd));
        }

        #endregion

        private readonly DbDataReader _reader;
        private SemaphoreSlim _closeAsyncSemaphore;

        public DataStressReader(DbDataReader internalReader)
        {
            _reader = internalReader;
        }

        public void Close()
        {
            _reader.Dispose();
        }

        public void Dispose()
        {
            _reader.Dispose();
            if (_closeAsyncSemaphore != null) _closeAsyncSemaphore.Dispose();
        }

        public Task CloseAsync()
        {
            _closeAsyncSemaphore = new SemaphoreSlim(1);
            return Task.Run(() => ExecuteWithCloseAsyncSemaphore(Close));
        }

        /// <summary>
        /// Executes the action while holding the CloseAsync Semaphore. 
        /// This MUST be used for reader.Close() and all methods that are not safe to call at the same time as reader.Close(), i.e. all sync methods.
        /// Otherwise we will see AV's.
        /// </summary>
        public void ExecuteWithCloseAsyncSemaphore(Action a)
        {
            try
            {
                if (_closeAsyncSemaphore != null) _closeAsyncSemaphore.Wait();
                a();
            }
            finally
            {
                if (_closeAsyncSemaphore != null) _closeAsyncSemaphore.Release();
            }
        }

        /// <summary>
        /// Executes the action while holding the CloseAsync Semaphore. 
        /// This MUST be used for reader.Close() and all methods that are not safe to call at the same time as reader.Close(), i.e. all sync methods.
        /// Otherwise we will see AV's.
        /// </summary>
        public T ExecuteWithCloseAsyncSemaphore<T>(Func<T> f)
        {
            try
            {
                if (_closeAsyncSemaphore != null) _closeAsyncSemaphore.Wait();
                return f();
            }
            finally
            {
                if (_closeAsyncSemaphore != null) _closeAsyncSemaphore.Release();
            }
        }

        #region SyncOrAsync methods

        public Task<bool> ReadSyncOrAsync(CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                () => ExecuteWithCloseAsyncSemaphore(() => _reader.Read()),
                () => ExecuteWithCloseAsyncSemaphore(() => _reader.ReadAsync(token)),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public Task<bool> NextResultSyncOrAsync(CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                () => ExecuteWithCloseAsyncSemaphore(() => _reader.NextResult()),
                () => ExecuteWithCloseAsyncSemaphore(() => _reader.NextResultAsync(token)),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public Task<bool> IsDBNullSyncOrAsync(int ordinal, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                () => ExecuteWithCloseAsyncSemaphore(() => _reader.IsDBNull(ordinal)),
                () => ExecuteWithCloseAsyncSemaphore(() => _reader.IsDBNullAsync(ordinal, token)),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        public Task<object> GetValueSyncOrAsync(int ordinal, CancellationToken token, Random rnd)
        {
            if (rnd.NextBool(0.3))
            {
                // Use sync-only GetValue
                return Task.FromResult(GetValue(ordinal));
            }
            else
            {
                // Use GetFieldValue or GetFieldValueAsync
                Func<DataStressReader, int, CancellationToken, Random, Task<object>> getFieldValueFunc = null;

                if (rnd.NextBool())
                {
                    // Choose provider-specific getter
                    Type sqlType = GetProviderSpecificFieldType(ordinal);
                    s_sqlTypes.TryGetValue(sqlType, out getFieldValueFunc);
                }
                else
                {
                    // Choose clr type getter
                    Type clrType = GetFieldType(ordinal);
                    s_clrTypes.TryGetValue(clrType, out getFieldValueFunc);
                }

                if (getFieldValueFunc != null)
                {
                    // Execute the type-specific func, e.g. GetFieldValue<int> or GetFieldValueAsync<int>
                    return getFieldValueFunc(this, ordinal, token, rnd);
                }
                else
                {
                    // Execute GetFieldValue<object> or GetFieldValueAsync<object> as a fallback
                    return GetFieldValueSyncOrAsync<object>(ordinal, token, rnd);
                }
            }
        }

        private Task<object> GetFieldValueSyncOrAsync<T>(int ordinal, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod<object>(
                () => ExecuteWithCloseAsyncSemaphore(() => _reader.GetFieldValue<T>(ordinal)),
                async () => ((object)(await ExecuteWithCloseAsyncSemaphore(() => _reader.GetFieldValueAsync<T>(ordinal, token)))),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }

        #endregion

        #region Sync-only methods

        public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return ExecuteWithCloseAsyncSemaphore(() => _reader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length));
        }

        public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return ExecuteWithCloseAsyncSemaphore(() => _reader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length));
        }

        public Type GetFieldType(int ordinal)
        {
            return ExecuteWithCloseAsyncSemaphore(() => _reader.GetFieldType(ordinal));
        }

        public string GetName(int ordinal)
        {
            return ExecuteWithCloseAsyncSemaphore(() => _reader.GetName(ordinal));
        }

        public Type GetProviderSpecificFieldType(int ordinal)
        {
            return ExecuteWithCloseAsyncSemaphore(() => _reader.GetProviderSpecificFieldType(ordinal));
        }


        public DataStressStream GetStream(int ordinal)
        {
            Stream s = ExecuteWithCloseAsyncSemaphore(() => _reader.GetStream(ordinal));
            return new DataStressStream(s, this);
        }

        public DataStressTextReader GetTextReader(int ordinal)
        {
            TextReader t = ExecuteWithCloseAsyncSemaphore(() => _reader.GetTextReader(ordinal));
            return new DataStressTextReader(t, this);
        }

        public DataStressXmlReader GetXmlReader(int ordinal)
        {
            XmlReader x = ExecuteWithCloseAsyncSemaphore(() => ((SqlDataReader)_reader).GetXmlReader(ordinal));
            return new DataStressXmlReader(x, this);
        }

        public object GetValue(int ordinal)
        {
            return ExecuteWithCloseAsyncSemaphore(() => _reader.GetValue(ordinal));
        }

        public int FieldCount
        {
            get { return ExecuteWithCloseAsyncSemaphore(() => _reader.FieldCount); }
        }

        #endregion
    }

    public class DataStressStream : IDisposable
    {
        private Stream _stream;
        private DataStressReader _reader;

        public DataStressStream(Stream stream, DataStressReader reader)
        {
            _stream = stream;
            _reader = reader;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public Task<int> ReadSyncOrAsync(byte[] buffer, int offset, int count, CancellationToken token, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                () => _reader.ExecuteWithCloseAsyncSemaphore(() => _stream.Read(buffer, offset, count)),
                () => _reader.ExecuteWithCloseAsyncSemaphore(() => _stream.ReadAsync(buffer, offset, count)),
                AsyncUtils.ChooseSyncAsyncMode(rnd)
                );
        }
    }

    public class DataStressTextReader : IDisposable
    {
        private TextReader _textReader;
        private DataStressReader _reader;

        public DataStressTextReader(TextReader textReader, DataStressReader reader)
        {
            _textReader = textReader;
            _reader = reader;
        }

        public void Dispose()
        {
            _textReader.Dispose();
        }

        public int Peek()
        {
            return _reader.ExecuteWithCloseAsyncSemaphore(() => _textReader.Peek());
        }

        public Task<int> ReadSyncOrAsync(char[] buffer, int index, int count, Random rnd)
        {
            return AsyncUtils.SyncOrAsyncMethod(
                () => _reader.ExecuteWithCloseAsyncSemaphore(() => _textReader.Read(buffer, index, count)),
                () => _reader.ExecuteWithCloseAsyncSemaphore(() => _textReader.ReadAsync(buffer, index, count)),
                AsyncUtils.ChooseSyncAsyncMode(rnd));
        }
    }

    public class DataStressXmlReader : IDisposable
    {
        private XmlReader _xmlReader;
        private DataStressReader _reader;

        public DataStressXmlReader(XmlReader xmlReader, DataStressReader reader)
        {
            _xmlReader = xmlReader;
            _reader = reader;
        }

        public void Dispose()
        {
            _xmlReader.Dispose();
        }

        public void Read()
        {
            _reader.ExecuteWithCloseAsyncSemaphore(() => _xmlReader.Read());
        }
    }
}
