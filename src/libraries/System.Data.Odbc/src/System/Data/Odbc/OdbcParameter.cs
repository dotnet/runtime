// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Data.Odbc
{
    public sealed partial class OdbcParameter : DbParameter, ICloneable, IDataParameter, IDbDataParameter
    {
        private bool _hasChanged;
        private bool _userSpecifiedType;

        // _typemap     User explicit set type  or  default parameter type
        // _infertpe    _typemap if the user explicitly sets type
        //              otherwise it is infered from the value
        // _bindtype    The actual type used for binding. E.g. string substitutes numeric
        //
        // set_DbType:      _bindtype = _infertype = _typemap = TypeMap.FromDbType(value)
        // set_OdbcType:    _bindtype = _infertype = _typemap = TypeMap.FromOdbcType(value)
        //
        // GetParameterType:    If _typemap != _infertype AND value != 0
        //                      _bindtype = _infertype = TypeMap.FromSystemType(value.GetType());
        //                      otherwise
        //                      _bindtype = _infertype
        //
        // Bind:            Bind may change _bindtype if the type is not supported through the driver
        //

        private TypeMap? _typemap;
        private TypeMap? _bindtype;

        private string? _parameterName;
        private byte _precision;
        private byte _scale;
        private bool _hasScale;


        private ODBC32.SQL_C _boundSqlCType;
        private ODBC32.SQL_TYPE _boundParameterType;       // if we bound already that is the type we used
        private int _boundSize;
        private int _boundScale;
        private IntPtr _boundBuffer;
        private IntPtr _boundIntbuffer;
        private TypeMap? _originalbindtype;         // the original type in case we had to change the bindtype
                                                   // (e.g. decimal to string)
        private byte _internalPrecision;
        private bool _internalShouldSerializeSize;
        private int _internalSize;
        private ParameterDirection _internalDirection;
        private byte _internalScale;
        private int _internalOffset;
        internal bool _internalUserSpecifiedType;
        private object? _internalValue;

        private int _preparedOffset;
        private int _preparedSize;
        private int _preparedBufferSize;
        private object? _preparedValue;
        private int _preparedIntOffset;
        private int _preparedValueOffset;

        private ODBC32.SQL_C _prepared_Sql_C_Type;

        public OdbcParameter() : base()
        {
            // uses System.Threading!
        }

        public OdbcParameter(string? name, object? value) : this()
        {
            ParameterName = name;
            Value = value;
        }

        public OdbcParameter(string? name, OdbcType type) : this()
        {
            ParameterName = name;
            OdbcType = type;
        }

        public OdbcParameter(string? name, OdbcType type, int size) : this()
        {
            ParameterName = name;
            OdbcType = type;
            Size = size;
        }

        public OdbcParameter(string? name, OdbcType type, int size, string? sourcecolumn) : this()
        {
            ParameterName = name;
            OdbcType = type;
            Size = size;
            SourceColumn = sourcecolumn;
        }


        [EditorBrowsableAttribute(EditorBrowsableState.Advanced)] // MDAC 69508
        public OdbcParameter(string? parameterName,
                             OdbcType odbcType,
                             int size,
                             ParameterDirection parameterDirection,
                             bool isNullable,
                             byte precision,
                             byte scale,
                             string? srcColumn,
                             DataRowVersion srcVersion,
                             object? value
                             ) : this()
        { // V1.0 everything
            this.ParameterName = parameterName;
            this.OdbcType = odbcType;
            this.Size = size;
            this.Direction = parameterDirection;
            this.IsNullable = isNullable;
            PrecisionInternal = precision;
            ScaleInternal = scale;
            this.SourceColumn = srcColumn;
            this.SourceVersion = srcVersion;
            this.Value = value;
        }

        [EditorBrowsableAttribute(EditorBrowsableState.Advanced)] // MDAC 69508
        public OdbcParameter(string? parameterName,
                                 OdbcType odbcType, int size,
                                 ParameterDirection parameterDirection,
                                 byte precision, byte scale,
                                 string? sourceColumn, DataRowVersion sourceVersion, bool sourceColumnNullMapping,
                                 object? value) : this()
        { // V2.0 everything - round trip all browsable properties + precision/scale
            this.ParameterName = parameterName;
            this.OdbcType = odbcType;
            this.Size = size;
            this.Direction = parameterDirection;
            this.PrecisionInternal = precision;
            this.ScaleInternal = scale;
            this.SourceColumn = sourceColumn;
            this.SourceVersion = sourceVersion;
            this.SourceColumnNullMapping = sourceColumnNullMapping;
            this.Value = value;
        }

        public override System.Data.DbType DbType
        {
            get
            {
                if (_userSpecifiedType)
                {
                    return _typemap!._dbType;
                }
                return TypeMap._NVarChar._dbType; // default type
            }
            set
            {
                if ((null == _typemap) || (_typemap._dbType != value))
                {
                    PropertyTypeChanging();
                    _typemap = TypeMap.FromDbType(value);
                    _userSpecifiedType = true;
                }
            }
        }

        public override void ResetDbType()
        {
            ResetOdbcType();
        }

        [
        DefaultValue(OdbcType.NChar),
        System.Data.Common.DbProviderSpecificTypePropertyAttribute(true),
        ]
        public OdbcType OdbcType
        {
            get
            {
                if (_userSpecifiedType)
                {
                    return _typemap!._odbcType;
                }
                return TypeMap._NVarChar._odbcType; // default type
            }
            set
            {
                if ((null == _typemap) || (_typemap._odbcType != value))
                {
                    PropertyTypeChanging();
                    _typemap = TypeMap.FromOdbcType(value);
                    _userSpecifiedType = true;
                }
            }
        }

        public void ResetOdbcType()
        {
            PropertyTypeChanging();
            _typemap = null;
            _userSpecifiedType = false;
        }

        internal bool HasChanged
        {
            set
            {
                _hasChanged = value;
            }
        }

        internal bool UserSpecifiedType
        {
            get
            {
                return _userSpecifiedType;
            }
        }

        [AllowNull]
        public override string ParameterName
        { // V1.2.3300, XXXParameter V1.0.3300
            get
            {
                return _parameterName ?? string.Empty;
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
                { // WebData 94688
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

        // returns the count of bytes for the data (ColumnSize argument to SqlBindParameter)
        private int GetColumnSize(object? value, int offset, int ordinal)
        {
            if ((ODBC32.SQL_C.NUMERIC == _bindtype!._sql_c) && (0 != _internalPrecision))
            {
                return Math.Min((int)_internalPrecision, ADP.DecimalMaxPrecision);
            }
            int cch = _bindtype._columnSize;
            if (0 >= cch)
            {
                if (ODBC32.SQL_C.NUMERIC == _typemap!._sql_c)
                {
                    cch = 62;  // (DecimalMaxPrecision+sign+terminator)*BytesPerUnicodeCharacter
                }
                else
                {
                    cch = _internalSize;
                    if (!_internalShouldSerializeSize || 0x3fffffff <= cch || cch < 0)
                    {
                        Debug.Assert((ODBC32.SQL_C.WCHAR == _bindtype._sql_c) || (ODBC32.SQL_C.BINARY == _bindtype._sql_c), "not wchar or binary");
                        if (!_internalShouldSerializeSize && (0 != (ParameterDirection.Output & _internalDirection)))
                        {
                            throw ADP.UninitializedParameterSize(ordinal, _bindtype._type);
                        }
                        if ((null == value) || Convert.IsDBNull(value))
                        {
                            cch = 0;
                        }
                        else if (value is string)
                        {
                            cch = ((string)value).Length - offset;

                            if ((0 != (ParameterDirection.Output & _internalDirection)) && (0x3fffffff <= _internalSize))
                            {
                                // restrict output parameters when user set Size to Int32.MaxValue
                                // to the greater of intput size or 8K
                                cch = Math.Max(cch, 4 * 1024); // MDAC 69224
                            }


                            // the following code causes failure against SQL 6.5
                            // ERROR [HY104] [Microsoft][ODBC SQL Server Driver]Invalid precision value
                            //
                            // the code causes failure if it is NOT there (remark added by [....])
                            // it causes failure with jet if it is there
                            //
                            // MDAC 76227: Code is required for japanese client/server tests.
                            // If this causes regressions with Jet please doc here including bug#. ([....])
                            //
                            if ((ODBC32.SQL_TYPE.CHAR == _bindtype._sql_type)
                                || (ODBC32.SQL_TYPE.VARCHAR == _bindtype._sql_type)
                                || (ODBC32.SQL_TYPE.LONGVARCHAR == _bindtype._sql_type))
                            {
                                cch = System.Text.Encoding.Default.GetMaxByteCount(cch);
                            }
                        }
                        else if (value is char[])
                        {
                            cch = ((char[])value).Length - offset;
                            if ((0 != (ParameterDirection.Output & _internalDirection)) && (0x3fffffff <= _internalSize))
                            {
                                cch = Math.Max(cch, 4 * 1024); // MDAC 69224
                            }
                            if ((ODBC32.SQL_TYPE.CHAR == _bindtype._sql_type)
                                || (ODBC32.SQL_TYPE.VARCHAR == _bindtype._sql_type)
                                || (ODBC32.SQL_TYPE.LONGVARCHAR == _bindtype._sql_type))
                            {
                                cch = System.Text.Encoding.Default.GetMaxByteCount(cch);
                            }
                        }
                        else if (value is byte[])
                        {
                            cch = ((byte[])value).Length - offset;

                            if ((0 != (ParameterDirection.Output & _internalDirection)) && (0x3fffffff <= _internalSize))
                            {
                                // restrict output parameters when user set Size to Int32.MaxValue
                                // to the greater of intput size or 8K
                                cch = Math.Max(cch, 8 * 1024); // MDAC 69224
                            }
                        }
#if DEBUG
                        else { Debug.Fail("not expecting this"); }
#endif
                        // Note: ColumnSize should never be 0,
                        // this represents the size of the column on the backend.
                        //
                        // without the following code causes failure
                        //ERROR [HY104] [Microsoft][ODBC Microsoft Access Driver]Invalid precision value
                        cch = Math.Max(2, cch);
                    }
                }
            }
            Debug.Assert((0 <= cch) && (cch < 0x3fffffff), $"GetColumnSize: cch = {cch} out of range, _internalShouldSerializeSize = {_internalShouldSerializeSize}, _internalSize = {_internalSize}");
            return cch;
        }



        // Return the count of bytes for the data (size in bytes for the native buffer)
        //
        private int GetValueSize(object? value, int offset)
        {
            if ((ODBC32.SQL_C.NUMERIC == _bindtype!._sql_c) && (0 != _internalPrecision))
            {
                return Math.Min((int)_internalPrecision, ADP.DecimalMaxPrecision);
            }
            int cch = _bindtype._columnSize;
            if (0 >= cch)
            {
                bool twobytesperunit = false;
                if (value is string)
                {
                    cch = ((string)value).Length - offset;
                    twobytesperunit = true;
                }
                else if (value is char[])
                {
                    cch = ((char[])value).Length - offset;
                    twobytesperunit = true;
                }
                else if (value is byte[])
                {
                    cch = ((byte[])value).Length - offset;
                }
                else
                {
                    cch = 0;
                }
                if (_internalShouldSerializeSize && (_internalSize >= 0) && (_internalSize < cch) && (_bindtype == _originalbindtype))
                {
                    cch = _internalSize;
                }
                if (twobytesperunit)
                {
                    cch *= 2;
                }
            }
            Debug.Assert((0 <= cch) && (cch < 0x3fffffff), $"GetValueSize: cch = {cch} out of range, _internalShouldSerializeSize = {_internalShouldSerializeSize}, _internalSize = {_internalSize}");
            return cch;
        }

        // return the count of bytes for the data, used for SQLBindParameter
        //
        private int GetParameterSize(object? value, int offset, int ordinal)
        {
            int ccb = _bindtype!._bufferSize;
            if (0 >= ccb)
            {
                if (ODBC32.SQL_C.NUMERIC == _typemap!._sql_c)
                {
                    ccb = 518; // _bindtype would be VarChar ([0-9]?{255} + '-' + '.') * 2
                }
                else
                {
                    ccb = _internalSize;
                    if (!_internalShouldSerializeSize || (0x3fffffff <= ccb) || (ccb < 0))
                    {
                        Debug.Assert((ODBC32.SQL_C.WCHAR == _bindtype._sql_c) || (ODBC32.SQL_C.BINARY == _bindtype._sql_c), "not wchar or binary");
                        if ((ccb <= 0) && (0 != (ParameterDirection.Output & _internalDirection)))
                        {
                            throw ADP.UninitializedParameterSize(ordinal, _bindtype._type);
                        }
                        if ((null == value) || Convert.IsDBNull(value))
                        {
                            if (_bindtype._sql_c == ODBC32.SQL_C.WCHAR)
                            {
                                ccb = 2; // allow for null termination
                            }
                            else
                            {
                                ccb = 0;
                            }
                        }
                        else if (value is string)
                        {
                            ccb = (((string)value).Length - offset) * 2 + 2;
                        }
                        else if (value is char[])
                        {
                            ccb = (((char[])value).Length - offset) * 2 + 2;
                        }
                        else if (value is byte[])
                        {
                            ccb = ((byte[])value).Length - offset;
                        }
#if DEBUG
                        else { Debug.Fail("not expecting this"); }
#endif
                        if ((0 != (ParameterDirection.Output & _internalDirection)) && (0x3fffffff <= _internalSize))
                        {
                            // restrict output parameters when user set Size to Int32.MaxValue
                            // to the greater of intput size or 8K
                            ccb = Math.Max(ccb, 8 * 1024); // MDAC 69224
                        }
                    }
                    else if (ODBC32.SQL_C.WCHAR == _bindtype._sql_c)
                    {
                        if ((value is string) && (ccb < ((string)value).Length) && (_bindtype == _originalbindtype))
                        {
                            // silently truncate ... MDAC 84408 ... do not truncate upgraded values ... MDAC 84706
                            ccb = ((string)value).Length;
                        }
                        ccb = (ccb * 2) + 2; // allow for null termination
                    }
                    else if ((value is byte[]) && (ccb < ((byte[])value).Length) && (_bindtype == _originalbindtype))
                    {
                        // silently truncate ... MDAC 84408 ... do not truncate upgraded values ... MDAC 84706
                        ccb = ((byte[])value).Length;
                    }
                }
            }
            Debug.Assert((0 <= ccb) && (ccb < 0x3fffffff), $"GetParameterSize: out of range {ccb}");
            return ccb;
        }

        private byte GetParameterPrecision(object? value)
        {
            if (0 != _internalPrecision && value is decimal)
            {
                // from qfe 762
                if (_internalPrecision < 29)
                {
                    // from SqlClient ...
                    if (_internalPrecision != 0)
                    {
                        // devnote: If the userspecified precision (_internalPrecision) is less than the actual values precision
                        // we silently adjust the userspecified precision to the values precision.
                        byte precision = ((SqlDecimal)(decimal)value).Precision;
                        _internalPrecision = Math.Max(_internalPrecision, precision);   // silently adjust the precision
                    }
                    return _internalPrecision;
                }
                return ADP.DecimalMaxPrecision;
            }
            if ((null == value) || (value is decimal) || Convert.IsDBNull(value))
            { // MDAC 60882
                return ADP.DecimalMaxPrecision28;
            }
            return 0;
        }


        private byte GetParameterScale(object? value)
        {
            // For any value that is not decimal simply return the Scale
            //
            if (!(value is decimal))
            {
                return _internalScale;
            }

            // Determin the values scale
            // If the user specified a lower scale we return the user specified scale,
            // otherwise the values scale
            //
            byte s = (byte)((decimal.GetBits((decimal)value)[3] & 0x00ff0000) >> 0x10);
            if ((_internalScale > 0) && (_internalScale < s))
            {
                return _internalScale;
            }
            return s;
        }

        //This is required for OdbcCommand.Clone to deep copy the parameters collection
        object ICloneable.Clone()
        {
            return new OdbcParameter(this);
        }

        private void CopyParameterInternal()
        {
            _internalValue = Value;
            // we should coerce the parameter value at this time.
            _internalPrecision = ShouldSerializePrecision() ? PrecisionInternal : ValuePrecision(_internalValue);
            _internalShouldSerializeSize = ShouldSerializeSize();
            _internalSize = _internalShouldSerializeSize ? Size : ValueSize(_internalValue);
            _internalDirection = Direction;
            _internalScale = ShouldSerializeScale() ? ScaleInternal : ValueScale(_internalValue);
            _internalOffset = Offset;
            _internalUserSpecifiedType = UserSpecifiedType;
        }

        private void CloneHelper(OdbcParameter destination)
        {
            CloneHelperCore(destination);
            destination._userSpecifiedType = _userSpecifiedType;
            destination._typemap = _typemap;
            destination._parameterName = _parameterName;
            destination._precision = _precision;
            destination._scale = _scale;
            destination._hasScale = _hasScale;
        }

        internal void ClearBinding()
        {
            if (!_userSpecifiedType)
            {
                _typemap = null;
            }
            _bindtype = null;
        }

        internal void PrepareForBind(OdbcCommand command, short ordinal, ref int parameterBufferSize)
        {
            Debug.Assert(command.Connection != null);

            // make a snapshot of the current properties. Properties may change while we work on them
            //
            CopyParameterInternal();

            object? value = ProcessAndGetParameterValue();
            int offset = _internalOffset;
            int size = _internalSize;
            ODBC32.SQL_C sql_c_type;


            // offset validation based on the values type
            //
            if (offset > 0)
            {
                if (value is string)
                {
                    if (offset > ((string)value).Length)
                    {
                        throw ADP.OffsetOutOfRangeException();
                    }
                }
                else if (value is char[])
                {
                    if (offset > ((char[])value).Length)
                    {
                        throw ADP.OffsetOutOfRangeException();
                    }
                }
                else if (value is byte[])
                {
                    if (offset > ((byte[])value).Length)
                    {
                        throw ADP.OffsetOutOfRangeException();
                    }
                }
                else
                {
                    // for all other types offset has no meaning
                    // this is important since we might upgrade some types to strings
                    offset = 0;
                }
            }

            // type support verification for certain data types
            //
            switch (_bindtype!._sql_type)
            {
                case ODBC32.SQL_TYPE.DECIMAL:
                case ODBC32.SQL_TYPE.NUMERIC:
                    if (
                        !command.Connection.IsV3Driver                                      // for non V3 driver we always do the conversion
                        || !command.Connection.TestTypeSupport(ODBC32.SQL_TYPE.NUMERIC)     // otherwise we convert if the driver does not support numeric
                        || command.Connection.TestRestrictedSqlBindType(_bindtype._sql_type)// or the type is not supported
                    )
                    {
                        // No support for NUMERIC
                        // Change the type
                        _bindtype = TypeMap._VarChar;
                        if ((null != value) && !Convert.IsDBNull(value))
                        {
                            value = ((decimal)value).ToString(CultureInfo.CurrentCulture);
                            size = ((string)value).Length;
                            offset = 0;
                        }
                    }
                    break;
                case ODBC32.SQL_TYPE.BIGINT:
                    if (!command.Connection.IsV3Driver)
                    {
                        // No support for BIGINT
                        // Change the type
                        _bindtype = TypeMap._VarChar;
                        if ((null != value) && !Convert.IsDBNull(value))
                        {
                            value = ((long)value).ToString(CultureInfo.CurrentCulture);
                            size = ((string)value).Length;
                            offset = 0;
                        }
                    }
                    break;
                case ODBC32.SQL_TYPE.WCHAR: // MDAC 68993
                case ODBC32.SQL_TYPE.WVARCHAR:
                case ODBC32.SQL_TYPE.WLONGVARCHAR:
                    if (value is char)
                    {
                        value = value.ToString()!;
                        size = ((string)value).Length;
                        offset = 0;
                    }
                    if (!command.Connection.TestTypeSupport(_bindtype._sql_type))
                    {
                        // No support for WCHAR, WVARCHAR or WLONGVARCHAR
                        // Change the type
                        if (ODBC32.SQL_TYPE.WCHAR == _bindtype._sql_type) { _bindtype = TypeMap._Char; }
                        else if (ODBC32.SQL_TYPE.WVARCHAR == _bindtype._sql_type) { _bindtype = TypeMap._VarChar; }
                        else if (ODBC32.SQL_TYPE.WLONGVARCHAR == _bindtype._sql_type)
                        {
                            _bindtype = TypeMap._Text;
                        }
                    }
                    break;
            } // end switch

            // Conversation from WCHAR to CHAR, VARCHAR or LONVARCHAR (AnsiString) is different for some providers
            // we need to chonvert WCHAR to CHAR and bind as sql_c_type = CHAR
            //
            sql_c_type = _bindtype._sql_c;

            if (!command.Connection.IsV3Driver)
            {
                if (sql_c_type == ODBC32.SQL_C.WCHAR)
                {
                    sql_c_type = ODBC32.SQL_C.CHAR;

                    if (null != value)
                    {
                        if (!Convert.IsDBNull(value) && value is string)
                        {
                            int lcid = System.Globalization.CultureInfo.CurrentCulture.LCID;
                            CultureInfo culInfo = new CultureInfo(lcid);
                            Encoding cpe = System.Text.Encoding.GetEncoding(culInfo.TextInfo.ANSICodePage);
                            value = cpe.GetBytes(value.ToString()!);
                            size = ((byte[])value).Length;
                        }
                    }
                }
            };

            int cbParameterSize = GetParameterSize(value, offset, ordinal); // count of bytes for the data, for SQLBindParameter

            // Upgrade input value type if the size of input value is bigger than the max size of the input value type.
            switch (_bindtype._sql_type)
            {
                case ODBC32.SQL_TYPE.VARBINARY:
                    // Max length of VARBINARY is 8,000 of byte array.
                    if (size > 8000)
                    {
                        _bindtype = TypeMap._Image; // will change to LONGVARBINARY
                    }
                    break;
                case ODBC32.SQL_TYPE.VARCHAR:
                    // Max length of VARCHAR is 8,000 of non-unicode characters.
                    if (size > 8000)
                    {
                        _bindtype = TypeMap._Text; // will change to LONGVARCHAR
                    }
                    break;
                case ODBC32.SQL_TYPE.WVARCHAR:
                    // Max length of WVARCHAR (NVARCHAR) is 4,000 of unicode characters.
                    if (size > 4000)
                    {
                        _bindtype = TypeMap._NText; // will change to WLONGVARCHAR
                    }
                    break;
            }

            _prepared_Sql_C_Type = sql_c_type;
            _preparedOffset = offset;
            _preparedSize = size;
            _preparedValue = value;
            _preparedBufferSize = cbParameterSize;
            _preparedIntOffset = parameterBufferSize;
            _preparedValueOffset = _preparedIntOffset + IntPtr.Size;
            parameterBufferSize += (cbParameterSize + IntPtr.Size);
        }

        internal void Bind(OdbcStatementHandle hstmt, OdbcCommand command, short ordinal, CNativeBuffer parameterBuffer, bool allowReentrance)
        {
            Debug.Assert(command.Connection != null);

            ODBC32.SQLRETURN retcode;
            ODBC32.SQL_C sql_c_type = _prepared_Sql_C_Type;
            ODBC32.SQL_PARAM sqldirection = SqlDirectionFromParameterDirection();

            int offset = _preparedOffset;
            int size = _preparedSize;
            object? value = _preparedValue;
            int cbValueSize = GetValueSize(value, offset);             // count of bytes for the data
            int cchSize = GetColumnSize(value, offset, ordinal);   // count of bytes for the data, used to allocate the buffer length
            byte precision = GetParameterPrecision(value);
            byte scale = GetParameterScale(value);
            int cbActual;

            HandleRef valueBuffer = parameterBuffer.PtrOffset(_preparedValueOffset, _preparedBufferSize);
            HandleRef intBuffer = parameterBuffer.PtrOffset(_preparedIntOffset, IntPtr.Size);

            // for the numeric datatype we need to do some special case handling ...
            //
            if (ODBC32.SQL_C.NUMERIC == sql_c_type)
            {
                // for input/output parameters we need to adjust the scale of the input value since the convert function in
                // sqlsrv32 takes this scale for the output parameter (possible bug in sqlsrv32?)
                //
                if ((ODBC32.SQL_PARAM.INPUT_OUTPUT == sqldirection) && (value is decimal))
                {
                    if (scale < _internalScale)
                    {
                        while (scale < _internalScale)
                        {
                            value = ((decimal)value) * 10;
                            scale++;
                        }
                    }
                }
                SetInputValue(value, sql_c_type, cbValueSize, precision, 0, parameterBuffer);

                // for output parameters we need to write precision and scale to the buffer since the convert function in
                // sqlsrv32 expects these values there (possible bug in sqlsrv32?)
                //
                if (ODBC32.SQL_PARAM.INPUT != sqldirection)
                {
                    parameterBuffer.WriteInt16(_preparedValueOffset, (short)(((ushort)scale << 8) | (ushort)precision));
                }
            }
            else
            {
                SetInputValue(value, sql_c_type, cbValueSize, size, offset, parameterBuffer);
            }


            // Try to reuse existing bindings if
            //  the binding is valid (means we already went through binding all parameters)
            //  the parametercollection is bound already
            //  the bindtype ParameterType did not change (forced upgrade)

            if (!_hasChanged
                && (_boundSqlCType == sql_c_type)
                && (_boundParameterType == _bindtype!._sql_type)
                && (_boundSize == cchSize)
                && (_boundScale == scale)
                && (_boundBuffer == valueBuffer.Handle)
                && (_boundIntbuffer == intBuffer.Handle)
            )
            {
                return;
            }

            //SQLBindParameter
            retcode = hstmt.BindParameter(
                                    ordinal,                    // Parameter Number
                                    (short)sqldirection,        // InputOutputType
                                    sql_c_type,                 // ValueType
                                    _bindtype!._sql_type,        // ParameterType
                                    (IntPtr)cchSize,            // ColumnSize
                                    (IntPtr)scale,              // DecimalDigits
                                    valueBuffer,                // ParameterValuePtr
                                    (IntPtr)_preparedBufferSize,
                                    intBuffer);                 // StrLen_or_IndPtr

            if (ODBC32.SQLRETURN.SUCCESS != retcode)
            {
                if ("07006" == command.GetDiagSqlState())
                {
                    command.Connection.FlagRestrictedSqlBindType(_bindtype._sql_type);
                    if (allowReentrance)
                    {
                        this.Bind(hstmt, command, ordinal, parameterBuffer, false);
                        return;
                    }
                }
                command.Connection.HandleError(hstmt, retcode);
            }
            _hasChanged = false;
            _boundSqlCType = sql_c_type;
            _boundParameterType = _bindtype._sql_type;
            _boundSize = cchSize;
            _boundScale = scale;
            _boundBuffer = valueBuffer.Handle;
            _boundIntbuffer = intBuffer.Handle;

            if (ODBC32.SQL_C.NUMERIC == sql_c_type)
            {
                OdbcDescriptorHandle hdesc = command.GetDescriptorHandle(ODBC32.SQL_ATTR.APP_PARAM_DESC);
                // descriptor handle is cached on command wrapper, don't release it

                // Set descriptor Type
                //
                //SQLSetDescField(hdesc, i+1, SQL_DESC_TYPE, (void *)SQL_C_NUMERIC, 0);
                retcode = hdesc.SetDescriptionField1(ordinal, ODBC32.SQL_DESC.TYPE, (IntPtr)ODBC32.SQL_C.NUMERIC);

                if (ODBC32.SQLRETURN.SUCCESS != retcode)
                {
                    command.Connection.HandleError(hstmt, retcode);
                }


                // Set precision
                //
                cbActual = (int)precision;
                //SQLSetDescField(hdesc, i+1, SQL_DESC_PRECISION, (void *)precision, 0);
                retcode = hdesc.SetDescriptionField1(ordinal, ODBC32.SQL_DESC.PRECISION, (IntPtr)cbActual);

                if (ODBC32.SQLRETURN.SUCCESS != retcode)
                {
                    command.Connection.HandleError(hstmt, retcode);
                }


                // Set scale
                //
                // SQLSetDescField(hdesc, i+1, SQL_DESC_SCALE,  (void *)llen, 0);
                cbActual = (int)scale;
                retcode = hdesc.SetDescriptionField1(ordinal, ODBC32.SQL_DESC.SCALE, (IntPtr)cbActual);

                if (ODBC32.SQLRETURN.SUCCESS != retcode)
                {
                    command.Connection.HandleError(hstmt, retcode);
                }

                // Set data pointer
                //
                // SQLSetDescField(hdesc, i+1, SQL_DESC_DATA_PTR,  (void *)&numeric, 0);
                retcode = hdesc.SetDescriptionField2(ordinal, ODBC32.SQL_DESC.DATA_PTR, valueBuffer);

                if (ODBC32.SQLRETURN.SUCCESS != retcode)
                {
                    command.Connection.HandleError(hstmt, retcode);
                }
            }
        }

        internal void GetOutputValue(CNativeBuffer parameterBuffer)
        { //Handle any output params
            // No value is available if the user fiddles with the parameters properties
            //
            if (_hasChanged) return;

            if ((null != _bindtype) && (_internalDirection != ParameterDirection.Input))
            {
                TypeMap typemap = _bindtype;
                _bindtype = null;

                int cbActual = (int)parameterBuffer.ReadIntPtr(_preparedIntOffset);
                if (ODBC32.SQL_NULL_DATA == cbActual)
                {
                    Value = DBNull.Value;
                }
                else if ((0 <= cbActual) || (cbActual == ODBC32.SQL_NTS))
                { // safeguard
                    Value = parameterBuffer.MarshalToManaged(_preparedValueOffset, _boundSqlCType, cbActual);

                    if (_boundSqlCType == ODBC32.SQL_C.CHAR)
                    {
                        if ((null != Value) && !Convert.IsDBNull(Value))
                        {
                            int lcid = System.Globalization.CultureInfo.CurrentCulture.LCID;
                            CultureInfo culInfo = new CultureInfo(lcid);
                            Encoding cpe = System.Text.Encoding.GetEncoding(culInfo.TextInfo.ANSICodePage);
                            Value = cpe.GetString((byte[])Value);
                        }
                    }

                    if ((typemap != _typemap) && (null != Value) && !Convert.IsDBNull(Value) && (Value.GetType() != _typemap!._type))
                    {
                        Debug.Assert(ODBC32.SQL_C.NUMERIC == _typemap._sql_c, "unexpected");
                        Value = decimal.Parse((string)Value, System.Globalization.CultureInfo.CurrentCulture);
                    }
                }
            }
        }

        private object? ProcessAndGetParameterValue()
        {
            object? value = _internalValue;
            if (_internalUserSpecifiedType)
            {
                if ((null != value) && !Convert.IsDBNull(value))
                {
                    Type valueType = value.GetType();
                    if (!valueType.IsArray)
                    {
                        if (valueType != _typemap!._type)
                        {
                            try
                            {
                                value = Convert.ChangeType(value, _typemap._type, null);
                            }
                            catch (Exception e)
                            {
                                // Don't know which exception to expect from ChangeType so we filter out the serious ones
                                //
                                if (!ADP.IsCatchableExceptionType(e))
                                {
                                    throw;
                                }
                                throw ADP.ParameterConversionFailed(value, _typemap._type, e); // WebData 75433
                            }
                        }
                    }
                    else if (valueType == typeof(char[]))
                    {
                        value = new string((char[])value);
                    }
                }
            }
            else if (null == _typemap)
            {
                if ((null == value) || Convert.IsDBNull(value))
                {
                    _typemap = TypeMap._NVarChar; // default type
                }
                else
                {
                    Type type = value.GetType();

                    _typemap = TypeMap.FromSystemType(type);
                }
            }
            Debug.Assert(null != _typemap, "GetParameterValue: null _typemap");
            _originalbindtype = _bindtype = _typemap;
            return value;
        }

        private void PropertyChanging()
        {
            _hasChanged = true;
        }

        private void PropertyTypeChanging()
        {
            PropertyChanging();
            //CoercedValue = null;
        }

        internal void SetInputValue(object? value, ODBC32.SQL_C sql_c_type, int cbsize, int sizeorprecision, int offset, CNativeBuffer parameterBuffer)
        { //Handle any input params
            if ((ParameterDirection.Input == _internalDirection) || (ParameterDirection.InputOutput == _internalDirection))
            {
                //Note: (lang) "null" means to use the servers default (not DBNull).
                //We probably should just not have bound this parameter, period, but that
                //would mess up the users question marks, etc...
                if ((null == value))
                {
                    parameterBuffer.WriteIntPtr(_preparedIntOffset, (IntPtr)ODBC32.SQL_DEFAULT_PARAM);
                }
                else if (Convert.IsDBNull(value))
                {
                    parameterBuffer.WriteIntPtr(_preparedIntOffset, (IntPtr)ODBC32.SQL_NULL_DATA);
                }
                else
                {
                    switch (sql_c_type)
                    {
                        case ODBC32.SQL_C.CHAR:
                        case ODBC32.SQL_C.WCHAR:
                        case ODBC32.SQL_C.BINARY:
                            //StrLen_or_IndPtr is ignored except for Character or Binary or data.
                            parameterBuffer.WriteIntPtr(_preparedIntOffset, (IntPtr)cbsize);
                            break;
                        default:
                            parameterBuffer.WriteIntPtr(_preparedIntOffset, IntPtr.Zero);
                            break;
                    }

                    //Place the input param value into the native buffer
                    parameterBuffer.MarshalToNative(_preparedValueOffset, value, sql_c_type, sizeorprecision, offset);
                }
            }
            else
            {
                // always set ouput only and return value parameter values to null when executing
                _internalValue = null;

                //Always initialize the intbuffer (for output params).  Since we need to know
                //if/when the parameters are available for output. (ie: when is the buffer valid...)
                //if (_sqldirection != ODBC32.SQL_PARAM.INPUT)
                parameterBuffer.WriteIntPtr(_preparedIntOffset, (IntPtr)ODBC32.SQL_NULL_DATA);
            }
        }

        private ODBC32.SQL_PARAM SqlDirectionFromParameterDirection()
        {
            switch (_internalDirection)
            {
                case ParameterDirection.Input:
                    return ODBC32.SQL_PARAM.INPUT;
                case ParameterDirection.Output:
                case ParameterDirection.ReturnValue:
                    //ODBC doesn't seem to distinguish between output and return value
                    //as SQL_PARAM_RETURN_VALUE fails with "Invalid parameter type"
                    return ODBC32.SQL_PARAM.OUTPUT;
                case ParameterDirection.InputOutput:
                    return ODBC32.SQL_PARAM.INPUT_OUTPUT;
                default:
                    Debug.Fail("Unexpected Direction Property on Parameter");
                    return ODBC32.SQL_PARAM.INPUT;
            }
        }

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

        private static byte ValuePrecision(object? value)
        {
            return ValuePrecisionCore(value);
        }

        private static byte ValueScale(object? value)
        {
            return ValueScaleCore(value);
        }

        private static int ValueSize(object? value)
        {
            return ValueSizeCore(value);
        }
    }
}
