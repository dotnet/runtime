// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace System.Data.Common
{
    public abstract class DbDataReader : MarshalByRefObject, IDataReader, IEnumerable
    {
        protected DbDataReader() : base() { }

        public abstract int Depth { get; }

        public abstract int FieldCount { get; }

        public abstract bool HasRows { get; }

        public abstract bool IsClosed { get; }

        public abstract int RecordsAffected { get; }

        public virtual int VisibleFieldCount => FieldCount;

        public abstract object this[int ordinal] { get; }

        public abstract object this[string name] { get; }

        public virtual void Close() { }

        public virtual Task CloseAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            try
            {
                Close();
                return Task.CompletedTask;
            }
            catch (Exception e)
            {
                return Task.FromException(e);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        public virtual ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public abstract string GetDataTypeName(int ordinal);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract IEnumerator GetEnumerator();

        public abstract Type GetFieldType(int ordinal);

        public abstract string GetName(int ordinal);

        public abstract int GetOrdinal(string name);

        public virtual DataTable GetSchemaTable()
        {
            throw new NotSupportedException();
        }

        public abstract bool GetBoolean(int ordinal);

        public abstract byte GetByte(int ordinal);

        public abstract long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length);

        public abstract char GetChar(int ordinal);

        public abstract long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public DbDataReader GetData(int ordinal) => GetDbDataReader(ordinal);

        IDataReader IDataRecord.GetData(int ordinal) => GetDbDataReader(ordinal);

        protected virtual DbDataReader GetDbDataReader(int ordinal)
        {
            throw ADP.NotSupported();
        }

        public abstract DateTime GetDateTime(int ordinal);

        public abstract decimal GetDecimal(int ordinal);

        public abstract double GetDouble(int ordinal);

        public abstract float GetFloat(int ordinal);

        public abstract Guid GetGuid(int ordinal);

        public abstract short GetInt16(int ordinal);

        public abstract int GetInt32(int ordinal);

        public abstract long GetInt64(int ordinal);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual Type GetProviderSpecificFieldType(int ordinal)
        {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey.
            return GetFieldType(ordinal);
        }

        [
        EditorBrowsable(EditorBrowsableState.Never)
        ]
        public virtual object GetProviderSpecificValue(int ordinal)
        {
            // NOTE: This is virtual because not all providers may choose to support
            //       this method, since it was added in Whidbey
            return GetValue(ordinal);
        }

        [
        EditorBrowsable(EditorBrowsableState.Never)
        ]
        public virtual int GetProviderSpecificValues(object[] values) => GetValues(values);

        public abstract string GetString(int ordinal);

        public virtual Stream GetStream(int ordinal)
        {
            using (MemoryStream bufferStream = new MemoryStream())
            {
                long bytesRead = 0;
                long bytesReadTotal = 0;
                byte[] buffer = new byte[4096];
                do
                {
                    bytesRead = GetBytes(ordinal, bytesReadTotal, buffer, 0, buffer.Length);
                    bufferStream.Write(buffer, 0, (int)bytesRead);
                    bytesReadTotal += bytesRead;
                }
                while (bytesRead > 0);

                return new MemoryStream(bufferStream.ToArray(), false);
            }
        }

        public virtual TextReader GetTextReader(int ordinal)
        {
            if (IsDBNull(ordinal))
            {
                return new StringReader(string.Empty);
            }
            else
            {
                return new StringReader(GetString(ordinal));
            }
        }

        public abstract object GetValue(int ordinal);

        public virtual T GetFieldValue<T>(int ordinal) => (T)GetValue(ordinal);

        public Task<T> GetFieldValueAsync<T>(int ordinal) =>
            GetFieldValueAsync<T>(ordinal, CancellationToken.None);

        public virtual Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ADP.CreatedTaskWithCancellation<T>();
            }
            else
            {
                try
                {
                    return Task.FromResult<T>(GetFieldValue<T>(ordinal));
                }
                catch (Exception e)
                {
                    return Task.FromException<T>(e);
                }
            }
        }

        public abstract int GetValues(object[] values);

        public abstract bool IsDBNull(int ordinal);

        public Task<bool> IsDBNullAsync(int ordinal) => IsDBNullAsync(ordinal, CancellationToken.None);

        public virtual Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ADP.CreatedTaskWithCancellation<bool>();
            }
            else
            {
                try
                {
                    return IsDBNull(ordinal) ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e)
                {
                    return Task.FromException<bool>(e);
                }
            }
        }

        public abstract bool NextResult();

        public abstract bool Read();

        public Task<bool> ReadAsync() => ReadAsync(CancellationToken.None);

        public virtual Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ADP.CreatedTaskWithCancellation<bool>();
            }
            else
            {
                try
                {
                    return Read() ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e)
                {
                    return Task.FromException<bool>(e);
                }
            }
        }

        public Task<bool> NextResultAsync() => NextResultAsync(CancellationToken.None);

        public virtual Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return ADP.CreatedTaskWithCancellation<bool>();
            }
            else
            {
                try
                {
                    return NextResult() ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e)
                {
                    return Task.FromException<bool>(e);
                }
            }
        }
    }
}
