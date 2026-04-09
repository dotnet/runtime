// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// EnumerableWrapperWeakToStrong.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A simple implementation of the IEnumerable{object} interface which wraps
    /// a weakly typed IEnumerable object, allowing it to be accessed as a strongly typed
    /// IEnumerable{object}.
    /// </summary>
    internal sealed class EnumerableWrapperWeakToStrong : IEnumerable<object?>
    {
        private readonly IEnumerable _wrappedEnumerable; // The wrapped enumerable object.

        //-----------------------------------------------------------------------------------
        // Instantiates a new wrapper object.
        //

        internal EnumerableWrapperWeakToStrong(IEnumerable wrappedEnumerable)
        {
            Debug.Assert(wrappedEnumerable != null);
            _wrappedEnumerable = wrappedEnumerable;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<object?>)this).GetEnumerator();
        }

        public IEnumerator<object?> GetEnumerator()
        {
            return new WrapperEnumeratorWeakToStrong(_wrappedEnumerable.GetEnumerator());
        }

        //-----------------------------------------------------------------------------------
        // A wrapper over IEnumerator that provides IEnumerator<object> interface
        //

        private sealed class WrapperEnumeratorWeakToStrong : IEnumerator<object?>
        {
            private readonly IEnumerator _wrappedEnumerator; // The weakly typed enumerator we've wrapped.

            //-----------------------------------------------------------------------------------
            // Wrap the specified enumerator in a new weak-to-strong converter.
            //

            internal WrapperEnumeratorWeakToStrong(IEnumerator wrappedEnumerator)
            {
                Debug.Assert(wrappedEnumerator != null);
                _wrappedEnumerator = wrappedEnumerator;
            }

            //-----------------------------------------------------------------------------------
            // These are all really simple IEnumerator<object> implementations that simply
            // forward to the corresponding weakly typed IEnumerator methods.
            //

            object? IEnumerator.Current
            {
                get { return _wrappedEnumerator.Current; }
            }

            object? IEnumerator<object?>.Current
            {
                get { return _wrappedEnumerator.Current; }
            }

            void IDisposable.Dispose()
            {
                if (_wrappedEnumerator is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            bool IEnumerator.MoveNext()
            {
                return _wrappedEnumerator.MoveNext();
            }

            void IEnumerator.Reset()
            {
                _wrappedEnumerator.Reset();
            }
        }
    }
}
