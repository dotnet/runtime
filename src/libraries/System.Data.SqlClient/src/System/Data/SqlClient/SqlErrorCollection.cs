// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Collections;


namespace System.Data.SqlClient
{
    public sealed class SqlErrorCollection : ICollection
    {
        private ArrayList _errors = new ArrayList();

        internal SqlErrorCollection()
        {
        }

        public void CopyTo(Array array, int index)
        {
            _errors.CopyTo(array, index);
        }

        public void CopyTo(SqlError[] array, int index)
        {
            _errors.CopyTo(array, index);
        }

        public int Count
        {
            get { return _errors.Count; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return this; }
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        public SqlError this[int index]
        {
            get
            {
                return (SqlError)_errors[index];
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _errors.GetEnumerator();
        }

        internal void Add(SqlError error)
        {
            _errors.Add(error);
        }
    }
}
