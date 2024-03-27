// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Swift
{
    /// <summary>
    /// Represents the Swift 'self' context, indicating that the argument is the self context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This struct is used to pass the 'self' context to Swift functions in the context of interop with .NET.
    /// </para>
    /// <para>
    /// Here's an example of how a SwiftSelf context can be declared:
    /// <code lang="csharp">
    /// [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    /// [DllImport("SwiftLibrary", EntryPoint = "export")]
    /// public static extern void swiftFunction(SwiftSelf self);
    /// </code>
    /// </para>
    /// </remarks>
    [CLSCompliant(false)]
    [Intrinsic]
    public readonly unsafe struct SwiftSelf
    {
        /// <summary>
        /// Creates a new instance of the SwiftSelf struct with the specified pointer value.
        /// </summary>
        /// <param name="value">The pointer value representing the self context.</param>
        public SwiftSelf(void* value)
        {
            Value = value;
        }
        /// <summary>
        /// Gets the pointer of the self context.
        /// </summary>
        public void* Value { get; }
    }

    /// <summary>
    /// Represents the Swift error context, indicating that the argument is the error context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This struct is used to retrieve the 'error' context from Swift functions in the context of interop with .NET.
    /// </para>
    /// <para>
    /// Here's an example of how a SwiftError can be declared:
    /// <code lang="csharp">
    /// [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    /// [DllImport("SwiftLibrary", EntryPoint = "export")]
    /// public static extern void swiftFunction(SwiftError* error);
    /// </code>
    /// </para>
    /// </remarks>
    [CLSCompliant(false)]
    [Intrinsic]
    public readonly unsafe struct SwiftError
    {
        /// <summary>
        /// Creates a new instance of the SwiftError struct with the specified pointer value.
        /// </summary>
        /// <param name="value">The pointer value representing the error context.</param>
        public SwiftError(void* value)
        {
            Value = value;
        }
        /// <summary>
        /// Gets the pointer of the error context.
        /// </summary>
        public void* Value { get; }
    }
}
