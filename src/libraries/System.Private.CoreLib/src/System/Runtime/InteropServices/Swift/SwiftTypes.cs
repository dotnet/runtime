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
    /// Represents the Swift 'self' context when the argument is Swift frozen struct T, which is either enregistered into multiple registers,
    /// or passed by reference in the 'self' register.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This struct is used to pass the Swift frozen struct T to Swift functions in the context of interop with .NET.
    /// </para>
    /// <para>
    /// Here's an example of how a SwiftSelf&lt;T&gt; context can be declared:
    /// <code lang="csharp">
    /// [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    /// [LibraryImport("SwiftLibrary", EntryPoint = "export")]
    /// public static extern void swiftFunction(SwiftSelf&lt;T&gt; self);
    /// </code>
    /// </para>
    /// </remarks>
    [Intrinsic]
    public readonly unsafe struct SwiftSelf<T> where T: unmanaged
    {
        /// <summary>
        /// Creates a new instance of the SwiftSelf struct with the specified value.
        /// </summary>
        /// <param name="value">The value representing the self context.</param>
        public SwiftSelf(T value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the value representing the Swift frozen struct.
        /// </summary>
        public T Value { get; }
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

    /// <summary>
    /// Represents the Swift return buffer context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This struct is used to access the return buffer when interoping with Swift functions that return non-frozen structs.
    /// It provides a pointer to the memory location where the result should be stored.
    /// </para>
    /// <para>
    /// Here's an example of how a SwiftIndirectResult can be declared:
    /// <code lang="csharp">
    /// [UnmanagedCallConv(CallConvs = [typeof(CallConvSwift)])]
    /// [LibraryImport("SwiftLibrary", EntryPoint = "export")]
    /// public static extern void swiftFunction(SwiftIndirectResult result);
    /// </code>
    /// </para>
    /// </remarks>
    [CLSCompliant(false)]
    [Intrinsic]
    public readonly unsafe struct SwiftIndirectResult
    {
        /// <summary>
        /// Creates a new instance of the SwiftIndirectResult struct with the specified pointer value.
        /// </summary>
        /// <param name="value">The pointer value representing return buffer context.</param>
        public SwiftIndirectResult(void* value)
        {
            Value = value;
        }

        /// <summary>
        /// Gets the pointer of the return buffer register.
        /// </summary>
        public void* Value { get; }
    }
}
