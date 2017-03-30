// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Holds a reference to a value.
    /// </summary>
    /// <typeparam name="T">The type of the value that the <see cref = "StrongBox{T}"></see> references.</typeparam>
    internal class StrongBox<T>
    {
        /// <summary>
        /// Gets the strongly typed value associated with the <see cref = "StrongBox{T}"></see>
        /// <remarks>This is explicitly exposed as a field instead of a property to enable loading the address of the field.</remarks>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1051:DoNotDeclareVisibleInstanceFields")]
        public T Value;

        /// <summary>
        /// Initializes a new <see cref = "StrongBox{T}"></see> with the specified value.
        /// </summary>
        /// <param name="value">A value that the <see cref = "StrongBox{T}"></see> will reference.</param>
        public StrongBox(T value)
        {
            Value = value;
        }
    }
}
