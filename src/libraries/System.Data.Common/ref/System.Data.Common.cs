// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System
{
    public sealed partial class DBNull
    {
        internal DBNull() { }
        public static readonly System.DBNull Value;
        public override string ToString() { return default(string); }
        public string ToString(System.IFormatProvider provider) { return default(string); }
    }
}
namespace System.Data
{
    [System.FlagsAttribute]
    public enum CommandBehavior
    {
        CloseConnection = 32,
        Default = 0,
        KeyInfo = 4,
        SchemaOnly = 2,
        SequentialAccess = 16,
        SingleResult = 1,
        SingleRow = 8,
    }
    public enum CommandType
    {
        StoredProcedure = 4,
        TableDirect = 512,
        Text = 1,
    }
    [System.FlagsAttribute]
    public enum ConnectionState
    {
        Broken = 16,
        Closed = 0,
        Connecting = 2,
        Executing = 4,
        Fetching = 8,
        Open = 1,
    }
    public enum DataRowVersion
    {
        Default = 1536,
    }
    public partial class DataTable
    {
        internal DataTable() { }
    }
    public enum DbType
    {
        AnsiString = 0,
        AnsiStringFixedLength = 22,
        Binary = 1,
        Boolean = 3,
        Byte = 2,
        Currency = 4,
        Date = 5,
        DateTime = 6,
        DateTime2 = 26,
        DateTimeOffset = 27,
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
        StringFixedLength = 23,
        Time = 17,
        UInt16 = 18,
        UInt32 = 19,
        UInt64 = 20,
        VarNumeric = 21,
        Xml = 25,
    }
    public partial interface IDataParameter
    {
        System.Data.DbType DbType { get; set; }
        System.Data.ParameterDirection Direction { get; set; }
        bool IsNullable { get; }
        string ParameterName { get; set; }
        string SourceColumn { get; set; }
        System.Data.DataRowVersion SourceVersion { get; set; }
        object Value { get; set; }
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
        string CommandText { get; set; }
        int CommandTimeout { get; set; }
        System.Data.CommandType CommandType { get; set; }
        System.Data.IDbConnection Connection { get; set; }
        System.Data.IDataParameterCollection Parameters { get; }
        System.Data.IDbTransaction Transaction { get; set; }
        System.Data.UpdateRowSource UpdatedRowSource { get; set; }
        void Cancel();
        System.Data.IDbDataParameter CreateParameter();
        int ExecuteNonQuery();
        System.Data.IDataReader ExecuteReader();
        System.Data.IDataReader ExecuteReader(System.Data.CommandBehavior behavior);
        object ExecuteScalar();
        void Prepare();
    }
    public partial interface IDbConnection : System.IDisposable
    {
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
    public partial interface IDbDataParameter : System.Data.IDataParameter
    {
        byte Precision { get; set; }
        byte Scale { get; set; }
        int Size { get; set; }
    }
    public partial interface IDbTransaction : System.IDisposable
    {
        System.Data.IDbConnection Connection { get; }
        System.Data.IsolationLevel IsolationLevel { get; }
        void Commit();
        void Rollback();
    }
    public enum IsolationLevel
    {
        Chaos = 16,
        ReadCommitted = 4096,
        ReadUncommitted = 256,
        RepeatableRead = 65536,
        Serializable = 1048576,
        Snapshot = 16777216,
        Unspecified = -1,
    }
    public enum ParameterDirection
    {
        Input = 1,
        InputOutput = 3,
        Output = 2,
        ReturnValue = 6,
    }
    public sealed partial class StateChangeEventArgs : System.EventArgs
    {
        public StateChangeEventArgs(System.Data.ConnectionState originalState, System.Data.ConnectionState currentState) { }
        public System.Data.ConnectionState CurrentState { get { return default(System.Data.ConnectionState); } }
        public System.Data.ConnectionState OriginalState { get { return default(System.Data.ConnectionState); } }
    }
    public delegate void StateChangeEventHandler(object sender, System.Data.StateChangeEventArgs e);
    public enum UpdateRowSource
    {
        Both = 3,
        FirstReturnedRecord = 2,
        None = 0,
        OutputParameters = 1,
    }
}
namespace System.Data.Common
{
    public abstract partial class DbCommand : System.Data.IDbCommand, System.IDisposable
    {
        protected DbCommand() { }
        public abstract string CommandText { get; set; }
        public abstract int CommandTimeout { get; set; }
        public abstract System.Data.CommandType CommandType { get; set; }
        public System.Data.Common.DbConnection Connection { get { return default(System.Data.Common.DbConnection); } set { } }
        protected abstract System.Data.Common.DbConnection DbConnection { get; set; }
        protected abstract System.Data.Common.DbParameterCollection DbParameterCollection { get; }
        protected abstract System.Data.Common.DbTransaction DbTransaction { get; set; }
        public abstract bool DesignTimeVisible { get; set; }
        public System.Data.Common.DbParameterCollection Parameters { get { return default(System.Data.Common.DbParameterCollection); } }
        System.Data.IDbConnection System.Data.IDbCommand.Connection { get { return default(System.Data.IDbConnection); } set { } }
        System.Data.IDataParameterCollection System.Data.IDbCommand.Parameters { get { return default(System.Data.IDataParameterCollection); } }
        System.Data.IDbTransaction System.Data.IDbCommand.Transaction { get { return default(System.Data.IDbTransaction); } set { } }
        public System.Data.Common.DbTransaction Transaction { get { return default(System.Data.Common.DbTransaction); } set { } }
        public abstract System.Data.UpdateRowSource UpdatedRowSource { get; set; }
        public abstract void Cancel();
        protected abstract System.Data.Common.DbParameter CreateDbParameter();
        public System.Data.Common.DbParameter CreateParameter() { return default(System.Data.Common.DbParameter); }
        protected abstract System.Data.Common.DbDataReader ExecuteDbDataReader(System.Data.CommandBehavior behavior);
        protected virtual System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteDbDataReaderAsync(System.Data.CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Data.Common.DbDataReader>); }
        public abstract int ExecuteNonQuery();
        public System.Threading.Tasks.Task<int> ExecuteNonQueryAsync() { return default(System.Threading.Tasks.Task<int>); }
        public virtual System.Threading.Tasks.Task<int> ExecuteNonQueryAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<int>); }
        public System.Data.Common.DbDataReader ExecuteReader() { return default(System.Data.Common.DbDataReader); }
        public System.Data.Common.DbDataReader ExecuteReader(System.Data.CommandBehavior behavior) { return default(System.Data.Common.DbDataReader); }
        public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync() { return default(System.Threading.Tasks.Task<System.Data.Common.DbDataReader>); }
        public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(System.Data.CommandBehavior behavior) { return default(System.Threading.Tasks.Task<System.Data.Common.DbDataReader>); }
        public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(System.Data.CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Data.Common.DbDataReader>); }
        public System.Threading.Tasks.Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<System.Data.Common.DbDataReader>); }
        public abstract object ExecuteScalar();
        public System.Threading.Tasks.Task<object> ExecuteScalarAsync() { return default(System.Threading.Tasks.Task<object>); }
        public virtual System.Threading.Tasks.Task<object> ExecuteScalarAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<object>); }
        public abstract void Prepare();
        System.Data.IDbDataParameter System.Data.IDbCommand.CreateParameter() { return default(System.Data.IDbDataParameter); }
        System.Data.IDataReader System.Data.IDbCommand.ExecuteReader() { return default(System.Data.IDataReader); }
        System.Data.IDataReader System.Data.IDbCommand.ExecuteReader(System.Data.CommandBehavior behavior) { return default(System.Data.IDataReader); }
    }
    public abstract partial class DbConnection : System.Data.IDbConnection, System.IDisposable
    {
        protected DbConnection() { }
        public abstract string ConnectionString { get; set; }
        public virtual int ConnectionTimeout { get { return default(int); } }
        public abstract string Database { get; }
        public abstract string DataSource { get; }
        public abstract string ServerVersion { get; }
        public abstract System.Data.ConnectionState State { get; }
        public virtual event System.Data.StateChangeEventHandler StateChange { add { } remove { } }
        protected abstract System.Data.Common.DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel);
        public System.Data.Common.DbTransaction BeginTransaction() { return default(System.Data.Common.DbTransaction); }
        public System.Data.Common.DbTransaction BeginTransaction(System.Data.IsolationLevel isolationLevel) { return default(System.Data.Common.DbTransaction); }
        public abstract void ChangeDatabase(string databaseName);
        public abstract void Close();
        public System.Data.Common.DbCommand CreateCommand() { return default(System.Data.Common.DbCommand); }
        protected abstract System.Data.Common.DbCommand CreateDbCommand();
        protected virtual void OnStateChange(System.Data.StateChangeEventArgs stateChange) { }
        public abstract void Open();
        public System.Threading.Tasks.Task OpenAsync() { return default(System.Threading.Tasks.Task); }
        public virtual System.Threading.Tasks.Task OpenAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task); }
        System.Data.IDbTransaction System.Data.IDbConnection.BeginTransaction() { return default(System.Data.IDbTransaction); }
        System.Data.IDbTransaction System.Data.IDbConnection.BeginTransaction(System.Data.IsolationLevel isolationLevel) { return default(System.Data.IDbTransaction); }
        System.Data.IDbCommand System.Data.IDbConnection.CreateCommand() { return default(System.Data.IDbCommand); }
    }
    public partial class DbConnectionStringBuilder : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        public DbConnectionStringBuilder() { }
        public string ConnectionString { get { return default(string); } set { } }
        public virtual int Count { get { return default(int); } }
        public virtual object this[string keyword] { get { return default(object); } set { } }
        public virtual System.Collections.ICollection Keys { get { return default(System.Collections.ICollection); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
        object System.Collections.ICollection.SyncRoot { get { return default(object); } }
        bool System.Collections.IDictionary.IsReadOnly { get { return default(bool); } }
        object System.Collections.IDictionary.this[object keyword] { get { return default(object); } set { } }
        public virtual System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
        public void Add(string keyword, object value) { }
        public static void AppendKeyValuePair(System.Text.StringBuilder builder, string keyword, string value) { }
        public virtual void Clear() { }
        public virtual bool ContainsKey(string keyword) { return default(bool); }
        public virtual bool EquivalentTo(System.Data.Common.DbConnectionStringBuilder connectionStringBuilder) { return default(bool); }
        public virtual bool Remove(string keyword) { return default(bool); }
        public virtual bool ShouldSerialize(string keyword) { return default(bool); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        void System.Collections.IDictionary.Add(object keyword, object value) { }
        bool System.Collections.IDictionary.Contains(object keyword) { return default(bool); }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        void System.Collections.IDictionary.Remove(object keyword) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
        public override string ToString() { return default(string); }
        public virtual bool TryGetValue(string keyword, out object value) { value = default(object); return default(bool); }
    }
    public abstract partial class DbDataReader : System.Collections.IEnumerable, System.Data.IDataReader, System.Data.IDataRecord, System.IDisposable
    {
        protected DbDataReader() { }
        public abstract int Depth { get; }
        public abstract int FieldCount { get; }
        public abstract bool HasRows { get; }
        public abstract bool IsClosed { get; }
        public abstract object this[int ordinal] { get; }
        public abstract object this[string name] { get; }
        public abstract int RecordsAffected { get; }
        public virtual int VisibleFieldCount { get { return default(int); } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract bool GetBoolean(int ordinal);
        public abstract byte GetByte(int ordinal);
        public abstract long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length);
        public abstract char GetChar(int ordinal);
        public abstract long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length);
        public System.Data.Common.DbDataReader GetData(int ordinal) { return default(System.Data.Common.DbDataReader); }
        public abstract string GetDataTypeName(int ordinal);
        public abstract System.DateTime GetDateTime(int ordinal);
        protected virtual System.Data.Common.DbDataReader GetDbDataReader(int ordinal) { return default(System.Data.Common.DbDataReader); }
        public abstract decimal GetDecimal(int ordinal);
        public abstract double GetDouble(int ordinal);
        public abstract System.Collections.IEnumerator GetEnumerator();
        public abstract System.Type GetFieldType(int ordinal);
        public virtual T GetFieldValue<T>(int ordinal) { return default(T); }
        public System.Threading.Tasks.Task<T> GetFieldValueAsync<T>(int ordinal) { return default(System.Threading.Tasks.Task<T>); }
        public virtual System.Threading.Tasks.Task<T> GetFieldValueAsync<T>(int ordinal, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<T>); }
        public abstract float GetFloat(int ordinal);
        public abstract System.Guid GetGuid(int ordinal);
        public abstract short GetInt16(int ordinal);
        public abstract int GetInt32(int ordinal);
        public abstract long GetInt64(int ordinal);
        public abstract string GetName(int ordinal);
        public abstract int GetOrdinal(string name);
        public virtual System.Type GetProviderSpecificFieldType(int ordinal) { return default(System.Type); }
        public virtual object GetProviderSpecificValue(int ordinal) { return default(object); }
        public virtual int GetProviderSpecificValues(object[] values) { return default(int); }
        public virtual System.IO.Stream GetStream(int ordinal) { return default(System.IO.Stream); }
        public abstract string GetString(int ordinal);
        public virtual System.IO.TextReader GetTextReader(int ordinal) { return default(System.IO.TextReader); }
        public abstract object GetValue(int ordinal);
        public abstract int GetValues(object[] values);
        public abstract bool IsDBNull(int ordinal);
        public System.Threading.Tasks.Task<bool> IsDBNullAsync(int ordinal) { return default(System.Threading.Tasks.Task<bool>); }
        public virtual System.Threading.Tasks.Task<bool> IsDBNullAsync(int ordinal, System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<bool>); }
        public abstract bool NextResult();
        public System.Threading.Tasks.Task<bool> NextResultAsync() { return default(System.Threading.Tasks.Task<bool>); }
        public virtual System.Threading.Tasks.Task<bool> NextResultAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<bool>); }
        public abstract bool Read();
        public System.Threading.Tasks.Task<bool> ReadAsync() { return default(System.Threading.Tasks.Task<bool>); }
        public virtual System.Threading.Tasks.Task<bool> ReadAsync(System.Threading.CancellationToken cancellationToken) { return default(System.Threading.Tasks.Task<bool>); }
        System.Data.IDataReader System.Data.IDataRecord.GetData(int ordinal) { return default(System.Data.IDataReader); }
        public void Close() { }
        public DataTable GetSchemaTable() { return default(DataTable); }
    }
    public abstract partial class DbDataRecord : System.Data.IDataRecord
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
        public System.Data.IDataReader GetData(int i) { return default(System.Data.IDataReader); }
        public abstract string GetDataTypeName(int i);
        public abstract System.DateTime GetDateTime(int i);
        protected virtual System.Data.Common.DbDataReader GetDbDataReader(int i) { return default(System.Data.Common.DbDataReader); }
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
    }
    public abstract partial class DbException : System.Exception
    {
        protected DbException() { }
        protected DbException(string message) { }
        protected DbException(string message, System.Exception innerException) { }
    }
    public abstract partial class DbParameter : System.Data.IDataParameter, System.Data.IDbDataParameter
    {
        protected DbParameter() { }
        public abstract System.Data.DbType DbType { get; set; }
        public abstract System.Data.ParameterDirection Direction { get; set; }
        public abstract bool IsNullable { get; set; }
        public abstract string ParameterName { get; set; }
        public virtual byte Precision { get { return default(byte); } set { } }
        public virtual byte Scale { get { return default(byte); } set { } }
        public abstract int Size { get; set; }
        public abstract string SourceColumn { get; set; }
        public abstract bool SourceColumnNullMapping { get; set; }
        byte System.Data.IDbDataParameter.Precision { get { return default(byte); } set { } }
        byte System.Data.IDbDataParameter.Scale { get { return default(byte); } set { } }
        public abstract object Value { get; set; }
        public DataRowVersion SourceVersion { get; set; }
        public abstract void ResetDbType();
    }
    public abstract partial class DbParameterCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.Data.IDataParameterCollection
    {
        protected DbParameterCollection() { }
        public abstract int Count { get; }
        public System.Data.Common.DbParameter this[int index] { get { return default(System.Data.Common.DbParameter); } set { } }
        public System.Data.Common.DbParameter this[string parameterName] { get { return default(System.Data.Common.DbParameter); } set { } }
        public abstract object SyncRoot { get; }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        object System.Data.IDataParameterCollection.this[string parameterName] { get { return default(object); } set { } }
        public abstract int Add(object value);
        public abstract void AddRange(System.Array values);
        public abstract void Clear();
        public abstract bool Contains(object value);
        public abstract bool Contains(string value);
        public abstract void CopyTo(System.Array array, int index);
        public abstract System.Collections.IEnumerator GetEnumerator();
        protected abstract System.Data.Common.DbParameter GetParameter(int index);
        protected abstract System.Data.Common.DbParameter GetParameter(string parameterName);
        public abstract int IndexOf(object value);
        public abstract int IndexOf(string parameterName);
        public abstract void Insert(int index, object value);
        public abstract void Remove(object value);
        public abstract void RemoveAt(int index);
        public abstract void RemoveAt(string parameterName);
        protected abstract void SetParameter(int index, System.Data.Common.DbParameter value);
        protected abstract void SetParameter(string parameterName, System.Data.Common.DbParameter value);
    }
    public abstract partial class DbProviderFactory
    {
        protected DbProviderFactory() { }
        public virtual System.Data.Common.DbCommand CreateCommand() { return default(System.Data.Common.DbCommand); }
        public virtual System.Data.Common.DbConnection CreateConnection() { return default(System.Data.Common.DbConnection); }
        public virtual System.Data.Common.DbConnectionStringBuilder CreateConnectionStringBuilder() { return default(System.Data.Common.DbConnectionStringBuilder); }
        public virtual System.Data.Common.DbParameter CreateParameter() { return default(System.Data.Common.DbParameter); }
    }
    public abstract partial class DbTransaction : System.Data.IDbTransaction, System.IDisposable
    {
        protected DbTransaction() { }
        public System.Data.Common.DbConnection Connection { get { return default(System.Data.Common.DbConnection); } }
        protected abstract System.Data.Common.DbConnection DbConnection { get; }
        public abstract System.Data.IsolationLevel IsolationLevel { get; }
        IDbConnection IDbTransaction.Connection { get; }
        public abstract void Commit();
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public abstract void Rollback();
    }
}
