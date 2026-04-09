// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Data.Common
{
    public abstract class DbBatchCommandCollection : IList<DbBatchCommand>
    {
        public abstract IEnumerator<DbBatchCommand> GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public abstract void Add(DbBatchCommand item);

        public abstract void Clear();

        public abstract bool Contains(DbBatchCommand item);

        public abstract void CopyTo(DbBatchCommand[] array, int arrayIndex);

        public abstract bool Remove(DbBatchCommand item);

        public abstract int Count { get; }

        public abstract bool IsReadOnly { get; }

        public abstract int IndexOf(DbBatchCommand item);

        public abstract void Insert(int index, DbBatchCommand item);

        public abstract void RemoveAt(int index);

        public DbBatchCommand this[int index]
        {
            get => GetBatchCommand(index);
            set => SetBatchCommand(index, value);
        }

        protected abstract DbBatchCommand GetBatchCommand(int index);

        protected abstract void SetBatchCommand(int index, DbBatchCommand batchCommand);
    }
}
