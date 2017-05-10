// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Reflection;
using System.Xml;
using MSS = Microsoft.SqlServer.Server;

using Microsoft.SqlServer.Server;

namespace System.Data.SqlClient
{
    internal abstract class DataFeed
    {
    }

    internal class StreamDataFeed : DataFeed
    {
        internal Stream _source;

        internal StreamDataFeed(Stream source)
        {
            _source = source;
        }
    }

    internal class TextDataFeed : DataFeed
    {
        internal TextReader _source;

        internal TextDataFeed(TextReader source)
        {
            _source = source;
        }
    }

    internal class XmlDataFeed : DataFeed
    {
        internal XmlReader _source;

        internal XmlDataFeed(XmlReader source)
        {
            _source = source;
        }
    }

    public sealed partial class SqlParameter : DbParameter, ICloneable
    {
        private MetaType _metaType;

        private SqlCollation _collation;
        private string _xmlSchemaCollectionDatabase;
        private string _xmlSchemaCollectionOwningSchema;
        private string _xmlSchemaCollectionName;

        private string _typeName;

        private string _parameterName;
        private byte _precision;
        private byte _scale;
        private bool _hasScale; // V1.0 compat, ignore _hasScale

        private MetaType _internalMetaType;
        private SqlBuffer _sqlBufferReturnValue;
        private INullable _valueAsINullable;
        private bool _isSqlParameterSqlType;
        private bool _isNull = true;
        private bool _coercedValueIsSqlType;
        private bool _coercedValueIsDataFeed;
        private int _actualSize = -1;

        private DataRowVersion _sourceVersion;

        public SqlParameter() : base()
        {
        }

        public SqlParameter(string parameterName, SqlDbType dbType) : this()
        {
            this.ParameterName = parameterName;
            this.SqlDbType = dbType;
        }

        public SqlParameter(string parameterName, object value) : this()
        {
            Debug.Assert(!(value is SqlDbType), "use SqlParameter(string, SqlDbType)");

            this.ParameterName = parameterName;
            this.Value = value;
        }

        public SqlParameter(string parameterName, SqlDbType dbType, int size) : this()
        {
            this.ParameterName = parameterName;
            this.SqlDbType = dbType;
            this.Size = size;
        }

        public SqlParameter(string parameterName, SqlDbType dbType, int size, string sourceColumn) : this()
        {
            this.ParameterName = parameterName;
            this.SqlDbType = dbType;
            this.Size = size;
            this.SourceColumn = sourceColumn;
        }

        public SqlParameter(
            string parameterName,
            SqlDbType dbType, 
            int size,
            ParameterDirection direction,
            byte precision,
            byte scale,
            string sourceColumn,
            DataRowVersion sourceVersion,
            bool sourceColumnNullMapping,
            object value,
            string xmlSchemaCollectionDatabase,
            string xmlSchemaCollectionOwningSchema,
            string xmlSchemaCollectionName
        ) : this()
        {
            this.ParameterName = parameterName;
            this.SqlDbType = dbType;
            this.Size = size;
            this.Direction = direction;
            this.Precision = precision;
            this.Scale = scale;
            this.SourceColumn = sourceColumn;
            this.SourceVersion = sourceVersion;
            this.SourceColumnNullMapping = sourceColumnNullMapping;
            this.Value = value;
            this.XmlSchemaCollectionDatabase = xmlSchemaCollectionDatabase;
            this.XmlSchemaCollectionOwningSchema = xmlSchemaCollectionOwningSchema;
            this.XmlSchemaCollectionName = xmlSchemaCollectionName;
        }

        private SqlParameter(SqlParameter source) : this()
        {
            ADP.CheckArgumentNull(source, nameof(source));

            source.CloneHelper(this);

            ICloneable cloneable = (_value as ICloneable);
            if (null != cloneable)
            {
                _value = cloneable.Clone();
            }
        }

        //
        // currently the user can't set this value.  it gets set by the return value from tds
        //
        internal SqlCollation Collation
        {
            get
            {
                return _collation;
            }
            set
            {
                _collation = value;
            }
        }

        public SqlCompareOptions CompareInfo
        {
            // Bits 21 through 25 represent the CompareInfo
            get
            {
                SqlCollation collation = _collation;
                if (null != collation)
                {
                    return collation.SqlCompareOptions;
                }
                return SqlCompareOptions.None;
            }
            set
            {
                SqlCollation collation = _collation;
                if (null == collation)
                {
                    _collation = collation = new SqlCollation();
                }
                if ((value & SqlTypeWorkarounds.SqlStringValidSqlCompareOptionMask) != value)
                {
                    throw ADP.ArgumentOutOfRange(nameof(CompareInfo));
                }
                collation.SqlCompareOptions = value;
            }
        }


        public string XmlSchemaCollectionDatabase
        {
            get
            {
                string xmlSchemaCollectionDatabase = _xmlSchemaCollectionDatabase;
                return ((xmlSchemaCollectionDatabase != null) ? xmlSchemaCollectionDatabase : ADP.StrEmpty);
            }
            set
            {
                _xmlSchemaCollectionDatabase = value;
            }
        }

        public string XmlSchemaCollectionOwningSchema
        {
            get
            {
                string xmlSchemaCollectionOwningSchema = _xmlSchemaCollectionOwningSchema;
                return ((xmlSchemaCollectionOwningSchema != null) ? xmlSchemaCollectionOwningSchema : ADP.StrEmpty);
            }
            set
            {
                _xmlSchemaCollectionOwningSchema = value;
            }
        }

        public string XmlSchemaCollectionName
        {
            get
            {
                string xmlSchemaCollectionName = _xmlSchemaCollectionName;
                return ((xmlSchemaCollectionName != null) ? xmlSchemaCollectionName : ADP.StrEmpty);
            }
            set
            {
                _xmlSchemaCollectionName = value;
            }
        }

        override public DbType DbType
        {
            get
            {
                return GetMetaTypeOnly().DbType;
            }
            set
            {
                MetaType metatype = _metaType;
                if ((null == metatype) || (metatype.DbType != value) ||
                        // Two special datetime cases for backward compat
                        // DbType.Date and DbType.Time should always be treated as setting DbType.DateTime instead
                        value == DbType.Date ||
                        value == DbType.Time)
                {
                    PropertyTypeChanging();
                    _metaType = MetaType.GetMetaTypeFromDbType(value);
                }
            }
        }

        public override void ResetDbType()
        {
            ResetSqlDbType();
        }

        internal MetaType InternalMetaType
        {
            get
            {
                Debug.Assert(null != _internalMetaType, "null InternalMetaType");
                return _internalMetaType;
            }
            set { _internalMetaType = value; }
        }

        public int LocaleId
        {
            // Lowest 20 bits represent LocaleId
            get
            {
                SqlCollation collation = _collation;
                if (null != collation)
                {
                    return collation.LCID;
                }
                return 0;
            }
            set
            {
                SqlCollation collation = _collation;
                if (null == collation)
                {
                    _collation = collation = new SqlCollation();
                }
                if (value != (SqlCollation.MaskLcid & value))
                {
                    throw ADP.ArgumentOutOfRange(nameof(LocaleId));
                }
                collation.LCID = value;
            }
        }


        internal bool SizeInferred
        {
            get
            {
                return 0 == _size;
            }
        }

        internal MSS.SmiParameterMetaData MetaDataForSmi(out ParameterPeekAheadValue peekAhead)
        {
            peekAhead = null;
            MetaType mt = ValidateTypeLengths();
            long actualLen = GetActualSize();
            long maxLen = this.Size;

            // GetActualSize returns bytes length, but smi expects char length for 
            //  character types, so adjust
            if (!mt.IsLong)
            {
                if (SqlDbType.NChar == mt.SqlDbType || SqlDbType.NVarChar == mt.SqlDbType)
                {
                    actualLen = actualLen / sizeof(char);
                }

                if (actualLen > maxLen)
                {
                    maxLen = actualLen;
                }
            }

            // Determine maxLength for types that ValidateTypeLengths won't figure out
            if (0 == maxLen)
            {
                if (SqlDbType.Binary == mt.SqlDbType || SqlDbType.VarBinary == mt.SqlDbType)
                {
                    maxLen = MSS.SmiMetaData.MaxBinaryLength;
                }
                else if (SqlDbType.Char == mt.SqlDbType || SqlDbType.VarChar == mt.SqlDbType)
                {
                    maxLen = MSS.SmiMetaData.MaxANSICharacters;
                }
                else if (SqlDbType.NChar == mt.SqlDbType || SqlDbType.NVarChar == mt.SqlDbType)
                {
                    maxLen = MSS.SmiMetaData.MaxUnicodeCharacters;
                }
            }
            else if ((maxLen > MSS.SmiMetaData.MaxBinaryLength && (SqlDbType.Binary == mt.SqlDbType || SqlDbType.VarBinary == mt.SqlDbType))
                  || (maxLen > MSS.SmiMetaData.MaxANSICharacters && (SqlDbType.Char == mt.SqlDbType || SqlDbType.VarChar == mt.SqlDbType))
                  || (maxLen > MSS.SmiMetaData.MaxUnicodeCharacters && (SqlDbType.NChar == mt.SqlDbType || SqlDbType.NVarChar == mt.SqlDbType)))
            {
                maxLen = -1;
            }


            int localeId = LocaleId;
            if (0 == localeId && mt.IsCharType)
            {
                object value = GetCoercedValue();
                if (value is SqlString && !((SqlString)value).IsNull)
                {
                    localeId = ((SqlString)value).LCID;
                }
                else
                {
                    localeId = CultureInfo.CurrentCulture.LCID;
                }
            }

            SqlCompareOptions compareOpts = CompareInfo;
            if (0 == compareOpts && mt.IsCharType)
            {
                object value = GetCoercedValue();
                if (value is SqlString && !((SqlString)value).IsNull)
                {
                    compareOpts = ((SqlString)value).SqlCompareOptions;
                }
                else
                {
                    compareOpts = MSS.SmiMetaData.GetDefaultForType(mt.SqlDbType).CompareOptions;
                }
            }

            string typeSpecificNamePart1 = null;
            string typeSpecificNamePart2 = null;
            string typeSpecificNamePart3 = null;

            if (SqlDbType.Xml == mt.SqlDbType)
            {
                typeSpecificNamePart1 = this.XmlSchemaCollectionDatabase;
                typeSpecificNamePart2 = this.XmlSchemaCollectionOwningSchema;
                typeSpecificNamePart3 = this.XmlSchemaCollectionName;
            }
            else if (SqlDbType.Udt == mt.SqlDbType || (SqlDbType.Structured == mt.SqlDbType && !string.IsNullOrEmpty(this.TypeName)))
            {
                // Split the input name. The type name is specified as single 3 part name.
                // NOTE: ParseTypeName throws if format is incorrect
                String[] names;
                if (SqlDbType.Udt == mt.SqlDbType)
                {
                    throw ADP.DbTypeNotSupported(SqlDbType.Udt.ToString());
                }
                else
                {
                    names = ParseTypeName(this.TypeName);
                }

                if (1 == names.Length)
                {
                    typeSpecificNamePart3 = names[0];
                }
                else if (2 == names.Length)
                {
                    typeSpecificNamePart2 = names[0];
                    typeSpecificNamePart3 = names[1];
                }
                else if (3 == names.Length)
                {
                    typeSpecificNamePart1 = names[0];
                    typeSpecificNamePart2 = names[1];
                    typeSpecificNamePart3 = names[2];
                }
                else
                {
                    throw ADP.ArgumentOutOfRange(nameof(names));
                }

                if ((!string.IsNullOrEmpty(typeSpecificNamePart1) && TdsEnums.MAX_SERVERNAME < typeSpecificNamePart1.Length)
                    || (!string.IsNullOrEmpty(typeSpecificNamePart2) && TdsEnums.MAX_SERVERNAME < typeSpecificNamePart2.Length)
                    || (!string.IsNullOrEmpty(typeSpecificNamePart3) && TdsEnums.MAX_SERVERNAME < typeSpecificNamePart3.Length))
                {
                    throw ADP.ArgumentOutOfRange(nameof(names));
                }
            }

            byte precision = GetActualPrecision();
            byte scale = GetActualScale();

            // precision for decimal types may still need adjustment.
            if (SqlDbType.Decimal == mt.SqlDbType)
            {
                if (0 == precision)
                {
                    precision = TdsEnums.DEFAULT_NUMERIC_PRECISION;
                }
            }

            // Sub-field determination
            List<SmiExtendedMetaData> fields = null;
            MSS.SmiMetaDataPropertyCollection extendedProperties = null;
            if (SqlDbType.Structured == mt.SqlDbType)
            {
                GetActualFieldsAndProperties(out fields, out extendedProperties, out peekAhead);
            }

            return new MSS.SmiParameterMetaData(mt.SqlDbType,
                                            maxLen,
                                            precision,
                                            scale,
                                            localeId,
                                            compareOpts,
                                            SqlDbType.Structured == mt.SqlDbType,
                                            fields,
                                            extendedProperties,
                                            this.ParameterNameFixed,
                                            typeSpecificNamePart1,
                                            typeSpecificNamePart2,
                                            typeSpecificNamePart3,
                                            this.Direction);
        }

        internal bool ParameterIsSqlType
        {
            get
            {
                return _isSqlParameterSqlType;
            }
            set
            {
                _isSqlParameterSqlType = value;
            }
        }

        override public string ParameterName
        {
            get
            {
                string parameterName = _parameterName;
                return ((null != parameterName) ? parameterName : ADP.StrEmpty);
            }
            set
            {
                if (string.IsNullOrEmpty(value) || (value.Length < TdsEnums.MAX_PARAMETER_NAME_LENGTH)
                    || (('@' == value[0]) && (value.Length <= TdsEnums.MAX_PARAMETER_NAME_LENGTH)))
                {
                    if (_parameterName != value)
                    {
                        PropertyChanging();
                        _parameterName = value;
                    }
                }
                else
                {
                    throw SQL.InvalidParameterNameLength(value);
                }
            }
        }

        internal string ParameterNameFixed
        {
            get
            {
                string parameterName = ParameterName;
                if ((0 < parameterName.Length) && ('@' != parameterName[0]))
                {
                    parameterName = "@" + parameterName;
                }
                Debug.Assert(parameterName.Length <= TdsEnums.MAX_PARAMETER_NAME_LENGTH, "parameter name too long");
                return parameterName;
            }
        }

        public override Byte Precision
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
                SqlDbType dbtype = GetMetaSqlDbTypeOnly();
                if ((0 == precision) && (SqlDbType.Decimal == dbtype))
                {
                    precision = ValuePrecision(SqlValue);
                }
                return precision;
            }
            set
            {
                SqlDbType sqlDbType = SqlDbType;
                if (sqlDbType == SqlDbType.Decimal && value > TdsEnums.MAX_NUMERIC_PRECISION)
                {
                    throw SQL.PrecisionValueOutOfRange(value);
                }
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

        public override Byte Scale
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
                SqlDbType dbtype = GetMetaSqlDbTypeOnly();
                if ((0 == scale) && (SqlDbType.Decimal == dbtype))
                {
                    scale = ValueScale(SqlValue);
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
                    _actualSize = -1;   // Invalidate actual size such that it is re-calculated
                }
            }
        }

        private bool ShouldSerializeScale()
        {
            return (0 != _scale); // V1.0 compat, ignore _hasScale
        }

        public SqlDbType SqlDbType
        {
            get
            {
                return GetMetaTypeOnly().SqlDbType;
            }
            set
            {
                MetaType metatype = _metaType;
                // HACK!!!
                // We didn't want to expose SmallVarBinary on SqlDbType so we 
                // stuck it at the end of SqlDbType in v1.0, except that now 
                // we have new data types after that and it's smack dab in the
                // middle of the valid range.  To prevent folks from setting 
                // this invalid value we have to have this code here until we
                // can take the time to fix it later.
                if ((SqlDbType)TdsEnums.SmallVarBinary == value)
                {
                    throw SQL.InvalidSqlDbType(value);
                }
                if ((null == metatype) || (metatype.SqlDbType != value))
                {
                    PropertyTypeChanging();
                    _metaType = MetaType.GetMetaTypeFromSqlDbType(value, value == SqlDbType.Structured);
                }
            }
        }

        private bool ShouldSerializeSqlDbType()
        {
            return (null != _metaType);
        }

        public void ResetSqlDbType()
        {
            if (null != _metaType)
            {
                PropertyTypeChanging();
                _metaType = null;
            }
        }

        public object SqlValue
        {
            get
            {
                if (_value != null)
                {
                    if (_value == DBNull.Value)
                    {
                        return MetaType.GetNullSqlValue(GetMetaTypeOnly().SqlType);
                    }
                    if (_value is INullable)
                    {
                        return _value;
                    }

                    // For Date and DateTime2, return the CLR object directly without converting it to a SqlValue
                    // GetMetaTypeOnly() will convert _value to a string in the case of char or char[], so only check
                    // the SqlDbType for DateTime. 
                    if (_value is DateTime)
                    {
                        SqlDbType sqlDbType = GetMetaTypeOnly().SqlDbType;
                        if (sqlDbType == SqlDbType.Date || sqlDbType == SqlDbType.DateTime2)
                        {
                            return _value;
                        }
                    }

                    return (MetaType.GetSqlValueFromComVariant(_value));
                }
                else if (_sqlBufferReturnValue != null)
                {
                    return _sqlBufferReturnValue.SqlValue;
                }
                return null;
            }
            set
            {
                Value = value;
            }
        }

        public String TypeName
        {
            get
            {
                string typeName = _typeName;
                return ((null != typeName) ? typeName : ADP.StrEmpty);
            }
            set
            {
                _typeName = value;
            }
        }

        override public object Value
        { // V1.2.3300, XXXParameter V1.0.3300
            get
            {
                if (_value != null)
                {
                    return _value;
                }
                else if (_sqlBufferReturnValue != null)
                {
                    if (ParameterIsSqlType)
                    {
                        return _sqlBufferReturnValue.SqlValue;
                    }
                    return _sqlBufferReturnValue.Value;
                }
                return null;
            }
            set
            {
                _value = value;
                _sqlBufferReturnValue = null;
                _coercedValue = null;
                _valueAsINullable = _value as INullable;
                _isSqlParameterSqlType = (_valueAsINullable != null);
                _isNull = ((_value == null) || (_value == DBNull.Value) || ((_isSqlParameterSqlType) && (_valueAsINullable.IsNull)));
                _actualSize = -1;
            }
        }

        internal INullable ValueAsINullable
        {
            get
            {
                return _valueAsINullable;
            }
        }

        internal bool IsNull
        {
            get
            {
                // NOTE: Udts can change their value any time
                if (_internalMetaType.SqlDbType == Data.SqlDbType.Udt)
                {
                    _isNull = ((_value == null) || (_value == DBNull.Value) || ((_isSqlParameterSqlType) && (_valueAsINullable.IsNull)));
                }
                return _isNull;
            }
        }

        //
        // always returns data in bytes - except for non-unicode chars, which will be in number of chars
        //
        internal int GetActualSize()
        {
            MetaType mt = InternalMetaType;
            SqlDbType actualType = mt.SqlDbType;
            // NOTE: Users can change the Udt at any time, so we may need to recalculate
            if ((_actualSize == -1) || (actualType == Data.SqlDbType.Udt))
            {
                _actualSize = 0;
                object val = GetCoercedValue();
                bool isSqlVariant = false;

                if (IsNull && !mt.IsVarTime)
                {
                    return 0;
                }

                // if this is a backend SQLVariant type, then infer the TDS type from the SQLVariant type
                if (actualType == SqlDbType.Variant)
                {
                    mt = MetaType.GetMetaTypeFromValue(val, streamAllowed: false);
                    actualType = MetaType.GetSqlDataType(mt.TDSType, 0 /*no user type*/, 0 /*non-nullable type*/).SqlDbType;
                    isSqlVariant = true;
                }

                if (mt.IsFixed)
                {
                    _actualSize = mt.FixedLength;
                }
                else
                {
                    // @hack: until we have ForceOffset behavior we have the following semantics:
                    // @hack: if the user supplies a Size through the Size property or constructor,
                    // @hack: we only send a MAX of Size bytes over.  If the actualSize is < Size, then
                    // @hack: we send over actualSize
                    int coercedSize = 0;

                    // get the actual length of the data, in bytes
                    switch (actualType)
                    {
                        case SqlDbType.NChar:
                        case SqlDbType.NVarChar:
                        case SqlDbType.NText:
                        case SqlDbType.Xml:
                            {
                                coercedSize = ((!_isNull) && (!_coercedValueIsDataFeed)) ? (StringSize(val, _coercedValueIsSqlType)) : 0;
                                _actualSize = (ShouldSerializeSize() ? Size : 0);
                                _actualSize = ((ShouldSerializeSize() && (_actualSize <= coercedSize)) ? _actualSize : coercedSize);
                                if (_actualSize == -1)
                                    _actualSize = coercedSize;
                                _actualSize <<= 1;
                            }
                            break;
                        case SqlDbType.Char:
                        case SqlDbType.VarChar:
                        case SqlDbType.Text:
                            {
                                // for these types, ActualSize is the num of chars, not actual bytes - since non-unicode chars are not always uniform size
                                coercedSize = ((!_isNull) && (!_coercedValueIsDataFeed)) ? (StringSize(val, _coercedValueIsSqlType)) : 0;
                                _actualSize = (ShouldSerializeSize() ? Size : 0);
                                _actualSize = ((ShouldSerializeSize() && (_actualSize <= coercedSize)) ? _actualSize : coercedSize);
                                if (_actualSize == -1)
                                    _actualSize = coercedSize;
                            }
                            break;
                        case SqlDbType.Binary:
                        case SqlDbType.VarBinary:
                        case SqlDbType.Image:
                        case SqlDbType.Timestamp:
                            coercedSize = ((!_isNull) && (!_coercedValueIsDataFeed)) ? (BinarySize(val, _coercedValueIsSqlType)) : 0;
                            _actualSize = (ShouldSerializeSize() ? Size : 0);
                            _actualSize = ((ShouldSerializeSize() && (_actualSize <= coercedSize)) ? _actualSize : coercedSize);
                            if (_actualSize == -1)
                                _actualSize = coercedSize;
                            break;
                        case SqlDbType.Udt:
                            throw ADP.DbTypeNotSupported(SqlDbType.Udt.ToString());
                        case SqlDbType.Structured:
                            coercedSize = -1;
                            break;
                        case SqlDbType.Time:
                            _actualSize = (isSqlVariant ? 5 : MetaType.GetTimeSizeFromScale(GetActualScale()));
                            break;
                        case SqlDbType.DateTime2:
                            // Date in number of days (3 bytes) + time
                            _actualSize = 3 + (isSqlVariant ? 5 : MetaType.GetTimeSizeFromScale(GetActualScale()));
                            break;
                        case SqlDbType.DateTimeOffset:
                            // Date in days (3 bytes) + offset in minutes (2 bytes) + time
                            _actualSize = 5 + (isSqlVariant ? 5 : MetaType.GetTimeSizeFromScale(GetActualScale()));
                            break;
                        default:
                            Debug.Assert(false, "Unknown variable length type!");
                            break;
                    } // switch

                    // don't even send big values over to the variant
                    if (isSqlVariant && (coercedSize > TdsEnums.TYPE_SIZE_LIMIT))
                        throw SQL.ParameterInvalidVariant(this.ParameterName);
                }
            }

            return _actualSize;
        }


        // Coerced Value is also used in SqlBulkCopy.ConvertValue(object value, _SqlMetaData metadata)
        internal static object CoerceValue(object value, MetaType destinationType, out bool coercedToDataFeed, out bool typeChanged, bool allowStreaming = true)
        {
            Debug.Assert(!(value is DataFeed), "Value provided should not already be a data feed");
            Debug.Assert(!ADP.IsNull(value), "Value provided should not be null");
            Debug.Assert(null != destinationType, "null destinationType");

            coercedToDataFeed = false;
            typeChanged = false;
            Type currentType = value.GetType();

            if ((typeof(object) != destinationType.ClassType) &&
                    (currentType != destinationType.ClassType) &&
                    ((currentType != destinationType.SqlType) || (SqlDbType.Xml == destinationType.SqlDbType)))
            {   // Special case for Xml types (since we need to convert SqlXml into a string)
                try
                {
                    // Assume that the type changed
                    typeChanged = true;
                    if ((typeof(string) == destinationType.ClassType))
                    {
                        // For Xml data, destination Type is always string
                        if (typeof(SqlXml) == currentType)
                        {
                            value = MetaType.GetStringFromXml((XmlReader)(((SqlXml)value).CreateReader()));
                        }
                        else if (typeof(SqlString) == currentType)
                        {
                            typeChanged = false;   // Do nothing
                        }
                        else if (typeof(XmlReader).IsAssignableFrom(currentType))
                        {
                            if (allowStreaming)
                            {
                                coercedToDataFeed = true;
                                value = new XmlDataFeed((XmlReader)value);
                            }
                            else
                            {
                                value = MetaType.GetStringFromXml((XmlReader)value);
                            }
                        }
                        else if (typeof(char[]) == currentType)
                        {
                            value = new string((char[])value);
                        }
                        else if (typeof(SqlChars) == currentType)
                        {
                            value = new string(((SqlChars)value).Value);
                        }
                        else if (value is TextReader && allowStreaming)
                        {
                            coercedToDataFeed = true;
                            value = new TextDataFeed((TextReader)value);
                        }
                        else
                        {
                            value = Convert.ChangeType(value, destinationType.ClassType, (IFormatProvider)null);
                        }
                    }
                    else if ((DbType.Currency == destinationType.DbType) && (typeof(string) == currentType))
                    {
                        value = Decimal.Parse((string)value, NumberStyles.Currency, (IFormatProvider)null); // WebData 99376
                    }
                    else if ((typeof(SqlBytes) == currentType) && (typeof(byte[]) == destinationType.ClassType))
                    {
                        typeChanged = false;    // Do nothing
                    }
                    else if ((typeof(string) == currentType) && (SqlDbType.Time == destinationType.SqlDbType))
                    {
                        value = TimeSpan.Parse((string)value);
                    }
                    else if ((typeof(string) == currentType) && (SqlDbType.DateTimeOffset == destinationType.SqlDbType))
                    {
                        value = DateTimeOffset.Parse((string)value, (IFormatProvider)null);
                    }
                    else if ((typeof(DateTime) == currentType) && (SqlDbType.DateTimeOffset == destinationType.SqlDbType))
                    {
                        value = new DateTimeOffset((DateTime)value);
                    }
                    else if (TdsEnums.SQLTABLE == destinationType.TDSType && (
                                value is DbDataReader ||
                                value is System.Collections.Generic.IEnumerable<SqlDataRecord>))
                    {
                        // no conversion for TVPs.
                        typeChanged = false;
                    }
                    else if (destinationType.ClassType == typeof(byte[]) && value is Stream && allowStreaming)
                    {
                        coercedToDataFeed = true;
                        value = new StreamDataFeed((Stream)value);
                    }
                    else
                    {
                        value = Convert.ChangeType(value, destinationType.ClassType, (IFormatProvider)null);
                    }
                }
                catch (Exception e)
                {
                    if (!ADP.IsCatchableExceptionType(e))
                    {
                        throw;
                    }

                    throw ADP.ParameterConversionFailed(value, destinationType.ClassType, e); // WebData 75433
                }
            }

            Debug.Assert(allowStreaming || !coercedToDataFeed, "Streaming is not allowed, but type was coerced into a data feed");
            Debug.Assert(value.GetType() == currentType ^ typeChanged, "Incorrect value for typeChanged");
            return value;
        }

        internal void FixStreamDataForNonPLP()
        {
            object value = GetCoercedValue();
            AssertCachedPropertiesAreValid();
            if (!_coercedValueIsDataFeed)
            {
                return;
            }

            _coercedValueIsDataFeed = false;

            if (value is TextDataFeed)
            {
                if (Size > 0)
                {
                    char[] buffer = new char[Size];
                    int nRead = ((TextDataFeed)value)._source.ReadBlock(buffer, 0, Size);
                    CoercedValue = new string(buffer, 0, nRead);
                }
                else
                {
                    CoercedValue = ((TextDataFeed)value)._source.ReadToEnd();
                }
                return;
            }

            if (value is StreamDataFeed)
            {
                if (Size > 0)
                {
                    byte[] buffer = new byte[Size];
                    int totalRead = 0;
                    Stream sourceStream = ((StreamDataFeed)value)._source;
                    while (totalRead < Size)
                    {
                        int nRead = sourceStream.Read(buffer, totalRead, Size - totalRead);
                        if (nRead == 0)
                        {
                            break;
                        }
                        totalRead += nRead;
                    }
                    if (totalRead < Size)
                    {
                        Array.Resize(ref buffer, totalRead);
                    }
                    CoercedValue = buffer;
                }
                else
                {
                    MemoryStream ms = new MemoryStream();
                    ((StreamDataFeed)value)._source.CopyTo(ms);
                    CoercedValue = ms.ToArray();
                }
                return;
            }

            if (value is XmlDataFeed)
            {
                CoercedValue = MetaType.GetStringFromXml(((XmlDataFeed)value)._source);
                return;
            }

            // We should have returned before reaching here
            Debug.Assert(false, "_coercedValueIsDataFeed was true, but the value was not a known DataFeed type");
        }


        internal byte GetActualPrecision()
        {
            return ShouldSerializePrecision() ? PrecisionInternal : ValuePrecision(CoercedValue);
        }

        internal byte GetActualScale()
        {
            if (ShouldSerializeScale())
            {
                return ScaleInternal;
            }

            // issue: how could a user specify 0 as the actual scale?
            if (GetMetaTypeOnly().IsVarTime)
            {
                return TdsEnums.DEFAULT_VARTIME_SCALE;
            }
            return ValueScale(CoercedValue);
        }

        internal int GetParameterSize()
        {
            return ShouldSerializeSize() ? Size : ValueSize(CoercedValue);
        }

        private void GetActualFieldsAndProperties(out List<MSS.SmiExtendedMetaData> fields, out SmiMetaDataPropertyCollection props, out ParameterPeekAheadValue peekAhead)
        {
            fields = null;
            props = null;
            peekAhead = null;

            object value = GetCoercedValue();
            if (value is SqlDataReader)
            {
                fields = new List<MSS.SmiExtendedMetaData>(((SqlDataReader)value).GetInternalSmiMetaData());
                if (fields.Count <= 0)
                {
                    throw SQL.NotEnoughColumnsInStructuredType();
                }

                bool[] keyCols = new bool[fields.Count];
                bool hasKey = false;
                for (int i = 0; i < fields.Count; i++)
                {
                    MSS.SmiQueryMetaData qmd = fields[i] as MSS.SmiQueryMetaData;
                    if (null != qmd && !qmd.IsKey.IsNull && qmd.IsKey.Value)
                    {
                        keyCols[i] = true;
                        hasKey = true;
                    }
                }

                // Add unique key property, if any found.
                if (hasKey)
                {
                    props = new SmiMetaDataPropertyCollection();
                    props[MSS.SmiPropertySelector.UniqueKey] = new MSS.SmiUniqueKeyProperty(new List<bool>(keyCols));
                }
            }
            else if (value is IEnumerable<SqlDataRecord>)
            {
                // must grab the first record of the enumerator to get the metadata
                IEnumerator<MSS.SqlDataRecord> enumerator = ((IEnumerable<MSS.SqlDataRecord>)value).GetEnumerator();
                MSS.SqlDataRecord firstRecord = null;
                try
                {
                    // no need for fields if there's no rows or no columns -- we'll be sending a null instance anyway.
                    if (enumerator.MoveNext())
                    {
                        firstRecord = enumerator.Current;
                        int fieldCount = firstRecord.FieldCount;
                        if (0 < fieldCount)
                        {
                            // It's valid!  Grab those fields.
                            bool[] keyCols = new bool[fieldCount];
                            bool[] defaultFields = new bool[fieldCount];
                            bool[] sortOrdinalSpecified = new bool[fieldCount];
                            int maxSortOrdinal = -1;  // largest sort ordinal seen, used to optimize locating holes in the list
                            bool hasKey = false;
                            bool hasDefault = false;
                            int sortCount = 0;
                            SmiOrderProperty.SmiColumnOrder[] sort = new SmiOrderProperty.SmiColumnOrder[fieldCount];
                            fields = new List<MSS.SmiExtendedMetaData>(fieldCount);
                            for (int i = 0; i < fieldCount; i++)
                            {
                                SqlMetaData colMeta = firstRecord.GetSqlMetaData(i);
                                fields.Add(MSS.MetaDataUtilsSmi.SqlMetaDataToSmiExtendedMetaData(colMeta));
                                if (colMeta.IsUniqueKey)
                                {
                                    keyCols[i] = true;
                                    hasKey = true;
                                }

                                if (colMeta.UseServerDefault)
                                {
                                    defaultFields[i] = true;
                                    hasDefault = true;
                                }

                                sort[i].Order = colMeta.SortOrder;
                                if (SortOrder.Unspecified != colMeta.SortOrder)
                                {
                                    // SqlMetaData takes care of checking for negative sort ordinals with specified sort order

                                    // bail early if there's no way sort order could be monotonically increasing
                                    if (fieldCount <= colMeta.SortOrdinal)
                                    {
                                        throw SQL.SortOrdinalGreaterThanFieldCount(i, colMeta.SortOrdinal);
                                    }

                                    // Check to make sure we haven't seen this ordinal before
                                    if (sortOrdinalSpecified[colMeta.SortOrdinal])
                                    {
                                        throw SQL.DuplicateSortOrdinal(colMeta.SortOrdinal);
                                    }

                                    sort[i].SortOrdinal = colMeta.SortOrdinal;
                                    sortOrdinalSpecified[colMeta.SortOrdinal] = true;
                                    if (colMeta.SortOrdinal > maxSortOrdinal)
                                    {
                                        maxSortOrdinal = colMeta.SortOrdinal;
                                    }
                                    sortCount++;
                                }
                            }

                            if (hasKey)
                            {
                                props = new SmiMetaDataPropertyCollection();
                                props[MSS.SmiPropertySelector.UniqueKey] = new MSS.SmiUniqueKeyProperty(new List<bool>(keyCols));
                            }

                            if (hasDefault)
                            {
                                // May have already created props list in unique key handling
                                if (null == props)
                                {
                                    props = new SmiMetaDataPropertyCollection();
                                }

                                props[MSS.SmiPropertySelector.DefaultFields] = new MSS.SmiDefaultFieldsProperty(new List<bool>(defaultFields));
                            }

                            if (0 < sortCount)
                            {
                                // validate monotonically increasing sort order.
                                //  Since we already checked for duplicates, we just need
                                //  to watch for values outside of the sortCount range.
                                if (maxSortOrdinal >= sortCount)
                                {
                                    // there is at least one hole, find the first one
                                    int i;
                                    for (i = 0; i < sortCount; i++)
                                    {
                                        if (!sortOrdinalSpecified[i])
                                        {
                                            break;
                                        }
                                    }
                                    Debug.Assert(i < sortCount, "SqlParameter.GetActualFieldsAndProperties: SortOrdinal hole-finding algorithm failed!");
                                    throw SQL.MissingSortOrdinal(i);
                                }

                                // May have already created props list
                                if (null == props)
                                {
                                    props = new SmiMetaDataPropertyCollection();
                                }

                                props[MSS.SmiPropertySelector.SortOrder] = new MSS.SmiOrderProperty(
                                        new List<SmiOrderProperty.SmiColumnOrder>(sort));
                            }

                            // pack it up so we don't have to rewind to send the first value
                            peekAhead = new ParameterPeekAheadValue();
                            peekAhead.Enumerator = enumerator;
                            peekAhead.FirstRecord = firstRecord;

                            // now that it's all packaged, make sure we don't dispose it.
                            enumerator = null;
                        }
                        else
                        {
                            throw SQL.NotEnoughColumnsInStructuredType();
                        }
                    }
                    else
                    {
                        throw SQL.IEnumerableOfSqlDataRecordHasNoRows();
                    }
                }
                finally
                {
                    if (enumerator != null)
                    {
                        enumerator.Dispose();
                    }
                }
            }
            else if (value is DbDataReader)
            {
                // For ProjectK\CoreCLR, DbDataReader no longer supports GetSchema
                // So instead we will attempt to generate the metadata from the Field Type alone
                var reader = (DbDataReader)value;
                if (reader.FieldCount <= 0)
                {
                    throw SQL.NotEnoughColumnsInStructuredType();
                }

                fields = new List<MSS.SmiExtendedMetaData>(reader.FieldCount);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    fields.Add(MSS.MetaDataUtilsSmi.SmiMetaDataFromType(reader.GetName(i), reader.GetFieldType(i)));
                }
            }
        }

        internal object GetCoercedValue()
        {
            // NOTE: User can change the Udt at any time
            if ((null == _coercedValue) || (_internalMetaType.SqlDbType == Data.SqlDbType.Udt))
            {  // will also be set during parameter Validation
                bool isDataFeed = Value is DataFeed;
                if ((IsNull) || (isDataFeed))
                {
                    // No coercion is done for DataFeeds and Nulls
                    _coercedValue = Value;
                    _coercedValueIsSqlType = (_coercedValue == null) ? false : _isSqlParameterSqlType; // set to null for output parameters that keeps _isSqlParameterSqlType
                    _coercedValueIsDataFeed = isDataFeed;
                    _actualSize = IsNull ? 0 : -1;
                }
                else
                {
                    bool typeChanged;
                    _coercedValue = CoerceValue(Value, _internalMetaType, out _coercedValueIsDataFeed, out typeChanged);
                    _coercedValueIsSqlType = ((_isSqlParameterSqlType) && (!typeChanged));  // Type changed always results in a CLR type
                    _actualSize = -1;
                }
            }
            AssertCachedPropertiesAreValid();
            return _coercedValue;
        }

        internal bool CoercedValueIsSqlType
        {
            get
            {
                if (null == _coercedValue)
                {
                    GetCoercedValue();
                }
                AssertCachedPropertiesAreValid();
                return _coercedValueIsSqlType;
            }
        }

        internal bool CoercedValueIsDataFeed
        {
            get
            {
                if (null == _coercedValue)
                {
                    GetCoercedValue();
                }
                AssertCachedPropertiesAreValid();
                return _coercedValueIsDataFeed;
            }
        }

        [Conditional("DEBUG")]
        internal void AssertCachedPropertiesAreValid()
        {
            AssertPropertiesAreValid(_coercedValue, _coercedValueIsSqlType, _coercedValueIsDataFeed, IsNull);
        }

        [Conditional("DEBUG")]
        internal void AssertPropertiesAreValid(object value, bool? isSqlType = null, bool? isDataFeed = null, bool? isNull = null)
        {
            Debug.Assert(!isSqlType.HasValue || (isSqlType.Value == (value is INullable)), "isSqlType is incorrect");
            Debug.Assert(!isDataFeed.HasValue || (isDataFeed.Value == (value is DataFeed)), "isDataFeed is incorrect");
            Debug.Assert(!isNull.HasValue || (isNull.Value == ADP.IsNull(value)), "isNull is incorrect");
        }

        private SqlDbType GetMetaSqlDbTypeOnly()
        {
            MetaType metaType = _metaType;
            if (null == metaType)
            { // infer the type from the value
                metaType = MetaType.GetDefaultMetaType();
            }
            return metaType.SqlDbType;
        }

        // This may not be a good thing to do in case someone overloads the parameter type but I
        // don't want to go from SqlDbType -> metaType -> TDSType
        private MetaType GetMetaTypeOnly()
        {
            if (null != _metaType)
            {
                return _metaType;
            }
            if (null != _value && DBNull.Value != _value)
            {
                // We have a value set by the user then just use that value
                // char and char[] are not directly supported so we convert those values to string
                if (_value is char)
                {
                    _value = _value.ToString();
                }
                else if (Value is char[])
                {
                    _value = new string((char[])_value);
                }
                return MetaType.GetMetaTypeFromValue(_value, inferLen: false);
            }
            else if (null != _sqlBufferReturnValue)
            {  // value came back from the server
                Type valueType = _sqlBufferReturnValue.GetTypeFromStorageType(_isSqlParameterSqlType);
                if (null != valueType)
                {
                    return MetaType.GetMetaTypeFromType(valueType);
                }
            }
            return MetaType.GetDefaultMetaType();
        }

        internal void Prepare(SqlCommand cmd)
        {
            if (null == _metaType)
            {
                throw ADP.PrepareParameterType(cmd);
            }
            else if (!ShouldSerializeSize() && !_metaType.IsFixed)
            {
                throw ADP.PrepareParameterSize(cmd);
            }
            else if ((!ShouldSerializePrecision() && !ShouldSerializeScale()) && (_metaType.SqlDbType == SqlDbType.Decimal))
            {
                throw ADP.PrepareParameterScale(cmd, SqlDbType.ToString());
            }
        }

        private void PropertyChanging()
        {
            _internalMetaType = null;
        }

        private void PropertyTypeChanging()
        {
            PropertyChanging();
            CoercedValue = null;
        }

        internal void SetSqlBuffer(SqlBuffer buff)
        {
            _sqlBufferReturnValue = buff;
            _value = null;
            _coercedValue = null;
            _isNull = _sqlBufferReturnValue.IsNull;
            _coercedValueIsDataFeed = false;
            _coercedValueIsSqlType = false;
            _actualSize = -1;
        }


        internal void Validate(int index, bool isCommandProc)
        {
            MetaType metaType = GetMetaTypeOnly();
            _internalMetaType = metaType;

            // NOTE: (General Criteria): SqlParameter does a Size Validation check and would fail if the size is 0. 
            //                           This condition filters all scenarios where we view a valid size 0.
            if (ADP.IsDirection(this, ParameterDirection.Output) &&
                !ADP.IsDirection(this, ParameterDirection.ReturnValue) &&
                (!metaType.IsFixed) &&
                !ShouldSerializeSize() &&
                ((null == _value) || (_value == DBNull.Value)) &&
                (SqlDbType != SqlDbType.Timestamp) &&
                (SqlDbType != SqlDbType.Udt) &&
                // Output parameter with size 0 throws for XML, TEXT, NTEXT, IMAGE.
                (SqlDbType != SqlDbType.Xml) &&
                !metaType.IsVarTime)
            {
                throw ADP.UninitializedParameterSize(index, metaType.ClassType);
            }

            if (metaType.SqlDbType != SqlDbType.Udt && Direction != ParameterDirection.Output)
            {
                GetCoercedValue();
            }


            // Validate structured-type-specific details.
            if (metaType.SqlDbType == SqlDbType.Structured)
            {
                if (!isCommandProc && string.IsNullOrEmpty(TypeName))
                    throw SQL.MustSetTypeNameForParam(metaType.TypeName, this.ParameterName);

                if (ParameterDirection.Input != this.Direction)
                {
                    throw SQL.UnsupportedTVPOutputParameter(this.Direction, this.ParameterName);
                }

                if (DBNull.Value == GetCoercedValue())
                {
                    throw SQL.DBNullNotSupportedForTVPValues(this.ParameterName);
                }
            }
            else if (!string.IsNullOrEmpty(TypeName))
            {
                throw SQL.UnexpectedTypeNameForNonStructParams(this.ParameterName);
            }
        }

        // func will change type to that with a 4 byte length if the type has a two
        // byte length and a parameter length > than that expressible in 2 bytes
        internal MetaType ValidateTypeLengths()
        {
            MetaType mt = InternalMetaType;
            // Since the server will automatically reject any
            // char, varchar, binary, varbinary, nchar, or nvarchar parameter that has a
            // byte sizeInCharacters > 8000 bytes, we promote the parameter to image, text, or ntext.  This
            // allows the user to specify a parameter type using a COM+ datatype and be able to
            // use that parameter against a BLOB column.
            if ((SqlDbType.Udt != mt.SqlDbType) && (false == mt.IsFixed) && (false == mt.IsLong))
            { // if type has 2 byte length
                long actualSizeInBytes = this.GetActualSize();
                long sizeInCharacters = this.Size;

                // 'actualSizeInBytes' is the size of value passed; 
                // 'sizeInCharacters' is the parameter size;
                // 'actualSizeInBytes' is in bytes; 
                // 'this.Size' is in characters; 
                // 'sizeInCharacters' is in characters; 
                // 'TdsEnums.TYPE_SIZE_LIMIT' is in bytes;
                // For Non-NCharType and for non-Yukon or greater variables, size should be maintained;
                // Modified variable names from 'size' to 'sizeInCharacters', 'actualSize' to 'actualSizeInBytes', and 
                // 'maxSize' to 'maxSizeInBytes'
                // The idea is to
                // Keeping these goals in mind - the following are the changes we are making

                long maxSizeInBytes = 0;
                if (mt.IsNCharType)
                    maxSizeInBytes = ((sizeInCharacters * sizeof(char)) > actualSizeInBytes) ? sizeInCharacters * sizeof(char) : actualSizeInBytes;
                else
                {
                    // Notes:
                    // Elevation from (n)(var)char (4001+) to (n)text succeeds without failure only with Yukon and greater.
                    // it fails in sql server 2000
                    maxSizeInBytes = (sizeInCharacters > actualSizeInBytes) ? sizeInCharacters : actualSizeInBytes;
                }

                if ((maxSizeInBytes > TdsEnums.TYPE_SIZE_LIMIT) || (_coercedValueIsDataFeed) ||
                    (sizeInCharacters == -1) || (actualSizeInBytes == -1))
                { // is size > size able to be described by 2 bytes
                    // Convert the parameter to its max type
                    mt = MetaType.GetMaxMetaTypeFromMetaType(mt);
                    _metaType = mt;
                    InternalMetaType = mt;
                    if (!mt.IsPlp)
                    {
                        if (mt.SqlDbType == SqlDbType.Xml)
                        {
                            throw ADP.InvalidMetaDataValue();     //Xml should always have IsPartialLength = true
                        }
                        if (mt.SqlDbType == SqlDbType.NVarChar
                         || mt.SqlDbType == SqlDbType.VarChar
                         || mt.SqlDbType == SqlDbType.VarBinary)
                        {
                            Size = (int)(SmiMetaData.UnlimitedMaxLengthIndicator);
                        }
                    }
                }
            }
            return mt;
        }

        private byte ValuePrecision(object value)
        {
            if (value is SqlDecimal)
            {
                if (((SqlDecimal)value).IsNull)
                    return 0;

                return ((SqlDecimal)value).Precision;
            }
            return ValuePrecisionCore(value);
        }

        private byte ValueScale(object value)
        {
            if (value is SqlDecimal)
            {
                if (((SqlDecimal)value).IsNull)
                    return 0;

                return ((SqlDecimal)value).Scale;
            }
            return ValueScaleCore(value);
        }

        private static int StringSize(object value, bool isSqlType)
        {
            if (isSqlType)
            {
                Debug.Assert(!((INullable)value).IsNull, "Should not call StringSize on null values");
                if (value is SqlString)
                {
                    return ((SqlString)value).Value.Length;
                }
                if (value is SqlChars)
                {
                    return ((SqlChars)value).Value.Length;
                }
            }
            else
            {
                string svalue = (value as string);
                if (null != svalue)
                {
                    return svalue.Length;
                }
                char[] cvalue = (value as char[]);
                if (null != cvalue)
                {
                    return cvalue.Length;
                }
                if (value is char)
                {
                    return 1;
                }
            }

            // Didn't match, unknown size
            return 0;
        }

        private static int BinarySize(object value, bool isSqlType)
        {
            if (isSqlType)
            {
                Debug.Assert(!((INullable)value).IsNull, "Should not call StringSize on null values");
                if (value is SqlBinary)
                {
                    return ((SqlBinary)value).Length;
                }
                if (value is SqlBytes)
                {
                    return ((SqlBytes)value).Value.Length;
                }
            }
            else
            {
                byte[] bvalue = (value as byte[]);
                if (null != bvalue)
                {
                    return bvalue.Length;
                }
                if (value is byte)
                {
                    return 1;
                }
            }

            // Didn't match, unknown size
            return 0;
        }

        private int ValueSize(object value)
        {
            if (value is SqlString)
            {
                if (((SqlString)value).IsNull)
                    return 0;

                return ((SqlString)value).Value.Length;
            }
            if (value is SqlChars)
            {
                if (((SqlChars)value).IsNull)
                    return 0;

                return ((SqlChars)value).Value.Length;
            }

            if (value is SqlBinary)
            {
                if (((SqlBinary)value).IsNull)
                    return 0;

                return ((SqlBinary)value).Length;
            }
            if (value is SqlBytes)
            {
                if (((SqlBytes)value).IsNull)
                    return 0;

                return (int)(((SqlBytes)value).Length);
            }
            if (value is DataFeed)
            {
                // Unknown length
                return 0;
            }
            return ValueSizeCore(value);
        }

        // parse an string of the form db.schema.name where any of the three components
        // might have "[" "]" and dots within it.
        // returns:
        //   [0] dbname (or null)
        //   [1] schema (or null)
        //   [2] name
        // NOTE: if perf/space implications of Regex is not a problem, we can get rid
        // of this and use a simple regex to do the parsing
        internal static string[] ParseTypeName(string typeName)
        {
            Debug.Assert(null != typeName, "null typename passed to ParseTypeName");

            try
            {
                string errorMsg;
                {
                    errorMsg = SR.SQL_TypeName;
                }
                return MultipartIdentifier.ParseMultipartIdentifier(typeName, "[\"", "]\"", '.', 3, true, errorMsg, true);
            }
            catch (ArgumentException)
            {
                {
                    throw SQL.InvalidParameterTypeNameFormat();
                }
            }
        }

        object ICloneable.Clone() => new SqlParameter(this);

        private void CloneHelper(SqlParameter destination)
        {
            CloneHelperCore(destination);
            destination._metaType = _metaType;
            destination._collation = _collation;
            destination._xmlSchemaCollectionDatabase = _xmlSchemaCollectionDatabase;
            destination._xmlSchemaCollectionOwningSchema = _xmlSchemaCollectionOwningSchema;
            destination._xmlSchemaCollectionName = _xmlSchemaCollectionName;
            destination._typeName = _typeName;

            destination._parameterName = _parameterName;
            destination._precision = _precision;
            destination._scale = _scale;
            destination._sqlBufferReturnValue = _sqlBufferReturnValue;
            destination._isSqlParameterSqlType = _isSqlParameterSqlType;
            destination._internalMetaType = _internalMetaType;
            destination.CoercedValue = CoercedValue; // copy cached value reference because of XmlReader problem
            destination._valueAsINullable = _valueAsINullable;
            destination._isNull = _isNull;
            destination._coercedValueIsDataFeed = _coercedValueIsDataFeed;
            destination._coercedValueIsSqlType = _coercedValueIsSqlType;
            destination._actualSize = _actualSize;
        }

        private void CloneHelperCore(SqlParameter destination)
        {
            destination._value = _value;

            destination._direction = _direction;
            destination._size = _size;

            destination._offset = _offset;
            destination._sourceColumn = _sourceColumn;
            destination._sourceVersion = _sourceVersion;
            destination._sourceColumnNullMapping = _sourceColumnNullMapping;
            destination._isNullable = _isNullable;
        }

        public override DataRowVersion SourceVersion
        {
            get
            {
                DataRowVersion sourceVersion = _sourceVersion;
                return ((0 != sourceVersion) ? sourceVersion : DataRowVersion.Current);
            }
            set
            {
                switch (value)
                {
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
    }
}
