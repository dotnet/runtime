// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//------------------------------------------------------------------------------


using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Data.SqlTypes;
using System.Data.SqlClient;

namespace Microsoft.SqlServer.Server
{
    using Res = System.SR;

    // class SqlMetaData
    //   Simple immutable implementation of the a metadata-holding class.  Only
    //    complexities are:
    //        1) enforcing immutability.
    //        2) Inferring type from a value.
    //        3) Adjusting a value to match the metadata.

    public sealed class SqlMetaData
    {
        private string _strName;
        private long _lMaxLength;
        private SqlDbType _sqlDbType;
        private byte _bPrecision;
        private byte _bScale;
        private long _lLocale;
        private SqlCompareOptions _eCompareOptions;
        private string _xmlSchemaCollectionDatabase;
        private string _xmlSchemaCollectionOwningSchema;
        private string _xmlSchemaCollectionName;
        private bool _bPartialLength;
        private bool _useServerDefault;
        private bool _isUniqueKey;
        private SortOrder _columnSortOrder;
        private int _sortOrdinal;

        // unlimited (except by implementation) max-length.
        private const long x_lMax = -1;
        private const long x_lServerMaxUnicode = 4000;
        private const long x_lServerMaxANSI = 8000;
        private const long x_lServerMaxBinary = 8000;
        private const bool x_defaultUseServerDefault = false;
        private const bool x_defaultIsUniqueKey = false;
        private const SortOrder x_defaultColumnSortOrder = SortOrder.Unspecified;
        private const int x_defaultSortOrdinal = -1;

        private const SqlCompareOptions x_eDefaultStringCompareOptions = SqlCompareOptions.IgnoreCase
                                        | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth;

        // scalar types constructor without tvp extended properties
        public SqlMetaData(String name, SqlDbType dbType)
        {
            Construct(name, dbType, x_defaultUseServerDefault,
                    x_defaultIsUniqueKey, x_defaultColumnSortOrder, x_defaultSortOrdinal);
        }

        // scalar types constructor
        public SqlMetaData(String name, SqlDbType dbType, bool useServerDefault,
                    bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal)
        {
            Construct(name, dbType, useServerDefault,
                    isUniqueKey, columnSortOrder, sortOrdinal);
        }

        // binary or string constructor with only max length
        // (for string types, locale and compare options will be picked up from the thread.
        public SqlMetaData(String name, SqlDbType dbType, long maxLength)
        {
            Construct(name, dbType, maxLength, x_defaultUseServerDefault,
                    x_defaultIsUniqueKey, x_defaultColumnSortOrder, x_defaultSortOrdinal);
        }

        // binary or string constructor with only max length and tvp extended properties
        // (for string types, locale and compare options will be picked up from the thread.
        public SqlMetaData(String name, SqlDbType dbType, long maxLength, bool useServerDefault,
                    bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal)
        {
            Construct(name, dbType, maxLength, useServerDefault,
                    isUniqueKey, columnSortOrder, sortOrdinal);
        }


        // decimal ctor without tvp extended properties
        public SqlMetaData(String name, SqlDbType dbType, byte precision, byte scale)
        {
            Construct(name, dbType, precision, scale, x_defaultUseServerDefault,
                    x_defaultIsUniqueKey, x_defaultColumnSortOrder, x_defaultSortOrdinal);
        }

        // decimal ctor
        public SqlMetaData(string name, SqlDbType dbType, byte precision, byte scale, bool useServerDefault,
                    bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal)
        {
            Construct(name, dbType, precision, scale, useServerDefault,
                    isUniqueKey, columnSortOrder, sortOrdinal);
        }

        // string type constructor with locale and compare options, no tvp extended properties
        public SqlMetaData(String name, SqlDbType dbType, long maxLength, long locale,
                           SqlCompareOptions compareOptions)
        {
            Construct(name, dbType, maxLength, locale, compareOptions, x_defaultUseServerDefault,
                    x_defaultIsUniqueKey, x_defaultColumnSortOrder, x_defaultSortOrdinal);
        }

        // string type constructor with locale and compare options
        public SqlMetaData(String name, SqlDbType dbType, long maxLength, long locale,
                           SqlCompareOptions compareOptions, bool useServerDefault,
                           bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal)
        {
            Construct(name, dbType, maxLength, locale, compareOptions, useServerDefault,
                    isUniqueKey, columnSortOrder, sortOrdinal);
        }

        // typed xml ctor
        public SqlMetaData(String name, SqlDbType dbType, string database, string owningSchema,
                           string objectName, bool useServerDefault, bool isUniqueKey,
                           SortOrder columnSortOrder, int sortOrdinal)
        {
            Construct(name, dbType, database, owningSchema, objectName, useServerDefault,
                        isUniqueKey, columnSortOrder, sortOrdinal);
        }

        // everything except xml schema and tvp properties ctor
        public SqlMetaData(String name, SqlDbType dbType, long maxLength, byte precision,
                           byte scale, long locale, SqlCompareOptions compareOptions,
                           Type userDefinedType) :
                           this(name, dbType, maxLength, precision, scale, locale, compareOptions,
                                userDefinedType, x_defaultUseServerDefault, x_defaultIsUniqueKey,
                                x_defaultColumnSortOrder, x_defaultSortOrdinal)
        {
        }

        // everything except xml schema ctor
        public SqlMetaData(String name, SqlDbType dbType, long maxLength, byte precision,
                           byte scale, long localeId, SqlCompareOptions compareOptions,
                           Type userDefinedType, bool useServerDefault,
                           bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal)
        {
            switch (dbType)
            {
                case SqlDbType.BigInt:
                case SqlDbType.Image:
                case SqlDbType.Timestamp:
                case SqlDbType.Bit:
                case SqlDbType.DateTime:
                case SqlDbType.SmallDateTime:
                case SqlDbType.Real:
                case SqlDbType.Int:
                case SqlDbType.Money:
                case SqlDbType.SmallMoney:
                case SqlDbType.Float:
                case SqlDbType.UniqueIdentifier:
                case SqlDbType.SmallInt:
                case SqlDbType.TinyInt:
                case SqlDbType.Xml:
                case SqlDbType.Date:
                    Construct(name, dbType, useServerDefault, isUniqueKey, columnSortOrder, sortOrdinal);
                    break;
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                    Construct(name, dbType, maxLength, useServerDefault, isUniqueKey, columnSortOrder, sortOrdinal);
                    break;
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                    Construct(name, dbType, maxLength, localeId, compareOptions, useServerDefault, isUniqueKey, columnSortOrder, sortOrdinal);
                    break;
                case SqlDbType.NText:
                case SqlDbType.Text:
                    // We should ignore user's max length and use Max instead to avoid exception
                    Construct(name, dbType, Max, localeId, compareOptions, useServerDefault, isUniqueKey, columnSortOrder, sortOrdinal);
                    break;
                case SqlDbType.Decimal:
                case SqlDbType.Time:
                case SqlDbType.DateTime2:
                case SqlDbType.DateTimeOffset:
                    Construct(name, dbType, precision, scale, useServerDefault, isUniqueKey, columnSortOrder, sortOrdinal);
                    break;
                case SqlDbType.Variant:
                    Construct(name, dbType, useServerDefault, isUniqueKey, columnSortOrder, sortOrdinal);
                    break;
                case SqlDbType.Udt:
                    throw ADP.DbTypeNotSupported(SqlDbType.Udt.ToString());
                default:
                    SQL.InvalidSqlDbTypeForConstructor(dbType);
                    break;
            }
        }

        public SqlMetaData(String name, SqlDbType dbType, string database, string owningSchema, string objectName)
        {
            Construct(name, dbType, database, owningSchema, objectName, x_defaultUseServerDefault, x_defaultIsUniqueKey,
                    x_defaultColumnSortOrder, x_defaultSortOrdinal);
        }

        // Most general constructor, should be able to intialize all SqlMetaData fields.(Used by SqlParameter)
        internal SqlMetaData(String name,
                              SqlDbType sqlDBType,
                              long maxLength,
                              byte precision,
                              byte scale,
                              long localeId,
                              SqlCompareOptions compareOptions,
                              string xmlSchemaCollectionDatabase,
                              string xmlSchemaCollectionOwningSchema,
                              string xmlSchemaCollectionName,
                              bool partialLength
        )
        {
            AssertNameIsValid(name);

            _strName = name;
            _sqlDbType = sqlDBType;
            _lMaxLength = maxLength;
            _bPrecision = precision;
            _bScale = scale;
            _lLocale = localeId;
            _eCompareOptions = compareOptions;
            _xmlSchemaCollectionDatabase = xmlSchemaCollectionDatabase;
            _xmlSchemaCollectionOwningSchema = xmlSchemaCollectionOwningSchema;
            _xmlSchemaCollectionName = xmlSchemaCollectionName;
            _bPartialLength = partialLength;

            ThrowIfUdt(sqlDBType);
        }

        // Private constructor used to initialize default instance array elements.
        // DO NOT EXPOSE OUTSIDE THIS CLASS!  It performs no validation.
        private SqlMetaData(String name,
                             SqlDbType sqlDbType,
                             long maxLength,
                             byte precision,
                             byte scale,
                             long localeId,
                             SqlCompareOptions compareOptions,
                             bool partialLength)
        {
            AssertNameIsValid(name);

            _strName = name;
            _sqlDbType = sqlDbType;
            _lMaxLength = maxLength;
            _bPrecision = precision;
            _bScale = scale;
            _lLocale = localeId;
            _eCompareOptions = compareOptions;
            _bPartialLength = partialLength;
            ThrowIfUdt(sqlDbType);
        }

        public SqlCompareOptions CompareOptions
        {
            get
            {
                return _eCompareOptions;
            }
        }


        public bool IsUniqueKey
        {
            get
            {
                return _isUniqueKey;
            }
        }

        public long LocaleId
        {
            get
            {
                return _lLocale;
            }
        }

        public static long Max
        {
            get
            {
                return x_lMax;
            }
        }

        public long MaxLength
        {
            get
            {
                return _lMaxLength;
            }
        }

        public string Name
        {
            get
            {
                return _strName;
            }
        }

        public byte Precision
        {
            get
            {
                return _bPrecision;
            }
        }

        public byte Scale
        {
            get
            {
                return _bScale;
            }
        }

        public SortOrder SortOrder
        {
            get
            {
                return _columnSortOrder;
            }
        }

        public int SortOrdinal
        {
            get
            {
                return _sortOrdinal;
            }
        }

        public SqlDbType SqlDbType
        {
            get
            {
                return _sqlDbType;
            }
        }


        public string TypeName
        {
            get
            {
                {
                    return sxm_rgDefaults[(int)SqlDbType].Name;
                }
            }
        }


        public bool UseServerDefault
        {
            get
            {
                return _useServerDefault;
            }
        }

        public string XmlSchemaCollectionDatabase
        {
            get
            {
                return _xmlSchemaCollectionDatabase;
            }
        }

        public string XmlSchemaCollectionName
        {
            get
            {
                return _xmlSchemaCollectionName;
            }
        }

        public string XmlSchemaCollectionOwningSchema
        {
            get
            {
                return _xmlSchemaCollectionOwningSchema;
            }
        }

        internal bool IsPartialLength
        {
            get
            {
                return _bPartialLength;
            }
        }


        // Construction for all types that do not have variable attributes
        private void Construct(String name, SqlDbType dbType, bool useServerDefault,
                    bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal)
        {
            AssertNameIsValid(name);

            ValidateSortOrder(columnSortOrder, sortOrdinal);

            // Check for absense of explicitly-allowed types to avoid unexpected additions when new types are added
            if (!(SqlDbType.BigInt == dbType ||
                    SqlDbType.Bit == dbType ||
                    SqlDbType.DateTime == dbType ||
                    SqlDbType.Date == dbType ||
                    SqlDbType.DateTime2 == dbType ||
                    SqlDbType.DateTimeOffset == dbType ||
                    SqlDbType.Decimal == dbType ||
                    SqlDbType.Float == dbType ||
                    SqlDbType.Image == dbType ||
                    SqlDbType.Int == dbType ||
                    SqlDbType.Money == dbType ||
                    SqlDbType.NText == dbType ||
                    SqlDbType.Real == dbType ||
                    SqlDbType.SmallDateTime == dbType ||
                    SqlDbType.SmallInt == dbType ||
                    SqlDbType.SmallMoney == dbType ||
                    SqlDbType.Text == dbType ||
                    SqlDbType.Time == dbType ||
                    SqlDbType.Timestamp == dbType ||
                    SqlDbType.TinyInt == dbType ||
                    SqlDbType.UniqueIdentifier == dbType ||
                    SqlDbType.Variant == dbType ||
                    SqlDbType.Xml == dbType))
                throw SQL.InvalidSqlDbTypeForConstructor(dbType);

            ThrowIfUdt(dbType);

            SetDefaultsForType(dbType);

            if (SqlDbType.NText == dbType || SqlDbType.Text == dbType)
            {
                _lLocale = Locale.GetCurrentCultureLcid();
            }


            _strName = name;
            _useServerDefault = useServerDefault;
            _isUniqueKey = isUniqueKey;
            _columnSortOrder = columnSortOrder;
            _sortOrdinal = sortOrdinal;
        }

        // Construction for all types that vary by user-specified length (not Udts)
        private void Construct(String name, SqlDbType dbType, long maxLength, bool useServerDefault,
                    bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal)
        {
            AssertNameIsValid(name);

            ValidateSortOrder(columnSortOrder, sortOrdinal);

            long lLocale = 0;
            if (SqlDbType.Char == dbType)
            {
                if (maxLength > x_lServerMaxANSI || maxLength < 0)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
                lLocale = Locale.GetCurrentCultureLcid();
            }
            else if (SqlDbType.VarChar == dbType)
            {
                if ((maxLength > x_lServerMaxANSI || maxLength < 0) && maxLength != Max)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
                lLocale = Locale.GetCurrentCultureLcid();
            }
            else if (SqlDbType.NChar == dbType)
            {
                if (maxLength > x_lServerMaxUnicode || maxLength < 0)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
                lLocale = Locale.GetCurrentCultureLcid();
            }
            else if (SqlDbType.NVarChar == dbType)
            {
                if ((maxLength > x_lServerMaxUnicode || maxLength < 0) && maxLength != Max)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
                lLocale = Locale.GetCurrentCultureLcid();
            }
            else if (SqlDbType.NText == dbType || SqlDbType.Text == dbType)
            {
                // old-style lobs only allowed with Max length
                if (SqlMetaData.Max != maxLength)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
                lLocale = Locale.GetCurrentCultureLcid();
            }
            else if (SqlDbType.Binary == dbType)
            {
                if (maxLength > x_lServerMaxBinary || maxLength < 0)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
            }
            else if (SqlDbType.VarBinary == dbType)
            {
                if ((maxLength > x_lServerMaxBinary || maxLength < 0) && maxLength != Max)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
            }
            else if (SqlDbType.Image == dbType)
            {
                // old-style lobs only allowed with Max length
                if (SqlMetaData.Max != maxLength)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
            }
            else
                throw SQL.InvalidSqlDbTypeForConstructor(dbType);

            SetDefaultsForType(dbType);

            _strName = name;
            _lMaxLength = maxLength;
            _lLocale = lLocale;
            _useServerDefault = useServerDefault;
            _isUniqueKey = isUniqueKey;
            _columnSortOrder = columnSortOrder;
            _sortOrdinal = sortOrdinal;
        }

        // Construction for string types with specified locale/compare options
        private void Construct(String name,
                               SqlDbType dbType,
                               long maxLength,
                               long locale,
                               SqlCompareOptions compareOptions,
                               bool useServerDefault,
                               bool isUniqueKey,
                               SortOrder columnSortOrder,
                               int sortOrdinal)
        {
            AssertNameIsValid(name);

            ValidateSortOrder(columnSortOrder, sortOrdinal);

            // Validate type and max length.
            if (SqlDbType.Char == dbType)
            {
                if (maxLength > x_lServerMaxANSI || maxLength < 0)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
            }
            else if (SqlDbType.VarChar == dbType)
            {
                if ((maxLength > x_lServerMaxANSI || maxLength < 0) && maxLength != Max)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
            }
            else if (SqlDbType.NChar == dbType)
            {
                if (maxLength > x_lServerMaxUnicode || maxLength < 0)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
            }
            else if (SqlDbType.NVarChar == dbType)
            {
                if ((maxLength > x_lServerMaxUnicode || maxLength < 0) && maxLength != Max)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
            }
            else if (SqlDbType.NText == dbType || SqlDbType.Text == dbType)
            {
                // old-style lobs only allowed with Max length
                if (SqlMetaData.Max != maxLength)
                    throw ADP.Argument(Res.GetString(Res.ADP_InvalidDataLength2, maxLength.ToString(CultureInfo.InvariantCulture)), "maxLength");
            }
            else
                throw SQL.InvalidSqlDbTypeForConstructor(dbType);

            // Validate locale?

            // Validate compare options
            //    Binary sort must be by itself.
            //    Nothing else but the Ignore bits is allowed.
            if (SqlCompareOptions.BinarySort != compareOptions &&
                    0 != (~((int)SqlCompareOptions.IgnoreCase | (int)SqlCompareOptions.IgnoreNonSpace |
                            (int)SqlCompareOptions.IgnoreKanaType | (int)SqlCompareOptions.IgnoreWidth) &
                        (int)compareOptions))
                throw ADP.InvalidEnumerationValue(typeof(SqlCompareOptions), (int)compareOptions);

            SetDefaultsForType(dbType);

            _strName = name;
            _lMaxLength = maxLength;
            _lLocale = locale;
            _eCompareOptions = compareOptions;
            _useServerDefault = useServerDefault;
            _isUniqueKey = isUniqueKey;
            _columnSortOrder = columnSortOrder;
            _sortOrdinal = sortOrdinal;
        }


        private static byte[] s_maxLenFromPrecision = new byte[] {5,5,5,5,5,5,5,5,5,9,9,9,9,9,
        9,9,9,9,9,13,13,13,13,13,13,13,13,13,17,17,17,17,17,17,17,17,17,17};

        private const byte MaxTimeScale = 7;

        private static byte[] s_maxVarTimeLenOffsetFromScale = new byte[] { 2, 2, 2, 1, 1, 0, 0, 0 };

        // Construction for Decimal type and new Katmai Date/Time types
        private void Construct(String name, SqlDbType dbType, byte precision, byte scale, bool useServerDefault,
                    bool isUniqueKey, SortOrder columnSortOrder, int sortOrdinal)
        {
            AssertNameIsValid(name);

            ValidateSortOrder(columnSortOrder, sortOrdinal);

            if (SqlDbType.Decimal == dbType)
            {
                if (precision > SqlDecimal.MaxPrecision || scale > precision)
                    throw SQL.PrecisionValueOutOfRange(precision);

                if (scale > SqlDecimal.MaxScale)
                    throw SQL.ScaleValueOutOfRange(scale);
            }
            else if (SqlDbType.Time == dbType || SqlDbType.DateTime2 == dbType || SqlDbType.DateTimeOffset == dbType)
            {
                if (scale > MaxTimeScale)
                {
                    throw SQL.TimeScaleValueOutOfRange(scale);
                }
            }
            else
            {
                throw SQL.InvalidSqlDbTypeForConstructor(dbType);
            }
            SetDefaultsForType(dbType);

            _strName = name;
            _bPrecision = precision;
            _bScale = scale;
            if (SqlDbType.Decimal == dbType)
            {
                _lMaxLength = s_maxLenFromPrecision[precision - 1];
            }
            else
            {
                _lMaxLength -= s_maxVarTimeLenOffsetFromScale[scale];
            }
            _useServerDefault = useServerDefault;
            _isUniqueKey = isUniqueKey;
            _columnSortOrder = columnSortOrder;
            _sortOrdinal = sortOrdinal;
        }


        // Construction for Xml type
        private void Construct(String name, SqlDbType dbType, string database, string owningSchema,
                    string objectName, bool useServerDefault, bool isUniqueKey, SortOrder columnSortOrder,
                    int sortOrdinal)
        {
            AssertNameIsValid(name);

            ValidateSortOrder(columnSortOrder, sortOrdinal);

            if (SqlDbType.Xml != dbType)
                throw SQL.InvalidSqlDbTypeForConstructor(dbType);

            if (null != database || null != owningSchema)
            {
                if (null == objectName)
                {
                    throw ADP.ArgumentNull("objectName");
                }
            }

            SetDefaultsForType(SqlDbType.Xml);
            _strName = name;

            _xmlSchemaCollectionDatabase = database;
            _xmlSchemaCollectionOwningSchema = owningSchema;
            _xmlSchemaCollectionName = objectName;
            _useServerDefault = useServerDefault;
            _isUniqueKey = isUniqueKey;
            _columnSortOrder = columnSortOrder;
            _sortOrdinal = sortOrdinal;
        }

        private void AssertNameIsValid(string name)
        {
            if (null == name)
                throw ADP.ArgumentNull("name");

            if (Microsoft.SqlServer.Server.SmiMetaData.MaxNameLength < name.Length)
                throw SQL.NameTooLong("name");
        }

        private void ValidateSortOrder(SortOrder columnSortOrder, int sortOrdinal)
        {
            // Check that sort order is valid enum value.
            if (SortOrder.Unspecified != columnSortOrder &&
                    SortOrder.Ascending != columnSortOrder &&
                    SortOrder.Descending != columnSortOrder)
            {
                throw SQL.InvalidSortOrder(columnSortOrder);
            }

            // Must specify both sort order and ordinal, or neither
            if ((SortOrder.Unspecified == columnSortOrder) != (x_defaultSortOrdinal == sortOrdinal))
            {
                throw SQL.MustSpecifyBothSortOrderAndOrdinal(columnSortOrder, sortOrdinal);
            }
        }


        public Int16 Adjust(Int16 value)
        {
            if (SqlDbType.SmallInt != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public Int32 Adjust(Int32 value)
        {
            if (SqlDbType.Int != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public Int64 Adjust(Int64 value)
        {
            if (SqlDbType.BigInt != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public float Adjust(float value)
        {
            if (SqlDbType.Real != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public double Adjust(double value)
        {
            if (SqlDbType.Float != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public string Adjust(string value)
        {
            if (SqlDbType.Char == SqlDbType || SqlDbType.NChar == SqlDbType)
            {
                //DBG.Assert(Max!=MaxLength, "SqlMetaData.Adjust(string): Fixed-length type with Max length!");
                // Don't pad null values
                if (null != value)
                {
                    // Pad if necessary
                    if (value.Length < MaxLength)
                        value = value.PadRight((int)MaxLength);
                }
            }
            else if (SqlDbType.VarChar != SqlDbType &&
                     SqlDbType.NVarChar != SqlDbType &&
                     SqlDbType.Text != SqlDbType &&
                     SqlDbType.NText != SqlDbType)
            {
                ThrowInvalidType();
            }

            // Handle null values after type check
            if (null == value)
            {
                return null;
            }

            if (value.Length > MaxLength && Max != MaxLength)
                value = value.Remove((int)MaxLength, (int)(value.Length - MaxLength));

            return value;
        }

        public decimal Adjust(decimal value)
        {
            if (SqlDbType.Decimal != SqlDbType &&
                SqlDbType.Money != SqlDbType &&
                SqlDbType.SmallMoney != SqlDbType)
            {
                ThrowInvalidType();
            }

            if (SqlDbType.Decimal != SqlDbType)
            {
                VerifyMoneyRange(new SqlMoney(value));
                return value;
            }
            else
            {
                SqlDecimal sdValue = InternalAdjustSqlDecimal(new SqlDecimal(value));
                return sdValue.Value;
            }
        }

        public DateTime Adjust(DateTime value)
        {
            if (SqlDbType.DateTime == SqlDbType || SqlDbType.SmallDateTime == SqlDbType)
            {
                VerifyDateTimeRange(value);
            }
            else if (SqlDbType.DateTime2 == SqlDbType)
            {
                return new DateTime(InternalAdjustTimeTicks(value.Ticks));
            }
            else if (SqlDbType.Date == SqlDbType)
            {
                return value.Date;
            }
            else
            {
                ThrowInvalidType();
            }
            return value;
        }

        public Guid Adjust(Guid value)
        {
            if (SqlDbType.UniqueIdentifier != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlBoolean Adjust(SqlBoolean value)
        {
            if (SqlDbType.Bit != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlByte Adjust(SqlByte value)
        {
            if (SqlDbType.TinyInt != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlInt16 Adjust(SqlInt16 value)
        {
            if (SqlDbType.SmallInt != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlInt32 Adjust(SqlInt32 value)
        {
            if (SqlDbType.Int != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlInt64 Adjust(SqlInt64 value)
        {
            if (SqlDbType.BigInt != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlSingle Adjust(SqlSingle value)
        {
            if (SqlDbType.Real != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlDouble Adjust(SqlDouble value)
        {
            if (SqlDbType.Float != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlMoney Adjust(SqlMoney value)
        {
            if (SqlDbType.Money != SqlDbType &&
                SqlDbType.SmallMoney != SqlDbType)
                ThrowInvalidType();

            if (!value.IsNull)
                VerifyMoneyRange(value);

            return value;
        }

        public SqlDateTime Adjust(SqlDateTime value)
        {
            if (SqlDbType.DateTime != SqlDbType &&
                SqlDbType.SmallDateTime != SqlDbType)
                ThrowInvalidType();

            if (!value.IsNull)
                VerifyDateTimeRange(value.Value);

            return value;
        }

        public SqlDecimal Adjust(SqlDecimal value)
        {
            if (SqlDbType.Decimal != SqlDbType)
                ThrowInvalidType();
            return InternalAdjustSqlDecimal(value);
        }

        public SqlString Adjust(SqlString value)
        {
            if (SqlDbType.Char == SqlDbType || SqlDbType.NChar == SqlDbType)
            {
                //DBG.Assert(Max!=MaxLength, "SqlMetaData.Adjust(SqlString): Fixed-length type with Max length!");
                // Don't pad null values
                if (!value.IsNull)
                {
                    // Pad fixed-length types
                    if (value.Value.Length < MaxLength)
                        return new SqlString(value.Value.PadRight((int)MaxLength));
                }
            }
            else if (SqlDbType.VarChar != SqlDbType && SqlDbType.NVarChar != SqlDbType &&
                    SqlDbType.Text != SqlDbType && SqlDbType.NText != SqlDbType)
                ThrowInvalidType();

            // Handle null values after type check
            if (value.IsNull)
            {
                return value;
            }

            // trim all types
            if (value.Value.Length > MaxLength && Max != MaxLength)
                value = new SqlString(value.Value.Remove((int)MaxLength, (int)(value.Value.Length - MaxLength)));

            return value;
        }

        public SqlBinary Adjust(SqlBinary value)
        {
            if (SqlDbType.Binary == SqlDbType ||
                SqlDbType.Timestamp == SqlDbType)
            {
                if (!value.IsNull)
                {
                    // Pad fixed-length types
                    if (value.Length < MaxLength)
                    {
                        byte[] rgbValue = value.Value;
                        byte[] rgbNewValue = new byte[MaxLength];
                        Buffer.BlockCopy(rgbValue, 0, rgbNewValue, 0, rgbValue.Length);
                        Array.Clear(rgbNewValue, rgbValue.Length, rgbNewValue.Length - rgbValue.Length);
                        return new SqlBinary(rgbNewValue);
                    }
                }
            }
            else if (SqlDbType.VarBinary != SqlDbType &&
                    SqlDbType.Image != SqlDbType)
                ThrowInvalidType();

            // Handle null values
            if (value.IsNull)
            {
                return value;
            }

            // trim all types
            if (value.Length > MaxLength && Max != MaxLength)
            {
                byte[] rgbValue = value.Value;
                byte[] rgbNewValue = new byte[MaxLength];
                Buffer.BlockCopy(rgbValue, 0, rgbNewValue, 0, (int)MaxLength);
                value = new SqlBinary(rgbNewValue);
            }

            return value;
        }

        public SqlGuid Adjust(SqlGuid value)
        {
            if (SqlDbType.UniqueIdentifier != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public SqlChars Adjust(SqlChars value)
        {
            if (SqlDbType.Char == SqlDbType || SqlDbType.NChar == SqlDbType)
            {
                //DBG.Assert(Max!=MaxLength, "SqlMetaData.Adjust(SqlChars): Fixed-length type with Max length!");
                // Don't pad null values
                if (null != value && !value.IsNull)
                {
                    // Pad fixed-length types
                    long oldLength = value.Length;
                    if (oldLength < MaxLength)
                    {
                        // Make sure buffer is long enough
                        if (value.MaxLength < MaxLength)
                        {
                            char[] rgchNew = new char[(int)MaxLength];
                            Array.Copy(value.Buffer, 0, rgchNew, 0, (int)oldLength);
                            value = new SqlChars(rgchNew);
                        }

                        // pad extra space
                        char[] rgchTemp = value.Buffer;
                        for (long i = oldLength; i < MaxLength; i++)
                            rgchTemp[i] = ' ';

                        value.SetLength(MaxLength);
                        return value;
                    }
                }
            }
            else if (SqlDbType.VarChar != SqlDbType && SqlDbType.NVarChar != SqlDbType &&
                    SqlDbType.Text != SqlDbType && SqlDbType.NText != SqlDbType)
                ThrowInvalidType();

            // Handle null values after type check.
            if (null == value || value.IsNull)
            {
                return value;
            }

            // trim all types
            if (value.Length > MaxLength && Max != MaxLength)
                value.SetLength(MaxLength);

            return value;
        }

        public SqlBytes Adjust(SqlBytes value)
        {
            if (SqlDbType.Binary == SqlDbType || SqlDbType.Timestamp == SqlDbType)
            {
                //DBG.Assert(Max!=MaxLength, "SqlMetaData.Adjust(SqlBytes): Fixed-length type with Max length!");
                // Don't pad null values
                if (null != value && !value.IsNull)
                {
                    // Pad fixed-length types
                    int oldLength = (int)value.Length;
                    if (oldLength < MaxLength)
                    {
                        // Make sure buffer is long enough
                        if (value.MaxLength < MaxLength)
                        {
                            byte[] rgbNew = new byte[MaxLength];
                            Buffer.BlockCopy(value.Buffer, 0, rgbNew, 0, (int)oldLength);
                            value = new SqlBytes(rgbNew);
                        }

                        // pad extra space
                        byte[] rgbTemp = value.Buffer;
                        Array.Clear(rgbTemp, oldLength, rgbTemp.Length - oldLength);
                        value.SetLength(MaxLength);
                        return value;
                    }
                }
            }
            else if (SqlDbType.VarBinary != SqlDbType &&
                     SqlDbType.Image != SqlDbType)
                ThrowInvalidType();

            // Handle null values after type check.
            if (null == value || value.IsNull)
            {
                return value;
            }

            // trim all types
            if (value.Length > MaxLength && Max != MaxLength)
                value.SetLength(MaxLength);

            return value;
        }

        public SqlXml Adjust(SqlXml value)
        {
            if (SqlDbType.Xml != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public TimeSpan Adjust(TimeSpan value)
        {
            if (SqlDbType.Time != SqlDbType)
                ThrowInvalidType();
            VerifyTimeRange(value);
            return new TimeSpan(InternalAdjustTimeTicks(value.Ticks));
        }

        public DateTimeOffset Adjust(DateTimeOffset value)
        {
            if (SqlDbType.DateTimeOffset != SqlDbType)
                ThrowInvalidType();
            return new DateTimeOffset(InternalAdjustTimeTicks(value.Ticks), value.Offset);
        }

        public object Adjust(object value)
        {
            // Pass null references through
            if (null == value)
                return null;

            if (value is bool)
                value = this.Adjust((Boolean)value);
            else if (value is byte)
                value = this.Adjust((Byte)value);
            else if (value is char)
                value = this.Adjust((Char)value);
            else if (value is DateTime)
                value = this.Adjust((DateTime)value);
            else if (value is DBNull)
            { /* DBNull passes through as is for all types */ }
            else if (value is decimal)
                value = this.Adjust((Decimal)value);
            else if (value is double)
                value = this.Adjust((Double)value);
            else if (value is short)
                value = this.Adjust((Int16)value);
            else if (value is int)
                value = this.Adjust((Int32)value);
            else if (value is long)
                value = this.Adjust((Int64)value);
            else if (value is sbyte)
                throw ADP.InvalidDataType("SByte");
            else if (value is float)
                value = this.Adjust((Single)value);
            else if (value is string)
                value = this.Adjust((String)value);
            else if (value is ushort)
                throw ADP.InvalidDataType("UInt16");
            else if (value is uint)
                throw ADP.InvalidDataType("UInt32");
            else if (value is ulong)
                throw ADP.InvalidDataType("UInt64");
            else if (value is byte[])
                value = this.Adjust((System.Byte[])value);
            else if (value is char[])
                value = this.Adjust((System.Char[])value);
            else if (value is Guid)
                value = this.Adjust((System.Guid)value);
            else if (value is SqlBinary)
                value = this.Adjust((SqlBinary)value);
            else if (value is SqlBoolean)
                value = this.Adjust((SqlBoolean)value);
            else if (value is SqlByte)
                value = this.Adjust((SqlByte)value);
            else if (value is SqlDateTime)
                value = this.Adjust((SqlDateTime)value);
            else if (value is SqlDouble)
                value = this.Adjust((SqlDouble)value);
            else if (value is SqlGuid)
                value = this.Adjust((SqlGuid)value);
            else if (value is SqlInt16)
                value = this.Adjust((SqlInt16)value);
            else if (value is SqlInt32)
                value = this.Adjust((SqlInt32)value);
            else if (value is SqlInt64)
                value = this.Adjust((SqlInt64)value);
            else if (value is SqlMoney)
                value = this.Adjust((SqlMoney)value);
            else if (value is SqlDecimal)
                value = this.Adjust((SqlDecimal)value);
            else if (value is SqlSingle)
                value = this.Adjust((SqlSingle)value);
            else if (value is SqlString)
                value = this.Adjust((SqlString)value);
            else if (value is SqlChars)
                value = this.Adjust((SqlChars)value);
            else if (value is SqlBytes)
                value = this.Adjust((SqlBytes)value);
            else if (value is SqlXml)
                value = this.Adjust((SqlXml)value);
            else if (value is TimeSpan)
                value = this.Adjust((TimeSpan)value);
            else if (value is DateTimeOffset)
                value = this.Adjust((DateTimeOffset)value);
            else
                throw ADP.UnknownDataType(value.GetType());

            return value;
        }

        public static SqlMetaData InferFromValue(object value, String name)
        {
            if (value == null)
                throw ADP.ArgumentNull("value");

            SqlMetaData smd;

            if (value is Boolean) smd = new SqlMetaData(name, SqlDbType.Bit);
            else if (value is Byte) smd = new SqlMetaData(name, SqlDbType.TinyInt);
            else if (value is Char) smd = new SqlMetaData(name, SqlDbType.NVarChar, 1);
            else if (value is DateTime) smd = new SqlMetaData(name, SqlDbType.DateTime);
            else if (value is DBNull) throw ADP.InvalidDataType("DBNull");
            else if (value is Decimal)
            {
                // use logic inside SqlDecimal to infer precision and scale.
                SqlDecimal sd = new SqlDecimal((Decimal)value);
                smd = new SqlMetaData(name, SqlDbType.Decimal, sd.Precision, sd.Scale);
            }
            else if (value is Double) smd = new SqlMetaData(name, SqlDbType.Float);
            else if (value is Int16) smd = new SqlMetaData(name, SqlDbType.SmallInt);
            else if (value is Int32) smd = new SqlMetaData(name, SqlDbType.Int);
            else if (value is Int64) smd = new SqlMetaData(name, SqlDbType.BigInt);
            else if (value is SByte) throw ADP.InvalidDataType("SByte");
            else if (value is Single) smd = new SqlMetaData(name, SqlDbType.Real);
            else if (value is String)
            {
                long maxLen = ((String)value).Length;
                if (maxLen < 1) maxLen = 1;

                if (x_lServerMaxUnicode < maxLen)
                    maxLen = Max;

                smd = new SqlMetaData(name, SqlDbType.NVarChar, maxLen);
            }
            else if (value is UInt16) throw ADP.InvalidDataType("UInt16");
            else if (value is UInt32) throw ADP.InvalidDataType("UInt32");
            else if (value is UInt64) throw ADP.InvalidDataType("UInt64");
            else if (value is System.Byte[])
            {
                long maxLen = ((System.Byte[])value).Length;
                if (maxLen < 1) maxLen = 1;

                if (x_lServerMaxBinary < maxLen)
                    maxLen = Max;

                smd = new SqlMetaData(name, SqlDbType.VarBinary, maxLen);
            }
            else if (value is System.Char[])
            {
                long maxLen = ((System.Char[])value).Length;
                if (maxLen < 1) maxLen = 1;

                if (x_lServerMaxUnicode < maxLen)
                    maxLen = Max;

                smd = new SqlMetaData(name, SqlDbType.NVarChar, maxLen);
            }
            else if (value is System.Guid)
                smd = new SqlMetaData(name, SqlDbType.UniqueIdentifier);
            else if (value is System.Object)
                smd = new SqlMetaData(name, SqlDbType.Variant);
            else if (value is SqlBinary)
            {
                long maxLen;
                SqlBinary sb = ((SqlBinary)value);
                if (!sb.IsNull)
                {
                    maxLen = sb.Length;
                    if (maxLen < 1) maxLen = 1;

                    if (x_lServerMaxBinary < maxLen)
                        maxLen = Max;
                }
                else
                    maxLen = sxm_rgDefaults[(int)SqlDbType.VarBinary].MaxLength;

                smd = new SqlMetaData(name, SqlDbType.VarBinary, maxLen);
            }
            else if (value is SqlBoolean)
                smd = new SqlMetaData(name, SqlDbType.Bit);
            else if (value is SqlByte)
                smd = new SqlMetaData(name, SqlDbType.TinyInt);
            else if (value is SqlDateTime)
                smd = new SqlMetaData(name, SqlDbType.DateTime);
            else if (value is SqlDouble)
                smd = new SqlMetaData(name, SqlDbType.Float);
            else if (value is SqlGuid)
                smd = new SqlMetaData(name, SqlDbType.UniqueIdentifier);
            else if (value is SqlInt16)
                smd = new SqlMetaData(name, SqlDbType.SmallInt);
            else if (value is SqlInt32)
                smd = new SqlMetaData(name, SqlDbType.Int);
            else if (value is SqlInt64)
                smd = new SqlMetaData(name, SqlDbType.BigInt);
            else if (value is SqlMoney)
                smd = new SqlMetaData(name, SqlDbType.Money);
            else if (value is SqlDecimal)
            {
                byte bPrec;
                byte scale;
                SqlDecimal sd = (SqlDecimal)value;
                if (!sd.IsNull)
                {
                    bPrec = sd.Precision;
                    scale = sd.Scale;
                }
                else
                {
                    bPrec = sxm_rgDefaults[(int)SqlDbType.Decimal].Precision;
                    scale = sxm_rgDefaults[(int)SqlDbType.Decimal].Scale;
                }
                smd = new SqlMetaData(name, SqlDbType.Decimal, bPrec, scale);
            }
            else if (value is SqlSingle)
                smd = new SqlMetaData(name, SqlDbType.Real);
            else if (value is SqlString)
            {
                SqlString ss = (SqlString)value;
                if (!ss.IsNull)
                {
                    long maxLen = ss.Value.Length;
                    if (maxLen < 1) maxLen = 1;

                    if (maxLen > x_lServerMaxUnicode)
                        maxLen = Max;

                    smd = new SqlMetaData(name, SqlDbType.NVarChar, maxLen, ss.LCID, ss.SqlCompareOptions);
                }
                else
                {
                    smd = new SqlMetaData(name, SqlDbType.NVarChar, sxm_rgDefaults[(int)SqlDbType.NVarChar].MaxLength);
                }
            }
            else if (value is SqlChars)
            {
                long maxLen;
                SqlChars sch = (SqlChars)value;
                if (!sch.IsNull)
                {
                    maxLen = sch.Length;
                    if (maxLen < 1) maxLen = 1;

                    if (maxLen > x_lServerMaxUnicode)
                        maxLen = Max;
                }
                else
                    maxLen = sxm_rgDefaults[(int)SqlDbType.NVarChar].MaxLength;

                smd = new SqlMetaData(name, SqlDbType.NVarChar, maxLen);
            }
            else if (value is SqlBytes)
            {
                long maxLen;
                SqlBytes sb = (SqlBytes)value;
                if (!sb.IsNull)
                {
                    maxLen = sb.Length;
                    if (maxLen < 1) maxLen = 1;
                    else if (x_lServerMaxBinary < maxLen) maxLen = Max;
                }
                else
                    maxLen = sxm_rgDefaults[(int)SqlDbType.VarBinary].MaxLength;

                smd = new SqlMetaData(name, SqlDbType.VarBinary, maxLen);
            }
            else if (value is SqlXml)
                smd = new SqlMetaData(name, SqlDbType.Xml);
            else if (value is TimeSpan)
                smd = new SqlMetaData(name, SqlDbType.Time, 0, InferScaleFromTimeTicks(((TimeSpan)value).Ticks));
            else if (value is DateTimeOffset)
                smd = new SqlMetaData(name, SqlDbType.DateTimeOffset, 0, InferScaleFromTimeTicks(((DateTimeOffset)value).Ticks));
            else
                throw ADP.UnknownDataType(value.GetType());

            return smd;
        }

        public bool Adjust(bool value)
        {
            if (SqlDbType.Bit != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public byte Adjust(byte value)
        {
            if (SqlDbType.TinyInt != SqlDbType)
                ThrowInvalidType();
            return value;
        }

        public byte[] Adjust(byte[] value)
        {
            if (SqlDbType.Binary == SqlDbType || SqlDbType.Timestamp == SqlDbType)
            {
                //DBG.Assert(Max!=MaxLength, "SqlMetaData.Adjust(byte[]): Fixed-length type with Max length!");
                // Don't pad null values
                if (null != value)
                {
                    // Pad fixed-length types
                    if (value.Length < MaxLength)
                    {
                        byte[] rgbNewValue = new byte[MaxLength];
                        Buffer.BlockCopy(value, 0, rgbNewValue, 0, value.Length);
                        Array.Clear(rgbNewValue, value.Length, (int)rgbNewValue.Length - value.Length);
                        return rgbNewValue;
                    }
                }
            }
            else if (SqlDbType.VarBinary != SqlDbType &&
                    SqlDbType.Image != SqlDbType)
                ThrowInvalidType();

            // Handle null values after type check
            if (null == value)
            {
                return null;
            }

            // trim all types
            if (value.Length > MaxLength && Max != MaxLength)
            {
                byte[] rgbNewValue = new byte[MaxLength];
                Buffer.BlockCopy(value, 0, rgbNewValue, 0, (int)MaxLength);
                value = rgbNewValue;
            }

            return value;
        }

        public char Adjust(char value)
        {
            if (SqlDbType.Char == SqlDbType || SqlDbType.NChar == SqlDbType)
            {
                if (1 != MaxLength)
                    ThrowInvalidType();
            }
            else if ((1 > MaxLength) ||  // char must have max length of at least 1
                    (SqlDbType.VarChar != SqlDbType && SqlDbType.NVarChar != SqlDbType &&
                    SqlDbType.Text != SqlDbType && SqlDbType.NText != SqlDbType)
                    )
                ThrowInvalidType();

            return value;
        }

        public char[] Adjust(char[] value)
        {
            if (SqlDbType.Char == SqlDbType || SqlDbType.NChar == SqlDbType)
            {
                //DBG.Assert(Max!=MaxLength, "SqlMetaData.Adjust(byte[]): Fixed-length type with Max length!");
                // Don't pad null values
                if (null != value)
                {
                    // Pad fixed-length types
                    long oldLength = value.Length;
                    if (oldLength < MaxLength)
                    {
                        char[] rgchNew = new char[(int)MaxLength];
                        Array.Copy(value, 0, rgchNew, 0, (int)oldLength);

                        // pad extra space
                        for (long i = oldLength; i < rgchNew.Length; i++)
                            rgchNew[i] = ' ';

                        return rgchNew;
                    }
                }
            }
            else if (SqlDbType.VarChar != SqlDbType && SqlDbType.NVarChar != SqlDbType &&
                    SqlDbType.Text != SqlDbType && SqlDbType.NText != SqlDbType)
                ThrowInvalidType();

            // Handle null values after type check
            if (null == value)
            {
                return null;
            }

            // trim all types
            if (value.Length > MaxLength && Max != MaxLength)
            {
                char[] rgchNewValue = new char[MaxLength];
                Array.Copy(value, 0, rgchNewValue, 0, (int)MaxLength);
                value = rgchNewValue;
            }


            return value;
        }


        internal static SqlMetaData GetPartialLengthMetaData(SqlMetaData md)
        {
            if (md.IsPartialLength == true)
            {
                return md;
            }
            if (md.SqlDbType == SqlDbType.Xml)
                ThrowInvalidType();     //Xml should always have IsPartialLength = true

            if (md.SqlDbType == SqlDbType.NVarChar || md.SqlDbType == SqlDbType.VarChar ||
                    md.SqlDbType == SqlDbType.VarBinary)
            {
                SqlMetaData mdnew = new SqlMetaData(md.Name, md.SqlDbType, SqlMetaData.Max, 0, 0, md.LocaleId,
                    md.CompareOptions, null, null, null, true
                        );
                return mdnew;
            }
            else
                return md;
        }


        private static void ThrowInvalidType()
        {
            throw ADP.InvalidMetaDataValue();
        }

        // Hard coding smalldatetime limits...
        private static readonly DateTime s_dtSmallMax = new DateTime(2079, 06, 06, 23, 59, 29, 998);
        private static readonly DateTime s_dtSmallMin = new DateTime(1899, 12, 31, 23, 59, 29, 999);
        private void VerifyDateTimeRange(DateTime value)
        {
            if (SqlDbType.SmallDateTime == SqlDbType && (s_dtSmallMax < value || s_dtSmallMin > value))
                ThrowInvalidType();
        }

        private static readonly SqlMoney s_smSmallMax = new SqlMoney(((Decimal)Int32.MaxValue) / 10000);
        private static readonly SqlMoney s_smSmallMin = new SqlMoney(((Decimal)Int32.MinValue) / 10000);
        private void VerifyMoneyRange(SqlMoney value)
        {
            if (SqlDbType.SmallMoney == SqlDbType && ((s_smSmallMax < value).Value || (s_smSmallMin > value).Value))
                ThrowInvalidType();
        }

        private SqlDecimal InternalAdjustSqlDecimal(SqlDecimal value)
        {
            if (!value.IsNull && (value.Precision != Precision || value.Scale != Scale))
            {
                // Force truncation if target scale is smaller than actual scale.
                if (value.Scale != Scale)
                {
                    value = SqlDecimal.AdjustScale(value, Scale - value.Scale, false /* Don't round, truncate. */);
                }
                return SqlDecimal.ConvertToPrecScale(value, Precision, Scale);
            }

            return value;
        }

        private static readonly TimeSpan s_timeMin = TimeSpan.Zero;
        private static readonly TimeSpan s_timeMax = new TimeSpan(TimeSpan.TicksPerDay - 1);
        private void VerifyTimeRange(TimeSpan value)
        {
            if (SqlDbType.Time == SqlDbType && (s_timeMin > value || value > s_timeMax))
            {
                ThrowInvalidType();
            }
        }

        private static readonly Int64[] s_unitTicksFromScale = {
            10000000,
            1000000,
            100000,
            10000,
            1000,
            100,
            10,
            1,
        };

        private Int64 InternalAdjustTimeTicks(Int64 ticks)
        {
            return (ticks / s_unitTicksFromScale[Scale] * s_unitTicksFromScale[Scale]);
        }

        private static byte InferScaleFromTimeTicks(Int64 ticks)
        {
            for (byte scale = 0; scale < MaxTimeScale; ++scale)
            {
                if ((ticks / s_unitTicksFromScale[scale] * s_unitTicksFromScale[scale]) == ticks)
                {
                    return scale;
                }
            }
            return MaxTimeScale;
        }

        private void SetDefaultsForType(SqlDbType dbType)
        {
            if (SqlDbType.BigInt <= dbType && SqlDbType.DateTimeOffset >= dbType)
            {
                SqlMetaData smdDflt = sxm_rgDefaults[(int)dbType];
                _sqlDbType = dbType;
                _lMaxLength = smdDflt.MaxLength;
                _bPrecision = smdDflt.Precision;
                _bScale = smdDflt.Scale;
                _lLocale = smdDflt.LocaleId;
                _eCompareOptions = smdDflt.CompareOptions;
            }
        }

        // Array of default-valued metadata ordered by corresponding SqlDbType.
        internal static SqlMetaData[] sxm_rgDefaults =
            {
            //    new SqlMetaData(name, DbType, SqlDbType, MaxLen, Prec, Scale, Locale, DatabaseName, SchemaName, isPartialLength)
            new SqlMetaData("bigint", SqlDbType.BigInt,
                    8, 19, 0, 0, SqlCompareOptions.None,  false),            // SqlDbType.BigInt
            new SqlMetaData("binary", SqlDbType.Binary,
                    1, 0, 0, 0, SqlCompareOptions.None,  false),                // SqlDbType.Binary
            new SqlMetaData("bit", SqlDbType.Bit,
                    1, 1, 0, 0, SqlCompareOptions.None, false),                // SqlDbType.Bit
            new SqlMetaData("char", SqlDbType.Char,
                    1, 0, 0, 0, x_eDefaultStringCompareOptions,  false),                // SqlDbType.Char
            new SqlMetaData("datetime", SqlDbType.DateTime,
                    8, 23, 3, 0, SqlCompareOptions.None, false),            // SqlDbType.DateTime
            new SqlMetaData("decimal", SqlDbType.Decimal,
                    9, 18, 0, 0, SqlCompareOptions.None,  false),            // SqlDbType.Decimal
            new SqlMetaData("float", SqlDbType.Float,
                    8, 53, 0, 0, SqlCompareOptions.None, false),            // SqlDbType.Float
            new SqlMetaData("image", SqlDbType.Image,
                    x_lMax, 0, 0, 0, SqlCompareOptions.None, false),                // SqlDbType.Image
            new SqlMetaData("int", SqlDbType.Int,
                    4, 10, 0, 0, SqlCompareOptions.None, false),            // SqlDbType.Int
            new SqlMetaData("money", SqlDbType.Money,
                    8, 19, 4, 0, SqlCompareOptions.None, false),            // SqlDbType.Money
            new SqlMetaData("nchar", SqlDbType.NChar,
                    1, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // SqlDbType.NChar
            new SqlMetaData("ntext", SqlDbType.NText,
                    x_lMax, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // SqlDbType.NText
            new SqlMetaData("nvarchar", SqlDbType.NVarChar,
                    x_lServerMaxUnicode, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // SqlDbType.NVarChar
            new SqlMetaData("real", SqlDbType.Real,
                    4, 24, 0, 0, SqlCompareOptions.None, false),            // SqlDbType.Real
            new SqlMetaData("uniqueidentifier", SqlDbType.UniqueIdentifier,
                    16, 0, 0, 0, SqlCompareOptions.None, false),            // SqlDbType.UniqueIdentifier
            new SqlMetaData("smalldatetime", SqlDbType.SmallDateTime,
                    4, 16, 0, 0, SqlCompareOptions.None, false),            // SqlDbType.SmallDateTime
            new SqlMetaData("smallint", SqlDbType.SmallInt,
                    2, 5, 0, 0, SqlCompareOptions.None, false),                                    // SqlDbType.SmallInt
            new SqlMetaData("smallmoney", SqlDbType.SmallMoney,
                    4, 10, 4, 0, SqlCompareOptions.None, false),                // SqlDbType.SmallMoney
            new SqlMetaData("text", SqlDbType.Text,
                    x_lMax, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // SqlDbType.Text
            new SqlMetaData("timestamp", SqlDbType.Timestamp,
                    8, 0, 0, 0, SqlCompareOptions.None, false),                // SqlDbType.Timestamp
            new SqlMetaData("tinyint", SqlDbType.TinyInt,
                    1, 3, 0, 0, SqlCompareOptions.None, false),                // SqlDbType.TinyInt
            new SqlMetaData("varbinary", SqlDbType.VarBinary,
                    x_lServerMaxBinary, 0, 0, 0, SqlCompareOptions.None, false),                // SqlDbType.VarBinary
            new SqlMetaData("varchar", SqlDbType.VarChar,
                    x_lServerMaxANSI, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // SqlDbType.VarChar
            new SqlMetaData("sql_variant", SqlDbType.Variant,
                    8016, 0, 0, 0, SqlCompareOptions.None, false),            // SqlDbType.Variant
            new SqlMetaData("nvarchar", SqlDbType.NVarChar,
                    1, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // Placeholder for value 24
            new SqlMetaData("xml", SqlDbType.Xml,
                    x_lMax, 0, 0, 0, x_eDefaultStringCompareOptions, true),                // SqlDbType.Xml
            new SqlMetaData("nvarchar", SqlDbType.NVarChar,
                    1, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // Placeholder for value 26
            new SqlMetaData("nvarchar", SqlDbType.NVarChar,
                    x_lServerMaxUnicode, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // Placeholder for value 27
            new SqlMetaData("nvarchar", SqlDbType.NVarChar,
                    x_lServerMaxUnicode, 0, 0, 0, x_eDefaultStringCompareOptions, false),                // Placeholder for value 28
            new SqlMetaData("udt", SqlDbType.Structured,
                    0, 0, 0, 0, SqlCompareOptions.None, false),   // Placeholder for udt (value 29) 
            new SqlMetaData("table", SqlDbType.Structured,
                    0, 0, 0, 0, SqlCompareOptions.None, false),                // SqlDbType.Structured
            new SqlMetaData("date", SqlDbType.Date,
                    3, 10,0, 0, SqlCompareOptions.None, false),                // SqlDbType.Date
            new SqlMetaData("time", SqlDbType.Time,
                    5, 0, 7, 0, SqlCompareOptions.None, false),                // SqlDbType.Time
            new SqlMetaData("datetime2", SqlDbType.DateTime2,
                    8, 0, 7, 0, SqlCompareOptions.None, false),                // SqlDbType.DateTime2
            new SqlMetaData("datetimeoffset", SqlDbType.DateTimeOffset,
                   10, 0, 7, 0, SqlCompareOptions.None, false),                // SqlDbType.DateTimeOffset
            };
        private void ThrowIfUdt(SqlDbType dbType)
        {
            if (dbType == SqlDbType.Udt)
            {
                throw ADP.DbTypeNotSupported(SqlDbType.Udt.ToString());
            }
        }
    }
}
