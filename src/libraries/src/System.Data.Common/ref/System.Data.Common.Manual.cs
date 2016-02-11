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
}
