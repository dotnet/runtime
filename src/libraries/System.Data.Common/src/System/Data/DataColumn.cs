// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using System.Data.Common;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Data.SqlTypes;
using System.Xml.Serialization;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading;
using System.Numerics;
using System.Reflection;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    /// <summary>
    /// Represents one column of data in a <see cref='System.Data.DataTable'/>.
    /// </summary>
    [ToolboxItem(false)]
    [DesignTimeVisible(false)]
    [DefaultProperty(nameof(ColumnName))]
    [Editor("Microsoft.VSDesigner.Data.Design.DataColumnEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a",
            "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] // needed by Clone() to preserve derived ctors
    public class DataColumn : MarshalByValueComponent
    {
        private bool _allowNull = true;
        private string? _caption;
        private string _columnName;
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        private Type _dataType = null!; // Always set in UpdateColumnType
        private StorageType _storageType;
        internal object _defaultValue = DBNull.Value; // DefaultValue Converter
        private DataSetDateTime _dateTimeMode = DataSetDateTime.UnspecifiedLocal;
        private DataExpression? _expression;
        private int _maxLength = -1;
        private int _ordinal = -1;
        private bool _readOnly;
        internal Index? _sortIndex;
        internal DataTable? _table;
        private bool _unique;
        internal MappingType _columnMapping = MappingType.Element;
        internal int _hashCode;

        internal int _errors;
        private bool _isSqlType;
        private bool _implementsINullable;
        private bool _implementsIChangeTracking;
        private bool _implementsIRevertibleChangeTracking;
        private bool _implementsIXMLSerializable;

        private bool _defaultValueIsNull = true;

        internal List<DataColumn>? _dependentColumns; // list of columns whose expression consume values from this column
        internal PropertyCollection? _extendedProperties;

        private DataStorage? _storage;

        /// <summary>represents current value to return, usage pattern is .get_Current then MoveAfter</summary>
        private AutoIncrementValue? _autoInc;

        // The _columnClass member is the class for the unfoliated virtual nodes in the XML.
        internal string? _columnUri;
        private string _columnPrefix = string.Empty;
        internal string? _encodedColumnName;

        internal SimpleType? _simpleType;

        private static int s_objectTypeCount;
        private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

        /// <summary>
        /// Initializes a new instance of a <see cref='System.Data.DataColumn'/>
        /// class.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This is safe because type is string and expression is null.")]
        public DataColumn() : this(null, typeof(string), null, MappingType.Element)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataColumn'/> class
        /// using the specified column name.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This is safe because type is string and expression is null.")]
        public DataColumn(string? columnName) : this(columnName, typeof(string), null, MappingType.Element)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataColumn'/> class
        /// using the specified column name and data type.
        /// </summary>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Expression is null and `dataType` is marked appropriately.")]
        public DataColumn(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type dataType) : this(columnName, dataType, null, MappingType.Element)
        {
        }

        /// <summary>
        /// Initializes a new instance
        /// of the <see cref='System.Data.DataColumn'/> class
        /// using the specified name, data type, and expression.
        /// </summary>
        [RequiresUnreferencedCode("Members from serialized types or types used in expressions may be trimmed if not referenced directly.")]
        public DataColumn(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type dataType, string? expr) : this(columnName, dataType, expr, MappingType.Element)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DataColumn'/> class
        /// using
        /// the specified name, data type, expression, and value that determines whether the
        /// column is an attribute.
        /// </summary>
        [RequiresUnreferencedCode("Members from serialized types or types used in expressions may be trimmed if not referenced directly.")]
        public DataColumn(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type dataType, string? expr, MappingType type)
        {
            GC.SuppressFinalize(this);
            DataCommonEventSource.Log.Trace("<ds.DataColumn.DataColumn|API> {0}, columnName='{1}', expr='{2}', type={3}", ObjectID, columnName, expr, type);

            if (dataType == null)
            {
                throw ExceptionBuilder.ArgumentNull(nameof(dataType));
            }

            StorageType typeCode = DataStorage.GetStorageType(dataType);
            if (DataStorage.ImplementsINullableValue(typeCode, dataType))
            {
                throw ExceptionBuilder.ColumnTypeNotSupported();
            }
            _columnName = columnName ?? string.Empty;

            SimpleType? stype = SimpleType.CreateSimpleType(typeCode, dataType);
            if (null != stype)
            {
                SimpleType = stype;
            }
            UpdateColumnType(dataType, typeCode);

            if (!string.IsNullOrEmpty(expr)) // its a performance hit to set Expression to the empty str when we know it will come out null
            {
                Expression = expr;
            }

            _columnMapping = type;
        }

        private void UpdateColumnType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)] Type type, StorageType typeCode)
        {
            TypeLimiter.EnsureTypeIsAllowed(type);
            _dataType = type;
            _storageType = typeCode;
            if (StorageType.DateTime != typeCode)
            {
                // revert _dateTimeMode back to default, when column type is changed
                _dateTimeMode = DataSetDateTime.UnspecifiedLocal;
            }

            DataStorage.ImplementsInterfaces(
                                typeCode, type,
                                out _isSqlType,
                                out _implementsINullable,
                                out _implementsIXMLSerializable,
                                out _implementsIChangeTracking,
                                out _implementsIRevertibleChangeTracking);

            if (!_isSqlType && _implementsINullable)
            {
                SqlUdtStorage.GetStaticNullForUdtType(type);
            }
        }


        /// <summary>
        /// Gets or sets a value indicating whether null
        /// values are
        /// allowed in this column for rows belonging to the table.
        /// </summary>
        [DefaultValue(true)]
        public bool AllowDBNull
        {
            get { return _allowNull; }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_AllowDBNull|API> {0}, {1}", ObjectID, value);
                try
                {
                    if (_allowNull != value)
                    {
                        if (_table != null)
                        {
                            if (!value && _table.EnforceConstraints)
                            {
                                CheckNotAllowNull();
                            }
                        }
                        _allowNull = value;
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the column automatically increments the value of the column for new
        /// rows added to the table.
        /// </summary>
        [DefaultValue(false)]
        [RefreshProperties(RefreshProperties.All)]
        public bool AutoIncrement
        {
            get { return ((null != _autoInc) && (_autoInc.Auto)); }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataColumn.set_AutoIncrement|API> {0}, {1}", ObjectID, value);
                if (AutoIncrement != value)
                {
                    if (value)
                    {
                        if (_expression != null)
                        {
                            throw ExceptionBuilder.AutoIncrementAndExpression();
                        }

                        if (!DefaultValueIsNull)
                        {
                            throw ExceptionBuilder.AutoIncrementAndDefaultValue();
                        }

                        if (!IsAutoIncrementType(DataType))
                        {
                            if (HasData)
                            {
                                throw ExceptionBuilder.AutoIncrementCannotSetIfHasData(DataType.Name);
                            }

                            DataType = typeof(int);
                        }
                    }

                    AutoInc.Auto = value;
                }
            }
        }

        internal object AutoIncrementCurrent
        {
            get { return ((null != _autoInc) ? _autoInc.Current : AutoIncrementSeed); }
            set
            {
                if ((BigInteger)AutoIncrementSeed != BigIntegerStorage.ConvertToBigInteger(value, FormatProvider))
                {
                    AutoInc.SetCurrent(value, FormatProvider);
                }
            }
        }

        internal AutoIncrementValue AutoInc =>
            (_autoInc ??= ((DataType == typeof(BigInteger)) ?
                (AutoIncrementValue)new AutoIncrementBigInteger() :
                new AutoIncrementInt64()));


        /// <summary>
        /// Gets or sets the starting value for a column that has its
        /// <see cref='System.Data.DataColumn.AutoIncrement'/> property set to <see langword='true'/>.
        /// </summary>
        [DefaultValue((long)0)]
        public long AutoIncrementSeed
        {
            get { return ((null != _autoInc) ? _autoInc.Seed : 0L); }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataColumn.set_AutoIncrementSeed|API> {0}, {1}", ObjectID, value);
                if (AutoIncrementSeed != value)
                {
                    AutoInc.Seed = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the increment used by a column with its <see cref='System.Data.DataColumn.AutoIncrement'/>
        /// property set to <see langword='true'/>
        /// </summary>
        [DefaultValue((long)1)]
        public long AutoIncrementStep
        {
            get { return ((null != _autoInc) ? _autoInc.Step : 1L); }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataColumn.set_AutoIncrementStep|API> {0}, {1}", ObjectID, value);
                if (AutoIncrementStep != value)
                {
                    AutoInc.Step = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets
        /// the caption for this column.
        /// </summary>
        [AllowNull]
        public string Caption
        {
            get { return _caption ?? _columnName; }
            set
            {
                value ??= string.Empty;

                if (_caption == null || string.Compare(_caption, value, true, Locale) != 0)
                {
                    _caption = value;
                }
            }
        }

        /// <summary>
        /// Resets the <see cref='System.Data.DataColumn.Caption'/> property to its previous value, or
        /// to <see langword='null'/> .
        /// </summary>
        private void ResetCaption()
        {
            if (_caption != null)
            {
                _caption = null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref='System.Data.DataColumn.Caption'/> has been explicitly set.
        /// </summary>
        private bool ShouldSerializeCaption() => _caption != null;

        /// <summary>
        /// Gets or sets the name of the column within the <see cref='System.Data.DataColumnCollection'/>.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue("")]
        [AllowNull]
        public string ColumnName
        {
            get { return _columnName; }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_ColumnName|API> {0}, '{1}'", ObjectID, value);
                try
                {
                    value ??= string.Empty;

                    if (string.Compare(_columnName, value, true, Locale) != 0)
                    {
                        if (_table != null)
                        {
                            if (value.Length == 0)
                            {
                                throw ExceptionBuilder.ColumnNameRequired();
                            }

                            _table.Columns.RegisterColumnName(value, this);
                            if (_columnName.Length != 0)
                            {
                                _table.Columns.UnregisterName(_columnName);
                            }
                        }

                        RaisePropertyChanging(nameof(ColumnName));
                        _columnName = value;
                        _encodedColumnName = null;
                        _table?.Columns.OnColumnPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
                    }
                    else if (_columnName != value)
                    {
                        RaisePropertyChanging(nameof(ColumnName));
                        _columnName = value;
                        _encodedColumnName = null;
                        _table?.Columns.OnColumnPropertyChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, this));
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        internal string EncodedColumnName
        {
            get
            {
                _encodedColumnName ??= XmlConvert.EncodeLocalName(ColumnName);

                Debug.Assert(!string.IsNullOrEmpty(_encodedColumnName));
                return _encodedColumnName;
            }
        }

        internal IFormatProvider FormatProvider =>
            // used for formating/parsing not comparing
            ((null != _table) ? _table.FormatProvider : CultureInfo.CurrentCulture);

        internal CultureInfo Locale =>
            // used for comparing not formating/parsing
            ((null != _table) ? _table.Locale : CultureInfo.CurrentCulture);

        internal int ObjectID => _objectID;

        [DefaultValue("")]
        [AllowNull]
        public string Prefix
        {
            get { return _columnPrefix; }
            set
            {
                value ??= string.Empty;

                DataCommonEventSource.Log.Trace("<ds.DataColumn.set_Prefix|API> {0}, '{1}'", ObjectID, value);

                if ((XmlConvert.DecodeName(value) == value) && (XmlConvert.EncodeName(value) != value))
                {
                    throw ExceptionBuilder.InvalidPrefix(value);
                }

                _columnPrefix = value;
            }
        }

        // Return the field value as a string. If the field value is NULL, then NULL is return.
        // If the column type is string and it's value is empty, then the empty string is returned.
        // If the column type is not string, or the column type is string and the value is not empty string, then a non-empty string is returned
        // This method does not throw any formatting exceptions, since we can always format the field value to a string.
        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal string? GetColumnValueAsString(DataRow row, DataRowVersion version)
        {
            object objValue = this[row.GetRecordFromVersion(version)];

            if (DataStorage.IsObjectNull(objValue))
            {
                return null;
            }

            string value = ConvertObjectToXml(objValue);
            Debug.Assert(value != null);
            return value;
        }

        /// <summary>
        /// Whether this column computes values.
        /// </summary>
        internal bool Computed => _expression != null;

        /// <summary>
        /// The internal expression object that computes the values.
        /// </summary>
        internal DataExpression? DataExpression => _expression;

        /// <summary>
        /// The type of data stored in the column.
        /// </summary>
        [DefaultValue(typeof(string))]
        [RefreshProperties(RefreshProperties.All)]
        [TypeConverter(typeof(ColumnTypeConverter))]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicFields)]
        [AllowNull]
        public Type DataType
        {
            get { return _dataType; }
            set
            {
                if (_dataType != value)
                {
                    if (HasData)
                    {
                        throw ExceptionBuilder.CantChangeDataType();
                    }
                    if (value == null)
                    {
                        throw ExceptionBuilder.NullDataType();
                    }
                    StorageType typeCode = DataStorage.GetStorageType(value);
                    if (DataStorage.ImplementsINullableValue(typeCode, value))
                    {
                        throw ExceptionBuilder.ColumnTypeNotSupported();
                    }
                    if (_table != null && IsInRelation())
                    {
                        throw ExceptionBuilder.ColumnsTypeMismatch();
                    }
                    if (typeCode == StorageType.BigInteger && _expression != null)
                    {
                        throw ExprException.UnsupportedDataType(value);
                    }

                    // If the DefualtValue is different from the Column DataType, we will coerce the value to the DataType
                    if (!DefaultValueIsNull)
                    {
                        try
                        {
                            if (_defaultValue is BigInteger)
                            {
                                _defaultValue = BigIntegerStorage.ConvertFromBigInteger((BigInteger)_defaultValue, value, FormatProvider);
                            }
                            else if (typeof(BigInteger) == value)
                            {
                                _defaultValue = BigIntegerStorage.ConvertToBigInteger(_defaultValue, FormatProvider);
                            }
                            else if (typeof(string) == value)
                            {
                                // since string types can be null in value! DO NOT REMOVE THIS CHECK
                                _defaultValue = DefaultValue.ToString()!;
                            }
                            else if (typeof(SqlString) == value)
                            {
                                // since string types can be null in value! DO NOT REMOVE THIS CHECK
                                _defaultValue = SqlConvert.ConvertToSqlString(DefaultValue);
                            }
                            else if (typeof(object) != value)
                            {
                                DefaultValue = SqlConvert.ChangeTypeForDefaultValue(DefaultValue, value, FormatProvider);
                            }
                        }
                        catch (InvalidCastException ex)
                        {
                            throw ExceptionBuilder.DefaultValueDataType(ColumnName, DefaultValue.GetType(), value, ex);
                        }
                        catch (FormatException ex)
                        {
                            throw ExceptionBuilder.DefaultValueDataType(ColumnName, DefaultValue.GetType(), value, ex);
                        }
                    }

                    if (ColumnMapping == MappingType.SimpleContent)
                    {
                        if (value == typeof(char))
                        {
                            throw ExceptionBuilder.CannotSetSimpleContentType(ColumnName, value);
                        }
                    }

                    SimpleType = SimpleType.CreateSimpleType(typeCode, value);
                    if (StorageType.String == typeCode)
                    {
                        _maxLength = -1;
                    }

                    UpdateColumnType(value, typeCode);
                    XmlDataType = null;

                    if (AutoIncrement)
                    {
                        if (!IsAutoIncrementType(value))
                        {
                            AutoIncrement = false;
                        }

                        if (null != _autoInc)
                        {
                            // if you already have data you can't change the data type
                            // if you don't have data - you wouldn't have incremented AutoIncrementCurrent.
                            AutoIncrementValue inc = _autoInc;
                            _autoInc = null;
                            AutoInc.Auto = inc.Auto; // recreate with correct datatype
                            AutoInc.Seed = inc.Seed;
                            AutoInc.Step = inc.Step;
                            if (_autoInc!.DataType == inc.DataType)
                            {
                                _autoInc.Current = inc.Current;
                            }
                            else if (inc.DataType == typeof(long))
                            {
                                AutoInc.Current = (BigInteger)(long)inc.Current;
                            }
                            else
                            {
                                AutoInc.Current = checked((long)(BigInteger)inc.Current);
                            }
                        }
                    }
                }
            }
        }

        [DefaultValue(DataSetDateTime.UnspecifiedLocal)]
        [RefreshProperties(RefreshProperties.All)]
        public DataSetDateTime DateTimeMode
        {
            get { return _dateTimeMode; }
            set
            {
                if (_dateTimeMode != value)
                {
                    if (DataType != typeof(DateTime) && value != DataSetDateTime.UnspecifiedLocal)
                    {
                        //Check for column being DateTime. If the column is not DateTime make sure the value that is being is only the default[UnspecifiedLocal].
                        throw ExceptionBuilder.CannotSetDateTimeModeForNonDateTimeColumns();
                    }
                    switch (value)
                    {
                        case DataSetDateTime.Utc:
                        case DataSetDateTime.Local:
                            if (HasData)
                            {
                                throw ExceptionBuilder.CantChangeDateTimeMode(_dateTimeMode, value);
                            }
                            break;
                        case DataSetDateTime.Unspecified:
                        case DataSetDateTime.UnspecifiedLocal:
                            if (_dateTimeMode == DataSetDateTime.Unspecified || _dateTimeMode == DataSetDateTime.UnspecifiedLocal)
                            {
                                break;
                            }
                            if (HasData)
                            {
                                throw ExceptionBuilder.CantChangeDateTimeMode(_dateTimeMode, value);
                            }
                            break;
                        default:
                            throw ExceptionBuilder.InvalidDateTimeMode(value);
                    }
                    _dateTimeMode = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the default value for the column when creating new rows.
        /// </summary>
        [TypeConverter(typeof(DefaultValueTypeConverter))]
        [AllowNull]
        public object DefaultValue
        {
            get
            {
                Debug.Assert(_defaultValue != null, "It should not have been set to null.");
                if (_defaultValue == DBNull.Value && _implementsINullable)
                {
                    // for perf I dont access property
                    if (_storage != null)
                    {
                        _defaultValue = _storage._nullValue;
                    }
                    else if (_isSqlType)
                    {
                        _defaultValue = SqlConvert.ChangeTypeForDefaultValue(_defaultValue, _dataType, FormatProvider);
                    }
                    else if (_implementsINullable)
                    {
                        PropertyInfo propInfo = _dataType.GetProperty("Null", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
                        if (propInfo != null)
                        {
                            _defaultValue = propInfo.GetValue(null, null)!;
                        }
                    }
                }

                return _defaultValue;
            }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataColumn.set_DefaultValue|API> {0}", ObjectID);
                if (_defaultValue == null || !DefaultValue.Equals(value))
                {
                    if (AutoIncrement)
                    {
                        throw ExceptionBuilder.DefaultValueAndAutoIncrement();
                    }

                    object newDefaultValue = value ?? DBNull.Value;
                    if (newDefaultValue != DBNull.Value && DataType != typeof(object))
                    {
                        // If the DefualtValue is different from the Column DataType, we will coerce the value to the DataType
                        try
                        {
                            newDefaultValue = SqlConvert.ChangeTypeForDefaultValue(newDefaultValue, DataType, FormatProvider);
                        }
                        catch (InvalidCastException ex)
                        {
                            throw ExceptionBuilder.DefaultValueColumnDataType(ColumnName, newDefaultValue.GetType(), DataType, ex);
                        }
                    }
                    _defaultValue = newDefaultValue;
                    _defaultValueIsNull = ((newDefaultValue == DBNull.Value) || (ImplementsINullable && DataStorage.IsObjectSqlNull(newDefaultValue))) ? true : false;
                }
            }
        }

        internal bool DefaultValueIsNull => _defaultValueIsNull;

        internal void BindExpression() => DataExpression!.Bind(_table);

        /// <summary>
        /// Gets or sets the expression used to either filter rows, calculate the column's
        /// value, or create an aggregate column.
        /// </summary>
        [RefreshProperties(RefreshProperties.All)]
        [DefaultValue("")]
        [AllowNull]
        public string Expression
        {
            get { return (_expression == null ? "" : _expression.Expression); }
            [RequiresUnreferencedCode("Members from types used in the expressions may be trimmed if not referenced directly.")]
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_Expression|API> {0}, '{1}'", ObjectID, value);

                value ??= string.Empty;

                try
                {
                    DataExpression? newExpression = null;
                    if (value.Length > 0)
                    {
                        DataExpression testExpression = new DataExpression(_table, value, _dataType);
                        if (testExpression.HasValue)
                        {
                            newExpression = testExpression;
                        }
                    }

                    if (_expression == null && newExpression != null)
                    {
                        if (AutoIncrement || Unique)
                        {
                            throw ExceptionBuilder.ExpressionAndUnique();
                        }

                        // We need to make sure the column is not involved in any Constriants
                        if (_table != null)
                        {
                            for (int i = 0; i < _table.Constraints.Count; i++)
                            {
                                if (_table.Constraints[i].ContainsColumn(this))
                                {
                                    throw ExceptionBuilder.ExpressionAndConstraint(this, _table.Constraints[i]);
                                }
                            }
                        }

                        bool oldReadOnly = ReadOnly;
                        try
                        {
                            ReadOnly = true;
                        }
                        catch (ReadOnlyException e)
                        {
                            ExceptionBuilder.TraceExceptionForCapture(e);
                            ReadOnly = oldReadOnly;
                            throw ExceptionBuilder.ExpressionAndReadOnly();
                        }
                    }

                    // re-calculate the evaluation queue
                    if (_table != null)
                    {
                        if (newExpression != null && newExpression.DependsOn(this))
                        {
                            throw ExceptionBuilder.ExpressionCircular();
                        }

                        HandleDependentColumnList(_expression, newExpression);
                        //hold onto oldExpression in case of error applying new Expression.
                        DataExpression? oldExpression = _expression;
                        _expression = newExpression;

                        // because the column is attached to a table we need to re-calc values
                        try
                        {
                            if (newExpression == null)
                            {
                                for (int i = 0; i < _table.RecordCapacity; i++)
                                {
                                    InitializeRecord(i);
                                }
                            }
                            else
                            {
                                _table.EvaluateExpressions(this);
                            }

                            _table.ResetInternalIndexes(this);
                            _table.EvaluateDependentExpressions(this);
                        }
                        catch (Exception e1) when (ADP.IsCatchableExceptionType(e1))
                        {
                            ExceptionBuilder.TraceExceptionForCapture(e1);
                            try
                            {
                                // in the case of error we need to set the column expression to the old value
                                _expression = oldExpression;
                                HandleDependentColumnList(newExpression, _expression);
                                if (oldExpression == null)
                                {
                                    for (int i = 0; i < _table.RecordCapacity; i++)
                                    {
                                        InitializeRecord(i);
                                    }
                                }
                                else
                                {
                                    _table.EvaluateExpressions(this);
                                }
                                _table.ResetInternalIndexes(this);
                                _table.EvaluateDependentExpressions(this);
                            }
                            catch (Exception e2) when (ADP.IsCatchableExceptionType(e2))
                            {
                                ExceptionBuilder.TraceExceptionWithoutRethrow(e2);
                            }
                            throw;
                        }
                    }
                    else
                    {
                        //if column is not attached to a table, just set.
                        _expression = newExpression;
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        /// <summary>
        /// Gets the collection of custom user information.
        /// </summary>
        [Browsable(false)]
        public PropertyCollection ExtendedProperties => _extendedProperties ??= new PropertyCollection();

        /// <summary>
        /// Indicates whether this column is now storing data.
        /// </summary>
        internal bool HasData => _storage != null;

        internal bool ImplementsINullable => _implementsINullable;

        internal bool ImplementsIChangeTracking => _implementsIChangeTracking;

        internal bool ImplementsIRevertibleChangeTracking => _implementsIRevertibleChangeTracking;

        internal bool IsCloneable
        {
            get
            {
                Debug.Assert(null != _storage, "no storage");
                return _storage._isCloneable;
            }
        }

        internal bool IsStringType
        {
            get
            {
                Debug.Assert(null != _storage, "no storage");
                return _storage._isStringType;
            }
        }

        internal bool IsValueType
        {
            get
            {
                Debug.Assert(null != _storage, "no storage");
                return _storage._isValueType;
            }
        }

        internal bool IsSqlType => _isSqlType;

        private void SetMaxLengthSimpleType()
        {
            if (_simpleType != null)
            {
                Debug.Assert(_simpleType.CanHaveMaxLength(), "expected simpleType to be string");

                _simpleType.MaxLength = _maxLength;

                // check if we reset the simpleType back to plain string
                if (_simpleType.IsPlainString())
                {
                    _simpleType = null;
                }
                else
                {
                    // Named Simple Type's Name should not be null
                    if (_simpleType.Name != null && XmlDataType != null)
                    {
                        // if MaxLength is changed, we need to make  namedsimpletype annonymous simpletype
                        _simpleType.ConvertToAnnonymousSimpleType();
                        XmlDataType = null;
                    }
                }
            }
            else if (-1 < _maxLength)
            {
                SimpleType = SimpleType.CreateLimitedStringType(_maxLength);
            }
        }

        [DefaultValue(-1)]
        public int MaxLength
        {
            get { return _maxLength; }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_MaxLength|API> {0}, {1}", ObjectID, value);
                try
                {
                    if (_maxLength != value)
                    {
                        if (ColumnMapping == MappingType.SimpleContent)
                        {
                            throw ExceptionBuilder.CannotSetMaxLength2(this);
                        }
                        if ((DataType != typeof(string)) && (DataType != typeof(SqlString)))
                        {
                            throw ExceptionBuilder.HasToBeStringType(this);
                        }
                        int oldValue = _maxLength;
                        _maxLength = Math.Max(value, -1);

                        if (((oldValue < 0) || (value < oldValue)) && (null != _table) && _table.EnforceConstraints)
                        {
                            if (!CheckMaxLength())
                            {
                                _maxLength = oldValue;
                                throw ExceptionBuilder.CannotSetMaxLength(this, value);
                            }
                        }
                        SetMaxLengthSimpleType();
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }

        [AllowNull]
        public string Namespace
        {
            get
            {
                if (_columnUri == null)
                {
                    if (Table != null && _columnMapping != MappingType.Attribute)
                    {
                        return Table.Namespace;
                    }
                    return string.Empty;
                }
                return _columnUri;
            }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataColumn.set_Namespace|API> {0}, '{1}'", ObjectID, value);

                if (_columnUri != value)
                {
                    if (_columnMapping != MappingType.SimpleContent)
                    {
                        RaisePropertyChanging(nameof(Namespace));
                        _columnUri = value;
                    }
                    else if (value != Namespace)
                    {
                        throw ExceptionBuilder.CannotChangeNamespace(ColumnName);
                    }
                }
            }
        }

        private bool ShouldSerializeNamespace() => _columnUri != null;

        private void ResetNamespace() => Namespace = null;

        /// <summary>
        /// Gets the position of the column in the <see cref='System.Data.DataColumnCollection'/>
        /// collection.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int Ordinal => _ordinal;

        public void SetOrdinal(int ordinal)
        {
            if (_ordinal == -1)
            {
                throw ExceptionBuilder.ColumnNotInAnyTable();
            }

            if (_ordinal != ordinal)
            {
                _table!.Columns.MoveTo(this, ordinal);
            }
        }

        internal void SetOrdinalInternal(int ordinal)
        {
            if (_ordinal != ordinal)
            {
                if (Unique && _ordinal != -1 && ordinal == -1)
                {
                    UniqueConstraint? key = _table!.Constraints.FindKeyConstraint(this);
                    if (key != null)
                    {
                        _table.Constraints.Remove(key);
                    }
                }

                if ((null != _sortIndex) && (-1 == ordinal))
                {
                    Debug.Assert(2 <= _sortIndex.RefCount, "bad sortIndex refcount");
                    _sortIndex.RemoveRef();
                    _sortIndex.RemoveRef(); // second should remove it from index collection
                    _sortIndex = null;
                }

                int originalOrdinal = _ordinal;
                _ordinal = ordinal;
                if (originalOrdinal == -1 && _ordinal != -1)
                {
                    if (Unique)
                    {
                        UniqueConstraint key = new UniqueConstraint(this);
                        _table!.Constraints.Add(key);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value
        /// indicating whether the column allows changes once a row has been added to the table.
        /// </summary>
        [DefaultValue(false)]
        public bool ReadOnly
        {
            get { return _readOnly; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataColumn.set_ReadOnly|API> {0}, {1}", ObjectID, value);
                if (_readOnly != value)
                {
                    if (!value && _expression != null)
                    {
                        throw ExceptionBuilder.ReadOnlyAndExpression();
                    }
                    _readOnly = value;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // don't have debugger view expand this
        private Index SortIndex
        {
            get
            {
                if (_sortIndex == null)
                {
                    var indexDesc = new IndexField[] { new IndexField(this, false) };
                    _sortIndex = _table!.GetIndex(indexDesc, DataViewRowState.CurrentRows, null);
                    _sortIndex.AddRef();
                }
                return _sortIndex;
            }
        }

        /// <summary>
        /// Gets the <see cref='System.Data.DataTable'/> to which the column belongs to.
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public DataTable? Table => _table;

        /// <summary>
        /// Internal mechanism for changing the table pointer.
        /// </summary>
        internal void SetTable(DataTable? table)
        {
            Debug.Assert(table != null || !Unique);
            if (_table != table)
            {
                if (Computed)
                {
                    if ((table == null) || (!table.fInitInProgress && ((table.DataSet == null) || (!table.DataSet._fIsSchemaLoading && !table.DataSet._fInitInProgress))))
                    {
                        // We need to re-bind all expression columns.
                        DataExpression!.Bind(table);
                    }
                }

                if (Unique && _table != null)
                {
                    UniqueConstraint? constraint = table!.Constraints.FindKeyConstraint(this);
                    if (constraint != null)
                    {
                        table.Constraints.CanRemove(constraint, true);
                    }
                }
                _table = table;
                _storage = null; // empty out storage for reuse.
            }
        }

        private DataRow GetDataRow(int index) => _table!._recordManager[index];

        /// <summary>
        /// This is how data is pushed in and out of the column.
        /// </summary>
        internal object this[int record]
        {
            get
            {
                _table!._recordManager.VerifyRecord(record);
                Debug.Assert(null != _storage, "null storage");
                return _storage.Get(record);
            }
            set
            {
                try
                {
                    _table!._recordManager.VerifyRecord(record);
                    Debug.Assert(null != _storage, "no storage");
                    Debug.Assert(null != value, "setting null, expecting dbnull");
                    _storage.Set(record, value);
                    Debug.Assert(null != _table, "storage with no DataTable on column");
                }
                catch (Exception e)
                {
                    ExceptionBuilder.TraceExceptionForCapture(e);
                    throw ExceptionBuilder.SetFailed(value, this, DataType, e);
                }

                if (AutoIncrement)
                {
                    if (!_storage.IsNull(record))
                    {
                        AutoInc.SetCurrentAndIncrement(_storage.Get(record));
                    }
                }
                if (Computed)
                {
                    // if and only if it is Expression column, we will cache LastChangedColumn, otherwise DO NOT
                    DataRow dr = GetDataRow(record);
                    if (dr != null)
                    {
                        // at initialization time (datatable.NewRow(), we would fill the storage with default value, but at that time we won't have datarow)
                        dr.LastChangedColumn = this;
                    }
                }
            }
        }

        internal void InitializeRecord(int record)
        {
            Debug.Assert(null != _storage, "no storage");
            _storage.Set(record, DefaultValue);
        }

        internal void SetValue(int record, object value)
        {
            // just silently set the value
            try
            {
                Debug.Assert(null != value, "setting null, expecting dbnull");
                Debug.Assert(null != _table, "storage with no DataTable on column");
                Debug.Assert(null != _storage, "no storage");
                _storage.Set(record, value);
            }
            catch (Exception e)
            {
                ExceptionBuilder.TraceExceptionForCapture(e);
                throw ExceptionBuilder.SetFailed(value!, this, DataType, e);
            }

            DataRow dr = GetDataRow(record);
            if (dr != null)
            {  // at initialization time (datatable.NewRow(), we would fill the storage with default value, but at that time we won't have datarow)
                dr.LastChangedColumn = this;
            }
        }

        internal void FreeRecord(int record)
        {
            Debug.Assert(null != _storage, "no storage");
            _storage.Set(record, _storage._nullValue);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the values in each row of the column must be unique.
        /// </summary>
        [DefaultValue(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Unique
        {
            get { return _unique; }
            set
            {
                long logScopeId = DataCommonEventSource.Log.EnterScope("<ds.DataColumn.set_Unique|API> {0}, {1}", ObjectID, value);
                try
                {
                    if (_unique != value)
                    {
                        if (value && _expression != null)
                        {
                            throw ExceptionBuilder.UniqueAndExpression();
                        }

                        UniqueConstraint? oldConstraint = null;
                        if (_table != null)
                        {
                            if (value)
                            {
                                CheckUnique();
                            }
                            else
                            {
                                for (IEnumerator e = _table.Constraints.GetEnumerator(); e.MoveNext();)
                                {
                                    UniqueConstraint? o = (e.Current as UniqueConstraint);
                                    if ((null != o) && (o.ColumnsReference.Length == 1) && (o.ColumnsReference[0] == this))
                                        oldConstraint = o;
                                }
                                Debug.Assert(oldConstraint != null, "Should have found a column to remove from the collection.");
                                _table.Constraints.CanRemove(oldConstraint, true);
                            }
                        }

                        _unique = value;

                        if (_table != null)
                        {
                            if (value)
                            {
                                // This should not fail due to a duplicate constraint. unique would have
                                // already been true if there was an existed UniqueConstraint for this column

                                UniqueConstraint constraint = new UniqueConstraint(this);
                                Debug.Assert(_table.Constraints.FindKeyConstraint(this) == null, "Should not be a duplication constraint in collection");
                                _table.Constraints.Add(constraint);
                            }
                            else
                            {
                                _table.Constraints.Remove(oldConstraint!);
                            }
                        }
                    }
                }
                finally
                {
                    DataCommonEventSource.Log.ExitScope(logScopeId);
                }
            }
        }


        internal void InternalUnique(bool value) => _unique = value;

        internal string? XmlDataType { get; set; } = string.Empty; // The type specified in dt:type attribute

        internal SimpleType? SimpleType
        {
            get { return _simpleType; }
            set
            {
                _simpleType = value;

                // there is a change, since we are supporting hierarchy(bacause of Names Simple Type) old check (just one leel base check) is wrong
                if (value != null && value.CanHaveMaxLength())
                {
                    _maxLength = value.MaxLength; // this is temp solution, since we dont let simple content to have
                }

                //maxlength set but for simple type we want to set it, after coming to decision about it , we should
                // use MaxLength property
            }
        }

        /// <summary>
        /// Gets the <see cref='System.Data.MappingType'/> of the column.
        /// </summary>
        [DefaultValue(MappingType.Element)]
        public virtual MappingType ColumnMapping
        {
            get { return _columnMapping; }
            set
            {
                DataCommonEventSource.Log.Trace("<ds.DataColumn.set_ColumnMapping|API> {0}, {1}", ObjectID, value);
                if (value != _columnMapping)
                {
                    if (value == MappingType.SimpleContent && _table != null)
                    {
                        int threshold = 0;
                        if (_columnMapping == MappingType.Element)
                        {
                            threshold = 1;
                        }
                        if (_dataType == typeof(char))
                        {
                            throw ExceptionBuilder.CannotSetSimpleContent(ColumnName, _dataType);
                        }

                        if (_table.XmlText != null && _table.XmlText != this)
                        {
                            throw ExceptionBuilder.CannotAddColumn3();
                        }
                        if (_table.ElementColumnCount > threshold)
                        {
                            throw ExceptionBuilder.CannotAddColumn4(ColumnName);
                        }
                    }

                    RaisePropertyChanging(nameof(ColumnMapping));

                    if (_table != null)
                    {
                        if (_columnMapping == MappingType.SimpleContent)
                        {
                            _table._xmlText = null;
                        }

                        if (value == MappingType.Element)
                        {
                            _table.ElementColumnCount++;
                        }
                        else if (_columnMapping == MappingType.Element)
                        {
                            _table.ElementColumnCount--;
                        }
                    }

                    _columnMapping = value;
                    if (value == MappingType.SimpleContent)
                    {
                        _columnUri = null;
                        if (_table != null)
                        {
                            _table.XmlText = this;
                        }
                        SimpleType = null;
                    }
                }
            }
        }

        internal event PropertyChangedEventHandler? PropertyChanging;

        internal void CheckColumnConstraint(DataRow row, DataRowAction action)
        {
            if (DataTable.UpdatingCurrent(row, action))
            {
                CheckNullable(row);
                CheckMaxLength(row);
            }
        }

        internal bool CheckMaxLength()
        {
            if ((0 <= _maxLength) && (null != Table) && (0 < Table.Rows.Count))
            {
                Debug.Assert(IsStringType, "not a String or SqlString column");
                foreach (DataRow dr in Table.Rows)
                {
                    if (dr.HasVersion(DataRowVersion.Current))
                    {
                        if (_maxLength < GetStringLength(dr.GetCurrentRecordNo()))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        internal void CheckMaxLength(DataRow dr)
        {
            if (0 <= _maxLength)
            {
                Debug.Assert(IsStringType, "not a String or SqlString column");
                if (_maxLength < GetStringLength(dr.GetDefaultRecord()))
                {
                    throw ExceptionBuilder.LongerThanMaxLength(this);
                }
            }
        }

        protected internal void CheckNotAllowNull()
        {
            if (_storage == null)
            {
                return;
            }

            if (_sortIndex != null)
            {
                if (_sortIndex.IsKeyInIndex(_storage._nullValue))
                {
                    // here we do use strong typed NULL for Sql types
                    throw ExceptionBuilder.NullKeyValues(ColumnName);
                }
            }
            else
            {
                // since we do not have index, we so sequential search
                foreach (DataRow dr in _table!.Rows)
                {
                    if (dr.RowState == DataRowState.Deleted)
                    {
                        continue;
                    }

                    if (!_implementsINullable)
                    {
                        if (dr[this] == DBNull.Value)
                        {
                            throw ExceptionBuilder.NullKeyValues(ColumnName);
                        }
                    }
                    else
                    {
                        if (DataStorage.IsObjectNull(dr[this]))
                        {
                            throw ExceptionBuilder.NullKeyValues(ColumnName);
                        }
                    }
                }
            }
        }

        internal void CheckNullable(DataRow row)
        {
            if (!AllowDBNull)
            {
                Debug.Assert(null != _storage, "no storage");
                if (_storage.IsNull(row.GetDefaultRecord()))
                {
                    throw ExceptionBuilder.NullValues(ColumnName);
                }
            }
        }

        protected void CheckUnique()
        {
            if (!SortIndex.CheckUnique())
            {
                // Throws an exception and the name of any column if its Unique property set to
                // True and non-unique values are found in the column.
                throw ExceptionBuilder.NonUniqueValues(ColumnName);
            }
        }

        internal int Compare(int record1, int record2)
        {
            Debug.Assert(null != _storage, "null storage");
            return _storage.Compare(record1, record2);
        }

        internal bool CompareValueTo(int record1, object value, bool checkType)
        {
            // this method is used to make sure value and exact type match.
            int valuesMatch = CompareValueTo(record1, value);

            // if values match according to storage, do extra checks for exact compare
            if (valuesMatch == 0)
            {
                Type leftType = value.GetType();
                Type rightType = _storage!.Get(record1).GetType();
                // if strings, then do exact character by character check
                if (leftType == typeof(string) && rightType == typeof(string))
                {
                    return string.CompareOrdinal((string)_storage.Get(record1), (string)value) == 0 ? true : false;
                }
                // make sure same type
                else if (leftType == rightType)
                {
                    return true;
                }
            }

            return false;
        }

        internal int CompareValueTo(int record1, object? value)
        {
            Debug.Assert(null != _storage, "null storage");
            return _storage.CompareValueTo(record1, value);
        }

        internal object? ConvertValue(object? value)
        {
            Debug.Assert(null != _storage, "null storage");
            return _storage.ConvertValue(value);
        }

        internal void Copy(int srcRecordNo, int dstRecordNo)
        {
            Debug.Assert(null != _storage, "null storage");
            _storage.Copy(srcRecordNo, dstRecordNo);
        }

        // Prevent inlining so that reflection calls are not moved to caller that may be in a different assembly that may have a different grant set.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal DataColumn Clone()
        {
            DataColumn clone = (DataColumn)Activator.CreateInstance(GetType())!;

            clone.SimpleType = SimpleType;

            clone._allowNull = _allowNull;
            if (null != _autoInc)
            {
                clone._autoInc = _autoInc.Clone();
            }
            clone._caption = _caption;
            clone.ColumnName = ColumnName;
            clone._columnUri = _columnUri;
            clone._columnPrefix = _columnPrefix;
            clone.DataType = DataType;
            clone._defaultValue = _defaultValue;
            clone._defaultValueIsNull = ((_defaultValue == DBNull.Value) || (clone.ImplementsINullable && DataStorage.IsObjectSqlNull(_defaultValue))) ? true : false;
            clone._columnMapping = _columnMapping; // clone column Mapping since we dont let MaxLength to be set throu API
            clone._readOnly = _readOnly;
            clone.MaxLength = MaxLength;
            clone.XmlDataType = XmlDataType;
            clone._dateTimeMode = _dateTimeMode;

            // so if we have set it, we should continue preserving the information

            // ...Extended Properties
            if (_extendedProperties != null)
            {
                foreach (object key in _extendedProperties.Keys)
                {
                    clone.ExtendedProperties[key] = _extendedProperties[key];
                }
            }

            return clone;
        }

        /// <summary>
        /// Finds a relation that this column is the sole child of or null.
        /// </summary>
        internal DataRelation? FindParentRelation()
        {
            var parentRelations = new DataRelation[Table!.ParentRelations.Count];
            Table.ParentRelations.CopyTo(parentRelations, 0);

            for (int i = 0; i < parentRelations.Length; i++)
            {
                DataRelation relation = parentRelations[i];
                DataKey key = relation.ChildKey;
                if (key.ColumnsReference.Length == 1 && key.ColumnsReference[0] == this)
                {
                    return relation;
                }
            }

            return null;
        }


        internal object GetAggregateValue(int[] records, AggregateType kind)
        {
            if (_storage == null)
            {
                return kind == AggregateType.Count ? (object)0 : DBNull.Value;
            }
            return _storage.Aggregate(records, kind);
        }

        private int GetStringLength(int record)
        {
            Debug.Assert(null != _storage, "no storage");
            return _storage.GetStringLength(record);
        }

        internal void Init(int record)
        {
            if (AutoIncrement)
            {
                object value = _autoInc!.Current;
                _autoInc.MoveAfter();
                Debug.Assert(null != _storage, "no storage");
                _storage.Set(record, value);
            }
            else
            {
                this[record] = _defaultValue;
            }
        }

        internal static bool IsAutoIncrementType(Type dataType) =>
            dataType == typeof(int) ||
            dataType == typeof(long) ||
            dataType == typeof(short) ||
            dataType == typeof(decimal) ||
            dataType == typeof(BigInteger) ||
            dataType == typeof(SqlInt32) ||
            dataType == typeof(SqlInt64) ||
            dataType == typeof(SqlInt16) ||
            dataType == typeof(SqlDecimal);

        private static bool IsColumnMappingValid(StorageType typeCode, MappingType mapping) =>
            !((mapping != MappingType.Element) && DataStorage.IsTypeCustomType(typeCode));

        internal bool IsCustomType => _storage != null ?
            _storage._isCustomDefinedType :
            DataStorage.IsTypeCustomType(DataType);

        internal static bool IsValueCustomTypeInstance(object value) =>
            // if instance is not a storage supported type (built in or SQL types)
            (DataStorage.IsTypeCustomType(value.GetType()) && !(value is Type));

        internal bool ImplementsIXMLSerializable => _implementsIXMLSerializable;

        internal bool IsNull(int record)
        {
            Debug.Assert(null != _storage, "no storage");
            return _storage.IsNull(record);
        }

        /// <summary>
        /// Returns true if this column is a part of a Parent or Child key for a relation.
        /// </summary>
        internal bool IsInRelation()
        {
            DataKey key;
            DataRelationCollection rels = _table!.ParentRelations;

            Debug.Assert(rels != null, "Invalid ParentRelations");
            for (int i = 0; i < rels.Count; i++)
            {
                key = rels[i].ChildKey;
                Debug.Assert(key.HasValue, "Invalid child key (null)");
                if (key.ContainsColumn(this))
                {
                    return true;
                }
            }

            rels = _table.ChildRelations;
            Debug.Assert(rels != null, "Invalid ChildRelations");
            for (int i = 0; i < rels.Count; i++)
            {
                key = rels[i].ParentKey;
                Debug.Assert(key.HasValue, "Invalid parent key (null)");
                if (key.ContainsColumn(this))
                {
                    return true;
                }
            }

            return false;
        }

        internal bool IsMaxLengthViolated()
        {
            if (MaxLength < 0)
                return true;

            bool error = false;
            object value;
            string? errorText = null;

            foreach (DataRow dr in Table!.Rows)
            {
                if (dr.HasVersion(DataRowVersion.Current))
                {
                    value = dr[this];
                    if (!_isSqlType)
                    {
                        if (value != null && value != DBNull.Value && ((string)value).Length > MaxLength)
                        {
                            errorText ??= ExceptionBuilder.MaxLengthViolationText(ColumnName);
                            dr.RowError = errorText;
                            dr.SetColumnError(this, errorText);
                            error = true;
                        }
                    }
                    else
                    {
                        if (!DataStorage.IsObjectNull(value) && ((SqlString)value).Value.Length > MaxLength)
                        {
                            errorText ??= ExceptionBuilder.MaxLengthViolationText(ColumnName);
                            dr.RowError = errorText;
                            dr.SetColumnError(this, errorText);
                            error = true;
                        }
                    }
                }
            }
            return error;
        }

        internal bool IsNotAllowDBNullViolated()
        {
            Index index = SortIndex;
            DataRow[] rows = index.GetRows(index.FindRecords(DBNull.Value));
            for (int i = 0; i < rows.Length; i++)
            {
                string errorText = ExceptionBuilder.NotAllowDBNullViolationText(ColumnName);
                rows[i].RowError = errorText;
                rows[i].SetColumnError(this, errorText);
            }
            return (rows.Length > 0);
        }

        internal void FinishInitInProgress()
        {
            if (Computed)
                BindExpression();
        }

        protected virtual void OnPropertyChanging(PropertyChangedEventArgs pcevent)
        {
            PropertyChanging?.Invoke(this, pcevent);
        }

        protected internal void RaisePropertyChanging(string name)
        {
            OnPropertyChanging(new PropertyChangedEventArgs(name));
        }

        private DataStorage InsureStorage() =>
            _storage ??= DataStorage.CreateStorage(this, _dataType, _storageType);

        internal void SetCapacity(int capacity)
        {
            InsureStorage().SetCapacity(capacity);
        }

        private bool ShouldSerializeDefaultValue() => !DefaultValueIsNull;

        // Returns the <see cref='System.Data.DataColumn.Expression'/> of the column, if one exists.
        public override string ToString() => _expression == null ?
            ColumnName :
            ColumnName + " + " + Expression;

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal object ConvertXmlToObject(string s)
        {
            Debug.Assert(s != null, "Caller is responsible for missing element/attribute case");
            return InsureStorage().ConvertXmlToObject(s);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal object ConvertXmlToObject(XmlReader xmlReader, XmlRootAttribute? xmlAttrib)
        {
            return InsureStorage().ConvertXmlToObject(xmlReader, xmlAttrib);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal string ConvertObjectToXml(object value)
        {
            Debug.Assert(value != null && (value != DBNull.Value), "Caller is responsible for checking on DBNull");
            return InsureStorage().ConvertObjectToXml(value);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void ConvertObjectToXml(object value, XmlWriter xmlWriter, XmlRootAttribute? xmlAttrib)
        {
            Debug.Assert(value != null && (value != DBNull.Value), "Caller is responsible for checking on DBNull");
            InsureStorage().ConvertObjectToXml(value, xmlWriter, xmlAttrib);
        }

        internal object GetEmptyColumnStore(int recordCount)
        {
            return InsureStorage().GetEmptyStorageInternal(recordCount);
        }

        internal void CopyValueIntoStore(int record, object store, BitArray nullbits, int storeIndex)
        {
            Debug.Assert(null != _storage, "no storage");
            _storage.CopyValueInternal(record, store, nullbits, storeIndex);
        }

        [RequiresUnreferencedCode(DataSet.RequiresUnreferencedCodeMessage)]
        internal void SetStorage(object store, BitArray nullbits)
        {
            InsureStorage().SetStorageInternal(store, nullbits);
        }

        internal void AddDependentColumn(DataColumn expressionColumn)
        {
            _dependentColumns ??= new List<DataColumn>();

            Debug.Assert(!_dependentColumns.Contains(expressionColumn), "duplicate column - expected to be unique");
            _dependentColumns.Add(expressionColumn);
            _table!.AddDependentColumn(expressionColumn);
        }

        internal void RemoveDependentColumn(DataColumn expressionColumn)
        {
            if (_dependentColumns != null && _dependentColumns.Contains(expressionColumn))
            {
                _dependentColumns.Remove(expressionColumn);
            }
            _table!.RemoveDependentColumn(expressionColumn);
        }

        internal void HandleDependentColumnList(DataExpression? oldExpression, DataExpression? newExpression)
        {
            Debug.Assert(_table != null);
            DataColumn[] dependency;

            // remove this column from the dependentColumn list of the columns this column depends on.
            if (oldExpression != null)
            {
                dependency = oldExpression.GetDependency();
                foreach (DataColumn col in dependency)
                {
                    Debug.Assert(null != col, "null datacolumn in expression dependencies");
                    col.RemoveDependentColumn(this);
                    if (col._table != _table)
                    {
                        _table.RemoveDependentColumn(this);
                    }
                }
                _table.RemoveDependentColumn(this);
            }

            if (newExpression != null)
            {
                // get the list of columns that this expression depends on
                dependency = newExpression.GetDependency();
                // add this column to dependent column list of each column this column depends on
                foreach (DataColumn col in dependency)
                {
                    col.AddDependentColumn(this);
                    if (col._table != _table)
                    {
                        _table.AddDependentColumn(this);
                    }
                }
                _table.AddDependentColumn(this);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "User has already got warning when creating original column.")]
        internal void CopyExpressionFrom(DataColumn source)
        {
            Expression = source.Expression;
        }

    }

    internal abstract class AutoIncrementValue
    {
        internal bool Auto { get; set; }
        internal abstract object Current { get; set; }
        internal abstract long Seed { get; set; }
        internal abstract long Step { get; set; }
        internal abstract Type DataType { get; }

        internal abstract void SetCurrent(object value, IFormatProvider formatProvider);
        internal abstract void SetCurrentAndIncrement(object value);
        internal abstract void MoveAfter();

        internal AutoIncrementValue Clone()
        {
            AutoIncrementValue clone = (this is AutoIncrementInt64) ? new AutoIncrementInt64() : (AutoIncrementValue)new AutoIncrementBigInteger();
            clone.Auto = Auto;
            clone.Seed = Seed;
            clone.Step = Step;
            clone.Current = Current;
            return clone;
        }
    }

    /// <summary>the auto stepped value with Int64 representation</summary>
    /// <remarks>use unchecked behavior</remarks>
    internal sealed class AutoIncrementInt64 : AutoIncrementValue
    {
        /// <summary>the last returned auto incremented value</summary>
        private long _current;

        /// <summary>the initial value use to set current</summary>
        private long _seed;

        /// <summary>the value by which to offset the next value</summary>
        private long _step = 1;

        /// <summary>Gets and sets the current auto incremented value to use</summary>
        internal override object Current
        {
            get { return _current; }
            set { _current = (long)value; }
        }

        internal override Type DataType => typeof(long);

        /// <summary>Get and sets the initial seed value.</summary>
        internal override long Seed
        {
            get { return _seed; }
            set
            {
                if ((_current == _seed) || BoundaryCheck(value))
                {
                    _current = value;
                }
                _seed = value;
            }
        }

        /// <summary>Get and sets the stepping value.</summary>
        /// <exception cref="ArgumentException">if value is 0</exception>
        internal override long Step
        {
            get { return _step; }
            set
            {
                if (0 == value)
                {
                    throw ExceptionBuilder.AutoIncrementSeed();
                }
                if (_step != value)
                {
                    if (_current != Seed)
                    {
                        _current = unchecked(_current - _step + value);
                    }
                    _step = value;
                }
            }
        }

        internal override void MoveAfter()
        {
            _current = unchecked(_current + _step);
        }

        internal override void SetCurrent(object value, IFormatProvider formatProvider)
        {
            _current = Convert.ToInt64(value, formatProvider);
        }

        internal override void SetCurrentAndIncrement(object value)
        {
            Debug.Assert(null != value && DataColumn.IsAutoIncrementType(value.GetType()) && !(value is BigInteger), "unexpected value for autoincrement");
            long v = (long)SqlConvert.ChangeType2(value, StorageType.Int64, typeof(long), CultureInfo.InvariantCulture);
            if (BoundaryCheck(v))
            {
                _current = unchecked(v + _step);
            }
        }

        private bool BoundaryCheck(BigInteger value) =>
            ((_step < 0) && (value <= _current)) || ((0 < _step) && (_current <= value));
    }

    /// <summary>the auto stepped value with BigInteger representation</summary>
    internal sealed class AutoIncrementBigInteger : AutoIncrementValue
    {
        /// <summary>the current auto incremented value to use</summary>
        private BigInteger _current;

        /// <summary>the initial value use to set current</summary>
        private long _seed;

        /// <summary>the value by which to offset the next value</summary>
        private BigInteger _step = 1;

        /// <summary>Gets and sets the current auto incremented value to use</summary>
        internal override object Current
        {
            get { return _current; }
            set { _current = (BigInteger)value; }
        }

        internal override Type DataType => typeof(BigInteger);

        /// <summary>Get and sets the initial seed value.</summary>
        internal override long Seed
        {
            get { return _seed; }
            set
            {
                if ((_current == _seed) || BoundaryCheck(value))
                {
                    _current = value;
                }
                _seed = value;
            }
        }

        /// <summary>Get and sets the stepping value.</summary>
        /// <exception cref="ArgumentException">if value is 0</exception>
        internal override long Step
        {
            get { return (long)_step; }
            set
            {
                if (0 == value)
                {
                    throw ExceptionBuilder.AutoIncrementSeed();
                }
                if (_step != value)
                {
                    if (_current != Seed)
                    {
                        _current = checked(_current - _step + value);
                    }
                    _step = value;
                }
            }
        }

        internal override void MoveAfter()
        {
            _current = checked(_current + _step);
        }

        internal override void SetCurrent(object value, IFormatProvider formatProvider)
        {
            _current = BigIntegerStorage.ConvertToBigInteger(value, formatProvider);
        }

        internal override void SetCurrentAndIncrement(object value)
        {
            BigInteger v = (BigInteger)value;
            if (BoundaryCheck(v))
            {
                _current = v + _step;
            }
        }

        private bool BoundaryCheck(BigInteger value) =>
           ((_step < 0) && (value <= _current)) || ((0 < _step) && (_current <= value));
    }
}
