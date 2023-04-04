// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

namespace N
{
    public static class C
    {
        [Fact]
        public static int TestEntryPoint()
        {
            // Regression test for an issue with assertion prop leading
            // to the wrong exception being thrown from Vector<T>.CopyTo
            try
            {
                Foo(Vector<int>.Zero);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                // Caught the right exception
                return 100;
            }
            catch
            {
                // Caught the wrong exception
                return -1;
            }
            // Caught no exception
            return -2;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Foo(Vector<int> vec)
        {
            int[] a = new int[5];
            // The index [5] is outside the bounds of array 'a',
            // so this should throw ArgumentOutOfRangeException.
            // There's a subsequent check for whether the destination
            // has enough space to receive the vector, which would
            // raise an ArgumentException; the bug was that assertion
            // prop was using the later exception check to prove the
            // prior one "redundant" because the commas confused the
            // ordering.
            vec.CopyTo(a, 5);
            return a[0];
        }
    }
}
