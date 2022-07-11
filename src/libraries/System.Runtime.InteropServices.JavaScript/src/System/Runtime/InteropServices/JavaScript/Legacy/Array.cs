// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Initializes a new instance of JavaScript Core Array class.
    /// </summary>
    [Obsolete]
    public class Array : JSObject
    {
        /// <summary>
        /// Initializes a new instance of the Array class.
        /// </summary>
        /// <param name="_params">Parameters.</param>
        public Array(params object[] _params)
            : base(JavaScriptImports.CreateCSOwnedObject(nameof(Array), _params))
        {
            JSHostImplementation.RegisterCSOwnedObject(this);
        }

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
        public int Push(params object[] elements) => (int)this.Invoke("push", elements);

        /// <summary>
        /// Pop this instance.
        /// </summary>
        /// <returns>The element removed from the array or null if the array was empty</returns>
        public object Pop() => (object)this.Invoke("pop");

        /// <summary>
        /// Remove the first element of the Array and return that element
        /// </summary>
        /// <returns>The removed element</returns>
        public object Shift() => this.Invoke("shift");

        /// <summary>
        /// Add <paramref name="elements"/> to the array starting at index <c>0</c>
        /// </summary>
        /// <returns>The length after shift.</returns>
        /// <param name="elements">Elements.</param>
        public int UnShift(params object[] elements) => (int)this.Invoke("unshift", elements);

        /// <summary>
        /// Index of the search element.
        /// </summary>
        /// <returns>The index of first occurrence of searchElement in the Array or -1 if not Found</returns>
        /// <param name="searchElement">Search element.</param>
        /// <param name="fromIndex">The index to start the search from</param>
        public int IndexOf(object searchElement, int fromIndex = 0) => (int)this.Invoke("indexOf", searchElement, fromIndex);

        /// <summary>
        /// Finds the index of the last occurrence of<paramref name="searchElement" />
        /// </summary>
        /// <returns>The index of the last occurrence</returns>
        /// <param name="searchElement">Search element.</param>
        public int LastIndexOf(object searchElement) => (int)this.Invoke("lastIndexOf", searchElement);

        /// <summary>
        /// Finds the index of the last occurrence of<paramref name="searchElement" /> between 0 and <paramref name="endIndex" />.
        /// </summary>
        /// <returns>The index of the last occurrence.</returns>
        /// <param name="searchElement">Search element.</param>
        /// <param name="endIndex">End index.</param>
        public int LastIndexOf(object searchElement, int endIndex) => (int)this.Invoke("lastIndexOf", searchElement, endIndex);

        /// <summary>
        /// Gets or sets the Array with the index specified by <paramref name="i" />.
        /// </summary>
        /// <param name="i">The index.</param>
        public object this[int i]
        {
            [MethodImpl(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
            get
            {
                this.AssertNotDisposed();

                Interop.Runtime.GetByIndexRef(JSHandle, i, out int exception, out object indexValue);

                if (exception != 0)
                    throw new JSException((string)indexValue);
                JSHostImplementation.ReleaseInFlight(indexValue);
                return indexValue;
            }
            [MethodImpl(MethodImplOptions.NoInlining)] // https://github.com/dotnet/runtime/issues/71425
            set
            {
                this.AssertNotDisposed();

                Interop.Runtime.SetByIndexRef(JSHandle, i, value, out int exception, out object res);

                if (exception != 0)
                    throw new JSException((string)res);

            }
        }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length
        {
            get => Convert.ToInt32(this.GetObjectProperty("length"));
            set => this.SetObjectProperty("length", value, false);
        }
    }
}
