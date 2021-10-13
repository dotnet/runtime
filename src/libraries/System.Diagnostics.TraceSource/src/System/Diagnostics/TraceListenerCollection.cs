// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <devdoc>
    /// <para>Provides a thread-safe list of <see cref='System.Diagnostics.TraceListenerCollection'/>. A thread-safe list is synchronized.</para>
    /// </devdoc>
    public class TraceListenerCollection : IList
    {
        private readonly List<TraceListener> _list;

        internal TraceListenerCollection()
        {
            _list = new List<TraceListener>(1);
        }

        /// <devdoc>
        /// <para>Gets or sets the <see cref='TraceListener'/> at
        ///    the specified index.</para>
        /// </devdoc>
        public TraceListener this[int i]
        {
            get => _list[i];
            set
            {
                InitializeListener(value);
                _list[i] = value;
            }
        }

        /// <devdoc>
        /// <para>Gets the first <see cref='System.Diagnostics.TraceListener'/> in the list with the specified name.</para>
        /// </devdoc>
        public TraceListener? this[string name]
        {
            get
            {
                foreach (TraceListener listener in _list)
                {
                    if (listener.Name == name)
                        return listener;
                }
                return null;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the number of listeners in the list.
        ///    </para>
        /// </devdoc>
        public int Count => _list.Count;

        /// <devdoc>
        /// <para>Adds a <see cref='System.Diagnostics.TraceListener'/> to the list.</para>
        /// </devdoc>
        public int Add(TraceListener listener)
        {
            InitializeListener(listener);

            lock (TraceInternal.critSec)
            {
                _list.Add(listener);
                return _list.Count - 1;
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void AddRange(TraceListener[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            foreach (TraceListener listener in value)
            {
                Add(listener);
            }
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void AddRange(TraceListenerCollection value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            int currentCount = value.Count;
            for (int i = 0; i < currentCount; i++)
            {
                this.Add(value[i]);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Clears all the listeners from the
        ///       list.
        ///    </para>
        /// </devdoc>
        public void Clear() => _list.Clear();

        /// <devdoc>
        ///    <para>Checks whether the list contains the specified
        ///       listener.</para>
        /// </devdoc>
        public bool Contains(TraceListener? listener) => _list.Contains(listener!);

        /// <devdoc>
        /// <para>Copies a section of the current <see cref='System.Diagnostics.TraceListenerCollection'/> list to the specified array at the specified
        ///    index.</para>
        /// </devdoc>
        public void CopyTo(TraceListener[] listeners, int index)
        {
            _list.CopyTo(listeners, index);
        }

        /// <devdoc>
        ///    <para>
        ///       Gets an enumerator for this list.
        ///    </para>
        /// </devdoc>
        public IEnumerator GetEnumerator() => _list.GetEnumerator();

        internal List<TraceListener> List => _list;

        internal void InitializeListener(TraceListener listener)
        {
            if (listener == null)
                throw new ArgumentNullException(nameof(listener));

            listener.IndentSize = Debug.IndentSize;
            listener.IndentLevel = Debug.IndentLevel;
        }

        /// <devdoc>
        ///    <para>Gets the index of the specified listener.</para>
        /// </devdoc>
        public int IndexOf(TraceListener? listener) => _list.IndexOf(listener!);

        /// <devdoc>
        ///    <para>Inserts the listener at the specified index.</para>
        /// </devdoc>
        public void Insert(int index, TraceListener listener)
        {
            InitializeListener(listener);
            lock (TraceInternal.critSec)
            {
                _list.Insert(index, listener);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Removes the specified instance of the <see cref='System.Diagnostics.TraceListener'/> class from the list.
        ///    </para>
        /// </devdoc>
        public void Remove(TraceListener? listener)
        {
            lock (TraceInternal.critSec)
            {
                _list.Remove(listener!);
            }
        }

        /// <devdoc>
        ///    <para>Removes the first listener in the list that has the
        ///       specified name.</para>
        /// </devdoc>
        public void Remove(string name)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].Name == name)
                {
                    RemoveAt(i);
                    break;
                }
            }
        }

        /// <devdoc>
        /// <para>Removes the <see cref='System.Diagnostics.TraceListener'/> at the specified index.</para>
        /// </devdoc>
        public void RemoveAt(int index)
        {
            lock (TraceInternal.critSec)
            {
                _list.RemoveAt(index);
            }
        }

        object? IList.this[int index]
        {
            get => _list[index];
            set
            {
                if (value is not TraceListener listener)
                    throw new ArgumentException(SR.MustAddListener, nameof(value));
                this[index] = listener;
            }
        }

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        int IList.Add(object? value)
        {
            if (value is not TraceListener listener)
                throw new ArgumentException(SR.MustAddListener, nameof(value));
            return Add(listener);
        }

        bool IList.Contains(object? value) => Contains((TraceListener?)value);

        int IList.IndexOf(object? value) => IndexOf((TraceListener?)value);

        void IList.Insert(int index, object? value)
        {
            if (value is not TraceListener listener)
                throw new ArgumentException(SR.MustAddListener, nameof(value));
            Insert(index, listener);
        }

        void IList.Remove(object? value) => Remove((TraceListener)value!);

        object ICollection.SyncRoot => this;

        bool ICollection.IsSynchronized => true;

        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_list).CopyTo(array, index);
        }
    }
}
