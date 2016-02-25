// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace System.Data.Common
{
    public abstract class DbDataReader :
        IDataReader,
        IDisposable,
        IEnumerable
    {
        protected DbDataReader() : base()
        {
        }

        abstract public int Depth
        {
            get;
        }

        abstract public int FieldCount
        {
            get;
        }

        abstract public bool HasRows
        {
            get;
        }

        abstract public bool IsClosed
        {
            get;
        }

        abstract public int RecordsAffected
        {
            get;
        }

        virtual public int VisibleFieldCount
        {
            get
            {
                return FieldCount;
            }
        }

        abstract public object this[int ordinal]
        {
            get;
        }

        abstract public object this[string name]
        {
            get;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        abstract public string GetDataTypeName(int ordinal);

        abstract public IEnumerator GetEnumerator();

        abstract public Type GetFieldType(int ordinal);

        abstract public string GetName(int ordinal);

        abstract public int GetOrdinal(string name);


        abstract public bool GetBoolean(int ordinal);

        abstract public byte GetByte(int ordinal);

        abstract public long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length);

        abstract public char GetChar(int ordinal);

        abstract public long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length);

        public DbDataReader GetData(int ordinal)
        {
            return GetDbDataReader(ordinal);
        }


        virtual protected DbDataReader GetDbDataReader(int ordinal)
        {
            throw ADP.NotSupported();
        }

        abstract public DateTime GetDateTime(int ordinal);

        abstract public Decimal GetDecimal(int ordinal);

        abstract public double GetDouble(int ordinal);

        abstract public float GetFloat(int ordinal);

        abstract public Guid GetGuid(int ordinal);

        abstract public Int16 GetInt16(int ordinal);

        abstract public Int32 GetInt32(int ordinal);

        abstract public Int64 GetInt64(int ordinal);

        virtual public Type GetProviderSpecificFieldType(int ordinal)
        {
            return GetFieldType(ordinal);
        }

        virtual public Object GetProviderSpecificValue(int ordinal)
        {
            return GetValue(ordinal);
        }

        virtual public int GetProviderSpecificValues(object[] values)
        {
            return GetValues(values);
        }

        abstract public String GetString(int ordinal);

        virtual public Stream GetStream(int ordinal)
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
                } while (bytesRead > 0);

                return new MemoryStream(bufferStream.ToArray(), false);
            }
        }

        virtual public TextReader GetTextReader(int ordinal)
        {
            if (IsDBNull(ordinal))
            {
                return new StringReader(String.Empty);
            }
            else
            {
                return new StringReader(GetString(ordinal));
            }
        }

        abstract public Object GetValue(int ordinal);

        virtual public T GetFieldValue<T>(int ordinal)
        {
            return (T)GetValue(ordinal);
        }

        public Task<T> GetFieldValueAsync<T>(int ordinal)
        {
            return GetFieldValueAsync<T>(ordinal, CancellationToken.None);
        }

        virtual public Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.FromCancellation<T>(cancellationToken);
            }
            else
            {
                try
                {
                    return Task.FromResult<T>(GetFieldValue<T>(ordinal));
                }
                catch (Exception e)
                {
                    return TaskHelpers.FromException<T>(e);
                }
            }
        }

        abstract public int GetValues(object[] values);

        abstract public bool IsDBNull(int ordinal);

        public Task<bool> IsDBNullAsync(int ordinal)
        {
            return IsDBNullAsync(ordinal, CancellationToken.None);
        }

        virtual public Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.FromCancellation<bool>(cancellationToken);
            }
            else
            {
                try
                {
                    return IsDBNull(ordinal) ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e)
                {
                    return TaskHelpers.FromException<bool>(e);
                }
            }
        }

        abstract public bool NextResult();

        abstract public bool Read();

        public Task<bool> ReadAsync()
        {
            return ReadAsync(CancellationToken.None);
        }

        virtual public Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.FromCancellation<bool>(cancellationToken);
            }
            else
            {
                try
                {
                    return Read() ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e)
                {
                    return TaskHelpers.FromException<bool>(e);
                }
            }
        }

        public Task<bool> NextResultAsync()
        {
            return NextResultAsync(CancellationToken.None);
        }

        virtual public Task<bool> NextResultAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelpers.FromCancellation<bool>(cancellationToken);
            }
            else
            {
                try
                {
                    return NextResult() ? ADP.TrueTask : ADP.FalseTask;
                }
                catch (Exception e)
                {
                    return TaskHelpers.FromException<bool>(e);
                }
            }
        }

        public virtual void Close()
        {
        }

        virtual public DataTable GetSchemaTable()
        {
            throw new NotSupportedException();
        }

        IDataReader IDataRecord.GetData(int ordinal)
        {
            return GetDbDataReader(ordinal);
        }
    }
}
