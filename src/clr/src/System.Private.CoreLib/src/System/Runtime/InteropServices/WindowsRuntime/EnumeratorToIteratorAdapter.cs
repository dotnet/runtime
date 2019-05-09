// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // This is a set of stub methods implementing the support for the IIterable`1 interface on managed
    // objects that implement IEnumerable`1. Used by the interop mashaling infrastructure.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not EnumerableToIterableAdapter objects. Rather, they are of type
    // IEnumerable<T>. No actual EnumerableToIterableAdapter object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" to "IEnumerable<T>". 
    internal sealed class EnumerableToIterableAdapter
    {
        private EnumerableToIterableAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        // This method is invoked when First is called on a managed implementation of IIterable<T>.
        internal IIterator<T> First_Stub<T>()
        {
            IEnumerable<T> _this = Unsafe.As<IEnumerable<T>>(this);
            return new EnumeratorToIteratorAdapter<T>(_this.GetEnumerator());
        }
    }

    internal sealed class EnumerableToBindableIterableAdapter
    {
        private EnumerableToBindableIterableAdapter()
        {
            Debug.Fail("This class is never instantiated");
        }

        internal sealed class NonGenericToGenericEnumerator : IEnumerator<object?>
        {
            private IEnumerator enumerator;

            public NonGenericToGenericEnumerator(IEnumerator enumerator)
            { this.enumerator = enumerator; }

            public object? Current { get { return enumerator.Current; } }
            public bool MoveNext() { return enumerator.MoveNext(); }
            public void Reset() { enumerator.Reset(); }
            public void Dispose() { }
        }

        // This method is invoked when First is called on a managed implementation of IBindableIterable.
        internal IBindableIterator First_Stub()
        {
            IEnumerable _this = Unsafe.As<IEnumerable>(this);
            return new EnumeratorToIteratorAdapter<object?>(new NonGenericToGenericEnumerator(_this.GetEnumerator()));
        }
    }

    // Adapter class which holds a managed IEnumerator<T>, exposing it as a Windows Runtime IIterator<T>
    internal sealed class EnumeratorToIteratorAdapter<T> : IIterator<T>, IBindableIterator
    {
        private IEnumerator<T> m_enumerator;
        private bool m_firstItem = true;
        private bool m_hasCurrent;

        internal EnumeratorToIteratorAdapter(IEnumerator<T> enumerator)
        {
            Debug.Assert(enumerator != null);
            m_enumerator = enumerator;
        }

        public T Current
        {
            get
            {
                // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
                // first access to the iterator we need to advance to the first item.
                if (m_firstItem)
                {
                    m_firstItem = false;
                    MoveNext();
                }

                if (!m_hasCurrent)
                {
                    throw WindowsRuntimeMarshal.GetExceptionForHR(HResults.E_BOUNDS, null);
                }

                return m_enumerator.Current;
            }
        }

        object? IBindableIterator.Current
        {
            get
            {
                return ((IIterator<T>)this).Current;
            }
        }

        public bool HasCurrent
        {
            get
            {
                // IEnumerator starts at item -1, while IIterators start at item 0.  Therefore, if this is the
                // first access to the iterator we need to advance to the first item.
                if (m_firstItem)
                {
                    m_firstItem = false;
                    MoveNext();
                }

                return m_hasCurrent;
            }
        }

        public bool MoveNext()
        {
            try
            {
                m_hasCurrent = m_enumerator.MoveNext();
            }
            catch (InvalidOperationException e)
            {
                throw WindowsRuntimeMarshal.GetExceptionForHR(HResults.E_CHANGED_STATE, e);
            }

            return m_hasCurrent;
        }

        public int GetMany(T[] items)
        {
            if (items == null)
            {
                return 0;
            }

            int index = 0;
            while (index < items.Length && HasCurrent)
            {
                items[index] = Current;
                MoveNext();
                ++index;
            }

            if (typeof(T) == typeof(string))
            {
                string[] stringItems = (items as string[])!;

                // Fill the rest of the array with string.Empty to avoid marshaling failure
                for (int i = index; i < items.Length; ++i)
                    stringItems[i] = string.Empty;
            }

            return index;
        }
    }
}
