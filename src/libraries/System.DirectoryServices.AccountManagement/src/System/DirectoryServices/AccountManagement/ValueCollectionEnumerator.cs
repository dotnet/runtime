// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    internal class ValueCollectionEnumerator<T> : IEnumerator<T>, IEnumerator
    // T must be a ValueType
    {
        //
        // Public properties
        //

        public T Current
        {
            get
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Entering Current");
                return _inner.Current;
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
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Entering MoveNext");
            return _inner.MoveNext();
        }

        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        public void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Entering Reset");
            _inner.Reset();
        }

        void IEnumerator.Reset()
        {
            Reset();
        }

        public void Dispose()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Entering Dispose");
            _inner.Dispose();
        }

        //
        // Internal constructors
        //
        internal ValueCollectionEnumerator(TrackedCollection<T> trackingList, List<TrackedCollection<T>.ValueEl> combinedValues)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Ctor");
            _inner = new TrackedCollectionEnumerator<T>("ValueCollectionEnumerator", trackingList, combinedValues);
        }

        //
        // Private implementation
        //

        private TrackedCollectionEnumerator<T> _inner;
    }
}

