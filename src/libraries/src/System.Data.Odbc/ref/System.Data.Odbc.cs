// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.Odbc
{
    public sealed partial class OdbcCommand : System.Data.Common.DbCommand, System.ICloneable
    {
        public OdbcCommand() { }
        public OdbcCommand(string cmdText) { }
        public OdbcCommand(string cmdText, System.Data.Odbc.OdbcConnection connection) { }
        public OdbcCommand(string cmdText, System.Data.Odbc.OdbcConnection connection, System.Data.Odbc.OdbcTransaction transaction) { }
        public override string CommandText { get { throw null; } set { } }
        public override int CommandTimeout { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Data.CommandType)(1))]
        public override System.Data.CommandType CommandType { get { throw null; } set { } }
        public new System.Data.Odbc.OdbcConnection Connection { get { throw null; } set { } }
        protected override System.Data.Common.DbConnection DbConnection { get { throw null; } set { } }
        protected override System.Data.Common.DbParameterCollection DbParameterCollection { get { throw null; } }
        protected override System.Data.Common.DbTransaction DbTransaction { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DefaultValueAttribute(true)]
        [System.ComponentModel.DesignOnlyAttribute(true)]
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override bool DesignTimeVisible { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(2))]
        public new System.Data.Odbc.OdbcParameterCollection Parameters { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public new System.Data.Odbc.OdbcTransaction Transaction { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Data.UpdateRowSource)(3))]
        public override System.Data.UpdateRowSource UpdatedRowSource { get { throw null; } set { } }
        public override void Cancel() { }
        protected override System.Data.Common.DbParameter CreateDbParameter() { throw null; }
        public new System.Data.Odbc.OdbcParameter CreateParameter() { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) { throw null; }
        public override int ExecuteNonQuery() { throw null; }
        public new System.Data.Odbc.OdbcDataReader ExecuteReader() { throw null; }
        public new System.Data.Odbc.OdbcDataReader ExecuteReader(System.Data.CommandBehavior behavior) { throw null; }
        public override object ExecuteScalar() { throw null; }
        public override void Prepare() { }
        public void ResetCommandTimeout() { }
        object System.ICloneable.Clone() { throw null; }
    }
    public sealed partial class OdbcCommandBuilder : System.Data.Common.DbCommandBuilder
    {
        public OdbcCommandBuilder() { }
        public OdbcCommandBuilder(System.Data.Odbc.OdbcDataAdapter adapter) { }
        public new System.Data.Odbc.OdbcDataAdapter DataAdapter { get { throw null; } set { } }
        protected override void ApplyParameterInfo(System.Data.Common.DbParameter parameter, System.Data.DataRow datarow, System.Data.StatementType statementType, bool whereClause) { }
        public static void DeriveParameters(System.Data.Odbc.OdbcCommand command) { }
        public new System.Data.Odbc.OdbcCommand GetDeleteCommand() { throw null; }
        public new System.Data.Odbc.OdbcCommand GetDeleteCommand(bool useColumnsForParameterNames) { throw null; }
        public new System.Data.Odbc.OdbcCommand GetInsertCommand() { throw null; }
        public new System.Data.Odbc.OdbcCommand GetInsertCommand(bool useColumnsForParameterNames) { throw null; }
        protected override string GetParameterName(int parameterOrdinal) { throw null; }
        protected override string GetParameterName(string parameterName) { throw null; }
        protected override string GetParameterPlaceholder(int parameterOrdinal) { throw null; }
        public new System.Data.Odbc.OdbcCommand GetUpdateCommand() { throw null; }
        public new System.Data.Odbc.OdbcCommand GetUpdateCommand(bool useColumnsForParameterNames) { throw null; }
        public override string QuoteIdentifier(string unquotedIdentifier) { throw null; }
        public string QuoteIdentifier(string unquotedIdentifier, System.Data.Odbc.OdbcConnection connection) { throw null; }
        protected override void SetRowUpdatingHandler(System.Data.Common.DbDataAdapter adapter) { }
        public override string UnquoteIdentifier(string quotedIdentifier) { throw null; }
        public string UnquoteIdentifier(string quotedIdentifier, System.Data.Odbc.OdbcConnection connection) { throw null; }
    }
    public sealed partial class OdbcConnection : System.Data.Common.DbConnection, System.ICloneable
    {
        public OdbcConnection() { }
        public OdbcConnection(string connectionString) { }
        public override string ConnectionString { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(15)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public new int ConnectionTimeout { get { throw null; } set { } }
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public override string Database { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public override string DataSource { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public string Driver { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public override string ServerVersion { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public override System.Data.ConnectionState State { get { throw null; } }
        public event System.Data.Odbc.OdbcInfoMessageEventHandler InfoMessage { add { } remove { } }
        protected override System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) { throw null; }
        public new System.Data.Odbc.OdbcTransaction BeginTransaction() { throw null; }
        public new System.Data.Odbc.OdbcTransaction BeginTransaction(System.Data.IsolationLevel isolevel) { throw null; }
        public override void ChangeDatabase(string value) { }
        public override void Close() { }
        public new System.Data.Odbc.OdbcCommand CreateCommand() { throw null; }
        protected override System.Data.Common.DbCommand CreateDbCommand() { throw null; }
        protected override void Dispose(bool disposing) { }
        public override void Open() { }
        public static void ReleaseObjectPool() { }
        object System.ICloneable.Clone() { throw null; }
    }
    public sealed partial class OdbcConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
    {
        public OdbcConnectionStringBuilder() { }
        public OdbcConnectionStringBuilder(string connectionString) { }
        [System.ComponentModel.DisplayNameAttribute("Driver")]
        public string Driver { get { throw null; } set { } }
        [System.ComponentModel.DisplayNameAttribute("Dsn")]
        public string Dsn { get { throw null; } set { } }
        public override object this[string keyword] { get { throw null; } set { } }
        public override System.Collections.ICollection Keys { get { throw null; } }
        public override void Clear() { }
        public override bool ContainsKey(string keyword) { throw null; }
        public override bool Remove(string keyword) { throw null; }
        public override bool TryGetValue(string keyword, out object value) { value = default(object); throw null; }
    }
    public sealed partial class OdbcDataAdapter : System.Data.Common.DbDataAdapter, System.Data.IDataAdapter, System.Data.IDbDataAdapter, System.ICloneable
    {
        public OdbcDataAdapter() { }
        public OdbcDataAdapter(System.Data.Odbc.OdbcCommand selectCommand) { }
        public OdbcDataAdapter(string selectCommandText, System.Data.Odbc.OdbcConnection selectConnection) { }
        public OdbcDataAdapter(string selectCommandText, string selectConnectionString) { }
        public new System.Data.Odbc.OdbcCommand DeleteCommand { get { throw null; } set { } }
        public new System.Data.Odbc.OdbcCommand InsertCommand { get { throw null; } set { } }
        public new System.Data.Odbc.OdbcCommand SelectCommand { get { throw null; } set { } }
        System.Data.IDbCommand System.Data.IDbDataAdapter.DeleteCommand { get { throw null; } set { } }
        System.Data.IDbCommand System.Data.IDbDataAdapter.InsertCommand { get { throw null; } set { } }
        System.Data.IDbCommand System.Data.IDbDataAdapter.SelectCommand { get { throw null; } set { } }
        System.Data.IDbCommand System.Data.IDbDataAdapter.UpdateCommand { get { throw null; } set { } }
        public new System.Data.Odbc.OdbcCommand UpdateCommand { get { throw null; } set { } }
        public event System.Data.Odbc.OdbcRowUpdatedEventHandler RowUpdated { add { } remove { } }
        public event System.Data.Odbc.OdbcRowUpdatingEventHandler RowUpdating { add { } remove { } }
        protected override System.Data.Common.RowUpdatedEventArgs CreateRowUpdatedEvent(System.Data.DataRow dataRow, System.Data.IDbCommand command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) { throw null; }
        protected override System.Data.Common.RowUpdatingEventArgs CreateRowUpdatingEvent(System.Data.DataRow dataRow, System.Data.IDbCommand command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) { throw null; }
        protected override void OnRowUpdated(System.Data.Common.RowUpdatedEventArgs value) { }
        protected override void OnRowUpdating(System.Data.Common.RowUpdatingEventArgs value) { }
        object System.ICloneable.Clone() { throw null; }
    }
    public sealed partial class OdbcDataReader : System.Data.Common.DbDataReader
    {
        internal OdbcDataReader() { }
        public override int Depth { get { throw null; } }
        public override int FieldCount { get { throw null; } }
        public override bool HasRows { get { throw null; } }
        public override bool IsClosed { get { throw null; } }
        public override object this[int i] { get { throw null; } }
        public override object this[string value] { get { throw null; } }
        public override int RecordsAffected { get { throw null; } }
        public override void Close() { }
        protected override void Dispose(bool disposing) { }
        public override bool GetBoolean(int i) { throw null; }
        public override byte GetByte(int i) { throw null; }
        public override long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) { throw null; }
        public override char GetChar(int i) { throw null; }
        public override long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length) { throw null; }
        public override string GetDataTypeName(int i) { throw null; }
        public System.DateTime GetDate(int i) { throw null; }
        public override System.DateTime GetDateTime(int i) { throw null; }
        public override decimal GetDecimal(int i) { throw null; }
        public override double GetDouble(int i) { throw null; }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override System.Type GetFieldType(int i) { throw null; }
        public override float GetFloat(int i) { throw null; }
        public override System.Guid GetGuid(int i) { throw null; }
        public override short GetInt16(int i) { throw null; }
        public override int GetInt32(int i) { throw null; }
        public override long GetInt64(int i) { throw null; }
        public override string GetName(int i) { throw null; }
        public override int GetOrdinal(string value) { throw null; }
        public override System.Data.DataTable GetSchemaTable() { throw null; }
        public override string GetString(int i) { throw null; }
        public System.TimeSpan GetTime(int i) { throw null; }
        public override object GetValue(int i) { throw null; }
        public override int GetValues(object[] values) { throw null; }
        public override bool IsDBNull(int i) { throw null; }
        public override bool NextResult() { throw null; }
        public override bool Read() { throw null; }
    }
    public sealed partial class OdbcError
    {
        internal OdbcError() { }
        public string Message { get { throw null; } }
        public int NativeError { get { throw null; } }
        public string Source { get { throw null; } }
        public string SQLState { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public sealed partial class OdbcErrorCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal OdbcErrorCollection() { }
        public int Count { get { throw null; } }
        public System.Data.Odbc.OdbcError this[int i] { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void CopyTo(System.Array array, int i) { }
        public void CopyTo(System.Data.Odbc.OdbcError[] array, int i) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
    }
    public sealed partial class OdbcException : System.Data.Common.DbException
    {
        internal OdbcException() { }
        public System.Data.Odbc.OdbcErrorCollection Errors { get { throw null; } }
        public override string Source { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class OdbcFactory : System.Data.Common.DbProviderFactory
    {
        internal OdbcFactory() { }
        public static readonly System.Data.Odbc.OdbcFactory Instance;
        public override System.Data.Common.DbCommand CreateCommand() { throw null; }
        public override System.Data.Common.DbCommandBuilder CreateCommandBuilder() { throw null; }
        public override System.Data.Common.DbConnection CreateConnection() { throw null; }
        public override System.Data.Common.DbConnectionStringBuilder CreateConnectionStringBuilder() { throw null; }
        public override System.Data.Common.DbDataAdapter CreateDataAdapter() { throw null; }
        public override System.Data.Common.DbParameter CreateParameter() { throw null; }
    }
    public sealed partial class OdbcInfoMessageEventArgs : System.EventArgs
    {
        internal OdbcInfoMessageEventArgs() { }
        public System.Data.Odbc.OdbcErrorCollection Errors { get { throw null; } }
        public string Message { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public delegate void OdbcInfoMessageEventHandler(object sender, System.Data.Odbc.OdbcInfoMessageEventArgs e);
    public static partial class OdbcMetaDataCollectionNames
    {
        public static readonly string Columns;
        public static readonly string Indexes;
        public static readonly string ProcedureColumns;
        public static readonly string ProcedureParameters;
        public static readonly string Procedures;
        public static readonly string Tables;
        public static readonly string Views;
    }
    public static partial class OdbcMetaDataColumnNames
    {
        public static readonly string BooleanFalseLiteral;
        public static readonly string BooleanTrueLiteral;
        public static readonly string SQLType;
    }
    public sealed partial class OdbcParameter : System.Data.Common.DbParameter, System.Data.IDataParameter, System.Data.IDbDataParameter, System.ICloneable
    {
        public OdbcParameter() { }
        public OdbcParameter(string name, System.Data.Odbc.OdbcType type) { }
        public OdbcParameter(string name, System.Data.Odbc.OdbcType type, int size) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public OdbcParameter(string parameterName, System.Data.Odbc.OdbcType odbcType, int size, System.Data.ParameterDirection parameterDirection, bool isNullable, byte precision, byte scale, string srcColumn, System.Data.DataRowVersion srcVersion, object value) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(2))]
        public OdbcParameter(string parameterName, System.Data.Odbc.OdbcType odbcType, int size, System.Data.ParameterDirection parameterDirection, byte precision, byte scale, string sourceColumn, System.Data.DataRowVersion sourceVersion, bool sourceColumnNullMapping, object value) { }
        public OdbcParameter(string name, System.Data.Odbc.OdbcType type, int size, string sourcecolumn) { }
        public OdbcParameter(string name, object value) { }
        public override System.Data.DbType DbType { get { throw null; } set { } }
        public override System.Data.ParameterDirection Direction { get { throw null; } set { } }
        public override bool IsNullable { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute((System.Data.Odbc.OdbcType)(11))]
        [System.Data.Common.DbProviderSpecificTypePropertyAttribute(true)]
        public System.Data.Odbc.OdbcType OdbcType { get { throw null; } set { } }
        public override string ParameterName { get { throw null; } set { } }
        public new byte Precision { get { throw null; } set { } }
        public new byte Scale { get { throw null; } set { } }
        public override int Size { get { throw null; } set { } }
        public override string SourceColumn { get { throw null; } set { } }
        public override bool SourceColumnNullMapping { get { throw null; } set { } }
        public override System.Data.DataRowVersion SourceVersion { get { throw null; } set { } }
        public override object Value { get { throw null; } set { } }
        public override void ResetDbType() { }
        public void ResetOdbcType() { }
        object System.ICloneable.Clone() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class OdbcParameterCollection : System.Data.Common.DbParameterCollection
    {
        internal OdbcParameterCollection() { }
        public override int Count { get { throw null; } }
        public override bool IsFixedSize { get { throw null; } }
        public override bool IsReadOnly { get { throw null; } }
        public override bool IsSynchronized { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public new System.Data.Odbc.OdbcParameter this[int index] { get { throw null; } set { } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute((System.ComponentModel.DesignerSerializationVisibility)(0))]
        public new System.Data.Odbc.OdbcParameter this[string parameterName] { get { throw null; } set { } }
        public override object SyncRoot { get { throw null; } }
        public System.Data.Odbc.OdbcParameter Add(System.Data.Odbc.OdbcParameter value) { throw null; }
        public override int Add(object value) { throw null; }
        public System.Data.Odbc.OdbcParameter Add(string parameterName, System.Data.Odbc.OdbcType odbcType) { throw null; }
        public System.Data.Odbc.OdbcParameter Add(string parameterName, System.Data.Odbc.OdbcType odbcType, int size) { throw null; }
        public System.Data.Odbc.OdbcParameter Add(string parameterName, System.Data.Odbc.OdbcType odbcType, int size, string sourceColumn) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Add(String parameterName, Object value) has been deprecated.  Use AddWithValue(String parameterName, Object value).  http://go.microsoft.com/fwlink/?linkid=14202", false)]
        public System.Data.Odbc.OdbcParameter Add(string parameterName, object value) { throw null; }
        public override void AddRange(System.Array values) { }
        public void AddRange(System.Data.Odbc.OdbcParameter[] values) { }
        public System.Data.Odbc.OdbcParameter AddWithValue(string parameterName, object value) { throw null; }
        public override void Clear() { }
        public bool Contains(System.Data.Odbc.OdbcParameter value) { throw null; }
        public override bool Contains(object value) { throw null; }
        public override bool Contains(string value) { throw null; }
        public override void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.Odbc.OdbcParameter[] array, int index) { }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        protected override System.Data.Common.DbParameter GetParameter(int index) { throw null; }
        protected override System.Data.Common.DbParameter GetParameter(string parameterName) { throw null; }
        public int IndexOf(System.Data.Odbc.OdbcParameter value) { throw null; }
        public override int IndexOf(object value) { throw null; }
        public override int IndexOf(string parameterName) { throw null; }
        public void Insert(int index, System.Data.Odbc.OdbcParameter value) { }
        public override void Insert(int index, object value) { }
        public void Remove(System.Data.Odbc.OdbcParameter value) { }
        public override void Remove(object value) { }
        public override void RemoveAt(int index) { }
        public override void RemoveAt(string parameterName) { }
        protected override void SetParameter(int index, System.Data.Common.DbParameter value) { }
        protected override void SetParameter(string parameterName, System.Data.Common.DbParameter value) { }
    }
    public sealed partial class OdbcRowUpdatedEventArgs : System.Data.Common.RowUpdatedEventArgs
    {
        public OdbcRowUpdatedEventArgs(System.Data.DataRow row, System.Data.IDbCommand command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) : base (default(System.Data.DataRow), default(System.Data.IDbCommand), default(System.Data.StatementType), default(System.Data.Common.DataTableMapping)) { }
        public new System.Data.Odbc.OdbcCommand Command { get { throw null; } }
    }
    public delegate void OdbcRowUpdatedEventHandler(object sender, System.Data.Odbc.OdbcRowUpdatedEventArgs e);
    public sealed partial class OdbcRowUpdatingEventArgs : System.Data.Common.RowUpdatingEventArgs
    {
        public OdbcRowUpdatingEventArgs(System.Data.DataRow row, System.Data.IDbCommand command, System.Data.StatementType statementType, System.Data.Common.DataTableMapping tableMapping) : base (default(System.Data.DataRow), default(System.Data.IDbCommand), default(System.Data.StatementType), default(System.Data.Common.DataTableMapping)) { }
        protected override System.Data.IDbCommand BaseCommand { get { throw null; } set { } }
        public new System.Data.Odbc.OdbcCommand Command { get { throw null; } set { } }
    }
    public delegate void OdbcRowUpdatingEventHandler(object sender, System.Data.Odbc.OdbcRowUpdatingEventArgs e);
    public sealed partial class OdbcTransaction : System.Data.Common.DbTransaction
    {
        internal OdbcTransaction() { }
        public new System.Data.Odbc.OdbcConnection Connection { get { throw null; } }
        protected override System.Data.Common.DbConnection DbConnection { get { throw null; } }
        public override System.Data.IsolationLevel IsolationLevel { get { throw null; } }
        public override void Commit() { }
        protected override void Dispose(bool disposing) { }
        public override void Rollback() { }
    }
    public enum OdbcType
    {
        BigInt = 1,
        Binary = 2,
        Bit = 3,
        Char = 4,
        Date = 23,
        DateTime = 5,
        Decimal = 6,
        Double = 8,
        Image = 9,
        Int = 10,
        NChar = 11,
        NText = 12,
        Numeric = 7,
        NVarChar = 13,
        Real = 14,
        SmallDateTime = 16,
        SmallInt = 17,
        Text = 18,
        Time = 24,
        Timestamp = 19,
        TinyInt = 20,
        UniqueIdentifier = 15,
        VarBinary = 21,
        VarChar = 22,
    }
}
