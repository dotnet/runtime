// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// QueryResults.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq.Parallel
{
    /// <summary>
    /// The QueryResults{T} is a class representing the results of the query. There may
    /// be different ways the query results can be manipulated. Currently, two ways are
    /// supported:
    ///
    /// 1. Open the query results as a partitioned stream by calling GivePartitionedStream
    ///    and pass a generic action as an argument.
    ///    
    /// 2. Access individual elements of the results list by calling GetElement(index) and
    ///    ElementsCount. This method of accessing the query results is available only if
    ///    IsIndexible return true. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal abstract class QueryResults<T> : IList<T>
    {
        //-----------------------------------------------------------------------------------
        // Gets the query results represented as a partitioned stream. Instead of returning
        // the PartitionedStream, we instead call recipient.Receive<TKey>(...). That way,
        // the code that receives the partitioned stream has access to the TKey type.
        //
        // Arguments:
        //    recipient - the object that the partitioned stream will be passed to
        //

        internal abstract void GivePartitionedStream(IPartitionedStreamRecipient<T> recipient);

        //-----------------------------------------------------------------------------------
        // Returns whether the query results are indexable. If this property is true, the
        // user can call GetElement(index) and ElementsCount. If it is false, both
        // GetElement(index) and ElementsCount should throw InvalidOperationException.
        //

        internal virtual bool IsIndexible
        {
            get { return false; }
        }

        //-----------------------------------------------------------------------------------
        // Returns index-th element in the query results
        //
        // Assumptions:
        //    IsIndexible returns true
        //    0 <= index < ElementsCount
        //    

        internal virtual T GetElement(int index)
        {
            Debug.Fail("GetElement property is not supported by non-indexable query results");
            throw new NotSupportedException();
        }

        //-----------------------------------------------------------------------------------
        // Returns the number of elements in the query results
        //
        // Assumptions:
        //    IsIndexible returns true
        //   

        internal virtual int ElementsCount
        {
            get
            {
                Debug.Fail("ElementsCount property is not supported by non-indexable query results");
                throw new NotSupportedException();
            }
        }

        //
        // An assortment of methods we need to support in order to implement the IList interface
        //

        int IList<T>.IndexOf(T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        public T this[int index]
        {
            get { return GetElement(index); }
            set
            {
                throw new NotSupportedException();
            }
        }

        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            throw new NotSupportedException();
        }

        public int Count
        {
            get { return ElementsCount; }
        }

        bool ICollection<T>.IsReadOnly
        {
            get { return true; }
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            for (int index = 0; index < Count; index++)
            {
                yield return this[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<T>)this).GetEnumerator();
        }
    }
}
