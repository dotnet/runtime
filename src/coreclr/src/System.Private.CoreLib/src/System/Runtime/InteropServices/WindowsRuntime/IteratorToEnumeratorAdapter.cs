// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    internal delegate IEnumerator<T> GetEnumerator_Delegate<out T>();

    // This is a set of stub methods implementing the support for the IEnumerable`1 interface on WinRT
    // objects that implement IIterable`1. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not IterableToEnumerableAdapter objects. Rather, they are of type
    // IIterable<T>. No actual IterableToEnumerableAdapter object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" to "IIterable<T>". 
    internal sealed class IterableToEnumerableAdapter
    {
        private IterableToEnumerableAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // This method is invoked when GetEnumerator is called on a WinRT-backed implementation of IEnumerable<T>.
        internal IEnumerator<T> GetEnumerator_Stub<T>()
        {
            IIterable<T> _this = Unsafe.As<IIterable<T>>(this);
            return new IteratorToEnumeratorAdapter<T>(_this.First());
        }

        // This method is invoked when GetEnumerator is called on a WinRT-backed implementation of IEnumerable<T>
        // and it is possible that the implementation supports IEnumerable<Type>/IEnumerable<string>/IEnumerable<Exception>/
        // IEnumerable<array>/IEnumerable<delegate> rather than IEnumerable<T> because T is assignable from Type/string/
        // Exception/array/delegate via co-variance.
        internal IEnumerator<T> GetEnumerator_Variance_Stub<T>() where T : class
        {
            bool fUseString;
            Delegate target = System.StubHelpers.StubHelpers.GetTargetForAmbiguousVariantCall(
                this,
                typeof(IEnumerable<T>).TypeHandle.Value,
                out fUseString);

            if (target != null)
            {
                return (Unsafe.As<GetEnumerator_Delegate<T>>(target))();
            }

            if (fUseString)
            {
                return Unsafe.As<IEnumerator<T>>(GetEnumerator_Stub<string>());
            }

            return GetEnumerator_Stub<T>();
        }
    }

    internal sealed class BindableIterableToEnumerableAdapter
    {
        private BindableIterableToEnumerableAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        private sealed class NonGenericToGenericIterator : IIterator<object?>
        {
            private IBindableIterator iterator;

            public NonGenericToGenericIterator(IBindableIterator iterator)
            { this.iterator = iterator; }

            public object? Current { get { return iterator.Current; } }
            public bool HasCurrent { get { return iterator.HasCurrent; } }
            public bool MoveNext() { return iterator.MoveNext(); }
            public int GetMany(object?[] items) { throw new NotSupportedException(); }
        }

        // This method is invoked when GetEnumerator is called on a WinRT-backed implementation of IEnumerable.
        internal IEnumerator GetEnumerator_Stub()
        {
            IBindableIterable _this = Unsafe.As<IBindableIterable>(this);
            return new IteratorToEnumeratorAdapter<object?>(new NonGenericToGenericIterator(_this.First()));
        }
    }

    // Adapter class which holds a Windows Runtime IIterator<T>, exposing it as a managed IEnumerator<T>


    // There are a few implementation differences between the Iterator and IEnumerator which need to be 
    // addressed. Iterator starts at index 0 while IEnumerator starts at index -1 as a result of which 
    // the first call to IEnumerator.Current is correct only after calling MoveNext(). 
    // Also IEnumerator throws an exception when we call Current after reaching the end of collection.
    internal sealed class IteratorToEnumeratorAdapter<T> : IEnumerator<T>
    {
        private IIterator<T> m_iterator;
        private bool m_hadCurrent;
        private T m_current = default!; // TODO-NULLABLE-GENERIC
        private bool m_isInitialized;

        internal IteratorToEnumeratorAdapter(IIterator<T> iterator)
        {
            Debug.Assert(iterator != null);
            m_iterator = iterator;
            m_hadCurrent = true;
            m_isInitialized = false;
        }

        public T Current
        {
            get
            {
                // The enumerator has not been advanced to the first element yet.
                if (!m_isInitialized)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                // The enumerator has reached the end of the collection
                if (!m_hadCurrent)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
                return m_current;
            }
        }

        object? IEnumerator.Current
        {
            get
            {
                // The enumerator has not been advanced to the first element yet.
                if (!m_isInitialized)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumNotStarted();
                // The enumerator has reached the end of the collection
                if (!m_hadCurrent)
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumEnded();
                return m_current;
            }
        }

        public bool MoveNext()
        {
            // If we've passed the end of the iteration, IEnumerable<T> should return false, while
            // IIterable will fail the interface call
            if (!m_hadCurrent)
            {
                return false;
            }

            // IIterators start at index 0, rather than -1.  If this is the first call, we need to just
            // check HasCurrent rather than actually moving to the next element
            try
            {
                if (!m_isInitialized)
                {
                    m_hadCurrent = m_iterator.HasCurrent;
                    m_isInitialized = true;
                }
                else
                {
                    m_hadCurrent = m_iterator.MoveNext();
                }

                // We want to save away the current value for two reasons:
                //  1. Accessing .Current is cheap on other iterators, so having it be a property which is a
                //     simple field access preserves the expected performance characteristics (as opposed to
                //     triggering a COM call every time the property is accessed)
                //
                //  2. This allows us to preserve the same semantics as generic collection iteration when iterating
                //     beyond the end of the collection - namely that Current continues to return the last value
                //     of the collection
                if (m_hadCurrent)
                {
                    m_current = m_iterator.Current;
                }
            }
            catch (Exception e)
            {
                // Translate E_CHANGED_STATE into an InvalidOperationException for an updated enumeration
                if (Marshal.GetHRForException(e) == HResults.E_CHANGED_STATE)
                {
                    ThrowHelper.ThrowInvalidOperationException_InvalidOperation_EnumFailedVersion();
                }
                else
                {
                    throw;
                }
            }

            return m_hadCurrent;
        }

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
