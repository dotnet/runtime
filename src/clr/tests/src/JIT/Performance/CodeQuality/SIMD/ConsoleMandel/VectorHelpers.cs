// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Algorithms
{
    public static class VectorHelper
    {
        // Helper to construct a vector from a lambda that takes an
        // index. It's not efficient, but it's more succint than the
        // corresponding for loop.  Don't use it on a hot code path
        // (i.e. inside a loop)
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> Create<T>(Func<int, T> creator) where T : struct
        {
            T[] data = new T[Vector<T>.Count];
            for (int i = 0; i < data.Length; i++)
                data[i] = creator(i);
            return new Vector<T>(data);
        }

        // Helper to invoke a function for each element of the
        // vector. This is NOT fast. I just like the way it looks
        // better than a for loop. Don't use it somewhere that
        // performance truly matters
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public static void ForEach<T>(this Vector<T> vec, Action<T, int> op) where T : struct
        {
            for (int i = 0; i < Vector<T>.Count; i++)
                op(vec[i], i);
        }
    }
}
