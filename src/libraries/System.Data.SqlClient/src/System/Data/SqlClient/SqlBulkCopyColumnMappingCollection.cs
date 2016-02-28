// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;

namespace System.Data.SqlClient
{
    public sealed class SqlBulkCopyColumnMappingCollection : ICollection, IEnumerable, IList
    {
        private enum MappingSchema
        {
            Undefined = 0,
            NamesNames = 1,
            NemesOrdinals = 2,
            OrdinalsNames = 3,
            OrdinalsOrdinals = 4,
        }

        private readonly List<object> _list = new List<object>();
        private MappingSchema _mappingSchema = MappingSchema.Undefined;

        internal SqlBulkCopyColumnMappingCollection()
        {
        }

        public int Count => _list.Count;

        internal bool ReadOnly { get; set; }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => NonGenericList.SyncRoot;

        bool IList.IsFixedSize => false;

        // This always returns false in the full framework, regardless
        // of the value of the internal ReadOnly property.
        bool IList.IsReadOnly => false;

        object IList.this[int index]
        {
            get
            {
                return NonGenericList[index];
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                NonGenericList[index] = value;
            }
        }

        public SqlBulkCopyColumnMapping this[int index] => (SqlBulkCopyColumnMapping)_list[index];

        public SqlBulkCopyColumnMapping Add(SqlBulkCopyColumnMapping bulkCopyColumnMapping)
        {
            AssertWriteAccess();
            Debug.Assert(ADP.IsEmpty(bulkCopyColumnMapping.SourceColumn) || bulkCopyColumnMapping._internalSourceColumnOrdinal == -1, "BulkLoadAmbigousSourceColumn");
            if (((ADP.IsEmpty(bulkCopyColumnMapping.SourceColumn)) && (bulkCopyColumnMapping.SourceOrdinal == -1))
                || ((ADP.IsEmpty(bulkCopyColumnMapping.DestinationColumn)) && (bulkCopyColumnMapping.DestinationOrdinal == -1)))
            {
                throw SQL.BulkLoadNonMatchingColumnMapping();
            }
            _list.Add(bulkCopyColumnMapping);
            return bulkCopyColumnMapping;
        }

        public SqlBulkCopyColumnMapping Add(string sourceColumn, string destinationColumn)
        {
            AssertWriteAccess();
            return Add(new SqlBulkCopyColumnMapping(sourceColumn, destinationColumn));
        }

        public SqlBulkCopyColumnMapping Add(int sourceColumnIndex, string destinationColumn)
        {
            AssertWriteAccess();
            return Add(new SqlBulkCopyColumnMapping(sourceColumnIndex, destinationColumn));
        }

        public SqlBulkCopyColumnMapping Add(string sourceColumn, int destinationColumnIndex)
        {
            AssertWriteAccess();
            return Add(new SqlBulkCopyColumnMapping(sourceColumn, destinationColumnIndex));
        }
        public SqlBulkCopyColumnMapping Add(int sourceColumnIndex, int destinationColumnIndex)
        {
            AssertWriteAccess();
            return Add(new SqlBulkCopyColumnMapping(sourceColumnIndex, destinationColumnIndex));
        }

        private void AssertWriteAccess()
        {
            if (ReadOnly)
            {
                throw SQL.BulkLoadMappingInaccessible();
            }
        }

        public void Clear()
        {
            AssertWriteAccess();
            _list.Clear();
        }

        public bool Contains(SqlBulkCopyColumnMapping value) => _list.Contains(value);

        public void CopyTo(SqlBulkCopyColumnMapping[] array, int index) => _list.CopyTo(array, index);

        internal void CreateDefaultMapping(int columnCount)
        {
            for (int i = 0; i < columnCount; i++)
            {
                _list.Add(new SqlBulkCopyColumnMapping(i, i));
            }
        }

        public IEnumerator GetEnumerator() => _list.GetEnumerator();

        public int IndexOf(SqlBulkCopyColumnMapping value) => _list.IndexOf(value);

        public void Insert(int index, SqlBulkCopyColumnMapping value)
        {
            AssertWriteAccess();
            _list.Insert(index, value);
        }

        public void Remove(SqlBulkCopyColumnMapping value)
        {
            AssertWriteAccess();
            _list.Remove(value);
        }

        public void RemoveAt(int index)
        {
            AssertWriteAccess();
            _list.RemoveAt(index);
        }

        internal void ValidateCollection()
        {
            MappingSchema mappingSchema;
            foreach (SqlBulkCopyColumnMapping a in _list)
            {
                mappingSchema = a.SourceOrdinal != -1 ?
                    (a.DestinationOrdinal != -1 ? MappingSchema.OrdinalsOrdinals : MappingSchema.OrdinalsNames) :
                    (a.DestinationOrdinal != -1 ? MappingSchema.NemesOrdinals : MappingSchema.NamesNames);

                if (_mappingSchema == MappingSchema.Undefined)
                {
                    _mappingSchema = mappingSchema;
                }
                else
                {
                    if (_mappingSchema != mappingSchema)
                    {
                        throw SQL.BulkLoadMappingsNamesOrOrdinalsOnly();
                    }
                }
            }
        }

        void ICollection.CopyTo(Array array, int index) => NonGenericList.CopyTo(array, index);

        int IList.Add(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return NonGenericList.Add(value);
        }

        bool IList.Contains(object value) => NonGenericList.Contains(value);

        int IList.IndexOf(object value) => NonGenericList.IndexOf(value);

        void IList.Insert(int index, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            NonGenericList.Insert(index, value);
        }

        void IList.Remove(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            bool removed = _list.Remove(value);

            // This throws on full framework, so it will also throw here.
            if (!removed)
            {
                throw new ArgumentException(SR.Arg_RemoveArgNotFound);
            }
        }

        private IList NonGenericList => _list;
    }
}
