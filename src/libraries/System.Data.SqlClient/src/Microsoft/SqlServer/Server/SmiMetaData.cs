// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;

namespace Microsoft.SqlServer.Server
{
    // DESIGN NOTES
    //
    //  The following classes are a tight inheritance hierarchy, and are not designed for
    //  being inherited outside of this file.  Instances are guaranteed to be immutable, and
    //  outside classes rely on this fact.
    //
    //  The various levels may not all be used outside of this file, but for clarity of purpose
    //  they are all useful distinctions to make.
    //
    //  In general, moving lower in the type hierarchy exposes less portable values.  Thus,
    //  the root metadata can be readily shared across different (MSSQL) servers and clients,
    //  while QueryMetaData has attributes tied to a specific query, running against specific
    //  data storage on a specific server.
    //
    //  The SmiMetaData hierarchy does not do data validation on retail builds!  It will assert
    //  that the values passed to it have been validated externally, however.
    //


    // SmiMetaData
    //
    //  Root of the hierarchy.
    //  Represents the minimal amount of metadata required to represent any Sql Server datum
    //  without any references to any particular server or schema (thus, no server-specific multi-part names).
    //  It could be used to communicate solely between two disconnected clients, for instance.
    //
    //  NOTE: It currently does not contain sufficient information to describe typed XML, since we
    //      don't have a good server-independent mechanism for such.
    //
    //  This class is also used as implementation for the public SqlMetaData class.
    internal class SmiMetaData
    {
        private SqlDbType _databaseType;          // Main enum that determines what is valid for other attributes.
        private long _maxLength;             // Varies for variable-length types, others are fixed value per type
        private byte _precision;             // Varies for SqlDbType.Decimal, others are fixed value per type
        private byte _scale;                 // Varies for SqlDbType.Decimal, others are fixed value per type
        private long _localeId;              // Valid only for character types, others are 0
        private SqlCompareOptions _compareOptions;        // Valid only for character types, others are SqlCompareOptions.Default
        private bool _isMultiValued;         // Multiple instances per value? (I.e. tables, arrays)
        private IList<SmiExtendedMetaData> _fieldMetaData;         // Metadata of fields for structured types
        private SmiMetaDataPropertyCollection _extendedProperties;  // Extended properties, Key columns, sort order, etc.

        // DevNote: For now, since the list of extended property types is small, we can handle them in a simple list.
        //  In the future, we may need to create a more performant storage & lookup mechanism, such as a hash table
        //  of lists indexed by type of property or an array of lists with a well-known index for each type.

        // Limits for attributes (SmiMetaData will assert that these limits as applicable in constructor)
        internal const long UnlimitedMaxLengthIndicator = -1;  // unlimited (except by implementation) max-length.
        internal const long MaxUnicodeCharacters = 4000;        // Maximum for limited type
        internal const long MaxANSICharacters = 8000;           // Maximum for limited type
        internal const long MaxBinaryLength = 8000;             // Maximum for limited type
        internal const int MinPrecision = 1;       // SqlDecimal defines max precision
        internal const int MinScale = 0;            // SqlDecimal defines max scale
        internal const int MaxTimeScale = 7;        // Max scale for time, datetime2, and datetimeoffset
        internal static readonly DateTime MaxSmallDateTime = new DateTime(2079, 06, 06, 23, 59, 29, 998);
        internal static readonly DateTime MinSmallDateTime = new DateTime(1899, 12, 31, 23, 59, 29, 999);
        internal static readonly SqlMoney MaxSmallMoney = new SqlMoney(((Decimal)Int32.MaxValue) / 10000);
        internal static readonly SqlMoney MinSmallMoney = new SqlMoney(((Decimal)Int32.MinValue) / 10000);
        internal const SqlCompareOptions DefaultStringCompareOptions = SqlCompareOptions.IgnoreCase
                                        | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth;

        internal const long MaxNameLength = 128;        // maximum length in the server is 128.
        private static readonly IList<SmiExtendedMetaData> s_emptyFieldList = new SmiExtendedMetaData[0];


        // Precision to max length lookup table
        private static byte[] s_maxLenFromPrecision = new byte[] {5,5,5,5,5,5,5,5,5,9,9,9,9,9,
            9,9,9,9,9,13,13,13,13,13,13,13,13,13,17,17,17,17,17,17,17,17,17,17};

        // Scale offset to max length lookup table
        private static byte[] s_maxVarTimeLenOffsetFromScale = new byte[] { 2, 2, 2, 1, 1, 0, 0, 0 };

        // Defaults
        //    SmiMetaData(SqlDbType,                  MaxLen,                     Prec, Scale,  CompareOptions)
        internal static readonly SmiMetaData DefaultBigInt = new SmiMetaData(SqlDbType.BigInt, 8, 19, 0, SqlCompareOptions.None);     // SqlDbType.BigInt
        internal static readonly SmiMetaData DefaultBinary = new SmiMetaData(SqlDbType.Binary, 1, 0, 0, SqlCompareOptions.None);     // SqlDbType.Binary
        internal static readonly SmiMetaData DefaultBit = new SmiMetaData(SqlDbType.Bit, 1, 1, 0, SqlCompareOptions.None);     // SqlDbType.Bit
        internal static readonly SmiMetaData DefaultChar_NoCollation = new SmiMetaData(SqlDbType.Char, 1, 0, 0, DefaultStringCompareOptions);// SqlDbType.Char
        internal static readonly SmiMetaData DefaultDateTime = new SmiMetaData(SqlDbType.DateTime, 8, 23, 3, SqlCompareOptions.None);     // SqlDbType.DateTime
        internal static readonly SmiMetaData DefaultDecimal = new SmiMetaData(SqlDbType.Decimal, 9, 18, 0, SqlCompareOptions.None);     // SqlDbType.Decimal
        internal static readonly SmiMetaData DefaultFloat = new SmiMetaData(SqlDbType.Float, 8, 53, 0, SqlCompareOptions.None);     // SqlDbType.Float
        internal static readonly SmiMetaData DefaultImage = new SmiMetaData(SqlDbType.Image, UnlimitedMaxLengthIndicator, 0, 0, SqlCompareOptions.None);     // SqlDbType.Image
        internal static readonly SmiMetaData DefaultInt = new SmiMetaData(SqlDbType.Int, 4, 10, 0, SqlCompareOptions.None);     // SqlDbType.Int
        internal static readonly SmiMetaData DefaultMoney = new SmiMetaData(SqlDbType.Money, 8, 19, 4, SqlCompareOptions.None);     // SqlDbType.Money
        internal static readonly SmiMetaData DefaultNChar_NoCollation = new SmiMetaData(SqlDbType.NChar, 1, 0, 0, DefaultStringCompareOptions);// SqlDbType.NChar
        internal static readonly SmiMetaData DefaultNText_NoCollation = new SmiMetaData(SqlDbType.NText, UnlimitedMaxLengthIndicator, 0, 0, DefaultStringCompareOptions);// SqlDbType.NText
        internal static readonly SmiMetaData DefaultNVarChar_NoCollation = new SmiMetaData(SqlDbType.NVarChar, MaxUnicodeCharacters, 0, 0, DefaultStringCompareOptions);// SqlDbType.NVarChar
        internal static readonly SmiMetaData DefaultReal = new SmiMetaData(SqlDbType.Real, 4, 24, 0, SqlCompareOptions.None);     // SqlDbType.Real
        internal static readonly SmiMetaData DefaultUniqueIdentifier = new SmiMetaData(SqlDbType.UniqueIdentifier, 16, 0, 0, SqlCompareOptions.None);     // SqlDbType.UniqueIdentifier
        internal static readonly SmiMetaData DefaultSmallDateTime = new SmiMetaData(SqlDbType.SmallDateTime, 4, 16, 0, SqlCompareOptions.None);     // SqlDbType.SmallDateTime
        internal static readonly SmiMetaData DefaultSmallInt = new SmiMetaData(SqlDbType.SmallInt, 2, 5, 0, SqlCompareOptions.None);     // SqlDbType.SmallInt
        internal static readonly SmiMetaData DefaultSmallMoney = new SmiMetaData(SqlDbType.SmallMoney, 4, 10, 4, SqlCompareOptions.None);     // SqlDbType.SmallMoney
        internal static readonly SmiMetaData DefaultText_NoCollation = new SmiMetaData(SqlDbType.Text, UnlimitedMaxLengthIndicator, 0, 0, DefaultStringCompareOptions);// SqlDbType.Text
        internal static readonly SmiMetaData DefaultTimestamp = new SmiMetaData(SqlDbType.Timestamp, 8, 0, 0, SqlCompareOptions.None);     // SqlDbType.Timestamp
        internal static readonly SmiMetaData DefaultTinyInt = new SmiMetaData(SqlDbType.TinyInt, 1, 3, 0, SqlCompareOptions.None);     // SqlDbType.TinyInt
        internal static readonly SmiMetaData DefaultVarBinary = new SmiMetaData(SqlDbType.VarBinary, MaxBinaryLength, 0, 0, SqlCompareOptions.None);     // SqlDbType.VarBinary
        internal static readonly SmiMetaData DefaultVarChar_NoCollation = new SmiMetaData(SqlDbType.VarChar, MaxANSICharacters, 0, 0, DefaultStringCompareOptions);// SqlDbType.VarChar
        internal static readonly SmiMetaData DefaultVariant = new SmiMetaData(SqlDbType.Variant, 8016, 0, 0, SqlCompareOptions.None);     // SqlDbType.Variant
        internal static readonly SmiMetaData DefaultXml = new SmiMetaData(SqlDbType.Xml, UnlimitedMaxLengthIndicator, 0, 0, DefaultStringCompareOptions);// SqlDbType.Xml
        internal static readonly SmiMetaData DefaultStructured = new SmiMetaData(SqlDbType.Structured, 0, 0, 0, SqlCompareOptions.None);     // SqlDbType.Structured
        internal static readonly SmiMetaData DefaultDate = new SmiMetaData(SqlDbType.Date, 3, 10, 0, SqlCompareOptions.None);     // SqlDbType.Date
        internal static readonly SmiMetaData DefaultTime = new SmiMetaData(SqlDbType.Time, 5, 0, 7, SqlCompareOptions.None);     // SqlDbType.Time
        internal static readonly SmiMetaData DefaultDateTime2 = new SmiMetaData(SqlDbType.DateTime2, 8, 0, 7, SqlCompareOptions.None);     // SqlDbType.DateTime2
        internal static readonly SmiMetaData DefaultDateTimeOffset = new SmiMetaData(SqlDbType.DateTimeOffset, 10, 0, 7, SqlCompareOptions.None);     // SqlDbType.DateTimeOffset
        // No default for generic UDT

        internal static SmiMetaData DefaultNVarChar
        {
            get
            {
                return new SmiMetaData(
                        DefaultNVarChar_NoCollation.SqlDbType,
                        DefaultNVarChar_NoCollation.MaxLength,
                        DefaultNVarChar_NoCollation.Precision,
                        DefaultNVarChar_NoCollation.Scale,
                        CultureInfo.CurrentCulture.LCID,
                        SqlCompareOptions.IgnoreCase | SqlCompareOptions.IgnoreKanaType | SqlCompareOptions.IgnoreWidth);
            }
        }

        internal SmiMetaData(
                                SqlDbType dbType,
                                long maxLength,
                                byte precision,
                                byte scale,
                                long localeId,
                                SqlCompareOptions compareOptions) :
                        this(dbType,
                                maxLength,
                                precision,
                                scale,
                                localeId,
                                compareOptions,
                                false,
                                null,
                                null)
        {
        }

        // SMI V220 ctor.
        internal SmiMetaData(
                                SqlDbType dbType,
                                long maxLength,
                                byte precision,
                                byte scale,
                                long localeId,
                                SqlCompareOptions compareOptions,
                                bool isMultiValued,
                                IList<SmiExtendedMetaData> fieldTypes,
                                SmiMetaDataPropertyCollection extendedProperties)
        {
            Debug.Assert(IsSupportedDbType(dbType), "Invalid SqlDbType: " + dbType);

            SetDefaultsForType(dbType);

            switch (dbType)
            {
                case SqlDbType.BigInt:
                case SqlDbType.Bit:
                case SqlDbType.DateTime:
                case SqlDbType.Float:
                case SqlDbType.Image:
                case SqlDbType.Int:
                case SqlDbType.Money:
                case SqlDbType.Real:
                case SqlDbType.SmallDateTime:
                case SqlDbType.SmallInt:
                case SqlDbType.SmallMoney:
                case SqlDbType.Timestamp:
                case SqlDbType.TinyInt:
                case SqlDbType.UniqueIdentifier:
                case SqlDbType.Variant:
                case SqlDbType.Xml:
                case SqlDbType.Date:
                    break;
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                    _maxLength = maxLength;
                    break;
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                    // locale and compare options are not validated until they get to the server
                    _maxLength = maxLength;
                    _localeId = localeId;
                    _compareOptions = compareOptions;
                    break;
                case SqlDbType.NText:
                case SqlDbType.Text:
                    _localeId = localeId;
                    _compareOptions = compareOptions;
                    break;
                case SqlDbType.Decimal:
                    Debug.Assert(MinPrecision <= precision && SqlDecimal.MaxPrecision >= precision, "Invalid precision: " + precision);
                    Debug.Assert(MinScale <= scale && SqlDecimal.MaxScale >= scale, "Invalid scale: " + scale);
                    Debug.Assert(scale <= precision, "Precision: " + precision + " greater than scale: " + scale);
                    _precision = precision;
                    _scale = scale;
                    _maxLength = s_maxLenFromPrecision[precision - 1];
                    break;
                case SqlDbType.Udt:
                    throw System.Data.Common.ADP.DbTypeNotSupported(SqlDbType.Udt.ToString());
                case SqlDbType.Structured:
                    if (null != fieldTypes)
                    {
                        _fieldMetaData = new System.Collections.ObjectModel.ReadOnlyCollection<SmiExtendedMetaData>(fieldTypes);
                    }
                    _isMultiValued = isMultiValued;
                    _maxLength = _fieldMetaData.Count;
                    break;
                case SqlDbType.Time:
                    Debug.Assert(MinScale <= scale && scale <= MaxTimeScale, "Invalid time scale: " + scale);
                    _scale = scale;
                    _maxLength = 5 - s_maxVarTimeLenOffsetFromScale[scale];
                    break;
                case SqlDbType.DateTime2:
                    Debug.Assert(MinScale <= scale && scale <= MaxTimeScale, "Invalid time scale: " + scale);
                    _scale = scale;
                    _maxLength = 8 - s_maxVarTimeLenOffsetFromScale[scale];
                    break;
                case SqlDbType.DateTimeOffset:
                    Debug.Assert(MinScale <= scale && scale <= MaxTimeScale, "Invalid time scale: " + scale);
                    _scale = scale;
                    _maxLength = 10 - s_maxVarTimeLenOffsetFromScale[scale];
                    break;
                default:
                    Debug.Assert(false, "How in the world did we get here? :" + dbType);
                    break;
            }

            if (null != extendedProperties)
            {
                extendedProperties.SetReadOnly();
                _extendedProperties = extendedProperties;
            }

            // properties and fields must meet the following conditions at this point:
            //  1) not null
            //  2) read only
            //  3) same number of columns in each list (0 count acceptable for properties that are "unused")
            Debug.Assert(null != _extendedProperties && _extendedProperties.IsReadOnly, "SmiMetaData.ctor: _extendedProperties is " + (null != _extendedProperties ? "writable" : "null"));
            Debug.Assert(null != _fieldMetaData && _fieldMetaData.IsReadOnly, "SmiMetaData.ctor: _fieldMetaData is " + (null != _fieldMetaData ? "writable" : "null"));
#if DEBUG
            ((SmiDefaultFieldsProperty)_extendedProperties[SmiPropertySelector.DefaultFields]).CheckCount(_fieldMetaData.Count);
            ((SmiOrderProperty)_extendedProperties[SmiPropertySelector.SortOrder]).CheckCount(_fieldMetaData.Count);
            ((SmiUniqueKeyProperty)_extendedProperties[SmiPropertySelector.UniqueKey]).CheckCount(_fieldMetaData.Count);
#endif
        }

        // Sql-style compare options for character types.
        internal SqlCompareOptions CompareOptions
        {
            get
            {
                return _compareOptions;
            }
        }

        // LCID for type.  0 for non-character types.
        internal long LocaleId
        {
            get
            {
                return _localeId;
            }
        }

        // Units of length depend on type.
        //  NVarChar, NChar, NText: # of Unicode characters
        //  Everything else: # of bytes
        internal long MaxLength
        {
            get
            {
                return _maxLength;
            }
        }

        internal byte Precision
        {
            get
            {
                return _precision;
            }
        }

        internal byte Scale
        {
            get
            {
                return _scale;
            }
        }

        internal SqlDbType SqlDbType
        {
            get
            {
                return _databaseType;
            }
        }

        internal bool IsMultiValued
        {
            get
            {
                return _isMultiValued;
            }
        }

        // Returns read-only list of field metadata
        internal IList<SmiExtendedMetaData> FieldMetaData
        {
            get
            {
                return _fieldMetaData;
            }
        }

        // Returns read-only list of extended properties
        internal SmiMetaDataPropertyCollection ExtendedProperties
        {
            get
            {
                return _extendedProperties;
            }
        }

        internal static bool IsSupportedDbType(SqlDbType dbType)
        {
            // Hole in SqlDbTypes between Xml and Udt for non-WinFS scenarios.
            return (SqlDbType.BigInt <= dbType && SqlDbType.Xml >= dbType) ||
                    (SqlDbType.Udt <= dbType && SqlDbType.DateTimeOffset >= dbType);
        }

        // Only correct access point for defaults per SqlDbType.
        internal static SmiMetaData GetDefaultForType(SqlDbType dbType)
        {
            Debug.Assert(IsSupportedDbType(dbType), "Unsupported SqlDbtype: " + dbType);
            Debug.Assert(dbType != SqlDbType.Udt, "UDT not supported");

            return s_defaultValues[(int)dbType];
        }

        // Private constructor used only to initialize default instance array elements.
        // DO NOT EXPOSE OUTSIDE THIS CLASS!
        private SmiMetaData(
            SqlDbType sqlDbType,
            long maxLength,
            byte precision,
            byte scale,
            SqlCompareOptions compareOptions)
        {
            _databaseType = sqlDbType;
            _maxLength = maxLength;
            _precision = precision;
            _scale = scale;
            _compareOptions = compareOptions;

            // defaults are the same for all types for the following attributes.
            _localeId = 0;
            _isMultiValued = false;
            _fieldMetaData = s_emptyFieldList;
            _extendedProperties = SmiMetaDataPropertyCollection.EmptyInstance;
        }

        // static array of default-valued metadata ordered by corresponding SqlDbType.
        // NOTE: INDEXED BY SqlDbType ENUM!  MUST UPDATE THIS ARRAY WHEN UPDATING SqlDbType!
        //   ONLY ACCESS THIS GLOBAL FROM GetDefaultForType!
        private static SmiMetaData[] s_defaultValues =
            {
                DefaultBigInt,                 // SqlDbType.BigInt
                DefaultBinary,                 // SqlDbType.Binary
                DefaultBit,                    // SqlDbType.Bit
                DefaultChar_NoCollation,       // SqlDbType.Char
                DefaultDateTime,               // SqlDbType.DateTime
                DefaultDecimal,                // SqlDbType.Decimal
                DefaultFloat,                  // SqlDbType.Float
                DefaultImage,                  // SqlDbType.Image
                DefaultInt,                    // SqlDbType.Int
                DefaultMoney,                  // SqlDbType.Money
                DefaultNChar_NoCollation,      // SqlDbType.NChar
                DefaultNText_NoCollation,      // SqlDbType.NText
                DefaultNVarChar_NoCollation,   // SqlDbType.NVarChar
                DefaultReal,                   // SqlDbType.Real
                DefaultUniqueIdentifier,       // SqlDbType.UniqueIdentifier
                DefaultSmallDateTime,          // SqlDbType.SmallDateTime
                DefaultSmallInt,               // SqlDbType.SmallInt
                DefaultSmallMoney,             // SqlDbType.SmallMoney
                DefaultText_NoCollation,       // SqlDbType.Text
                DefaultTimestamp,              // SqlDbType.Timestamp
                DefaultTinyInt,                // SqlDbType.TinyInt
                DefaultVarBinary,              // SqlDbType.VarBinary
                DefaultVarChar_NoCollation,    // SqlDbType.VarChar
                DefaultVariant,                // SqlDbType.Variant
                DefaultNVarChar_NoCollation,   // Placeholder for value 24
                DefaultXml,                    // SqlDbType.Xml
                DefaultNVarChar_NoCollation,   // Placeholder for value 26
                DefaultNVarChar_NoCollation,   // Placeholder for value 27
                DefaultNVarChar_NoCollation,   // Placeholder for value 28
                null,                          // Generic Udt (not supported)
                DefaultStructured,             // Generic structured type
                DefaultDate,                   // SqlDbType.Date
                DefaultTime,                   // SqlDbType.Time
                DefaultDateTime2,              // SqlDbType.DateTime2
                DefaultDateTimeOffset,         // SqlDbType.DateTimeOffset
            };

        // Internal setter to be used by constructors only!  Modifies state!
        private void SetDefaultsForType(SqlDbType dbType)
        {
            SmiMetaData smdDflt = GetDefaultForType(dbType);
            _databaseType = dbType;
            _maxLength = smdDflt.MaxLength;
            _precision = smdDflt.Precision;
            _scale = smdDflt.Scale;
            _localeId = smdDflt.LocaleId;
            _compareOptions = smdDflt.CompareOptions;
            _isMultiValued = smdDflt._isMultiValued;
            _fieldMetaData = smdDflt._fieldMetaData;            // This is ok due to immutability
            _extendedProperties = smdDflt._extendedProperties;  // This is ok due to immutability
        }
    }

    // SmiExtendedMetaData
    //
    //  Adds server-specific type extension information to base metadata, but still portable across a specific server.
    //
    internal class SmiExtendedMetaData : SmiMetaData
    {

        private string _name;           // context-dependent identifier, i.e. parameter name for parameters, column name for columns, etc.

        // three-part name for typed xml schema and for udt names
        private string _typeSpecificNamePart1;
        private string _typeSpecificNamePart2;
        private string _typeSpecificNamePart3;

        internal SmiExtendedMetaData(
            SqlDbType dbType,
            long maxLength,
            byte precision,
            byte scale,
            long localeId,
            SqlCompareOptions compareOptions,
            string name,
            string typeSpecificNamePart1,
            string typeSpecificNamePart2,
            string typeSpecificNamePart3) :
            this(
                dbType,
                maxLength,
                precision,
                scale,
                localeId,
                compareOptions,
                false,
                null,
                null,
                name,
                typeSpecificNamePart1,
                typeSpecificNamePart2,
                typeSpecificNamePart3)
        {
        }

        // SMI V220 ctor.
        internal SmiExtendedMetaData(
            SqlDbType dbType,
            long maxLength,
            byte precision,
            byte scale,
            long localeId,
            SqlCompareOptions compareOptions,
            bool isMultiValued,
            IList<SmiExtendedMetaData> fieldMetaData,
            SmiMetaDataPropertyCollection extendedProperties,
            string name,
            string typeSpecificNamePart1,
            string typeSpecificNamePart2,
            string typeSpecificNamePart3) :
            base(dbType,
                maxLength,
                precision,
                scale,
                localeId,
                compareOptions,
                isMultiValued,
                fieldMetaData,
                extendedProperties)
        {
            Debug.Assert(null == name || MaxNameLength >= name.Length, "Name is too long");

            _name = name;
            _typeSpecificNamePart1 = typeSpecificNamePart1;
            _typeSpecificNamePart2 = typeSpecificNamePart2;
            _typeSpecificNamePart3 = typeSpecificNamePart3;
        }

        internal string Name
        {
            get
            {
                return _name;
            }
        }

        internal string TypeSpecificNamePart1
        {
            get
            {
                return _typeSpecificNamePart1;
            }
        }

        internal string TypeSpecificNamePart2
        {
            get
            {
                return _typeSpecificNamePart2;
            }
        }

        internal string TypeSpecificNamePart3
        {
            get
            {
                return _typeSpecificNamePart3;
            }
        }
    }

    // SmiParameterMetaData
    //
    //  MetaData class to send parameter definitions to server.
    //  Sealed because we don't need to derive from it yet.
    internal sealed class SmiParameterMetaData : SmiExtendedMetaData
    {
        private ParameterDirection _direction;

        // SMI V220 ctor.
        internal SmiParameterMetaData(
            SqlDbType dbType,
            long maxLength,
            byte precision,
            byte scale,
            long localeId,
            SqlCompareOptions compareOptions,
            bool isMultiValued,
            IList<SmiExtendedMetaData> fieldMetaData,
            SmiMetaDataPropertyCollection extendedProperties,
            string name,
            string typeSpecificNamePart1,
            string typeSpecificNamePart2,
            string typeSpecificNamePart3,
            ParameterDirection direction) :
            base(dbType,
                maxLength,
                precision,
                scale,
                localeId,
                compareOptions,
                isMultiValued,
                fieldMetaData,
                extendedProperties,
                name,
                typeSpecificNamePart1,
                typeSpecificNamePart2,
                typeSpecificNamePart3)
        {
            Debug.Assert(ParameterDirection.Input == direction
                       || ParameterDirection.Output == direction
                       || ParameterDirection.InputOutput == direction
                       || ParameterDirection.ReturnValue == direction, "Invalid direction: " + direction);
            _direction = direction;
        }

        internal ParameterDirection Direction
        {
            get
            {
                return _direction;
            }
        }
    }

    // SmiStorageMetaData
    //
    //  This class represents the addition of storage-level attributes to the hierarchy (i.e. attributes from 
    //  underlying table, source variables, or whatever).
    //
    //  Most values use Null (either IsNullable == true or CLR null) to indicate "Not specified" state.  Selection
    //  of which values allow "not specified" determined by backward compatibility.
    //
    //  Maps approximately to TDS' COLMETADATA token with TABNAME and part of COLINFO thrown in.
    internal class SmiStorageMetaData : SmiExtendedMetaData
    {
        // AllowsDBNull is the only value required to be specified.
        private bool _allowsDBNull;  // could the column return nulls? equivalent to TDS's IsNullable bit
        private string _serverName;  // underlying column's server
        private string _catalogName; // underlying column's database
        private string _schemaName;  // underlying column's schema
        private string _tableName;   // underlying column's table
        private string _columnName;  // underlying column's name
        private SqlBoolean _isKey;   // Is this one of a set of key columns that uniquely identify an underlying table?
        private bool _isIdentity;    // Is this from an identity column
        private bool _isColumnSet;   // Is this column the XML representation of a columnset?

        // SMI V220 ctor.
        internal SmiStorageMetaData(
            SqlDbType dbType,
            long maxLength,
            byte precision,
            byte scale,
            long localeId,
            SqlCompareOptions compareOptions,
            bool isMultiValued,
            IList<SmiExtendedMetaData> fieldMetaData,
            SmiMetaDataPropertyCollection extendedProperties,
            string name,
            string typeSpecificNamePart1,
            string typeSpecificNamePart2,
            string typeSpecificNamePart3,
            bool allowsDBNull,
            string serverName,
            string catalogName,
            string schemaName,
            string tableName,
            string columnName,
            SqlBoolean isKey,
            bool isIdentity,
            bool isColumnSet) :
            base(dbType,
                maxLength,
                precision,
                scale,
                localeId,
                compareOptions,
                isMultiValued,
                fieldMetaData,
                extendedProperties,
                name,
                typeSpecificNamePart1,
                typeSpecificNamePart2,
                typeSpecificNamePart3)
        {
            _allowsDBNull = allowsDBNull;
            _serverName = serverName;
            _catalogName = catalogName;
            _schemaName = schemaName;
            _tableName = tableName;
            _columnName = columnName;
            _isKey = isKey;
            _isIdentity = isIdentity;
            _isColumnSet = isColumnSet;
        }

        internal SqlBoolean IsKey
        {
            get
            {
                return _isKey;
            }
        }
    }

    // SmiQueryMetaData
    //
    //  Adds Query-specific attributes.
    //  Sealed since we don't need to extend it for now.
    //  Maps to full COLMETADATA + COLINFO + TABNAME tokens on TDS.
    internal class SmiQueryMetaData : SmiStorageMetaData
    {
        private bool _isReadOnly;
        private SqlBoolean _isExpression;
        private SqlBoolean _isAliased;
        private SqlBoolean _isHidden;

        // SMI V200 ctor.
        internal SmiQueryMetaData(
            SqlDbType dbType,
            long maxLength,
            byte precision,
            byte scale,
            long localeId,
            SqlCompareOptions compareOptions,
            bool isMultiValued,
            IList<SmiExtendedMetaData> fieldMetaData,
            SmiMetaDataPropertyCollection extendedProperties,
            string name,
            string typeSpecificNamePart1,
            string typeSpecificNamePart2,
            string typeSpecificNamePart3,
            bool allowsDBNull,
            string serverName,
            string catalogName,
            string schemaName,
            string tableName,
            string columnName,
            SqlBoolean isKey,
            bool isIdentity,
            bool isReadOnly,
            SqlBoolean isExpression,
            SqlBoolean isAliased,
            SqlBoolean isHidden) :
            this(dbType,
                maxLength,
                precision,
                scale,
                localeId,
                compareOptions,
                isMultiValued,
                fieldMetaData,
                extendedProperties,
                name,
                typeSpecificNamePart1,
                typeSpecificNamePart2,
                typeSpecificNamePart3,
                allowsDBNull,
                serverName,
                catalogName,
                schemaName,
                tableName,
                columnName,
                isKey,
                isIdentity,
                false,
                isReadOnly,
                isExpression,
                isAliased,
                isHidden)
        {
        }

        // SMI V220 ctor.
        internal SmiQueryMetaData(
            SqlDbType dbType,
            long maxLength,
            byte precision,
            byte scale,
            long localeId,
            SqlCompareOptions compareOptions,
            bool isMultiValued,
            IList<SmiExtendedMetaData> fieldMetaData,
            SmiMetaDataPropertyCollection extendedProperties,
            string name,
            string typeSpecificNamePart1,
            string typeSpecificNamePart2,
            string typeSpecificNamePart3,
            bool allowsDBNull,
            string serverName,
            string catalogName,
            string schemaName,
            string tableName,
            string columnName,
            SqlBoolean isKey,
            bool isIdentity,
            bool isColumnSet,
            bool isReadOnly,
            SqlBoolean isExpression,
            SqlBoolean isAliased,
            SqlBoolean isHidden) :
            base(dbType,
                maxLength,
                precision,
                scale,
                localeId,
                compareOptions,
                isMultiValued,
                fieldMetaData,
                extendedProperties,
                name,
                typeSpecificNamePart1,
                typeSpecificNamePart2,
                typeSpecificNamePart3,
                allowsDBNull,
                serverName,
                catalogName,
                schemaName,
                tableName,
                columnName,
                isKey,
                isIdentity,
                isColumnSet)
        {
            _isReadOnly = isReadOnly;
            _isExpression = isExpression;
            _isAliased = isAliased;
            _isHidden = isHidden;
        }
    }
}
