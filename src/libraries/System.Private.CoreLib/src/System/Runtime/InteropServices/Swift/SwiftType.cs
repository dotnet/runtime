// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Swift
{
    /// <summary>
    /// Indicates that the argument is the self context.
    /// </summary>
    public readonly struct SwiftSelf
    {
        /// <summary>Creates a new instance of the SwiftSelf struct with the specified pointer value.</summary>
        /// <param name="value">The pointer value of the self context</param>
        public SwiftSelf(IntPtr value) {
            Value = value;
        }
        /// <summary>Gets the pointer of the self context.</summary>
        public IntPtr Value { get; }
    }

    /// <summary>
    /// Indicates that the argument is the error context.
    /// </summary>
    public readonly struct SwiftError
    {
        /// <summary>Creates a new instance of the SwiftError struct with the specified pointer value.</summary>
        /// <param name="value">The pointer value of the error context</param>
        public SwiftError(IntPtr value) {
            Value = value;
        }
        /// <summary>Gets the pointer of the error context.</summary>
        public IntPtr Value { get; }
    }

    /// <summary>
    /// Indicates that the argument is the async context.
    /// </summary>
    public readonly struct SwiftAsyncContext
    {
        /// <summary>Creates a new instance of the SwiftAsyncContext struct with the specified pointer value.</summary>
        /// <param name="value">The pointer value of the async context</param>
        public SwiftAsyncContext(IntPtr value) {
            Value = value;
        }
        /// <summary>Gets the pointer of the async context.</summary>
        public IntPtr Value { get; }
    }
}
