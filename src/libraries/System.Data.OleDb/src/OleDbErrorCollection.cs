// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Data.Common;

namespace System.Data.OleDb
{
    [Serializable, ListBindable(false)]
    public sealed class OleDbErrorCollection : System.Collections.ICollection
    {
        private readonly ArrayList items;

        internal OleDbErrorCollection(UnsafeNativeMethods.IErrorInfo? errorInfo)
        {
            ArrayList items = new ArrayList();
            UnsafeNativeMethods.IErrorRecords? errorRecords = (errorInfo as UnsafeNativeMethods.IErrorRecords);
            if (null != errorRecords)
            {
                int recordCount = errorRecords.GetRecordCount();

                for (int i = 0; i < recordCount; ++i)
                {
                    OleDbError error = new OleDbError(errorRecords, i);
                    items.Add(error);
                }
            }
            this.items = items;
        }

        bool System.Collections.ICollection.IsSynchronized
        {
            get { return false; }
        }

        object System.Collections.ICollection.SyncRoot
        {
            get { return this; }
        }

        public int Count
        {
            get
            {
                ArrayList items = this.items;
                return ((null != items) ? items.Count : 0);
            }
        }

        public OleDbError this[int index]
        {
            get
            {
                return (this.items[index] as OleDbError)!;
            }
        }

        internal void AddRange(ICollection c)
        {
            items.AddRange(c);
        }

        public void CopyTo(Array array, int index)
        {
            this.items.CopyTo(array, index);
        }

        public void CopyTo(OleDbError[] array, int index)
        {
            this.items.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return this.items.GetEnumerator();
        }
    }
}
