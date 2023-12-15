// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> AsEnumerable<TSource>(this IEnumerable<TSource> source) => source;

        /// <summary>
        /// Sets the <paramref name="list"/>'s <see cref="List{T}.Count"/> to be <paramref name="count"/>
        /// and returns the relevant portion of the list's backing array as a span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Span<T> SetCountAndGetSpan<T>(List<T> list, int count)
        {
            CollectionsMarshal.SetCount(list, count);
            return CollectionsMarshal.AsSpan(list);
        }

        /// <summary>Validates that source is not null and then tries to extract a span from the source.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // fast type checks that don't add a lot of overhead
        private static bool TryGetSpan<TSource>(this IEnumerable<TSource> source, out ReadOnlySpan<TSource> span)
            // This constraint isn't required, but the overheads involved here can be more substantial when TSource
            // is a reference type and generic implementations are shared.  So for now we're protecting ourselves
            // and forcing a conscious choice to remove this in the future, at which point it should be paired with
            // sufficient performance testing.
            where TSource : struct
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            // Use `GetType() == typeof(...)` rather than `is` to avoid cast helpers.  This is measurably cheaper
            // but does mean we could end up missing some rare cases where we could get a span but don't (e.g. a uint[]
            // masquerading as an int[]).  That's an acceptable tradeoff.  The Unsafe usage is only after we've
            // validated the exact type; this could be changed to a cast in the future if the JIT starts to recognize it.
            // We only pay the comparison/branching costs here for super common types we expect to be used frequently
            // with LINQ methods.

            bool result = true;
            if (source.GetType() == typeof(TSource[]))
            {
                span = Unsafe.As<TSource[]>(source);
            }
            else if (source.GetType() == typeof(List<TSource>))
            {
                span = CollectionsMarshal.AsSpan(Unsafe.As<List<TSource>>(source));
            }
            else
            {
                span = default;
                result = false;
            }

            return result;
        }
    }
}
