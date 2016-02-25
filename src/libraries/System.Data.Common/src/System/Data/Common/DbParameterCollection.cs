// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Data;

namespace System.Data.Common
{
    public abstract class DbParameterCollection : IDataParameterCollection,
        ICollection, IEnumerable, IList
    {
        protected DbParameterCollection() : base()
        {
        }

        abstract public int Count
        {
            get;
        }

        bool IList.IsFixedSize
        {
            get { return false; }
        }

        virtual public bool IsReadOnly
        {
            get { return false; }
        }

        virtual public bool IsSynchronized
        {
            get { return false; }
        }

        abstract public object SyncRoot
        {
            get;
        }

        object IDataParameterCollection.this[string parameterName]
        {
            get
            {
                return GetParameter(parameterName);
            }
            set
            {
                SetParameter(parameterName, (DbParameter)value);
            }
        }

        object IList.this[int index]
        {
            get
            {
                return GetParameter(index);
            }
            set
            {
                SetParameter(index, (DbParameter)value);
            }
        }


        public DbParameter this[int index]
        {
            get
            {
                return GetParameter(index);
            }
            set
            {
                SetParameter(index, value);
            }
        }

        public DbParameter this[string parameterName]
        {
            get
            {
                return GetParameter(parameterName) as DbParameter;
            }
            set
            {
                SetParameter(parameterName, value);
            }
        }

        abstract public int Add(object value);

        abstract public void AddRange(System.Array values);

        abstract public bool Contains(object value);

        abstract public bool Contains(string value); // WebData 97349

        abstract public void CopyTo(System.Array array, int index);

        abstract public void Clear();

        abstract public IEnumerator GetEnumerator();

        abstract protected DbParameter GetParameter(int index);

        abstract protected DbParameter GetParameter(string parameterName);

        abstract public int IndexOf(object value);

        abstract public int IndexOf(string parameterName);

        abstract public void Insert(int index, object value);

        abstract public void Remove(object value);

        abstract public void RemoveAt(int index);

        abstract public void RemoveAt(string parameterName);

        abstract protected void SetParameter(int index, DbParameter value);

        abstract protected void SetParameter(string parameterName, DbParameter value);
    }
}
