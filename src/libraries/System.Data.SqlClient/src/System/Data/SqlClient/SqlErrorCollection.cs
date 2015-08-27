// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



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
