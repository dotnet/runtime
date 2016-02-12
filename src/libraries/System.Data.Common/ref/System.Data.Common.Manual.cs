// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Data.Common
{
    public abstract partial class DbCommand : System.IDisposable
    {
        // DbCommand expects IDisposable methods to be implemented via System.ComponentModel.Component, which it no longer inherits from
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public abstract partial class DbConnection : System.IDisposable
    {
        // DbConnection expects IDisposable methods to be implemented via System.ComponentModel.Component, which it no longer inherits from
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
    }
    public partial class DbConnectionStringBuilder : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable
    {
        // Explicitly implementing methods that are now discouraged\deprecated
        bool System.Collections.IDictionary.IsFixedSize { get { return default(bool); } }
    }
    public abstract partial class DbParameterCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        // Explicitly implementing methods that are now discouraged\deprecated
        bool System.Collections.IList.IsFixedSize { get { return default(bool); } }
        bool System.Collections.IList.IsReadOnly { get { return default(bool); } }
        bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    }

    public abstract partial class DbColumn
    {
        public bool? AllowDBNull { get; protected set; }
        public string BaseCatalogName { get; protected set; }
        public string BaseColumnName { get; protected set; }
        public string BaseSchemaName { get; protected set; }
        public string BaseServerName { get; protected set; }
        public string BaseTableName { get; protected set; }
        public string ColumnName { get; protected set; }
        public int? ColumnOrdinal { get; protected set; }
        public int? ColumnSize { get; protected set; }
        public bool? IsAliased { get; protected set; }
        public bool? IsAutoIncrement { get; protected set; }
        public bool? IsExpression { get; protected set; }
        public bool? IsHidden { get; protected set; }
        public bool? IsIdentity { get; protected set; }
        public bool? IsKey { get; protected set; }
        public bool? IsLong { get; protected set; }
        public bool? IsReadOnly { get; protected set; }
        public bool? IsUnique { get; protected set; }
        public int? NumericPrecision { get; protected set; }
        public int? NumericScale { get; protected set; }
        public string UdtAssemblyQualifiedName { get; protected set; }
        public Type DataType { get; protected set; }
        public string DataTypeName { get; protected set; }
        public virtual object this[string property]
        {
            get
            {
                return default(object);
            }
        }
    }

    public static class DbDataReaderExtensions
    {
        public static System.Collections.ObjectModel.ReadOnlyCollection<DbColumn> GetColumnSchema(this DbDataReader reader)
        {
            return default(System.Collections.ObjectModel.ReadOnlyCollection<DbColumn>);
        }

        public static bool CanGetColumnSchema(this DbDataReader reader)
        {
            return default(bool);
        }
    }

}
