// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Collections;

namespace System.Data.Common
{
    public abstract class DbParameterCollection : MarshalByRefObject, IDataParameterCollection
    {
        protected DbParameterCollection() : base() { }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public abstract int Count { get; }

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool IsFixedSize => false;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool IsReadOnly => false;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual bool IsSynchronized => false;

        [Browsable(false)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public abstract object SyncRoot { get; }

        object IList.this[int index]
        {
            get { return GetParameter(index); }
            set { SetParameter(index, (DbParameter)value); }
        }

        object IDataParameterCollection.this[string parameterName]
        {
            get { return GetParameter(parameterName); }
            set { SetParameter(parameterName, (DbParameter)value); }
        }

        public DbParameter this[int index]
        {
            get { return GetParameter(index); }
            set { SetParameter(index, value); }
        }

        public DbParameter this[string parameterName]
        {
            get { return GetParameter(parameterName) as DbParameter; }
            set { SetParameter(parameterName, value); }
        }

        public abstract int Add(object value);

        public abstract void AddRange(System.Array values);

        public abstract bool Contains(object value);

        public abstract bool Contains(string value);

        public abstract void CopyTo(System.Array array, int index);

        public abstract void Clear();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract IEnumerator GetEnumerator();

        protected abstract DbParameter GetParameter(int index);

        protected abstract DbParameter GetParameter(string parameterName);

        public abstract int IndexOf(object value);

        public abstract int IndexOf(string parameterName);

        public abstract void Insert(int index, object value);

        public abstract void Remove(object value);

        public abstract void RemoveAt(int index);

        public abstract void RemoveAt(string parameterName);

        protected abstract void SetParameter(int index, DbParameter value);

        protected abstract void SetParameter(string parameterName, DbParameter value);
    }
}
