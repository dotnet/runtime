// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
// Void
//    This class represents a Missing Variant
////////////////////////////////////////////////////////////////////////////////
namespace System {
    
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    [System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    public sealed class DBNull : ISerializable, IConvertible {
    
        //Package private constructor
        private DBNull(){
        }

        private DBNull(SerializationInfo info, StreamingContext context) {
            throw new NotSupportedException(Environment.GetResourceString("NotSupported_DBNullSerial"));
        }
        
        public static readonly DBNull Value = new DBNull();

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.NullUnity, null, null);
        }
    
        public override String ToString() {
            return String.Empty;
        }

        public String ToString(IFormatProvider provider) {
            return String.Empty;
        }

        public TypeCode GetTypeCode() {
            return TypeCode.DBNull;
        }

        /// <internalonly/>
        bool IConvertible.ToBoolean(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        char IConvertible.ToChar(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        sbyte IConvertible.ToSByte(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        byte IConvertible.ToByte(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        short IConvertible.ToInt16(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        ushort IConvertible.ToUInt16(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        int IConvertible.ToInt32(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        uint IConvertible.ToUInt32(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        long IConvertible.ToInt64(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        ulong IConvertible.ToUInt64(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        float IConvertible.ToSingle(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        double IConvertible.ToDouble(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        decimal IConvertible.ToDecimal(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        DateTime IConvertible.ToDateTime(IFormatProvider provider) {
            throw new InvalidCastException(Environment.GetResourceString("InvalidCast_FromDBNull"));
        }

        /// <internalonly/>
        Object IConvertible.ToType(Type type, IFormatProvider provider) {
            return Convert.DefaultToType((IConvertible)this, type, provider);
        }
    }
}

