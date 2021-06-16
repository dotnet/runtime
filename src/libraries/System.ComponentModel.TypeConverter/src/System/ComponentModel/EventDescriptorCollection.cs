// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Represents a collection of events.
    /// </summary>
    public class EventDescriptorCollection : ICollection, IList
    {
        private EventDescriptor[] _events;
        private readonly string[] _namedSort;
        private readonly IComparer _comparer;
        private bool _eventsOwned;
        private bool _needSort;
        private readonly bool _readOnly;

        /// <summary>
        /// An empty AttributeCollection that can used instead of creating a new one with no items.
        /// </summary>
        public static readonly EventDescriptorCollection Empty = new EventDescriptorCollection(null, true);

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EventDescriptorCollection'/> class.
        /// </summary>
        public EventDescriptorCollection(EventDescriptor[] events)
        {
            if (events == null)
            {
                _events = Array.Empty<EventDescriptor>();
            }
            else
            {
                _events = events;
                Count = events.Length;
            }
            _eventsOwned = true;
        }

        /// <summary>
        /// Initializes a new instance of an event descriptor collection, and allows you to mark the
        /// collection as read-only so it cannot be modified.
        /// </summary>
        public EventDescriptorCollection(EventDescriptor[] events, bool readOnly) : this(events)
        {
            _readOnly = readOnly;
        }

        private EventDescriptorCollection(EventDescriptor[] events, int eventCount, string[] namedSort, IComparer comparer)
        {
            _eventsOwned = false;
            if (namedSort != null)
            {
                _namedSort = (string[])namedSort.Clone();
            }
            _comparer = comparer;
            _events = events;
            Count = eventCount;
            _needSort = true;
        }

        /// <summary>
        /// Gets the number of event descriptors in the collection.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the event with the specified index number.
        /// </summary>
        public virtual EventDescriptor this[int index]
        {
            get
            {
                if (index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }
                EnsureEventsOwned();
                return _events[index];
            }
        }

        /// <summary>
        /// Gets the event with the specified name.
        /// </summary>
        public virtual EventDescriptor this[string name] => Find(name, false);

        public int Add(EventDescriptor value)
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            EnsureSize(Count + 1);
            _events[Count++] = value;
            return Count - 1;
        }

        public void Clear()
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            Count = 0;
        }

        public bool Contains(EventDescriptor value) => IndexOf(value) >= 0;

        void ICollection.CopyTo(Array array, int index)
        {
            EnsureEventsOwned();
            Array.Copy(_events, 0, array, index, Count);
        }

        private void EnsureEventsOwned()
        {
            if (!_eventsOwned)
            {
                _eventsOwned = true;
                if (_events != null)
                {
                    EventDescriptor[] newEvents = new EventDescriptor[Count];
                    Array.Copy(_events, newEvents, Count);
                    _events = newEvents;
                }
            }

            if (_needSort)
            {
                _needSort = false;
                InternalSort(_namedSort);
            }
        }

        private void EnsureSize(int sizeNeeded)
        {
            if (sizeNeeded <= _events.Length)
            {
                return;
            }

            if (_events.Length == 0)
            {
                Count = 0;
                _events = new EventDescriptor[sizeNeeded];
                return;
            }

            EnsureEventsOwned();

            int newSize = Math.Max(sizeNeeded, _events.Length * 2);
            EventDescriptor[] newEvents = new EventDescriptor[newSize];
            Array.Copy(_events, newEvents, Count);
            _events = newEvents;
        }

        /// <summary>
        /// Gets the description of the event with the specified
        /// name in the collection.
        /// </summary>
        public virtual EventDescriptor Find(string name, bool ignoreCase)
        {
            EventDescriptor p = null;

            if (ignoreCase)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (string.Equals(_events[i].Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        p = _events[i];
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < Count; i++)
                {
                    if (string.Equals(_events[i].Name, name, StringComparison.Ordinal))
                    {
                        p = _events[i];
                        break;
                    }
                }
            }

            return p;
        }

        public int IndexOf(EventDescriptor value) => Array.IndexOf(_events, value, 0, Count);

        public void Insert(int index, EventDescriptor value)
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            EnsureSize(Count + 1);
            if (index < Count)
            {
                Array.Copy(_events, index, _events, index + 1, Count - index);
            }
            _events[index] = value;
            Count++;
        }

        public void Remove(EventDescriptor value)
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            int index = IndexOf(value);

            if (index != -1)
            {
                RemoveAt(index);
            }
        }

        public void RemoveAt(int index)
        {
            if (_readOnly)
            {
                throw new NotSupportedException();
            }

            if (index < Count - 1)
            {
                Array.Copy(_events, index + 1, _events, index, Count - index - 1);
            }
            _events[Count - 1] = null;
            Count--;
        }

        /// <summary>
        /// Gets an enumerator for this <see cref='System.ComponentModel.EventDescriptorCollection'/>.
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            // We can only return an enumerator on the events we actually have.
            if (_events.Length == Count)
            {
                return _events.GetEnumerator();
            }
            else
            {
                return new ArraySubsetEnumerator(_events, Count);
            }
        }

        /// <summary>
        /// Sorts the members of this EventDescriptorCollection, using the default sort for this collection,
        /// which is usually alphabetical.
        /// </summary>
        public virtual EventDescriptorCollection Sort()
        {
            return new EventDescriptorCollection(_events, Count, _namedSort, _comparer);
        }

        /// <summary>
        /// Sorts the members of this EventDescriptorCollection. Any specified NamedSort arguments will
        /// be applied first, followed by sort using the specified IComparer.
        /// </summary>
        public virtual EventDescriptorCollection Sort(string[] names)
        {
            return new EventDescriptorCollection(_events, Count, names, _comparer);
        }

        /// <summary>
        /// Sorts the members of this EventDescriptorCollection. Any specified NamedSort arguments will
        /// be applied first, followed by sort using the specified IComparer.
        /// </summary>
        public virtual EventDescriptorCollection Sort(string[] names, IComparer comparer)
        {
            return new EventDescriptorCollection(_events, Count, names, comparer);
        }

        /// <summary>
        /// Sorts the members of this EventDescriptorCollection, using the specified IComparer to compare,
        /// the EventDescriptors contained in the collection.
        /// </summary>
        public virtual EventDescriptorCollection Sort(IComparer comparer)
        {
            return new EventDescriptorCollection(_events, Count, _namedSort, comparer);
        }

        /// <summary>
        /// Sorts the members of this EventDescriptorCollection. Any specified NamedSort arguments will
        /// be applied first, followed by sort using the specified IComparer.
        /// </summary>
        protected void InternalSort(string[] names)
        {
            if (_events.Length == 0)
            {
                return;
            }

            InternalSort(_comparer);

            if (names != null && names.Length > 0)
            {
                List<EventDescriptor> eventList = new List<EventDescriptor>(_events);
                int foundCount = 0;
                int eventCount = _events.Length;

                for (int i = 0; i < names.Length; i++)
                {
                    for (int j = 0; j < eventCount; j++)
                    {
                        EventDescriptor currentEvent = eventList[j];

                        // Found a matching event. Here, we add it to our array. We also
                        // mark it as null in our array list so we don't add it twice later.
                        //
                        if (currentEvent != null && currentEvent.Name.Equals(names[i]))
                        {
                            _events[foundCount++] = currentEvent;
                            eventList[j] = null;
                            break;
                        }
                    }
                }

                // At this point we have filled in the first "foundCount" number of propeties, one for each
                // name in our name array. If a name didn't match, then it is ignored. Next, we must fill
                // in the rest of the properties. We now have a sparse array containing the remainder, so
                // it's easy.
                //
                for (int i = 0; i < eventCount; i++)
                {
                    if (eventList[i] != null)
                    {
                        _events[foundCount++] = eventList[i];
                    }
                }

                Debug.Assert(foundCount == eventCount, "We did not completely fill our event array");
            }
        }

        /// <summary>
        /// Sorts the members of this EventDescriptorCollection using the specified IComparer.
        /// </summary>
        protected void InternalSort(IComparer sorter)
        {
            if (sorter == null)
            {
                TypeDescriptor.SortDescriptorArray(this);
            }
            else
            {
                Array.Sort(_events, sorter);
            }
        }

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => null;

        int ICollection.Count => Count;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        object IList.this[int index]
        {
            get => this[index];
            set
            {
                if (_readOnly)
                {
                    throw new NotSupportedException();
                }

                if (index >= Count)
                {
                    throw new IndexOutOfRangeException();
                }
                EnsureEventsOwned();
                _events[index] = (EventDescriptor)value;
            }
        }

        int IList.Add(object value) => Add((EventDescriptor)value);

        bool IList.Contains(object value) => Contains((EventDescriptor)value);

        void IList.Clear() => Clear();

        int IList.IndexOf(object value) => IndexOf((EventDescriptor)value);

        void IList.Insert(int index, object value) => Insert(index, (EventDescriptor)value);

        void IList.Remove(object value) => Remove((EventDescriptor)value);

        void IList.RemoveAt(int index) => RemoveAt(index);

        bool IList.IsReadOnly => _readOnly;

        bool IList.IsFixedSize => _readOnly;

        private sealed class ArraySubsetEnumerator : IEnumerator
        {
            private readonly Array _array;
            private readonly int _total;
            private int _current;

            public ArraySubsetEnumerator(Array array, int count)
            {
                Debug.Assert(count == 0 || array != null, "if array is null, count should be 0");
                Debug.Assert(array == null || count <= array.Length, "Trying to enumerate more than the array contains");

                _array = array;
                _total = count;
                _current = -1;
            }

            public bool MoveNext()
            {
                if (_current < _total - 1)
                {
                    _current++;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public void Reset() => _current = -1;

            public object Current
            {
                get
                {
                    if (_current == -1)
                    {
                        throw new InvalidOperationException();
                    }
                    else
                    {
                        return _array.GetValue(_current);
                    }
                }
            }
        }
    }
}
