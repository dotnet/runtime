// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.SqlTypes;

namespace Microsoft.SqlServer.Server
{
    // Central interface for getting/setting data values from/to a set of values indexed by ordinal 
    //  (record, row, array, etc)
    //  Which methods are allowed to be called depends on SmiMetaData type of data offset.
    internal abstract class SmiTypedGetterSetter : ITypedGettersV3, ITypedSettersV3
    {
        #region Read/Write
        // Are calls to Get methods allowed?
        internal abstract bool CanGet
        {
            get;
        }

        // Are calls to Set methods allowed?
        internal abstract bool CanSet
        {
            get;
        }
        #endregion

        #region Getters
        // Null test
        //      valid for all types
        public virtual bool IsDBNull(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // Check what type current sql_variant value is
        //      valid for SqlDbType.Variant
        public virtual SmiMetaData GetVariantType(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        //  valid for SqlDbType.Bit
        public virtual bool GetBoolean(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        //  valid for SqlDbType.TinyInt
        public virtual byte GetByte(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbTypes: Binary, VarBinary, Image, Udt, Xml, Char, VarChar, Text, NChar, NVarChar, NText
        //  (Character type support needed for ExecuteXmlReader handling)
        public virtual long GetBytesLength(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual int GetBytes(SmiEventSink sink, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for character types: Char, VarChar, Text, NChar, NVarChar, NText
        public virtual long GetCharsLength(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual int GetChars(SmiEventSink sink, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual string GetString(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.SmallInt
        public virtual short GetInt16(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Int
        public virtual int GetInt32(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.BigInt, SqlDbType.Money, SqlDbType.SmallMoney
        public virtual long GetInt64(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Real
        public virtual float GetSingle(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Float
        public virtual double GetDouble(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Numeric (uses SqlDecimal since Decimal cannot hold full range)
        public virtual SqlDecimal GetSqlDecimal(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for DateTime, SmallDateTime, Date, and DateTime2
        public virtual DateTime GetDateTime(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for UniqueIdentifier
        public virtual Guid GetGuid(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Time
        public virtual TimeSpan GetTimeSpan(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for DateTimeOffset
        public virtual DateTimeOffset GetDateTimeOffset(SmiEventSink sink, int ordinal)
        {
            if (!CanGet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for structured types
        //  This method called for both get and set.
        internal virtual SmiTypedGetterSetter GetTypedGetterSetter(SmiEventSink sink, int ordinal)
        {
            throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
        }

        #endregion

        #region Setters

        // Set value to null
        //  valid for all types
        public virtual void SetDBNull(SmiEventSink sink, int ordinal)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        //  valid for SqlDbType.Bit
        public virtual void SetBoolean(SmiEventSink sink, int ordinal, bool value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        //  valid for SqlDbType.TinyInt
        public virtual void SetByte(SmiEventSink sink, int ordinal, byte value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // Semantics for SetBytes are to modify existing value, not overwrite
        //  Use in combination with SetLength to ensure overwriting when necessary
        // valid for SqlDbTypes: Binary, VarBinary, Image, Udt, Xml
        //      (VarBinary assumed for variants)
        public virtual int SetBytes(SmiEventSink sink, int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual void SetBytesLength(SmiEventSink sink, int ordinal, long length)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // Semantics for SetChars are to modify existing value, not overwrite
        //  Use in combination with SetLength to ensure overwriting when necessary
        // valid for character types: Char, VarChar, Text, NChar, NVarChar, NText
        //      (NVarChar and global clr collation assumed for variants)
        public virtual int SetChars(SmiEventSink sink, int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        public virtual void SetCharsLength(SmiEventSink sink, int ordinal, long length)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for character types: Char, VarChar, Text, NChar, NVarChar, NText
        public virtual void SetString(SmiEventSink sink, int ordinal, string value, int offset, int length)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.SmallInt
        public virtual void SetInt16(SmiEventSink sink, int ordinal, short value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Int
        public virtual void SetInt32(SmiEventSink sink, int ordinal, int value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.BigInt, SqlDbType.Money, SqlDbType.SmallMoney
        public virtual void SetInt64(SmiEventSink sink, int ordinal, long value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Real
        public virtual void SetSingle(SmiEventSink sink, int ordinal, float value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Float
        public virtual void SetDouble(SmiEventSink sink, int ordinal, double value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Numeric (uses SqlDecimal since Decimal cannot hold full range)
        public virtual void SetSqlDecimal(SmiEventSink sink, int ordinal, SqlDecimal value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for DateTime, SmallDateTime, Date, and DateTime2
        public virtual void SetDateTime(SmiEventSink sink, int ordinal, DateTime value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for UniqueIdentifier
        public virtual void SetGuid(SmiEventSink sink, int ordinal, Guid value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for SqlDbType.Time
        public virtual void SetTimeSpan(SmiEventSink sink, int ordinal, TimeSpan value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        // valid for DateTimeOffset
        public virtual void SetDateTimeOffset(SmiEventSink sink, int ordinal, DateTimeOffset value)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        public virtual void SetVariantMetaData(SmiEventSink sink, int ordinal, SmiMetaData metaData)
        {
            // ******** OBSOLETING from SMI -- this should have been removed from ITypedSettersV3
            //  Intended to be removed prior to RTM.  Sub-classes need not implement

            // Implement body with throw because there are only a couple of ways to get to this code:
            //  1) Client is calling this method even though the server negotiated for V3+ and dropped support for V2-.
            //  2) Server didn't implement V2- on some interface and negotiated V2-.
            throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
        }

        // valid for multi-valued types only
        internal virtual void NewElement(SmiEventSink sink)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }

        internal virtual void EndElements(SmiEventSink sink)
        {
            if (!CanSet)
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.InvalidSmiCall);
            }
            else
            {
                throw System.Data.Common.ADP.InternalError(System.Data.Common.ADP.InternalErrorCode.UnimplementedSMIMethod);
            }
        }
        #endregion

    }
}
