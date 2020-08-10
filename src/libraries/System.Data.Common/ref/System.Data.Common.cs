// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Data
{
    public enum AcceptRejectRule
    {
        None = 0,
        Cascade = 1,
    }
    [System.FlagsAttribute]
    public enum CommandBehavior
    {
        Default = 0,
        SingleResult = 1,
        SchemaOnly = 2,
        KeyInfo = 4,
        SingleRow = 8,
        SequentialAccess = 16,
        CloseConnection = 32,
    }
    public enum CommandType
    {
        Text = 1,
        StoredProcedure = 4,
        TableDirect = 512,
    }
    public enum ConflictOption
    {
        CompareAllSearchableValues = 1,
        CompareRowVersion = 2,
        OverwriteChanges = 3,
    }
    [System.FlagsAttribute]
    public enum ConnectionState
    {
        Closed = 0,
        Open = 1,
        Connecting = 2,
        Executing = 4,
        Fetching = 8,
        Broken = 16,
    }
    [System.ComponentModel.DefaultPropertyAttribute("ConstraintName")]
    public abstract partial class Constraint
    {
        internal Constraint() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public virtual string ConstraintName { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.PropertyCollection ExtendedProperties { get { throw null; } }
        public abstract System.Data.DataTable? Table { get; }
        [System.CLSCompliantAttribute(false)]
        protected virtual System.Data.DataSet? _DataSet { get { throw null; } }
        protected void CheckStateForProperty() { }
        protected internal void SetDataSet(System.Data.DataSet dataSet) { }
        public override string ToString() { throw null; }
    }
    [System.ComponentModel.DefaultEventAttribute("CollectionChanged")]
    public sealed partial class ConstraintCollection : System.Data.InternalDataCollectionBase
    {
        internal ConstraintCollection() { }
        public System.Data.Constraint this[int index] { get { throw null; } }
        public System.Data.Constraint? this[string? name] { get { throw null; } }
        protected override System.Collections.ArrayList List { get { throw null; } }
        public event System.ComponentModel.CollectionChangeEventHandler? CollectionChanged { add { } remove { } }
        public void Add(System.Data.Constraint constraint) { }
        public System.Data.Constraint Add(string? name, System.Data.DataColumn column, bool primaryKey) { throw null; }
        public System.Data.Constraint Add(string? name, System.Data.DataColumn primaryKeyColumn, System.Data.DataColumn foreignKeyColumn) { throw null; }
        public System.Data.Constraint Add(string? name, System.Data.DataColumn[] columns, bool primaryKey) { throw null; }
        public System.Data.Constraint Add(string? name, System.Data.DataColumn[] primaryKeyColumns, System.Data.DataColumn[] foreignKeyColumns) { throw null; }
        public void AddRange(System.Data.Constraint[]? constraints) { }
        public bool CanRemove(System.Data.Constraint constraint) { throw null; }
        public void Clear() { }
        public bool Contains(string? name) { throw null; }
        public void CopyTo(System.Data.Constraint[] array, int index) { }
        public int IndexOf(System.Data.Constraint? constraint) { throw null; }
        public int IndexOf(string? constraintName) { throw null; }
        public void Remove(System.Data.Constraint constraint) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
    }
    public partial class ConstraintException : System.Data.DataException
    {
        public ConstraintException() { }
        protected ConstraintException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ConstraintException(string? s) { }
        public ConstraintException(string? message, System.Exception? innerException) { }
    }
    [System.ComponentModel.DefaultPropertyAttribute("ColumnName")]
    [System.ComponentModel.DesignTimeVisibleAttribute(false)]
    [System.ComponentModel.ToolboxItemAttribute(false)]
    public partial class DataColumn : System.ComponentModel.MarshalByValueComponent
    {
        public DataColumn() { }
        public DataColumn(string? columnName) { }
        public DataColumn(string? columnName, System.Type dataType) { }
        public DataColumn(string? columnName, System.Type dataType, string? expr) { }
        public DataColumn(string? columnName, System.Type dataType, string? expr, System.Data.MappingType type) { }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool AllowDBNull { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public bool AutoIncrement { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((long)0)]
        public long AutoIncrementSeed { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((long)1)]
        public long AutoIncrementStep { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Caption { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.MappingType.Element)]
        public virtual System.Data.MappingType ColumnMapping { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string ColumnName { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.DataSetDateTime.UnspecifiedLocal)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public System.Data.DataSetDateTime DateTimeMode { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Expression { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.PropertyCollection ExtendedProperties { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(-1)]
        public int MaxLength { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Namespace { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Ordinal { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Prefix { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ReadOnly { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.DataTable? Table { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool Unique { get { throw null; } set { } }
        protected internal void CheckNotAllowNull() { }
        protected void CheckUnique() { }
        protected virtual void OnPropertyChanging(System.ComponentModel.PropertyChangedEventArgs pcevent) { }
        protected internal void RaisePropertyChanging(string name) { }
        public void SetOrdinal(int ordinal) { }
        public override string ToString() { throw null; }
    }
    public partial class DataColumnChangeEventArgs : System.EventArgs
    {
        public DataColumnChangeEventArgs(System.Data.DataRow row, System.Data.DataColumn? column, object? value) { }
        public System.Data.DataColumn? Column { get { throw null; } }
        public object? ProposedValue { get { throw null; } set { } }
        public System.Data.DataRow Row { get { throw null; } }
    }
    public delegate void DataColumnChangeEventHandler(object sender, System.Data.DataColumnChangeEventArgs e);
    [System.ComponentModel.DefaultEventAttribute("CollectionChanged")]
    public sealed partial class DataColumnCollection : System.Data.InternalDataCollectionBase
    {
        internal DataColumnCollection() { }
        public System.Data.DataColumn this[int index] { get { throw null; } }
        public System.Data.DataColumn? this[string name] { get { throw null; } }
        protected override System.Collections.ArrayList List { get { throw null; } }
        public event System.ComponentModel.CollectionChangeEventHandler? CollectionChanged { add { } remove { } }
        public System.Data.DataColumn Add() { throw null; }
        public void Add(System.Data.DataColumn column) { }
        public System.Data.DataColumn Add(string? columnName) { throw null; }
        public System.Data.DataColumn Add(string? columnName, System.Type type) { throw null; }
        public System.Data.DataColumn Add(string? columnName, System.Type type, string expression) { throw null; }
        public void AddRange(System.Data.DataColumn[] columns) { }
        public bool CanRemove(System.Data.DataColumn? column) { throw null; }
        public void Clear() { }
        public bool Contains(string name) { throw null; }
        public void CopyTo(System.Data.DataColumn[] array, int index) { }
        public int IndexOf(System.Data.DataColumn? column) { throw null; }
        public int IndexOf(string? columnName) { throw null; }
        public void Remove(System.Data.DataColumn column) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
    }
    public partial class DataException : System.SystemException
    {
        public DataException() { }
        protected DataException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DataException(string? s) { }
        public DataException(string? s, System.Exception? innerException) { }
    }
    public static partial class DataReaderExtensions
    {
        public static bool GetBoolean(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static byte GetByte(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static long GetBytes(this System.Data.Common.DbDataReader reader, string name, long dataOffset, byte[] buffer, int bufferOffset, int length) { throw null; }
        public static char GetChar(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static long GetChars(this System.Data.Common.DbDataReader reader, string name, long dataOffset, char[] buffer, int bufferOffset, int length) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.Data.Common.DbDataReader GetData(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static string GetDataTypeName(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static System.DateTime GetDateTime(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static decimal GetDecimal(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static double GetDouble(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static System.Type GetFieldType(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static System.Threading.Tasks.Task<T> GetFieldValueAsync<T>(this System.Data.Common.DbDataReader reader, string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public static T GetFieldValue<T>(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static float GetFloat(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static System.Guid GetGuid(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static short GetInt16(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static int GetInt32(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static long GetInt64(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static System.Type GetProviderSpecificFieldType(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static object GetProviderSpecificValue(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static System.IO.Stream GetStream(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static string GetString(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static System.IO.TextReader GetTextReader(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static object GetValue(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static bool IsDBNull(this System.Data.Common.DbDataReader reader, string name) { throw null; }
        public static System.Threading.Tasks.Task<bool> IsDBNullAsync(this System.Data.Common.DbDataReader reader, string name, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
    }
    [System.ComponentModel.DefaultPropertyAttribute("RelationName")]
    public partial class DataRelation
    {
        public DataRelation(string? relationName, System.Data.DataColumn parentColumn, System.Data.DataColumn childColumn) { }
        public DataRelation(string? relationName, System.Data.DataColumn parentColumn, System.Data.DataColumn childColumn, bool createConstraints) { }
        public DataRelation(string? relationName, System.Data.DataColumn[] parentColumns, System.Data.DataColumn[] childColumns) { }
        public DataRelation(string? relationName, System.Data.DataColumn[] parentColumns, System.Data.DataColumn[] childColumns, bool createConstraints) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public DataRelation(string relationName, string? parentTableName, string? parentTableNamespace, string? childTableName, string? childTableNamespace, string[]? parentColumnNames, string[]? childColumnNames, bool nested) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public DataRelation(string relationName, string? parentTableName, string? childTableName, string[]? parentColumnNames, string[]? childColumnNames, bool nested) { }
        public virtual System.Data.DataColumn[] ChildColumns { get { throw null; } }
        public virtual System.Data.ForeignKeyConstraint? ChildKeyConstraint { get { throw null; } }
        public virtual System.Data.DataTable ChildTable { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public virtual System.Data.DataSet? DataSet { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.PropertyCollection ExtendedProperties { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public virtual bool Nested { get { throw null; } set { } }
        public virtual System.Data.DataColumn[] ParentColumns { get { throw null; } }
        public virtual System.Data.UniqueConstraint? ParentKeyConstraint { get { throw null; } }
        public virtual System.Data.DataTable ParentTable { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public virtual string RelationName { get { throw null; } set { } }
        protected void CheckStateForProperty() { }
        protected internal void OnPropertyChanging(System.ComponentModel.PropertyChangedEventArgs pcevent) { }
        protected internal void RaisePropertyChanging(string name) { }
        public override string ToString() { throw null; }
    }
    [System.ComponentModel.DefaultEventAttribute("CollectionChanged")]
    [System.ComponentModel.DefaultPropertyAttribute("Table")]
    public abstract partial class DataRelationCollection : System.Data.InternalDataCollectionBase
    {
        protected DataRelationCollection() { }
        public abstract System.Data.DataRelation this[int index] { get; }
        public abstract System.Data.DataRelation? this[string? name] { get; }
        public event System.ComponentModel.CollectionChangeEventHandler? CollectionChanged { add { } remove { } }
        public virtual System.Data.DataRelation Add(System.Data.DataColumn parentColumn, System.Data.DataColumn childColumn) { throw null; }
        public virtual System.Data.DataRelation Add(System.Data.DataColumn[] parentColumns, System.Data.DataColumn[] childColumns) { throw null; }
        public void Add(System.Data.DataRelation relation) { }
        public virtual System.Data.DataRelation Add(string? name, System.Data.DataColumn parentColumn, System.Data.DataColumn childColumn) { throw null; }
        public virtual System.Data.DataRelation Add(string? name, System.Data.DataColumn parentColumn, System.Data.DataColumn childColumn, bool createConstraints) { throw null; }
        public virtual System.Data.DataRelation Add(string? name, System.Data.DataColumn[] parentColumns, System.Data.DataColumn[] childColumns) { throw null; }
        public virtual System.Data.DataRelation Add(string? name, System.Data.DataColumn[] parentColumns, System.Data.DataColumn[] childColumns, bool createConstraints) { throw null; }
        protected virtual void AddCore(System.Data.DataRelation relation) { }
        public virtual void AddRange(System.Data.DataRelation[]? relations) { }
        public virtual bool CanRemove(System.Data.DataRelation? relation) { throw null; }
        public virtual void Clear() { }
        public virtual bool Contains(string? name) { throw null; }
        public void CopyTo(System.Data.DataRelation[] array, int index) { }
        protected abstract System.Data.DataSet GetDataSet();
        public virtual int IndexOf(System.Data.DataRelation? relation) { throw null; }
        public virtual int IndexOf(string? relationName) { throw null; }
        protected virtual void OnCollectionChanged(System.ComponentModel.CollectionChangeEventArgs ccevent) { }
        protected virtual void OnCollectionChanging(System.ComponentModel.CollectionChangeEventArgs ccevent) { }
        public void Remove(System.Data.DataRelation relation) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
        protected virtual void RemoveCore(System.Data.DataRelation relation) { }
    }
    public partial class DataRow
    {
        protected internal DataRow(System.Data.DataRowBuilder builder) { }
        public bool HasErrors { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object this[System.Data.DataColumn column] { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object this[System.Data.DataColumn column, System.Data.DataRowVersion version] { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object this[int columnIndex] { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object this[int columnIndex, System.Data.DataRowVersion version] { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object this[string columnName] { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object this[string columnName, System.Data.DataRowVersion version] { get { throw null; } }
        public object?[] ItemArray { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string RowError { get { throw null; } set { } }
        public System.Data.DataRowState RowState { get { throw null; } }
        public System.Data.DataTable Table { get { throw null; } }
        public void AcceptChanges() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public void BeginEdit() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public void CancelEdit() { }
        public void ClearErrors() { }
        public void Delete() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public void EndEdit() { }
        public System.Data.DataRow[] GetChildRows(System.Data.DataRelation? relation) { throw null; }
        public System.Data.DataRow[] GetChildRows(System.Data.DataRelation? relation, System.Data.DataRowVersion version) { throw null; }
        public System.Data.DataRow[] GetChildRows(string? relationName) { throw null; }
        public System.Data.DataRow[] GetChildRows(string? relationName, System.Data.DataRowVersion version) { throw null; }
        public string GetColumnError(System.Data.DataColumn column) { throw null; }
        public string GetColumnError(int columnIndex) { throw null; }
        public string GetColumnError(string columnName) { throw null; }
        public System.Data.DataColumn[] GetColumnsInError() { throw null; }
        public System.Data.DataRow? GetParentRow(System.Data.DataRelation? relation) { throw null; }
        public System.Data.DataRow? GetParentRow(System.Data.DataRelation? relation, System.Data.DataRowVersion version) { throw null; }
        public System.Data.DataRow? GetParentRow(string? relationName) { throw null; }
        public System.Data.DataRow? GetParentRow(string? relationName, System.Data.DataRowVersion version) { throw null; }
        public System.Data.DataRow[] GetParentRows(System.Data.DataRelation? relation) { throw null; }
        public System.Data.DataRow[] GetParentRows(System.Data.DataRelation? relation, System.Data.DataRowVersion version) { throw null; }
        public System.Data.DataRow[] GetParentRows(string? relationName) { throw null; }
        public System.Data.DataRow[] GetParentRows(string? relationName, System.Data.DataRowVersion version) { throw null; }
        public bool HasVersion(System.Data.DataRowVersion version) { throw null; }
        public bool IsNull(System.Data.DataColumn column) { throw null; }
        public bool IsNull(System.Data.DataColumn column, System.Data.DataRowVersion version) { throw null; }
        public bool IsNull(int columnIndex) { throw null; }
        public bool IsNull(string columnName) { throw null; }
        public void RejectChanges() { }
        public void SetAdded() { }
        public void SetColumnError(System.Data.DataColumn column, string? error) { }
        public void SetColumnError(int columnIndex, string? error) { }
        public void SetColumnError(string columnName, string? error) { }
        public void SetModified() { }
        protected void SetNull(System.Data.DataColumn column) { }
        public void SetParentRow(System.Data.DataRow? parentRow) { }
        public void SetParentRow(System.Data.DataRow? parentRow, System.Data.DataRelation? relation) { }
    }
    [System.FlagsAttribute]
    public enum DataRowAction
    {
        Nothing = 0,
        Delete = 1,
        Change = 2,
        Rollback = 4,
        Commit = 8,
        Add = 16,
        ChangeOriginal = 32,
        ChangeCurrentAndOriginal = 64,
    }
    public sealed partial class DataRowBuilder
    {
        internal DataRowBuilder() { }
    }
    public partial class DataRowChangeEventArgs : System.EventArgs
    {
        public DataRowChangeEventArgs(System.Data.DataRow row, System.Data.DataRowAction action) { }
        public System.Data.DataRowAction Action { get { throw null; } }
        public System.Data.DataRow Row { get { throw null; } }
    }
    public delegate void DataRowChangeEventHandler(object sender, System.Data.DataRowChangeEventArgs e);
    public sealed partial class DataRowCollection : System.Data.InternalDataCollectionBase
    {
        internal DataRowCollection() { }
        public override int Count { get { throw null; } }
        public System.Data.DataRow this[int index] { get { throw null; } }
        public void Add(System.Data.DataRow row) { }
        public System.Data.DataRow Add(params object?[] values) { throw null; }
        public void Clear() { }
        public bool Contains(object? key) { throw null; }
        public bool Contains(object?[] keys) { throw null; }
        public override void CopyTo(System.Array ar, int index) { }
        public void CopyTo(System.Data.DataRow[] array, int index) { }
        public System.Data.DataRow? Find(object? key) { throw null; }
        public System.Data.DataRow? Find(object?[] keys) { throw null; }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.Data.DataRow? row) { throw null; }
        public void InsertAt(System.Data.DataRow row, int pos) { }
        public void Remove(System.Data.DataRow row) { }
        public void RemoveAt(int index) { }
    }
    public static partial class DataRowComparer
    {
        public static System.Data.DataRowComparer<System.Data.DataRow> Default { get { throw null; } }
    }
    public sealed partial class DataRowComparer<TRow> : System.Collections.Generic.IEqualityComparer<TRow> where TRow : System.Data.DataRow
    {
        internal DataRowComparer() { }
        public static System.Data.DataRowComparer<TRow> Default { get { throw null; } }
        public bool Equals(TRow? leftRow, TRow? rightRow) { throw null; }
        public int GetHashCode(TRow row) { throw null; }
    }
    public static partial class DataRowExtensions
    {
        public static T? Field<T>(this System.Data.DataRow row, System.Data.DataColumn column) { throw null; }
        public static T? Field<T>(this System.Data.DataRow row, System.Data.DataColumn column, System.Data.DataRowVersion version) { throw null; }
        public static T? Field<T>(this System.Data.DataRow row, int columnIndex) { throw null; }
        public static T? Field<T>(this System.Data.DataRow row, int columnIndex, System.Data.DataRowVersion version) { throw null; }
        public static T? Field<T>(this System.Data.DataRow row, string columnName) { throw null; }
        public static T? Field<T>(this System.Data.DataRow row, string columnName, System.Data.DataRowVersion version) { throw null; }
        public static void SetField<T>(this System.Data.DataRow row, System.Data.DataColumn column, T? value) { }
        public static void SetField<T>(this System.Data.DataRow row, int columnIndex, T? value) { }
        public static void SetField<T>(this System.Data.DataRow row, string columnName, T? value) { }
    }
    [System.FlagsAttribute]
    public enum DataRowState
    {
        Detached = 1,
        Unchanged = 2,
        Added = 4,
        Deleted = 8,
        Modified = 16,
    }
    public enum DataRowVersion
    {
        Original = 256,
        Current = 512,
        Proposed = 1024,
        Default = 1536,
    }
    public partial class DataRowView : System.ComponentModel.ICustomTypeDescriptor, System.ComponentModel.IDataErrorInfo, System.ComponentModel.IEditableObject, System.ComponentModel.INotifyPropertyChanged
    {
        internal DataRowView() { }
        public System.Data.DataView DataView { get { throw null; } }
        public bool IsEdit { get { throw null; } }
        public bool IsNew { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object this[int ndx] { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public object this[string property] { get { throw null; } set { } }
        public System.Data.DataRow Row { get { throw null; } }
        public System.Data.DataRowVersion RowVersion { get { throw null; } }
#nullable disable
        string System.ComponentModel.IDataErrorInfo.Error { get { throw null; } }
        string System.ComponentModel.IDataErrorInfo.this[string colName] { get { throw null; } }
#nullable enable
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged { add { } remove { } }
        public void BeginEdit() { }
        public void CancelEdit() { }
        public System.Data.DataView CreateChildView(System.Data.DataRelation relation) { throw null; }
        public System.Data.DataView CreateChildView(System.Data.DataRelation relation, bool followParent) { throw null; }
        public System.Data.DataView CreateChildView(string relationName) { throw null; }
        public System.Data.DataView CreateChildView(string relationName, bool followParent) { throw null; }
        public void Delete() { }
        public void EndEdit() { }
        public override bool Equals(object? other) { throw null; }
        public override int GetHashCode() { throw null; }
#nullable disable
        System.ComponentModel.AttributeCollection System.ComponentModel.ICustomTypeDescriptor.GetAttributes() { throw null; }
        string System.ComponentModel.ICustomTypeDescriptor.GetClassName() { throw null; }
        string System.ComponentModel.ICustomTypeDescriptor.GetComponentName() { throw null; }
        System.ComponentModel.TypeConverter System.ComponentModel.ICustomTypeDescriptor.GetConverter() { throw null; }
        System.ComponentModel.EventDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent() { throw null; }
        System.ComponentModel.PropertyDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty() { throw null; }
        object System.ComponentModel.ICustomTypeDescriptor.GetEditor(System.Type editorBaseType) { throw null; }
        System.ComponentModel.EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents() { throw null; }
        System.ComponentModel.EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents(System.Attribute[] attributes) { throw null; }
        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties() { throw null; }
        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties(System.Attribute[] attributes) { throw null; }
        object System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd) { throw null; }
#nullable enable
    }
    [System.ComponentModel.DesignerAttribute("Microsoft.VSDesigner.Data.VS.DataSetDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [System.ComponentModel.DefaultPropertyAttribute("DataSetName")]
    [System.Xml.Serialization.XmlRootAttribute("DataSet")]
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetDataSetSchema")]
    public partial class DataSet : System.ComponentModel.MarshalByValueComponent, System.ComponentModel.IListSource, System.ComponentModel.ISupportInitialize, System.ComponentModel.ISupportInitializeNotification, System.Runtime.Serialization.ISerializable, System.Xml.Serialization.IXmlSerializable
    {
        public DataSet() { }
        protected DataSet(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        protected DataSet(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context, bool ConstructSchema) { }
        public DataSet(string dataSetName) { }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool CaseSensitive { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string DataSetName { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.DataViewManager DefaultViewManager { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool EnforceConstraints { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.PropertyCollection ExtendedProperties { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool HasErrors { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsInitialized { get { throw null; } }
        public System.Globalization.CultureInfo Locale { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Namespace { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Prefix { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Data.DataRelationCollection Relations { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.SerializationFormat.Xml)]
        public System.Data.SerializationFormat RemotingFormat { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public virtual System.Data.SchemaSerializationMode SchemaSerializationMode { get { throw null; } set { } }
#nullable disable
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public override System.ComponentModel.ISite Site { get { throw null; } set { } }
        bool System.ComponentModel.IListSource.ContainsListCollection { get { throw null; } }
#nullable enable
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Data.DataTableCollection Tables { get { throw null; } }
        public event System.EventHandler? Initialized { add { } remove { } }
        public event System.Data.MergeFailedEventHandler? MergeFailed { add { } remove { } }
        public void AcceptChanges() { }
        public void BeginInit() { }
        public void Clear() { }
        public virtual System.Data.DataSet Clone() { throw null; }
        public System.Data.DataSet Copy() { throw null; }
        public System.Data.DataTableReader CreateDataReader() { throw null; }
        public System.Data.DataTableReader CreateDataReader(params System.Data.DataTable[] dataTables) { throw null; }
        protected System.Data.SchemaSerializationMode DetermineSchemaSerializationMode(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { throw null; }
        protected System.Data.SchemaSerializationMode DetermineSchemaSerializationMode(System.Xml.XmlReader reader) { throw null; }
        public void EndInit() { }
        public System.Data.DataSet? GetChanges() { throw null; }
        public System.Data.DataSet? GetChanges(System.Data.DataRowState rowStates) { throw null; }
        public static System.Xml.Schema.XmlSchemaComplexType GetDataSetSchema(System.Xml.Schema.XmlSchemaSet? schemaSet) { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        protected virtual System.Xml.Schema.XmlSchema? GetSchemaSerializable() { throw null; }
        protected void GetSerializationData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public string GetXml() { throw null; }
        public string GetXmlSchema() { throw null; }
        public bool HasChanges() { throw null; }
        public bool HasChanges(System.Data.DataRowState rowStates) { throw null; }
        public void InferXmlSchema(System.IO.Stream? stream, string[]? nsArray) { }
        public void InferXmlSchema(System.IO.TextReader? reader, string[]? nsArray) { }
        public void InferXmlSchema(string fileName, string[]? nsArray) { }
        public void InferXmlSchema(System.Xml.XmlReader? reader, string[]? nsArray) { }
        protected virtual void InitializeDerivedDataSet() { }
        protected bool IsBinarySerialized(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { throw null; }
        public void Load(System.Data.IDataReader reader, System.Data.LoadOption loadOption, params System.Data.DataTable[] tables) { }
        public virtual void Load(System.Data.IDataReader reader, System.Data.LoadOption loadOption, System.Data.FillErrorEventHandler? errorHandler, params System.Data.DataTable[] tables) { }
        public void Load(System.Data.IDataReader reader, System.Data.LoadOption loadOption, params string[] tables) { }
        public void Merge(System.Data.DataRow[] rows) { }
        public void Merge(System.Data.DataRow[] rows, bool preserveChanges, System.Data.MissingSchemaAction missingSchemaAction) { }
        public void Merge(System.Data.DataSet dataSet) { }
        public void Merge(System.Data.DataSet dataSet, bool preserveChanges) { }
        public void Merge(System.Data.DataSet dataSet, bool preserveChanges, System.Data.MissingSchemaAction missingSchemaAction) { }
        public void Merge(System.Data.DataTable table) { }
        public void Merge(System.Data.DataTable table, bool preserveChanges, System.Data.MissingSchemaAction missingSchemaAction) { }
        protected virtual void OnPropertyChanging(System.ComponentModel.PropertyChangedEventArgs pcevent) { }
        protected virtual void OnRemoveRelation(System.Data.DataRelation relation) { }
        protected internal virtual void OnRemoveTable(System.Data.DataTable table) { }
        protected internal void RaisePropertyChanging(string name) { }
        public System.Data.XmlReadMode ReadXml(System.IO.Stream? stream) { throw null; }
        public System.Data.XmlReadMode ReadXml(System.IO.Stream? stream, System.Data.XmlReadMode mode) { throw null; }
        public System.Data.XmlReadMode ReadXml(System.IO.TextReader? reader) { throw null; }
        public System.Data.XmlReadMode ReadXml(System.IO.TextReader? reader, System.Data.XmlReadMode mode) { throw null; }
        public System.Data.XmlReadMode ReadXml(string fileName) { throw null; }
        public System.Data.XmlReadMode ReadXml(string fileName, System.Data.XmlReadMode mode) { throw null; }
        public System.Data.XmlReadMode ReadXml(System.Xml.XmlReader? reader) { throw null; }
        public System.Data.XmlReadMode ReadXml(System.Xml.XmlReader? reader, System.Data.XmlReadMode mode) { throw null; }
        public void ReadXmlSchema(System.IO.Stream? stream) { }
        public void ReadXmlSchema(System.IO.TextReader? reader) { }
        public void ReadXmlSchema(string fileName) { }
        public void ReadXmlSchema(System.Xml.XmlReader? reader) { }
        protected virtual void ReadXmlSerializable(System.Xml.XmlReader reader) { }
        public virtual void RejectChanges() { }
        public virtual void Reset() { }
        protected virtual bool ShouldSerializeRelations() { throw null; }
        protected virtual bool ShouldSerializeTables() { throw null; }
#nullable disable
        System.Collections.IList System.ComponentModel.IListSource.GetList() { throw null; }
        System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
#nullable enable
        public void WriteXml(System.IO.Stream? stream) { }
        public void WriteXml(System.IO.Stream? stream, System.Data.XmlWriteMode mode) { }
        public void WriteXml(System.IO.TextWriter? writer) { }
        public void WriteXml(System.IO.TextWriter? writer, System.Data.XmlWriteMode mode) { }
        public void WriteXml(string fileName) { }
        public void WriteXml(string fileName, System.Data.XmlWriteMode mode) { }
        public void WriteXml(System.Xml.XmlWriter? writer) { }
        public void WriteXml(System.Xml.XmlWriter? writer, System.Data.XmlWriteMode mode) { }
        public void WriteXmlSchema(System.IO.Stream? stream) { }
        public void WriteXmlSchema(System.IO.Stream? stream, System.Converter<System.Type, string>? multipleTargetConverter) { }
        public void WriteXmlSchema(System.IO.TextWriter? writer) { }
        public void WriteXmlSchema(System.IO.TextWriter? writer, System.Converter<System.Type, string>? multipleTargetConverter) { }
        public void WriteXmlSchema(string fileName) { }
        public void WriteXmlSchema(string fileName, System.Converter<System.Type, string>? multipleTargetConverter) { }
        public void WriteXmlSchema(System.Xml.XmlWriter? writer) { }
        public void WriteXmlSchema(System.Xml.XmlWriter? writer, System.Converter<System.Type, string>? multipleTargetConverter) { }
    }
    public enum DataSetDateTime
    {
        Local = 1,
        Unspecified = 2,
        UnspecifiedLocal = 3,
        Utc = 4,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    [System.ObsoleteAttribute("DataSysDescriptionAttribute has been deprecated.  https://go.microsoft.com/fwlink/?linkid=14202", false)]
    public partial class DataSysDescriptionAttribute : System.ComponentModel.DescriptionAttribute
    {
        [System.ObsoleteAttribute("DataSysDescriptionAttribute has been deprecated.  https://go.microsoft.com/fwlink/?linkid=14202", false)]
        public DataSysDescriptionAttribute(string description) { }
        public override string Description { get { throw null; } }
    }
    [System.ComponentModel.DefaultEventAttribute("RowChanging")]
    [System.ComponentModel.DefaultPropertyAttribute("TableName")]
    [System.ComponentModel.DesignTimeVisibleAttribute(false)]
    [System.ComponentModel.ToolboxItemAttribute(false)]
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetDataTableSchema")]
    public partial class DataTable : System.ComponentModel.MarshalByValueComponent, System.ComponentModel.IListSource, System.ComponentModel.ISupportInitialize, System.ComponentModel.ISupportInitializeNotification, System.Runtime.Serialization.ISerializable, System.Xml.Serialization.IXmlSerializable
    {
        protected internal bool fInitInProgress;
        public DataTable() { }
        protected DataTable(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DataTable(string? tableName) { }
        public DataTable(string? tableName, string? tableNamespace) { }
        public bool CaseSensitive { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.DataRelationCollection ChildRelations { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Data.DataColumnCollection Columns { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Data.ConstraintCollection Constraints { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.DataSet? DataSet { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.DataView DefaultView { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DisplayExpression { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.PropertyCollection ExtendedProperties { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool HasErrors { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsInitialized { get { throw null; } }
        public System.Globalization.CultureInfo Locale { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(50)]
        public int MinimumCapacity { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.DataRelationCollection ParentRelations { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Prefix { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.SerializationFormat.Xml)]
        public System.Data.SerializationFormat RemotingFormat { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.DataRowCollection Rows { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
#nullable disable
        public override System.ComponentModel.ISite Site { get { throw null; } set { } }
#nullable enable
        bool System.ComponentModel.IListSource.ContainsListCollection { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string TableName { get { throw null; } set { } }
        public event System.Data.DataColumnChangeEventHandler? ColumnChanged { add { } remove { } }
        public event System.Data.DataColumnChangeEventHandler? ColumnChanging { add { } remove { } }
        public event System.EventHandler? Initialized { add { } remove { } }
        public event System.Data.DataRowChangeEventHandler? RowChanged { add { } remove { } }
        public event System.Data.DataRowChangeEventHandler? RowChanging { add { } remove { } }
        public event System.Data.DataRowChangeEventHandler? RowDeleted { add { } remove { } }
        public event System.Data.DataRowChangeEventHandler? RowDeleting { add { } remove { } }
        public event System.Data.DataTableClearEventHandler? TableCleared { add { } remove { } }
        public event System.Data.DataTableClearEventHandler? TableClearing { add { } remove { } }
        public event System.Data.DataTableNewRowEventHandler? TableNewRow { add { } remove { } }
        public void AcceptChanges() { }
        public virtual void BeginInit() { }
        public void BeginLoadData() { }
        public void Clear() { }
        public virtual System.Data.DataTable Clone() { throw null; }
        public object Compute(string? expression, string? filter) { throw null; }
        public System.Data.DataTable Copy() { throw null; }
        public System.Data.DataTableReader CreateDataReader() { throw null; }
        protected virtual System.Data.DataTable CreateInstance() { throw null; }
        public virtual void EndInit() { }
        public void EndLoadData() { }
        public System.Data.DataTable? GetChanges() { throw null; }
        public System.Data.DataTable? GetChanges(System.Data.DataRowState rowStates) { throw null; }
        public static System.Xml.Schema.XmlSchemaComplexType GetDataTableSchema(System.Xml.Schema.XmlSchemaSet? schemaSet) { throw null; }
        public System.Data.DataRow[] GetErrors() { throw null; }
        public virtual void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        protected virtual System.Type GetRowType() { throw null; }
        protected virtual System.Xml.Schema.XmlSchema GetSchema() { throw null; }
        public void ImportRow(System.Data.DataRow? row) { }
        public void Load(System.Data.IDataReader reader) { }
        public void Load(System.Data.IDataReader reader, System.Data.LoadOption loadOption) { }
        public virtual void Load(System.Data.IDataReader reader, System.Data.LoadOption loadOption, System.Data.FillErrorEventHandler? errorHandler) { }
        public System.Data.DataRow LoadDataRow(object?[] values, bool fAcceptChanges) { throw null; }
        public System.Data.DataRow LoadDataRow(object?[] values, System.Data.LoadOption loadOption) { throw null; }
        public void Merge(System.Data.DataTable table) { }
        public void Merge(System.Data.DataTable table, bool preserveChanges) { }
        public void Merge(System.Data.DataTable table, bool preserveChanges, System.Data.MissingSchemaAction missingSchemaAction) { }
        public System.Data.DataRow NewRow() { throw null; }
        protected internal System.Data.DataRow[] NewRowArray(int size) { throw null; }
        protected virtual System.Data.DataRow NewRowFromBuilder(System.Data.DataRowBuilder builder) { throw null; }
        protected internal virtual void OnColumnChanged(System.Data.DataColumnChangeEventArgs e) { }
        protected internal virtual void OnColumnChanging(System.Data.DataColumnChangeEventArgs e) { }
        protected virtual void OnPropertyChanging(System.ComponentModel.PropertyChangedEventArgs pcevent) { }
        protected virtual void OnRemoveColumn(System.Data.DataColumn column) { }
        protected virtual void OnRowChanged(System.Data.DataRowChangeEventArgs e) { }
        protected virtual void OnRowChanging(System.Data.DataRowChangeEventArgs e) { }
        protected virtual void OnRowDeleted(System.Data.DataRowChangeEventArgs e) { }
        protected virtual void OnRowDeleting(System.Data.DataRowChangeEventArgs e) { }
        protected virtual void OnTableCleared(System.Data.DataTableClearEventArgs e) { }
        protected virtual void OnTableClearing(System.Data.DataTableClearEventArgs e) { }
        protected virtual void OnTableNewRow(System.Data.DataTableNewRowEventArgs e) { }
        public System.Data.XmlReadMode ReadXml(System.IO.Stream? stream) { throw null; }
        public System.Data.XmlReadMode ReadXml(System.IO.TextReader? reader) { throw null; }
        public System.Data.XmlReadMode ReadXml(string fileName) { throw null; }
        public System.Data.XmlReadMode ReadXml(System.Xml.XmlReader? reader) { throw null; }
        public void ReadXmlSchema(System.IO.Stream? stream) { }
        public void ReadXmlSchema(System.IO.TextReader? reader) { }
        public void ReadXmlSchema(string fileName) { }
        public void ReadXmlSchema(System.Xml.XmlReader? reader) { }
        protected virtual void ReadXmlSerializable(System.Xml.XmlReader? reader) { }
        public void RejectChanges() { }
        public virtual void Reset() { }
        public System.Data.DataRow[] Select() { throw null; }
        public System.Data.DataRow[] Select(string? filterExpression) { throw null; }
        public System.Data.DataRow[] Select(string? filterExpression, string? sort) { throw null; }
        public System.Data.DataRow[] Select(string? filterExpression, string? sort, System.Data.DataViewRowState recordStates) { throw null; }
        System.Collections.IList System.ComponentModel.IListSource.GetList() { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
#nullable disable
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
#nullable enable
        public override string ToString() { throw null; }
        public void WriteXml(System.IO.Stream? stream) { }
        public void WriteXml(System.IO.Stream? stream, bool writeHierarchy) { }
        public void WriteXml(System.IO.Stream? stream, System.Data.XmlWriteMode mode) { }
        public void WriteXml(System.IO.Stream? stream, System.Data.XmlWriteMode mode, bool writeHierarchy) { }
        public void WriteXml(System.IO.TextWriter? writer) { }
        public void WriteXml(System.IO.TextWriter? writer, bool writeHierarchy) { }
        public void WriteXml(System.IO.TextWriter? writer, System.Data.XmlWriteMode mode) { }
        public void WriteXml(System.IO.TextWriter? writer, System.Data.XmlWriteMode mode, bool writeHierarchy) { }
        public void WriteXml(string fileName) { }
        public void WriteXml(string fileName, bool writeHierarchy) { }
        public void WriteXml(string fileName, System.Data.XmlWriteMode mode) { }
        public void WriteXml(string fileName, System.Data.XmlWriteMode mode, bool writeHierarchy) { }
        public void WriteXml(System.Xml.XmlWriter? writer) { }
        public void WriteXml(System.Xml.XmlWriter? writer, bool writeHierarchy) { }
        public void WriteXml(System.Xml.XmlWriter? writer, System.Data.XmlWriteMode mode) { }
        public void WriteXml(System.Xml.XmlWriter? writer, System.Data.XmlWriteMode mode, bool writeHierarchy) { }
        public void WriteXmlSchema(System.IO.Stream? stream) { }
        public void WriteXmlSchema(System.IO.Stream? stream, bool writeHierarchy) { }
        public void WriteXmlSchema(System.IO.TextWriter? writer) { }
        public void WriteXmlSchema(System.IO.TextWriter? writer, bool writeHierarchy) { }
        public void WriteXmlSchema(string fileName) { }
        public void WriteXmlSchema(string fileName, bool writeHierarchy) { }
        public void WriteXmlSchema(System.Xml.XmlWriter? writer) { }
        public void WriteXmlSchema(System.Xml.XmlWriter? writer, bool writeHierarchy) { }
    }
    public sealed partial class DataTableClearEventArgs : System.EventArgs
    {
        public DataTableClearEventArgs(System.Data.DataTable dataTable) { }
        public System.Data.DataTable Table { get { throw null; } }
        public string TableName { get { throw null; } }
        public string TableNamespace { get { throw null; } }
    }
    public delegate void DataTableClearEventHandler(object sender, System.Data.DataTableClearEventArgs e);
    [System.ComponentModel.DefaultEventAttribute("CollectionChanged")]
    [System.ComponentModel.ListBindableAttribute(false)]
    public sealed partial class DataTableCollection : System.Data.InternalDataCollectionBase
    {
        internal DataTableCollection() { }
        public System.Data.DataTable this[int index] { get { throw null; } }
        public System.Data.DataTable? this[string? name] { get { throw null; } }
        public System.Data.DataTable? this[string? name, string tableNamespace] { get { throw null; } }
        protected override System.Collections.ArrayList List { get { throw null; } }
        public event System.ComponentModel.CollectionChangeEventHandler? CollectionChanged { add { } remove { } }
        public event System.ComponentModel.CollectionChangeEventHandler? CollectionChanging { add { } remove { } }
        public System.Data.DataTable Add() { throw null; }
        public void Add(System.Data.DataTable table) { }
        public System.Data.DataTable Add(string? name) { throw null; }
        public System.Data.DataTable Add(string? name, string? tableNamespace) { throw null; }
        public void AddRange(System.Data.DataTable?[]? tables) { }
        public bool CanRemove(System.Data.DataTable? table) { throw null; }
        public void Clear() { }
        public bool Contains(string? name) { throw null; }
        public bool Contains(string name, string tableNamespace) { throw null; }
        public void CopyTo(System.Data.DataTable[] array, int index) { }
        public int IndexOf(System.Data.DataTable table) { throw null; }
        public int IndexOf(string? tableName) { throw null; }
        public int IndexOf(string tableName, string tableNamespace) { throw null; }
        public void Remove(System.Data.DataTable table) { }
        public void Remove(string name) { }
        public void Remove(string name, string tableNamespace) { }
        public void RemoveAt(int index) { }
    }
    public static partial class DataTableExtensions
    {
        public static System.Data.DataView AsDataView(this System.Data.DataTable table) { throw null; }
        public static System.Data.DataView AsDataView<T>(this System.Data.EnumerableRowCollection<T> source) where T : System.Data.DataRow { throw null; }
        public static System.Data.EnumerableRowCollection<System.Data.DataRow> AsEnumerable(this System.Data.DataTable source) { throw null; }
        public static System.Data.DataTable CopyToDataTable<T>(this System.Collections.Generic.IEnumerable<T> source) where T : System.Data.DataRow { throw null; }
        public static void CopyToDataTable<T>(this System.Collections.Generic.IEnumerable<T> source, System.Data.DataTable table, System.Data.LoadOption options) where T : System.Data.DataRow { }
        public static void CopyToDataTable<T>(this System.Collections.Generic.IEnumerable<T> source, System.Data.DataTable table, System.Data.LoadOption options, System.Data.FillErrorEventHandler? errorHandler) where T : System.Data.DataRow { }
    }
    public sealed partial class DataTableNewRowEventArgs : System.EventArgs
    {
        public DataTableNewRowEventArgs(System.Data.DataRow dataRow) { }
        public System.Data.DataRow Row { get { throw null; } }
    }
    public delegate void DataTableNewRowEventHandler(object sender, System.Data.DataTableNewRowEventArgs e);
    public sealed partial class DataTableReader : System.Data.Common.DbDataReader
    {
        public DataTableReader(System.Data.DataTable dataTable) { }
        public DataTableReader(System.Data.DataTable[] dataTables) { }
        public override int Depth { get { throw null; } }
        public override int FieldCount { get { throw null; } }
        public override bool HasRows { get { throw null; } }
        public override bool IsClosed { get { throw null; } }
        public override object this[int ordinal] { get { throw null; } }
        public override object this[string name] { get { throw null; } }
        public override int RecordsAffected { get { throw null; } }
        public override void Close() { }
        public override bool GetBoolean(int ordinal) { throw null; }
        public override byte GetByte(int ordinal) { throw null; }
        public override long GetBytes(int ordinal, long dataIndex, byte[]? buffer, int bufferIndex, int length) { throw null; }
        public override char GetChar(int ordinal) { throw null; }
        public override long GetChars(int ordinal, long dataIndex, char[]? buffer, int bufferIndex, int length) { throw null; }
        public override string GetDataTypeName(int ordinal) { throw null; }
        public override System.DateTime GetDateTime(int ordinal) { throw null; }
        public override decimal GetDecimal(int ordinal) { throw null; }
        public override double GetDouble(int ordinal) { throw null; }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override System.Type GetFieldType(int ordinal) { throw null; }
        public override float GetFloat(int ordinal) { throw null; }
        public override System.Guid GetGuid(int ordinal) { throw null; }
        public override short GetInt16(int ordinal) { throw null; }
        public override int GetInt32(int ordinal) { throw null; }
        public override long GetInt64(int ordinal) { throw null; }
        public override string GetName(int ordinal) { throw null; }
        public override int GetOrdinal(string name) { throw null; }
        public override System.Type GetProviderSpecificFieldType(int ordinal) { throw null; }
        public override object GetProviderSpecificValue(int ordinal) { throw null; }
        public override int GetProviderSpecificValues(object[] values) { throw null; }
        public override System.Data.DataTable GetSchemaTable() { throw null; }
        public override string GetString(int ordinal) { throw null; }
        public override object GetValue(int ordinal) { throw null; }
        public override int GetValues(object[] values) { throw null; }
        public override bool IsDBNull(int ordinal) { throw null; }
        public override bool NextResult() { throw null; }
        public override bool Read() { throw null; }
    }
    [System.ComponentModel.DesignerAttribute("Microsoft.VSDesigner.Data.VS.DataViewDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [System.ComponentModel.DefaultEventAttribute("PositionChanged")]
    [System.ComponentModel.DefaultPropertyAttribute("Table")]
    public partial class DataView : System.ComponentModel.MarshalByValueComponent, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.ComponentModel.IBindingList, System.ComponentModel.IBindingListView, System.ComponentModel.ISupportInitialize, System.ComponentModel.ISupportInitializeNotification, System.ComponentModel.ITypedList
    {
        public DataView() { }
        public DataView(System.Data.DataTable? table) { }
        public DataView(System.Data.DataTable table, string? RowFilter, string? Sort, System.Data.DataViewRowState RowState) { }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool AllowDelete { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool AllowEdit { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool AllowNew { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public bool ApplyDefaultSort { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public int Count { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.DataViewManager? DataViewManager { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsInitialized { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        protected bool IsOpen { get { throw null; } }
        public System.Data.DataRowView this[int recordIndex] { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public virtual string? RowFilter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.DataViewRowState.CurrentRows)]
        public System.Data.DataViewRowState RowStateFilter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        public string? Sort { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int recordIndex] { get { throw null; } set { } }
#nullable disable
        bool System.ComponentModel.IBindingList.AllowEdit { get { throw null; } }
        bool System.ComponentModel.IBindingList.AllowNew { get { throw null; } }
        bool System.ComponentModel.IBindingList.AllowRemove { get { throw null; } }
        bool System.ComponentModel.IBindingList.IsSorted { get { throw null; } }
        System.ComponentModel.ListSortDirection System.ComponentModel.IBindingList.SortDirection { get { throw null; } }
        System.ComponentModel.PropertyDescriptor System.ComponentModel.IBindingList.SortProperty { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsChangeNotification { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsSearching { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsSorting { get { throw null; } }
        string System.ComponentModel.IBindingListView.Filter { get { throw null; } set { } }
        System.ComponentModel.ListSortDescriptionCollection System.ComponentModel.IBindingListView.SortDescriptions { get { throw null; } }
        bool System.ComponentModel.IBindingListView.SupportsAdvancedSorting { get { throw null; } }
        bool System.ComponentModel.IBindingListView.SupportsFiltering { get { throw null; } }
#nullable enable
        public event System.EventHandler? Initialized { add { } remove { } }
        public event System.ComponentModel.ListChangedEventHandler? ListChanged { add { } remove { } }
        public virtual System.Data.DataRowView AddNew() { throw null; }
        public void BeginInit() { }
        protected void Close() { }
        protected virtual void ColumnCollectionChanged(object sender, System.ComponentModel.CollectionChangeEventArgs e) { }
        public void CopyTo(System.Array array, int index) { }
        public void Delete(int index) { }
        protected override void Dispose(bool disposing) { }
        public void EndInit() { }
        public virtual bool Equals(System.Data.DataView? view) { throw null; }
        public int Find(object? key) { throw null; }
        public int Find(object?[] key) { throw null; }
        public System.Data.DataRowView[] FindRows(object? key) { throw null; }
        public System.Data.DataRowView[] FindRows(object?[] key) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        protected virtual void IndexListChanged(object sender, System.ComponentModel.ListChangedEventArgs e) { }
        protected virtual void OnListChanged(System.ComponentModel.ListChangedEventArgs e) { }
        protected void Open() { }
        protected void Reset() { }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
#nullable disable
        void System.ComponentModel.IBindingList.AddIndex(System.ComponentModel.PropertyDescriptor property) { }
        object System.ComponentModel.IBindingList.AddNew() { throw null; }
        void System.ComponentModel.IBindingList.ApplySort(System.ComponentModel.PropertyDescriptor property, System.ComponentModel.ListSortDirection direction) { }
        int System.ComponentModel.IBindingList.Find(System.ComponentModel.PropertyDescriptor property, object key) { throw null; }
        void System.ComponentModel.IBindingList.RemoveIndex(System.ComponentModel.PropertyDescriptor property) { }
        void System.ComponentModel.IBindingList.RemoveSort() { }
        void System.ComponentModel.IBindingListView.ApplySort(System.ComponentModel.ListSortDescriptionCollection sorts) { }
        void System.ComponentModel.IBindingListView.RemoveFilter() { }
        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ITypedList.GetItemProperties(System.ComponentModel.PropertyDescriptor[] listAccessors) { throw null; }
        string System.ComponentModel.ITypedList.GetListName(System.ComponentModel.PropertyDescriptor[] listAccessors) { throw null; }
#nullable enable
        public System.Data.DataTable ToTable() { throw null; }
        public System.Data.DataTable ToTable(bool distinct, params string[] columnNames) { throw null; }
        public System.Data.DataTable ToTable(string? tableName) { throw null; }
        public System.Data.DataTable ToTable(string? tableName, bool distinct, params string[] columnNames) { throw null; }
        protected void UpdateIndex() { }
        protected virtual void UpdateIndex(bool force) { }
    }
    [System.ComponentModel.DesignerAttribute("Microsoft.VSDesigner.Data.VS.DataViewManagerDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial class DataViewManager : System.ComponentModel.MarshalByValueComponent, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.ComponentModel.IBindingList, System.ComponentModel.ITypedList
    {
        public DataViewManager() { }
        public DataViewManager(System.Data.DataSet? dataSet) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public System.Data.DataSet? DataSet { get { throw null; } set { } }
        public string DataViewSettingCollectionString { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Data.DataViewSettingCollection DataViewSettings { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
#nullable disable
        bool System.ComponentModel.IBindingList.AllowEdit { get { throw null; } }
        bool System.ComponentModel.IBindingList.AllowNew { get { throw null; } }
        bool System.ComponentModel.IBindingList.AllowRemove { get { throw null; } }
        bool System.ComponentModel.IBindingList.IsSorted { get { throw null; } }
        System.ComponentModel.ListSortDirection System.ComponentModel.IBindingList.SortDirection { get { throw null; } }
        System.ComponentModel.PropertyDescriptor System.ComponentModel.IBindingList.SortProperty { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsChangeNotification { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsSearching { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsSorting { get { throw null; } }
#nullable enable
        public event System.ComponentModel.ListChangedEventHandler? ListChanged { add { } remove { } }
        public System.Data.DataView CreateDataView(System.Data.DataTable table) { throw null; }
        protected virtual void OnListChanged(System.ComponentModel.ListChangedEventArgs e) { }
        protected virtual void RelationCollectionChanged(object sender, System.ComponentModel.CollectionChangeEventArgs e) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
#nullable disable
        void System.ComponentModel.IBindingList.AddIndex(System.ComponentModel.PropertyDescriptor property) { }
        object System.ComponentModel.IBindingList.AddNew() { throw null; }
        void System.ComponentModel.IBindingList.ApplySort(System.ComponentModel.PropertyDescriptor property, System.ComponentModel.ListSortDirection direction) { }
        int System.ComponentModel.IBindingList.Find(System.ComponentModel.PropertyDescriptor property, object key) { throw null; }
        void System.ComponentModel.IBindingList.RemoveIndex(System.ComponentModel.PropertyDescriptor property) { }
        void System.ComponentModel.IBindingList.RemoveSort() { }
        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ITypedList.GetItemProperties(System.ComponentModel.PropertyDescriptor[] listAccessors) { throw null; }
        string System.ComponentModel.ITypedList.GetListName(System.ComponentModel.PropertyDescriptor[] listAccessors) { throw null; }
#nullable enable
        protected virtual void TableCollectionChanged(object sender, System.ComponentModel.CollectionChangeEventArgs e) { }
    }
    [System.FlagsAttribute]
    public enum DataViewRowState
    {
        None = 0,
        Unchanged = 2,
        Added = 4,
        Deleted = 8,
        ModifiedCurrent = 16,
        CurrentRows = 22,
        ModifiedOriginal = 32,
        OriginalRows = 42,
    }
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ExpandableObjectConverter))]
    public partial class DataViewSetting
    {
        internal DataViewSetting() { }
        public bool ApplyDefaultSort { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.DataViewManager? DataViewManager { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string RowFilter { get { throw null; } set { } }
        public System.Data.DataViewRowState RowStateFilter { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Sort { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public System.Data.DataTable? Table { get { throw null; } }
    }
    public partial class DataViewSettingCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal DataViewSettingCollection() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public virtual int Count { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsReadOnly { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsSynchronized { get { throw null; } }
        public virtual System.Data.DataViewSetting this[System.Data.DataTable table] { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public virtual System.Data.DataViewSetting? this[int index] { get { throw null; } set { } }
        public virtual System.Data.DataViewSetting? this[string tableName] { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public object SyncRoot { get { throw null; } }
        public void CopyTo(System.Array ar, int index) { }
        public void CopyTo(System.Data.DataViewSetting[] ar, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
    }
    public sealed partial class DBConcurrencyException : System.SystemException
    {
        public DBConcurrencyException() { }
        public DBConcurrencyException(string? message) { }
        public DBConcurrencyException(string? message, System.Exception? inner) { }
        public DBConcurrencyException(string? message, System.Exception? inner, System.Data.DataRow[]? dataRows) { }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        public System.Data.DataRow? Row { get { throw null; } set { } }
        public int RowCount { get { throw null; } }
        public void CopyToRows(System.Data.DataRow[] array) { }
        public void CopyToRows(System.Data.DataRow[] array, int arrayIndex) { }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public enum DbType
    {
        AnsiString = 0,
        Binary = 1,
        Byte = 2,
        Boolean = 3,
        Currency = 4,
        Date = 5,
        DateTime = 6,
        Decimal = 7,
        Double = 8,
        Guid = 9,
        Int16 = 10,
        Int32 = 11,
        Int64 = 12,
        Object = 13,
        SByte = 14,
        Single = 15,
        String = 16,
        Time = 17,
        UInt16 = 18,
        UInt32 = 19,
        UInt64 = 20,
        VarNumeric = 21,
        AnsiStringFixedLength = 22,
        StringFixedLength = 23,
        Xml = 25,
        DateTime2 = 26,
        DateTimeOffset = 27,
    }
    public partial class DeletedRowInaccessibleException : System.Data.DataException
    {
        public DeletedRowInaccessibleException() { }
        protected DeletedRowInaccessibleException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DeletedRowInaccessibleException(string? s) { }
        public DeletedRowInaccessibleException(string? message, System.Exception? innerException) { }
    }
    public partial class DuplicateNameException : System.Data.DataException
    {
        public DuplicateNameException() { }
        protected DuplicateNameException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public DuplicateNameException(string? s) { }
        public DuplicateNameException(string? message, System.Exception? innerException) { }
    }
    public abstract partial class EnumerableRowCollection : System.Collections.IEnumerable
    {
        internal EnumerableRowCollection() { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public static partial class EnumerableRowCollectionExtensions
    {
        public static System.Data.EnumerableRowCollection<TResult> Cast<TResult>(this System.Data.EnumerableRowCollection source) { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this System.Data.EnumerableRowCollection<TRow> source, System.Func<TRow, TKey> keySelector) { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this System.Data.EnumerableRowCollection<TRow> source, System.Func<TRow, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this System.Data.EnumerableRowCollection<TRow> source, System.Func<TRow, TKey> keySelector) { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this System.Data.EnumerableRowCollection<TRow> source, System.Func<TRow, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Data.EnumerableRowCollection<S> Select<TRow, S>(this System.Data.EnumerableRowCollection<TRow> source, System.Func<TRow, S> selector) { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> ThenByDescending<TRow, TKey>(this System.Data.OrderedEnumerableRowCollection<TRow> source, System.Func<TRow, TKey> keySelector) { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> ThenByDescending<TRow, TKey>(this System.Data.OrderedEnumerableRowCollection<TRow> source, System.Func<TRow, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> ThenBy<TRow, TKey>(this System.Data.OrderedEnumerableRowCollection<TRow> source, System.Func<TRow, TKey> keySelector) { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> ThenBy<TRow, TKey>(this System.Data.OrderedEnumerableRowCollection<TRow> source, System.Func<TRow, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) { throw null; }
        public static System.Data.EnumerableRowCollection<TRow> Where<TRow>(this System.Data.EnumerableRowCollection<TRow> source, System.Func<TRow, bool> predicate) { throw null; }
    }
    public partial class EnumerableRowCollection<TRow> : System.Data.EnumerableRowCollection, System.Collections.Generic.IEnumerable<TRow>, System.Collections.IEnumerable
    {
        internal EnumerableRowCollection() { }
        public System.Collections.Generic.IEnumerator<TRow> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class EvaluateException : System.Data.InvalidExpressionException
    {
        public EvaluateException() { }
        protected EvaluateException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public EvaluateException(string? s) { }
        public EvaluateException(string? message, System.Exception? innerException) { }
    }
    public partial class FillErrorEventArgs : System.EventArgs
    {
        public FillErrorEventArgs(System.Data.DataTable? dataTable, object?[]? values) { }
        public bool Continue { get { throw null; } set { } }
        public System.Data.DataTable? DataTable { get { throw null; } }
        public System.Exception? Errors { get { throw null; } set { } }
        public object?[] Values { get { throw null; } }
    }
    public delegate void FillErrorEventHandler(object sender, System.Data.FillErrorEventArgs e);
    [System.ComponentModel.DefaultPropertyAttribute("ConstraintName")]
    public partial class ForeignKeyConstraint : System.Data.Constraint
    {
        public ForeignKeyConstraint(System.Data.DataColumn parentColumn, System.Data.DataColumn childColumn) { }
        public ForeignKeyConstraint(System.Data.DataColumn[] parentColumns, System.Data.DataColumn[] childColumns) { }
        public ForeignKeyConstraint(string? constraintName, System.Data.DataColumn parentColumn, System.Data.DataColumn childColumn) { }
        public ForeignKeyConstraint(string? constraintName, System.Data.DataColumn[] parentColumns, System.Data.DataColumn[] childColumns) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public ForeignKeyConstraint(string? constraintName, string? parentTableName, string? parentTableNamespace, string[] parentColumnNames, string[] childColumnNames, System.Data.AcceptRejectRule acceptRejectRule, System.Data.Rule deleteRule, System.Data.Rule updateRule) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public ForeignKeyConstraint(string? constraintName, string? parentTableName, string[] parentColumnNames, string[] childColumnNames, System.Data.AcceptRejectRule acceptRejectRule, System.Data.Rule deleteRule, System.Data.Rule updateRule) { }
        [System.ComponentModel.DefaultValueAttribute(System.Data.AcceptRejectRule.None)]
        public virtual System.Data.AcceptRejectRule AcceptRejectRule { get { throw null; } set { } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public virtual System.Data.DataColumn[] Columns { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.Rule.Cascade)]
        public virtual System.Data.Rule DeleteRule { get { throw null; } set { } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public virtual System.Data.DataColumn[] RelatedColumns { get { throw null; } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public virtual System.Data.DataTable RelatedTable { get { throw null; } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public override System.Data.DataTable? Table { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.Rule.Cascade)]
        public virtual System.Data.Rule UpdateRule { get { throw null; } set { } }
        public override bool Equals(object? key) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial interface IColumnMapping
    {
        string DataSetColumn { get; set; }
        string SourceColumn { get; set; }
    }
    public partial interface IColumnMappingCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        object this[string index] { get; set; }
        System.Data.IColumnMapping Add(string sourceColumnName, string dataSetColumnName);
        bool Contains(string? sourceColumnName);
        System.Data.IColumnMapping GetByDataSetColumn(string dataSetColumnName);
        int IndexOf(string? sourceColumnName);
        void RemoveAt(string sourceColumnName);
    }
    public partial interface IDataAdapter
    {
        System.Data.MissingMappingAction MissingMappingAction { get; set; }
        System.Data.MissingSchemaAction MissingSchemaAction { get; set; }
        System.Data.ITableMappingCollection TableMappings { get; }
        int Fill(System.Data.DataSet dataSet);
        System.Data.DataTable[] FillSchema(System.Data.DataSet dataSet, System.Data.SchemaType schemaType);
        System.Data.IDataParameter[] GetFillParameters();
        int Update(System.Data.DataSet dataSet);
    }
    public partial interface IDataParameter
    {
        System.Data.DbType DbType { get; set; }
        System.Data.ParameterDirection Direction { get; set; }
        bool IsNullable { get; }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        string ParameterName { get; set; }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        string SourceColumn { get; set; }
        System.Data.DataRowVersion SourceVersion { get; set; }
        object? Value { get; set; }
    }
    public partial interface IDataParameterCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        object this[string parameterName] { get; set; }
        bool Contains(string parameterName);
        int IndexOf(string parameterName);
        void RemoveAt(string parameterName);
    }
    public partial interface IDataReader : System.Data.IDataRecord, System.IDisposable
    {
        int Depth { get; }
        bool IsClosed { get; }
        int RecordsAffected { get; }
        void Close();
        System.Data.DataTable GetSchemaTable();
        bool NextResult();
        bool Read();
    }
    public partial interface IDataRecord
    {
        int FieldCount { get; }
        object this[int i] { get; }
        object this[string name] { get; }
        bool GetBoolean(int i);
        byte GetByte(int i);
        long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length);
        char GetChar(int i);
        long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length);
        System.Data.IDataReader GetData(int i);
        string GetDataTypeName(int i);
        System.DateTime GetDateTime(int i);
        decimal GetDecimal(int i);
        double GetDouble(int i);
        System.Type GetFieldType(int i);
        float GetFloat(int i);
        System.Guid GetGuid(int i);
        short GetInt16(int i);
        int GetInt32(int i);
        long GetInt64(int i);
        string GetName(int i);
        int GetOrdinal(string name);
        string GetString(int i);
        object GetValue(int i);
        int GetValues(object[] values);
        bool IsDBNull(int i);
    }
    public partial interface IDbCommand : System.IDisposable
    {
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        string CommandText { get; set; }
        int CommandTimeout { get; set; }
        System.Data.CommandType CommandType { get; set; }
        System.Data.IDbConnection? Connection { get; set; }
        System.Data.IDataParameterCollection Parameters { get; }
        System.Data.IDbTransaction? Transaction { get; set; }
        System.Data.UpdateRowSource UpdatedRowSource { get; set; }
        void Cancel();
        System.Data.IDbDataParameter CreateParameter();
        int ExecuteNonQuery();
        System.Data.IDataReader ExecuteReader();
        System.Data.IDataReader ExecuteReader(System.Data.CommandBehavior behavior);
        object? ExecuteScalar();
        void Prepare();
    }
    public partial interface IDbConnection : System.IDisposable
    {
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        string ConnectionString { get; set; }
        int ConnectionTimeout { get; }
        string Database { get; }
        System.Data.ConnectionState State { get; }
        System.Data.IDbTransaction BeginTransaction();
        System.Data.IDbTransaction BeginTransaction(System.Data.IsolationLevel il);
        void ChangeDatabase(string databaseName);
        void Close();
        System.Data.IDbCommand CreateCommand();
        void Open();
    }
    public partial interface IDbDataAdapter : System.Data.IDataAdapter
    {
        System.Data.IDbCommand? DeleteCommand { get; set; }
        System.Data.IDbCommand? InsertCommand { get; set; }
        System.Data.IDbCommand? SelectCommand { get; set; }
        System.Data.IDbCommand? UpdateCommand { get; set; }
    }
    public partial interface IDbDataParameter : System.Data.IDataParameter
    {
        byte Precision { get; set; }
        byte Scale { get; set; }
        int Size { get; set; }
    }
    public partial interface IDbTransaction : System.IDisposable
    {
        System.Data.IDbConnection? Connection { get; }
        System.Data.IsolationLevel IsolationLevel { get; }
        void Commit();
        void Rollback();
    }
    public partial class InRowChangingEventException : System.Data.DataException
    {
        public InRowChangingEventException() { }
        protected InRowChangingEventException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InRowChangingEventException(string? s) { }
        public InRowChangingEventException(string? message, System.Exception? innerException) { }
    }
    public partial class InternalDataCollectionBase : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public InternalDataCollectionBase() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public virtual int Count { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsReadOnly { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsSynchronized { get { throw null; } }
        protected virtual System.Collections.ArrayList List { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public object SyncRoot { get { throw null; } }
        public virtual void CopyTo(System.Array ar, int index) { }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
    }
    public partial class InvalidConstraintException : System.Data.DataException
    {
        public InvalidConstraintException() { }
        protected InvalidConstraintException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidConstraintException(string? s) { }
        public InvalidConstraintException(string? message, System.Exception? innerException) { }
    }
    public partial class InvalidExpressionException : System.Data.DataException
    {
        public InvalidExpressionException() { }
        protected InvalidExpressionException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidExpressionException(string? s) { }
        public InvalidExpressionException(string? message, System.Exception? innerException) { }
    }
    public enum IsolationLevel
    {
        Unspecified = -1,
        Chaos = 16,
        ReadUncommitted = 256,
        ReadCommitted = 4096,
        RepeatableRead = 65536,
        Serializable = 1048576,
        Snapshot = 16777216,
    }
    public partial interface ITableMapping
    {
        System.Data.IColumnMappingCollection ColumnMappings { get; }
        string DataSetTable { get; set; }
        string SourceTable { get; set; }
    }
    public partial interface ITableMappingCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        object this[string index] { get; set; }
        System.Data.ITableMapping Add(string sourceTableName, string dataSetTableName);
        bool Contains(string? sourceTableName);
        System.Data.ITableMapping GetByDataSetTable(string dataSetTableName);
        int IndexOf(string? sourceTableName);
        void RemoveAt(string sourceTableName);
    }
    public enum KeyRestrictionBehavior
    {
        AllowOnly = 0,
        PreventUsage = 1,
    }
    public enum LoadOption
    {
        OverwriteChanges = 1,
        PreserveChanges = 2,
        Upsert = 3,
    }
    public enum MappingType
    {
        Element = 1,
        Attribute = 2,
        SimpleContent = 3,
        Hidden = 4,
    }
    public partial class MergeFailedEventArgs : System.EventArgs
    {
        public MergeFailedEventArgs(System.Data.DataTable? table, string conflict) { }
        public string Conflict { get { throw null; } }
        public System.Data.DataTable? Table { get { throw null; } }
    }
    public delegate void MergeFailedEventHandler(object sender, System.Data.MergeFailedEventArgs e);
    public enum MissingMappingAction
    {
        Passthrough = 1,
        Ignore = 2,
        Error = 3,
    }
    public partial class MissingPrimaryKeyException : System.Data.DataException
    {
        public MissingPrimaryKeyException() { }
        protected MissingPrimaryKeyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public MissingPrimaryKeyException(string? s) { }
        public MissingPrimaryKeyException(string? message, System.Exception? innerException) { }
    }
    public enum MissingSchemaAction
    {
        Add = 1,
        Ignore = 2,
        Error = 3,
        AddWithKey = 4,
    }
    public partial class NoNullAllowedException : System.Data.DataException
    {
        public NoNullAllowedException() { }
        protected NoNullAllowedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public NoNullAllowedException(string? s) { }
        public NoNullAllowedException(string? message, System.Exception? innerException) { }
    }
    public sealed partial class OrderedEnumerableRowCollection<TRow> : System.Data.EnumerableRowCollection<TRow>
    {
        internal OrderedEnumerableRowCollection() { }
    }
    public enum ParameterDirection
    {
        Input = 1,
        Output = 2,
        InputOutput = 3,
        ReturnValue = 6,
    }
    public partial class PropertyCollection : System.Collections.Hashtable, System.ICloneable
    {
        public PropertyCollection() { }
        protected PropertyCollection(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public override object Clone() { throw null; }
    }
    public partial class ReadOnlyException : System.Data.DataException
    {
        public ReadOnlyException() { }
        protected ReadOnlyException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ReadOnlyException(string? s) { }
        public ReadOnlyException(string? message, System.Exception? innerException) { }
    }
    public partial class RowNotInTableException : System.Data.DataException
    {
        public RowNotInTableException() { }
        protected RowNotInTableException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public RowNotInTableException(string? s) { }
        public RowNotInTableException(string? message, System.Exception? innerException) { }
    }
    public enum Rule
    {
        None = 0,
        Cascade = 1,
        SetNull = 2,
        SetDefault = 3,
    }
    public enum SchemaSerializationMode
    {
        IncludeSchema = 1,
        ExcludeSchema = 2,
    }
    public enum SchemaType
    {
        Source = 1,
        Mapped = 2,
    }
    public enum SerializationFormat
    {
        Xml = 0,
        Binary = 1,
    }
    public enum SqlDbType
    {
        BigInt = 0,
        Binary = 1,
        Bit = 2,
        Char = 3,
        DateTime = 4,
        Decimal = 5,
        Float = 6,
        Image = 7,
        Int = 8,
        Money = 9,
        NChar = 10,
        NText = 11,
        NVarChar = 12,
        Real = 13,
        UniqueIdentifier = 14,
        SmallDateTime = 15,
        SmallInt = 16,
        SmallMoney = 17,
        Text = 18,
        Timestamp = 19,
        TinyInt = 20,
        VarBinary = 21,
        VarChar = 22,
        Variant = 23,
        Xml = 25,
        Udt = 29,
        Structured = 30,
        Date = 31,
        Time = 32,
        DateTime2 = 33,
        DateTimeOffset = 34,
    }
    public sealed partial class StateChangeEventArgs : System.EventArgs
    {
        public StateChangeEventArgs(System.Data.ConnectionState originalState, System.Data.ConnectionState currentState) { }
        public System.Data.ConnectionState CurrentState { get { throw null; } }
        public System.Data.ConnectionState OriginalState { get { throw null; } }
    }
    public delegate void StateChangeEventHandler(object sender, System.Data.StateChangeEventArgs e);
    public sealed partial class StatementCompletedEventArgs : System.EventArgs
    {
        public StatementCompletedEventArgs(int recordCount) { }
        public int RecordCount { get { throw null; } }
    }
    public delegate void StatementCompletedEventHandler(object sender, System.Data.StatementCompletedEventArgs e);
    public enum StatementType
    {
        Select = 0,
        Insert = 1,
        Update = 2,
        Delete = 3,
        Batch = 4,
    }
    public partial class StrongTypingException : System.Data.DataException
    {
        public StrongTypingException() { }
        protected StrongTypingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public StrongTypingException(string? message) { }
        public StrongTypingException(string? s, System.Exception? innerException) { }
    }
    public partial class SyntaxErrorException : System.Data.InvalidExpressionException
    {
        public SyntaxErrorException() { }
        protected SyntaxErrorException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public SyntaxErrorException(string? s) { }
        public SyntaxErrorException(string? message, System.Exception? innerException) { }
    }
    public static partial class TypedTableBaseExtensions
    {
        public static System.Data.EnumerableRowCollection<TRow> AsEnumerable<TRow>(this System.Data.TypedTableBase<TRow> source) where TRow : System.Data.DataRow { throw null; }
        public static TRow? ElementAtOrDefault<TRow>(this System.Data.TypedTableBase<TRow> source, int index) where TRow : System.Data.DataRow { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this System.Data.TypedTableBase<TRow> source, System.Func<TRow, TKey> keySelector) where TRow : System.Data.DataRow { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this System.Data.TypedTableBase<TRow> source, System.Func<TRow, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) where TRow : System.Data.DataRow { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this System.Data.TypedTableBase<TRow> source, System.Func<TRow, TKey> keySelector) where TRow : System.Data.DataRow { throw null; }
        public static System.Data.OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this System.Data.TypedTableBase<TRow> source, System.Func<TRow, TKey> keySelector, System.Collections.Generic.IComparer<TKey> comparer) where TRow : System.Data.DataRow { throw null; }
        public static System.Data.EnumerableRowCollection<S> Select<TRow, S>(this System.Data.TypedTableBase<TRow> source, System.Func<TRow, S> selector) where TRow : System.Data.DataRow { throw null; }
        public static System.Data.EnumerableRowCollection<TRow> Where<TRow>(this System.Data.TypedTableBase<TRow> source, System.Func<TRow, bool> predicate) where TRow : System.Data.DataRow { throw null; }
    }
    public abstract partial class TypedTableBase<T> : System.Data.DataTable, System.Collections.Generic.IEnumerable<T>, System.Collections.IEnumerable where T : System.Data.DataRow
    {
        protected TypedTableBase() { }
        protected TypedTableBase(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public System.Data.EnumerableRowCollection<TResult> Cast<TResult>() { throw null; }
        public System.Collections.Generic.IEnumerator<T> GetEnumerator() { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.ComponentModel.DefaultPropertyAttribute("ConstraintName")]
    public partial class UniqueConstraint : System.Data.Constraint
    {
        public UniqueConstraint(System.Data.DataColumn column) { }
        public UniqueConstraint(System.Data.DataColumn column, bool isPrimaryKey) { }
        public UniqueConstraint(System.Data.DataColumn[] columns) { }
        public UniqueConstraint(System.Data.DataColumn[] columns, bool isPrimaryKey) { }
        public UniqueConstraint(string? name, System.Data.DataColumn column) { }
        public UniqueConstraint(string? name, System.Data.DataColumn column, bool isPrimaryKey) { }
        public UniqueConstraint(string? name, System.Data.DataColumn[] columns) { }
        public UniqueConstraint(string? name, System.Data.DataColumn[] columns, bool isPrimaryKey) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        public UniqueConstraint(string name, string[]? columnNames, bool isPrimaryKey) { }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public virtual System.Data.DataColumn[] Columns { get { throw null; } }
        public bool IsPrimaryKey { get { throw null; } }
        [System.ComponentModel.ReadOnlyAttribute(true)]
        public override System.Data.DataTable? Table { get { throw null; } }
        public override bool Equals(object? key2) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public enum UpdateRowSource
    {
        None = 0,
        OutputParameters = 1,
        FirstReturnedRecord = 2,
        Both = 3,
    }
    public enum UpdateStatus
    {
        Continue = 0,
        ErrorsOccurred = 1,
        SkipCurrentRow = 2,
        SkipAllRemainingRows = 3,
    }
    public partial class VersionNotFoundException : System.Data.DataException
    {
        public VersionNotFoundException() { }
        protected VersionNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public VersionNotFoundException(string? s) { }
        public VersionNotFoundException(string? message, System.Exception? innerException) { }
    }
    public enum XmlReadMode
    {
        Auto = 0,
        ReadSchema = 1,
        IgnoreSchema = 2,
        InferSchema = 3,
        DiffGram = 4,
        Fragment = 5,
        InferTypedSchema = 6,
    }
    public enum XmlWriteMode
    {
        WriteSchema = 0,
        IgnoreSchema = 1,
        DiffGram = 2,
    }
}
namespace System.Data.Common
{
    public enum CatalogLocation
    {
        Start = 1,
        End = 2,
    }
    public partial class DataAdapter : System.ComponentModel.Component, System.Data.IDataAdapter
    {
        protected DataAdapter() { }
        protected DataAdapter(System.Data.Common.DataAdapter from) { }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool AcceptChangesDuringFill { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool AcceptChangesDuringUpdate { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool ContinueUpdateOnError { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public System.Data.LoadOption FillLoadOption { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.MissingMappingAction.Passthrough)]
        public System.Data.MissingMappingAction MissingMappingAction { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.MissingSchemaAction.Add)]
        public System.Data.MissingSchemaAction MissingSchemaAction { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public virtual bool ReturnProviderSpecificTypes { get { throw null; } set { } }
        System.Data.ITableMappingCollection System.Data.IDataAdapter.TableMappings { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Data.Common.DataTableMappingCollection TableMappings { get { throw null; } }
        public event System.Data.FillErrorEventHandler? FillError { add { } remove { } }
        [System.ObsoleteAttribute("CloneInternals() has been deprecated.  Use the DataAdapter(DataAdapter from) constructor.  https://go.microsoft.com/fwlink/?linkid=14202")]
        protected virtual System.Data.Common.DataAdapter CloneInternals() { throw null; }
        protected virtual System.Data.Common.DataTableMappingCollection CreateTableMappings() { throw null; }
        protected override void Dispose(bool disposing) { }
        public virtual int Fill(System.Data.DataSet dataSet) { throw null; }
        protected virtual int Fill(System.Data.DataSet dataSet, string srcTable, System.Data.IDataReader dataReader, int startRecord, int maxRecords) { throw null; }
        protected virtual int Fill(System.Data.DataTable dataTable, System.Data.IDataReader dataReader) { throw null; }
        protected virtual int Fill(System.Data.DataTable[] dataTables, System.Data.IDataReader dataReader, int startRecord, int maxRecords) { throw null; }
        public virtual System.Data.DataTable[] FillSchema(System.Data.DataSet dataSet, System.Data.SchemaType schemaType) { throw null; }
        protected virtual System.Data.DataTable[] FillSchema(System.Data.DataSet dataSet, System.Data.SchemaType schemaType, string srcTable, System.Data.IDataReader dataReader) { throw null; }
        protected virtual System.Data.DataTable? FillSchema(System.Data.DataTable dataTable, System.Data.SchemaType schemaType, System.Data.IDataReader dataReader) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public virtual System.Data.IDataParameter[] GetFillParameters() { throw null; }
        protected bool HasTableMappings() { throw null; }
        protected virtual void OnFillError(System.Data.FillErrorEventArgs value) { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public void ResetFillLoadOption() { }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeAcceptChangesDuringFill() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool ShouldSerializeFillLoadOption() { throw null; }
        protected virtual bool ShouldSerializeTableMappings() { throw null; }
        public virtual int Update(System.Data.DataSet dataSet) { throw null; }
    }
    public sealed partial class DataColumnMapping : System.MarshalByRefObject, System.Data.IColumnMapping, System.ICloneable
    {
        public DataColumnMapping() { }
        public DataColumnMapping(string? sourceColumn, string? dataSetColumn) { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataSetColumn { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string SourceColumn { get { throw null; } set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public System.Data.DataColumn? GetDataColumnBySchemaAction(System.Data.DataTable dataTable, System.Type? dataType, System.Data.MissingSchemaAction schemaAction) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Data.DataColumn GetDataColumnBySchemaAction(string? sourceColumn, string? dataSetColumn, System.Data.DataTable dataTable, System.Type? dataType, System.Data.MissingSchemaAction schemaAction) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class DataColumnMappingCollection : System.MarshalByRefObject, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Data.IColumnMappingCollection
    {
        public DataColumnMappingCollection() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Count { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DataColumnMapping this[int index] { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DataColumnMapping this[string sourceColumn] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        object System.Data.IColumnMappingCollection.this[string index] { get { throw null; } set { } }
        public int Add(object? value) { throw null; }
        public System.Data.Common.DataColumnMapping Add(string? sourceColumn, string? dataSetColumn) { throw null; }
        public void AddRange(System.Array values) { }
        public void AddRange(System.Data.Common.DataColumnMapping[] values) { }
        public void Clear() { }
        public bool Contains(object? value) { throw null; }
        public bool Contains(string? value) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.Common.DataColumnMapping[] array, int index) { }
        public System.Data.Common.DataColumnMapping GetByDataSetColumn(string value) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Data.Common.DataColumnMapping? GetColumnMappingBySchemaAction(System.Data.Common.DataColumnMappingCollection? columnMappings, string sourceColumn, System.Data.MissingMappingAction mappingAction) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Data.DataColumn? GetDataColumn(System.Data.Common.DataColumnMappingCollection? columnMappings, string sourceColumn, System.Type? dataType, System.Data.DataTable dataTable, System.Data.MissingMappingAction mappingAction, System.Data.MissingSchemaAction schemaAction) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(object? value) { throw null; }
        public int IndexOf(string? sourceColumn) { throw null; }
        public int IndexOfDataSetColumn(string? dataSetColumn) { throw null; }
        public void Insert(int index, System.Data.Common.DataColumnMapping value) { }
        public void Insert(int index, object? value) { }
        public void Remove(System.Data.Common.DataColumnMapping value) { }
        public void Remove(object? value) { }
        public void RemoveAt(int index) { }
        public void RemoveAt(string sourceColumn) { }
        System.Data.IColumnMapping System.Data.IColumnMappingCollection.Add(string? sourceColumnName, string? dataSetColumnName) { throw null; }
        System.Data.IColumnMapping System.Data.IColumnMappingCollection.GetByDataSetColumn(string dataSetColumnName) { throw null; }
    }
    public sealed partial class DataTableMapping : System.MarshalByRefObject, System.Data.ITableMapping, System.ICloneable
    {
        public DataTableMapping() { }
        public DataTableMapping(string? sourceTable, string? dataSetTable) { }
        public DataTableMapping(string? sourceTable, string? dataSetTable, System.Data.Common.DataColumnMapping[]? columnMappings) { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Data.Common.DataColumnMappingCollection ColumnMappings { get { throw null; } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataSetTable { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string SourceTable { get { throw null; } set { } }
        System.Data.IColumnMappingCollection System.Data.ITableMapping.ColumnMappings { get { throw null; } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public System.Data.Common.DataColumnMapping? GetColumnMappingBySchemaAction(string sourceColumn, System.Data.MissingMappingAction mappingAction) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public System.Data.DataColumn? GetDataColumn(string sourceColumn, System.Type? dataType, System.Data.DataTable dataTable, System.Data.MissingMappingAction mappingAction, System.Data.MissingSchemaAction schemaAction) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public System.Data.DataTable? GetDataTableBySchemaAction(System.Data.DataSet dataSet, System.Data.MissingSchemaAction schemaAction) { throw null; }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.ComponentModel.ListBindableAttribute(false)]
    public sealed partial class DataTableMappingCollection : System.MarshalByRefObject, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Data.ITableMappingCollection
    {
        public DataTableMappingCollection() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public int Count { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DataTableMapping this[int index] { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DataTableMapping this[string sourceTable] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        object System.Data.ITableMappingCollection.this[string index] { get { throw null; } set { } }
        public int Add(object? value) { throw null; }
        public System.Data.Common.DataTableMapping Add(string? sourceTable, string? dataSetTable) { throw null; }
        public void AddRange(System.Array values) { }
        public void AddRange(System.Data.Common.DataTableMapping[] values) { }
        public void Clear() { }
        public bool Contains(object? value) { throw null; }
        public bool Contains(string? value) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.Common.DataTableMapping[] array, int index) { }
        public System.Data.Common.DataTableMapping GetByDataSetTable(string dataSetTable) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Data.Common.DataTableMapping? GetTableMappingBySchemaAction(System.Data.Common.DataTableMappingCollection? tableMappings, string sourceTable, string dataSetTable, System.Data.MissingMappingAction mappingAction) { throw null; }
        public int IndexOf(object? value) { throw null; }
        public int IndexOf(string? sourceTable) { throw null; }
        public int IndexOfDataSetTable(string? dataSetTable) { throw null; }
        public void Insert(int index, System.Data.Common.DataTableMapping value) { }
        public void Insert(int index, object? value) { }
        public void Remove(System.Data.Common.DataTableMapping value) { }
        public void Remove(object? value) { }
        public void RemoveAt(int index) { }
        public void RemoveAt(string sourceTable) { }
        System.Data.ITableMapping System.Data.ITableMappingCollection.Add(string sourceTableName, string dataSetTableName) { throw null; }
        System.Data.ITableMapping System.Data.ITableMappingCollection.GetByDataSetTable(string dataSetTableName) { throw null; }
    }
    public abstract partial class DbColumn
    {
        protected DbColumn() { }
        public bool? AllowDBNull { get { throw null; } protected set { } }
        public string? BaseCatalogName { get { throw null; } protected set { } }
        public string? BaseColumnName { get { throw null; } protected set { } }
        public string? BaseSchemaName { get { throw null; } protected set { } }
        public string? BaseServerName { get { throw null; } protected set { } }
        public string? BaseTableName { get { throw null; } protected set { } }
        public string ColumnName { get { throw null; } protected set { } }
        public int? ColumnOrdinal { get { throw null; } protected set { } }
        public int? ColumnSize { get { throw null; } protected set { } }
        public System.Type? DataType { get { throw null; } protected set { } }
        public string? DataTypeName { get { throw null; } protected set { } }
        public bool? IsAliased { get { throw null; } protected set { } }
        public bool? IsAutoIncrement { get { throw null; } protected set { } }
        public bool? IsExpression { get { throw null; } protected set { } }
        public bool? IsHidden { get { throw null; } protected set { } }
        public bool? IsIdentity { get { throw null; } protected set { } }
        public bool? IsKey { get { throw null; } protected set { } }
        public bool? IsLong { get { throw null; } protected set { } }
        public bool? IsReadOnly { get { throw null; } protected set { } }
        public bool? IsUnique { get { throw null; } protected set { } }
        public virtual object? this[string property] { get { throw null; } }
        public int? NumericPrecision { get { throw null; } protected set { } }
        public int? NumericScale { get { throw null; } protected set { } }
        public string? UdtAssemblyQualifiedName { get { throw null; } protected set { } }
    }
    public abstract partial class DbCommand : System.ComponentModel.Component, System.Data.IDbCommand, System.IDisposable, System.IAsyncDisposable
    {
        protected DbCommand() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public abstract string CommandText { get; set; }
        public abstract int CommandTimeout { get; set; }
        [System.ComponentModel.DefaultValueAttribute(System.Data.CommandType.Text)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public abstract System.Data.CommandType CommandType { get; set; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DbConnection? Connection { get { throw null; } set { } }
        protected abstract System.Data.Common.DbConnection? DbConnection { get; set; }
        protected abstract System.Data.Common.DbParameterCollection DbParameterCollection { get; }
        protected abstract System.Data.Common.DbTransaction? DbTransaction { get; set; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(true)]
        [System.ComponentModel.DesignOnlyAttribute(true)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public abstract bool DesignTimeVisible { get; set; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DbParameterCollection Parameters { get { throw null; } }
        System.Data.IDbConnection? System.Data.IDbCommand.Connection { get { throw null; } set { } }
        System.Data.IDataParameterCollection System.Data.IDbCommand.Parameters { get { throw null; } }
        System.Data.IDbTransaction? System.Data.IDbCommand.Transaction { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DbTransaction? Transaction { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.UpdateRowSource.Both)]
        public abstract System.Data.UpdateRowSource UpdatedRowSource { get; set; }
        public abstract void Cancel();
        protected abstract System.Data.Common.DbParameter CreateDbParameter();
        public System.Data.Common.DbParameter CreateParameter() { throw null; }
        public virtual System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        protected abstract System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior);
        protected virtual System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteDbDataReaderAsync(System.Data.CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract int ExecuteNonQuery();
        public System.Threading.Tasks.Task<int> ExecuteNonQueryAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<int> ExecuteNonQueryAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Data.Common.DbDataReader ExecuteReader() { throw null; }
        public System.Data.Common.DbDataReader ExecuteReader(System.Data.CommandBehavior behavior) { throw null; }
        public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync() { throw null; }
        public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(System.Data.CommandBehavior behavior) { throw null; }
        public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(System.Data.CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract object? ExecuteScalar();
        public System.Threading.Tasks.Task<object?> ExecuteScalarAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<object?> ExecuteScalarAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract void Prepare();
        public virtual System.Threading.Tasks.Task PrepareAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        System.Data.IDbDataParameter System.Data.IDbCommand.CreateParameter() { throw null; }
        System.Data.IDataReader System.Data.IDbCommand.ExecuteReader() { throw null; }
        System.Data.IDataReader System.Data.IDbCommand.ExecuteReader(System.Data.CommandBehavior behavior) { throw null; }
    }
    public abstract partial class DbCommandBuilder : System.ComponentModel.Component
    {
        protected DbCommandBuilder() { }
        [System.ComponentModel.DefaultValueAttribute(System.Data.Common.CatalogLocation.Start)]
        public virtual System.Data.Common.CatalogLocation CatalogLocation { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(".")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public virtual string CatalogSeparator { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.ConflictOption.CompareAllSearchableValues)]
        public virtual System.Data.ConflictOption ConflictOption { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DbDataAdapter? DataAdapter { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public virtual string QuotePrefix { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public virtual string QuoteSuffix { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(".")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public virtual string SchemaSeparator { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool SetAllValues { get { throw null; } set { } }
        protected abstract void ApplyParameterInfo(System.Data.Common.DbParameter parameter, System.Data.DataRow row, System.Data.StatementType statementType, bool whereClause);
        protected override void Dispose(bool disposing) { }
        public System.Data.Common.DbCommand GetDeleteCommand() { throw null; }
        public System.Data.Common.DbCommand GetDeleteCommand(bool useColumnsForParameterNames) { throw null; }
        public System.Data.Common.DbCommand GetInsertCommand() { throw null; }
        public System.Data.Common.DbCommand GetInsertCommand(bool useColumnsForParameterNames) { throw null; }
        protected abstract string GetParameterName(int parameterOrdinal);
        protected abstract string GetParameterName(string parameterName);
        protected abstract string GetParameterPlaceholder(int parameterOrdinal);
        protected virtual System.Data.DataTable GetSchemaTable(System.Data.Common.DbCommand sourceCommand) { throw null; }
        public System.Data.Common.DbCommand GetUpdateCommand() { throw null; }
        public System.Data.Common.DbCommand GetUpdateCommand(bool useColumnsForParameterNames) { throw null; }
        protected virtual System.Data.Common.DbCommand InitializeCommand(System.Data.Common.DbCommand? command) { throw null; }
        public virtual string QuoteIdentifier(string unquotedIdentifier) { throw null; }
        public virtual void RefreshSchema() { }
        protected void RowUpdatingHandler(System.Data.Common.RowUpdatingEventArgs rowUpdatingEvent) { }
        protected abstract void SetRowUpdatingHandler(System.Data.Common.DbDataAdapter adapter);
        public virtual string UnquoteIdentifier(string quotedIdentifier) { throw null; }
    }
    public abstract partial class DbConnection : System.ComponentModel.Component, System.Data.IDbConnection, System.IDisposable, System.IAsyncDisposable
    {
        protected DbConnection() { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.RecommendedAsConfigurableAttribute(true)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.ComponentModel.SettingsBindableAttribute(true)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public abstract string ConnectionString { get; set; }
        public virtual int ConnectionTimeout { get { throw null; } }
        public abstract string Database { get; }
        public abstract string DataSource { get; }
        protected virtual System.Data.Common.DbProviderFactory? DbProviderFactory { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public abstract string ServerVersion { get; }
        [System.ComponentModel.BrowsableAttribute(false)]
        public abstract System.Data.ConnectionState State { get; }
        public virtual event System.Data.StateChangeEventHandler? StateChange { add { } remove { } }
        protected abstract System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel);
        protected virtual System.Threading.Tasks.ValueTask<System.Data.Common.DbTransaction> BeginDbTransactionAsync(System.Data.IsolationLevel isolationLevel, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Data.Common.DbTransaction BeginTransaction() { throw null; }
        public System.Data.Common.DbTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel) { throw null; }
        public System.Threading.Tasks.ValueTask<System.Data.Common.DbTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public System.Threading.Tasks.ValueTask<System.Data.Common.DbTransaction> BeginTransactionAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public abstract void ChangeDatabase(string databaseName);
        public virtual System.Threading.Tasks.Task ChangeDatabaseAsync(string databaseName, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public abstract void Close();
        public virtual System.Threading.Tasks.Task CloseAsync() { throw null; }
        public System.Data.Common.DbCommand CreateCommand() { throw null; }
        protected abstract System.Data.Common.DbCommand CreateDbCommand();
        public virtual System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public virtual void EnlistTransaction(System.Transactions.Transaction? transaction) { }
        public virtual System.Data.DataTable GetSchema() { throw null; }
        public virtual System.Data.DataTable GetSchema(string collectionName) { throw null; }
        public virtual System.Data.DataTable GetSchema(string collectionName, string?[] restrictionValues) { throw null; }
        public virtual System.Threading.Tasks.Task<System.Data.DataTable> GetSchemaAsync(System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public virtual System.Threading.Tasks.Task<System.Data.DataTable> GetSchemaAsync(string collectionName, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public virtual System.Threading.Tasks.Task<System.Data.DataTable> GetSchemaAsync(string collectionName, string?[] restrictionValues, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        protected virtual void OnStateChange(System.Data.StateChangeEventArgs stateChange) { }
        public abstract void Open();
        public System.Threading.Tasks.Task OpenAsync() { throw null; }
        public virtual System.Threading.Tasks.Task OpenAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        System.Data.IDbTransaction System.Data.IDbConnection.BeginTransaction() { throw null; }
        System.Data.IDbTransaction System.Data.IDbConnection.BeginTransaction(System.Data.IsolationLevel isolationLevel) { throw null; }
        System.Data.IDbCommand System.Data.IDbConnection.CreateCommand() { throw null; }
    }
    public partial class DbConnectionStringBuilder : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.ComponentModel.ICustomTypeDescriptor
    {
        public DbConnectionStringBuilder() { }
        public DbConnectionStringBuilder(bool useOdbcRules) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.DesignOnlyAttribute(true)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool BrowsableConnectionString { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string ConnectionString { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public virtual int Count { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public virtual bool IsFixedSize { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public bool IsReadOnly { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public virtual object this[string keyword] { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public virtual System.Collections.ICollection Keys { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        object? System.Collections.IDictionary.this[object keyword] { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        public virtual System.Collections.ICollection Values { get { throw null; } }
        public void Add(string keyword, object value) { }
        public static void AppendKeyValuePair(System.Text.StringBuilder builder, string keyword, string? value) { }
        public static void AppendKeyValuePair(System.Text.StringBuilder builder, string keyword, string? value, bool useOdbcRules) { }
        public virtual void Clear() { }
        protected internal void ClearPropertyDescriptors() { }
        public virtual bool ContainsKey(string keyword) { throw null; }
        public virtual bool EquivalentTo(System.Data.Common.DbConnectionStringBuilder connectionStringBuilder) { throw null; }
        protected virtual void GetProperties(System.Collections.Hashtable propertyDescriptors) { }
        public virtual bool Remove(string keyword) { throw null; }
        public virtual bool ShouldSerialize(string keyword) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object keyword, object? value) { }
        bool System.Collections.IDictionary.Contains(object keyword) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object keyword) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        System.ComponentModel.AttributeCollection System.ComponentModel.ICustomTypeDescriptor.GetAttributes() { throw null; }
        string System.ComponentModel.ICustomTypeDescriptor.GetClassName() { throw null; }
        string System.ComponentModel.ICustomTypeDescriptor.GetComponentName() { throw null; }
        System.ComponentModel.TypeConverter System.ComponentModel.ICustomTypeDescriptor.GetConverter() { throw null; }
        System.ComponentModel.EventDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent() { throw null; }
        System.ComponentModel.PropertyDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty() { throw null; }
        object System.ComponentModel.ICustomTypeDescriptor.GetEditor(System.Type editorBaseType) { throw null; }
        System.ComponentModel.EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents() { throw null; }
        System.ComponentModel.EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents(System.Attribute[] attributes) { throw null; }
        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties() { throw null; }
        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties(System.Attribute[] attributes) { throw null; }
        object System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd) { throw null; }
        public override string ToString() { throw null; }
        public virtual bool TryGetValue(string keyword, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out object? value) { throw null; }
    }
    public abstract partial class DbDataAdapter : System.Data.Common.DataAdapter, System.Data.IDataAdapter, System.Data.IDbDataAdapter, System.ICloneable
    {
        public const string DefaultSourceTableName = "Table";
        protected DbDataAdapter() { }
        protected DbDataAdapter(System.Data.Common.DbDataAdapter adapter) { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DbCommand? DeleteCommand { get { throw null; } set { } }
        protected internal System.Data.CommandBehavior FillCommandBehavior { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DbCommand? InsertCommand { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DbCommand? SelectCommand { get { throw null; } set { } }
        System.Data.IDbCommand? System.Data.IDbDataAdapter.DeleteCommand { get { throw null; } set { } }
        System.Data.IDbCommand? System.Data.IDbDataAdapter.InsertCommand { get { throw null; } set { } }
        System.Data.IDbCommand? System.Data.IDbDataAdapter.SelectCommand { get { throw null; } set { } }
        System.Data.IDbCommand? System.Data.IDbDataAdapter.UpdateCommand { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(1)]
        public virtual int UpdateBatchSize { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public System.Data.Common.DbCommand? UpdateCommand { get { throw null; } set { } }
        protected virtual int AddToBatch(System.Data.IDbCommand command) { throw null; }
        protected virtual void ClearBatch() { }
        protected virtual System.Data.Common.RowUpdatedEventArgs CreateRowUpdatedEvent(System.Data.DataRow dataRow, System.Data.IDbCommand? command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) { throw null; }
        protected virtual System.Data.Common.RowUpdatingEventArgs CreateRowUpdatingEvent(System.Data.DataRow dataRow, System.Data.IDbCommand? command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected virtual int ExecuteBatch() { throw null; }
        public override int Fill(System.Data.DataSet dataSet) { throw null; }
        public int Fill(System.Data.DataSet dataSet, int startRecord, int maxRecords, string srcTable) { throw null; }
        protected virtual int Fill(System.Data.DataSet dataSet, int startRecord, int maxRecords, string srcTable, System.Data.IDbCommand command, System.Data.CommandBehavior behavior) { throw null; }
        public int Fill(System.Data.DataSet dataSet, string srcTable) { throw null; }
        public int Fill(System.Data.DataTable dataTable) { throw null; }
        protected virtual int Fill(System.Data.DataTable dataTable, System.Data.IDbCommand command, System.Data.CommandBehavior behavior) { throw null; }
        protected virtual int Fill(System.Data.DataTable[] dataTables, int startRecord, int maxRecords, System.Data.IDbCommand command, System.Data.CommandBehavior behavior) { throw null; }
        public int Fill(int startRecord, int maxRecords, params System.Data.DataTable[] dataTables) { throw null; }
        public override System.Data.DataTable[] FillSchema(System.Data.DataSet dataSet, System.Data.SchemaType schemaType) { throw null; }
        protected virtual System.Data.DataTable[] FillSchema(System.Data.DataSet dataSet, System.Data.SchemaType schemaType, System.Data.IDbCommand command, string srcTable, System.Data.CommandBehavior behavior) { throw null; }
        public System.Data.DataTable[] FillSchema(System.Data.DataSet dataSet, System.Data.SchemaType schemaType, string srcTable) { throw null; }
        public System.Data.DataTable? FillSchema(System.Data.DataTable dataTable, System.Data.SchemaType schemaType) { throw null; }
        protected virtual System.Data.DataTable? FillSchema(System.Data.DataTable dataTable, System.Data.SchemaType schemaType, System.Data.IDbCommand command, System.Data.CommandBehavior behavior) { throw null; }
        protected virtual System.Data.IDataParameter GetBatchedParameter(int commandIdentifier, int parameterIndex) { throw null; }
        protected virtual bool GetBatchedRecordsAffected(int commandIdentifier, out int recordsAffected, out System.Exception? error) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public override System.Data.IDataParameter[] GetFillParameters() { throw null; }
        protected virtual void InitializeBatching() { }
        protected virtual void OnRowUpdated(System.Data.Common.RowUpdatedEventArgs value) { }
        protected virtual void OnRowUpdating(System.Data.Common.RowUpdatingEventArgs value) { }
        object System.ICloneable.Clone() { throw null; }
        protected virtual void TerminateBatching() { }
        public int Update(System.Data.DataRow[] dataRows) { throw null; }
        protected virtual int Update(System.Data.DataRow[] dataRows, System.Data.Common.DataTableMapping tableMapping) { throw null; }
        public override int Update(System.Data.DataSet dataSet) { throw null; }
        public int Update(System.Data.DataSet dataSet, string srcTable) { throw null; }
        public int Update(System.Data.DataTable dataTable) { throw null; }
    }
    public abstract partial class DbDataReader : System.MarshalByRefObject, System.Collections.IEnumerable, System.Data.IDataReader, System.Data.IDataRecord, System.IDisposable, System.IAsyncDisposable
    {
        protected DbDataReader() { }
        public abstract int Depth { get; }
        public abstract int FieldCount { get; }
        public abstract bool HasRows { get; }
        public abstract bool IsClosed { get; }
        public abstract object this[int ordinal] { get; }
        public abstract object this[string name] { get; }
        public abstract int RecordsAffected { get; }
        public virtual int VisibleFieldCount { get { throw null; } }
        public virtual void Close() { }
        public virtual System.Threading.Tasks.Task CloseAsync() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public abstract bool GetBoolean(int ordinal);
        public abstract byte GetByte(int ordinal);
        public abstract long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length);
        public abstract char GetChar(int ordinal);
        public abstract long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length);
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public System.Data.Common.DbDataReader GetData(int ordinal) { throw null; }
        public abstract string GetDataTypeName(int ordinal);
        public abstract System.DateTime GetDateTime(int ordinal);
        protected virtual System.Data.Common.DbDataReader GetDbDataReader(int ordinal) { throw null; }
        public abstract decimal GetDecimal(int ordinal);
        public abstract double GetDouble(int ordinal);
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public abstract System.Collections.IEnumerator GetEnumerator();
        public abstract System.Type GetFieldType(int ordinal);
        public System.Threading.Tasks.Task<T> GetFieldValueAsync<T>(int ordinal) { throw null; }
        public virtual System.Threading.Tasks.Task<T> GetFieldValueAsync<T>(int ordinal, System.Threading.CancellationToken cancellationToken) { throw null; }
        public virtual T GetFieldValue<T>(int ordinal) { throw null; }
        public abstract float GetFloat(int ordinal);
        public abstract System.Guid GetGuid(int ordinal);
        public abstract short GetInt16(int ordinal);
        public abstract int GetInt32(int ordinal);
        public abstract long GetInt64(int ordinal);
        public abstract string GetName(int ordinal);
        public abstract int GetOrdinal(string name);
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual System.Type GetProviderSpecificFieldType(int ordinal) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual object GetProviderSpecificValue(int ordinal) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual int GetProviderSpecificValues(object[] values) { throw null; }
        public virtual System.Data.DataTable GetSchemaTable() { throw null; }
        public virtual System.Threading.Tasks.Task<System.Data.DataTable> GetSchemaTableAsync(System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public virtual System.Threading.Tasks.Task<System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Common.DbColumn>> GetColumnSchemaAsync(System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public virtual System.IO.Stream GetStream(int ordinal) { throw null; }
        public abstract string GetString(int ordinal);
        public virtual System.IO.TextReader GetTextReader(int ordinal) { throw null; }
        public abstract object GetValue(int ordinal);
        public abstract int GetValues(object[] values);
        public abstract bool IsDBNull(int ordinal);
        public System.Threading.Tasks.Task<bool> IsDBNullAsync(int ordinal) { throw null; }
        public virtual System.Threading.Tasks.Task<bool> IsDBNullAsync(int ordinal, System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract bool NextResult();
        public System.Threading.Tasks.Task<bool> NextResultAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<bool> NextResultAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public abstract bool Read();
        public System.Threading.Tasks.Task<bool> ReadAsync() { throw null; }
        public virtual System.Threading.Tasks.Task<bool> ReadAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        System.Data.IDataReader System.Data.IDataRecord.GetData(int ordinal) { throw null; }
    }
    public static partial class DbDataReaderExtensions
    {
        public static bool CanGetColumnSchema(this System.Data.Common.DbDataReader reader) { throw null; }
        public static System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Common.DbColumn> GetColumnSchema(this System.Data.Common.DbDataReader reader) { throw null; }
    }
    public abstract partial class DbDataRecord : System.ComponentModel.ICustomTypeDescriptor, System.Data.IDataRecord
    {
        protected DbDataRecord() { }
        public abstract int FieldCount { get; }
        public abstract object this[int i] { get; }
        public abstract object this[string name] { get; }
        public abstract bool GetBoolean(int i);
        public abstract byte GetByte(int i);
        public abstract long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length);
        public abstract char GetChar(int i);
        public abstract long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length);
        public System.Data.IDataReader GetData(int i) { throw null; }
        public abstract string GetDataTypeName(int i);
        public abstract System.DateTime GetDateTime(int i);
        protected virtual System.Data.Common.DbDataReader GetDbDataReader(int i) { throw null; }
        public abstract decimal GetDecimal(int i);
        public abstract double GetDouble(int i);
        public abstract System.Type GetFieldType(int i);
        public abstract float GetFloat(int i);
        public abstract System.Guid GetGuid(int i);
        public abstract short GetInt16(int i);
        public abstract int GetInt32(int i);
        public abstract long GetInt64(int i);
        public abstract string GetName(int i);
        public abstract int GetOrdinal(string name);
        public abstract string GetString(int i);
        public abstract object GetValue(int i);
        public abstract int GetValues(object[] values);
        public abstract bool IsDBNull(int i);
#nullable disable
        System.ComponentModel.AttributeCollection System.ComponentModel.ICustomTypeDescriptor.GetAttributes() { throw null; }
        string System.ComponentModel.ICustomTypeDescriptor.GetClassName() { throw null; }
        string System.ComponentModel.ICustomTypeDescriptor.GetComponentName() { throw null; }
        System.ComponentModel.TypeConverter System.ComponentModel.ICustomTypeDescriptor.GetConverter() { throw null; }
        System.ComponentModel.EventDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultEvent() { throw null; }
        System.ComponentModel.PropertyDescriptor System.ComponentModel.ICustomTypeDescriptor.GetDefaultProperty() { throw null; }
        object System.ComponentModel.ICustomTypeDescriptor.GetEditor(System.Type editorBaseType) { throw null; }
        System.ComponentModel.EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents() { throw null; }
        System.ComponentModel.EventDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetEvents(System.Attribute[] attributes) { throw null; }
        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties() { throw null; }
        System.ComponentModel.PropertyDescriptorCollection System.ComponentModel.ICustomTypeDescriptor.GetProperties(System.Attribute[] attributes) { throw null; }
        object System.ComponentModel.ICustomTypeDescriptor.GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd) { throw null; }
#nullable enable
    }
    public abstract partial class DbDataSourceEnumerator
    {
        protected DbDataSourceEnumerator() { }
        public abstract System.Data.DataTable GetDataSources();
    }
    public partial class DbEnumerator : System.Collections.IEnumerator
    {
        public DbEnumerator(System.Data.Common.DbDataReader reader) { }
        public DbEnumerator(System.Data.Common.DbDataReader reader, bool closeReader) { }
        public DbEnumerator(System.Data.IDataReader reader) { }
        public DbEnumerator(System.Data.IDataReader reader, bool closeReader) { }
        public object Current { get { throw null; } }
        public bool MoveNext() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public void Reset() { }
    }
    public abstract partial class DbException : System.Runtime.InteropServices.ExternalException
    {
        protected DbException() { }
        protected DbException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        protected DbException(string? message) { }
        protected DbException(string? message, System.Exception? innerException) { }
        protected DbException(string? message, int errorCode) { }
        public virtual bool IsTransient { get { throw null; } }
        public virtual string? SqlState { get { throw null; } }
    }
    public static partial class DbMetaDataCollectionNames
    {
        public static readonly string DataSourceInformation;
        public static readonly string DataTypes;
        public static readonly string MetaDataCollections;
        public static readonly string ReservedWords;
        public static readonly string Restrictions;
    }
    public static partial class DbMetaDataColumnNames
    {
        public static readonly string CollectionName;
        public static readonly string ColumnSize;
        public static readonly string CompositeIdentifierSeparatorPattern;
        public static readonly string CreateFormat;
        public static readonly string CreateParameters;
        public static readonly string DataSourceProductName;
        public static readonly string DataSourceProductVersion;
        public static readonly string DataSourceProductVersionNormalized;
        public static readonly string DataType;
        public static readonly string GroupByBehavior;
        public static readonly string IdentifierCase;
        public static readonly string IdentifierPattern;
        public static readonly string IsAutoIncrementable;
        public static readonly string IsBestMatch;
        public static readonly string IsCaseSensitive;
        public static readonly string IsConcurrencyType;
        public static readonly string IsFixedLength;
        public static readonly string IsFixedPrecisionScale;
        public static readonly string IsLiteralSupported;
        public static readonly string IsLong;
        public static readonly string IsNullable;
        public static readonly string IsSearchable;
        public static readonly string IsSearchableWithLike;
        public static readonly string IsUnsigned;
        public static readonly string LiteralPrefix;
        public static readonly string LiteralSuffix;
        public static readonly string MaximumScale;
        public static readonly string MinimumScale;
        public static readonly string NumberOfIdentifierParts;
        public static readonly string NumberOfRestrictions;
        public static readonly string OrderByColumnsInSelect;
        public static readonly string ParameterMarkerFormat;
        public static readonly string ParameterMarkerPattern;
        public static readonly string ParameterNameMaxLength;
        public static readonly string ParameterNamePattern;
        public static readonly string ProviderDbType;
        public static readonly string QuotedIdentifierCase;
        public static readonly string QuotedIdentifierPattern;
        public static readonly string ReservedWord;
        public static readonly string StatementSeparatorPattern;
        public static readonly string StringLiteralPattern;
        public static readonly string SupportedJoinOperators;
        public static readonly string TypeName;
    }
    public abstract partial class DbParameter : System.MarshalByRefObject, System.Data.IDataParameter, System.Data.IDbDataParameter
    {
        protected DbParameter() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public abstract System.Data.DbType DbType { get; set; }
        [System.ComponentModel.DefaultValueAttribute(System.Data.ParameterDirection.Input)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public abstract System.Data.ParameterDirection Direction { get; set; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignOnlyAttribute(true)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public abstract bool IsNullable { get; set; }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public abstract string ParameterName { get; set; }
        public virtual byte Precision { get { throw null; } set { } }
        public virtual byte Scale { get { throw null; } set { } }
        public abstract int Size { get; set; }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public abstract string SourceColumn { get; set; }
        [System.ComponentModel.DefaultValueAttribute(false)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public abstract bool SourceColumnNullMapping { get; set; }
        [System.ComponentModel.DefaultValueAttribute(System.Data.DataRowVersion.Current)]
        public virtual System.Data.DataRowVersion SourceVersion { get { throw null; } set { } }
        byte System.Data.IDbDataParameter.Precision { get { throw null; } set { } }
        byte System.Data.IDbDataParameter.Scale { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public abstract object? Value { get; set; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public abstract void ResetDbType();
    }
    public abstract partial class DbParameterCollection : System.MarshalByRefObject, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Data.IDataParameterCollection
    {
        protected DbParameterCollection() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public abstract int Count { get; }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool IsFixedSize { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool IsReadOnly { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool IsSynchronized { get { throw null; } }
        public System.Data.Common.DbParameter this[int index] { get { throw null; } set { } }
        public System.Data.Common.DbParameter this[string parameterName] { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public abstract object SyncRoot { get; }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        object System.Data.IDataParameterCollection.this[string parameterName] { get { throw null; } set { } }
        int System.Collections.IList.Add(object? value) { throw null; }
        public abstract int Add(object value);
        public abstract void AddRange(System.Array values);
        public abstract void Clear();
        bool System.Collections.IList.Contains(object? value) { throw null; }
        public abstract bool Contains(object value);
        public abstract bool Contains(string value);
        public abstract void CopyTo(System.Array array, int index);
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public abstract System.Collections.IEnumerator GetEnumerator();
        protected abstract System.Data.Common.DbParameter GetParameter(int index);
        protected abstract System.Data.Common.DbParameter GetParameter(string parameterName);
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        public abstract int IndexOf(object value);
        public abstract int IndexOf(string parameterName);
        void System.Collections.IList.Insert(int index, object? value) { throw null; }
        public abstract void Insert(int index, object value);
        void System.Collections.IList.Remove(object? value) { throw null; }
        public abstract void Remove(object value);
        public abstract void RemoveAt(int index);
        public abstract void RemoveAt(string parameterName);
        protected abstract void SetParameter(int index, System.Data.Common.DbParameter value);
        protected abstract void SetParameter(string parameterName, System.Data.Common.DbParameter value);
    }
    public static partial class DbProviderFactories
    {
        public static System.Data.Common.DbProviderFactory? GetFactory(System.Data.Common.DbConnection connection) { throw null; }
        public static System.Data.Common.DbProviderFactory GetFactory(System.Data.DataRow providerRow) { throw null; }
        public static System.Data.Common.DbProviderFactory GetFactory(string providerInvariantName) { throw null; }
        public static System.Data.DataTable GetFactoryClasses() { throw null; }
        public static System.Collections.Generic.IEnumerable<string> GetProviderInvariantNames() { throw null; }
        public static void RegisterFactory(string providerInvariantName, System.Data.Common.DbProviderFactory factory) { }
        public static void RegisterFactory(string providerInvariantName, string factoryTypeAssemblyQualifiedName) { }
        public static void RegisterFactory(string providerInvariantName, System.Type providerFactoryClass) { }
        public static bool TryGetFactory(string providerInvariantName, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Data.Common.DbProviderFactory? factory) { throw null; }
        public static bool UnregisterFactory(string providerInvariantName) { throw null; }
    }
    public abstract partial class DbProviderFactory
    {
        protected DbProviderFactory() { }
        public virtual bool CanCreateCommandBuilder { get { throw null; } }
        public virtual bool CanCreateDataAdapter { get { throw null; } }
        public virtual bool CanCreateDataSourceEnumerator { get { throw null; } }
        public virtual System.Data.Common.DbCommand? CreateCommand() { throw null; }
        public virtual System.Data.Common.DbCommandBuilder? CreateCommandBuilder() { throw null; }
        public virtual System.Data.Common.DbConnection? CreateConnection() { throw null; }
        public virtual System.Data.Common.DbConnectionStringBuilder? CreateConnectionStringBuilder() { throw null; }
        public virtual System.Data.Common.DbDataAdapter? CreateDataAdapter() { throw null; }
        public virtual System.Data.Common.DbDataSourceEnumerator? CreateDataSourceEnumerator() { throw null; }
        public virtual System.Data.Common.DbParameter? CreateParameter() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property, AllowMultiple=false, Inherited=true)]
    public sealed partial class DbProviderSpecificTypePropertyAttribute : System.Attribute
    {
        public DbProviderSpecificTypePropertyAttribute(bool isProviderSpecificTypeProperty) { }
        public bool IsProviderSpecificTypeProperty { get { throw null; } }
    }
    public abstract partial class DbTransaction : System.MarshalByRefObject, System.Data.IDbTransaction, System.IDisposable, System.IAsyncDisposable
    {
        protected DbTransaction() { }
        public System.Data.Common.DbConnection? Connection { get { throw null; } }
        protected abstract System.Data.Common.DbConnection? DbConnection { get; }
        public abstract System.Data.IsolationLevel IsolationLevel { get; }
        System.Data.IDbConnection? System.Data.IDbTransaction.Connection { get { throw null; } }
        public abstract void Commit();
        public virtual System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual System.Threading.Tasks.ValueTask DisposeAsync() { throw null; }
        public abstract void Rollback();
        public virtual System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken)) { throw null; }
        public virtual bool SupportsSavepoints { get { throw null; } }
        public virtual System.Threading.Tasks.Task SaveAsync(string savepointName, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public virtual System.Threading.Tasks.Task RollbackAsync(string savepointName, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public virtual System.Threading.Tasks.Task ReleaseAsync(string savepointName, System.Threading.CancellationToken cancellationToken = default) { throw null; }
        public virtual void Save(string savepointName) { throw null; }
        public virtual void Rollback(string savepointName) { throw null; }
        public virtual void Release(string savepointName) { throw null; }
    }
    public enum GroupByBehavior
    {
        Unknown = 0,
        NotSupported = 1,
        Unrelated = 2,
        MustContainAll = 3,
        ExactMatch = 4,
    }
    public partial interface IDbColumnSchemaGenerator
    {
        System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Common.DbColumn> GetColumnSchema();
    }
    public enum IdentifierCase
    {
        Unknown = 0,
        Insensitive = 1,
        Sensitive = 2,
    }
    public partial class RowUpdatedEventArgs : System.EventArgs
    {
        public RowUpdatedEventArgs(System.Data.DataRow dataRow, System.Data.IDbCommand? command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) { }
        public System.Data.IDbCommand? Command { get { throw null; } }
        public System.Exception? Errors { get { throw null; } set { } }
        public int RecordsAffected { get { throw null; } }
        public System.Data.DataRow Row { get { throw null; } }
        public int RowCount { get { throw null; } }
        public System.Data.StatementType StatementType { get { throw null; } }
        public System.Data.UpdateStatus Status { get { throw null; } set { } }
        public System.Data.Common.DataTableMapping TableMapping { get { throw null; } }
        public void CopyToRows(System.Data.DataRow[] array) { }
        public void CopyToRows(System.Data.DataRow[] array, int arrayIndex) { }
    }
    public partial class RowUpdatingEventArgs : System.EventArgs
    {
        public RowUpdatingEventArgs(System.Data.DataRow dataRow, System.Data.IDbCommand? command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) { }
        protected virtual System.Data.IDbCommand? BaseCommand { get { throw null; } set { } }
        public System.Data.IDbCommand? Command { get { throw null; } set { } }
        public System.Exception? Errors { get { throw null; } set { } }
        public System.Data.DataRow Row { get { throw null; } }
        public System.Data.StatementType StatementType { get { throw null; } }
        public System.Data.UpdateStatus Status { get { throw null; } set { } }
        public System.Data.Common.DataTableMapping TableMapping { get { throw null; } }
    }
    public static partial class SchemaTableColumn
    {
        public static readonly string AllowDBNull;
        public static readonly string BaseColumnName;
        public static readonly string BaseSchemaName;
        public static readonly string BaseTableName;
        public static readonly string ColumnName;
        public static readonly string ColumnOrdinal;
        public static readonly string ColumnSize;
        public static readonly string DataType;
        public static readonly string IsAliased;
        public static readonly string IsExpression;
        public static readonly string IsKey;
        public static readonly string IsLong;
        public static readonly string IsUnique;
        public static readonly string NonVersionedProviderType;
        public static readonly string NumericPrecision;
        public static readonly string NumericScale;
        public static readonly string ProviderType;
    }
    public static partial class SchemaTableOptionalColumn
    {
        public static readonly string AutoIncrementSeed;
        public static readonly string AutoIncrementStep;
        public static readonly string BaseCatalogName;
        public static readonly string BaseColumnNamespace;
        public static readonly string BaseServerName;
        public static readonly string BaseTableNamespace;
        public static readonly string ColumnMapping;
        public static readonly string DefaultValue;
        public static readonly string Expression;
        public static readonly string IsAutoIncrement;
        public static readonly string IsHidden;
        public static readonly string IsReadOnly;
        public static readonly string IsRowVersion;
        public static readonly string ProviderSpecificDataType;
    }
    [System.FlagsAttribute]
    public enum SupportedJoinOperators
    {
        None = 0,
        Inner = 1,
        LeftOuter = 2,
        RightOuter = 4,
        FullOuter = 8,
    }
}
namespace System.Data.SqlTypes
{
    public partial interface INullable
    {
        bool IsNull { get; }
    }
    public sealed partial class SqlAlreadyFilledException : System.Data.SqlTypes.SqlTypeException
    {
        public SqlAlreadyFilledException() { }
        public SqlAlreadyFilledException(string message) { }
        public SqlAlreadyFilledException(string message, System.Exception e) { }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlBinary : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlBinary Null;
        public SqlBinary(byte[]? value) { throw null; }
        public bool IsNull { get { throw null; } }
        public byte this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public byte[] Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlBinary Add(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlBinary value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlBinary Concat(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBinary operator +(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static explicit operator byte[]? (System.Data.SqlTypes.SqlBinary x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBinary (System.Data.SqlTypes.SqlGuid x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlBinary (byte[] x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlBinary x, System.Data.SqlTypes.SqlBinary y) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlGuid ToSqlGuid() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlBoolean : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlBoolean False;
        public static readonly System.Data.SqlTypes.SqlBoolean Null;
        public static readonly System.Data.SqlTypes.SqlBoolean One;
        public static readonly System.Data.SqlTypes.SqlBoolean True;
        public static readonly System.Data.SqlTypes.SqlBoolean Zero;
        public SqlBoolean(bool value) { throw null; }
        public SqlBoolean(int value) { throw null; }
        public byte ByteValue { get { throw null; } }
        public bool IsFalse { get { throw null; } }
        public bool IsNull { get { throw null; } }
        public bool IsTrue { get { throw null; } }
        public bool Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlBoolean And(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlBoolean value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEquals(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEquals(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean OnesComplement(System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator &(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator |(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ^(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static explicit operator bool (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBoolean (System.Data.SqlTypes.SqlString x) { throw null; }
        public static bool operator false(System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlBoolean (bool x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !(System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ~(System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static bool operator true(System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Or(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Parse(string s) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Xor(System.Data.SqlTypes.SqlBoolean x, System.Data.SqlTypes.SqlBoolean y) { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlByte : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlByte MaxValue;
        public static readonly System.Data.SqlTypes.SqlByte MinValue;
        public static readonly System.Data.SqlTypes.SqlByte Null;
        public static readonly System.Data.SqlTypes.SqlByte Zero;
        public SqlByte(byte value) { throw null; }
        public bool IsNull { get { throw null; } }
        public byte Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlByte Add(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte BitwiseAnd(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte BitwiseOr(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlByte value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlByte Divide(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte Mod(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte Modulus(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte Multiply(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte OnesComplement(System.Data.SqlTypes.SqlByte x) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator +(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator &(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator |(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator /(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator ^(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator byte (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlByte (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlByte (byte x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator %(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator *(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator ~(System.Data.SqlTypes.SqlByte x) { throw null; }
        public static System.Data.SqlTypes.SqlByte operator -(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        public static System.Data.SqlTypes.SqlByte Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlByte Subtract(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
        System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
        public static System.Data.SqlTypes.SqlByte Xor(System.Data.SqlTypes.SqlByte x, System.Data.SqlTypes.SqlByte y) { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public sealed partial class SqlBytes : System.Data.SqlTypes.INullable, System.Runtime.Serialization.ISerializable, System.Xml.Serialization.IXmlSerializable
    {
        public SqlBytes() { }
        public SqlBytes(byte[]? buffer) { }
        public SqlBytes(System.Data.SqlTypes.SqlBinary value) { }
        public SqlBytes(System.IO.Stream? s) { }
        public byte[]? Buffer { get { throw null; } }
        public bool IsNull { get { throw null; } }
        public byte this[long offset] { get { throw null; } set { } }
        public long Length { get { throw null; } }
        public long MaxLength { get { throw null; } }
        public static System.Data.SqlTypes.SqlBytes Null { get { throw null; } }
        public System.Data.SqlTypes.StorageState Storage { get { throw null; } }
        public System.IO.Stream Stream { get { throw null; } set { } }
        public byte[] Value { get { throw null; } }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBytes (System.Data.SqlTypes.SqlBinary value) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlBinary (System.Data.SqlTypes.SqlBytes value) { throw null; }
        public long Read(long offset, byte[] buffer, int offsetInBuffer, int count) { throw null; }
        public void SetLength(long value) { }
        public void SetNull() { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader r) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlBinary ToSqlBinary() { throw null; }
        public void Write(long offset, byte[] buffer, int offsetInBuffer, int count) { }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public sealed partial class SqlChars : System.Data.SqlTypes.INullable, System.Runtime.Serialization.ISerializable, System.Xml.Serialization.IXmlSerializable
    {
        public SqlChars() { }
        public SqlChars(char[]? buffer) { }
        public SqlChars(System.Data.SqlTypes.SqlString value) { }
        public char[]? Buffer { get { throw null; } }
        public bool IsNull { get { throw null; } }
        public char this[long offset] { get { throw null; } set { } }
        public long Length { get { throw null; } }
        public long MaxLength { get { throw null; } }
        public static System.Data.SqlTypes.SqlChars Null { get { throw null; } }
        public System.Data.SqlTypes.StorageState Storage { get { throw null; } }
        public char[] Value { get { throw null; } }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlChars value) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlChars (System.Data.SqlTypes.SqlString value) { throw null; }
        public long Read(long offset, char[] buffer, int offsetInBuffer, int count) { throw null; }
        public void SetLength(long value) { }
        public void SetNull() { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader r) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public void Write(long offset, char[] buffer, int offsetInBuffer, int count) { }
    }
    [System.FlagsAttribute]
    public enum SqlCompareOptions
    {
        None = 0,
        IgnoreCase = 1,
        IgnoreNonSpace = 2,
        IgnoreKanaType = 8,
        IgnoreWidth = 16,
        BinarySort2 = 16384,
        BinarySort = 32768,
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlDateTime : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlDateTime MaxValue;
        public static readonly System.Data.SqlTypes.SqlDateTime MinValue;
        public static readonly System.Data.SqlTypes.SqlDateTime Null;
        public static readonly int SQLTicksPerHour;
        public static readonly int SQLTicksPerMinute;
        public static readonly int SQLTicksPerSecond;
        public SqlDateTime(System.DateTime value) { throw null; }
        public SqlDateTime(int dayTicks, int timeTicks) { throw null; }
        public SqlDateTime(int year, int month, int day) { throw null; }
        public SqlDateTime(int year, int month, int day, int hour, int minute, int second) { throw null; }
        public SqlDateTime(int year, int month, int day, int hour, int minute, int second, double millisecond) { throw null; }
        public SqlDateTime(int year, int month, int day, int hour, int minute, int second, int bilisecond) { throw null; }
        public int DayTicks { get { throw null; } }
        public bool IsNull { get { throw null; } }
        public int TimeTicks { get { throw null; } }
        public System.DateTime Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlDateTime Add(System.Data.SqlTypes.SqlDateTime x, System.TimeSpan t) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlDateTime value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlDateTime operator +(System.Data.SqlTypes.SqlDateTime x, System.TimeSpan t) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static explicit operator System.DateTime (System.Data.SqlTypes.SqlDateTime x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlDateTime (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDateTime (System.DateTime value) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlDateTime x, System.Data.SqlTypes.SqlDateTime y) { throw null; }
        public static System.Data.SqlTypes.SqlDateTime operator -(System.Data.SqlTypes.SqlDateTime x, System.TimeSpan t) { throw null; }
        public static System.Data.SqlTypes.SqlDateTime Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlDateTime Subtract(System.Data.SqlTypes.SqlDateTime x, System.TimeSpan t) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlDecimal : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly byte MaxPrecision;
        public static readonly byte MaxScale;
        public static readonly System.Data.SqlTypes.SqlDecimal MaxValue;
        public static readonly System.Data.SqlTypes.SqlDecimal MinValue;
        public static readonly System.Data.SqlTypes.SqlDecimal Null;
        public SqlDecimal(byte bPrecision, byte bScale, bool fPositive, int data1, int data2, int data3, int data4) { throw null; }
        public SqlDecimal(byte bPrecision, byte bScale, bool fPositive, int[] bits) { throw null; }
        public SqlDecimal(decimal value) { throw null; }
        public SqlDecimal(double dVal) { throw null; }
        public SqlDecimal(int value) { throw null; }
        public SqlDecimal(long value) { throw null; }
        public byte[] BinData { get { throw null; } }
        public int[] Data { get { throw null; } }
        public bool IsNull { get { throw null; } }
        public bool IsPositive { get { throw null; } }
        public byte Precision { get { throw null; } }
        public byte Scale { get { throw null; } }
        public decimal Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlDecimal Abs(System.Data.SqlTypes.SqlDecimal n) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Add(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal AdjustScale(System.Data.SqlTypes.SqlDecimal n, int digits, bool fRound) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Ceiling(System.Data.SqlTypes.SqlDecimal n) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlDecimal value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal ConvertToPrecScale(System.Data.SqlTypes.SqlDecimal n, int precision, int scale) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Divide(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Floor(System.Data.SqlTypes.SqlDecimal n) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Multiply(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal operator +(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal operator /(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator decimal (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlString x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlDecimal (double x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDecimal (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDecimal (decimal x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDecimal (long x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal operator *(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal operator -(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal operator -(System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Power(System.Data.SqlTypes.SqlDecimal n, double exp) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Round(System.Data.SqlTypes.SqlDecimal n, int position) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 Sign(System.Data.SqlTypes.SqlDecimal n) { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Subtract(System.Data.SqlTypes.SqlDecimal x, System.Data.SqlTypes.SqlDecimal y) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public double ToDouble() { throw null; }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
        public static System.Data.SqlTypes.SqlDecimal Truncate(System.Data.SqlTypes.SqlDecimal n, int position) { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlDouble : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlDouble MaxValue;
        public static readonly System.Data.SqlTypes.SqlDouble MinValue;
        public static readonly System.Data.SqlTypes.SqlDouble Null;
        public static readonly System.Data.SqlTypes.SqlDouble Zero;
        public SqlDouble(double value) { throw null; }
        public bool IsNull { get { throw null; } }
        public double Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlDouble Add(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlDouble value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlDouble Divide(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlDouble Multiply(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlDouble operator +(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlDouble operator /(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator double (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDouble (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlDouble (double x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlDouble operator *(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlDouble operator -(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        public static System.Data.SqlTypes.SqlDouble operator -(System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static System.Data.SqlTypes.SqlDouble Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlDouble Subtract(System.Data.SqlTypes.SqlDouble x, System.Data.SqlTypes.SqlDouble y) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlGuid : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlGuid Null;
        public SqlGuid(byte[] value) { throw null; }
        public SqlGuid(System.Guid g) { throw null; }
        public SqlGuid(int a, short b, short c, byte d, byte e, byte f, byte g, byte h, byte i, byte j, byte k) { throw null; }
        public SqlGuid(string s) { throw null; }
        public bool IsNull { get { throw null; } }
        public System.Guid Value { get { throw null; } }
        public int CompareTo(System.Data.SqlTypes.SqlGuid value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlGuid (System.Data.SqlTypes.SqlBinary x) { throw null; }
        public static explicit operator System.Guid (System.Data.SqlTypes.SqlGuid x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlGuid (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlGuid (System.Guid x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlGuid x, System.Data.SqlTypes.SqlGuid y) { throw null; }
        public static System.Data.SqlTypes.SqlGuid Parse(string s) { throw null; }
        System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public byte[]? ToByteArray() { throw null; }
        public System.Data.SqlTypes.SqlBinary ToSqlBinary() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlInt16 : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlInt16 MaxValue;
        public static readonly System.Data.SqlTypes.SqlInt16 MinValue;
        public static readonly System.Data.SqlTypes.SqlInt16 Null;
        public static readonly System.Data.SqlTypes.SqlInt16 Zero;
        public SqlInt16(short value) { throw null; }
        public bool IsNull { get { throw null; } }
        public short Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlInt16 Add(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 BitwiseAnd(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 BitwiseOr(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlInt16 value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 Divide(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 Mod(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 Modulus(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 Multiply(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 OnesComplement(System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator +(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator &(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator |(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator /(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator ^(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator short (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt16 (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt16 (short x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator %(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator *(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator ~(System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator -(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 operator -(System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlInt16 Subtract(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
        public static System.Data.SqlTypes.SqlInt16 Xor(System.Data.SqlTypes.SqlInt16 x, System.Data.SqlTypes.SqlInt16 y) { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlInt32 : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlInt32 MaxValue;
        public static readonly System.Data.SqlTypes.SqlInt32 MinValue;
        public static readonly System.Data.SqlTypes.SqlInt32 Null;
        public static readonly System.Data.SqlTypes.SqlInt32 Zero;
        public SqlInt32(int value) { throw null; }
        public bool IsNull { get { throw null; } }
        public int Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlInt32 Add(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 BitwiseAnd(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 BitwiseOr(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlInt32 value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 Divide(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 Mod(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 Modulus(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 Multiply(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 OnesComplement(System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator +(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator &(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator |(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator /(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator ^(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator int (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt32 (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt32 (int x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator %(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator *(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator ~(System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator -(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 operator -(System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlInt32 Subtract(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
        public static System.Data.SqlTypes.SqlInt32 Xor(System.Data.SqlTypes.SqlInt32 x, System.Data.SqlTypes.SqlInt32 y) { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlInt64 : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlInt64 MaxValue;
        public static readonly System.Data.SqlTypes.SqlInt64 MinValue;
        public static readonly System.Data.SqlTypes.SqlInt64 Null;
        public static readonly System.Data.SqlTypes.SqlInt64 Zero;
        public SqlInt64(long value) { throw null; }
        public bool IsNull { get { throw null; } }
        public long Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlInt64 Add(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 BitwiseAnd(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 BitwiseOr(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlInt64 value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 Divide(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 Mod(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 Modulus(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 Multiply(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 OnesComplement(System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator +(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator &(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator |(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator /(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator ^(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator long (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt64 (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlInt64 (long x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator %(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator *(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator ~(System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator -(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 operator -(System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlInt64 Subtract(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
        public static System.Data.SqlTypes.SqlInt64 Xor(System.Data.SqlTypes.SqlInt64 x, System.Data.SqlTypes.SqlInt64 y) { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlMoney : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlMoney MaxValue;
        public static readonly System.Data.SqlTypes.SqlMoney MinValue;
        public static readonly System.Data.SqlTypes.SqlMoney Null;
        public static readonly System.Data.SqlTypes.SqlMoney Zero;
        public SqlMoney(decimal value) { throw null; }
        public SqlMoney(double value) { throw null; }
        public SqlMoney(int value) { throw null; }
        public SqlMoney(long value) { throw null; }
        public bool IsNull { get { throw null; } }
        public decimal Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlMoney Add(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlMoney value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlMoney Divide(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlMoney Multiply(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlMoney operator +(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlMoney operator /(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator decimal (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlString x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlMoney (double x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlMoney (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlMoney (decimal x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlMoney (long x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlMoney operator *(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlMoney operator -(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        public static System.Data.SqlTypes.SqlMoney operator -(System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static System.Data.SqlTypes.SqlMoney Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlMoney Subtract(System.Data.SqlTypes.SqlMoney x, System.Data.SqlTypes.SqlMoney y) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public decimal ToDecimal() { throw null; }
        public double ToDouble() { throw null; }
        public int ToInt32() { throw null; }
        public long ToInt64() { throw null; }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class SqlNotFilledException : System.Data.SqlTypes.SqlTypeException
    {
        public SqlNotFilledException() { }
        public SqlNotFilledException(string? message) { }
        public SqlNotFilledException(string? message, System.Exception? e) { }
    }
    public sealed partial class SqlNullValueException : System.Data.SqlTypes.SqlTypeException
    {
        public SqlNullValueException() { }
        public SqlNullValueException(string? message) { }
        public SqlNullValueException(string? message, System.Exception? e) { }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlSingle : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private int _dummyPrimitive;
        public static readonly System.Data.SqlTypes.SqlSingle MaxValue;
        public static readonly System.Data.SqlTypes.SqlSingle MinValue;
        public static readonly System.Data.SqlTypes.SqlSingle Null;
        public static readonly System.Data.SqlTypes.SqlSingle Zero;
        public SqlSingle(double value) { throw null; }
        public SqlSingle(float value) { throw null; }
        public bool IsNull { get { throw null; } }
        public float Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlSingle Add(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlSingle value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlSingle Divide(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlSingle Multiply(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlSingle operator +(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlSingle operator /(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator float (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlSingle (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlSingle (float x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlSingle operator *(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlSingle operator -(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        public static System.Data.SqlTypes.SqlSingle operator -(System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static System.Data.SqlTypes.SqlSingle Parse(string s) { throw null; }
        public static System.Data.SqlTypes.SqlSingle Subtract(System.Data.SqlTypes.SqlSingle x, System.Data.SqlTypes.SqlSingle y) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlString ToSqlString() { throw null; }
        public override string ToString() { throw null; }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public partial struct SqlString : System.Data.SqlTypes.INullable, System.IComparable, System.Xml.Serialization.IXmlSerializable
    {
        private object _dummy;
        private int _dummyPrimitive;
        public static readonly int BinarySort;
        public static readonly int BinarySort2;
        public static readonly int IgnoreCase;
        public static readonly int IgnoreKanaType;
        public static readonly int IgnoreNonSpace;
        public static readonly int IgnoreWidth;
        public static readonly System.Data.SqlTypes.SqlString Null;
        public SqlString(int lcid, System.Data.SqlTypes.SqlCompareOptions compareOptions, byte[] data) { throw null; }
        public SqlString(int lcid, System.Data.SqlTypes.SqlCompareOptions compareOptions, byte[] data, bool fUnicode) { throw null; }
        public SqlString(int lcid, System.Data.SqlTypes.SqlCompareOptions compareOptions, byte[]? data, int index, int count) { throw null; }
        public SqlString(int lcid, System.Data.SqlTypes.SqlCompareOptions compareOptions, byte[]? data, int index, int count, bool fUnicode) { throw null; }
        public SqlString(string? data) { throw null; }
        public SqlString(string? data, int lcid) { throw null; }
        public SqlString(string? data, int lcid, System.Data.SqlTypes.SqlCompareOptions compareOptions) { throw null; }
        public System.Globalization.CompareInfo CompareInfo { get { throw null; } }
        public System.Globalization.CultureInfo CultureInfo { get { throw null; } }
        public bool IsNull { get { throw null; } }
        public int LCID { get { throw null; } }
        public System.Data.SqlTypes.SqlCompareOptions SqlCompareOptions { get { throw null; } }
        public string Value { get { throw null; } }
        public static System.Data.SqlTypes.SqlString Add(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public System.Data.SqlTypes.SqlString Clone() { throw null; }
        public static System.Globalization.CompareOptions CompareOptionsFromSqlCompareOptions(System.Data.SqlTypes.SqlCompareOptions compareOptions) { throw null; }
        public int CompareTo(System.Data.SqlTypes.SqlString value) { throw null; }
        public int CompareTo(object? value) { throw null; }
        public static System.Data.SqlTypes.SqlString Concat(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean Equals(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public byte[]? GetNonUnicodeBytes() { throw null; }
        public byte[]? GetUnicodeBytes() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThan(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean GreaterThanOrEqual(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThan(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean LessThanOrEqual(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean NotEquals(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlString operator +(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator ==(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlBoolean x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlByte x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlDateTime x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlDecimal x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlDouble x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlGuid x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlInt16 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlInt32 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlInt64 x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlMoney x) { throw null; }
        public static explicit operator System.Data.SqlTypes.SqlString (System.Data.SqlTypes.SqlSingle x) { throw null; }
        public static explicit operator string (System.Data.SqlTypes.SqlString x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator >=(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static implicit operator System.Data.SqlTypes.SqlString (string x) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator !=(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        public static System.Data.SqlTypes.SqlBoolean operator <=(System.Data.SqlTypes.SqlString x, System.Data.SqlTypes.SqlString y) { throw null; }
        System.Xml.Schema.XmlSchema System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader reader) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
        public System.Data.SqlTypes.SqlBoolean ToSqlBoolean() { throw null; }
        public System.Data.SqlTypes.SqlByte ToSqlByte() { throw null; }
        public System.Data.SqlTypes.SqlDateTime ToSqlDateTime() { throw null; }
        public System.Data.SqlTypes.SqlDecimal ToSqlDecimal() { throw null; }
        public System.Data.SqlTypes.SqlDouble ToSqlDouble() { throw null; }
        public System.Data.SqlTypes.SqlGuid ToSqlGuid() { throw null; }
        public System.Data.SqlTypes.SqlInt16 ToSqlInt16() { throw null; }
        public System.Data.SqlTypes.SqlInt32 ToSqlInt32() { throw null; }
        public System.Data.SqlTypes.SqlInt64 ToSqlInt64() { throw null; }
        public System.Data.SqlTypes.SqlMoney ToSqlMoney() { throw null; }
        public System.Data.SqlTypes.SqlSingle ToSqlSingle() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class SqlTruncateException : System.Data.SqlTypes.SqlTypeException
    {
        public SqlTruncateException() { }
        public SqlTruncateException(string? message) { }
        public SqlTruncateException(string? message, System.Exception? e) { }
    }
    public partial class SqlTypeException : System.SystemException
    {
        public SqlTypeException() { }
        protected SqlTypeException(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext sc) { }
        public SqlTypeException(string? message) { }
        public SqlTypeException(string? message, System.Exception? e) { }
    }
    [System.Xml.Serialization.XmlSchemaProviderAttribute("GetXsdType")]
    public sealed partial class SqlXml : System.Data.SqlTypes.INullable, System.Xml.Serialization.IXmlSerializable
    {
        public SqlXml() { }
        public SqlXml(System.IO.Stream? value) { }
        public SqlXml(System.Xml.XmlReader? value) { }
        public bool IsNull { get { throw null; } }
        public static System.Data.SqlTypes.SqlXml Null { get { throw null; } }
        public string Value { get { throw null; } }
        public System.Xml.XmlReader CreateReader() { throw null; }
        public static System.Xml.XmlQualifiedName GetXsdType(System.Xml.Schema.XmlSchemaSet schemaSet) { throw null; }
        System.Xml.Schema.XmlSchema? System.Xml.Serialization.IXmlSerializable.GetSchema() { throw null; }
        void System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader r) { }
        void System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) { }
    }
    public enum StorageState
    {
        Buffer = 0,
        Stream = 1,
        UnmanagedBuffer = 2,
    }
}
#nullable disable
namespace System.Xml
{
    [System.ObsoleteAttribute("XmlDataDocument class will be removed in a future release.")]
    public partial class XmlDataDocument : System.Xml.XmlDocument
    {
        public XmlDataDocument() { }
        public XmlDataDocument(System.Data.DataSet dataset) { }
        public System.Data.DataSet DataSet { get { throw null; } }
        public override System.Xml.XmlNode CloneNode(bool deep) { throw null; }
        public override System.Xml.XmlElement CreateElement(string prefix, string localName, string namespaceURI) { throw null; }
        public override System.Xml.XmlEntityReference CreateEntityReference(string name) { throw null; }
        protected override System.Xml.XPath.XPathNavigator CreateNavigator(System.Xml.XmlNode node) { throw null; }
        public override System.Xml.XmlElement GetElementById(string elemId) { throw null; }
        public System.Xml.XmlElement GetElementFromRow(System.Data.DataRow r) { throw null; }
        public override System.Xml.XmlNodeList GetElementsByTagName(string name) { throw null; }
        public System.Data.DataRow GetRowFromElement(System.Xml.XmlElement e) { throw null; }
        public override void Load(System.IO.Stream inStream) { }
        public override void Load(System.IO.TextReader txtReader) { }
        public override void Load(string filename) { }
        public override void Load(System.Xml.XmlReader reader) { }
    }
}
#nullable enable
