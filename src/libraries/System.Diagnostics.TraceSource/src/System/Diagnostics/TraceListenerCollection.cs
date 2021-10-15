// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Diagnostics
{
    /// <devdoc>
    /// <para>Provides a thread-safe list of <see cref='System.Diagnostics.TraceListenerCollection'/>. A thread-safe list is synchronized.</para>
    /// </devdoc>
    public sealed class TraceListenerCollection : IList
    {
        private readonly List<TraceListener> _list;

        internal TraceListenerCollection(TraceListener listener)
        {
            InitializeListener(listener);
            _list = new List<TraceListener>(1) { listener };
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
                var listeners = _list;
                for (int i = 0; i < listeners.Count; i++)
                {
                    var listener = listeners[i];
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
            ArgumentNullException.ThrowIfNull(value);
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
            ArgumentNullException.ThrowIfNull(value);
            var list = value._list;
            for (int i = 0, currentCount = list.Count; i < currentCount; i++)
            {
                Add(list[i]);
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Clears all the listeners from the
        ///       list.
        ///    </para>
        /// </devdoc>
        public void Clear()
        {
            lock (TraceInternal.critSec)
            {
                _list.Clear();
            }
        }

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

        private void InitializeListener(TraceListener listener)
        {
            ArgumentNullException.ThrowIfNull(listener);
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
            var listeners = _list;
            for (int i = 0; i < listeners.Count; i++)
            {
                if (listeners[i].Name == name)
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
            set => this[index] = value as TraceListener ?? throw new ArgumentException(SR.MustAddListener, nameof(value));
        }

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        int IList.Add(object? value) => Add(value as TraceListener ?? throw new ArgumentException(SR.MustAddListener, nameof(value)));

        bool IList.Contains(object? value) => Contains((TraceListener?)value);

        int IList.IndexOf(object? value) => IndexOf((TraceListener?)value);

        void IList.Insert(int index, object? value) => Insert(index, value as TraceListener ?? throw new ArgumentException(SR.MustAddListener, nameof(value)));

        void IList.Remove(object? value) => Remove((TraceListener?)value);

        object ICollection.SyncRoot => this;

        bool ICollection.IsSynchronized => true;

        void ICollection.CopyTo(Array array, int index) => ((ICollection)_list).CopyTo(array, index);
    }
}
