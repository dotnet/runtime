// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlDbType))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.StatementCompletedEventArgs))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.StatementCompletedEventHandler))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.INullable))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlBinary))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlBoolean))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlByte))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlBytes))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlChars))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlCompareOptions))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlDateTime))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlDecimal))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlDouble))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlGuid))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlInt16))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlInt32))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlInt64))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlMoney))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlNullValueException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlSingle))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlString))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlTruncateException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlTypeException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Data.SqlTypes.SqlXml))]

namespace Microsoft.SqlServer.Server
{
    public partial class SqlDataRecord : System.Data.IDataRecord
    {
        public SqlDataRecord(params Microsoft.SqlServer.Server.SqlMetaData[] metaData) { }
        public virtual int FieldCount { get { throw null; } }
        public virtual object this[int ordinal] { get { throw null; } }
        public virtual object this[string name] { get { throw null; } }
        public virtual bool GetBoolean(int ordinal) { throw null; }
        public virtual byte GetByte(int ordinal) { throw null; }
        public virtual long GetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length) { throw null; }
        public virtual char GetChar(int ordinal) { throw null; }
        public virtual long GetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length) { throw null; }
        System.Data.IDataReader System.Data.IDataRecord.GetData(int ordinal) { throw null; }
        public virtual string GetDataTypeName(int ordinal) { throw null; }
        public virtual System.DateTime GetDateTime(int ordinal) { throw null; }
        public virtual System.DateTimeOffset GetDateTimeOffset(int ordinal) { throw null; }
        public virtual decimal GetDecimal(int ordinal) { throw null; }
        public virtual double GetDouble(int ordinal) { throw null; }
        public virtual System.Type GetFieldType(int ordinal) { throw null; }
        public virtual float GetFloat(int ordinal) { throw null; }
        public virtual System.Guid GetGuid(int ordinal) { throw null; }
        public virtual short GetInt16(int ordinal) { throw null; }
        public virtual int GetInt32(int ordinal) { throw null; }
        public virtual long GetInt64(int ordinal) { throw null; }
        public virtual string GetName(int ordinal) { throw null; }
        public virtual int GetOrdinal(string name) { throw null; }
        public virtual System.Data.SqlTypes.SqlBinary GetSqlBinary(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlBoolean GetSqlBoolean(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlByte GetSqlByte(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlBytes GetSqlBytes(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlChars GetSqlChars(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlDateTime GetSqlDateTime(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlDecimal GetSqlDecimal(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlDouble GetSqlDouble(int ordinal) { throw null; }
        public virtual System.Type GetSqlFieldType(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlGuid GetSqlGuid(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlInt16 GetSqlInt16(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlInt32 GetSqlInt32(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlInt64 GetSqlInt64(int ordinal) { throw null; }
        public virtual Microsoft.SqlServer.Server.SqlMetaData GetSqlMetaData(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlMoney GetSqlMoney(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlSingle GetSqlSingle(int ordinal) { throw null; }
        public virtual System.Data.SqlTypes.SqlString GetSqlString(int ordinal) { throw null; }
        public virtual object GetSqlValue(int ordinal) { throw null; }
        public virtual int GetSqlValues(object[] values) { throw null; }
        public virtual System.Data.SqlTypes.SqlXml GetSqlXml(int ordinal) { throw null; }
        public virtual string GetString(int ordinal) { throw null; }
        public virtual System.TimeSpan GetTimeSpan(int ordinal) { throw null; }
        public virtual object GetValue(int ordinal) { throw null; }
        public virtual int GetValues(object[] values) { throw null; }
        public virtual bool IsDBNull(int ordinal) { throw null; }
        public virtual void SetBoolean(int ordinal, bool value) { }
        public virtual void SetByte(int ordinal, byte value) { }
        public virtual void SetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length) { }
        public virtual void SetChar(int ordinal, char value) { }
        public virtual void SetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length) { }
        public virtual void SetDateTime(int ordinal, System.DateTime value) { }
        public virtual void SetDateTimeOffset(int ordinal, System.DateTimeOffset value) { }
        public virtual void SetDBNull(int ordinal) { }
        public virtual void SetDecimal(int ordinal, decimal value) { }
        public virtual void SetDouble(int ordinal, double value) { }
        public virtual void SetFloat(int ordinal, float value) { }
        public virtual void SetGuid(int ordinal, System.Guid value) { }
        public virtual void SetInt16(int ordinal, short value) { }
        public virtual void SetInt32(int ordinal, int value) { }
        public virtual void SetInt64(int ordinal, long value) { }
        public virtual void SetSqlBinary(int ordinal, System.Data.SqlTypes.SqlBinary value) { }
        public virtual void SetSqlBoolean(int ordinal, System.Data.SqlTypes.SqlBoolean value) { }
        public virtual void SetSqlByte(int ordinal, System.Data.SqlTypes.SqlByte value) { }
        public virtual void SetSqlBytes(int ordinal, System.Data.SqlTypes.SqlBytes value) { }
        public virtual void SetSqlChars(int ordinal, System.Data.SqlTypes.SqlChars value) { }
        public virtual void SetSqlDateTime(int ordinal, System.Data.SqlTypes.SqlDateTime value) { }
        public virtual void SetSqlDecimal(int ordinal, System.Data.SqlTypes.SqlDecimal value) { }
        public virtual void SetSqlDouble(int ordinal, System.Data.SqlTypes.SqlDouble value) { }
        public virtual void SetSqlGuid(int ordinal, System.Data.SqlTypes.SqlGuid value) { }
        public virtual void SetSqlInt16(int ordinal, System.Data.SqlTypes.SqlInt16 value) { }
        public virtual void SetSqlInt32(int ordinal, System.Data.SqlTypes.SqlInt32 value) { }
        public virtual void SetSqlInt64(int ordinal, System.Data.SqlTypes.SqlInt64 value) { }
        public virtual void SetSqlMoney(int ordinal, System.Data.SqlTypes.SqlMoney value) { }
        public virtual void SetSqlSingle(int ordinal, System.Data.SqlTypes.SqlSingle value) { }
        public virtual void SetSqlString(int ordinal, System.Data.SqlTypes.SqlString value) { }
        public virtual void SetSqlXml(int ordinal, System.Data.SqlTypes.SqlXml value) { }
        public virtual void SetString(int ordinal, string value) { }
        public virtual void SetTimeSpan(int ordinal, System.TimeSpan value) { }
        public virtual void SetValue(int ordinal, object value) { }
        public virtual int SetValues(params object[] values) { throw null; }
    }
    public sealed partial class SqlMetaData
    {
        public SqlMetaData(string name, System.Data.SqlDbType dbType) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, bool useServerDefault, bool isUniqueKey, System.Data.SqlClient.SortOrder columnSortOrder, int sortOrdinal) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, byte precision, byte scale) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, byte precision, byte scale, bool useServerDefault, bool isUniqueKey, System.Data.SqlClient.SortOrder columnSortOrder, int sortOrdinal) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, long maxLength) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, long maxLength, bool useServerDefault, bool isUniqueKey, System.Data.SqlClient.SortOrder columnSortOrder, int sortOrdinal) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, long maxLength, byte precision, byte scale, long locale, System.Data.SqlTypes.SqlCompareOptions compareOptions, System.Type userDefinedType) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, long maxLength, byte precision, byte scale, long localeId, System.Data.SqlTypes.SqlCompareOptions compareOptions, System.Type userDefinedType, bool useServerDefault, bool isUniqueKey, System.Data.SqlClient.SortOrder columnSortOrder, int sortOrdinal) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, long maxLength, long locale, System.Data.SqlTypes.SqlCompareOptions compareOptions) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, long maxLength, long locale, System.Data.SqlTypes.SqlCompareOptions compareOptions, bool useServerDefault, bool isUniqueKey, System.Data.SqlClient.SortOrder columnSortOrder, int sortOrdinal) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, string database, string owningSchema, string objectName) { }
        public SqlMetaData(string name, System.Data.SqlDbType dbType, string database, string owningSchema, string objectName, bool useServerDefault, bool isUniqueKey, System.Data.SqlClient.SortOrder columnSortOrder, int sortOrdinal) { }
        public System.Data.SqlTypes.SqlCompareOptions CompareOptions { get { throw null; } }
        public bool IsUniqueKey { get { throw null; } }
        public long LocaleId { get { throw null; } }
        public static long Max { get { throw null; } }
        public long MaxLength { get { throw null; } }
        public string Name { get { throw null; } }
        public byte Precision { get { throw null; } }
        public byte Scale { get { throw null; } }
        public System.Data.SqlClient.SortOrder SortOrder { get { throw null; } }
        public int SortOrdinal { get { throw null; } }
        public System.Data.SqlDbType SqlDbType { get { throw null; } }
        public string TypeName { get { throw null; } }
        public bool UseServerDefault { get { throw null; } }
        public string XmlSchemaCollectionDatabase { get { throw null; } }
        public string XmlSchemaCollectionName { get { throw null; } }
        public string XmlSchemaCollectionOwningSchema { get { throw null; } }
        public bool Adjust(bool value) { throw null; }
        public byte Adjust(byte value) { throw null; }
        public byte[] Adjust(byte[] value) { throw null; }
        public char Adjust(char value) { throw null; }
        public char[] Adjust(char[] value) { throw null; }
        public System.Data.SqlTypes.SqlBinary Adjust(System.Data.SqlTypes.SqlBinary value) { throw null; }
        public System.Data.SqlTypes.SqlBoolean Adjust(System.Data.SqlTypes.SqlBoolean value) { throw null; }
        public System.Data.SqlTypes.SqlByte Adjust(System.Data.SqlTypes.SqlByte value) { throw null; }
        public System.Data.SqlTypes.SqlBytes Adjust(System.Data.SqlTypes.SqlBytes value) { throw null; }
        public System.Data.SqlTypes.SqlChars Adjust(System.Data.SqlTypes.SqlChars value) { throw null; }
        public System.Data.SqlTypes.SqlDateTime Adjust(System.Data.SqlTypes.SqlDateTime value) { throw null; }
        public System.Data.SqlTypes.SqlDecimal Adjust(System.Data.SqlTypes.SqlDecimal value) { throw null; }
        public System.Data.SqlTypes.SqlDouble Adjust(System.Data.SqlTypes.SqlDouble value) { throw null; }
        public System.Data.SqlTypes.SqlGuid Adjust(System.Data.SqlTypes.SqlGuid value) { throw null; }
        public System.Data.SqlTypes.SqlInt16 Adjust(System.Data.SqlTypes.SqlInt16 value) { throw null; }
        public System.Data.SqlTypes.SqlInt32 Adjust(System.Data.SqlTypes.SqlInt32 value) { throw null; }
        public System.Data.SqlTypes.SqlInt64 Adjust(System.Data.SqlTypes.SqlInt64 value) { throw null; }
        public System.Data.SqlTypes.SqlMoney Adjust(System.Data.SqlTypes.SqlMoney value) { throw null; }
        public System.Data.SqlTypes.SqlSingle Adjust(System.Data.SqlTypes.SqlSingle value) { throw null; }
        public System.Data.SqlTypes.SqlString Adjust(System.Data.SqlTypes.SqlString value) { throw null; }
        public System.Data.SqlTypes.SqlXml Adjust(System.Data.SqlTypes.SqlXml value) { throw null; }
        public System.DateTime Adjust(System.DateTime value) { throw null; }
        public System.DateTimeOffset Adjust(System.DateTimeOffset value) { throw null; }
        public decimal Adjust(decimal value) { throw null; }
        public double Adjust(double value) { throw null; }
        public System.Guid Adjust(System.Guid value) { throw null; }
        public short Adjust(short value) { throw null; }
        public int Adjust(int value) { throw null; }
        public long Adjust(long value) { throw null; }
        public object Adjust(object value) { throw null; }
        public float Adjust(float value) { throw null; }
        public string Adjust(string value) { throw null; }
        public System.TimeSpan Adjust(System.TimeSpan value) { throw null; }
        public static Microsoft.SqlServer.Server.SqlMetaData InferFromValue(object value, string name) { throw null; }
    }
}
namespace System.Data.Sql
{
    public sealed partial class SqlNotificationRequest
    {
        public SqlNotificationRequest() { }
        public SqlNotificationRequest(string userData, string options, int timeout) { }
        public string Options { get { throw null; } set { } }
        public int Timeout { get { throw null; } set { } }
        public string UserData { get { throw null; } set { } }
    }
}
namespace System.Data.SqlClient
{
    public enum ApplicationIntent
    {
        ReadOnly = 1,
        ReadWrite = 0,
    }
    public enum SortOrder
    {
        Ascending = 0,
        Descending = 1,
        Unspecified = -1,
    }
    public sealed partial class SqlBulkCopy : System.IDisposable
    {
        public SqlBulkCopy(System.Data.SqlClient.SqlConnection connection) { }
        public SqlBulkCopy(System.Data.SqlClient.SqlConnection connection, System.Data.SqlClient.SqlBulkCopyOptions copyOptions, System.Data.SqlClient.SqlTransaction externalTransaction) { }
        public SqlBulkCopy(string connectionString) { }
        public SqlBulkCopy(string connectionString, System.Data.SqlClient.SqlBulkCopyOptions copyOptions) { }
        public int BatchSize { get { throw null; } set { } }
        public int BulkCopyTimeout { get { throw null; } set { } }
        public System.Data.SqlClient.SqlBulkCopyColumnMappingCollection ColumnMappings { get { throw null; } }
        public string DestinationTableName { get { throw null; } set { } }
        public bool EnableStreaming { get { throw null; } set { } }
        public int NotifyAfter { get { throw null; } set { } }
        public event System.Data.SqlClient.SqlRowsCopiedEventHandler SqlRowsCopied { add { } remove { } }
        public void Close() { }
        void System.IDisposable.Dispose() { }
        public void WriteToServer(System.Data.Common.DbDataReader reader) { }
        public void WriteToServer(System.Data.IDataReader reader) { }
        public void WriteToServer(System.Data.DataTable table) { }
        public void WriteToServer(System.Data.DataTable table, System.Data.DataRowState rowState) { }
        public void WriteToServer(System.Data.DataRow[] rows) { }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.DataRow[] rows) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.DataRow[] rows, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.IDataReader reader) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.IDataReader reader, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.Common.DbDataReader reader) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.DataTable table) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.DataTable table, System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.DataTable table, System.Data.DataRowState rowState) { throw null; }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.DataTable table, System.Data.DataRowState rowState, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public sealed partial class SqlBulkCopyColumnMapping
    {
        public SqlBulkCopyColumnMapping() { }
        public SqlBulkCopyColumnMapping(int sourceColumnOrdinal, int destinationOrdinal) { }
        public SqlBulkCopyColumnMapping(int sourceColumnOrdinal, string destinationColumn) { }
        public SqlBulkCopyColumnMapping(string sourceColumn, int destinationOrdinal) { }
        public SqlBulkCopyColumnMapping(string sourceColumn, string destinationColumn) { }
        public string DestinationColumn { get { throw null; } set { } }
        public int DestinationOrdinal { get { throw null; } set { } }
        public string SourceColumn { get { throw null; } set { } }
        public int SourceOrdinal { get { throw null; } set { } }
    }
    public sealed partial class SqlBulkCopyColumnMappingCollection : System.Collections.CollectionBase
    {
        internal SqlBulkCopyColumnMappingCollection() { }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping this[int index] { get { throw null; } }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(System.Data.SqlClient.SqlBulkCopyColumnMapping bulkCopyColumnMapping) { throw null; }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(int sourceColumnIndex, int destinationColumnIndex) { throw null; }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(int sourceColumnIndex, string destinationColumn) { throw null; }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(string sourceColumn, int destinationColumnIndex) { throw null; }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(string sourceColumn, string destinationColumn) { throw null; }
        public bool Contains(System.Data.SqlClient.SqlBulkCopyColumnMapping value) { throw null; }
        public void CopyTo(System.Data.SqlClient.SqlBulkCopyColumnMapping[] array, int index) { }
        public int IndexOf(System.Data.SqlClient.SqlBulkCopyColumnMapping value) { throw null; }
        public void Insert(int index, System.Data.SqlClient.SqlBulkCopyColumnMapping value) { }
        public void Remove(System.Data.SqlClient.SqlBulkCopyColumnMapping value) { }
    }
    [System.FlagsAttribute]
    public enum SqlBulkCopyOptions
    {
        CheckConstraints = 2,
        Default = 0,
        FireTriggers = 16,
        KeepIdentity = 1,
        KeepNulls = 8,
        TableLock = 4,
        UseInternalTransaction = 32,
    }
    public sealed partial class SqlClientFactory : System.Data.Common.DbProviderFactory
    {
        internal SqlClientFactory() { }
        public static readonly System.Data.SqlClient.SqlClientFactory Instance;
        public override System.Data.Common.DbCommand CreateCommand() { throw null; }
        public override System.Data.Common.DbCommandBuilder CreateCommandBuilder() { throw null; }
        public override System.Data.Common.DbConnection CreateConnection() { throw null; }
        public override System.Data.Common.DbConnectionStringBuilder CreateConnectionStringBuilder() { throw null; }
        public override System.Data.Common.DbDataAdapter CreateDataAdapter() { throw null; }
        public override System.Data.Common.DbParameter CreateParameter() { throw null; }
    }
    public static partial class SqlClientMetaDataCollectionNames
    {
        public static readonly string Columns;
        public static readonly string Databases;
        public static readonly string ForeignKeys;
        public static readonly string IndexColumns;
        public static readonly string Indexes;
        public static readonly string Parameters;
        public static readonly string ProcedureColumns;
        public static readonly string Procedures;
        public static readonly string Tables;
        public static readonly string UserDefinedTypes;
        public static readonly string Users;
        public static readonly string ViewColumns;
        public static readonly string Views;
    }
    public sealed partial class SqlCommand : System.Data.Common.DbCommand, System.ICloneable
    {
        public SqlCommand() { }
        public SqlCommand(string cmdText) { }
        public SqlCommand(string cmdText, System.Data.SqlClient.SqlConnection connection) { }
        public SqlCommand(string cmdText, System.Data.SqlClient.SqlConnection connection, System.Data.SqlClient.SqlTransaction transaction) { }
        public override string CommandText { get { throw null; } set { } }
        public override int CommandTimeout { get { throw null; } set { } }
        public override System.Data.CommandType CommandType { get { throw null; } set { } }
        public new System.Data.SqlClient.SqlConnection Connection { get { throw null; } set { } }
        protected override System.Data.Common.DbConnection DbConnection { get { throw null; } set { } }
        protected override System.Data.Common.DbParameterCollection DbParameterCollection { get { throw null; } }
        protected override System.Data.Common.DbTransaction DbTransaction { get { throw null; } set { } }
        public override bool DesignTimeVisible { get { throw null; } set { } }
        public new System.Data.SqlClient.SqlParameterCollection Parameters { get { throw null; } }
        public new System.Data.SqlClient.SqlTransaction Transaction { get { throw null; } set { } }
        public override System.Data.UpdateRowSource UpdatedRowSource { get { throw null; } set { } }
        public event System.Data.StatementCompletedEventHandler StatementCompleted { add { } remove { } }
        public override void Cancel() { }
        object System.ICloneable.Clone() { throw null; }
        public SqlCommand Clone() { throw null;  }
        protected override System.Data.Common.DbParameter CreateDbParameter() { throw null; }
        public new System.Data.SqlClient.SqlParameter CreateParameter() { throw null; }
        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) { throw null; }
        protected override System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteDbDataReaderAsync(System.Data.CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override int ExecuteNonQuery() { throw null; }
        public override System.Threading.Tasks.Task<int> ExecuteNonQueryAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public new System.Data.SqlClient.SqlDataReader ExecuteReader() { throw null; }
        public new System.Data.SqlClient.SqlDataReader ExecuteReader(System.Data.CommandBehavior behavior) { throw null; }
        public new System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader> ExecuteReaderAsync() { throw null; }
        public new System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader> ExecuteReaderAsync(System.Data.CommandBehavior behavior) { throw null; }
        public new System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader> ExecuteReaderAsync(System.Data.CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) { throw null; }
        public new System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader> ExecuteReaderAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override object ExecuteScalar() { throw null; }
        public override System.Threading.Tasks.Task<object> ExecuteScalarAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public System.Xml.XmlReader ExecuteXmlReader() { throw null; }
        public System.Threading.Tasks.Task<System.Xml.XmlReader> ExecuteXmlReaderAsync() { throw null; }
        public System.Threading.Tasks.Task<System.Xml.XmlReader> ExecuteXmlReaderAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override void Prepare() { }
        public System.Data.Sql.SqlNotificationRequest Notification { get { throw null; } set { } }
    }
    public sealed class SqlCommandBuilder : System.Data.Common.DbCommandBuilder
    {
        public SqlCommandBuilder() { }
        public SqlCommandBuilder(SqlDataAdapter adapter) { }
        public override System.Data.Common.CatalogLocation CatalogLocation { get { throw null; } set { } }
        public override string CatalogSeparator { get { throw null; } set { } }
        new public SqlDataAdapter DataAdapter { get { throw null; } set { } }
        public override string QuotePrefix { get { throw null; } set { } }
        public override string QuoteSuffix { get { throw null; } set { } }
        public override string SchemaSeparator { get { throw null; } set { } }
        new public SqlCommand GetInsertCommand() { throw null; }
        new public SqlCommand GetInsertCommand(bool useColumnsForParameterNames) { throw null; }
        new public SqlCommand GetUpdateCommand() { throw null; }
        new public SqlCommand GetUpdateCommand(bool useColumnsForParameterNames) { throw null; }
        new public SqlCommand GetDeleteCommand() { throw null; }
        new public SqlCommand GetDeleteCommand(bool useColumnsForParameterNames) { throw null; }
        protected override void ApplyParameterInfo(System.Data.Common.DbParameter parameter, System.Data.DataRow datarow, System.Data.StatementType statementType, bool whereClause) { }
        protected override string GetParameterName(int parameterOrdinal) { throw null; }
        protected override string GetParameterName(string parameterName) { throw null; }
        protected override string GetParameterPlaceholder(int parameterOrdinal) { throw null; }
        public static void DeriveParameters(SqlCommand command) { }
        protected override DataTable GetSchemaTable(System.Data.Common.DbCommand srcCommand) { throw null; }
        protected override System.Data.Common.DbCommand InitializeCommand(System.Data.Common.DbCommand command) { throw null; }
        public override string QuoteIdentifier(string unquotedIdentifier) { throw null; }
        protected override void SetRowUpdatingHandler(System.Data.Common.DbDataAdapter adapter) { }
        public override string UnquoteIdentifier(string quotedIdentifier) { throw null; }
    }
    public sealed partial class SqlConnection : System.Data.Common.DbConnection, System.ICloneable
    {
        public SqlConnection() { }
        public SqlConnection(string connectionString) { }
        public System.Guid ClientConnectionId { get { throw null; } }
        object ICloneable.Clone() { throw null; }
        public override string ConnectionString { get { throw null; } set { } }
        public override int ConnectionTimeout { get { throw null; } }
        public override string Database { get { throw null; } }
        public override string DataSource { get { throw null; } }
        public bool FireInfoMessageEventOnUserErrors { get { throw null; } set { } }
        public int PacketSize { get { throw null; } }
        public override string ServerVersion { get { throw null; } }
        public override System.Data.ConnectionState State { get { throw null; } }
        public bool StatisticsEnabled { get { throw null; } set { } }
        public string WorkstationId { get { throw null; } }
        public event System.Data.SqlClient.SqlInfoMessageEventHandler InfoMessage { add { } remove { } }
        protected override System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) { throw null; }
        public new System.Data.SqlClient.SqlTransaction BeginTransaction() { throw null; }
        public new System.Data.SqlClient.SqlTransaction BeginTransaction(System.Data.IsolationLevel iso) { throw null; }
        public System.Data.SqlClient.SqlTransaction BeginTransaction(System.Data.IsolationLevel iso, string transactionName) { throw null; }
        public System.Data.SqlClient.SqlTransaction BeginTransaction(string transactionName) { throw null; }
        public override void ChangeDatabase(string database) { }
        public static void ClearAllPools() { }
        public static void ClearPool(System.Data.SqlClient.SqlConnection connection) { }
        public override void Close() { }
        public new System.Data.SqlClient.SqlCommand CreateCommand() { throw null; }
        protected override System.Data.Common.DbCommand CreateDbCommand() { throw null; }
        public override void Open() { }
        public override System.Threading.Tasks.Task OpenAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public void ResetStatistics() { }
        public System.Collections.IDictionary RetrieveStatistics() { throw null; }
    }
    public sealed partial class SqlConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
    {
        public SqlConnectionStringBuilder() { }
        public SqlConnectionStringBuilder(string connectionString) { }
        public System.Data.SqlClient.ApplicationIntent ApplicationIntent { get { throw null; } set { } }
        public string ApplicationName { get { throw null; } set { } }
        public string AttachDBFilename { get { throw null; } set { } }
        public int ConnectRetryCount { get { throw null; } set { } }
        public int ConnectRetryInterval { get { throw null; } set { } }
        public int ConnectTimeout { get { throw null; } set { } }
        public string CurrentLanguage { get { throw null; } set { } }
        public string DataSource { get { throw null; } set { } }
        public bool Encrypt { get { throw null; } set { } }
        public string FailoverPartner { get { throw null; } set { } }
        public string InitialCatalog { get { throw null; } set { } }
        public bool IntegratedSecurity { get { throw null; } set { } }
        public override object this[string keyword] { get { throw null; } set { } }
        public override System.Collections.ICollection Keys { get { throw null; } }
        public int LoadBalanceTimeout { get { throw null; } set { } }
        public int MaxPoolSize { get { throw null; } set { } }
        public int MinPoolSize { get { throw null; } set { } }
        public bool MultipleActiveResultSets { get { throw null; } set { } }
        public bool MultiSubnetFailover { get { throw null; } set { } }
        public int PacketSize { get { throw null; } set { } }
        public string Password { get { throw null; } set { } }
        public bool PersistSecurityInfo { get { throw null; } set { } }
        public bool Pooling { get { throw null; } set { } }
        public bool Replication { get { throw null; } set { } }
        public bool TrustServerCertificate { get { throw null; } set { } }
        public string TypeSystemVersion { get { throw null; } set { } }
        public string UserID { get { throw null; } set { } }
        public bool UserInstance { get { throw null; } set { } }
        public override System.Collections.ICollection Values { get { throw null; } }
        public string WorkstationID { get { throw null; } set { } }
        public override void Clear() { }
        public override bool ContainsKey(string keyword) { throw null; }
        public override bool Remove(string keyword) { throw null; }
        public override bool ShouldSerialize(string keyword) { throw null; }
        public override bool TryGetValue(string keyword, out object value) { throw null; }
    }
    public sealed partial class SqlDataAdapter : System.Data.Common.DbDataAdapter, System.Data.IDbDataAdapter, System.ICloneable
    {
        public SqlDataAdapter() { }
        public SqlDataAdapter(SqlCommand selectCommand) { }
        public SqlDataAdapter(string selectCommandText, string selectConnectionString) { }
        public SqlDataAdapter(string selectCommandText, SqlConnection selectConnection) { }
        new public SqlCommand DeleteCommand { get { throw null; } set { } }
        new public SqlCommand InsertCommand { get { throw null; } set { } }
        new public SqlCommand SelectCommand { get { throw null; } set { } }
        System.Data.IDbCommand System.Data.IDbDataAdapter.DeleteCommand { get { throw null; } set { } }
        System.Data.IDbCommand System.Data.IDbDataAdapter.InsertCommand { get { throw null; } set { } }
        System.Data.IDbCommand System.Data.IDbDataAdapter.SelectCommand { get { throw null; } set { } }
        System.Data.IDbCommand System.Data.IDbDataAdapter.UpdateCommand { get { throw null; } set { } }
        override public int UpdateBatchSize { get { throw null; } set { } }
        new public SqlCommand UpdateCommand { get { throw null; } set { } }
        public event SqlRowUpdatedEventHandler RowUpdated { add { } remove { } }
        public event SqlRowUpdatingEventHandler RowUpdating { add { } remove { } }
        protected override void OnRowUpdated(System.Data.Common.RowUpdatedEventArgs value) { }
        protected override void OnRowUpdating(System.Data.Common.RowUpdatingEventArgs value) { }
        object System.ICloneable.Clone() { throw null; }
    }
    public sealed partial class SqlDependency
    {
        public SqlDependency() { }
        public SqlDependency(SqlCommand command) { }
        public SqlDependency(SqlCommand command, string options, int timeout) { }
        public bool HasChanges { get { throw null; } }
        public string Id { get { throw null; } }
        public event OnChangeEventHandler OnChange { add { } remove { } }
        public void AddCommandDependency(SqlCommand command) { }
        public static bool Start(string connectionString) { throw null; }
        public static bool Start(string connectionString, string queue) { throw null; }
        public static bool Stop(string connectionString) { throw null; }
        public static bool Stop(string connectionString, string queue) { throw null; }
    }
    public delegate void OnChangeEventHandler(object sender, SqlNotificationEventArgs e);
    public partial class SqlNotificationEventArgs : System.EventArgs
    {
        public SqlNotificationEventArgs(SqlNotificationType type, SqlNotificationInfo info, SqlNotificationSource source) { }
        public SqlNotificationType Type { get { throw null; } }
        public SqlNotificationInfo Info { get { throw null; } }
        public SqlNotificationSource Source { get { throw null; } }
    }
    public enum SqlNotificationInfo
    {
        Truncate = 0,
        Insert = 1,
        Update = 2,
        Delete = 3,
        Drop = 4,
        Alter = 5,
        Restart = 6,
        Error = 7,
        Query = 8,
        Invalid = 9,
        Options = 10,
        Isolation = 11,
        Expired = 12,
        Resource = 13,
        PreviousFire = 14,
        TemplateLimit = 15,
        Merge = 16,
        Unknown = -1,
        AlreadyChanged = -2
    }
    public enum SqlNotificationSource
    {
        Data = 0,
        Timeout = 1,
        Object = 2,
        Database = 3,
        System = 4,
        Statement = 5,
        Environment = 6,
        Execution = 7,
        Owner = 8,
        Unknown = -1,
        Client = -2
    }
    public enum SqlNotificationType
    {
        Change = 0,
        Subscribe = 1,
        Unknown = -1
    }
    public sealed partial class SqlRowUpdatedEventArgs : System.Data.Common.RowUpdatedEventArgs
    {
        public SqlRowUpdatedEventArgs(DataRow row, IDbCommand command, StatementType statementType, System.Data.Common.DataTableMapping tableMapping)
        : base(row, command, statementType, tableMapping) { }

        new public SqlCommand Command { get { throw null; } }
    }
    public delegate void SqlRowUpdatedEventHandler(object sender, SqlRowUpdatedEventArgs e);
    public sealed partial class SqlRowUpdatingEventArgs : System.Data.Common.RowUpdatingEventArgs
    {
        public SqlRowUpdatingEventArgs(DataRow row, IDbCommand command, StatementType statementType, System.Data.Common.DataTableMapping tableMapping)
        : base(row, command, statementType, tableMapping) { }

        new public SqlCommand Command { get { throw null; } set { } }
        protected override System.Data.IDbCommand BaseCommand { get { throw null; } set { } }
    }
    public delegate void SqlRowUpdatingEventHandler(object sender, SqlRowUpdatingEventArgs e);
    public partial class SqlDataReader : System.Data.Common.DbDataReader, System.IDisposable
    {
        internal SqlDataReader() { }
        protected System.Data.SqlClient.SqlConnection Connection { get { throw null; } }
        public override int Depth { get { throw null; } }
        public override int FieldCount { get { throw null; } }
        public override bool HasRows { get { throw null; } }
        public override bool IsClosed { get { throw null; } }
        public override object this[int i] { get { throw null; } }
        public override object this[string name] { get { throw null; } }
        public override int RecordsAffected { get { throw null; } }
        public override int VisibleFieldCount { get { throw null; } }
        public override bool GetBoolean(int i) { throw null; }
        public override byte GetByte(int i) { throw null; }
        public override long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) { throw null; }
        public override char GetChar(int i) { throw null; }
        public override long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length) { throw null; }
        public override string GetDataTypeName(int i) { throw null; }
        public override System.DateTime GetDateTime(int i) { throw null; }
        public virtual System.DateTimeOffset GetDateTimeOffset(int i) { throw null; }
        public override decimal GetDecimal(int i) { throw null; }
        public override double GetDouble(int i) { throw null; }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override System.Type GetFieldType(int i) { throw null; }
        public override T GetFieldValue<T>(int i) { throw null; }
        public override System.Threading.Tasks.Task<T> GetFieldValueAsync<T>(int i, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override float GetFloat(int i) { throw null; }
        public override System.Guid GetGuid(int i) { throw null; }
        public override short GetInt16(int i) { throw null; }
        public override int GetInt32(int i) { throw null; }
        public override long GetInt64(int i) { throw null; }
        public override string GetName(int i) { throw null; }
        public override int GetOrdinal(string name) { throw null; }
        public override System.Type GetProviderSpecificFieldType(int i) { throw null; }
        public override object GetProviderSpecificValue(int i) { throw null; }
        public override int GetProviderSpecificValues(object[] values) { throw null; }
        public virtual System.Data.SqlTypes.SqlBinary GetSqlBinary(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlBoolean GetSqlBoolean(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlByte GetSqlByte(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlBytes GetSqlBytes(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlChars GetSqlChars(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlDateTime GetSqlDateTime(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlDecimal GetSqlDecimal(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlDouble GetSqlDouble(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlGuid GetSqlGuid(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlInt16 GetSqlInt16(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlInt32 GetSqlInt32(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlInt64 GetSqlInt64(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlMoney GetSqlMoney(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlSingle GetSqlSingle(int i) { throw null; }
        public virtual System.Data.SqlTypes.SqlString GetSqlString(int i) { throw null; }
        public virtual object GetSqlValue(int i) { throw null; }
        public virtual int GetSqlValues(object[] values) { throw null; }
        public virtual System.Data.SqlTypes.SqlXml GetSqlXml(int i) { throw null; }
        public override System.IO.Stream GetStream(int i) { throw null; }
        public override string GetString(int i) { throw null; }
        public override System.IO.TextReader GetTextReader(int i) { throw null; }
        public virtual System.TimeSpan GetTimeSpan(int i) { throw null; }
        public override object GetValue(int i) { throw null; }
        public override int GetValues(object[] values) { throw null; }
        public virtual System.Xml.XmlReader GetXmlReader(int i) { throw null; }
        public override bool IsDBNull(int i) { throw null; }
        public override System.Threading.Tasks.Task<bool> IsDBNullAsync(int i, System.Threading.CancellationToken cancellationToken) { throw null; }
        public override bool NextResult() { throw null; }
        public override System.Threading.Tasks.Task<bool> NextResultAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
        public override bool Read() { throw null; }
        public override System.Threading.Tasks.Task<bool> ReadAsync(System.Threading.CancellationToken cancellationToken) { throw null; }
    }
    public sealed partial class SqlError
    {
        internal SqlError() { }
        public byte Class { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public string Message { get { throw null; } }
        public int Number { get { throw null; } }
        public string Procedure { get { throw null; } }
        public string Server { get { throw null; } }
        public string Source { get { throw null; } }
        public byte State { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public sealed partial class SqlErrorCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal SqlErrorCollection() { }
        public int Count { get { throw null; } }
        public System.Data.SqlClient.SqlError this[int index] { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.SqlClient.SqlError[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
    }
    public sealed partial class SqlException : System.Data.Common.DbException
    {
        internal SqlException() { }
        public byte Class { get { throw null; } }
        public System.Guid ClientConnectionId { get { throw null; } }
        public System.Data.SqlClient.SqlErrorCollection Errors { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public int Number { get { throw null; } }
        public string Procedure { get { throw null; } }
        public string Server { get { throw null; } }
        public override string Source { get { throw null; } }
        public byte State { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo si, System.Runtime.Serialization.StreamingContext context) { }
        public override string ToString() { throw null; }
    }
    public sealed partial class SqlInfoMessageEventArgs : System.EventArgs
    {
        internal SqlInfoMessageEventArgs() { }
        public System.Data.SqlClient.SqlErrorCollection Errors { get { throw null; } }
        public string Message { get { throw null; } }
        public string Source { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public delegate void SqlInfoMessageEventHandler(object sender, System.Data.SqlClient.SqlInfoMessageEventArgs e);
    public sealed partial class SqlParameter : System.Data.Common.DbParameter, System.ICloneable
    {
        public SqlParameter() { }
        public SqlParameter(string parameterName, System.Data.SqlDbType dbType) { }
        public SqlParameter(string parameterName, System.Data.SqlDbType dbType, int size) { }
        public SqlParameter(string parameterName, System.Data.SqlDbType dbType, int size, string sourceColumn) { }
        public SqlParameter(string parameterName, object value) { }
        public SqlParameter(string parameterName, System.Data.SqlDbType dbType, int size, System.Data.ParameterDirection direction, byte precision, byte scale, string sourceColumn, System.Data.DataRowVersion sourceVersion, bool sourceColumnNullMapping, object value, string xmlSchemaCollectionDatabase, string xmlSchemaCollectionOwningSchema, string xmlSchemaCollectionName) { }
        object ICloneable.Clone() { throw null; }
        public System.Data.SqlTypes.SqlCompareOptions CompareInfo { get { throw null; } set { } }
        public override System.Data.DbType DbType { get { throw null; } set { } }
        public override System.Data.ParameterDirection Direction { get { throw null; } set { } }
        public override bool IsNullable { get { throw null; } set { } }
        public int LocaleId { get { throw null; } set { } }
        public int Offset { get { throw null; } set { } }
        public override string ParameterName { get { throw null; } set { } }
        public new byte Precision { get { throw null; } set { } }
        public new byte Scale { get { throw null; } set { } }
        public override int Size { get { throw null; } set { } }
        public override string SourceColumn { get { throw null; } set { } }
        public override bool SourceColumnNullMapping { get { throw null; } set { } }
        public override DataRowVersion SourceVersion { get { throw null; } set { } }
        public System.Data.SqlDbType SqlDbType { get { throw null; } set { } }
        public object SqlValue { get { throw null; } set { } }
        public string TypeName { get { throw null; } set { } }
        public override object Value { get { throw null; } set { } }
        public string XmlSchemaCollectionDatabase { get { throw null; } set { } }
        public string XmlSchemaCollectionName { get { throw null; } set { } }
        public string XmlSchemaCollectionOwningSchema { get { throw null; } set { } }
        public override void ResetDbType() { }
        public void ResetSqlDbType() { }
        public override string ToString() { throw null; }
    }
    public sealed partial class SqlParameterCollection : System.Data.Common.DbParameterCollection
    {
        internal SqlParameterCollection() { }
        public override int Count { get { throw null; } }
        public new System.Data.SqlClient.SqlParameter this[int index] { get { throw null; } set { } }
        public new System.Data.SqlClient.SqlParameter this[string parameterName] { get { throw null; } set { } }
        public override object SyncRoot { get { throw null; } }
        public System.Data.SqlClient.SqlParameter Add(System.Data.SqlClient.SqlParameter value) { throw null; }
        public override int Add(object value) { throw null; }
        public System.Data.SqlClient.SqlParameter Add(string parameterName, System.Data.SqlDbType sqlDbType) { throw null; }
        public System.Data.SqlClient.SqlParameter Add(string parameterName, System.Data.SqlDbType sqlDbType, int size) { throw null; }
        public override void AddRange(System.Array values) { }
        public void AddRange(System.Data.SqlClient.SqlParameter[] values) { }
        public System.Data.SqlClient.SqlParameter AddWithValue(string parameterName, object value) { throw null; }
        public override void Clear() { }
        public bool Contains(System.Data.SqlClient.SqlParameter value) { throw null; }
        public override bool Contains(object value) { throw null; }
        public override bool Contains(string value) { throw null; }
        public override void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.SqlClient.SqlParameter[] array, int index) { }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        protected override System.Data.Common.DbParameter GetParameter(int index) { throw null; }
        protected override System.Data.Common.DbParameter GetParameter(string parameterName) { throw null; }
        public int IndexOf(System.Data.SqlClient.SqlParameter value) { throw null; }
        public override int IndexOf(object value) { throw null; }
        public override int IndexOf(string parameterName) { throw null; }
        public void Insert(int index, System.Data.SqlClient.SqlParameter value) { }
        public override void Insert(int index, object value) { }
        public void Remove(System.Data.SqlClient.SqlParameter value) { }
        public override void Remove(object value) { }
        public override void RemoveAt(int index) { }
        public override void RemoveAt(string parameterName) { }
        protected override void SetParameter(int index, System.Data.Common.DbParameter value) { }
        protected override void SetParameter(string parameterName, System.Data.Common.DbParameter value) { }
    }
    public partial class SqlRowsCopiedEventArgs : System.EventArgs
    {
        public SqlRowsCopiedEventArgs(long rowsCopied) { }
        public bool Abort { get { throw null; } set { } }
        public long RowsCopied { get { throw null; } }
    }
    public delegate void SqlRowsCopiedEventHandler(object sender, System.Data.SqlClient.SqlRowsCopiedEventArgs e);
    public sealed partial class SqlTransaction : System.Data.Common.DbTransaction
    {
        internal SqlTransaction() { }
        public new System.Data.SqlClient.SqlConnection Connection { get { throw null; } }
        protected override System.Data.Common.DbConnection DbConnection { get { throw null; } }
        public override System.Data.IsolationLevel IsolationLevel { get { throw null; } }
        public override void Commit() { }
        protected override void Dispose(bool disposing) { }
        public override void Rollback() { }
        public void Rollback(string transactionName) { }
        public void Save(string savePointName) { }
    }
}
namespace System.Data
{
    public sealed partial class OperationAbortedException : System.SystemException
    {
        internal OperationAbortedException() { }
    }
}