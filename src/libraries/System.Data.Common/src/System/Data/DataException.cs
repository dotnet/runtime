// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;

namespace System.Data
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class DataException : SystemException
    {
        protected DataException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DataException() : base(SR.DataSet_DefaultDataException)
        {
            HResult = HResults.Data;
        }

        public DataException(string? s) : base(s)
        {
            HResult = HResults.Data;
        }

        public DataException(string? s, Exception? innerException) : base(s, innerException) { }
    };

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ConstraintException : DataException
    {
        protected ConstraintException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ConstraintException() : base(SR.DataSet_DefaultConstraintException)
        {
            HResult = HResults.DataConstraint;
        }

        public ConstraintException(string? s) : base(s)
        {
            HResult = HResults.DataConstraint;
        }

        public ConstraintException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataConstraint;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class DeletedRowInaccessibleException : DataException
    {
        protected DeletedRowInaccessibleException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DeletedRowInaccessibleException'/> class.
        /// </summary>
        public DeletedRowInaccessibleException() : base(SR.DataSet_DefaultDeletedRowInaccessibleException)
        {
            HResult = HResults.DataDeletedRowInaccessible;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.Data.DeletedRowInaccessibleException'/> class with the specified string.
        /// </summary>
        public DeletedRowInaccessibleException(string? s) : base(s)
        {
            HResult = HResults.DataDeletedRowInaccessible;
        }

        public DeletedRowInaccessibleException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataDeletedRowInaccessible;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class DuplicateNameException : DataException
    {
        protected DuplicateNameException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DuplicateNameException() : base(SR.DataSet_DefaultDuplicateNameException)
        {
            HResult = HResults.DataDuplicateName;
        }

        public DuplicateNameException(string? s) : base(s)
        {
            HResult = HResults.DataDuplicateName;
        }

        public DuplicateNameException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataDuplicateName;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InRowChangingEventException : DataException
    {
        protected InRowChangingEventException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InRowChangingEventException() : base(SR.DataSet_DefaultInRowChangingEventException)
        {
            HResult = HResults.DataInRowChangingEvent;
        }

        public InRowChangingEventException(string? s) : base(s)
        {
            HResult = HResults.DataInRowChangingEvent;
        }

        public InRowChangingEventException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataInRowChangingEvent;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class InvalidConstraintException : DataException
    {
        protected InvalidConstraintException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public InvalidConstraintException() : base(SR.DataSet_DefaultInvalidConstraintException)
        {
            HResult = HResults.DataInvalidConstraint;
        }

        public InvalidConstraintException(string? s) : base(s)
        {
            HResult = HResults.DataInvalidConstraint;
        }

        public InvalidConstraintException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataInvalidConstraint;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class MissingPrimaryKeyException : DataException
    {
        protected MissingPrimaryKeyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public MissingPrimaryKeyException() : base(SR.DataSet_DefaultMissingPrimaryKeyException)
        {
            HResult = HResults.DataMissingPrimaryKey;
        }

        public MissingPrimaryKeyException(string? s) : base(s)
        {
            HResult = HResults.DataMissingPrimaryKey;
        }

        public MissingPrimaryKeyException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataMissingPrimaryKey;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class NoNullAllowedException : DataException
    {
        protected NoNullAllowedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public NoNullAllowedException() : base(SR.DataSet_DefaultNoNullAllowedException)
        {
            HResult = HResults.DataNoNullAllowed;
        }

        public NoNullAllowedException(string? s) : base(s)
        {
            HResult = HResults.DataNoNullAllowed;
        }

        public NoNullAllowedException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataNoNullAllowed;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class ReadOnlyException : DataException
    {
        protected ReadOnlyException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ReadOnlyException() : base(SR.DataSet_DefaultReadOnlyException)
        {
            HResult = HResults.DataReadOnly;
        }

        public ReadOnlyException(string? s) : base(s)
        {
            HResult = HResults.DataReadOnly;
        }

        public ReadOnlyException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataReadOnly;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class RowNotInTableException : DataException
    {
        protected RowNotInTableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public RowNotInTableException() : base(SR.DataSet_DefaultRowNotInTableException)
        {
            HResult = HResults.DataRowNotInTable;
        }

        public RowNotInTableException(string? s) : base(s)
        {
            HResult = HResults.DataRowNotInTable;
        }

        public RowNotInTableException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataRowNotInTable;
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class VersionNotFoundException : DataException
    {
        protected VersionNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public VersionNotFoundException() : base(SR.DataSet_DefaultVersionNotFoundException)
        {
            HResult = HResults.DataVersionNotFound;
        }

        public VersionNotFoundException(string? s) : base(s)
        {
            HResult = HResults.DataVersionNotFound;
        }

        public VersionNotFoundException(string? message, Exception? innerException) : base(message, innerException)
        {
            HResult = HResults.DataVersionNotFound;
        }
    }

    internal static class ExceptionBuilder
    {
        // The class defines the exceptions that are specific to the DataSet.
        // The class contains functions that take the proper informational variables and then construct
        // the appropriate exception with an error string obtained from the resource Data.txt.
        // The exception is then returned to the caller, so that the caller may then throw from its
        // location so that the catcher of the exception will have the appropriate call stack.
        // This class is used so that there will be compile time checking of error messages.
        // The resource Data.txt will ensure proper string text based on the appropriate locale.

        // this method accepts BID format as an argument, this attribute allows FXCopBid rule to validate calls to it
        private static void TraceException(string trace, Exception e)
        {
            Debug.Assert(null != e, "TraceException: null Exception");
            if (e != null)
            {
                DataCommonEventSource.Log.Trace(trace, e);
            }
        }

        internal static Exception TraceExceptionAsReturnValue(Exception e)
        {
            TraceException("<comm.ADP.TraceException|ERR|THROW> '{0}'", e);
            return e;
        }

        internal static Exception TraceExceptionForCapture(Exception e)
        {
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '{0}'", e);
            return e;
        }

        internal static Exception TraceExceptionWithoutRethrow(Exception e)
        {
            TraceException("<comm.ADP.TraceException|ERR|CATCH> '{0}'", e);
            return e;
        }

        internal static Exception _Argument(string error) => TraceExceptionAsReturnValue(new ArgumentException(error));
        internal static Exception _Argument(string paramName, string error) => TraceExceptionAsReturnValue(new ArgumentException(error));
        internal static Exception _Argument(string error, Exception? innerException) => TraceExceptionAsReturnValue(new ArgumentException(error, innerException));
        private static Exception _ArgumentNull(string paramName, string msg) => TraceExceptionAsReturnValue(new ArgumentNullException(paramName, msg));
        internal static Exception _ArgumentOutOfRange(string paramName, string msg) => TraceExceptionAsReturnValue(new ArgumentOutOfRangeException(paramName, msg));
        private static Exception _IndexOutOfRange(string error) => TraceExceptionAsReturnValue(new IndexOutOfRangeException(error));
        private static Exception _InvalidOperation(string error) => TraceExceptionAsReturnValue(new InvalidOperationException(error));
        private static Exception _InvalidEnumArgumentException(string error) => TraceExceptionAsReturnValue(new InvalidEnumArgumentException(error));
        private static Exception _InvalidEnumArgumentException<T>(T value) where T : Enum
            => _InvalidEnumArgumentException(SR.Format(SR.ADP_InvalidEnumerationValue, typeof(T).Name, value.ToString()));

        //
        // System.Data exceptions
        //

        private static void ThrowDataException(string error, Exception? innerException)
        {
            throw TraceExceptionAsReturnValue(new DataException(error, innerException));
        }

        private static Exception _Data(string error) => TraceExceptionAsReturnValue(new DataException(error));
        private static Exception _Constraint(string error) => TraceExceptionAsReturnValue(new ConstraintException(error));
        private static Exception _InvalidConstraint(string error) => TraceExceptionAsReturnValue(new InvalidConstraintException(error));
        private static Exception _DeletedRowInaccessible(string error) => TraceExceptionAsReturnValue(new DeletedRowInaccessibleException(error));
        private static Exception _DuplicateName(string error) => TraceExceptionAsReturnValue(new DuplicateNameException(error));
        private static Exception _InRowChangingEvent(string error) => TraceExceptionAsReturnValue(new InRowChangingEventException(error));
        private static Exception _MissingPrimaryKey(string error) => TraceExceptionAsReturnValue(new MissingPrimaryKeyException(error));
        private static Exception _NoNullAllowed(string error) => TraceExceptionAsReturnValue(new NoNullAllowedException(error));

        private static Exception _ReadOnly(string error) => TraceExceptionAsReturnValue(new ReadOnlyException(error));
        private static Exception _RowNotInTable(string error) => TraceExceptionAsReturnValue(new RowNotInTableException(error));
        private static Exception _VersionNotFound(string error) => TraceExceptionAsReturnValue(new VersionNotFoundException(error));

        public static Exception ArgumentNull(string paramName) => _ArgumentNull(paramName, SR.Format(SR.Data_ArgumentNull, paramName));
        public static Exception ArgumentOutOfRange(string paramName) => _ArgumentOutOfRange(paramName, SR.Format(SR.Data_ArgumentOutOfRange, paramName));
        public static Exception BadObjectPropertyAccess(string error) => _InvalidOperation(SR.Format(SR.DataConstraint_BadObjectPropertyAccess, error));
        public static Exception ArgumentContainsNull(string paramName) => _Argument(paramName, SR.Format(SR.Data_ArgumentContainsNull, paramName));
        public static Exception TypeNotAllowed(Type type) => _InvalidOperation(SR.Format(SR.Data_TypeNotAllowed, type.AssemblyQualifiedName));


        //
        // Collections
        //

        public static Exception CannotModifyCollection() => _Argument(SR.Data_CannotModifyCollection);
        public static Exception CaseInsensitiveNameConflict(string? name) => _Argument(SR.Format(SR.Data_CaseInsensitiveNameConflict, name));
        public static Exception NamespaceNameConflict(string? name) => _Argument(SR.Format(SR.Data_NamespaceNameConflict, name));
        public static Exception InvalidOffsetLength() => _Argument(SR.Data_InvalidOffsetLength);

        //
        // DataColumnCollection
        //

        public static Exception ColumnNotInTheTable(string column, string table) => _Argument(SR.Format(SR.DataColumn_NotInTheTable, column, table));
        public static Exception ColumnNotInAnyTable() => _Argument(SR.DataColumn_NotInAnyTable);
        public static Exception ColumnOutOfRange(int index) => _IndexOutOfRange(SR.Format(SR.DataColumns_OutOfRange, (index).ToString(CultureInfo.InvariantCulture)));
        public static Exception ColumnOutOfRange(string column) => _IndexOutOfRange(SR.Format(SR.DataColumns_OutOfRange, column));
        public static Exception CannotAddColumn1(string column) => _Argument(SR.Format(SR.DataColumns_Add1, column));
        public static Exception CannotAddColumn2(string column) => _Argument(SR.Format(SR.DataColumns_Add2, column));
        public static Exception CannotAddColumn3() => _Argument(SR.DataColumns_Add3);
        public static Exception CannotAddColumn4(string column) => _Argument(SR.Format(SR.DataColumns_Add4, column));
        public static Exception CannotAddDuplicate(string column) => _DuplicateName(SR.Format(SR.DataColumns_AddDuplicate, column));
        public static Exception CannotAddDuplicate2(string table) => _DuplicateName(SR.Format(SR.DataColumns_AddDuplicate2, table));
        public static Exception CannotAddDuplicate3(string table) => _DuplicateName(SR.Format(SR.DataColumns_AddDuplicate3, table));
        public static Exception CannotRemoveColumn() => _Argument(SR.DataColumns_Remove);
        public static Exception CannotRemovePrimaryKey() => _Argument(SR.DataColumns_RemovePrimaryKey);
        public static Exception CannotRemoveChildKey(string relation) => _Argument(SR.Format(SR.DataColumns_RemoveChildKey, relation));
        public static Exception CannotRemoveConstraint(string constraint, string table) => _Argument(SR.Format(SR.DataColumns_RemoveConstraint, constraint, table));
        public static Exception CannotRemoveExpression(string column, string expression) => _Argument(SR.Format(SR.DataColumns_RemoveExpression, column, expression));
        public static Exception ColumnNotInTheUnderlyingTable(string column, string table) => _Argument(SR.Format(SR.DataColumn_NotInTheUnderlyingTable, column, table));
        public static Exception InvalidOrdinal(string name, int ordinal) => _ArgumentOutOfRange(name, SR.Format(SR.DataColumn_OrdinalExceedMaximun, (ordinal).ToString(CultureInfo.InvariantCulture)));

        //
        // _Constraint and ConstrainsCollection
        //

        public static Exception AddPrimaryKeyConstraint() => _Argument(SR.DataConstraint_AddPrimaryKeyConstraint);
        public static Exception NoConstraintName() => _Argument(SR.DataConstraint_NoName);
        public static Exception ConstraintViolation(string constraint) => _Constraint(SR.Format(SR.DataConstraint_Violation, constraint));
        public static Exception ConstraintNotInTheTable(string constraint) => _Argument(SR.Format(SR.DataConstraint_NotInTheTable, constraint));

        public static string KeysToString(object[] keys)
        {
            string values = string.Empty;
            for (int i = 0; i < keys.Length; i++)
            {
                values += Convert.ToString(keys[i], null) + (i < keys.Length - 1 ? ", " : string.Empty);
            }
            return values;
        }

        public static string UniqueConstraintViolationText(DataColumn[] columns, object[] values)
        {
            if (columns.Length > 1)
            {
                string columnNames = string.Empty;
                for (int i = 0; i < columns.Length; i++)
                {
                    columnNames += columns[i].ColumnName + (i < columns.Length - 1 ? ", " : "");
                }
                return SR.Format(SR.DataConstraint_ViolationValue, columnNames, KeysToString(values));
            }
            else
            {
                return SR.Format(SR.DataConstraint_ViolationValue, columns[0].ColumnName, Convert.ToString(values[0], null));
            }
        }

        public static Exception ConstraintViolation(DataColumn[] columns, object[] values) => _Constraint(UniqueConstraintViolationText(columns, values));
        public static Exception ConstraintOutOfRange(int index) => _IndexOutOfRange(SR.Format(SR.DataConstraint_OutOfRange, (index).ToString(CultureInfo.InvariantCulture)));
        public static Exception DuplicateConstraint(string constraint) => _Data(SR.Format(SR.DataConstraint_Duplicate, constraint));
        public static Exception DuplicateConstraintName(string constraint) => _DuplicateName(SR.Format(SR.DataConstraint_DuplicateName, constraint));
        public static Exception NeededForForeignKeyConstraint(UniqueConstraint key, ForeignKeyConstraint fk) => _Argument(SR.Format(SR.DataConstraint_NeededForForeignKeyConstraint, key.ConstraintName, fk.ConstraintName));
        public static Exception UniqueConstraintViolation() => _Argument(SR.DataConstraint_UniqueViolation);
        public static Exception ConstraintForeignTable() => _Argument(SR.DataConstraint_ForeignTable);
        public static Exception ConstraintParentValues() => _Argument(SR.DataConstraint_ParentValues);
        public static Exception ConstraintAddFailed(DataTable table) => _InvalidConstraint(SR.Format(SR.DataConstraint_AddFailed, table.TableName));
        public static Exception ConstraintRemoveFailed() => _Argument(SR.DataConstraint_RemoveFailed);
        public static Exception FailedCascadeDelete(string constraint) => _InvalidConstraint(SR.Format(SR.DataConstraint_CascadeDelete, constraint));
        public static Exception FailedCascadeUpdate(string constraint) => _InvalidConstraint(SR.Format(SR.DataConstraint_CascadeUpdate, constraint));
        public static Exception FailedClearParentTable(string table, string constraint, string childTable) => _InvalidConstraint(SR.Format(SR.DataConstraint_ClearParentTable, table, constraint, childTable));
        public static Exception ForeignKeyViolation(string constraint, object[] keys) => _InvalidConstraint(SR.Format(SR.DataConstraint_ForeignKeyViolation, constraint, KeysToString(keys)));
        public static Exception RemoveParentRow(ForeignKeyConstraint constraint) => _InvalidConstraint(SR.Format(SR.DataConstraint_RemoveParentRow, constraint.ConstraintName));
        public static string MaxLengthViolationText(string columnName) => SR.Format(SR.DataColumn_ExceedMaxLength, columnName);
        public static string NotAllowDBNullViolationText(string columnName) => SR.Format(SR.DataColumn_NotAllowDBNull, columnName);
        public static Exception CantAddConstraintToMultipleNestedTable(string tableName) => _Argument(SR.Format(SR.DataConstraint_CantAddConstraintToMultipleNestedTable, tableName));

        //
        // DataColumn Set Properties conflicts
        //

        public static Exception AutoIncrementAndExpression() => _Argument(SR.DataColumn_AutoIncrementAndExpression);
        public static Exception AutoIncrementAndDefaultValue() => _Argument(SR.DataColumn_AutoIncrementAndDefaultValue);
        public static Exception AutoIncrementSeed() => _Argument(SR.DataColumn_AutoIncrementSeed);
        public static Exception CantChangeDataType() => _Argument(SR.DataColumn_ChangeDataType);
        public static Exception NullDataType() => _Argument(SR.DataColumn_NullDataType);
        public static Exception ColumnNameRequired() => _Argument(SR.DataColumn_NameRequired);
        public static Exception DefaultValueAndAutoIncrement() => _Argument(SR.DataColumn_DefaultValueAndAutoIncrement);
        public static Exception DefaultValueDataType(string column, Type defaultType, Type columnType, Exception inner) =>
            column.Length == 0 ?
                _Argument(SR.Format(SR.DataColumn_DefaultValueDataType1, defaultType.FullName, columnType.FullName), inner) :
                _Argument(SR.Format(SR.DataColumn_DefaultValueDataType, column, defaultType.FullName, columnType.FullName), inner);

        public static Exception DefaultValueColumnDataType(string column, Type defaultType, Type columnType, Exception inner) => _Argument(SR.Format(SR.DataColumn_DefaultValueColumnDataType, column, defaultType.FullName, columnType.FullName), inner);
        public static Exception ExpressionAndUnique() => _Argument(SR.DataColumn_ExpressionAndUnique);
        public static Exception ExpressionAndReadOnly() => _Argument(SR.DataColumn_ExpressionAndReadOnly);
        public static Exception ExpressionAndConstraint(DataColumn column, Constraint constraint) => _Argument(SR.Format(SR.DataColumn_ExpressionAndConstraint, column.ColumnName, constraint.ConstraintName));
        public static Exception ExpressionInConstraint(DataColumn column) => _Argument(SR.Format(SR.DataColumn_ExpressionInConstraint, column.ColumnName));
        public static Exception ExpressionCircular() => _Argument(SR.DataColumn_ExpressionCircular);
        public static Exception NonUniqueValues(string column) => _InvalidConstraint(SR.Format(SR.DataColumn_NonUniqueValues, column));
        public static Exception NullKeyValues(string column) => _Data(SR.Format(SR.DataColumn_NullKeyValues, column));
        public static Exception NullValues(string column) => _NoNullAllowed(SR.Format(SR.DataColumn_NullValues, column));
        public static Exception ReadOnlyAndExpression() => _ReadOnly(SR.DataColumn_ReadOnlyAndExpression);
        public static Exception ReadOnly(string column) => _ReadOnly(SR.Format(SR.DataColumn_ReadOnly, column));
        public static Exception UniqueAndExpression() => _Argument(SR.DataColumn_UniqueAndExpression);
        public static Exception SetFailed(object? value, DataColumn column, Type type, Exception innerException) => _Argument(innerException.Message + SR.Format(SR.DataColumn_SetFailed, value?.ToString(), column.ColumnName, type.Name), innerException);
        public static Exception CannotSetToNull(DataColumn column) => _Argument(SR.Format(SR.DataColumn_CannotSetToNull, column.ColumnName));
        public static Exception LongerThanMaxLength(DataColumn column) => _Argument(SR.Format(SR.DataColumn_LongerThanMaxLength, column.ColumnName));
        public static Exception CannotSetMaxLength(DataColumn column, int value) => _Argument(SR.Format(SR.DataColumn_CannotSetMaxLength, column.ColumnName, value.ToString(CultureInfo.InvariantCulture)));
        public static Exception CannotSetMaxLength2(DataColumn column) => _Argument(SR.Format(SR.DataColumn_CannotSetMaxLength2, column.ColumnName));
        public static Exception CannotSetSimpleContentType(string columnName, Type type) => _Argument(SR.Format(SR.DataColumn_CannotSimpleContentType, columnName, type));
        public static Exception CannotSetSimpleContent(string columnName, Type type) => _Argument(SR.Format(SR.DataColumn_CannotSimpleContent, columnName, type));
        public static Exception CannotChangeNamespace(string columnName) => _Argument(SR.Format(SR.DataColumn_CannotChangeNamespace, columnName));
        public static Exception HasToBeStringType(DataColumn column) => _Argument(SR.Format(SR.DataColumn_HasToBeStringType, column.ColumnName));
        public static Exception AutoIncrementCannotSetIfHasData(string typeName) => _Argument(SR.Format(SR.DataColumn_AutoIncrementCannotSetIfHasData, typeName));
        public static Exception INullableUDTwithoutStaticNull(string typeName) => _Argument(SR.Format(SR.DataColumn_INullableUDTwithoutStaticNull, typeName));
        public static Exception IComparableNotImplemented(string typeName) => _Data(SR.Format(SR.DataStorage_IComparableNotDefined, typeName));
        public static Exception UDTImplementsIChangeTrackingButnotIRevertible(string typeName) => _InvalidOperation(SR.Format(SR.DataColumn_UDTImplementsIChangeTrackingButnotIRevertible, typeName));
        public static Exception SetAddedAndModifiedCalledOnnonUnchanged() => _InvalidOperation(SR.DataColumn_SetAddedAndModifiedCalledOnNonUnchanged);
        public static Exception InvalidDataColumnMapping(Type type) => _Argument(SR.Format(SR.DataColumn_InvalidDataColumnMapping, type.AssemblyQualifiedName));
        public static Exception CannotSetDateTimeModeForNonDateTimeColumns() => _InvalidOperation(SR.DataColumn_CannotSetDateTimeModeForNonDateTimeColumns);
        public static Exception InvalidDateTimeMode(DataSetDateTime mode) => _InvalidEnumArgumentException(mode);
        public static Exception CantChangeDateTimeMode(DataSetDateTime oldValue, DataSetDateTime newValue) => _InvalidOperation(SR.Format(SR.DataColumn_DateTimeMode, oldValue.ToString(), newValue.ToString()));
        public static Exception ColumnTypeNotSupported() => Common.ADP.NotSupported(SR.DataColumn_NullableTypesNotSupported);

        //
        // DataView
        //

        public static Exception SetFailed(string name) => _Data(SR.Format(SR.DataView_SetFailed, name));
        public static Exception SetDataSetFailed() => _Data(SR.DataView_SetDataSetFailed);
        public static Exception SetRowStateFilter() => _Data(SR.DataView_SetRowStateFilter);
        public static Exception CanNotSetDataSet() => _Data(SR.DataView_CanNotSetDataSet);
        public static Exception CanNotUseDataViewManager() => _Data(SR.DataView_CanNotUseDataViewManager);
        public static Exception CanNotSetTable() => _Data(SR.DataView_CanNotSetTable);
        public static Exception CanNotUse() => _Data(SR.DataView_CanNotUse);
        public static Exception CanNotBindTable() => _Data(SR.DataView_CanNotBindTable);
        public static Exception SetTable() => _Data(SR.DataView_SetTable);
        public static Exception SetIListObject() => _Argument(SR.DataView_SetIListObject);
        public static Exception AddNewNotAllowNull() => _Data(SR.DataView_AddNewNotAllowNull);
        public static Exception NotOpen() => _Data(SR.DataView_NotOpen);
        public static Exception CreateChildView() => _Argument(SR.DataView_CreateChildView);
        public static Exception CanNotDelete() => _Data(SR.DataView_CanNotDelete);
        public static Exception CanNotEdit() => _Data(SR.DataView_CanNotEdit);
        public static Exception GetElementIndex(int index) => _IndexOutOfRange(SR.Format(SR.DataView_GetElementIndex, (index).ToString(CultureInfo.InvariantCulture)));
        public static Exception AddExternalObject() => _Argument(SR.DataView_AddExternalObject);
        public static Exception CanNotClear() => _Argument(SR.DataView_CanNotClear);
        public static Exception InsertExternalObject() => _Argument(SR.DataView_InsertExternalObject);
        public static Exception RemoveExternalObject() => _Argument(SR.DataView_RemoveExternalObject);
        public static Exception PropertyNotFound(string property, string table) => _Argument(SR.Format(SR.DataROWView_PropertyNotFound, property, table));
        public static Exception ColumnToSortIsOutOfRange(string column) => _Argument(SR.Format(SR.DataColumns_OutOfRange, column));

        //
        // Keys
        //

        public static Exception KeyTableMismatch() => _InvalidConstraint(SR.DataKey_TableMismatch);
        public static Exception KeyNoColumns() => _InvalidConstraint(SR.DataKey_NoColumns);
        public static Exception KeyTooManyColumns(int cols) => _InvalidConstraint(SR.Format(SR.DataKey_TooManyColumns, (cols).ToString(CultureInfo.InvariantCulture)));
        public static Exception KeyDuplicateColumns(string columnName) => _InvalidConstraint(SR.Format(SR.DataKey_DuplicateColumns, columnName));

        //
        // Relations, constraints
        //

        public static Exception RelationDataSetMismatch() => _InvalidConstraint(SR.DataRelation_DataSetMismatch);
        public static Exception NoRelationName() => _Argument(SR.DataRelation_NoName);
        public static Exception ColumnsTypeMismatch() => _InvalidConstraint(SR.DataRelation_ColumnsTypeMismatch);
        public static Exception KeyLengthMismatch() => _Argument(SR.DataRelation_KeyLengthMismatch);
        public static Exception KeyLengthZero() => _Argument(SR.DataRelation_KeyZeroLength);
        public static Exception ForeignRelation() => _Argument(SR.DataRelation_ForeignDataSet);
        public static Exception KeyColumnsIdentical() => _InvalidConstraint(SR.DataRelation_KeyColumnsIdentical);
        public static Exception RelationForeignTable(string t1, string t2) => _InvalidConstraint(SR.Format(SR.DataRelation_ForeignTable, t1, t2));
        public static Exception GetParentRowTableMismatch(string t1, string t2) => _InvalidConstraint(SR.Format(SR.DataRelation_GetParentRowTableMismatch, t1, t2));
        public static Exception SetParentRowTableMismatch(string t1, string t2) => _InvalidConstraint(SR.Format(SR.DataRelation_SetParentRowTableMismatch, t1, t2));
        public static Exception RelationForeignRow() => _Argument(SR.DataRelation_ForeignRow);
        public static Exception RelationNestedReadOnly() => _Argument(SR.DataRelation_RelationNestedReadOnly);
        public static Exception TableCantBeNestedInTwoTables(string tableName) => _Argument(SR.Format(SR.DataRelation_TableCantBeNestedInTwoTables, tableName));
        public static Exception LoopInNestedRelations(string tableName) => _Argument(SR.Format(SR.DataRelation_LoopInNestedRelations, tableName));
        public static Exception RelationDoesNotExist() => _Argument(SR.DataRelation_DoesNotExist);
        public static Exception ParentRowNotInTheDataSet() => _Argument(SR.DataRow_ParentRowNotInTheDataSet);
        public static Exception ParentOrChildColumnsDoNotHaveDataSet() => _InvalidConstraint(SR.DataRelation_ParentOrChildColumnsDoNotHaveDataSet);
        public static Exception InValidNestedRelation(string childTableName) => _InvalidOperation(SR.Format(SR.DataRelation_InValidNestedRelation, childTableName));
        public static Exception InvalidParentNamespaceinNestedRelation(string childTableName) => _InvalidOperation(SR.Format(SR.DataRelation_InValidNamespaceInNestedRelation, childTableName));

        //
        // Rows
        //

        public static Exception RowNotInTheDataSet() => _Argument(SR.DataRow_NotInTheDataSet);
        public static Exception RowNotInTheTable() => _RowNotInTable(SR.DataRow_NotInTheTable);
        public static Exception EditInRowChanging() => _InRowChangingEvent(SR.DataRow_EditInRowChanging);
        public static Exception EndEditInRowChanging() => _InRowChangingEvent(SR.DataRow_EndEditInRowChanging);
        public static Exception BeginEditInRowChanging() => _InRowChangingEvent(SR.DataRow_BeginEditInRowChanging);
        public static Exception CancelEditInRowChanging() => _InRowChangingEvent(SR.DataRow_CancelEditInRowChanging);
        public static Exception DeleteInRowDeleting() => _InRowChangingEvent(SR.DataRow_DeleteInRowDeleting);
        public static Exception ValueArrayLength() => _Argument(SR.DataRow_ValuesArrayLength);
        public static Exception NoCurrentData() => _VersionNotFound(SR.DataRow_NoCurrentData);
        public static Exception NoOriginalData() => _VersionNotFound(SR.DataRow_NoOriginalData);
        public static Exception NoProposedData() => _VersionNotFound(SR.DataRow_NoProposedData);
        public static Exception RowRemovedFromTheTable() => _RowNotInTable(SR.DataRow_RemovedFromTheTable);
        public static Exception DeletedRowInaccessible() => _DeletedRowInaccessible(SR.DataRow_DeletedRowInaccessible);
        public static Exception RowAlreadyDeleted() => _DeletedRowInaccessible(SR.DataRow_AlreadyDeleted);
        public static Exception RowEmpty() => _Argument(SR.DataRow_Empty);
        public static Exception InvalidRowVersion() => _Data(SR.DataRow_InvalidVersion);
        public static Exception RowOutOfRange() => _IndexOutOfRange(SR.DataRow_RowOutOfRange);
        public static Exception RowOutOfRange(int index) => _IndexOutOfRange(SR.Format(SR.DataRow_OutOfRange, (index).ToString(CultureInfo.InvariantCulture)));
        public static Exception RowInsertOutOfRange(int index) => _IndexOutOfRange(SR.Format(SR.DataRow_RowInsertOutOfRange, (index).ToString(CultureInfo.InvariantCulture)));
        public static Exception RowInsertTwice(int index, string tableName) => _IndexOutOfRange(SR.Format(SR.DataRow_RowInsertTwice, (index).ToString(CultureInfo.InvariantCulture), tableName));
        public static Exception RowInsertMissing(string tableName) => _IndexOutOfRange(SR.Format(SR.DataRow_RowInsertMissing, tableName));
        public static Exception RowAlreadyRemoved() => _Data(SR.DataRow_AlreadyRemoved);
        public static Exception MultipleParents() => _Data(SR.DataRow_MultipleParents);
        public static Exception InvalidRowState(DataRowState state) => _InvalidEnumArgumentException<DataRowState>(state);
        public static Exception InvalidRowBitPattern() => _Argument(SR.DataRow_InvalidRowBitPattern);

        //
        // DataSet
        //

        internal static Exception SetDataSetNameToEmpty() => _Argument(SR.DataSet_SetNameToEmpty);
        internal static Exception SetDataSetNameConflicting(string name) => _Argument(SR.Format(SR.DataSet_SetDataSetNameConflicting, name));
        public static Exception DataSetUnsupportedSchema(string ns) => _Argument(SR.Format(SR.DataSet_UnsupportedSchema, ns));
        public static Exception MergeMissingDefinition(string obj) => _Argument(SR.Format(SR.DataMerge_MissingDefinition, obj));
        public static Exception TablesInDifferentSets() => _Argument(SR.DataRelation_TablesInDifferentSets);
        public static Exception RelationAlreadyExists() => _Argument(SR.DataRelation_AlreadyExists);
        public static Exception RowAlreadyInOtherCollection() => _Argument(SR.DataRow_AlreadyInOtherCollection);
        public static Exception RowAlreadyInTheCollection() => _Argument(SR.DataRow_AlreadyInTheCollection);
        public static Exception TableMissingPrimaryKey() => _MissingPrimaryKey(SR.DataTable_MissingPrimaryKey);
        public static Exception RecordStateRange() => _Argument(SR.DataIndex_RecordStateRange);
        public static Exception IndexKeyLength(int length, int keyLength) => length == 0 ?
            _Argument(SR.DataIndex_FindWithoutSortOrder) :
            _Argument(SR.Format(SR.DataIndex_KeyLength, (length).ToString(CultureInfo.InvariantCulture), (keyLength).ToString(CultureInfo.InvariantCulture)));
        public static Exception RemovePrimaryKey(DataTable table) => table.TableName.Length == 0 ?
            _Argument(SR.DataKey_RemovePrimaryKey) :
            _Argument(SR.Format(SR.DataKey_RemovePrimaryKey1, table.TableName));
        public static Exception RelationAlreadyInOtherDataSet() => _Argument(SR.DataRelation_AlreadyInOtherDataSet);
        public static Exception RelationAlreadyInTheDataSet() => _Argument(SR.DataRelation_AlreadyInTheDataSet);
        public static Exception RelationNotInTheDataSet(string relation) => _Argument(SR.Format(SR.DataRelation_NotInTheDataSet, relation));
        public static Exception RelationOutOfRange(object index) => _IndexOutOfRange(SR.Format(SR.DataRelation_OutOfRange, Convert.ToString(index, null)));
        public static Exception DuplicateRelation(string relation) => _DuplicateName(SR.Format(SR.DataRelation_DuplicateName, relation));
        public static Exception RelationTableNull() => _Argument(SR.DataRelation_TableNull);
        public static Exception RelationDataSetNull() => _Argument(SR.DataRelation_TableNull);
        public static Exception RelationTableWasRemoved() => _Argument(SR.DataRelation_TableWasRemoved);
        public static Exception ParentTableMismatch() => _Argument(SR.DataRelation_ParentTableMismatch);
        public static Exception ChildTableMismatch() => _Argument(SR.DataRelation_ChildTableMismatch);
        public static Exception EnforceConstraint() => _Constraint(SR.Data_EnforceConstraints);
        public static Exception CaseLocaleMismatch() => _Argument(SR.DataRelation_CaseLocaleMismatch);
        public static Exception CannotChangeCaseLocale() => CannotChangeCaseLocale(null);
        public static Exception CannotChangeCaseLocale(Exception? innerException) => _Argument(SR.DataSet_CannotChangeCaseLocale, innerException);
        public static Exception CannotChangeSchemaSerializationMode() => _InvalidOperation(SR.DataSet_CannotChangeSchemaSerializationMode);
        public static Exception InvalidSchemaSerializationMode(Type enumType, string mode) => _InvalidEnumArgumentException(SR.Format(SR.ADP_InvalidEnumerationValue, enumType.Name, mode));
        public static Exception InvalidRemotingFormat(SerializationFormat mode) => _InvalidEnumArgumentException<SerializationFormat>(mode);

        //
        // DataTable and DataTableCollection
        //
        public static Exception TableForeignPrimaryKey() => _Argument(SR.DataTable_ForeignPrimaryKey);
        public static Exception TableCannotAddToSimpleContent() => _Argument(SR.DataTable_CannotAddToSimpleContent);
        public static Exception NoTableName() => _Argument(SR.DataTable_NoName);
        public static Exception MultipleTextOnlyColumns() => _Argument(SR.DataTable_MultipleSimpleContentColumns);
        public static Exception InvalidSortString(string sort) => _Argument(SR.Format(SR.DataTable_InvalidSortString, sort));
        public static Exception DuplicateTableName(string table) => _DuplicateName(SR.Format(SR.DataTable_DuplicateName, table));
        public static Exception DuplicateTableName2(string table, string ns) => _DuplicateName(SR.Format(SR.DataTable_DuplicateName2, table, ns));
        public static Exception SelfnestedDatasetConflictingName(string table) => _DuplicateName(SR.Format(SR.DataTable_SelfnestedDatasetConflictingName, table));
        public static Exception DatasetConflictingName(string table) => _DuplicateName(SR.Format(SR.DataTable_DatasetConflictingName, table));
        public static Exception TableAlreadyInOtherDataSet() => _Argument(SR.DataTable_AlreadyInOtherDataSet);
        public static Exception TableAlreadyInTheDataSet() => _Argument(SR.DataTable_AlreadyInTheDataSet);
        public static Exception TableOutOfRange(int index) => _IndexOutOfRange(SR.Format(SR.DataTable_OutOfRange, (index).ToString(CultureInfo.InvariantCulture)));
        public static Exception TableNotInTheDataSet(string table) => _Argument(SR.Format(SR.DataTable_NotInTheDataSet, table));
        public static Exception TableInRelation() => _Argument(SR.DataTable_InRelation);
        public static Exception TableInConstraint(DataTable table, Constraint constraint) => _Argument(SR.Format(SR.DataTable_InConstraint, table.TableName, constraint.ConstraintName));
        public static Exception CanNotSerializeDataTableHierarchy() => _InvalidOperation(SR.DataTable_CanNotSerializeDataTableHierarchy);
        public static Exception CanNotRemoteDataTable() => _InvalidOperation(SR.DataTable_CanNotRemoteDataTable);
        public static Exception CanNotSetRemotingFormat() => _Argument(SR.DataTable_CanNotSetRemotingFormat);
        public static Exception CanNotSerializeDataTableWithEmptyName() => _InvalidOperation(SR.DataTable_CanNotSerializeDataTableWithEmptyName);
        public static Exception TableNotFound(string tableName) => _Argument(SR.Format(SR.DataTable_TableNotFound, tableName));


        //
        // Storage
        //
        public static Exception AggregateException(AggregateType aggregateType, Type type) => _Data(SR.Format(SR.DataStorage_AggregateException, aggregateType, type.Name));
        public static Exception InvalidStorageType(TypeCode typecode) => _Data(SR.Format(SR.DataStorage_InvalidStorageType, typecode.ToString()));
        public static Exception RangeArgument(int min, int max) => _Argument(SR.Format(SR.Range_Argument, (min).ToString(CultureInfo.InvariantCulture), (max).ToString(CultureInfo.InvariantCulture)));
        public static Exception NullRange() => _Data(SR.Range_NullRange);
        public static Exception NegativeMinimumCapacity() => _Argument(SR.RecordManager_MinimumCapacity);
        public static Exception ProblematicChars(char charValue) => _Argument(SR.Format(SR.DataStorage_ProblematicChars, "0x" + ((ushort)charValue).ToString("X", CultureInfo.InvariantCulture)));
        public static Exception StorageSetFailed() => _Argument(SR.DataStorage_SetInvalidDataType);


        //
        // XML schema
        //
        public static Exception SimpleTypeNotSupported() => _Data(SR.Xml_SimpleTypeNotSupported);
        public static Exception MissingAttribute(string attribute) => MissingAttribute(string.Empty, attribute);
        public static Exception MissingAttribute(string element, string attribute) => _Data(SR.Format(SR.Xml_MissingAttribute, element, attribute));
        public static Exception InvalidAttributeValue(string name, string value) => _Data(SR.Format(SR.Xml_ValueOutOfRange, name, value));
        public static Exception AttributeValues(string name, string value1, string value2) => _Data(SR.Format(SR.Xml_AttributeValues, name, value1, value2));
        public static Exception ElementTypeNotFound(string name) => _Data(SR.Format(SR.Xml_ElementTypeNotFound, name));
        public static Exception RelationParentNameMissing(string rel) => _Data(SR.Format(SR.Xml_RelationParentNameMissing, rel));
        public static Exception RelationChildNameMissing(string rel) => _Data(SR.Format(SR.Xml_RelationChildNameMissing, rel));
        public static Exception RelationTableKeyMissing(string rel) => _Data(SR.Format(SR.Xml_RelationTableKeyMissing, rel));
        public static Exception RelationChildKeyMissing(string rel) => _Data(SR.Format(SR.Xml_RelationChildKeyMissing, rel));
        public static Exception UndefinedDatatype(string name) => _Data(SR.Format(SR.Xml_UndefinedDatatype, name));
        public static Exception DatatypeNotDefined() => _Data(SR.Xml_DatatypeNotDefined);
        public static Exception MismatchKeyLength() => _Data(SR.Xml_MismatchKeyLength);
        public static Exception InvalidField(string name) => _Data(SR.Format(SR.Xml_InvalidField, name));
        public static Exception InvalidSelector(string name) => _Data(SR.Format(SR.Xml_InvalidSelector, name));
        public static Exception CircularComplexType(string name) => _Data(SR.Format(SR.Xml_CircularComplexType, name));
        public static Exception CannotInstantiateAbstract(string name) => _Data(SR.Format(SR.Xml_CannotInstantiateAbstract, name));
        public static Exception InvalidKey(string name) => _Data(SR.Format(SR.Xml_InvalidKey, name));
        public static Exception DiffgramMissingSQL() => _Data(SR.Xml_MissingSQL);
        public static Exception DuplicateConstraintRead(string str) => _Data(SR.Format(SR.Xml_DuplicateConstraint, str));
        public static Exception ColumnTypeConflict(string name) => _Data(SR.Format(SR.Xml_ColumnConflict, name));
        public static Exception CannotConvert(string name, string? type) => _Data(SR.Format(SR.Xml_CannotConvert, name, type));
        public static Exception MissingRefer(string name) => _Data(SR.Format(SR.Xml_MissingRefer, Keywords.REFER, Keywords.XSD_KEYREF, name));
        public static Exception InvalidPrefix(string name) => _Data(SR.Format(SR.Xml_InvalidPrefix_SpecialCharacters, name));
        public static Exception CanNotDeserializeObjectType() => _InvalidOperation(SR.Xml_CanNotDeserializeObjectType);
        public static Exception IsDataSetAttributeMissingInSchema() => _Data(SR.Xml_IsDataSetAttributeMissingInSchema);
        public static Exception TooManyIsDataSetAttributesInSchema() => _Data(SR.Xml_TooManyIsDataSetAttributesInSchema);

        // XML save
        public static Exception NestedCircular(string name) => _Data(SR.Format(SR.Xml_NestedCircular, name));
        public static Exception MultipleParentRows(string tableQName) => _Data(SR.Format(SR.Xml_MultipleParentRows, tableQName));
        public static Exception PolymorphismNotSupported(string typeName) => _InvalidOperation(SR.Format(SR.Xml_PolymorphismNotSupported, typeName));
        public static Exception DataTableInferenceNotSupported() => _InvalidOperation(SR.Xml_DataTableInferenceNotSupported);

        /// <summary>throw DataException for multitarget failure</summary>
        internal static void ThrowMultipleTargetConverter(Exception? innerException)
        {
            string res = (null != innerException) ? SR.Xml_MultipleTargetConverterError : SR.Xml_MultipleTargetConverterEmpty;
            ThrowDataException(res, innerException);
        }

        // Merge
        public static Exception DuplicateDeclaration(string name) => _Data(SR.Format(SR.Xml_MergeDuplicateDeclaration, name));

        //Read Xml data
        public static Exception FoundEntity() => _Data(SR.Xml_FoundEntity);
        public static Exception MergeFailed(string name) => _Data(name);

        // SqlConvert
        public static Exception ConvertFailed(Type type1, Type type2) => _Data(SR.Format(SR.SqlConvert_ConvertFailed, type1.FullName, type2.FullName));

        // DataTableReader
        public static Exception InvalidDataTableReader(string tableName) => _InvalidOperation(SR.Format(SR.DataTableReader_InvalidDataTableReader, tableName));
        public static Exception DataTableReaderSchemaIsInvalid(string tableName) => _InvalidOperation(SR.Format(SR.DataTableReader_SchemaInvalidDataTableReader, tableName));
        public static Exception CannotCreateDataReaderOnEmptyDataSet() => _Argument(SR.DataTableReader_CannotCreateDataReaderOnEmptyDataSet);
        public static Exception DataTableReaderArgumentIsEmpty() => _Argument(SR.DataTableReader_DataTableReaderArgumentIsEmpty);
        public static Exception ArgumentContainsNullValue() => _Argument(SR.DataTableReader_ArgumentContainsNullValue);
        public static Exception InvalidCurrentRowInDataTableReader() => _DeletedRowInaccessible(SR.DataTableReader_InvalidRowInDataTableReader);
        public static Exception EmptyDataTableReader(string tableName) => _DeletedRowInaccessible(SR.Format(SR.DataTableReader_DataTableCleared, tableName));
        internal static Exception InvalidDuplicateNamedSimpleTypeDelaration(string stName, string errorStr) => _Argument(SR.Format(SR.NamedSimpleType_InvalidDuplicateNamedSimpleTypeDelaration, stName, errorStr));

        // RbTree
        internal static Exception InternalRBTreeError(RBTreeError internalError) => _InvalidOperation(SR.Format(SR.RbTree_InvalidState, (int)internalError));
        public static Exception EnumeratorModified() => _InvalidOperation(SR.RbTree_EnumerationBroken);
    }
}
