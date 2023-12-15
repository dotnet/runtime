// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq.Tests
{
    public static class Shuffler
    {
        public static T[] Shuffle<T>(T[] array)
        {
            var r = new Random(42);
            r.Shuffle(array);
            return array;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) => Shuffle(source.ToArray());
    }
}
