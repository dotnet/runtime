// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

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

        object? IList.this[int index]
        {
            get { return GetParameter(index); }
            set { SetParameter(index, (DbParameter)value!); }
        }

        object IDataParameterCollection.this[string parameterName]
        {
            get { return GetParameter(parameterName); }
            set { SetParameter(parameterName, (DbParameter)value!); }
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

        /// <summary>
        /// For a description of this member, see <see cref="IList.Add" />.
        /// </summary>
        /// <param name="value">For a description of this member, see <see cref="IList.Add" />.</param>
        /// <returns>For a description of this member, see <see cref="IList.Add" />.</returns>
        /// <remarks>
        /// This member is an explicit interface member implementation.
        /// It can be used only when the <see cref="DbParameterCollection" /> instance is cast to
        /// <see cref="IList" /> interface.
        /// </remarks>
        int IList.Add(object? value) => Add(value!);

        public abstract int Add(object value);

        public abstract void AddRange(System.Array values);

        /// <summary>
        /// For a description of this member, see <see cref="IList.Contains" />.
        /// </summary>
        /// <param name="value">For a description of this member, see <see cref="IList.Contains" />.</param>
        /// <returns>For a description of this member, see <see cref="IList.Contains" />.</returns>
        /// <remarks>
        /// This member is an explicit interface member implementation.
        /// It can be used only when the <see cref="DbParameterCollection" /> instance is cast to
        /// <see cref="IList" /> interface.
        /// </remarks>
        bool IList.Contains(object? value) => Contains(value!);

        public abstract bool Contains(object value);

        public abstract bool Contains(string value);

        public abstract void CopyTo(System.Array array, int index);

        public abstract void Clear();

        [EditorBrowsable(EditorBrowsableState.Never)]
        public abstract IEnumerator GetEnumerator();

        protected abstract DbParameter GetParameter(int index);

        protected abstract DbParameter GetParameter(string parameterName);

        /// <summary>
        /// For a description of this member, see <see cref="IList.IndexOf" />.
        /// </summary>
        /// <param name="value">For a description of this member, see <see cref="IList.IndexOf" />.</param>
        /// <returns>For a description of this member, see <see cref="IList.IndexOf" />.</returns>
        /// <remarks>
        /// This member is an explicit interface member implementation.
        /// It can be used only when the <see cref="DbParameterCollection" /> instance is cast to
        /// <see cref="IList" /> interface.
        /// </remarks>
        int IList.IndexOf(object? value) => IndexOf(value!);

        public abstract int IndexOf(object value);

        public abstract int IndexOf(string parameterName);

        /// <summary>
        /// For a description of this member, see <see cref="IList.Insert" />.
        /// </summary>
        /// <param name="index">For a description of this member, see <see cref="IList.Insert" />.</param>
        /// <param name="value">For a description of this member, see <see cref="IList.Insert" />.</param>
        /// <remarks>
        /// This member is an explicit interface member implementation.
        /// It can be used only when the <see cref="DbParameterCollection" /> instance is cast to
        /// <see cref="IList" /> interface.
        /// </remarks>
        void IList.Insert(int index, object? value) => Insert(index, value!);

        public abstract void Insert(int index, object value);

        /// <summary>
        /// For a description of this member, see <see cref="IList.Remove" />.
        /// </summary>
        /// <param name="value">For a description of this member, see <see cref="IList.Remove" />.</param>
        /// <remarks>
        /// This member is an explicit interface member implementation.
        /// It can be used only when the <see cref="DbParameterCollection" /> instance is cast to
        /// <see cref="IList" /> interface.
        /// </remarks>
        void IList.Remove(object? value) => Remove(value!);

        public abstract void Remove(object value);

        public abstract void RemoveAt(int index);

        public abstract void RemoveAt(string parameterName);

        protected abstract void SetParameter(int index, DbParameter value);

        protected abstract void SetParameter(string parameterName, DbParameter value);
    }
}
