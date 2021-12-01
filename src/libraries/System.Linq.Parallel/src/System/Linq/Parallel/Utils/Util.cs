// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
// Util.cs
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System.Collections.Generic;

namespace System.Linq.Parallel
{
    /// <summary>
    /// Common miscellaneous utility methods used throughout the code-base.
    /// </summary>
    internal static class Util
    {
        //-----------------------------------------------------------------------------------
        // Simple helper that returns a constant depending on the sign of the argument. I.e.
        // if the argument is negative, the result is -1; if it's positive, the result is 1;
        // otherwise, if it's zero, the result is 0.
        //

        internal static int Sign(int x)
        {
            return x < 0 ? -1 : x == 0 ? 0 : 1;
        }

        //-----------------------------------------------------------------------------------
        // Unlike the X86 JIT, null checks on value types aren't optimized away in the X64 JIT compiler.
        // That means using the GenericComparer<K> infrastructure results in boxing value
        // types. This degrades performance all over the place.
        //

        internal static Comparer<TKey> GetDefaultComparer<TKey>()
        {
            if (typeof(TKey) == typeof(int))
            {
                return (Comparer<TKey>)(object)s_fastIntComparer;
            }
            else if (typeof(TKey) == typeof(long))
            {
                return (Comparer<TKey>)(object)s_fastLongComparer;
            }
            else if (typeof(TKey) == typeof(float))
            {
                return (Comparer<TKey>)(object)s_fastFloatComparer;
            }
            else if (typeof(TKey) == typeof(double))
            {
                return (Comparer<TKey>)(object)s_fastDoubleComparer;
            }
            else if (typeof(TKey) == typeof(DateTime))
            {
                return (Comparer<TKey>)(object)s_fastDateTimeComparer;
            }

            return Comparer<TKey>.Default;
        }

        private static readonly FastIntComparer s_fastIntComparer = new FastIntComparer();

        private sealed class FastIntComparer : Comparer<int>
        {
            public override int Compare(int x, int y)
            {
                return x.CompareTo(y);
            }
        }

        private static readonly FastLongComparer s_fastLongComparer = new FastLongComparer();

        private sealed class FastLongComparer : Comparer<long>
        {
            public override int Compare(long x, long y)
            {
                return x.CompareTo(y);
            }
        }

        private static readonly FastFloatComparer s_fastFloatComparer = new FastFloatComparer();

        private sealed class FastFloatComparer : Comparer<float>
        {
            public override int Compare(float x, float y)
            {
                return x.CompareTo(y);
            }
        }

        private static readonly FastDoubleComparer s_fastDoubleComparer = new FastDoubleComparer();

        private sealed class FastDoubleComparer : Comparer<double>
        {
            public override int Compare(double x, double y)
            {
                return x.CompareTo(y);
            }
        }

        private static readonly FastDateTimeComparer s_fastDateTimeComparer = new FastDateTimeComparer();

        private sealed class FastDateTimeComparer : Comparer<DateTime>
        {
            public override int Compare(DateTime x, DateTime y)
            {
                return x.CompareTo(y);
            }
        }
    }
}
