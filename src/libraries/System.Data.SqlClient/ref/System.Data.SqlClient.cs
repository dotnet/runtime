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
    public partial class SqlDataRecord
    {
        public SqlDataRecord(params Microsoft.SqlServer.Server.SqlMetaData[] metaData) { }
        public virtual int FieldCount { get { return default(int); } }
        public virtual object this[int ordinal] { get { return default(object); } }
        public virtual object this[string name] { get { return default(object); } }
        public virtual bool GetBoolean(int ordinal) { return default(bool); }
        public virtual byte GetByte(int ordinal) { return default(byte); }
        public virtual long GetBytes(int ordinal, long fieldOffset, byte[] buffer, int bufferOffset, int length) { return default(long); }
        public virtual char GetChar(int ordinal) { return default(char); }
        public virtual long GetChars(int ordinal, long fieldOffset, char[] buffer, int bufferOffset, int length) { return default(long); }
        public virtual string GetDataTypeName(int ordinal) { return default(string); }
        public virtual System.DateTime GetDateTime(int ordinal) { return default(System.DateTime); }
        public virtual System.DateTimeOffset GetDateTimeOffset(int ordinal) { return default(System.DateTimeOffset); }
        public virtual decimal GetDecimal(int ordinal) { return default(decimal); }
        public virtual double GetDouble(int ordinal) { return default(double); }
        public virtual System.Type GetFieldType(int ordinal) { return default(System.Type); }
        public virtual float GetFloat(int ordinal) { return default(float); }
        public virtual System.Guid GetGuid(int ordinal) { return default(System.Guid); }
        public virtual short GetInt16(int ordinal) { return default(short); }
        public virtual int GetInt32(int ordinal) { return default(int); }
        public virtual long GetInt64(int ordinal) { return default(long); }
        public virtual string GetName(int ordinal) { return default(string); }
        public virtual int GetOrdinal(string name) { return default(int); }
        public virtual System.Data.SqlTypes.SqlBinary GetSqlBinary(int ordinal) { return default(System.Data.SqlTypes.SqlBinary); }
        public virtual System.Data.SqlTypes.SqlBoolean GetSqlBoolean(int ordinal) { return default(System.Data.SqlTypes.SqlBoolean); }
        public virtual System.Data.SqlTypes.SqlByte GetSqlByte(int ordinal) { return default(System.Data.SqlTypes.SqlByte); }
        public virtual System.Data.SqlTypes.SqlBytes GetSqlBytes(int ordinal) { return default(System.Data.SqlTypes.SqlBytes); }
        public virtual System.Data.SqlTypes.SqlChars GetSqlChars(int ordinal) { return default(System.Data.SqlTypes.SqlChars); }
        public virtual System.Data.SqlTypes.SqlDateTime GetSqlDateTime(int ordinal) { return default(System.Data.SqlTypes.SqlDateTime); }
        public virtual System.Data.SqlTypes.SqlDecimal GetSqlDecimal(int ordinal) { return default(System.Data.SqlTypes.SqlDecimal); }
        public virtual System.Data.SqlTypes.SqlDouble GetSqlDouble(int ordinal) { return default(System.Data.SqlTypes.SqlDouble); }
        public virtual System.Type GetSqlFieldType(int ordinal) { return default(System.Type); }
        public virtual System.Data.SqlTypes.SqlGuid GetSqlGuid(int ordinal) { return default(System.Data.SqlTypes.SqlGuid); }
        public virtual System.Data.SqlTypes.SqlInt16 GetSqlInt16(int ordinal) { return default(System.Data.SqlTypes.SqlInt16); }
        public virtual System.Data.SqlTypes.SqlInt32 GetSqlInt32(int ordinal) { return default(System.Data.SqlTypes.SqlInt32); }
        public virtual System.Data.SqlTypes.SqlInt64 GetSqlInt64(int ordinal) { return default(System.Data.SqlTypes.SqlInt64); }
        public virtual Microsoft.SqlServer.Server.SqlMetaData GetSqlMetaData(int ordinal) { return default(Microsoft.SqlServer.Server.SqlMetaData); }
        public virtual System.Data.SqlTypes.SqlMoney GetSqlMoney(int ordinal) { return default(System.Data.SqlTypes.SqlMoney); }
        public virtual System.Data.SqlTypes.SqlSingle GetSqlSingle(int ordinal) { return default(System.Data.SqlTypes.SqlSingle); }
        public virtual System.Data.SqlTypes.SqlString GetSqlString(int ordinal) { return default(System.Data.SqlTypes.SqlString); }
        public virtual object GetSqlValue(int ordinal) { return default(object); }
        public virtual int GetSqlValues(object[] values) { return default(int); }
        public virtual System.Data.SqlTypes.SqlXml GetSqlXml(int ordinal) { return default(System.Data.SqlTypes.SqlXml); }
        public virtual string GetString(int ordinal) { return default(string); }
        public virtual System.TimeSpan GetTimeSpan(int ordinal) { return default(System.TimeSpan); }
        public virtual object GetValue(int ordinal) { return default(object); }
        public virtual int GetValues(object[] values) { return default(int); }
        public virtual bool IsDBNull(int ordinal) { return default(bool); }
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
        public virtual int SetValues(params object[] values) { return default(int); }
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
        public System.Data.SqlTypes.SqlCompareOptions CompareOptions { get { return default(System.Data.SqlTypes.SqlCompareOptions); } }
        public bool IsUniqueKey { get { return default(bool); } }
        public long LocaleId { get { return default(long); } }
        public static long Max { get { return default(long); } }
        public long MaxLength { get { return default(long); } }
        public string Name { get { return default(string); } }
        public byte Precision { get { return default(byte); } }
        public byte Scale { get { return default(byte); } }
        public System.Data.SqlClient.SortOrder SortOrder { get { return default(System.Data.SqlClient.SortOrder); } }
        public int SortOrdinal { get { return default(int); } }
        public System.Data.SqlDbType SqlDbType { get { return default(System.Data.SqlDbType); } }
        public string TypeName { get { return default(string); } }
        public bool UseServerDefault { get { return default(bool); } }
        public string XmlSchemaCollectionDatabase { get { return default(string); } }
        public string XmlSchemaCollectionName { get { return default(string); } }
        public string XmlSchemaCollectionOwningSchema { get { return default(string); } }
        public bool Adjust(bool value) { return default(bool); }
        public byte Adjust(byte value) { return default(byte); }
        public byte[] Adjust(byte[] value) { return default(byte[]); }
        public char Adjust(char value) { return default(char); }
        public char[] Adjust(char[] value) { return default(char[]); }
        public System.Data.SqlTypes.SqlBinary Adjust(System.Data.SqlTypes.SqlBinary value) { return default(System.Data.SqlTypes.SqlBinary); }
        public System.Data.SqlTypes.SqlBoolean Adjust(System.Data.SqlTypes.SqlBoolean value) { return default(System.Data.SqlTypes.SqlBoolean); }
        public System.Data.SqlTypes.SqlByte Adjust(System.Data.SqlTypes.SqlByte value) { return default(System.Data.SqlTypes.SqlByte); }
        public System.Data.SqlTypes.SqlBytes Adjust(System.Data.SqlTypes.SqlBytes value) { return default(System.Data.SqlTypes.SqlBytes); }
        public System.Data.SqlTypes.SqlChars Adjust(System.Data.SqlTypes.SqlChars value) { return default(System.Data.SqlTypes.SqlChars); }
        public System.Data.SqlTypes.SqlDateTime Adjust(System.Data.SqlTypes.SqlDateTime value) { return default(System.Data.SqlTypes.SqlDateTime); }
        public System.Data.SqlTypes.SqlDecimal Adjust(System.Data.SqlTypes.SqlDecimal value) { return default(System.Data.SqlTypes.SqlDecimal); }
        public System.Data.SqlTypes.SqlDouble Adjust(System.Data.SqlTypes.SqlDouble value) { return default(System.Data.SqlTypes.SqlDouble); }
        public System.Data.SqlTypes.SqlGuid Adjust(System.Data.SqlTypes.SqlGuid value) { return default(System.Data.SqlTypes.SqlGuid); }
        public System.Data.SqlTypes.SqlInt16 Adjust(System.Data.SqlTypes.SqlInt16 value) { return default(System.Data.SqlTypes.SqlInt16); }
        public System.Data.SqlTypes.SqlInt32 Adjust(System.Data.SqlTypes.SqlInt32 value) { return default(System.Data.SqlTypes.SqlInt32); }
        public System.Data.SqlTypes.SqlInt64 Adjust(System.Data.SqlTypes.SqlInt64 value) { return default(System.Data.SqlTypes.SqlInt64); }
        public System.Data.SqlTypes.SqlMoney Adjust(System.Data.SqlTypes.SqlMoney value) { return default(System.Data.SqlTypes.SqlMoney); }
        public System.Data.SqlTypes.SqlSingle Adjust(System.Data.SqlTypes.SqlSingle value) { return default(System.Data.SqlTypes.SqlSingle); }
        public System.Data.SqlTypes.SqlString Adjust(System.Data.SqlTypes.SqlString value) { return default(System.Data.SqlTypes.SqlString); }
        public System.Data.SqlTypes.SqlXml Adjust(System.Data.SqlTypes.SqlXml value) { return default(System.Data.SqlTypes.SqlXml); }
        public System.DateTime Adjust(System.DateTime value) { return default(System.DateTime); }
        public System.DateTimeOffset Adjust(System.DateTimeOffset value) { return default(System.DateTimeOffset); }
        public decimal Adjust(decimal value) { return default(decimal); }
        public double Adjust(double value) { return default(double); }
        public System.Guid Adjust(System.Guid value) { return default(System.Guid); }
        public short Adjust(short value) { return default(short); }
        public int Adjust(int value) { return default(int); }
        public long Adjust(long value) { return default(long); }
        public object Adjust(object value) { return default(object); }
        public float Adjust(float value) { return default(float); }
        public string Adjust(string value) { return default(string); }
        public System.TimeSpan Adjust(System.TimeSpan value) { return default(System.TimeSpan); }
        public static Microsoft.SqlServer.Server.SqlMetaData InferFromValue(object value, string name) { return default(Microsoft.SqlServer.Server.SqlMetaData); }
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
        public int BatchSize { get { return default(int); } set { } }
        public int BulkCopyTimeout { get { return default(int); } set { } }
        public System.Data.SqlClient.SqlBulkCopyColumnMappingCollection ColumnMappings { get { return default(System.Data.SqlClient.SqlBulkCopyColumnMappingCollection); } }
        public string DestinationTableName { get { return default(string); } set { } }
        public bool EnableStreaming { get { return default(bool); } set { } }
        public int NotifyAfter { get { return default(int); } set { } }
        public event System.Data.SqlClient.SqlRowsCopiedEventHandler SqlRowsCopied { add { } remove { } }
        public void Close() { }
        void System.IDisposable.Dispose() { }
        public void WriteToServer(System.Data.Common.DbDataReader reader) { }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.Common.DbDataReader reader) { return default(System.Threading.Tasks.Task); }
        public System.Threading.Tasks.Task WriteToServerAsync(System.Data.Common.DbDataReader reader, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
    }
    public sealed partial class SqlBulkCopyColumnMapping
    {
        public SqlBulkCopyColumnMapping() { }
        public SqlBulkCopyColumnMapping(int sourceColumnOrdinal, int destinationOrdinal) { }
        public SqlBulkCopyColumnMapping(int sourceColumnOrdinal, string destinationColumn) { }
        public SqlBulkCopyColumnMapping(string sourceColumn, int destinationOrdinal) { }
        public SqlBulkCopyColumnMapping(string sourceColumn, string destinationColumn) { }
        public string DestinationColumn { get { return default(string); } set { } }
        public int DestinationOrdinal { get { return default(int); } set { } }
        public string SourceColumn { get { return default(string); } set { } }
        public int SourceOrdinal { get { return default(int); } set { } }
    }
    public sealed partial class SqlBulkCopyColumnMappingCollection
    {
        internal SqlBulkCopyColumnMappingCollection() { }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping this[int index] { get { return default(System.Data.SqlClient.SqlBulkCopyColumnMapping); } }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(System.Data.SqlClient.SqlBulkCopyColumnMapping bulkCopyColumnMapping) { return default(System.Data.SqlClient.SqlBulkCopyColumnMapping); }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(int sourceColumnIndex, int destinationColumnIndex) { return default(System.Data.SqlClient.SqlBulkCopyColumnMapping); }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(int sourceColumnIndex, string destinationColumn) { return default(System.Data.SqlClient.SqlBulkCopyColumnMapping); }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(string sourceColumn, int destinationColumnIndex) { return default(System.Data.SqlClient.SqlBulkCopyColumnMapping); }
        public System.Data.SqlClient.SqlBulkCopyColumnMapping Add(string sourceColumn, string destinationColumn) { return default(System.Data.SqlClient.SqlBulkCopyColumnMapping); }
        public bool Contains(System.Data.SqlClient.SqlBulkCopyColumnMapping value) { return default(bool); }
        public void CopyTo(System.Data.SqlClient.SqlBulkCopyColumnMapping[] array, int index) { }
        public int IndexOf(System.Data.SqlClient.SqlBulkCopyColumnMapping value) { return default(int); }
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
        public override System.Data.Common.DbCommand CreateCommand() { return default(System.Data.Common.DbCommand); }
        public override System.Data.Common.DbConnection CreateConnection() { return default(System.Data.Common.DbConnection); }
        public override System.Data.Common.DbConnectionStringBuilder CreateConnectionStringBuilder() { return default(System.Data.Common.DbConnectionStringBuilder); }
        public override System.Data.Common.DbParameter CreateParameter() { return default(System.Data.Common.DbParameter); }
    }
    public sealed partial class SqlCommand : System.Data.Common.DbCommand
    {
        public SqlCommand() { }
        public SqlCommand(string cmdText) { }
        public SqlCommand(string cmdText, System.Data.SqlClient.SqlConnection connection) { }
        public SqlCommand(string cmdText, System.Data.SqlClient.SqlConnection connection, System.Data.SqlClient.SqlTransaction transaction) { }
        public override string CommandText { get { return default(string); } set { } }
        public override int CommandTimeout { get { return default(int); } set { } }
        public override System.Data.CommandType CommandType { get { return default(System.Data.CommandType); } set { } }
        public new System.Data.SqlClient.SqlConnection Connection { get { return default(System.Data.SqlClient.SqlConnection); } set { } }
        protected override System.Data.Common.DbConnection DbConnection { get { return default(System.Data.Common.DbConnection); } set { } }
        protected override System.Data.Common.DbParameterCollection DbParameterCollection { get { return default(System.Data.Common.DbParameterCollection); } }
        protected override System.Data.Common.DbTransaction DbTransaction { get { return default(System.Data.Common.DbTransaction); } set { } }
        public override bool DesignTimeVisible { get { return default(bool); } set { } }
        public new System.Data.SqlClient.SqlParameterCollection Parameters { get { return default(System.Data.SqlClient.SqlParameterCollection); } }
        public new System.Data.SqlClient.SqlTransaction Transaction { get { return default(System.Data.SqlClient.SqlTransaction); } set { } }
        public override System.Data.UpdateRowSource UpdatedRowSource { get { return default(System.Data.UpdateRowSource); } set { } }
        public event System.Data.StatementCompletedEventHandler StatementCompleted { add { } remove { } }
        public override void Cancel() { }
        protected override System.Data.Common.DbParameter CreateDbParameter() { return default(System.Data.Common.DbParameter); }
        public new System.Data.SqlClient.SqlParameter CreateParameter() { return default(System.Data.SqlClient.SqlParameter); }
        protected override System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior) { return default(System.Data.Common.DbDataReader); }
        protected override System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteDbDataReaderAsync(System.Data.CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Data.Common.DbDataReader>); }
        public override int ExecuteNonQuery() { return default(int); }
        public override System.Threading.Tasks.Task<int> ExecuteNonQueryAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public new System.Data.SqlClient.SqlDataReader ExecuteReader() { return default(System.Data.SqlClient.SqlDataReader); }
        public new System.Data.SqlClient.SqlDataReader ExecuteReader(System.Data.CommandBehavior behavior) { return default(System.Data.SqlClient.SqlDataReader); }
        public new System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader> ExecuteReaderAsync() { return default(System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader>); }
        public new System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader> ExecuteReaderAsync(System.Data.CommandBehavior behavior) { return default(System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader>); }
        public new System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader> ExecuteReaderAsync(System.Data.CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader>); }
        public new System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader> ExecuteReaderAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Data.SqlClient.SqlDataReader>); }
        public override object ExecuteScalar() { return default(object); }
        public override System.Threading.Tasks.Task<object> ExecuteScalarAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<object>); }
        public System.Xml.XmlReader ExecuteXmlReader() { return default(System.Xml.XmlReader); }
        public System.Threading.Tasks.Task<System.Xml.XmlReader> ExecuteXmlReaderAsync() { return default(System.Threading.Tasks.Task<System.Xml.XmlReader>); }
        public System.Threading.Tasks.Task<System.Xml.XmlReader> ExecuteXmlReaderAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Xml.XmlReader>); }
        public override void Prepare() { }
    }
    public sealed partial class SqlConnection : System.Data.Common.DbConnection
    {
        public SqlConnection() { }
        public SqlConnection(string connectionString) { }
        public System.Guid ClientConnectionId { get { return default(System.Guid); } }
        public override string ConnectionString { get { return default(string); } set { } }
        public override int ConnectionTimeout { get { return default(int); } }
        public override string Database { get { return default(string); } }
        public override string DataSource { get { return default(string); } }
        public bool FireInfoMessageEventOnUserErrors { get { return default(bool); } set { } }
        public int PacketSize { get { return default(int); } }
        public override string ServerVersion { get { return default(string); } }
        public override System.Data.ConnectionState State { get { return default(System.Data.ConnectionState); } }
        public bool StatisticsEnabled { get { return default(bool); } set { } }
        public string WorkstationId { get { return default(string); } }
        public event System.Data.SqlClient.SqlInfoMessageEventHandler InfoMessage { add { } remove { } }
        protected override System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) { return default(System.Data.Common.DbTransaction); }
        public new System.Data.SqlClient.SqlTransaction BeginTransaction() { return default(System.Data.SqlClient.SqlTransaction); }
        public new System.Data.SqlClient.SqlTransaction BeginTransaction(System.Data.IsolationLevel iso) { return default(System.Data.SqlClient.SqlTransaction); }
        public System.Data.SqlClient.SqlTransaction BeginTransaction(System.Data.IsolationLevel iso, string transactionName) { return default(System.Data.SqlClient.SqlTransaction); }
        public System.Data.SqlClient.SqlTransaction BeginTransaction(string transactionName) { return default(System.Data.SqlClient.SqlTransaction); }
        public override void ChangeDatabase(string database) { }
        public static void ClearAllPools() { }
        public static void ClearPool(System.Data.SqlClient.SqlConnection connection) { }
        public override void Close() { }
        public new System.Data.SqlClient.SqlCommand CreateCommand() { return default(System.Data.SqlClient.SqlCommand); }
        protected override System.Data.Common.DbCommand CreateDbCommand() { return default(System.Data.Common.DbCommand); }
        public override void Open() { }
        public override System.Threading.Tasks.Task OpenAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        public void ResetStatistics() { }
        public System.Collections.IDictionary RetrieveStatistics() { return default(System.Collections.IDictionary); }
    }
    public sealed partial class SqlConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
    {
        public SqlConnectionStringBuilder() { }
        public SqlConnectionStringBuilder(string connectionString) { }
        public System.Data.SqlClient.ApplicationIntent ApplicationIntent { get { return default(System.Data.SqlClient.ApplicationIntent); } set { } }
        public string ApplicationName { get { return default(string); } set { } }
        public string AttachDBFilename { get { return default(string); } set { } }
        public int ConnectRetryCount { get { return default(int); } set { } }
        public int ConnectRetryInterval { get { return default(int); } set { } }
        public int ConnectTimeout { get { return default(int); } set { } }
        public string CurrentLanguage { get { return default(string); } set { } }
        public string DataSource { get { return default(string); } set { } }
        public bool Encrypt { get { return default(bool); } set { } }
        public string FailoverPartner { get { return default(string); } set { } }
        public string InitialCatalog { get { return default(string); } set { } }
        public bool IntegratedSecurity { get { return default(bool); } set { } }
        public override object this[string keyword] { get { return default(object); } set { } }
        public override System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        public int LoadBalanceTimeout { get { return default(int); } set { } }
        public int MaxPoolSize { get { return default(int); } set { } }
        public int MinPoolSize { get { return default(int); } set { } }
        public bool MultipleActiveResultSets { get { return default(bool); } set { } }
        public bool MultiSubnetFailover { get { return default(bool); } set { } }
        public int PacketSize { get { return default(int); } set { } }
        public string Password { get { return default(string); } set { } }
        public bool PersistSecurityInfo { get { return default(bool); } set { } }
        public bool Pooling { get { return default(bool); } set { } }
        public bool Replication { get { return default(bool); } set { } }
        public bool TrustServerCertificate { get { return default(bool); } set { } }
        public string TypeSystemVersion { get { return default(string); } set { } }
        public string UserID { get { return default(string); } set { } }
        public bool UserInstance { get { return default(bool); } set { } }
        public override System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public string WorkstationID { get { return default(string); } set { } }
        public override void Clear() { }
        public override bool ContainsKey(string keyword) { return default(bool); }
        public override bool Remove(string keyword) { return default(bool); }
        public override bool ShouldSerialize(string keyword) { return default(bool); }
        public override bool TryGetValue(string keyword, out object value) { value = default(object); return default(bool); }
    }
    public partial class SqlDataReader : System.Data.Common.DbDataReader, System.IDisposable
    {
        internal SqlDataReader() { }
        protected System.Data.SqlClient.SqlConnection Connection { get { return default(System.Data.SqlClient.SqlConnection); } }
        public override int Depth { get { return default(int); } }
        public override int FieldCount { get { return default(int); } }
        public override bool HasRows { get { return default(bool); } }
        public override bool IsClosed { get { return default(bool); } }
        public override object this[int i] { get { return default(object); } }
        public override object this[string name] { get { return default(object); } }
        public override int RecordsAffected { get { return default(int); } }
        public override int VisibleFieldCount { get { return default(int); } }
        public override bool GetBoolean(int i) { return default(bool); }
        public override byte GetByte(int i) { return default(byte); }
        public override long GetBytes(int i, long dataIndex, byte[] buffer, int bufferIndex, int length) { return default(long); }
        public override char GetChar(int i) { return default(char); }
        public override long GetChars(int i, long dataIndex, char[] buffer, int bufferIndex, int length) { return default(long); }
        public override string GetDataTypeName(int i) { return default(string); }
        public override System.DateTime GetDateTime(int i) { return default(System.DateTime); }
        public virtual System.DateTimeOffset GetDateTimeOffset(int i) { return default(System.DateTimeOffset); }
        public override decimal GetDecimal(int i) { return default(decimal); }
        public override double GetDouble(int i) { return default(double); }
        public override System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public override System.Type GetFieldType(int i) { return default(System.Type); }
        public override T GetFieldValue<T>(int i) { return default(T); }
        public override System.Threading.Tasks.Task<T> GetFieldValueAsync<T>(int i, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<T>); }
        public override float GetFloat(int i) { return default(float); }
        public override System.Guid GetGuid(int i) { return default(System.Guid); }
        public override short GetInt16(int i) { return default(short); }
        public override int GetInt32(int i) { return default(int); }
        public override long GetInt64(int i) { return default(long); }
        public override string GetName(int i) { return default(string); }
        public override int GetOrdinal(string name) { return default(int); }
        public override System.Type GetProviderSpecificFieldType(int i) { return default(System.Type); }
        public override object GetProviderSpecificValue(int i) { return default(object); }
        public override int GetProviderSpecificValues(object[] values) { return default(int); }
        public virtual System.Data.SqlTypes.SqlBinary GetSqlBinary(int i) { return default(System.Data.SqlTypes.SqlBinary); }
        public virtual System.Data.SqlTypes.SqlBoolean GetSqlBoolean(int i) { return default(System.Data.SqlTypes.SqlBoolean); }
        public virtual System.Data.SqlTypes.SqlByte GetSqlByte(int i) { return default(System.Data.SqlTypes.SqlByte); }
        public virtual System.Data.SqlTypes.SqlBytes GetSqlBytes(int i) { return default(System.Data.SqlTypes.SqlBytes); }
        public virtual System.Data.SqlTypes.SqlChars GetSqlChars(int i) { return default(System.Data.SqlTypes.SqlChars); }
        public virtual System.Data.SqlTypes.SqlDateTime GetSqlDateTime(int i) { return default(System.Data.SqlTypes.SqlDateTime); }
        public virtual System.Data.SqlTypes.SqlDecimal GetSqlDecimal(int i) { return default(System.Data.SqlTypes.SqlDecimal); }
        public virtual System.Data.SqlTypes.SqlDouble GetSqlDouble(int i) { return default(System.Data.SqlTypes.SqlDouble); }
        public virtual System.Data.SqlTypes.SqlGuid GetSqlGuid(int i) { return default(System.Data.SqlTypes.SqlGuid); }
        public virtual System.Data.SqlTypes.SqlInt16 GetSqlInt16(int i) { return default(System.Data.SqlTypes.SqlInt16); }
        public virtual System.Data.SqlTypes.SqlInt32 GetSqlInt32(int i) { return default(System.Data.SqlTypes.SqlInt32); }
        public virtual System.Data.SqlTypes.SqlInt64 GetSqlInt64(int i) { return default(System.Data.SqlTypes.SqlInt64); }
        public virtual System.Data.SqlTypes.SqlMoney GetSqlMoney(int i) { return default(System.Data.SqlTypes.SqlMoney); }
        public virtual System.Data.SqlTypes.SqlSingle GetSqlSingle(int i) { return default(System.Data.SqlTypes.SqlSingle); }
        public virtual System.Data.SqlTypes.SqlString GetSqlString(int i) { return default(System.Data.SqlTypes.SqlString); }
        public virtual object GetSqlValue(int i) { return default(object); }
        public virtual int GetSqlValues(object[] values) { return default(int); }
        public virtual System.Data.SqlTypes.SqlXml GetSqlXml(int i) { return default(System.Data.SqlTypes.SqlXml); }
        public override System.IO.Stream GetStream(int i) { return default(System.IO.Stream); }
        public override string GetString(int i) { return default(string); }
        public override System.IO.TextReader GetTextReader(int i) { return default(System.IO.TextReader); }
        public virtual System.TimeSpan GetTimeSpan(int i) { return default(System.TimeSpan); }
        public override object GetValue(int i) { return default(object); }
        public override int GetValues(object[] values) { return default(int); }
        public virtual System.Xml.XmlReader GetXmlReader(int i) { return default(System.Xml.XmlReader); }
        public override bool IsDBNull(int i) { return default(bool); }
        public override System.Threading.Tasks.Task<bool> IsDBNullAsync(int i, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<bool>); }
        public override bool NextResult() { return default(bool); }
        public override System.Threading.Tasks.Task<bool> NextResultAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<bool>); }
        public override bool Read() { return default(bool); }
        public override System.Threading.Tasks.Task<bool> ReadAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<bool>); }
    }
    public sealed partial class SqlError
    {
        internal SqlError() { }
        public byte Class { get { return default(byte); } }
        public int LineNumber { get { return default(int); } }
        public string Message { get { return default(string); } }
        public int Number { get { return default(int); } }
        public string Procedure { get { return default(string); } }
        public string Server { get { return default(string); } }
        public string Source { get { return default(string); } }
        public byte State { get { return default(byte); } }
        public override string ToString() { return default(string); }
    }
    public sealed partial class SqlErrorCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal SqlErrorCollection() { }
        public int Count { get { return default(int); } }
        public System.Data.SqlClient.SqlError this[int index] { get { return default(System.Data.SqlClient.SqlError); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        public void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.SqlClient.SqlError[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
    }
    public sealed partial class SqlException : System.Data.Common.DbException
    {
        internal SqlException() { }
        public byte Class { get { return default(byte); } }
        public System.Guid ClientConnectionId { get { return default(System.Guid); } }
        public System.Data.SqlClient.SqlErrorCollection Errors { get { return default(System.Data.SqlClient.SqlErrorCollection); } }
        public int LineNumber { get { return default(int); } }
        public int Number { get { return default(int); } }
        public string Procedure { get { return default(string); } }
        public string Server { get { return default(string); } }
        public override string Source { get { return default(string); } }
        public byte State { get { return default(byte); } }
        public override string ToString() { return default(string); }
    }
    public sealed partial class SqlInfoMessageEventArgs : System.EventArgs
    {
        internal SqlInfoMessageEventArgs() { }
        public System.Data.SqlClient.SqlErrorCollection Errors { get { return default(System.Data.SqlClient.SqlErrorCollection); } }
        public string Message { get { return default(string); } }
        public string Source { get { return default(string); } }
        public override string ToString() { return default(string); }
    }
    public delegate void SqlInfoMessageEventHandler(object sender, System.Data.SqlClient.SqlInfoMessageEventArgs e);
    public sealed partial class SqlParameter : System.Data.Common.DbParameter
    {
        public SqlParameter() { }
        public SqlParameter(string parameterName, System.Data.SqlDbType dbType) { }
        public SqlParameter(string parameterName, System.Data.SqlDbType dbType, int size) { }
        public SqlParameter(string parameterName, System.Data.SqlDbType dbType, int size, string sourceColumn) { }
        public SqlParameter(string parameterName, object value) { }
        public System.Data.SqlTypes.SqlCompareOptions CompareInfo { get { return default(System.Data.SqlTypes.SqlCompareOptions); } set { } }
        public override System.Data.DbType DbType { get { return default(System.Data.DbType); } set { } }
        public override System.Data.ParameterDirection Direction { get { return default(System.Data.ParameterDirection); } set { } }
        public override bool IsNullable { get { return default(bool); } set { } }
        public int LocaleId { get { return default(int); } set { } }
        public int Offset { get { return default(int); } set { } }
        public override string ParameterName { get { return default(string); } set { } }
        public new byte Precision { get { return default(byte); } set { } }
        public new byte Scale { get { return default(byte); } set { } }
        public override int Size { get { return default(int); } set { } }
        public override string SourceColumn { get { return default(string); } set { } }
        public override bool SourceColumnNullMapping { get { return default(bool); } set { } }
        public System.Data.SqlDbType SqlDbType { get { return default(System.Data.SqlDbType); } set { } }
        public object SqlValue { get { return default(object); } set { } }
        public string TypeName { get { return default(string); } set { } }
        public override object Value { get { return default(object); } set { } }
        public string XmlSchemaCollectionDatabase { get { return default(string); } set { } }
        public string XmlSchemaCollectionName { get { return default(string); } set { } }
        public string XmlSchemaCollectionOwningSchema { get { return default(string); } set { } }
        public override void ResetDbType() { }
        public void ResetSqlDbType() { }
        public override string ToString() { return default(string); }
    }
    public sealed partial class SqlParameterCollection : System.Data.Common.DbParameterCollection
    {
        internal SqlParameterCollection() { }
        public override int Count { get { return default(int); } }
        public new System.Data.SqlClient.SqlParameter this[int index] { get { return default(System.Data.SqlClient.SqlParameter); } set { } }
        public new System.Data.SqlClient.SqlParameter this[string parameterName] { get { return default(System.Data.SqlClient.SqlParameter); } set { } }
        public override object SyncRoot { get { return default(object); } }
        public System.Data.SqlClient.SqlParameter Add(System.Data.SqlClient.SqlParameter value) { return default(System.Data.SqlClient.SqlParameter); }
        public override int Add(object value) { return default(int); }
        public System.Data.SqlClient.SqlParameter Add(string parameterName, System.Data.SqlDbType sqlDbType) { return default(System.Data.SqlClient.SqlParameter); }
        public System.Data.SqlClient.SqlParameter Add(string parameterName, System.Data.SqlDbType sqlDbType, int size) { return default(System.Data.SqlClient.SqlParameter); }
        public override void AddRange(System.Array values) { }
        public void AddRange(System.Data.SqlClient.SqlParameter[] values) { }
        public System.Data.SqlClient.SqlParameter AddWithValue(string parameterName, object value) { return default(System.Data.SqlClient.SqlParameter); }
        public override void Clear() { }
        public bool Contains(System.Data.SqlClient.SqlParameter value) { return default(bool); }
        public override bool Contains(object value) { return default(bool); }
        public override bool Contains(string value) { return default(bool); }
        public override void CopyTo(System.Array array, int index) { }
        public void CopyTo(System.Data.SqlClient.SqlParameter[] array, int index) { }
        public override System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        protected override System.Data.Common.DbParameter GetParameter(int index) { return default(System.Data.Common.DbParameter); }
        protected override System.Data.Common.DbParameter GetParameter(string parameterName) { return default(System.Data.Common.DbParameter); }
        public int IndexOf(System.Data.SqlClient.SqlParameter value) { return default(int); }
        public override int IndexOf(object value) { return default(int); }
        public override int IndexOf(string parameterName) { return default(int); }
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
        public bool Abort { get { return default(bool); } set { } }
        public long RowsCopied { get { return default(long); } }
    }
    public delegate void SqlRowsCopiedEventHandler(object sender, System.Data.SqlClient.SqlRowsCopiedEventArgs e);
    public sealed partial class SqlTransaction : System.Data.Common.DbTransaction
    {
        internal SqlTransaction() { }
        public new System.Data.SqlClient.SqlConnection Connection { get { return default(System.Data.SqlClient.SqlConnection); } }
        protected override System.Data.Common.DbConnection DbConnection { get { return default(System.Data.Common.DbConnection); } }
        public override System.Data.IsolationLevel IsolationLevel { get { return default(System.Data.IsolationLevel); } }
        public override void Commit() { }
        protected override void Dispose(bool disposing) { }
        public override void Rollback() { }
        public void Rollback(string transactionName) { }
        public void Save(string savePointName) { }
    }
}
