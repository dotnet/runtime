// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




//------------------------------------------------------------------------------

using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.SqlServer.Server
{
    internal sealed class SqlRecordBuffer
    {
        internal enum StorageType
        {
            Boolean,
            Byte,
            ByteArray,
            CharArray,
            DateTime,
            DateTimeOffset,
            Double,
            Guid,
            Int16,
            Int32,
            Int64,
            Single,
            String,
            SqlDecimal,
            TimeSpan,
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Storage
        {
            [FieldOffset(0)]
            internal bool _boolean;
            [FieldOffset(0)]
            internal byte _byte;
            [FieldOffset(0)]
            internal DateTime _dateTime;
            [FieldOffset(0)]
            internal DateTimeOffset _dateTimeOffset;
            [FieldOffset(0)]
            internal double _double;
            [FieldOffset(0)]
            internal Guid _guid;
            [FieldOffset(0)]
            internal short _int16;
            [FieldOffset(0)]
            internal int _int32;
            [FieldOffset(0)]
            internal long _int64;    // also used to BytesLength and CharsLength
            [FieldOffset(0)]
            internal float _single;
            [FieldOffset(0)]
            internal TimeSpan _timeSpan;
        }

        private bool _isNull;
        private StorageType _type;
        private Storage _value;
        private object _object;    // String, SqlDecimal
        private SmiMetaData _metadata;  // for variant
        private bool _isMetaSet; // flag to indicate whether we have set the variant metadata

        internal SqlRecordBuffer(SmiMetaData metaData)
        {
            _isNull = true;
        }

        internal bool IsNull
        {
            get
            {
                return _isNull;
            }
        }

        internal bool Boolean
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.Boolean == _type, "Wrong storage type: " + _type);

                return _value._boolean;
            }
            set
            {
                _value._boolean = value;
                _type = StorageType.Boolean;
                _isNull = false;
            }
        }

        internal byte Byte
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.Byte == _type, "Wrong storage type: " + _type);

                return _value._byte;
            }
            set
            {
                _value._byte = value;
                _type = StorageType.Byte;
                _isNull = false;
            }
        }

        internal DateTime DateTime
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.DateTime == _type, "Wrong storage type: " + _type);

                return _value._dateTime;
            }
            set
            {
                _value._dateTime = value;
                _type = StorageType.DateTime;
                _isNull = false;
                // in case of variant types, the caller first sets metadata and than the value. we need to keep metadata after first value set
                // if value is set second time without variant metadata, reset the metadata since it is not variant anymore
                if (_isMetaSet)
                {
                    _isMetaSet = false;
                }
                else
                {
                    _metadata = null; // need to clear the variant metadata
                }
            }
        }

        internal DateTimeOffset DateTimeOffset
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.DateTimeOffset == _type, "Wrong storage type: " + _type);

                return _value._dateTimeOffset;
            }
            set
            {
                _value._dateTimeOffset = value;
                _type = StorageType.DateTimeOffset;
                _isNull = false;
            }
        }

        internal double Double
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.Double == _type, "Wrong storage type: " + _type);

                return _value._double;
            }
            set
            {
                _value._double = value;
                _type = StorageType.Double;
                _isNull = false;
            }
        }

        internal Guid Guid
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.Guid == _type, "Wrong storage type: " + _type);

                return _value._guid;
            }
            set
            {
                _value._guid = value;
                _type = StorageType.Guid;
                _isNull = false;
            }
        }

        internal short Int16
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.Int16 == _type, "Wrong storage type: " + _type);

                return _value._int16;
            }
            set
            {
                _value._int16 = value;
                _type = StorageType.Int16;
                _isNull = false;
            }
        }

        internal int Int32
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.Int32 == _type, "Wrong storage type: " + _type);

                return _value._int32;
            }
            set
            {
                _value._int32 = value;
                _type = StorageType.Int32;
                _isNull = false;
            }
        }

        internal long Int64
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.Int64 == _type, "Wrong storage type: " + _type);

                return _value._int64;
            }
            set
            {
                _value._int64 = value;
                _type = StorageType.Int64;
                _isNull = false;
                if (_isMetaSet)
                {
                    _isMetaSet = false;
                }
                else
                {
                    _metadata = null; // need to clear the variant metadata
                }
            }
        }

        internal float Single
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.Single == _type, "Wrong storage type: " + _type);

                return _value._single;
            }
            set
            {
                _value._single = value;
                _type = StorageType.Single;
                _isNull = false;
            }
        }

        internal string String
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");

                if (StorageType.String == _type)
                {
                    return (string)_object;
                }
                else if (StorageType.CharArray == _type)
                {
                    return new string((char[])_object, 0, (int)CharsLength);
                }
                else
                {
                    // Xml may be stored as byte array, yet have GetString called against it.
                    Debug.Assert(StorageType.ByteArray == _type, "Wrong storage type: " + _type);
                    System.IO.Stream byteStream = new System.IO.MemoryStream((byte[])_object, false);
                    return (new SqlXml(byteStream)).Value;
                }
            }
            set
            {
                Debug.Assert(null != value, "");

                _object = value;
                _value._int64 = ((string)value).Length;
                _type = StorageType.String;
                _isNull = false;
                if (_isMetaSet)
                {
                    _isMetaSet = false;
                }
                else
                {
                    _metadata = null; // need to clear the variant metadata
                }
            }
        }

        internal SqlDecimal SqlDecimal
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.SqlDecimal == _type, "Wrong storage type: " + _type);

                return (SqlDecimal)_object;
            }
            set
            {
                Debug.Assert(!value.IsNull, "Null input");

                _object = value;
                _type = StorageType.SqlDecimal;
                _isNull = false;
            }
        }

        internal TimeSpan TimeSpan
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.TimeSpan == _type, "Wrong storage type: " + _type);

                return _value._timeSpan;
            }
            set
            {
                _value._timeSpan = value;
                _type = StorageType.TimeSpan;
                _isNull = false;
            }
        }

        internal long BytesLength
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");

                // sometimes Xml is stored as string, but must support byte access
                if (StorageType.String == _type)
                {
                    ConvertXmlStringToByteArray();
                }
                else
                {
                    Debug.Assert(StorageType.ByteArray == _type, "Wrong storage type: " + _type);
                }

                return _value._int64;
            }
            set
            {
                if (0 == value)
                {
                    _value._int64 = value;
                    _object = Array.Empty<byte>();
                    _type = StorageType.ByteArray;
                    _isNull = false;
                }
                else
                {
                    Debug.Assert(!_isNull, "Null data type");
                    Debug.Assert(StorageType.ByteArray == _type, "Wrong storage type: " + _type);
                    Debug.Assert(value > 0 && value <= ((byte[])_object).Length, "Invalid BytesLength");

                    _value._int64 = value;
                }
            }
        }

        internal long CharsLength
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(StorageType.CharArray == _type || StorageType.String == _type, "Wrong storage type: " + _type);

                return _value._int64;
            }
            set
            {
                if (0 == value)
                {
                    _value._int64 = value;
                    _object = Array.Empty<char>();
                    _type = StorageType.CharArray;
                    _isNull = false;
                }
                else
                {
                    Debug.Assert(!_isNull, "Null data type");
                    Debug.Assert(StorageType.CharArray == _type || StorageType.String == _type, "Wrong storage type: " + _type);
                    Debug.Assert(value > 0 &&
                        ((StorageType.CharArray == _type && value <= ((char[])_object).Length) || (StorageType.String == _type && value <= ((string)_object).Length)),
                        "Invalid CharsLength");

                    _value._int64 = value;
                }
            }
        }

        internal SmiMetaData VariantType
        {
            get
            {
                Debug.Assert(!_isNull, "Null data type");
                Debug.Assert(_metadata == null
                    || _metadata.SqlDbType == SqlDbType.Money
                    || _metadata.SqlDbType == SqlDbType.NVarChar
                    || _metadata.SqlDbType == SqlDbType.DateTime
                    || _metadata.SqlDbType == SqlDbType.Date
                    || _metadata.SqlDbType == SqlDbType.DateTime2,
                    "Invalid metadata");

                switch (_type)
                {
                    case StorageType.Boolean: return SmiMetaData.DefaultBit;
                    case StorageType.Byte: return SmiMetaData.DefaultTinyInt;
                    case StorageType.ByteArray: return SmiMetaData.DefaultVarBinary;
                    case StorageType.CharArray: return SmiMetaData.DefaultNVarChar;
                    case StorageType.DateTime: return _metadata ?? SmiMetaData.DefaultDateTime;
                    case StorageType.DateTimeOffset: return SmiMetaData.DefaultDateTimeOffset;
                    case StorageType.Double: return SmiMetaData.DefaultFloat;
                    case StorageType.Guid: return SmiMetaData.DefaultUniqueIdentifier;
                    case StorageType.Int16: return SmiMetaData.DefaultSmallInt;
                    case StorageType.Int32: return SmiMetaData.DefaultInt;
                    case StorageType.Int64: return _metadata ?? SmiMetaData.DefaultBigInt;
                    case StorageType.Single: return SmiMetaData.DefaultReal;
                    case StorageType.String: return _metadata ?? SmiMetaData.DefaultNVarChar;
                    case StorageType.SqlDecimal: return new SmiMetaData(SqlDbType.Decimal, 17, ((SqlDecimal)_object).Precision, ((SqlDecimal)_object).Scale, 0, SqlCompareOptions.None, null);
                    case StorageType.TimeSpan: return SmiMetaData.DefaultTime;
                }
                return null;
            }
            set
            {
                Debug.Assert(value != null && (value.SqlDbType == SqlDbType.Money || value.SqlDbType == SqlDbType.NVarChar),
                    "Invalid metadata");

                _metadata = value;
                _isMetaSet = true;
            }
        }

        internal int GetBytes(long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            int ndataIndex = (int)fieldOffset;

            Debug.Assert(!_isNull, "Null data type");
            // sometimes Xml is stored as string, but must support byte access
            if (StorageType.String == _type)
            {
                ConvertXmlStringToByteArray();
            }
            else
            {
                Debug.Assert(StorageType.ByteArray == _type, "Wrong storage type: " + _type);
            }
            Debug.Assert(null != buffer, "Null buffer");
            Debug.Assert(ndataIndex + length <= BytesLength, "Invalid fieldOffset or length");

            Buffer.BlockCopy((byte[])_object, ndataIndex, buffer, bufferOffset, length);

            return length;
        }

        internal int GetChars(long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            int ndataIndex = (int)fieldOffset;

            Debug.Assert(!_isNull, "Null data type");
            Debug.Assert(StorageType.CharArray == _type || StorageType.String == _type, "Wrong storage type: " + _type);
            Debug.Assert(null != buffer, "Null buffer");
            Debug.Assert(ndataIndex + length <= CharsLength, "Invalid fieldOffset or length");

            if (StorageType.CharArray == _type)
            {
                Array.Copy((char[])_object, ndataIndex, buffer, bufferOffset, length);
            }
            else
            {    // String type
                ((string)_object).CopyTo(ndataIndex, buffer, bufferOffset, length);
            }

            return length;
        }

        internal int SetBytes(long fieldOffset, byte[] buffer, int bufferOffset, int length)
        {
            int ndataIndex = (int)fieldOffset;

            if (IsNull || StorageType.ByteArray != _type)
            {
                if (ndataIndex != 0)
                {    // set the first time: should start from the beginning
                    throw ADP.ArgumentOutOfRange(nameof(fieldOffset));
                }
                _object = new byte[length];
                _type = StorageType.ByteArray;
                _isNull = false;
                BytesLength = length;
            }
            else
            {
                if (ndataIndex > BytesLength)
                {    // no gap is allowed
                    throw ADP.ArgumentOutOfRange(nameof(fieldOffset));
                }
                if (ndataIndex + length > BytesLength)
                {    // beyond the current length
                    int cbytes = ((byte[])_object).Length;

                    if (ndataIndex + length > cbytes)
                    {    // dynamic expansion
                        byte[] data = new byte[Math.Max(ndataIndex + length, 2 * cbytes)];
                        Buffer.BlockCopy((byte[])_object, 0, data, 0, (int)BytesLength);
                        _object = data;
                    }
                    BytesLength = ndataIndex + length;
                }
            }

            Buffer.BlockCopy(buffer, bufferOffset, (byte[])_object, ndataIndex, length);

            return length;
        }

        internal int SetChars(long fieldOffset, char[] buffer, int bufferOffset, int length)
        {
            int ndataIndex = (int)fieldOffset;

            if (IsNull || (StorageType.CharArray != _type && StorageType.String != _type))
            {
                if (ndataIndex != 0)
                {    // set the first time: should start from the beginning
                    throw ADP.ArgumentOutOfRange(nameof(fieldOffset));
                }
                _object = new char[length];
                _type = StorageType.CharArray;
                _isNull = false;
                CharsLength = length;
            }
            else
            {
                if (ndataIndex > CharsLength)
                {    // no gap is allowed
                    throw ADP.ArgumentOutOfRange(nameof(fieldOffset));
                }
                if (StorageType.String == _type)
                {    // convert string to char[]
                    _object = ((string)_object).ToCharArray();
                    _type = StorageType.CharArray;
                }
                if (ndataIndex + length > CharsLength)
                {    // beyond the current length
                    int cchars = ((char[])_object).Length;

                    if (ndataIndex + length > cchars)
                    {    // dynamic expansion
                        char[] data = new char[Math.Max(ndataIndex + length, 2 * cchars)];
                        Debug.Assert(CharsLength < int.MaxValue);
                        Array.Copy((char[])_object, 0, data, 0, (int)CharsLength);
                        _object = data;
                    }
                    CharsLength = ndataIndex + length;
                }
            }

            Array.Copy(buffer, bufferOffset, (char[])_object, ndataIndex, length);

            return length;
        }

        internal void SetNull()
        {
            _isNull = true;
        }

        // Handle case for Xml where SetString() was called, followed by GetBytes()
        private void ConvertXmlStringToByteArray()
        {
            Debug.Assert(StorageType.String == _type, "ConvertXmlStringToByteArray: Invalid storage type for conversion: " + _type.ToString());

            // Grab the unicode bytes, but prepend the XML unicode BOM
            string value = (string)_object;
            byte[] bytes = new byte[2 + System.Text.Encoding.Unicode.GetByteCount(value)];
            bytes[0] = 0xff;
            bytes[1] = 0xfe;
            System.Text.Encoding.Unicode.GetBytes(value, 0, value.Length, bytes, 2);

            _object = bytes;
            _value._int64 = bytes.Length;
            _type = StorageType.ByteArray;
        }
    }
}
