/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    ValueCollectionEnumerator.cs

Abstract:

    Implements the ValueCollectionEnumerator<T> class.

History:

    10-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.Identity.Principals
{
    public class ValueCollectionEnumerator<T> : IEnumerator<T>, IEnumerator
    // T must be a ValueType
    {

        //
        // Public properties
        //

        public T Current
        {
            get
            {
                return inner.Current;
            }
        }

        

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }


        //
        // Public methods
        //

        public bool MoveNext()
        {
            return inner.MoveNext();
        }

        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        public void Reset()
        {
            inner.Reset();
        }

        void IEnumerator.Reset()
        {
            Reset();
        }

        public void Dispose()
        {
            inner.Dispose();
        }


        //
        // Internal constructors
        //
        internal ValueCollectionEnumerator(TrackedCollection<T> trackingList, List<TrackedCollection<T>.ValueEl> combinedValues)
        {
            inner = new TrackedCollectionEnumerator<T>("ValueCollectionEnumerator", trackingList, combinedValues);
        }


        //
        // Private implementation
        //

        TrackedCollectionEnumerator<T> inner;
    
    }
}

