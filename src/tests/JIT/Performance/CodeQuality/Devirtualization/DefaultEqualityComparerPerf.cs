// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

// Performance tests for optimizations related to EqualityComparer<T>.Default

namespace Devirtualization
{
    public class EqualityComparerFixture<T> where T : IEquatable<T>
    {
        IEqualityComparer<T> comparer;

        public EqualityComparerFixture(IEqualityComparer<T> customComparer = null)
        {
            comparer = customComparer ?? EqualityComparer<T>.Default;
        }

        // Baseline method showing unoptimized performance
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public bool CompareNoOpt(ref T a, ref T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }

        // The code this method invokes should be well-optimized
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Compare(ref T a, ref T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }

        // This models how Dictionary uses a comparer. We're not
        // yet able to optimize such cases.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool CompareCached(ref T a, ref T b)
        {
            return comparer.Equals(a, b);
        }

        private static IEqualityComparer<T> Wrapped()
        {
            return EqualityComparer<T>.Default;
        }

        // We would need enhancements to late devirtualization
        // to optimize this case.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool CompareWrapped(ref T x, ref T y)
        {
            return Wrapped().Equals(x, y);
        }

        public bool BenchCompareNoOpt(ref T t, long count)
        {
            bool result = true;
            for (int i = 0; i < count; i++)
            {
                result &= CompareNoOpt(ref t, ref t);
            }
            return result;
        }

        public bool BenchCompare(ref T t, long count)
        {
            bool result = true;
            for (int i = 0; i < count; i++)
            {
                result &= Compare(ref t, ref t);
            }
            return result;
        }

        public bool BenchCompareCached(ref T t, long count)
        {
            bool result = true;
            for (int i = 0; i < count; i++)
            {
                result &= CompareCached(ref t, ref t);
            }
            return result;
        }

        public bool BenchCompareWrapped(ref T t, long count)
        {
            bool result = true;
            for (int i = 0; i < count; i++)
            {
                result &= CompareWrapped(ref t, ref t);
            }
            return result;
        }
    }

    public class EqualityComparer
    {

#if DEBUG
        public const int Iterations = 1;
#else
        public const int Iterations = 150 * 1000 * 1000;
#endif

        public enum E
        {
            RED = 1,
            BLUE = 2
        }

        [Fact]
        public static int TestEntryPoint()
        {
            var valueTupleFixture = new EqualityComparerFixture<ValueTuple<byte, E, int>>();
            var v0 = new ValueTuple<byte, E, int>(3, E.RED, 11);

            bool vtCompare = valueTupleFixture.Compare(ref v0, ref v0);
            bool vtCompareNoOpt = valueTupleFixture.CompareNoOpt(ref v0, ref v0);
            bool vtCompareCached = valueTupleFixture.CompareCached(ref v0, ref v0);
            bool vtCompareWrapped = valueTupleFixture.CompareWrapped(ref v0, ref v0);

            bool vtOk = vtCompare & vtCompareNoOpt & vtCompareCached & vtCompareWrapped;

            return vtOk ? 100 : 0;
        }
    }
}
