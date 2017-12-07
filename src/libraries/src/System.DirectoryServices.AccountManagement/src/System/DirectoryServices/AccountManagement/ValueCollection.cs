// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    public class PrincipalValueCollection<T> : IList<T>, IList
    // T must be a ValueType or immutable ref type (such as string)
    {
        //
        // IList
        //
        bool IList.IsFixedSize
        {
            get
            {
                return IsFixedSize;
            }
        }

        bool IList.IsReadOnly
        {
            get
            {
                return IsReadOnly;
            }
        }

        int IList.Add(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            _inner.Add((T)value);
            return Count;
        }

        void IList.Clear()
        {
            Clear();
        }

        bool IList.Contains(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return _inner.Contains((T)value);
        }

        int IList.IndexOf(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return IndexOf((T)value);
        }

        void IList.Insert(int index, object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            Insert(index, (T)value);
        }

        void IList.Remove(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            _inner.Remove((T)value);
        }

        void IList.RemoveAt(int index)
        {
            RemoveAt(index);
        }

        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                this[index] = (T)value;
            }
        }

        //
        // ICollection
        //
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)_inner).CopyTo(array, index);
        }

        int ICollection.Count
        {
            get
            {
                return _inner.Count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return IsSynchronized;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                return SyncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return ((ICollection)_inner).IsSynchronized;
            }
        }

        public object SyncRoot
        {
            get
            {
                return this;
            }
        }

        //
        // IEnumerable
        //
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        //
        // IList<T>
        //
        public bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        // Adds obj to the end of the list by inserting it into insertedValues.
        public void Add(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            _inner.Add(value);
        }

        public void Clear()
        {
            _inner.Clear();
        }

        public bool Contains(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return _inner.Contains(value);
        }

        public int IndexOf(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            int index = 0;

            foreach (TrackedCollection<T>.ValueEl el in _inner.combinedValues)
            {
                if (el.isInserted && el.insertedValue.Equals(value))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "IndexOf: found {0} on inserted at {1}", value, index);
                    return index;
                }

                if (!el.isInserted && el.originalValue.Right.Equals(value))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "IndexOf: found {0} on original at {1}", value, index);
                    return index;
                }

                index++;
            }

            return -1;
        }

        public void Insert(int index, T value)
        {
            _inner.MarkChange();

            if (value == null)
                throw new ArgumentNullException("value");

            if ((index < 0) || (index > _inner.combinedValues.Count))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalValueCollection", "Insert({0}): out of range (count={1})", index, _inner.combinedValues.Count);
                throw new ArgumentOutOfRangeException("index");
            }

            TrackedCollection<T>.ValueEl el = new TrackedCollection<T>.ValueEl();
            el.isInserted = true;
            el.insertedValue = value;

            _inner.combinedValues.Insert(index, el);
        }

        public bool Remove(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            return _inner.Remove(value);
        }

        public void RemoveAt(int index)
        {
            _inner.MarkChange();

            if ((index < 0) || (index >= _inner.combinedValues.Count))
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalValueCollection", "RemoveAt({0}): out of range (count={1})", index, _inner.combinedValues.Count);
                throw new ArgumentOutOfRangeException("index");
            }

            TrackedCollection<T>.ValueEl el = _inner.combinedValues[index];

            if (el.isInserted)
            {
                // We're removing an inserted value.
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "RemoveAt({0}): removing inserted", index);
                _inner.combinedValues.RemoveAt(index);
            }
            else
            {
                // We're removing an original value.
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "RemoveAt({0}): removing original", index);
                Pair<T, T> pair = _inner.combinedValues[index].originalValue;
                _inner.combinedValues.RemoveAt(index);
                _inner.removedValues.Add(pair.Left);
            }
        }

        public T this[int index]
        {
            get
            {
                if ((index < 0) || (index >= _inner.combinedValues.Count))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalValueCollection", "this[{0}].get: out of range (count={1})", index, _inner.combinedValues.Count);
                    throw new ArgumentOutOfRangeException("index");
                }

                TrackedCollection<T>.ValueEl el = _inner.combinedValues[index];

                if (el.isInserted)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "this[{0}].get: is inserted {1}", index, el.insertedValue);
                    return el.insertedValue;
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "this[{0}].get: is original {1}", index, el.originalValue.Right);
                    return el.originalValue.Right;  // Right == current value
                }
            }

            set
            {
                _inner.MarkChange();

                if ((index < 0) || (index >= _inner.combinedValues.Count))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalValueCollection", "this[{0}].set: out of range (count={1})", index, _inner.combinedValues.Count);
                    throw new ArgumentOutOfRangeException("index");
                }

                if (value == null)
                    throw new ArgumentNullException("value");

                TrackedCollection<T>.ValueEl el = _inner.combinedValues[index];

                if (el.isInserted)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "this[{0}].set: is inserted {1}", index, value);
                    el.insertedValue = value;
                }
                else
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "this[{0}].set: is original {1}", index, value);
                    el.originalValue.Right = value;
                }
            }
        }

        //
        // ICollection<T>
        //
        public void CopyTo(T[] array, int index)
        {
            ((ICollection)this).CopyTo((Array)array, index);
        }

        public int Count
        {
            get
            {
                return _inner.Count;
            }
        }

        //
        // IEnumerable<T>
        //
        public IEnumerator<T> GetEnumerator()
        {
            return new ValueCollectionEnumerator<T>(_inner, _inner.combinedValues);
        }

        //
        // Private implementation
        //
        private TrackedCollection<T> _inner = new TrackedCollection<T>();

        //
        // Internal constructor
        //
        internal PrincipalValueCollection()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "Ctor");

            // Nothing to do here
        }

        //
        // Load/Store implementation
        //

        internal void Load(List<T> values)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "Load, count={0}", values.Count);

            // To support reload
            _inner.combinedValues.Clear();
            _inner.removedValues.Clear();

            foreach (T value in values)
            {
                // If T was a mutable reference type, would need to make a copy of value
                // for the left-side of the Pair, so that changes to the value in the
                // right-side wouldn't also change the left-side value (due to aliasing).
                // However, we constrain T to be either a value type or a immutable ref type (e.g., string)
                // to avoid this problem.
                TrackedCollection<T>.ValueEl el = new TrackedCollection<T>.ValueEl();
                el.isInserted = false;
                el.originalValue = new Pair<T, T>(value, value);

                _inner.combinedValues.Add(el);
            }
        }

        internal List<T> Inserted
        {
            get
            {
                return _inner.Inserted;
            }
        }

        internal List<T> Removed
        {
            get
            {
                return _inner.Removed;
            }
        }

        internal List<Pair<T, T>> ChangedValues
        {
            get
            {
                return _inner.ChangedValues;
            }
        }

        internal bool Changed
        {
            get
            {
                return _inner.Changed;
            }
        }

        // Resets the change-tracking of the collection to 'unchanged', by clearing out the removedValues,
        // changing inserted values into original values, and for all Pairs<T,T> in originalValues for which
        // the left-side does not equal the right-side, copying the right-side over the left-side
        internal void ResetTracking()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalValueCollection", "Entering ResetTracking");

            _inner.removedValues.Clear();

            foreach (TrackedCollection<T>.ValueEl el in _inner.combinedValues)
            {
                if (el.isInserted)
                {
                    GlobalDebug.WriteLineIf(
                                    GlobalDebug.Info,
                                    "PrincipalValueCollection",
                                    "ResetTracking: moving {0} (type {1}) from inserted to original",
                                    el.insertedValue,
                                    el.insertedValue.GetType());

                    el.isInserted = false;
                    el.originalValue = new Pair<T, T>(el.insertedValue, el.insertedValue);
                    //el.insertedValue = T.default;
                }
                else
                {
                    Pair<T, T> pair = el.originalValue;

                    if (!pair.Left.Equals(pair.Right))
                    {
                        GlobalDebug.WriteLineIf(
                                        GlobalDebug.Info,
                                        "PrincipalValueCollection",
                                        "ResetTracking: found changed original, left={0}, right={1}, type={2}",
                                        pair.Left,
                                        pair.Right,
                                        pair.Left.GetType());

                        pair.Left = pair.Right;
                    }
                }
            }
        }
    }
}
