// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data
{
    public static class DataReaderExtensions
    {
        public static bool GetBoolean(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetBoolean(reader.GetOrdinal(name));
        }

        public static byte GetByte(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetByte(reader.GetOrdinal(name));
        }

        public static long GetBytes(this DbDataReader reader, string name, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetBytes(reader.GetOrdinal(name), dataOffset, buffer, bufferOffset, length);
        }

        public static char GetChar(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetChar(reader.GetOrdinal(name));
        }

        public static long GetChars(this DbDataReader reader, string name, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetChars(reader.GetOrdinal(name), dataOffset, buffer, bufferOffset, length);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static DbDataReader GetData(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetData(reader.GetOrdinal(name));
        }

        public static string GetDataTypeName(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetDataTypeName(reader.GetOrdinal(name));
        }

        public static DateTime GetDateTime(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetDateTime(reader.GetOrdinal(name));
        }

        public static decimal GetDecimal(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetDecimal(reader.GetOrdinal(name));
        }

        public static double GetDouble(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetDouble(reader.GetOrdinal(name));
        }

        public static Type GetFieldType(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetFieldType(reader.GetOrdinal(name));
        }

        public static T GetFieldValue<T>(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetFieldValue<T>(reader.GetOrdinal(name));
        }

        public static Task<T> GetFieldValueAsync<T>(this DbDataReader reader, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetFieldValueAsync<T>(reader.GetOrdinal(name), cancellationToken);
        }

        public static float GetFloat(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetFloat(reader.GetOrdinal(name));
        }

        public static Guid GetGuid(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetGuid(reader.GetOrdinal(name));
        }

        public static short GetInt16(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetInt16(reader.GetOrdinal(name));
        }

        public static int GetInt32(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetInt32(reader.GetOrdinal(name));
        }

        public static long GetInt64(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetInt64(reader.GetOrdinal(name));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Type GetProviderSpecificFieldType(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetProviderSpecificFieldType(reader.GetOrdinal(name));
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object GetProviderSpecificValue(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetProviderSpecificValue(reader.GetOrdinal(name));
        }

        public static Stream GetStream(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetStream(reader.GetOrdinal(name));
        }

        public static string GetString(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetString(reader.GetOrdinal(name));
        }

        public static TextReader GetTextReader(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetTextReader(reader.GetOrdinal(name));
        }

        public static object GetValue(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.GetValue(reader.GetOrdinal(name));
        }

        public static bool IsDBNull(this DbDataReader reader, string name)
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.IsDBNull(reader.GetOrdinal(name));
        }

        public static Task<bool> IsDBNullAsync(this DbDataReader reader, string name, CancellationToken cancellationToken = default(CancellationToken))
        {
            ArgumentNullException.ThrowIfNull(reader);

            return reader.IsDBNullAsync(reader.GetOrdinal(name), cancellationToken);
        }
    }
}
