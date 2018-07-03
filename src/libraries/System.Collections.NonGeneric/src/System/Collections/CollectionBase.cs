// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** Class: CollectionBase
**
** Purpose: Provides the abstract base class for a strongly typed collection.
**
=============================================================================*/

namespace System.Collections
{
    // Useful base class for typed read/write collections where items derive from object
    public abstract class CollectionBase : IList
    {
        private ArrayList _list;

        protected CollectionBase()
        {
            _list = new ArrayList();
        }

        protected CollectionBase(int capacity)
        {
            _list = new ArrayList(capacity);
        }


        protected ArrayList InnerList
        {
            get
            {
                return _list;
            }
        }

        protected IList List
        {
            get { return (IList)this; }
        }

        public int Capacity
        {
            get
            {
                return InnerList.Capacity;
            }
            set
            {
                InnerList.Capacity = value;
            }
        }


        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public void Clear()
        {
            OnClear();
            InnerList.Clear();
            OnClearComplete();
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            object temp = InnerList[index];
            OnValidate(temp);
            OnRemove(index, temp);
            InnerList.RemoveAt(index);
            try
            {
                OnRemoveComplete(index, temp);
            }
            catch
            {
                InnerList.Insert(index, temp);
                throw;
            }
        }

        bool IList.IsReadOnly
        {
            get { return InnerList.IsReadOnly; }
        }

        bool IList.IsFixedSize
        {
            get { return InnerList.IsFixedSize; }
        }

        bool ICollection.IsSynchronized
        {
            get { return InnerList.IsSynchronized; }
        }

        object ICollection.SyncRoot
        {
            get { return InnerList.SyncRoot; }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        object IList.this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
                return InnerList[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
                OnValidate(value);
                object temp = InnerList[index];
                OnSet(index, temp, value);
                InnerList[index] = value;
                try
                {
                    OnSetComplete(index, temp, value);
                }
                catch
                {
                    InnerList[index] = temp;
                    throw;
                }
            }
        }

        bool IList.Contains(object value)
        {
            return InnerList.Contains(value);
        }

        int IList.Add(object value)
        {
            OnValidate(value);
            OnInsert(InnerList.Count, value);
            int index = InnerList.Add(value);
            try
            {
                OnInsertComplete(index, value);
            }
            catch
            {
                InnerList.RemoveAt(index);
                throw;
            }
            return index;
        }


        void IList.Remove(object value)
        {
            OnValidate(value);
            int index = InnerList.IndexOf(value);
            if (index < 0) throw new ArgumentException(SR.Arg_RemoveArgNotFound);
            OnRemove(index, value);
            InnerList.RemoveAt(index);
            try
            {
                OnRemoveComplete(index, value);
            }
            catch
            {
                InnerList.Insert(index, value);
                throw;
            }
        }

        int IList.IndexOf(object value)
        {
            return InnerList.IndexOf(value);
        }

        void IList.Insert(int index, object value)
        {
            if (index < 0 || index > Count)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_Index);
            OnValidate(value);
            OnInsert(index, value);
            InnerList.Insert(index, value);
            try
            {
                OnInsertComplete(index, value);
            }
            catch
            {
                InnerList.RemoveAt(index);
                throw;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        protected virtual void OnSet(int index, object oldValue, object newValue)
        {
        }

        protected virtual void OnInsert(int index, object value)
        {
        }

        protected virtual void OnClear()
        {
        }

        protected virtual void OnRemove(int index, object value)
        {
        }

        protected virtual void OnValidate(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
        }

        protected virtual void OnSetComplete(int index, object oldValue, object newValue)
        {
        }

        protected virtual void OnInsertComplete(int index, object value)
        {
        }

        protected virtual void OnClearComplete()
        {
        }

        protected virtual void OnRemoveComplete(int index, object value)
        {
        }
    }
}
