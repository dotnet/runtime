// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace System.Data.OleDb
{
    public sealed partial class OleDbParameter : DbParameter
    { // V1.2.3300
        private object? _value;

        private object? _parent;

        private ParameterDirection _direction;
        private int _size;
        private string? _sourceColumn;
        private DataRowVersion _sourceVersion;
        private bool _sourceColumnNullMapping;

        private bool _isNullable;

        private object? _coercedValue;

        private OleDbParameter(OleDbParameter source) : this()
        { // V1.2.3300, Clone
            ADP.CheckArgumentNull(source, "source");

            source.CloneHelper(this);

            ICloneable? cloneable = (_value as ICloneable);
            if (null != cloneable)
            {
                _value = cloneable.Clone();
            }
        }

        private object? CoercedValue
        { // V1.2.3300
            get
            {
                return _coercedValue;
            }
            set
            {
                _coercedValue = value;
            }
        }

        [RefreshProperties(RefreshProperties.All)]
        public override ParameterDirection Direction
        { // V1.2.3300, XXXParameter V1.0.3300
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
                    { // @perfnote: Enum.IsDefined
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
        { // V1.2.3300, XXXParameter V1.0.3300
            get
            {
                return _isNullable;
            }
            set
            {
                _isNullable = value;
            }
        }

        internal const int Offset = 0;

        public override int Size
        { // V1.2.3300, XXXParameter V1.0.3300
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

        private void ResetSize()
        {
            if (0 != _size)
            {
                PropertyChanging();
                _size = 0;
            }
        }

        private bool ShouldSerializeSize()
        { // V1.2.3300
            return (0 != _size);
        }

        [AllowNull]
        public override string SourceColumn
        { // V1.2.3300, XXXParameter V1.0.3300
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

        private void CloneHelperCore(OleDbParameter destination)
        {
            destination._value = _value;
            // NOTE: _parent is not cloned
            destination._direction = _direction;
            destination._size = _size;
            destination._sourceColumn = _sourceColumn;
            destination._sourceVersion = _sourceVersion;
            destination._sourceColumnNullMapping = _sourceColumnNullMapping;
            destination._isNullable = _isNullable;
        }

        internal void CopyTo(DbParameter destination)
        {
            ADP.CheckArgumentNull(destination, "destination");
            CloneHelper((OleDbParameter)destination);
        }

        internal object? CompareExchangeParent(object? value, object? comparand)
        {
            // the interlock guarantees same parameter won't belong to multiple collections
            // at the same time, but to actually occur the user must really try
            // since we never declared thread safety, we don't care at this time
            //return System.Threading.Interlocked.CompareExchange(ref _parent, value, comparand);
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
        { // V1.2.3300, XXXParameter V1.0.3300
            return ParameterName;
        }

        private static byte ValuePrecisionCore(object? value)
        { // V1.2.3300
            if (value is decimal)
            {
                return ((System.Data.SqlTypes.SqlDecimal)(decimal)value).Precision;
            }
            return 0;
        }

        private static byte ValueScaleCore(object? value)
        { // V1.2.3300
            if (value is decimal)
            {
                return (byte)((decimal.GetBits((decimal)value)[3] & 0x00ff0000) >> 0x10);
            }
            return 0;
        }

        private static int ValueSizeCore(object? value)
        { // V1.2.3300
            if (!ADP.IsNull(value))
            {
                string? svalue = (value as string);
                if (null != svalue)
                {
                    return svalue.Length;
                }
                byte[]? bvalue = (value as byte[]);
                if (null != bvalue)
                {
                    return bvalue.Length;
                }
                char[]? cvalue = (value as char[]);
                if (null != cvalue)
                {
                    return cvalue.Length;
                }
                if ((value is byte) || (value is char))
                {
                    return 1;
                }
            }
            return 0;
        }
    }
}
