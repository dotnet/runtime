// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data.OleDb
{
    [TypeConverter(typeof(OleDbParameter.OleDbParameterConverter))]
    public sealed partial class OleDbParameter : DbParameter, ICloneable, IDbDataParameter
    {
        private NativeDBType? _metaType;
        private int _changeID;

        private string? _parameterName;
        private byte _precision;
        private byte _scale;
        private bool _hasScale;

        private NativeDBType? _coerceMetaType;

        public OleDbParameter() : base()
        { // V1.0 nothing
        }

        public OleDbParameter(string? name, object? value) : this()
        {
            Debug.Assert(!(value is OleDbType), "use OleDbParameter(string, OleDbType)");
            Debug.Assert(!(value is SqlDbType), "use OleDbParameter(string, OleDbType)");

            ParameterName = name;
            Value = value;
        }

        public OleDbParameter(string? name, OleDbType dataType) : this()
        {
            ParameterName = name;
            OleDbType = dataType;
        }

        public OleDbParameter(string? name, OleDbType dataType, int size) : this()
        {
            ParameterName = name;
            OleDbType = dataType;
            Size = size;
        }

        public OleDbParameter(string? name, OleDbType dataType, int size, string? srcColumn) : this()
        {
            ParameterName = name;
            OleDbType = dataType;
            Size = size;
            SourceColumn = srcColumn;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public OleDbParameter(string? parameterName,
                              OleDbType dbType, int size,
                              ParameterDirection direction, bool isNullable,
                              byte precision, byte scale,
                              string? srcColumn, DataRowVersion srcVersion,
                              object? value) : this()
        { // V1.0 everything
            ParameterName = parameterName;
            OleDbType = dbType;
            Size = size;
            Direction = direction;
            IsNullable = isNullable;
            PrecisionInternal = precision;
            ScaleInternal = scale;
            SourceColumn = srcColumn;
            SourceVersion = srcVersion;
            Value = value;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public OleDbParameter(string? parameterName,
                              OleDbType dbType, int size,
                              ParameterDirection direction,
                              byte precision, byte scale,
                              string? sourceColumn, DataRowVersion sourceVersion, bool sourceColumnNullMapping,
                              object? value) : this()
        { // V2.0 everything - round trip all browsable properties + precision/scale
            ParameterName = parameterName;
            OleDbType = dbType;
            Size = size;
            Direction = direction;
            PrecisionInternal = precision;
            ScaleInternal = scale;
            SourceColumn = sourceColumn;
            SourceVersion = sourceVersion;
            SourceColumnNullMapping = sourceColumnNullMapping;
            Value = value;
        }

        internal int ChangeID
        {
            get
            {
                return _changeID;
            }
        }

        public override DbType DbType
        {
            get
            {
                return GetBindType(Value).enumDbType;
            }
            set
            {
                NativeDBType? dbtype = _metaType;
                if ((null == dbtype) || (dbtype.enumDbType != value))
                {
                    PropertyTypeChanging();
                    _metaType = NativeDBType.FromDbType(value);
                }
            }
        }

        public override void ResetDbType()
        {
            ResetOleDbType();
        }

        [
        RefreshProperties(RefreshProperties.All),
        DbProviderSpecificTypeProperty(true),
        ]
        public OleDbType OleDbType
        {
            get
            {
                return GetBindType(Value).enumOleDbType;
            }
            set
            {
                NativeDBType? dbtype = _metaType;
                if ((null == dbtype) || (dbtype.enumOleDbType != value))
                {
                    PropertyTypeChanging();
                    _metaType = NativeDBType.FromDataType(value);
                }
            }
        }

        private bool ShouldSerializeOleDbType()
        {
            return (null != _metaType);
        }

        public void ResetOleDbType()
        {
            if (null != _metaType)
            {
                PropertyTypeChanging();
                _metaType = null;
            }
        }

        [AllowNull]
        public override string ParameterName
        { // V1.2.3300, XXXParameter V1.0.3300
            get
            {
                string? parameterName = _parameterName;
                return ((null != parameterName) ? parameterName : string.Empty);
            }
            set
            {
                if (_parameterName != value)
                {
                    PropertyChanging();
                    _parameterName = value;
                }
            }
        }

        [DefaultValue((byte)0)]
        public new byte Precision
        {
            get
            {
                return PrecisionInternal;
            }
            set
            {
                PrecisionInternal = value;
            }
        }
        internal byte PrecisionInternal
        {
            get
            {
                byte precision = _precision;
                if (0 == precision)
                {
                    precision = ValuePrecision(Value);
                }
                return precision;
            }
            set
            {
                if (_precision != value)
                {
                    PropertyChanging();
                    _precision = value;
                }
            }
        }
        private bool ShouldSerializePrecision()
        {
            return (0 != _precision);
        }

        [DefaultValue((byte)0)]
        public new byte Scale
        {
            get
            {
                return ScaleInternal;
            }
            set
            {
                ScaleInternal = value;
            }
        }
        internal byte ScaleInternal
        {
            get
            {
                byte scale = _scale;
                if (!ShouldSerializeScale(scale))
                {
                    scale = ValueScale(Value);
                }
                return scale;
            }
            set
            {
                if (_scale != value || !_hasScale)
                {
                    PropertyChanging();
                    _scale = value;
                    _hasScale = true;
                }
            }
        }
        private bool ShouldSerializeScale()
        {
            return ShouldSerializeScale(_scale);
        }

        private bool ShouldSerializeScale(byte scale)
        {
            return _hasScale && ((0 != scale) || ShouldSerializePrecision());
        }

        object ICloneable.Clone()
        {
            return new OleDbParameter(this);
        }

        private void CloneHelper(OleDbParameter destination)
        {
            CloneHelperCore(destination);
            destination._metaType = _metaType;
            destination._parameterName = _parameterName;
            destination._precision = _precision;
            destination._scale = _scale;
            destination._hasScale = _hasScale;
        }

        private void PropertyChanging()
        {
            unchecked
            { _changeID++; }
        }

        private void PropertyTypeChanging()
        {
            PropertyChanging();
            _coerceMetaType = null;
            CoercedValue = null;
        }

        // goal: call virtual property getters only once per parameter
        internal bool BindParameter(int index, Bindings bindings)
        {
            int changeID = _changeID;
            object? value = Value;

            NativeDBType dbtype = GetBindType(value);
            if (OleDbType.Empty == dbtype.enumOleDbType)
            {
                throw ODB.UninitializedParameters(index, dbtype.enumOleDbType);
            }
            _coerceMetaType = dbtype;
            value = CoerceValue(value, dbtype);
            CoercedValue = value;

            ParameterDirection direction = Direction;

            byte precision;
            if (ShouldSerializePrecision())
            {
                precision = PrecisionInternal;
            }
            else
            {
                precision = ValuePrecision(value);
            }
            if (0 == precision)
            {
                precision = dbtype.maxpre;
            }

            byte scale;
            if (ShouldSerializeScale())
            {
                scale = ScaleInternal;
            }
            else
            {
                scale = ValueScale(value);
            }

            int wtype = dbtype.wType;
            int bytecount, size;

            if (dbtype.islong)
            { // long data (image, text, ntext)
                bytecount = ADP.PtrSize;
                if (ShouldSerializeSize())
                {
                    size = Size;
                }
                else
                {
                    if (NativeDBType.STR == dbtype.dbType)
                    {
                        size = int.MaxValue;
                    }
                    else if (NativeDBType.WSTR == dbtype.dbType)
                    {
                        size = int.MaxValue / 2;
                    }
                    else
                    {
                        size = int.MaxValue;
                    }
                }
                wtype |= NativeDBType.BYREF;
            }
            else if (dbtype.IsVariableLength)
            { // variable length data (varbinary, varchar, nvarchar)
                if (!ShouldSerializeSize() && ADP.IsDirection(this, ParameterDirection.Output))
                {
                    throw ADP.UninitializedParameterSize(index, _coerceMetaType.dataType!);
                }

                bool computedSize;
                if (ShouldSerializeSize())
                {
                    size = Size;
                    computedSize = false;
                }
                else
                {
                    size = ValueSize(value);
                    computedSize = true;
                }
                if (0 < size)
                {
                    if (NativeDBType.WSTR == dbtype.wType)
                    {
                        // maximum 0x3FFFFFFE characters, computed this way to avoid overflow exception
                        bytecount = Math.Min(size, 0x3FFFFFFE) * 2 + 2;
                    }
                    else
                    {
                        Debug.Assert(NativeDBType.STR != dbtype.wType, "should have ANSI binding, describing is okay");
                        bytecount = size;
                    }

                    if (computedSize)
                    {
                        if (NativeDBType.STR == dbtype.dbType)
                        {
                            // maximum 0x7ffffffe characters, computed this way to avoid overflow exception
                            size = Math.Min(size, 0x3FFFFFFE) * 2;
                        }
                    }

                    if (ODB.LargeDataSize < bytecount)
                    {
                        bytecount = ADP.PtrSize;
                        wtype |= NativeDBType.BYREF;
                    }
                }
                else if (0 == size)
                {
                    if (NativeDBType.WSTR == wtype)
                    { // allow space for null termination character
                        bytecount = 2;
                        // 0 == size, okay for (STR == dbType)
                    }
                    else
                    {
                        Debug.Assert(NativeDBType.STR != dbtype.wType, "should have ANSI binding, describing is okay");
                        bytecount = 0;
                    }
                }
                else if (-1 == size)
                {
                    bytecount = ADP.PtrSize;
                    wtype |= NativeDBType.BYREF;
                }
                else
                {
                    throw ADP.InvalidSizeValue(size);
                }
            }
            else
            { // fixed length data
                bytecount = dbtype.fixlen;
                size = bytecount;
            }
            bindings.CurrentIndex = index;

            // tagDBPARAMBINDINFO info for SetParameterInfo
            bindings.DataSourceType = dbtype.dbString.DangerousGetHandle(); // NOTE: This is a constant and isn't exposed publicly, so there really isn't a potential for Handle Recycling.
            bindings.Name = ADP.PtrZero;
            bindings.ParamSize = new IntPtr(size);
            bindings.Flags = GetBindFlags(direction);
            //bindings.Precision    = precision;
            //bindings.Scale        = scale;

            // tagDBBINDING info for CreateAccessor
            bindings.Ordinal = (IntPtr)(index + 1);
            bindings.Part = dbtype.dbPart;
            bindings.ParamIO = GetBindDirection(direction);
            bindings.Precision = precision;
            bindings.Scale = scale;
            bindings.DbType = wtype;
            bindings.MaxLen = bytecount; // also increments databuffer size (uses DbType)
                                         //bindings.ValueOffset  = bindings.DataBufferSize; // set via MaxLen
                                         //bindings.LengthOffset = i * sizeof_int64;
                                         //bindings.StatusOffset = i * sizeof_int64 + sizeof_int32;
                                         //bindings.TypeInfoPtr  = 0;
                                         //bindings.ObjectPtr    = 0;
                                         //bindings.BindExtPtr   = 0;
                                         //bindings.MemOwner     = /*DBMEMOWNER_CLIENTOWNED*/0;
                                         //bindings.Flags        = 0;

            //bindings.ParameterChangeID = changeID; // bind until something changes
            Debug.Assert(_changeID == changeID, "parameter has unexpectedly changed");

            return IsParameterComputed();
        }

        private static object? CoerceValue(object? value, NativeDBType destinationType)
        {
            Debug.Assert(null != destinationType, "null destinationType");
            if ((null != value) && (DBNull.Value != value) && (typeof(object) != destinationType.dataType))
            {
                Type currentType = value.GetType();
                if (currentType != destinationType.dataType)
                {
                    try
                    {
                        if ((typeof(string) == destinationType.dataType) && (typeof(char[]) == currentType))
                        {
                        }
                        else if ((NativeDBType.CY == destinationType.dbType) && (typeof(string) == currentType))
                        {
                            value = decimal.Parse((string)value, NumberStyles.Currency, null);
                        }
                        else
                        {
                            value = Convert.ChangeType(value, destinationType.dataType!, null);
                        }
                    }
                    catch (Exception e)
                    {
                        // UNDONE - should not be catching all exceptions!!!
                        if (!ADP.IsCatchableExceptionType(e))
                        {
                            throw;
                        }

                        throw ADP.ParameterConversionFailed(value, destinationType.dataType!, e);
                    }
                }
            }
            return value;
        }

        private NativeDBType GetBindType(object? value)
        {
            NativeDBType? dbtype = _metaType;
            if (null == dbtype)
            {
                if (ADP.IsNull(value))
                {
                    dbtype = OleDb.NativeDBType.Default;
                }
                else
                {
                    dbtype = NativeDBType.FromSystemType(value);
                }
            }
            return dbtype;
        }

        internal object? GetCoercedValue()
        {
            object? value = CoercedValue; // will also be set during binding, will rebind everytime if _metaType not set
            if (null == value)
            {
                value = CoerceValue(Value, _coerceMetaType!);
                CoercedValue = value;
            }
            return value;
        }

        internal bool IsParameterComputed()
        {
            NativeDBType? metaType = _metaType;
            return ((null == metaType)
                    || (!ShouldSerializeSize() && metaType.IsVariableLength)
                    || ((NativeDBType.DECIMAL == metaType.dbType) || (NativeDBType.NUMERIC == metaType.dbType)
                        && (!ShouldSerializeScale() || !ShouldSerializePrecision())
                        )
                    );
        }

        // @devnote: use IsParameterComputed which is called in the normal case
        // only to call Prepare to throw the specialized error message
        // reducing the overall number of methods to actually jit
        internal void Prepare(OleDbCommand cmd)
        {
            Debug.Assert(IsParameterComputed(), "Prepare computed parameter");
            if (null == _metaType)
            {
                throw ADP.PrepareParameterType(cmd);
            }
            else if (!ShouldSerializeSize() && _metaType.IsVariableLength)
            {
                throw ADP.PrepareParameterSize(cmd);
            }
            else if (!ShouldSerializePrecision() && !ShouldSerializeScale() && ((NativeDBType.DECIMAL == _metaType.wType) || (NativeDBType.NUMERIC == _metaType.wType)))
            {
                throw ADP.PrepareParameterScale(cmd, _metaType.wType.ToString("G", CultureInfo.InvariantCulture));
            }
        }

        [
        RefreshProperties(RefreshProperties.All),
        TypeConverter(typeof(StringConverter)),
        ]
        public override object? Value
        { // V1.2.3300, XXXParameter V1.0.3300
            get
            {
                return _value;
            }
            set
            {
                _coercedValue = null;
                _value = value;
            }
        }

        private byte ValuePrecision(object? value)
        {
            return ValuePrecisionCore(value);
        }

        private byte ValueScale(object? value)
        {
            return ValueScaleCore(value);
        }

        private int ValueSize(object? value)
        {
            return ValueSizeCore(value);
        }

        private static int GetBindDirection(ParameterDirection direction)
        {
            return (ODB.ParameterDirectionFlag & (int)direction);
            /*switch(Direction) {
            default:
            case ParameterDirection.Input:
                return ODB.DBPARAMIO_INPUT;
            case ParameterDirection.Output:
            case ParameterDirection.ReturnValue:
                return ODB.DBPARAMIO_OUTPUT;
            case ParameterDirection.InputOutput:
                return (ODB.DBPARAMIO_INPUT | ODB.DBPARAMIO_OUTPUT);
            }*/
        }

        private static int GetBindFlags(ParameterDirection direction)
        {
            return (ODB.ParameterDirectionFlag & (int)direction);
            /*switch(Direction) {
            default:
            case ParameterDirection.Input:
                return ODB.DBPARAMFLAGS_ISINPUT;
            case ParameterDirection.Output:
            case ParameterDirection.ReturnValue:
                return ODB.DBPARAMFLAGS_ISOUTPUT;
            case ParameterDirection.InputOutput:
                return (ODB.DBPARAMFLAGS_ISINPUT | ODB.DBPARAMFLAGS_ISOUTPUT);
            }*/
        }

        // implemented as nested class to take advantage of the private/protected ShouldSerializeXXX methods
        internal sealed class OleDbParameterConverter : System.ComponentModel.ExpandableObjectConverter
        {
            // converter classes should have public ctor
            public OleDbParameterConverter()
            {
            }

            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            {
                if (typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor) == destinationType)
                {
                    return true;
                }
                return base.CanConvertTo(context, destinationType);
            }

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (null == destinationType)
                {
                    throw ADP.ArgumentNull("destinationType");
                }
                if ((typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor) == destinationType) && (value is OleDbParameter parameter))
                {
                    return ConvertToInstanceDescriptor(parameter);
                }
                return base.ConvertTo(context, culture, value, destinationType);
            }

            private System.ComponentModel.Design.Serialization.InstanceDescriptor ConvertToInstanceDescriptor(OleDbParameter p)
            {
                int flags = 0;

                if (p.ShouldSerializeOleDbType())
                {
                    flags |= 1;
                }
                if (p.ShouldSerializeSize())
                {
                    flags |= 2;
                }
                if (!ADP.IsEmpty(p.SourceColumn))
                {
                    flags |= 4;
                }
                if (null != p.Value)
                {
                    flags |= 8;
                }
                if ((ParameterDirection.Input != p.Direction) || p.IsNullable
                    || p.ShouldSerializePrecision() || p.ShouldSerializeScale()
                    || (DataRowVersion.Current != p.SourceVersion))
                {
                    flags |= 16; // V1.0 everything
                }
                if (p.SourceColumnNullMapping)
                {
                    flags |= 32; // v2.0 everything
                }

                Type[] ctorParams;
                object?[] ctorValues;
                switch (flags)
                {
                    case 0: // ParameterName
                    case 1: // OleDbType
                        ctorParams = new Type[] { typeof(string), typeof(OleDbType) };
                        ctorValues = new object[] { p.ParameterName, p.OleDbType };
                        break;
                    case 2: // Size
                    case 3: // Size, OleDbType
                        ctorParams = new Type[] { typeof(string), typeof(OleDbType), typeof(int) };
                        ctorValues = new object[] { p.ParameterName, p.OleDbType, p.Size };
                        break;
                    case 4: // SourceColumn
                    case 5: // SourceColumn, OleDbType
                    case 6: // SourceColumn, Size
                    case 7: // SourceColumn, Size, OleDbType
                        ctorParams = new Type[] { typeof(string), typeof(OleDbType), typeof(int), typeof(string) };
                        ctorValues = new object[] { p.ParameterName, p.OleDbType, p.Size, p.SourceColumn };
                        break;
                    case 8: // Value
                        ctorParams = new Type[] { typeof(string), typeof(object) };
                        ctorValues = new object?[] { p.ParameterName, p.Value };
                        break;
                    default: // everything else
                        if (0 == (32 & flags))
                        { // V1.0 everything
                            ctorParams = new Type[] {
                            typeof(string), typeof(OleDbType), typeof(int), typeof(ParameterDirection),
                            typeof(bool), typeof(byte), typeof(byte), typeof(string),
                            typeof(DataRowVersion), typeof(object) };
                            ctorValues = new object?[] {
                            p.ParameterName, p.OleDbType,  p.Size, p.Direction,
                            p.IsNullable, p.PrecisionInternal, p.ScaleInternal, p.SourceColumn,
                            p.SourceVersion, p.Value };
                        }
                        else
                        { // v2.0 everything - round trip all browsable properties + precision/scale
                            ctorParams = new Type[] {
                            typeof(string), typeof(OleDbType), typeof(int), typeof(ParameterDirection),
                            typeof(byte), typeof(byte),
                            typeof(string), typeof(DataRowVersion), typeof(bool),
                            typeof(object) };
                            ctorValues = new object?[] {
                            p.ParameterName, p.OleDbType,  p.Size, p.Direction,
                            p.PrecisionInternal, p.ScaleInternal,
                            p.SourceColumn, p.SourceVersion, p.SourceColumnNullMapping,
                            p.Value };
                        }
                        break;
                }
                System.Reflection.ConstructorInfo ctor = typeof(OleDbParameter).GetConstructor(ctorParams)!;
                return new System.ComponentModel.Design.Serialization.InstanceDescriptor(ctor, ctorValues);
            }
        }
    }
}
