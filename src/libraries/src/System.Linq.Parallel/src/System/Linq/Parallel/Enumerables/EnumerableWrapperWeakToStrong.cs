// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// EnumerableWrapperWeakToStrong.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System.Linq.Parallel
{
    /// <summary>
    /// A simple implementation of the IEnumerable{object} interface which wraps
    /// a weakly typed IEnumerable object, allowing it to be accessed as a strongly typed
    /// IEnumerable{object}.
    /// </summary>
    internal class EnumerableWrapperWeakToStrong : IEnumerable<object>
    {
        private readonly IEnumerable _wrappedEnumerable; // The wrapped enumerable object.

        //-----------------------------------------------------------------------------------
        // Instantiates a new wrapper object.
        //

        internal EnumerableWrapperWeakToStrong(IEnumerable wrappedEnumerable)
        {
            Contract.Assert(wrappedEnumerable != null);
            _wrappedEnumerable = wrappedEnumerable;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<object>)this).GetEnumerator();
        }

        public IEnumerator<object> GetEnumerator()
        {
            return new WrapperEnumeratorWeakToStrong(_wrappedEnumerable.GetEnumerator());
        }

        //-----------------------------------------------------------------------------------
        // A wrapper over IEnumerator that provides IEnumerator<object> interface
        //

        class WrapperEnumeratorWeakToStrong : IEnumerator<object>
        {
            private IEnumerator _wrappedEnumerator; // The weakly typed enumerator we've wrapped.

            //-----------------------------------------------------------------------------------
            // Wrap the specified enumerator in a new weak-to-strong converter.
            //

            internal WrapperEnumeratorWeakToStrong(IEnumerator wrappedEnumerator)
            {
                Contract.Assert(wrappedEnumerator != null);
                _wrappedEnumerator = wrappedEnumerator;
            }

            //-----------------------------------------------------------------------------------
            // These are all really simple IEnumerator<object> implementations that simply
            // forward to the corresponding weakly typed IEnumerator methods.
            //

            object IEnumerator.Current
            {
                get { return _wrappedEnumerator.Current; }
            }

            object IEnumerator<object>.Current
            {
                get { return _wrappedEnumerator.Current; }
            }

            void IDisposable.Dispose()
            {
                IDisposable disposable = _wrappedEnumerator as IDisposable;
                if (disposable != null)
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