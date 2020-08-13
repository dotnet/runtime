// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Data.OleDb
{
    [System.ComponentModel.DesignerAttribute("Microsoft.VSDesigner.Data.VS.OleDbCommandDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed partial class OleDbCommand : System.Data.Common.DbCommand, System.Data.IDbCommand, System.ICloneable, System.IDisposable
    {
        public OleDbCommand() { }
        public OleDbCommand(string? cmdText) { }
        public OleDbCommand(string? cmdText, System.Data.OleDb.OleDbConnection? connection) { }
        public OleDbCommand(string? cmdText, System.Data.OleDb.OleDbConnection? connection, System.Data.OleDb.OleDbTransaction? transaction) { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public override string? CommandText { get { throw null; } set { } }
        public override int CommandTimeout { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.CommandType.Text)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public override System.Data.CommandType CommandType { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public new System.Data.OleDb.OleDbConnection? Connection { get { throw null; } set { } }
        protected override System.Data.Common.DbConnection? DbConnection { get { throw null; } set { } }
        protected override System.Data.Common.DbParameterCollection DbParameterCollection { get { throw null; } }
        protected override System.Data.Common.DbTransaction? DbTransaction { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(true)]
        [System.ComponentModel.DesignOnlyAttribute(true)]
        public override bool DesignTimeVisible { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public new System.Data.OleDb.OleDbParameterCollection Parameters { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public new System.Data.OleDb.OleDbTransaction? Transaction { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(System.Data.UpdateRowSource.Both)]
        public override System.Data.UpdateRowSource UpdatedRowSource { get { throw null; } set { } }
        public override void Cancel() { }
        public System.Data.OleDb.OleDbCommand Clone() { throw null; }
        protected override System.Data.Common.DbParameter CreateDbParameter() { throw null; }
        public new System.Data.OleDb.OleDbParameter CreateParameter() { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) { throw null; }
        public override int ExecuteNonQuery() { throw null; }
        public new System.Data.OleDb.OleDbDataReader ExecuteReader() { throw null; }
        public new System.Data.OleDb.OleDbDataReader ExecuteReader(System.Data.CommandBehavior behavior) { throw null; }
        public override object? ExecuteScalar() { throw null; }
        public override void Prepare() { }
        public void ResetCommandTimeout() { }
        System.Data.IDataReader System.Data.IDbCommand.ExecuteReader() { throw null; }
        System.Data.IDataReader System.Data.IDbCommand.ExecuteReader(System.Data.CommandBehavior behavior) { throw null; }
        object System.ICloneable.Clone() { throw null; }
    }
    public sealed partial class OleDbCommandBuilder : System.Data.Common.DbCommandBuilder
    {
        public OleDbCommandBuilder() { }
        public OleDbCommandBuilder(System.Data.OleDb.OleDbDataAdapter? adapter) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public new System.Data.OleDb.OleDbDataAdapter? DataAdapter { get { throw null; } set { } }
        protected override void ApplyParameterInfo(System.Data.Common.DbParameter parameter, System.Data.DataRow datarow, System.Data.StatementType statementType, bool whereClause) { }
        public static void DeriveParameters(System.Data.OleDb.OleDbCommand command) { }
        public new System.Data.OleDb.OleDbCommand GetDeleteCommand() { throw null; }
        public new System.Data.OleDb.OleDbCommand GetDeleteCommand(bool useColumnsForParameterNames) { throw null; }
        public new System.Data.OleDb.OleDbCommand GetInsertCommand() { throw null; }
        public new System.Data.OleDb.OleDbCommand GetInsertCommand(bool useColumnsForParameterNames) { throw null; }
        protected override string GetParameterName(int parameterOrdinal) { throw null; }
        protected override string GetParameterName(string parameterName) { throw null; }
        protected override string GetParameterPlaceholder(int parameterOrdinal) { throw null; }
        public new System.Data.OleDb.OleDbCommand GetUpdateCommand() { throw null; }
        public new System.Data.OleDb.OleDbCommand GetUpdateCommand(bool useColumnsForParameterNames) { throw null; }
        public override string QuoteIdentifier(string unquotedIdentifier) { throw null; }
        public string QuoteIdentifier(string unquotedIdentifier, System.Data.OleDb.OleDbConnection connection) { throw null; }
        protected override void SetRowUpdatingHandler(System.Data.Common.DbDataAdapter adapter) { }
        public override string UnquoteIdentifier(string quotedIdentifier) { throw null; }
        public string UnquoteIdentifier(string quotedIdentifier, System.Data.OleDb.OleDbConnection connection) { throw null; }
    }
    [System.ComponentModel.DefaultEventAttribute("InfoMessage")]
    public sealed partial class OleDbConnection : System.Data.Common.DbConnection, System.Data.IDbConnection, System.ICloneable, System.IDisposable
    {
        public OleDbConnection() { }
        public OleDbConnection(string? connectionString) { }
        [System.ComponentModel.DefaultValueAttribute("")]
        [System.ComponentModel.RecommendedAsConfigurableAttribute(true)]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.ComponentModel.SettingsBindableAttribute(true)]
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public override string ConnectionString { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public override int ConnectionTimeout { get { throw null; } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public override string Database { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(true)]
        public override string DataSource { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(true)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string Provider { get { throw null; } }
        public override string ServerVersion { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public override System.Data.ConnectionState State { get { throw null; } }
        public event System.Data.OleDb.OleDbInfoMessageEventHandler? InfoMessage { add { } remove { } }
        protected override System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) { throw null; }
        public new System.Data.OleDb.OleDbTransaction BeginTransaction() { throw null; }
        public new System.Data.OleDb.OleDbTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel) { throw null; }
        public override void ChangeDatabase(string value) { }
        public override void Close() { }
        public new System.Data.OleDb.OleDbCommand CreateCommand() { throw null; }
        protected override System.Data.Common.DbCommand CreateDbCommand() { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void EnlistTransaction(System.Transactions.Transaction? transaction) { }
        public System.Data.DataTable? GetOleDbSchemaTable(System.Guid schema, object?[]? restrictions) { throw null; }
        public override System.Data.DataTable GetSchema() { throw null; }
        public override System.Data.DataTable GetSchema(string collectionName) { throw null; }
        public override System.Data.DataTable GetSchema(string collectionName, string?[]? restrictionValues) { throw null; }
        public override void Open() { }
        public static void ReleaseObjectPool() { }
        public void ResetState() { }
        object System.ICloneable.Clone() { throw null; }
    }
    [System.ComponentModel.DefaultPropertyAttribute("Provider")]
    [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
    public sealed partial class OleDbConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
    {
        public OleDbConnectionStringBuilder() { }
        public OleDbConnectionStringBuilder(string? connectionString) { }
        [System.ComponentModel.DisplayNameAttribute("Data Source")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public string DataSource { get { throw null; } set { } }
        [System.ComponentModel.DisplayNameAttribute("File Name")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public string FileName { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public override object this[string keyword] { get { throw null; } set { } }
        public override System.Collections.ICollection Keys { get { throw null; } }
        [System.ComponentModel.DisplayNameAttribute("Persist Security Info")]
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public bool PersistSecurityInfo { get { throw null; } set { } }
        public override void Clear() { }
        public override bool ContainsKey(string keyword) { throw null; }
        public override bool Remove(string keyword) { throw null; }
        public override bool TryGetValue(string keyword, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out object? value) { throw null; }
    }
    [System.ComponentModel.DesignerAttribute("Microsoft.VSDesigner.Data.VS.OleDbDataAdapterDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public sealed partial class OleDbDataAdapter : System.Data.Common.DbDataAdapter, System.Data.IDataAdapter, System.Data.IDbDataAdapter, System.ICloneable
    {
        public OleDbDataAdapter() { }
        public OleDbDataAdapter(System.Data.OleDb.OleDbCommand? selectCommand) { }
        public OleDbDataAdapter(string? selectCommandText, System.Data.OleDb.OleDbConnection? selectConnection) { }
        public OleDbDataAdapter(string? selectCommandText, string? selectConnectionString) { }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public new System.Data.OleDb.OleDbCommand? DeleteCommand { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public new System.Data.OleDb.OleDbCommand? InsertCommand { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public new System.Data.OleDb.OleDbCommand? SelectCommand { get { throw null; } set { } }
        System.Data.IDbCommand? System.Data.IDbDataAdapter.DeleteCommand { get { throw null; } set { } }
        System.Data.IDbCommand? System.Data.IDbDataAdapter.InsertCommand { get { throw null; } set { } }
        System.Data.IDbCommand? System.Data.IDbDataAdapter.SelectCommand { get { throw null; } set { } }
        System.Data.IDbCommand? System.Data.IDbDataAdapter.UpdateCommand { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public new System.Data.OleDb.OleDbCommand? UpdateCommand { get { throw null; } set { } }
        public event System.Data.OleDb.OleDbRowUpdatedEventHandler? RowUpdated { add { } remove { } }
        public event System.Data.OleDb.OleDbRowUpdatingEventHandler? RowUpdating { add { } remove { } }
        protected override System.Data.Common.RowUpdatedEventArgs CreateRowUpdatedEvent(System.Data.DataRow dataRow, System.Data.IDbCommand? command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) { throw null; }
        protected override System.Data.Common.RowUpdatingEventArgs CreateRowUpdatingEvent(System.Data.DataRow dataRow, System.Data.IDbCommand? command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) { throw null; }
        public int Fill(System.Data.DataSet dataSet, object ADODBRecordSet, string srcTable) { throw null; }
        public int Fill(System.Data.DataTable dataTable, object ADODBRecordSet) { throw null; }
        protected override void OnRowUpdated(System.Data.Common.RowUpdatedEventArgs value) { }
        protected override void OnRowUpdating(System.Data.Common.RowUpdatingEventArgs value) { }
        object? System.ICloneable.Clone() { throw null; }
    }
    public sealed partial class OleDbDataReader : System.Data.Common.DbDataReader
    {
        internal OleDbDataReader() { }
        public override int Depth { get { throw null; } }
        public override int FieldCount { get { throw null; } }
        public override bool HasRows { get { throw null; } }
        public override bool IsClosed { get { throw null; } }
        public override object this[int index] { get { throw null; } }
        public override object this[string name] { get { throw null; } }
        public override int RecordsAffected { get { throw null; } }
        public override int VisibleFieldCount { get { throw null; } }
        public override void Close() { }
        public override bool GetBoolean(int ordinal) { throw null; }
        public override byte GetByte(int ordinal) { throw null; }
        public override long GetBytes(int ordinal, long dataIndex, byte[]? buffer, int bufferIndex, int length) { throw null; }
        public override char GetChar(int ordinal) { throw null; }
        public override long GetChars(int ordinal, long dataIndex, char[]? buffer, int bufferIndex, int length) { throw null; }
        public new System.Data.OleDb.OleDbDataReader GetData(int ordinal) { throw null; }
        public override string GetDataTypeName(int index) { throw null; }
        public override System.DateTime GetDateTime(int ordinal) { throw null; }
        protected override System.Data.Common.DbDataReader GetDbDataReader(int ordinal) { throw null; }
        public override decimal GetDecimal(int ordinal) { throw null; }
        public override double GetDouble(int ordinal) { throw null; }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override System.Type GetFieldType(int index) { throw null; }
        public override float GetFloat(int ordinal) { throw null; }
        public override System.Guid GetGuid(int ordinal) { throw null; }
        public override short GetInt16(int ordinal) { throw null; }
        public override int GetInt32(int ordinal) { throw null; }
        public override long GetInt64(int ordinal) { throw null; }
        public override string GetName(int index) { throw null; }
        public override int GetOrdinal(string name) { throw null; }
        public override System.Data.DataTable GetSchemaTable() { throw null; }
        public override string GetString(int ordinal) { throw null; }
        public System.TimeSpan GetTimeSpan(int ordinal) { throw null; }
        public override object GetValue(int ordinal) { throw null; }
        public override int GetValues(object[] values) { throw null; }
        public override bool IsDBNull(int ordinal) { throw null; }
        public override bool NextResult() { throw null; }
        public override bool Read() { throw null; }
    }
    public sealed partial class OleDbEnumerator
    {
        public OleDbEnumerator() { }
        public System.Data.DataTable GetElements() { throw null; }
        public static System.Data.OleDb.OleDbDataReader GetEnumerator(System.Type type) { throw null; }
        public static System.Data.OleDb.OleDbDataReader GetRootEnumerator() { throw null; }
    }
    public sealed partial class OleDbError
    {
        internal OleDbError() { }
        public string Message { get { throw null; } }
        public int NativeError { get { throw null; } }
        public string Source { get { throw null; } }
        public string SQLState { get { throw null; } }
        public override string ToString() { throw null; }
    }
    [System.ComponentModel.ListBindableAttribute(false)]
    public sealed partial class OleDbErrorCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal OleDbErrorCollection() { }
        public int Count { get { throw null; } }
        public System.Data.OleDb.OleDbError this[int index] { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.OleDb.OleDbError[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
    }
    public sealed partial class OleDbException : System.Data.Common.DbException
    {
        internal OleDbException() { }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Content)]
        public System.Data.OleDb.OleDbErrorCollection Errors { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class OleDbFactory : System.Data.Common.DbProviderFactory
    {
        internal OleDbFactory() { }
        public static readonly System.Data.OleDb.OleDbFactory Instance;
        public override System.Data.Common.DbCommand CreateCommand() { throw null; }
        public override System.Data.Common.DbCommandBuilder CreateCommandBuilder() { throw null; }
        public override System.Data.Common.DbConnection CreateConnection() { throw null; }
        public override System.Data.Common.DbConnectionStringBuilder CreateConnectionStringBuilder() { throw null; }
        public override System.Data.Common.DbDataAdapter CreateDataAdapter() { throw null; }
        public override System.Data.Common.DbParameter CreateParameter() { throw null; }
    }
    public sealed partial class OleDbInfoMessageEventArgs : System.EventArgs
    {
        internal OleDbInfoMessageEventArgs() { }
        public int ErrorCode { get { throw null; } }
        public System.Data.OleDb.OleDbErrorCollection Errors { get { throw null; } }
        public string Message { get { throw null; } }
        public string? Source { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public delegate void OleDbInfoMessageEventHandler(object sender, System.Data.OleDb.OleDbInfoMessageEventArgs e);
    public enum OleDbLiteral
    {
        Invalid = 0,
        Binary_Literal = 1,
        Catalog_Name = 2,
        Catalog_Separator = 3,
        Char_Literal = 4,
        Column_Alias = 5,
        Column_Name = 6,
        Correlation_Name = 7,
        Cursor_Name = 8,
        Escape_Percent_Prefix = 9,
        Escape_Underscore_Prefix = 10,
        Index_Name = 11,
        Like_Percent = 12,
        Like_Underscore = 13,
        Procedure_Name = 14,
        Quote_Prefix = 15,
        Schema_Name = 16,
        Table_Name = 17,
        Text_Command = 18,
        User_Name = 19,
        View_Name = 20,
        Cube_Name = 21,
        Dimension_Name = 22,
        Hierarchy_Name = 23,
        Level_Name = 24,
        Member_Name = 25,
        Property_Name = 26,
        Schema_Separator = 27,
        Quote_Suffix = 28,
        Escape_Percent_Suffix = 29,
        Escape_Underscore_Suffix = 30,
    }
    public static partial class OleDbMetaDataCollectionNames
    {
        public static readonly string Catalogs;
        public static readonly string Collations;
        public static readonly string Columns;
        public static readonly string Indexes;
        public static readonly string ProcedureColumns;
        public static readonly string ProcedureParameters;
        public static readonly string Procedures;
        public static readonly string Tables;
        public static readonly string Views;
    }
    public static partial class OleDbMetaDataColumnNames
    {
        public static readonly string BooleanFalseLiteral;
        public static readonly string BooleanTrueLiteral;
        public static readonly string DateTimeDigits;
        public static readonly string NativeDataType;
    }
    public sealed partial class OleDbParameter : System.Data.Common.DbParameter, System.Data.IDataParameter, System.Data.IDbDataParameter, System.ICloneable
    {
        public OleDbParameter() { }
        public OleDbParameter(string? name, System.Data.OleDb.OleDbType dataType) { }
        public OleDbParameter(string? name, System.Data.OleDb.OleDbType dataType, int size) { }
        public OleDbParameter(string? parameterName, System.Data.OleDb.OleDbType dbType, int size, System.Data.ParameterDirection direction, bool isNullable, byte precision, byte scale, string? srcColumn, System.Data.DataRowVersion srcVersion, object? value) { }
        public OleDbParameter(string? parameterName, System.Data.OleDb.OleDbType dbType, int size, System.Data.ParameterDirection direction, byte precision, byte scale, string? sourceColumn, System.Data.DataRowVersion sourceVersion, bool sourceColumnNullMapping, object? value) { }
        public OleDbParameter(string? name, System.Data.OleDb.OleDbType dataType, int size, string? srcColumn) { }
        public OleDbParameter(string? name, object? value) { }
        public override System.Data.DbType DbType { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        public override System.Data.ParameterDirection Direction { get { throw null; } set { } }
        public override bool IsNullable { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.Data.Common.DbProviderSpecificTypePropertyAttribute(true)]
        public System.Data.OleDb.OleDbType OleDbType { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public override string ParameterName { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((byte)0)]
        public new byte Precision { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((byte)0)]
        public new byte Scale { get { throw null; } set { } }
        public override int Size { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public override string SourceColumn { get { throw null; } set { } }
        public override bool SourceColumnNullMapping { get { throw null; } set { } }
        public override System.Data.DataRowVersion SourceVersion { get { throw null; } set { } }
        [System.ComponentModel.RefreshPropertiesAttribute(System.ComponentModel.RefreshProperties.All)]
        [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.StringConverter))]
        public override object? Value { get { throw null; } set { } }
        public override void ResetDbType() { }
        public void ResetOleDbType() { }
        object? System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class OleDbParameterCollection : System.Data.Common.DbParameterCollection
    {
        internal OleDbParameterCollection() { }
        public override int Count { get { throw null; } }
        public override bool IsFixedSize { get { throw null; } }
        public override bool IsReadOnly { get { throw null; } }
        public override bool IsSynchronized { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public new System.Data.OleDb.OleDbParameter this[int index] { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public new System.Data.OleDb.OleDbParameter this[string parameterName] { get { throw null; } set { } }
        public override object SyncRoot { get { throw null; } }
        public System.Data.OleDb.OleDbParameter Add(System.Data.OleDb.OleDbParameter value) { throw null; }
        public override int Add(object value) { throw null; }
        public System.Data.OleDb.OleDbParameter Add(string? parameterName, System.Data.OleDb.OleDbType oleDbType) { throw null; }
        public System.Data.OleDb.OleDbParameter Add(string? parameterName, System.Data.OleDb.OleDbType oleDbType, int size) { throw null; }
        public System.Data.OleDb.OleDbParameter Add(string? parameterName, System.Data.OleDb.OleDbType oleDbType, int size, string? sourceColumn) { throw null; }
        public System.Data.OleDb.OleDbParameter Add(string? parameterName, object? value) { throw null; }
        public override void AddRange(System.Array values) { }
        public void AddRange(System.Data.OleDb.OleDbParameter[] values) { }
        public System.Data.OleDb.OleDbParameter AddWithValue(string? parameterName, object? value) { throw null; }
        public override void Clear() { }
        public bool Contains(System.Data.OleDb.OleDbParameter value) { throw null; }
        public override bool Contains(object value) { throw null; }
        public override bool Contains(string value) { throw null; }
        public override void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.OleDb.OleDbParameter[] array, int index) { }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        protected override System.Data.Common.DbParameter GetParameter(int index) { throw null; }
        protected override System.Data.Common.DbParameter GetParameter(string parameterName) { throw null; }
        public int IndexOf(System.Data.OleDb.OleDbParameter value) { throw null; }
        public override int IndexOf(object value) { throw null; }
        public override int IndexOf(string parameterName) { throw null; }
        public void Insert(int index, System.Data.OleDb.OleDbParameter value) { }
        public override void Insert(int index, object value) { }
        public void Remove(System.Data.OleDb.OleDbParameter value) { }
        public override void Remove(object value) { }
        public override void RemoveAt(int index) { }
        public override void RemoveAt(string parameterName) { }
        protected override void SetParameter(int index, System.Data.Common.DbParameter value) { }
        protected override void SetParameter(string parameterName, System.Data.Common.DbParameter value) { }
    }
    public sealed partial class OleDbRowUpdatedEventArgs : System.Data.Common.RowUpdatedEventArgs
    {
        public OleDbRowUpdatedEventArgs(System.Data.DataRow dataRow, System.Data.IDbCommand? command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) : base (default(System.Data.DataRow), default(System.Data.IDbCommand), default(System.Data.StatementType), default(System.Data.Common.DataTableMapping)) { }
        public new System.Data.OleDb.OleDbCommand? Command { get { throw null; } }
    }
    public delegate void OleDbRowUpdatedEventHandler(object sender, System.Data.OleDb.OleDbRowUpdatedEventArgs e);
    public sealed partial class OleDbRowUpdatingEventArgs : System.Data.Common.RowUpdatingEventArgs
    {
        public OleDbRowUpdatingEventArgs(System.Data.DataRow dataRow, System.Data.IDbCommand? command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) : base (default(System.Data.DataRow), default(System.Data.IDbCommand), default(System.Data.StatementType), default(System.Data.Common.DataTableMapping)) { }
        protected override System.Data.IDbCommand? BaseCommand { get { throw null; } set { } }
        public new System.Data.OleDb.OleDbCommand? Command { get { throw null; } set { } }
    }
    public delegate void OleDbRowUpdatingEventHandler(object sender, System.Data.OleDb.OleDbRowUpdatingEventArgs e);
    public sealed partial class OleDbSchemaGuid
    {
        public static readonly System.Guid Assertions;
        public static readonly System.Guid Catalogs;
        public static readonly System.Guid Character_Sets;
        public static readonly System.Guid Check_Constraints;
        public static readonly System.Guid Check_Constraints_By_Table;
        public static readonly System.Guid Collations;
        public static readonly System.Guid Columns;
        public static readonly System.Guid Column_Domain_Usage;
        public static readonly System.Guid Column_Privileges;
        public static readonly System.Guid Constraint_Column_Usage;
        public static readonly System.Guid Constraint_Table_Usage;
        public static readonly System.Guid DbInfoKeywords;
        public static readonly System.Guid DbInfoLiterals;
        public static readonly System.Guid Foreign_Keys;
        public static readonly System.Guid Indexes;
        public static readonly System.Guid Key_Column_Usage;
        public static readonly System.Guid Primary_Keys;
        public static readonly System.Guid Procedures;
        public static readonly System.Guid Procedure_Columns;
        public static readonly System.Guid Procedure_Parameters;
        public static readonly System.Guid Provider_Types;
        public static readonly System.Guid Referential_Constraints;
        public static readonly System.Guid SchemaGuids;
        public static readonly System.Guid Schemata;
        public static readonly System.Guid Sql_Languages;
        public static readonly System.Guid Statistics;
        public static readonly System.Guid Tables;
        public static readonly System.Guid Tables_Info;
        public static readonly System.Guid Table_Constraints;
        public static readonly System.Guid Table_Privileges;
        public static readonly System.Guid Table_Statistics;
        public static readonly System.Guid Translations;
        public static readonly System.Guid Trustee;
        public static readonly System.Guid Usage_Privileges;
        public static readonly System.Guid Views;
        public static readonly System.Guid View_Column_Usage;
        public static readonly System.Guid View_Table_Usage;
        public OleDbSchemaGuid() { }
    }
    public sealed partial class OleDbTransaction : System.Data.Common.DbTransaction
    {
        internal OleDbTransaction() { }
        public new System.Data.OleDb.OleDbConnection? Connection { get { throw null; } }
        protected override System.Data.Common.DbConnection? DbConnection { get { throw null; } }
        public override System.Data.IsolationLevel IsolationLevel { get { throw null; } }
        public System.Data.OleDb.OleDbTransaction Begin() { throw null; }
        public System.Data.OleDb.OleDbTransaction Begin(System.Data.IsolationLevel isolevel) { throw null; }
        public override void Commit() { }
        protected override void Dispose(bool disposing) { }
        public override void Rollback() { }
    }
    public enum OleDbType
    {
        Empty = 0,
        SmallInt = 2,
        Integer = 3,
        Single = 4,
        Double = 5,
        Currency = 6,
        Date = 7,
        BSTR = 8,
        IDispatch = 9,
        Error = 10,
        Boolean = 11,
        Variant = 12,
        IUnknown = 13,
        Decimal = 14,
        TinyInt = 16,
        UnsignedTinyInt = 17,
        UnsignedSmallInt = 18,
        UnsignedInt = 19,
        BigInt = 20,
        UnsignedBigInt = 21,
        Filetime = 64,
        Guid = 72,
        Binary = 128,
        Char = 129,
        WChar = 130,
        Numeric = 131,
        DBDate = 133,
        DBTime = 134,
        DBTimeStamp = 135,
        PropVariant = 138,
        VarNumeric = 139,
        VarChar = 200,
        LongVarChar = 201,
        VarWChar = 202,
        LongVarWChar = 203,
        VarBinary = 204,
        LongVarBinary = 205,
    }
}
