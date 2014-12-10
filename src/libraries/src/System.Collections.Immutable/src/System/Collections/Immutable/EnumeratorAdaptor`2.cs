// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Collections.Immutable
{
    /// <summary>
    /// An adapter that allows a single foreach loop in C# to avoid
    /// boxing an enumerator when possible, but fallback to boxing when necessary.
    /// </summary>
    /// <typeparam name="T">The type of value to be enumerated.</typeparam>
    /// <typeparam name="TEnumerator">The type of the enumerator struct.</typeparam>
    internal struct EnumeratorAdaptor<T, TEnumerator>
        where TEnumerator : struct, IEnumerator<T>
    {
        /// <summary>
        /// The enumerator struct to use if <see cref="enumeratorObject"/> is <c>null</c>.
        /// </summary>
        private TEnumerator enumeratorStruct;

        /// <summary>
        /// The enumerator object to use if not null.
        /// </summary>
        private IEnumerator<T> enumeratorObject;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumeratorAdaptor{T, TEnumerator}"/> struct
        /// for enumerating over a strongly typed struct enumerator.
        /// </summary>
        /// <param name="enumerator">The initialized enumerator struct.</param>
        internal EnumeratorAdaptor(TEnumerator enumerator)
        {
            this.enumeratorStruct = enumerator;
            this.enumeratorObject = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EnumeratorAdaptor{T, TEnumerator}"/> struct
        /// for enumerating over a (boxed) <see cref="IEnumerable{T}"/> enumerator.
        /// </summary>
        /// <param name="enumerator">The initialized enumerator object.</param>
        internal EnumeratorAdaptor(IEnumerator<T> enumerator)
        {
            this.enumeratorStruct = default(TEnumerator);
            this.enumeratorObject = enumerator;
        }

        /// <summary>
        /// Gets the current enumerated value.
        /// </summary>
        public T Current
        {
            get { return enumeratorObject != null ? enumeratorObject.Current : enumeratorStruct.Current; }
        }

        /// <summary>
        /// Moves to the next value.
        /// </summary>
        public bool MoveNext()
        {
            if (enumeratorObject != null)
            {
                return enumeratorObject.MoveNext();
            }
            else
            {
                return enumeratorStruct.MoveNext();
            }
        }

        /// <summary>
        /// Returns a copy of this struct. 
        /// </summary>
        /// <remarks>
        /// This member is here so that it can be used in C# foreach loops.
        /// </remarks>
        public EnumeratorAdaptor<T, TEnumerator> GetEnumerator()
        {
            return this;
        }
    }
}
