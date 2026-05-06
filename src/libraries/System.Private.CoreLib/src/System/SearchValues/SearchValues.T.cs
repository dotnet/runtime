// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Provides an immutable, read-only set of values optimized for efficient searching.
    /// Instances are created by <see cref="SearchValues.Create(ReadOnlySpan{byte})"/>, <see cref="SearchValues.Create(ReadOnlySpan{char})"/>, or
    /// <see cref="SearchValues.Create(ReadOnlySpan{string}, StringComparison)"/>.
    /// </summary>
    /// <typeparam name="T">The type of the values to search for.</typeparam>
    /// <remarks>
    /// <see cref="SearchValues{T}"/> are optimized for situations where the same set of values is frequently used for searching at runtime.
    /// </remarks>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    [DebuggerTypeProxy(typeof(SearchValuesDebugView<>))]
    public class SearchValues<T> where T : IEquatable<T>?
    {
        // Only CoreLib can create derived types
        private protected SearchValues() { }

        /// <summary>Used by <see cref="DebuggerDisplay"/>s and <see cref="DebuggerTypeProxyAttribute"/>s for <see cref="SearchValues{T}"/>.</summary>
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

        internal virtual bool ContainsAny(ReadOnlySpan<T> span) => IndexOfAny(span) >= 0;
        internal virtual bool ContainsAnyExcept(ReadOnlySpan<T> span) => IndexOfAnyExcept(span) >= 0;

        // This is only implemented and used by SearchValues<string>.
        internal virtual int IndexOfAnyMultiString(ReadOnlySpan<char> span) => throw new UnreachableException();

        private string DebuggerDisplay
        {
            get
            {
                T[] values = GetValues();

                string display = $"{GetType().Name}, Count = {values.Length}";
                if (values.Length > 0)
                {
                    display += ", Values = ";
                    display += typeof(T) == typeof(char) ?
                        "\"" + new string(Unsafe.As<T[], char[]>(ref values)) + "\"" :
                        string.Join(",", values);
                }

                return display;
            }
        }
    }
}
