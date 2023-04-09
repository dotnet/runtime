// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.Collections.Frozen
{
    /// <summary>
    /// A few numbers to drive implementation selection heuristics.
    /// </summary>
    /// <remarks>
    /// These numbers were arrived through simple benchmarks conducted against .NET 7.
    /// It's worth potentially tweaking these values if the implementation of the
    /// collections changes in a substantial way, or if the JIT improves related code gen over time.
    /// </remarks>
    internal static class Constants
    {
        /// <summary>Threshold when we switch from scanning to hashing for non-value-type or non-default-comparer collections.</summary>
        /// <remarks>
        /// This determines the threshold where we switch from
        /// the scanning-based SmallFrozenDictionary/Set to the hashing-based
        /// DefaultFrozenDictionary/Set.
        /// </remarks>
        public const int MaxItemsInSmallFrozenCollection = 4;

        /// <summary>Threshold when we switch from scanning to hashing value type collections using a default comparer.</summary>
        /// <remarks>
        /// This determines the threshold when we switch from the scanning
        /// SmallValueTypeDefaultComparerFrozenDictionary/Set to the
        /// hashing ValueTypeDefaultComparerFrozenDictionary/Set.
        /// </remarks>
        public const int MaxItemsInSmallValueTypeFrozenCollection = 10;

        /// <summary>
        /// Whether the <typeparamref name="T"/> is known to implement <see cref="IComparable{T}"/> safely and efficiently,
        /// such that its comparison operations should be used in searching for types in small collections.
        /// </summary>
        /// <remarks>
        /// This does not automatically return true for any type that implements <see cref="IComparable{T}"/>.
        /// Doing so leads to problems for container types (e.g. ValueTuple{T1, T2}) where the
        /// container implements <see cref="IComparable{T}"/> to delegate to its contained items' implementation
        /// but those then don't provide such support.
        /// </remarks>
        public static bool IsKnownComparable<T>() =>
            // This list covers all of the IComparable<T> value types in Corelib that aren't containers (like ValueTuple).
            typeof(T) == typeof(bool) ||
            typeof(T) == typeof(sbyte) ||
            typeof(T) == typeof(byte) ||
            typeof(T) == typeof(char) ||
            typeof(T) == typeof(short) ||
            typeof(T) == typeof(ushort) ||
            typeof(T) == typeof(int) ||
            typeof(T) == typeof(uint) ||
            typeof(T) == typeof(long) ||
            typeof(T) == typeof(ulong) ||
            typeof(T) == typeof(nint) ||
            typeof(T) == typeof(nuint) ||
            typeof(T) == typeof(decimal) ||
            typeof(T) == typeof(float) ||
            typeof(T) == typeof(double) ||
            typeof(T) == typeof(decimal) ||
            typeof(T) == typeof(TimeSpan) ||
            typeof(T) == typeof(DateTime) ||
            typeof(T) == typeof(DateTimeOffset) ||
            typeof(T) == typeof(Guid) ||
#if NETCOREAPP3_0_OR_GREATER
            typeof(T) == typeof(Rune) ||
#endif
#if NET5_0_OR_GREATER
            typeof(T) == typeof(Half) ||
#endif
#if NET6_0_OR_GREATER
            typeof(T) == typeof(DateOnly) ||
            typeof(T) == typeof(TimeOnly) ||
#endif
#if NET7_0_OR_GREATER
            typeof(T) == typeof(Int128) ||
            typeof(T) == typeof(UInt128) ||
#endif
            typeof(T).IsEnum;
    }
}
