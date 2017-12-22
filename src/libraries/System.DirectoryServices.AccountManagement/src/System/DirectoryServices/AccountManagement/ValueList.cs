// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Identity.Principals
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
        
            return inner.Add((T) value);
        }

        void IList.Clear()
        {
            Clear();
        }

        bool IList.Contains(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");   
        
            return inner.Contains((T) value);
        }

        int IList.IndexOf(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");   
        
            return IndexOf((T) value);
        }

        void IList.Insert(int index, object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");   
        
            Insert(index, (T) value);
        }

        void IList.Remove(object value)
        {
            if (value == null)
                throw new ArgumentNullException("value");
        
            inner.Remove((T) value);
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
            
                this[index] = (T) value;
            }
        }

        //
        // ICollection
        //
        void ICollection.CopyTo(Array array, int index)
        {
            ((ICollection)inner).CopyTo(array, index);
        }
        
        int ICollection.Count
        {
            get
            {
                return inner.Count;
            }
        }   

        bool ICollection.IsSynchronized
        {
            get
            {
                return ((ICollection)inner).IsSynchronized;
            }
        }

        object ICollection.SyncRoot
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
            return (IEnumerator) GetEnumerator();
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
        public int Add(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");   
        
            return inner.Add(value);
        }

        public void Clear()
        {
            inner.Clear();
        }

        public bool Contains(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");   
        
            return inner.Contains(value);
        }

        public int IndexOf(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");   

            int index = 0;

            foreach (TrackedCollection<T>.ValueEl el in inner.combinedValues)
            {
                if (el.isInserted && el.insertedValue.Equals(value))
                    return index;

                if (!el.isInserted && el.originalValue.Right.Equals(value))
                    return index;

                index++;
            }

            return -1;            
        }

        public void Insert(int index, T value)
        {
            inner.MarkChange();

            if (value == null)
                throw new ArgumentNullException("value");   

            if ((index < 0) || (index > inner.combinedValues.Count))
                throw new ArgumentOutOfRangeException("index");

            TrackedCollection<T>.ValueEl el = new TrackedCollection<T>.ValueEl();
            el.isInserted = true;
            el.insertedValue = value;
            
            inner.combinedValues.Insert(index, el);
        }

        public bool Remove(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");   
        
            return inner.Remove(value);
        }

        public void RemoveAt(int index)
        {
            inner.MarkChange();
            
            if ((index < 0) || (index >= inner.combinedValues.Count))
                throw new ArgumentOutOfRangeException("index");

            TrackedCollection<T>.ValueEl el = inner.combinedValues[index];

            if (el.isInserted)
            {
                // We're removing an inserted value.
                inner.combinedValues.RemoveAt(index);
            }
            else
            {
                // We're removing an original value.
                Pair<T,T> pair = inner.combinedValues[index].originalValue;
                inner.combinedValues.RemoveAt(index);   
                inner.removedValues.Add(pair.Left);                
            }            
        }

        public T this[int index]
        {
            get
            {
                if ((index < 0) || (index >= inner.combinedValues.Count))
                    throw new ArgumentOutOfRangeException("index");

                TrackedCollection<T>.ValueEl el = inner.combinedValues[index];

                if (el.isInserted)
                {
                    return el.insertedValue;
                }
                else
                {
                    return el.originalValue.Right;  // Right == current value
                }            
            }

            set
            {
                inner.MarkChange();

                if ((index < 0) || (index >= inner.combinedValues.Count))
                    throw new ArgumentOutOfRangeException("index");

                if (value == null)
                    throw new ArgumentNullException("value");   

                TrackedCollection<T>.ValueEl el = inner.combinedValues[index];

                if (el.isInserted)
                {
                    el.insertedValue = value;
                }
                else
                {
                    el.originalValue.Right = value;
                }                
            }
        }

        //
        // ICollection<T>
        //
        public void CopyTo(T[] array, int index)
        {
            ((ICollection)this).CopyTo((Array) array, index);
        }
        
        public int Count
        {
            get
            {
                return inner.Count;
            }
        }

        //
        // IEnumerable<T>
        //
        public IEnumerator<T> GetEnumerator()
        {
            return new ValueCollectionEnumerator<T>(inner, inner.combinedValues);
        }

        //
        // Private implementation
        //
        TrackedCollection<T> inner = new TrackedCollection<T>();

        //
        // Internal constructor
        //
        internal PrincipalValueCollection()
        {
            // Nothing to do here
        }
        
        //
        // Load/Store implementation
        //

        internal void Load(List<T> values)
        {
            // To support reload
            inner.combinedValues.Clear();
            inner.removedValues.Clear();

            
            foreach (T value in values)
            {
                // If T was a mutable reference type, would need to make a copy of value
                // for the left-side of the Pair, so that changes to the value in the
                // right-side wouldn't also change the left-side value (due to aliasing).
                // However, we constrain T to be either a value type or a immutable ref type (e.g., string)
                // to avoid this problem.
                TrackedCollection<T>.ValueEl el = new TrackedCollection<T>.ValueEl();
                el.isInserted = false;
                el.originalValue = new Pair<T,T>(value, value);

                inner.combinedValues.Add(el);
            }
        }

        internal List<T> Inserted
        {
            get
            {
                return inner.Inserted;
            }
        }

        internal List<T> Removed
        {
            get
            {
                return inner.Removed;
            }
        }

        internal List<Pair<T,T>> ChangedValues
        {
            get
            {
                return inner.ChangedValues;
            }
        }

        internal bool Changed
        {
            get
            {
                return inner.Changed;
            }
        }

        // Resets the change-tracking of the collection to 'unchanged', by clearing out the removedValues,
        // changing inserted values into original values, and for all Pairs<T,T> in originalValues for which
        // the left-side does not equal the right-side, copying the right-side over the left-side
        internal void ResetTracking()
        {
            inner.removedValues.Clear();

            foreach (TrackedCollection<T>.ValueEl el in inner.combinedValues)
            {
                if (el.isInserted)
                {
                    el.isInserted = false;
                    el.originalValue = new Pair<T,T>(el.insertedValue, el.insertedValue);
                    el.insertedValue = T.default;
                }
                else
                {
                    Pair<T,T> pair = el.originalValue;

                    if (!pair.Left.Equals(pair.Right))
                        pair.Left = pair.Right;            
                }
            }
        }
    }
}
