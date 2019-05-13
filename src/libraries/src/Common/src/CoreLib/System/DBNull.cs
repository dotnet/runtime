// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    public sealed class DBNull : ISerializable, IConvertible
    {
        private DBNull()
        {
        }

        private DBNull(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException(SR.NotSupported_DBNullSerial);
        }

        public static readonly DBNull Value = new DBNull();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.NullUnity);
        }

        public override string ToString()
        {
            return string.Empty;
        }

        public string ToString(IFormatProvider? provider)
        {
            return string.Empty;
        }

        public TypeCode GetTypeCode()
        {
            return TypeCode.DBNull;
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            throw new InvalidCastException(SR.InvalidCast_FromDBNull);
        }

        object IConvertible.ToType(Type type, IFormatProvider? provider)
        {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}

