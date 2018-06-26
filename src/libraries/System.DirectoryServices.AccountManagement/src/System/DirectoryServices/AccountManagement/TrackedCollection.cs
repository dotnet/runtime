// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    internal class TrackedCollection<T> : ICollection<T>, ICollection, IEnumerable<T>, IEnumerable
    {
        //
        // ICollection
        //
        void ICollection.CopyTo(Array array, int index)
        {
            // Parameter validation
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (array.Rank != 1)
                throw new ArgumentException(SR.TrackedCollectionNotOneDimensional);

            if (index >= array.GetLength(0))
                throw new ArgumentException(SR.TrackedCollectionIndexNotInArray);

            // Make sure the array has enough space, allowing for the "index" offset
            if ((array.GetLength(0) - index) < this.combinedValues.Count)
                throw new ArgumentException(SR.TrackedCollectionArrayTooSmall);

            // Copy out the original and inserted values
            foreach (ValueEl el in this.combinedValues)
            {
                array.SetValue(el.GetCurrentValue(), index);
                checked { index++; }
            }
        }

        int ICollection.Count
        {
            get
            {
                return Count;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                // The classes that wrap us should expose themselves as the sync root.
                // Hence, this should never be called.
                Debug.Fail("TrackedCollection.SyncRoot: shouldn't be here.");
                return this;
            }
        }

        //
        // IEnumerable
        //
        IEnumerator IEnumerable.GetEnumerator()
        {
            Debug.Fail("TrackedCollection.IEnumerable.GetEnumerator(): should not be here");
            return (IEnumerator)GetEnumerator();
        }

        //
        // ICollection<T>
        //

        public void CopyTo(T[] array, int index)
        {
            ((ICollection)this).CopyTo((Array)array, index);
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public int Count
        {
            get
            {
                // Note that any values removed by the application have already been removed
                // from combinedValues, so we don't need to adjust for that here
                return this.combinedValues.Count;
            }
        }

        //
        // IEnumerable<T>
        //
        public IEnumerator<T> GetEnumerator()
        {
            Debug.Fail("TrackedCollection.GetEnumerator(): should not be here");
            return new TrackedCollectionEnumerator<T>("TrackedCollectionEnumerator", this, this.combinedValues);
        }

        //
        //
        //
        public bool Contains(T value)
        {
            // Is it one of the inserted or original values?
            foreach (ValueEl el in this.combinedValues)
            {
                if (el.GetCurrentValue().Equals(value))
                    return true;
            }

            return false;
        }

        public void Clear()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollection", "Clear");

            MarkChange();

            // Move all original values to the removed values list
            foreach (ValueEl el in this.combinedValues)
            {
                if (!el.isInserted)
                {
                    this.removedValues.Add(el.originalValue.Left);
                }
            }

            // Remove all inserted values, and clean up the original values
            // (which have been moved onto the removed value list)
            this.combinedValues.Clear();
        }

        // Adds obj to the end of the list by inserting it into combinedValues.
        public void Add(T o)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollection", "Add({0})", o.ToString());

            MarkChange();

            ValueEl el = new ValueEl();
            el.isInserted = true;
            el.insertedValue = o;

            this.combinedValues.Add(el);
        }

        // If obj is an inserted value, removes it.
        // Otherwise, if obj is in the right-side of a pair of an original value, removes that pair from combinedValues
        // and adds the left-side of that pair to removedValues, to record the removal.
        public bool Remove(T value)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollection", "Remove({0})", value.ToString());

            MarkChange();

            foreach (ValueEl el in this.combinedValues)
            {
                if (el.isInserted && el.insertedValue.Equals(value))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollection", "found value to remove on inserted");

                    this.combinedValues.Remove(el);
                    return true;
                }

                if (!el.isInserted && el.originalValue.Right.Equals(value))
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "TrackedCollection", "found value to remove on original");

                    this.combinedValues.Remove(el);
                    this.removedValues.Add(el.originalValue.Left);
                    return true;
                }
            }

            return false;
        }

        //
        // Private implementation
        //

        internal class ValueEl
        {
            public bool isInserted;

            //public T insertedValue = T.default;
            public T insertedValue;

            public Pair<T, T> originalValue = null;

            public T GetCurrentValue()
            {
                if (this.isInserted)
                    return this.insertedValue;
                else
                    return this.originalValue.Right;    // Right == current value
            }
        }

        // Contains both the values retrieved from the store object backing this property.
        // If isInserted == true, it is an inserted value and is stored in insertedValue.
        // If isInserted == false, it is an original value and is stored in originalValue.
        // For each originalValue Pair, the left side contains a copy of the value as it was originally retrieved from the store,
        // while the right side contains the current value (which differs from the left side iff the application
        // modified the value).        
        internal List<ValueEl> combinedValues = new List<ValueEl>();

        // Contains values removed by the application for which the removal has not yet been committed
        // to the store.
        internal List<T> removedValues = new List<T>();

        // Used so our enumerator can detect changes to the collection and throw an exception
        private DateTime _lastChange = DateTime.UtcNow;

        internal DateTime LastChange
        {
            get { return _lastChange; }
        }

        internal void MarkChange()
        {
            _lastChange = DateTime.UtcNow;
        }

        //
        // Shared Load/Store implementation
        //

        internal List<T> Inserted
        {
            get
            {
                List<T> insertedValues = new List<T>();

                foreach (ValueEl el in this.combinedValues)
                {
                    if (el.isInserted)
                        insertedValues.Add(el.insertedValue);
                }

                return insertedValues;
            }
        }

        internal List<T> Removed
        {
            get
            {
                return this.removedValues;
            }
        }

        internal List<Pair<T, T>> ChangedValues
        {
            get
            {
                List<Pair<T, T>> changedList = new List<Pair<T, T>>();

                foreach (ValueEl el in this.combinedValues)
                {
                    if (!el.isInserted)
                    {
                        if (!el.originalValue.Left.Equals(el.originalValue.Right))
                        {
                            // Don't need to worry about whether we need to copy the T,
                            // since we're not handing it out to the app and we'll internally treat it as read-only                    
                            changedList.Add(new Pair<T, T>(el.originalValue.Left, el.originalValue.Right));
                        }
                    }
                }

                return changedList;
            }
        }

        internal bool Changed
        {
            get
            {
                // Do the cheap test first: have any values been removed?
                if (this.removedValues.Count > 0)
                    return true;

                // have to do the comparatively expensive test: have any values been inserted or changed?
                foreach (ValueEl el in this.combinedValues)
                {
                    // Inserted
                    if (el.isInserted)
                        return true;

                    // Changed
                    if (!el.originalValue.Left.Equals(el.originalValue.Right))
                        return true;
                }

                return false;
            }
        }
    }
}
