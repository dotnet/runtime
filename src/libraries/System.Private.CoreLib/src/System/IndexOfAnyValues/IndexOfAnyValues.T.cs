// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Provides an immutable, read-only set of values optimized for efficient searching.
    /// Instances are created by <see cref="IndexOfAnyValues.Create(ReadOnlySpan{byte})"/> or <see cref="IndexOfAnyValues.Create(ReadOnlySpan{char})"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values to search for.</typeparam>
    /// <remarks>
    /// <see cref="IndexOfAnyValues{T}"/> are optimized for situations where the same set of values is frequently used for searching at runtime.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IndexOfAnyValuesDebugView<>))]
    public class IndexOfAnyValues<T> where T : IEquatable<T>?
    {
        // Only CoreLib can create derived types
        private protected IndexOfAnyValues() { }

        /// <summary>Used by <see cref="IndexOfAnyValuesDebugView{T}"/>.</summary>
        internal virtual T[] GetValues() => throw new UnreachableException();

        /// <summary>
        /// Searches for the specified value and returns true if found. If not found, returns false.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T value) => ContainsCore(value);

        internal virtual bool ContainsCore(T value) => throw new UnreachableException();

        internal virtual int IndexOfAny(ReadOnlySpan<T> span) => throw new UnreachableException();
        internal virtual int IndexOfAnyExcept(ReadOnlySpan<T> span) => throw new UnreachableException();
        internal virtual int LastIndexOfAny(ReadOnlySpan<T> span) => throw new UnreachableException();
        internal virtual int LastIndexOfAnyExcept(ReadOnlySpan<T> span) => throw new UnreachableException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAny(ReadOnlySpan<T> span, IndexOfAnyValues<T> values)
        {
            if (values is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            return values.IndexOfAny(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IndexOfAnyExcept(ReadOnlySpan<T> span, IndexOfAnyValues<T> values)
        {
            if (values is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            return values.IndexOfAnyExcept(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAny(ReadOnlySpan<T> span, IndexOfAnyValues<T> values)
        {
            if (values is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            return values.LastIndexOfAny(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LastIndexOfAnyExcept(ReadOnlySpan<T> span, IndexOfAnyValues<T> values)
        {
            if (values is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.values);
            }

            return values.LastIndexOfAnyExcept(span);
        }
    }
}
