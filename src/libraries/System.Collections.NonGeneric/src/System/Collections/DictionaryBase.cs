// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class:  DictionaryBase
**
** Purpose: Provides the abstract base class for a
**          strongly typed collection of key/value pairs.
**
===========================================================*/


namespace System.Collections
{
    // Useful base class for typed read/write collections where items derive from object
    public abstract class DictionaryBase : IDictionary
    {
        private Hashtable _hashtable;

        protected Hashtable InnerHashtable
        {
            get
            {
                if (_hashtable == null)
                    _hashtable = new Hashtable();
                return _hashtable;
            }
        }

        protected IDictionary Dictionary
        {
            get { return (IDictionary)this; }
        }

        public int Count
        {
            // to avoid newing inner list if no items are ever added
            get { return _hashtable == null ? 0 : _hashtable.Count; }
        }

        bool IDictionary.IsReadOnly
        {
            get { return InnerHashtable.IsReadOnly; }
        }

        bool IDictionary.IsFixedSize
        {
            get { return InnerHashtable.IsFixedSize; }
        }

        bool ICollection.IsSynchronized
        {
            get { return InnerHashtable.IsSynchronized; }
        }

        ICollection IDictionary.Keys
        {
            get { return InnerHashtable.Keys; }
        }

        object ICollection.SyncRoot
        {
            get { return InnerHashtable.SyncRoot; }
        }

        ICollection IDictionary.Values
        {
            get { return InnerHashtable.Values; }
        }

        public void CopyTo(Array array, int index)
        {
            InnerHashtable.CopyTo(array, index);
        }

        object IDictionary.this[object key]
        {
            get
            {
                object currentValue = InnerHashtable[key];
                OnGet(key, currentValue);
                return currentValue;
            }
            set
            {
                OnValidate(key, value);
                bool keyExists = true;
                object temp = InnerHashtable[key];
                if (temp == null)
                {
                    keyExists = InnerHashtable.Contains(key);
                }

                OnSet(key, temp, value);
                InnerHashtable[key] = value;
                try
                {
                    OnSetComplete(key, temp, value);
                }
                catch
                {
                    if (keyExists)
                    {
                        InnerHashtable[key] = temp;
                    }
                    else
                    {
                        InnerHashtable.Remove(key);
                    }
                    throw;
                }
            }
        }

        bool IDictionary.Contains(object key)
        {
            return InnerHashtable.Contains(key);
        }

        void IDictionary.Add(object key, object value)
        {
            OnValidate(key, value);
            OnInsert(key, value);
            InnerHashtable.Add(key, value);
            try
            {
                OnInsertComplete(key, value);
            }
            catch
            {
                InnerHashtable.Remove(key);
                throw;
            }
        }

        public void Clear()
        {
            OnClear();
            InnerHashtable.Clear();
            OnClearComplete();
        }

        void IDictionary.Remove(object key)
        {
            if (InnerHashtable.Contains(key))
            {
                object temp = InnerHashtable[key];
                OnValidate(key, temp);
                OnRemove(key, temp);

                InnerHashtable.Remove(key);
                try
                {
                    OnRemoveComplete(key, temp);
                }
                catch
                {
                    InnerHashtable.Add(key, temp);
                    throw;
                }
            }
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return InnerHashtable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnerHashtable.GetEnumerator();
        }

        protected virtual object OnGet(object key, object currentValue)
        {
            return currentValue;
        }

        protected virtual void OnSet(object key, object oldValue, object newValue)
        {
        }

        protected virtual void OnInsert(object key, object value)
        {
        }

        protected virtual void OnClear()
        {
        }

        protected virtual void OnRemove(object key, object value)
        {
        }

        protected virtual void OnValidate(object key, object value)
        {
        }

        protected virtual void OnSetComplete(object key, object oldValue, object newValue)
        {
        }

        protected virtual void OnInsertComplete(object key, object value)
        {
        }

        protected virtual void OnClearComplete()
        {
        }

        protected virtual void OnRemoveComplete(object key, object value)
        {
        }
    }
}
