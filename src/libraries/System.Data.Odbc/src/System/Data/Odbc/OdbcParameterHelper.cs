// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.Odbc
{
    public sealed partial class OdbcParameter : DbParameter
    {
        private object? _value;

        private object? _parent;

        private ParameterDirection _direction;
        private int _size;
        private int _offset;
        private string? _sourceColumn;
        private DataRowVersion _sourceVersion;
        private bool _sourceColumnNullMapping;

        private bool _isNullable;

        private object? _coercedValue;

        private OdbcParameter(OdbcParameter source) : this() // V1.2.3300, Clone
        {
            ADP.CheckArgumentNull(source, nameof(source));

            source.CloneHelper(this);

            if (_value is ICloneable cloneable)
            { // MDAC 49322
                _value = cloneable.Clone();
            }
        }

        private object? CoercedValue
        {
            get
            {
                return _coercedValue;
            }
            set
            {
                _coercedValue = value;
            }
        }

        public override ParameterDirection Direction
        {
            get
            {
                ParameterDirection direction = _direction;
                return ((0 != direction) ? direction : ParameterDirection.Input);
            }
            set
            {
                if (_direction != value)
                {
                    switch (value)
                    {
                        case ParameterDirection.Input:
                        case ParameterDirection.Output:
                        case ParameterDirection.InputOutput:
                        case ParameterDirection.ReturnValue:
                            PropertyChanging();
                            _direction = value;
                            break;
                        default:
                            throw ADP.InvalidParameterDirection(value);
                    }
                }
            }
        }

        public override bool IsNullable
        {
            get
            {
                return _isNullable;
            }
            set
            {
                _isNullable = value;
            }
        }


        public int Offset
        {
            get
            {
                return _offset;
            }
            set
            {
                if (value < 0)
                {
                    throw ADP.InvalidOffsetValue(value);
                }
                _offset = value;
            }
        }

        public override int Size
        {
            get
            {
                int size = _size;
                if (0 == size)
                {
                    size = ValueSize(Value);
                }
                return size;
            }
            set
            {
                if (_size != value)
                {
                    if (value < -1)
                    {
                        throw ADP.InvalidSizeValue(value);
                    }
                    PropertyChanging();
                    _size = value;
                }
            }
        }


        private bool ShouldSerializeSize()
        {
            return (0 != _size);
        }

        [AllowNull]
        public override string SourceColumn
        {
            get
            {
                return _sourceColumn ?? string.Empty;
            }
            set
            {
                _sourceColumn = value;
            }
        }

        public override bool SourceColumnNullMapping
        {
            get
            {
                return _sourceColumnNullMapping;
            }
            set
            {
                _sourceColumnNullMapping = value;
            }
        }

        public override DataRowVersion SourceVersion
        { // V1.2.3300, XXXParameter V1.0.3300
            get
            {
                DataRowVersion sourceVersion = _sourceVersion;
                return ((0 != sourceVersion) ? sourceVersion : DataRowVersion.Current);
            }
            set
            {
                switch (value)
                { // @perfnote: Enum.IsDefined
                    case DataRowVersion.Original:
                    case DataRowVersion.Current:
                    case DataRowVersion.Proposed:
                    case DataRowVersion.Default:
                        _sourceVersion = value;
                        break;
                    default:
                        throw ADP.InvalidDataRowVersion(value);
                }
            }
        }

        private void CloneHelperCore(OdbcParameter destination)
        {
            destination._value = _value;
            // NOTE: _parent is not cloned
            destination._direction = _direction;
            destination._size = _size;
            destination._offset = _offset;
            destination._sourceColumn = _sourceColumn;
            destination._sourceVersion = _sourceVersion;
            destination._sourceColumnNullMapping = _sourceColumnNullMapping;
            destination._isNullable = _isNullable;
        }

        internal object? CompareExchangeParent(object? value, object? comparand)
        {
            object? parent = _parent;
            if (comparand == parent)
            {
                _parent = value;
            }
            return parent;
        }

        internal void ResetParent()
        {
            _parent = null;
        }

        public override string ToString()
        {
            return ParameterName;
        }

        private static byte ValuePrecisionCore(object? value)
        {
            if (value is decimal)
            {
                return ((System.Data.SqlTypes.SqlDecimal)(decimal)value).Precision;
            }
            return 0;
        }

        private static byte ValueScaleCore(object? value)
        {
            if (value is decimal)
            {
                return (byte)((decimal.GetBits((decimal)value)[3] & 0x00ff0000) >> 0x10);
            }
            return 0;
        }

        private static int ValueSizeCore(object? value)
        {
            if (!ADP.IsNull(value))
            {
                return value switch
                {
                    string svalue => svalue.Length,
                    byte[] bvalue => bvalue.Length,
                    char[] cvalue => cvalue.Length,
                    byte => 1,
                    char => 1,
                    _ => 0
                };
            }
            return 0;
        }
    }
}
