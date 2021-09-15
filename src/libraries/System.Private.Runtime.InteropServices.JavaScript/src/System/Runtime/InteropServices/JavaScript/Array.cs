// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Initializes a new instance of JavaScript Core Array class.
    /// </summary>
    public class Array : CoreObject
    {
        /// <summary>
        /// Initializes a new instance of the Array class.
        /// </summary>
        /// <param name="_params">Parameters.</param>
        public Array(params object[] _params) : base(nameof(Array), _params)
        { }

        /// <summary>
        /// Initializes a new instance of the Array/> class.
        /// </summary>
        /// <param name="jsHandle">Js handle.</param>
        internal Array(IntPtr jsHandle) : base(jsHandle)
        { }

        /// <summary>
        /// Push the specified elements.
        /// </summary>
        /// <returns>The new length of the Array push was called on</returns>
        /// <param name="elements">Elements.</param>
        public int Push(params object[] elements) => (int)Invoke("push", elements);

        /// <summary>
        /// Pop this instance.
        /// </summary>
        /// <returns>The element removed from the array or null if the array was empty</returns>
        public object Pop() => (object)Invoke("pop");

        /// <summary>
        /// Remove the first element of the Array and return that element
        /// </summary>
        /// <returns>The removed element</returns>
        public object Shift() => Invoke("shift");

        /// <summary>
        /// Add <paramref name="elements"/> to the array starting at index <c>0</c>
        /// </summary>
        /// <returns>The length after shift.</returns>
        /// <param name="elements">Elements.</param>
        public int UnShift(params object[] elements) => (int)Invoke("unshift", elements);

        /// <summary>
        /// Index of the search element.
        /// </summary>
        /// <returns>The index of first occurrence of searchElement in the Array or -1 if not Found</returns>
        /// <param name="searchElement">Search element.</param>
        /// <param name="fromIndex">The index to start the search from</param>
        public int IndexOf(object searchElement, int fromIndex = 0) => (int)Invoke("indexOf", searchElement, fromIndex);

        /// <summary>
        /// Finds the index of the last occurrence of<paramref name="searchElement" />
        /// </summary>
        /// <returns>The index of the last occurrence</returns>
        /// <param name="searchElement">Search element.</param>
        public int LastIndexOf(object searchElement) => (int)Invoke("lastIndexOf", searchElement);

        /// <summary>
        /// Finds the index of the last occurrence of<paramref name="searchElement" /> between 0 and <paramref name="endIndex" />.
        /// </summary>
        /// <returns>The index of the last occurrence.</returns>
        /// <param name="searchElement">Search element.</param>
        /// <param name="endIndex">End index.</param>
        public int LastIndexOf(object searchElement, int endIndex) => (int)Invoke("lastIndexOf", searchElement, endIndex);

        /// <summary>
        /// Gets or sets the Array with the index specified by <paramref name="i" />.
        /// </summary>
        /// <param name="i">The index.</param>
        public object this[int i]
        {
            get
            {
                AssertNotDisposed();

                object indexValue = Interop.Runtime.GetByIndex(JSHandle, i, out int exception);

                if (exception != 0)
                    throw new JSException((string)indexValue);
                Interop.Runtime.ReleaseInFlight(indexValue);
                return indexValue;
            }
            set
            {
                AssertNotDisposed();

                object res = Interop.Runtime.SetByIndex(JSHandle, i, value, out int exception);

                if (exception != 0)
                    throw new JSException((string)res);

            }
        }
    }
}
