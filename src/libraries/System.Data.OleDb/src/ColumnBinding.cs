// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Data.OleDb
{
    internal sealed class ColumnBinding
    {
        // shared with other ColumnBindings
        private readonly OleDbDataReader _dataReader; // HCHAPTER
        private readonly RowBinding _rowbinding; // for native buffer interaction
        private readonly Bindings _bindings;

        // unique to this ColumnBinding
        private readonly OleDbParameter? _parameter; // output value
        private readonly int _parameterChangeID;
        private readonly int _offsetStatus;
        private readonly int _offsetLength;
        private readonly int _offsetValue;

        // Delegate ad hoc created 'Marshal.GetIDispatchForObject' reflection object cache
        private static Func<object, IntPtr>? s_getIDispatchForObject;

        private readonly int _ordinal;
        private readonly int _maxLen;
        private readonly short _wType;
        private readonly byte _precision;

        private readonly int _index;
        private readonly int _indexForAccessor;    // HCHAPTER
        private readonly int _indexWithinAccessor; // HCHAPTER

        private readonly bool _ifIRowsetElseIRow;

        // unique per current input value
        private int _valueBindingOffset;
        private int _valueBindingSize;
        internal StringMemHandle? _sptr;
        private GCHandle _pinnedBuffer;

        // value is cached via property getters so the original may be released
        // for Value, ValueByteArray, ValueString, ValueVariant
        private object? _value;

        internal ColumnBinding(OleDbDataReader dataReader, int index, int indexForAccessor, int indexWithinAccessor,
                                OleDbParameter? parameter, RowBinding rowbinding, Bindings bindings, tagDBBINDING binding, int offset,
                                bool ifIRowsetElseIRow)
        {
            Debug.Assert(null != rowbinding, "null rowbinding");
            Debug.Assert(null != bindings, "null bindings");
            Debug.Assert(ODB.SizeOf_tagDBBINDING <= offset, $"invalid offset {offset}");

            _dataReader = dataReader;
            _rowbinding = rowbinding;
            _bindings = bindings;
            _index = index;
            _indexForAccessor = indexForAccessor;
            _indexWithinAccessor = indexWithinAccessor;

            if (null != parameter)
            {
                _parameter = parameter;
                _parameterChangeID = parameter.ChangeID;
            }
            _offsetStatus = binding.obStatus.ToInt32() + offset;
            _offsetLength = binding.obLength.ToInt32() + offset;
            _offsetValue = binding.obValue.ToInt32() + offset;

            Debug.Assert(0 <= _offsetStatus, "negative _offsetStatus");
            Debug.Assert(0 <= _offsetLength, "negative _offsetLength");
            Debug.Assert(0 <= _offsetValue, "negative _offsetValue");

            _ordinal = binding.iOrdinal.ToInt32();
            _maxLen = binding.cbMaxLen.ToInt32();
            _wType = binding.wType;
            _precision = binding.bPrecision;

            _ifIRowsetElseIRow = ifIRowsetElseIRow;

            SetSize(Bindings.ParamSize.ToInt32());
        }

        internal Bindings Bindings
        {
            get
            {
                _bindings.CurrentIndex = IndexWithinAccessor;
                return _bindings;
            }
        }

        internal RowBinding RowBinding
        {
            get { return _rowbinding; }
        }

        internal int ColumnBindingOrdinal
        {
            get { return _ordinal; }
        }

        private int ColumnBindingMaxLen
        {
            get { return _maxLen; }
        }

        private byte ColumnBindingPrecision
        {
            get { return _precision; }
        }

        private short DbType
        {
            get { return _wType; }
        }

        private Type ExpectedType
        {
            get { return NativeDBType.FromDBType(DbType, false, false).dataType!; }
        }

        internal int Index
        {
            get { return _index; }
        }
        internal int IndexForAccessor
        {
            get { return _indexForAccessor; }
        }

        internal int IndexWithinAccessor
        {
            get { return _indexWithinAccessor; }
        }

        private int ValueBindingOffset
        { // offset within the value of where to start copying
            get { return _valueBindingOffset; }
        }

        private int ValueBindingSize
        { // maximum size of the value to copy
            get { return _valueBindingSize; }
        }

        internal int ValueOffset
        { // offset within the native buffer to put the value
            get { return _offsetValue; }
        }

        private OleDbDataReader DataReader()
        {
            Debug.Assert(null != _dataReader, "null DataReader");
            return _dataReader;
        }

        internal bool IsParameterBindingInvalid(OleDbParameter parameter)
        {
            Debug.Assert((null != _parameter) && (null != parameter), "null parameter");
            return ((_parameter.ChangeID != _parameterChangeID) || (_parameter != parameter));
        }

        internal bool IsValueNull()
        {
            return ((DBStatus.S_ISNULL == StatusValue())
                    || (((NativeDBType.VARIANT == DbType) || (NativeDBType.PROPVARIANT == DbType))
                        && (Convert.IsDBNull(ValueVariant()))));
        }

        private int LengthValue()
        {
            int length;
            if (_ifIRowsetElseIRow)
            {
                length = RowBinding.ReadIntPtr(_offsetLength).ToInt32();
            }
            else
            {
                length = Bindings.DBColumnAccess[IndexWithinAccessor].cbDataLen.ToInt32();
            }
            return Math.Max(length, 0);
        }
        private void LengthValue(int value)
        {
            Debug.Assert(0 <= value, "negative LengthValue");
            RowBinding.WriteIntPtr(_offsetLength, (IntPtr)value);
        }

        internal OleDbParameter Parameter()
        {
            Debug.Assert(null != _parameter, "null parameter");
            return _parameter;
        }

        internal void ResetValue()
        {
            _value = null;

            StringMemHandle? sptr = _sptr;
            _sptr = null;

            sptr?.Dispose();

            if (_pinnedBuffer.IsAllocated)
            {
                _pinnedBuffer.Free();
            }
        }

        internal DBStatus StatusValue()
        {
            if (_ifIRowsetElseIRow)
            {
                return (DBStatus)RowBinding.ReadInt32(_offsetStatus);
            }
            else
            {
                return (DBStatus)Bindings.DBColumnAccess[IndexWithinAccessor].dwStatus;
            }
        }
        internal void StatusValue(DBStatus value)
        {
#if DEBUG
            switch (value)
            {
                case DBStatus.S_OK:
                case DBStatus.S_ISNULL:
                case DBStatus.S_DEFAULT:
                    break;
                default:
                    Debug.Assert(false, "unexpected StatusValue");
                    break;
            }
#endif
            RowBinding.WriteInt32(_offsetStatus, (int)value);
        }

        internal void SetOffset(int offset)
        {
            if (0 > offset)
            {
                throw ADP.InvalidOffsetValue(offset);
            }
            _valueBindingOffset = Math.Max(offset, 0);
        }

        internal void SetSize(int size)
        {
            _valueBindingSize = Math.Max(size, 0);
        }

        private void SetValueDBNull()
        {
            LengthValue(0);
            StatusValue(DBStatus.S_ISNULL);
            RowBinding.WriteInt64(ValueOffset, 0); // safe because AlignDataSize forces 8 byte blocks
        }
        private void SetValueEmpty()
        {
            LengthValue(0);
            StatusValue(DBStatus.S_DEFAULT);
            RowBinding.WriteInt64(ValueOffset, 0); // safe because AlignDataSize forces 8 byte blocks
        }

        internal object Value()
        {
            object value;
            if (null == _value)
            {
                switch (StatusValue())
                {
                    case DBStatus.S_OK:
                        switch (DbType)
                        {
                            case NativeDBType.EMPTY:
                            case NativeDBType.NULL:
                                value = DBNull.Value;
                                break;
                            case NativeDBType.I2:
                                value = Value_I2(); // Int16
                                break;
                            case NativeDBType.I4:
                                value = Value_I4(); // Int32
                                break;
                            case NativeDBType.R4:
                                value = Value_R4(); // Single
                                break;
                            case NativeDBType.R8:
                                value = Value_R8(); // Double
                                break;
                            case NativeDBType.CY:
                                value = Value_CY(); // Decimal
                                break;
                            case NativeDBType.DATE:
                                value = Value_DATE(); // DateTime
                                break;
                            case NativeDBType.BSTR:
                                value = Value_BSTR(); // String
                                break;
                            case NativeDBType.IDISPATCH:
                                value = Value_IDISPATCH(); // Object
                                break;
                            case NativeDBType.ERROR:
                                value = Value_ERROR(); // Int32
                                break;
                            case NativeDBType.BOOL:
                                value = Value_BOOL(); // Boolean
                                break;
                            case NativeDBType.VARIANT:
                                value = Value_VARIANT(); // Object
                                break;
                            case NativeDBType.IUNKNOWN:
                                value = Value_IUNKNOWN(); // Object
                                break;
                            case NativeDBType.DECIMAL:
                                value = Value_DECIMAL(); // Decimal
                                break;
                            case NativeDBType.I1:
                                value = (short)Value_I1(); // SByte->Int16
                                break;
                            case NativeDBType.UI1:
                                value = Value_UI1(); // Byte
                                break;
                            case NativeDBType.UI2:
                                value = (int)Value_UI2(); // UInt16->Int32
                                break;
                            case NativeDBType.UI4:
                                value = (long)Value_UI4(); // UInt32->Int64
                                break;
                            case NativeDBType.I8:
                                value = Value_I8(); // Int64
                                break;
                            case NativeDBType.UI8:
                                value = (decimal)Value_UI8(); // UInt64->Decimal
                                break;
                            case NativeDBType.FILETIME:
                                value = Value_FILETIME(); // DateTime
                                break;
                            case NativeDBType.GUID:
                                value = Value_GUID(); // Guid
                                break;
                            case NativeDBType.BYTES:
                                value = Value_BYTES(); // Byte[]
                                break;
                            case NativeDBType.WSTR:
                                value = Value_WSTR(); // String
                                break;
                            case NativeDBType.NUMERIC:
                                value = Value_NUMERIC(); // Decimal
                                break;
                            case NativeDBType.DBDATE:
                                value = Value_DBDATE(); // DateTime
                                break;
                            case NativeDBType.DBTIME:
                                value = Value_DBTIME(); // TimeSpan
                                break;
                            case NativeDBType.DBTIMESTAMP:
                                value = Value_DBTIMESTAMP(); // DateTime
                                break;
                            case NativeDBType.PROPVARIANT:
                                value = Value_VARIANT(); // Object
                                break;
                            case NativeDBType.HCHAPTER:
                                value = Value_HCHAPTER(); // OleDbDataReader
                                break;
                            case (NativeDBType.BYREF | NativeDBType.BYTES):
                                value = Value_ByRefBYTES();
                                break;
                            case (NativeDBType.BYREF | NativeDBType.WSTR):
                                value = Value_ByRefWSTR();
                                break;
                            default:
                                throw ODB.GVtUnknown(DbType);
#if DEBUG
                            case NativeDBType.STR:
                                Debug.Assert(false, "should have bound as WSTR");
                                goto default;
                            case NativeDBType.VARNUMERIC:
                                Debug.Assert(false, "should have bound as NUMERIC");
                                goto default;
                            case NativeDBType.UDT:
                                Debug.Assert(false, "UDT binding should not have been encountered");
                                goto default;
                            case (NativeDBType.BYREF | NativeDBType.STR):
                                Debug.Assert(false, "should have bound as BYREF|WSTR");
                                goto default;
#endif
                        }
                        break;
                    case DBStatus.S_TRUNCATED:
                        switch (DbType)
                        {
                            case NativeDBType.BYTES:
                                value = Value_BYTES();
                                break;
                            case NativeDBType.WSTR:
                                value = Value_WSTR();
                                break;
                            case (NativeDBType.BYREF | NativeDBType.BYTES):
                                value = Value_ByRefBYTES();
                                break;
                            case (NativeDBType.BYREF | NativeDBType.WSTR):
                                value = Value_ByRefWSTR();
                                break;
                            default:
                                throw ODB.GVtUnknown(DbType);
#if DEBUG
                            case NativeDBType.STR:
                                Debug.Assert(false, "should have bound as WSTR");
                                goto default;
                            case (NativeDBType.BYREF | NativeDBType.STR):
                                Debug.Assert(false, "should have bound as BYREF|WSTR");
                                goto default;
#endif
                        }
                        break;
                    case DBStatus.S_ISNULL:
                    case DBStatus.S_DEFAULT:
                        value = DBNull.Value;
                        break;
                    default:
                        throw CheckTypeValueStatusValue();
                }
                _value = value;
            }
            return _value!;
        }
        internal void Value(object? value)
        {
            if (null == value)
            {
                SetValueEmpty();
            }
            else if (Convert.IsDBNull(value))
            {
                SetValueDBNull();
            }
            else
                switch (DbType)
                {
                    case NativeDBType.EMPTY:
                        SetValueEmpty();
                        break;
                    case NativeDBType.NULL: // language null - no representation, use DBNull
                        SetValueDBNull();
                        break;
                    case NativeDBType.I2:
                        Value_I2((short)value);
                        break;
                    case NativeDBType.I4:
                        Value_I4((int)value);
                        break;
                    case NativeDBType.R4:
                        Value_R4((float)value);
                        break;
                    case NativeDBType.R8:
                        Value_R8((double)value);
                        break;
                    case NativeDBType.CY:
                        Value_CY((decimal)value);
                        break;
                    case NativeDBType.DATE:
                        Value_DATE((DateTime)value);
                        break;
                    case NativeDBType.BSTR:
                        Value_BSTR((string)value);
                        break;
                    case NativeDBType.IDISPATCH:
                        Value_IDISPATCH(value);
                        break;
                    case NativeDBType.ERROR:
                        Value_ERROR((int)value);
                        break;
                    case NativeDBType.BOOL:
                        Value_BOOL((bool)value);
                        break;
                    case NativeDBType.VARIANT:
                        Value_VARIANT(value);
                        break;
                    case NativeDBType.IUNKNOWN:
                        Value_IUNKNOWN(value);
                        break;
                    case NativeDBType.DECIMAL:
                        Value_DECIMAL((decimal)value);
                        break;
                    case NativeDBType.I1:
                        if (value is short)
                        {
                            Value_I1(Convert.ToSByte((short)value, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            Value_I1((sbyte)value);
                        }
                        break;
                    case NativeDBType.UI1:
                        Value_UI1((byte)value);
                        break;
                    case NativeDBType.UI2:
                        if (value is int)
                        {
                            Value_UI2(Convert.ToUInt16((int)value, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            Value_UI2((ushort)value);
                        }
                        break;
                    case NativeDBType.UI4:
                        if (value is long)
                        {
                            Value_UI4(Convert.ToUInt32((long)value, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            Value_UI4((uint)value);
                        }
                        break;
                    case NativeDBType.I8:
                        Value_I8((long)value);
                        break;
                    case NativeDBType.UI8:
                        if (value is decimal)
                        {
                            Value_UI8(Convert.ToUInt64((decimal)value, CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            Value_UI8((ulong)value);
                        }
                        break;
                    case NativeDBType.FILETIME:
                        Value_FILETIME((DateTime)value);
                        break;
                    case NativeDBType.GUID:
                        Value_GUID((Guid)value);
                        break;
                    case NativeDBType.BYTES:
                        Value_BYTES((byte[])value);
                        break;
                    case NativeDBType.WSTR:
                        if (value is string)
                        {
                            Value_WSTR((string)value);
                        }
                        else
                        {
                            Value_WSTR((char[])value);
                        }
                        break;
                    case NativeDBType.NUMERIC:
                        Value_NUMERIC((decimal)value);
                        break;
                    case NativeDBType.DBDATE:
                        Value_DBDATE((DateTime)value);
                        break;
                    case NativeDBType.DBTIME:
                        Value_DBTIME((TimeSpan)value);
                        break;
                    case NativeDBType.DBTIMESTAMP:
                        Value_DBTIMESTAMP((DateTime)value);
                        break;
                    case NativeDBType.PROPVARIANT:
                        Value_VARIANT(value);
                        break;
                    case (NativeDBType.BYREF | NativeDBType.BYTES):
                        Value_ByRefBYTES((byte[])value);
                        break;
                    case (NativeDBType.BYREF | NativeDBType.WSTR):
                        if (value is string)
                        {
                            Value_ByRefWSTR((string)value);
                        }
                        else
                        {
                            Value_ByRefWSTR((char[])value);
                        }
                        break;
                    default:
                        Debug.Assert(false, "unknown DBTYPE");
                        throw ODB.SVtUnknown(DbType);
#if DEBUG
                    case NativeDBType.STR:
                        Debug.Assert(false, "Should have bound as WSTR");
                        goto default;
                    case NativeDBType.UDT:
                        Debug.Assert(false, "UDT binding should not have been encountered");
                        goto default;
                    case NativeDBType.HCHAPTER:
                        Debug.Assert(false, "not allowed to set HCHAPTER");
                        goto default;
                    case NativeDBType.VARNUMERIC:
                        Debug.Assert(false, "should have bound as NUMERIC");
                        goto default;
                    case (NativeDBType.BYREF | NativeDBType.STR):
                        Debug.Assert(false, "should have bound as BYREF|WSTR");
                        goto default;
#endif
                }
        }

        internal bool Value_BOOL()
        {
            Debug.Assert((NativeDBType.BOOL == DbType), "Value_BOOL");
            Debug.Assert((DBStatus.S_OK == StatusValue()), "Value_BOOL");
            short value = RowBinding.ReadInt16(ValueOffset);
            return (0 != value);
        }
        private void Value_BOOL(bool value)
        {
            Debug.Assert((NativeDBType.BOOL == DbType), "Value_BOOL");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt16(ValueOffset, (short)(value ? ODB.VARIANT_TRUE : ODB.VARIANT_FALSE));
        }

        private string Value_BSTR()
        {
            Debug.Assert((NativeDBType.BSTR == DbType), "Value_BSTR");
            Debug.Assert((DBStatus.S_OK == StatusValue()), "Value_BSTR");
            string value = "";
            RowBinding bindings = RowBinding;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                bindings.DangerousAddRef(ref mustRelease);
                IntPtr ptr = bindings.ReadIntPtr(ValueOffset);
                if (ADP.PtrZero != ptr)
                {
                    value = Marshal.PtrToStringBSTR(ptr);
                }
            }
            finally
            {
                if (mustRelease)
                {
                    bindings.DangerousRelease();
                }
            }
            return value;
        }
        private void Value_BSTR(string value)
        {
            Debug.Assert((null != value), "Value_BSTR null");
            Debug.Assert((NativeDBType.BSTR == DbType), "Value_BSTR");
            LengthValue(value.Length * 2); /* bytecount*/
            StatusValue(DBStatus.S_OK);
            RowBinding.SetBstrValue(ValueOffset, value);
        }

        private byte[] Value_ByRefBYTES()
        {
            Debug.Assert(((NativeDBType.BYREF | NativeDBType.BYTES) == DbType), "Value_ByRefBYTES");
            Debug.Assert((DBStatus.S_OK == StatusValue()), "Value_ByRefBYTES");
            byte[]? value = null;
            RowBinding bindings = RowBinding;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                bindings.DangerousAddRef(ref mustRelease);
                IntPtr ptr = bindings.ReadIntPtr(ValueOffset);
                if (ADP.PtrZero != ptr)
                {
                    value = new byte[LengthValue()];
                    Marshal.Copy(ptr, value, 0, value.Length);
                }
            }
            finally
            {
                if (mustRelease)
                {
                    bindings.DangerousRelease();
                }
            }
            return value ?? Array.Empty<byte>();
        }
        private void Value_ByRefBYTES(byte[] value)
        {
            Debug.Assert(null != value, "Value_ByRefBYTES null");
            Debug.Assert((NativeDBType.BYREF | NativeDBType.BYTES) == DbType, "Value_ByRefBYTES");

            // we expect the provider/server to apply the silent truncation when binding BY_REF
            // if (value.Length < ValueBindingOffset) { throw "Offset must refer to a location within the value" }
            int length = ((ValueBindingOffset < value.Length) ? (value.Length - ValueBindingOffset) : 0);
            LengthValue(((0 < ValueBindingSize) ? Math.Min(ValueBindingSize, length) : length));
            StatusValue(DBStatus.S_OK);

            IntPtr ptr = ADP.PtrZero;
            if (0 < length)
            { // avoid pinning empty byte[]
                _pinnedBuffer = GCHandle.Alloc(value, GCHandleType.Pinned);
                ptr = _pinnedBuffer.AddrOfPinnedObject();
                ptr = ADP.IntPtrOffset(ptr, ValueBindingOffset);
            }
            RowBinding.SetByRefValue(ValueOffset, ptr);
        }

        private string Value_ByRefWSTR()
        {
            Debug.Assert((NativeDBType.BYREF | NativeDBType.WSTR) == DbType, "Value_ByRefWSTR");
            Debug.Assert((DBStatus.S_OK == StatusValue()) || (DBStatus.S_TRUNCATED == StatusValue()), "Value_ByRefWSTR");
            string value = "";
            RowBinding bindings = RowBinding;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                bindings.DangerousAddRef(ref mustRelease);
                IntPtr ptr = bindings.ReadIntPtr(ValueOffset);
                if (ADP.PtrZero != ptr)
                {
                    int charCount = LengthValue() / 2;
                    value = Marshal.PtrToStringUni(ptr, charCount);
                }
            }
            finally
            {
                if (mustRelease)
                {
                    bindings.DangerousRelease();
                }
            }
            return value;
        }
        private void Value_ByRefWSTR(string value)
        {
            Debug.Assert(null != value, "Value_ByRefWSTR null");
            Debug.Assert((NativeDBType.BYREF | NativeDBType.WSTR) == DbType, "Value_ByRefWSTR");
            // we expect the provider/server to apply the silent truncation when binding BY_REF
            // if (value.Length < ValueBindingOffset) { throw "Offset must refer to a location within the value" }
            int length = ((ValueBindingOffset < value.Length) ? (value.Length - ValueBindingOffset) : 0);
            LengthValue(((0 < ValueBindingSize) ? Math.Min(ValueBindingSize, length) : length) * 2); /* charcount->bytecount*/
            StatusValue(DBStatus.S_OK);

            IntPtr ptr = ADP.PtrZero;
            if (0 < length)
            { // avoid pinning empty string, i.e String.Empty
                _pinnedBuffer = GCHandle.Alloc(value, GCHandleType.Pinned);
                ptr = _pinnedBuffer.AddrOfPinnedObject();
                ptr = ADP.IntPtrOffset(ptr, ValueBindingOffset);
            }
            RowBinding.SetByRefValue(ValueOffset, ptr);
        }
        private void Value_ByRefWSTR(char[] value)
        {
            Debug.Assert(null != value, "Value_ByRefWSTR null");
            Debug.Assert((NativeDBType.BYREF | NativeDBType.WSTR) == DbType, "Value_ByRefWSTR");
            // we expect the provider/server to apply the silent truncation when binding BY_REF
            // if (value.Length < ValueBindingOffset) { throw "Offset must refer to a location within the value" }
            int length = ((ValueBindingOffset < value.Length) ? (value.Length - ValueBindingOffset) : 0);
            LengthValue(((0 < ValueBindingSize) ? Math.Min(ValueBindingSize, length) : length) * 2); /* charcount->bytecount*/
            StatusValue(DBStatus.S_OK);

            IntPtr ptr = ADP.PtrZero;
            if (0 < length)
            { // avoid pinning empty char[]
                _pinnedBuffer = GCHandle.Alloc(value, GCHandleType.Pinned);
                ptr = _pinnedBuffer.AddrOfPinnedObject();
                ptr = ADP.IntPtrOffset(ptr, ValueBindingOffset);
            }
            RowBinding.SetByRefValue(ValueOffset, ptr);
        }

        private byte[] Value_BYTES()
        {
            Debug.Assert(NativeDBType.BYTES == DbType, "Value_BYTES");
            Debug.Assert((DBStatus.S_OK == StatusValue()) || (DBStatus.S_TRUNCATED == StatusValue()), "Value_BYTES");
            int byteCount = Math.Min(LengthValue(), ColumnBindingMaxLen);
            byte[] value = new byte[byteCount];
            RowBinding.ReadBytes(ValueOffset, value, 0, byteCount);
            return value;
        }
        private void Value_BYTES(byte[] value)
        {
            Debug.Assert(null != value, "Value_BYTES null");
            // we silently truncate when the user has specified a given Size
            int bytecount = ((ValueBindingOffset < value.Length) ? Math.Min(value.Length - ValueBindingOffset, ColumnBindingMaxLen) : 0);
            LengthValue(bytecount);
            StatusValue(DBStatus.S_OK);
            if (0 < bytecount)
            {
                RowBinding.WriteBytes(ValueOffset, value, ValueBindingOffset, bytecount);
            }
        }

        private decimal Value_CY()
        {
            Debug.Assert(NativeDBType.CY == DbType, "Value_CY");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_CY");
            return decimal.FromOACurrency(RowBinding.ReadInt64(ValueOffset));
        }
        private void Value_CY(decimal value)
        {
            Debug.Assert(NativeDBType.CY == DbType, "Value_CY");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt64(ValueOffset, decimal.ToOACurrency(value));
        }

        private DateTime Value_DATE()
        {
            Debug.Assert(NativeDBType.DATE == DbType, "Value_DATE");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_DATE");
            return DateTime.FromOADate(RowBinding.ReadDouble(ValueOffset));
        }
        private void Value_DATE(DateTime value)
        {
            Debug.Assert(NativeDBType.DATE == DbType, "Value_DATE");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteDouble(ValueOffset, value.ToOADate());
        }

        private DateTime Value_DBDATE()
        {
            Debug.Assert(NativeDBType.DBDATE == DbType, "Value_DBDATE");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_DBDATE");
            return RowBinding.ReadDate(ValueOffset);
        }
        private void Value_DBDATE(DateTime value)
        {
            Debug.Assert(NativeDBType.DBDATE == DbType, "Value_DATE");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteDate(ValueOffset, value);
        }

        private TimeSpan Value_DBTIME()
        {
            Debug.Assert(NativeDBType.DBTIME == DbType, "Value_DBTIME");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_DBTIME");
            return RowBinding.ReadTime(ValueOffset);
        }
        private void Value_DBTIME(TimeSpan value)
        {
            Debug.Assert(NativeDBType.DBTIME == DbType, "Value_DBTIME");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteTime(ValueOffset, value);
        }

        private DateTime Value_DBTIMESTAMP()
        {
            Debug.Assert(NativeDBType.DBTIMESTAMP == DbType, "Value_DBTIMESTAMP");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_DBTIMESTAMP");
            return RowBinding.ReadDateTime(ValueOffset);
        }
        private void Value_DBTIMESTAMP(DateTime value)
        {
            Debug.Assert(NativeDBType.DBTIMESTAMP == DbType, "Value_DBTIMESTAMP");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteDateTime(ValueOffset, value);
        }

        private decimal Value_DECIMAL()
        {
            Debug.Assert(NativeDBType.DECIMAL == DbType, "Value_DECIMAL");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_DECIMAL");
            int[] buffer = new int[4];
            RowBinding.ReadInt32Array(ValueOffset, buffer, 0, 4);
            return new decimal(
                buffer[2],  // low
                buffer[3],  // mid
                buffer[1],  // high
                (0 != (buffer[0] & unchecked((int)0x80000000))), // sign
                unchecked((byte)((buffer[0] & unchecked((int)0x00FF0000)) >> 16))); // scale
        }
        private void Value_DECIMAL(decimal value)
        {
            Debug.Assert(NativeDBType.DECIMAL == DbType, "Value_DECIMAL");

            /* pending breaking change approval
            if (_precision < ((System.Data.SqlTypes.SqlDecimal) value).Precision) {
                throw ADP.ParameterValueOutOfRange(value);
            }
            */

            LengthValue(0);
            StatusValue(DBStatus.S_OK);

            int[] tmp = decimal.GetBits(value);
            int[] buffer = new int[4] {
                tmp[3], tmp[2], tmp[0], tmp[1]
            };
            RowBinding.WriteInt32Array(ValueOffset, buffer, 0, 4);
        }

        private int Value_ERROR()
        {
            Debug.Assert(NativeDBType.ERROR == DbType, "Value_ERROR");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_ERROR");
            return RowBinding.ReadInt32(ValueOffset);
        }
        private void Value_ERROR(int value)
        {
            Debug.Assert(NativeDBType.ERROR == DbType, "Value_ERROR");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt32(ValueOffset, value);
        }

        private DateTime Value_FILETIME()
        {
            Debug.Assert(NativeDBType.FILETIME == DbType, "Value_FILETIME");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_FILETIME");
            long tmp = RowBinding.ReadInt64(ValueOffset);
            return DateTime.FromFileTime(tmp);
        }
        private void Value_FILETIME(DateTime value)
        {
            Debug.Assert(NativeDBType.FILETIME == DbType, "Value_FILETIME");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            long tmp = value.ToFileTime();
            RowBinding.WriteInt64(ValueOffset, tmp);
        }

        internal Guid Value_GUID()
        {
            Debug.Assert(NativeDBType.GUID == DbType, "Value_GUID");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_GUID");
            return RowBinding.ReadGuid(ValueOffset);
        }
        private void Value_GUID(Guid value)
        {
            Debug.Assert(NativeDBType.GUID == DbType, "Value_GUID");

            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteGuid(ValueOffset, value);
        }

        internal OleDbDataReader Value_HCHAPTER()
        {
            Debug.Assert(NativeDBType.HCHAPTER == DbType, "Value_HCHAPTER");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_HCHAPTER");

            return DataReader().ResetChapter(IndexForAccessor, IndexWithinAccessor, RowBinding, ValueOffset);
        }

        private sbyte Value_I1()
        {
            Debug.Assert(NativeDBType.I1 == DbType, "Value_I1");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_I1");
            byte value = RowBinding.ReadByte(ValueOffset);
            return unchecked((sbyte)value);
        }
        private void Value_I1(sbyte value)
        {
            Debug.Assert(NativeDBType.I1 == DbType, "Value_I1");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteByte(ValueOffset, unchecked((byte)value));
        }

        internal short Value_I2()
        {
            Debug.Assert(NativeDBType.I2 == DbType, "Value_I2");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_I2");
            return RowBinding.ReadInt16(ValueOffset);
        }
        private void Value_I2(short value)
        {
            Debug.Assert(NativeDBType.I2 == DbType, "Value_I2");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt16(ValueOffset, value);
        }

        private int Value_I4()
        {
            Debug.Assert(NativeDBType.I4 == DbType, "Value_I4");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_I4");
            return RowBinding.ReadInt32(ValueOffset);
        }
        private void Value_I4(int value)
        {
            Debug.Assert(NativeDBType.I4 == DbType, "Value_I4");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt32(ValueOffset, value);
        }

        private long Value_I8()
        {
            Debug.Assert(NativeDBType.I8 == DbType, "Value_I8");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_I8");
            return RowBinding.ReadInt64(ValueOffset);
        }
        private void Value_I8(long value)
        {
            Debug.Assert(NativeDBType.I8 == DbType, "Value_I8");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt64(ValueOffset, value);
        }

        private object Value_IDISPATCH()
        {
            Debug.Assert(NativeDBType.IDISPATCH == DbType, "Value_IDISPATCH");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_IDISPATCH");
            object value;
            RowBinding bindings = RowBinding;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                bindings.DangerousAddRef(ref mustRelease);
                IntPtr ptr = bindings.ReadIntPtr(ValueOffset);
                value = Marshal.GetObjectForIUnknown(ptr);
            }
            finally
            {
                if (mustRelease)
                {
                    bindings.DangerousRelease();
                }
            }
            return value;
        }
        private void Value_IDISPATCH(object value)
        {
            // UNDONE: OLE DB will IUnknown.Release input storage parameter values
            Debug.Assert(NativeDBType.IDISPATCH == DbType, "Value_IDISPATCH");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);

            IntPtr ptr = IntPtr.Zero;
            // lazy init reflection objects
            if (s_getIDispatchForObject == null)
            {
                object? delegateInstance = null;
                MethodInfo? mi = typeof(Marshal).GetMethod("GetIDispatchForObject", BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                {
                    throw new NotSupportedException(SR.PlatformNotSupported_GetIDispatchForObject);
                }
                Volatile.Write(ref delegateInstance, mi.CreateDelegate(typeof(Func<object, IntPtr>)));
                s_getIDispatchForObject = delegateInstance as Func<object, IntPtr>;
                ptr = s_getIDispatchForObject!(value);
            }
            RowBinding.WriteIntPtr(ValueOffset, ptr);
        }

        private object Value_IUNKNOWN()
        {
            Debug.Assert(NativeDBType.IUNKNOWN == DbType, "Value_IUNKNOWN");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_IUNKNOWN");
            object value;
            RowBinding bindings = RowBinding;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                bindings.DangerousAddRef(ref mustRelease);
                IntPtr ptr = bindings.ReadIntPtr(ValueOffset);
                value = Marshal.GetObjectForIUnknown(ptr);
            }
            finally
            {
                if (mustRelease)
                {
                    bindings.DangerousRelease();
                }
            }
            return value;
        }
        private void Value_IUNKNOWN(object value)
        {
            // UNDONE: OLE DB will IUnknown.Release input storage parameter values
            Debug.Assert(NativeDBType.IUNKNOWN == DbType, "Value_IUNKNOWN");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            IntPtr ptr = Marshal.GetIUnknownForObject(value);
            RowBinding.WriteIntPtr(ValueOffset, ptr);
        }

        private decimal Value_NUMERIC()
        {
            Debug.Assert(NativeDBType.NUMERIC == DbType, "Value_NUMERIC");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_NUMERIC");
            return RowBinding.ReadNumeric(ValueOffset);
        }
        private void Value_NUMERIC(decimal value)
        {
            Debug.Assert(NativeDBType.NUMERIC == DbType, "Value_NUMERIC");

            /* pending breaking change approval
            if (_precision < ((System.Data.SqlTypes.SqlDecimal) value).Precision) {
                throw ADP.ParameterValueOutOfRange(value);
            }
            */

            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteNumeric(ValueOffset, value, ColumnBindingPrecision);
        }

        private float Value_R4()
        {
            Debug.Assert(NativeDBType.R4 == DbType, "Value_R4");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_R4");

            return RowBinding.ReadSingle(ValueOffset);
        }
        private void Value_R4(float value)
        {
            Debug.Assert(NativeDBType.R4 == DbType, "Value_R4");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteSingle(ValueOffset, value);
        }

        private double Value_R8()
        {
            Debug.Assert(NativeDBType.R8 == DbType, "Value_R8");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_R8");

            return RowBinding.ReadDouble(ValueOffset);
        }
        private void Value_R8(double value)
        {
            Debug.Assert(NativeDBType.R8 == DbType, "Value_I4");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteDouble(ValueOffset, value);
        }

        private byte Value_UI1()
        {
            Debug.Assert(NativeDBType.UI1 == DbType, "Value_UI1");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_UI1");
            return RowBinding.ReadByte(ValueOffset);
        }
        private void Value_UI1(byte value)
        {
            Debug.Assert(NativeDBType.UI1 == DbType, "Value_UI1");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteByte(ValueOffset, value);
        }

        internal ushort Value_UI2()
        {
            Debug.Assert(NativeDBType.UI2 == DbType, "Value_UI2");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_UI2");
            return unchecked((ushort)RowBinding.ReadInt16(ValueOffset));
        }
        private void Value_UI2(ushort value)
        {
            Debug.Assert(NativeDBType.UI2 == DbType, "Value_UI2");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt16(ValueOffset, unchecked((short)value));
        }

        internal uint Value_UI4()
        {
            Debug.Assert(NativeDBType.UI4 == DbType, "Value_UI4");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_UI4");
            return unchecked((uint)RowBinding.ReadInt32(ValueOffset));
        }
        private void Value_UI4(uint value)
        {
            Debug.Assert(NativeDBType.UI4 == DbType, "Value_UI4");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt32(ValueOffset, unchecked((int)value));
        }

        internal ulong Value_UI8()
        {
            Debug.Assert(NativeDBType.UI8 == DbType, "Value_UI8");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_UI8");
            return unchecked((ulong)RowBinding.ReadInt64(ValueOffset));
        }
        private void Value_UI8(ulong value)
        {
            Debug.Assert(NativeDBType.UI8 == DbType, "Value_UI8");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.WriteInt64(ValueOffset, unchecked((long)value));
        }

        private string Value_WSTR()
        {
            Debug.Assert(NativeDBType.WSTR == DbType, "Value_WSTR");
            Debug.Assert((DBStatus.S_OK == StatusValue()) || (DBStatus.S_TRUNCATED == StatusValue()), "Value_WSTR");
            Debug.Assert(2 < ColumnBindingMaxLen, "Value_WSTR");
            int byteCount = Math.Min(LengthValue(), ColumnBindingMaxLen - 2);
            return RowBinding.PtrToStringUni(ValueOffset, byteCount / 2);
        }
        private void Value_WSTR(string value)
        {
            Debug.Assert(null != value, "Value_BYTES null");
            Debug.Assert(NativeDBType.WSTR == DbType, "Value_WSTR");
            // we silently truncate when the user has specified a given Size
            int charCount = ((ValueBindingOffset < value.Length) ? Math.Min(value.Length - ValueBindingOffset, (ColumnBindingMaxLen - 2) / 2) : 0);

            LengthValue(charCount * 2);
            StatusValue(DBStatus.S_OK);
            if (0 < charCount)
            {
                char[] chars = value.ToCharArray(ValueBindingOffset, charCount);
                RowBinding.WriteCharArray(ValueOffset, chars, ValueBindingOffset, charCount);
            }
        }
        private void Value_WSTR(char[] value)
        {
            Debug.Assert(null != value, "Value_BYTES null");
            Debug.Assert(NativeDBType.WSTR == DbType, "Value_WSTR");
            // we silently truncate when the user has specified a given Size
            int charCount = ((ValueBindingOffset < value.Length) ? Math.Min(value.Length - ValueBindingOffset, (ColumnBindingMaxLen - 2) / 2) : 0);

            LengthValue(charCount * 2);
            StatusValue(DBStatus.S_OK);
            if (0 < charCount)
            {
                RowBinding.WriteCharArray(ValueOffset, value, ValueBindingOffset, charCount);
            }
        }
        private object Value_VARIANT()
        {
            Debug.Assert((NativeDBType.VARIANT == DbType) || (NativeDBType.PROPVARIANT == DbType), "Value_VARIANT");
            Debug.Assert(DBStatus.S_OK == StatusValue(), "Value_VARIANT");
            return RowBinding.GetVariantValue(ValueOffset);
        }
        private void Value_VARIANT(object value)
        {
            Debug.Assert((NativeDBType.VARIANT == DbType) || (NativeDBType.PROPVARIANT == DbType), "Value_VARIANT");
            LengthValue(0);
            StatusValue(DBStatus.S_OK);
            RowBinding.SetVariantValue(ValueOffset, value);
        }

        internal bool ValueBoolean()
        {
            bool value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.BOOL:
                            value = Value_BOOL();
                            break;
                        case NativeDBType.VARIANT:
                            value = (bool)ValueVariant();
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(bool));
            }
            return value;
        }

        internal byte[] ValueByteArray()
        {
            byte[]? value = (byte[]?)_value;
            if (null == value)
            {
                switch (StatusValue())
                {
                    case DBStatus.S_OK:
                        switch (DbType)
                        {
                            case NativeDBType.BYTES:
                                value = Value_BYTES(); // String
                                break;
                            case NativeDBType.VARIANT:
                                value = (byte[])ValueVariant(); // Object
                                break;
                            case (NativeDBType.BYREF | NativeDBType.BYTES):
                                value = Value_ByRefBYTES();
                                break;
                            default:
                                throw ODB.ConversionRequired();
                        }
                        break;
                    case DBStatus.S_TRUNCATED:
                        switch (DbType)
                        {
                            case NativeDBType.BYTES:
                                value = Value_BYTES();
                                break;
                            case (NativeDBType.BYREF | NativeDBType.BYTES):
                                value = Value_ByRefBYTES();
                                break;
                            default:
                                throw ODB.ConversionRequired();
                        }
                        break;
                    default:
                        throw CheckTypeValueStatusValue(typeof(byte[]));
                }
                _value = value;
            }
            return value;
        }

        internal byte ValueByte()
        {
            byte value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.UI1:
                            value = Value_UI1();
                            break;
                        case NativeDBType.VARIANT:
                            value = (byte)ValueVariant();
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(byte));
            }
            return value;
        }

        internal OleDbDataReader ValueChapter()
        {
            OleDbDataReader? value = (OleDbDataReader?)_value;
            if (null == value)
            {
                switch (StatusValue())
                {
                    case DBStatus.S_OK:
                        switch (DbType)
                        {
                            case NativeDBType.HCHAPTER:
                                value = Value_HCHAPTER(); // OleDbDataReader
                                break;
                            default:
                                throw ODB.ConversionRequired();
                        }
                        break;
                    default:
                        throw CheckTypeValueStatusValue(typeof(string));
                }
                _value = value;
            }
            return value;
        }

        internal DateTime ValueDateTime()
        {
            DateTime value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.DATE:
                            value = Value_DATE();
                            break;
                        case NativeDBType.DBDATE:
                            value = Value_DBDATE();
                            break;
                        case NativeDBType.DBTIMESTAMP:
                            value = Value_DBTIMESTAMP();
                            break;
                        case NativeDBType.FILETIME:
                            value = Value_FILETIME();
                            break;
                        case NativeDBType.VARIANT:
                            value = (DateTime)ValueVariant();
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(short));
            }
            return value;
        }

        internal decimal ValueDecimal()
        {
            decimal value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.CY:
                            value = Value_CY();
                            break;
                        case NativeDBType.DECIMAL:
                            value = Value_DECIMAL();
                            break;
                        case NativeDBType.NUMERIC:
                            value = Value_NUMERIC();
                            break;
                        case NativeDBType.UI8:
                            value = (decimal)Value_UI8();
                            break;
                        case NativeDBType.VARIANT:
                            value = (decimal)ValueVariant();
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(short));
            }
            return value;
        }

        internal Guid ValueGuid()
        {
            Guid value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.GUID:
                            value = Value_GUID();
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(short));
            }
            return value;
        }

        internal short ValueInt16()
        {
            short value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.I2:
                            value = Value_I2();
                            break;
                        case NativeDBType.I1:
                            value = (short)Value_I1();
                            break;
                        case NativeDBType.VARIANT:
                            object variant = ValueVariant();
                            if (variant is sbyte)
                            {
                                value = (short)(sbyte)variant;
                            }
                            else
                            {
                                value = (short)variant;
                            }
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(short));
            }
            return value;
        }

        internal int ValueInt32()
        {
            int value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.I4:
                            value = Value_I4();
                            break;
                        case NativeDBType.UI2:
                            value = (int)Value_UI2();
                            break;
                        case NativeDBType.VARIANT:
                            object variant = ValueVariant();
                            if (variant is ushort)
                            {
                                value = (int)(ushort)variant;
                            }
                            else
                            {
                                value = (int)variant;
                            }
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(int));
            }
            return value;
        }

        internal long ValueInt64()
        {
            long value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.I8:
                            value = Value_I8();
                            break;
                        case NativeDBType.UI4:
                            value = (long)Value_UI4();
                            break;
                        case NativeDBType.VARIANT:
                            object variant = ValueVariant();
                            if (variant is uint)
                            {
                                value = (long)(uint)variant;
                            }
                            else
                            {
                                value = (long)variant;
                            }
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(long));
            }
            return value;
        }

        internal float ValueSingle()
        {
            float value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.R4:
                            value = Value_R4();
                            break;
                        case NativeDBType.VARIANT:
                            value = (float)ValueVariant();
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(float));
            }
            return value;
        }

        internal double ValueDouble()
        {
            double value;
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    switch (DbType)
                    {
                        case NativeDBType.R8:
                            value = Value_R8();
                            break;
                        case NativeDBType.VARIANT:
                            value = (double)ValueVariant();
                            break;
                        default:
                            throw ODB.ConversionRequired();
                    }
                    break;
                default:
                    throw CheckTypeValueStatusValue(typeof(double));
            }
            return value;
        }

        internal string ValueString()
        {
            string? value = (string?)_value;
            if (null == value)
            {
                switch (StatusValue())
                {
                    case DBStatus.S_OK:
                        switch (DbType)
                        {
                            case NativeDBType.BSTR:
                                value = Value_BSTR(); // String
                                break;
                            case NativeDBType.VARIANT:
                                value = (string)ValueVariant(); // Object
                                break;
                            case NativeDBType.WSTR:
                                value = Value_WSTR(); // String
                                break;
                            case (NativeDBType.BYREF | NativeDBType.WSTR):
                                value = Value_ByRefWSTR();
                                break;
                            default:
                                throw ODB.ConversionRequired();
                        }
                        break;
                    case DBStatus.S_TRUNCATED:
                        switch (DbType)
                        {
                            case NativeDBType.WSTR:
                                value = Value_WSTR();
                                break;
                            case (NativeDBType.BYREF | NativeDBType.WSTR):
                                value = Value_ByRefWSTR();
                                break;
                            default:
                                throw ODB.ConversionRequired();
                        }
                        break;
                    default:
                        throw CheckTypeValueStatusValue(typeof(string));
                }
                _value = value;
            }
            return value;
        }

        private object ValueVariant()
        {
            object? value = _value;
            if (null == value)
            {
                value = Value_VARIANT();
                _value = value;
            }
            return value;
        }

        private Exception CheckTypeValueStatusValue()
        {
            return CheckTypeValueStatusValue(ExpectedType);
        }

        private Exception CheckTypeValueStatusValue(Type expectedType)
        {
            switch (StatusValue())
            {
                case DBStatus.S_OK:
                    Debug.Assert(false, "CheckStatusValue: unhandled data with ok status");
                    goto case DBStatus.E_CANTCONVERTVALUE;
                case DBStatus.S_TRUNCATED:
                    Debug.Assert(false, "CheckStatusValue: unhandled data with truncated status");
                    goto case DBStatus.E_CANTCONVERTVALUE;
                case DBStatus.E_BADACCESSOR:
                    return ODB.BadAccessor();
                case DBStatus.E_CANTCONVERTVALUE:
                    return ODB.CantConvertValue(); // UNDONE: need original data type
                case DBStatus.S_ISNULL: // database null
                    return ADP.InvalidCast(); // UNDONE: NullValue exception
                case DBStatus.E_SIGNMISMATCH:
                    return ODB.SignMismatch(expectedType);
                case DBStatus.E_DATAOVERFLOW:
                    return ODB.DataOverflow(expectedType);
                case DBStatus.E_CANTCREATE:
                    return ODB.CantCreate(expectedType);
                case DBStatus.E_UNAVAILABLE:
                    return ODB.Unavailable(expectedType);
                default:
                    return ODB.UnexpectedStatusValue(StatusValue());
            }
        }
    }
}
