// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Data.SqlClient
{
    [Serializable]
    public sealed class SqlErrorCollection : ICollection
    {
        // Ideally this would be typed as List<SqlError>, but that would make the non-generic
        // CopyTo behave differently than the full framework (which uses ArrayList), throwing
        // ArgumentException instead of the expected InvalidCastException for incompatible types.
        // Instead, we use List<object>, which makes the non-generic CopyTo behave like
        // ArrayList.CopyTo.
        private readonly List<object> _errors = new List<object>();

        internal SqlErrorCollection() { }

        public void CopyTo(Array array, int index) => ((ICollection)_errors).CopyTo(array, index);

        public void CopyTo(SqlError[] array, int index) => _errors.CopyTo(array, index);

        public int Count => _errors.Count;

        object ICollection.SyncRoot => this;

        bool ICollection.IsSynchronized => false;

        public SqlError this[int index] => (SqlError)_errors[index];

        public IEnumerator GetEnumerator() => _errors.GetEnumerator();

        internal void Add(SqlError error) => _errors.Add(error);
    }
}
