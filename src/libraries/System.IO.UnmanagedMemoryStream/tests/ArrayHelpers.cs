// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.IO.Tests
{
    public static class ArrayHelpers
    {
        public static IEqualityComparer<T[]> Comparer<T>()
        {
            return ArrayComparer<T>.Instance;
        }

        public static byte[] Flatten(this IEnumerable<byte[]> segments)
        {
            List<byte> bytes = new List<byte>();
            foreach (var segment in segments)
            {
                bytes.AddRange(segment);
            }
            return bytes.ToArray();
        }

        public static byte[] CreateByteArray(long length)
        {
            var random = new Random(100);
            var data = new byte[length];
            random.NextBytes(data);
            return data;
        }

        public static byte[] CreateByteArray(long length, byte value)
        {
            var data = new byte[length];
            for (int index = 0; index < length; index++)
            {
                data[index] = value;
            }
            return data;
        }

        public static T[] Copy<T>(this T[] source)
        {
            return (T[])source.Clone();
        }

        private sealed class ArrayComparer<T> : IEqualityComparer<T[]>
        {
            public static readonly ArrayComparer<T> Instance = new ArrayComparer<T>();

            private ArrayComparer() // use the static Instance singleton
            {
            }

            public bool Equals(T[] x, T[] y) => x.AsSpan().SequenceEqual(y);

            public int GetHashCode(T[] obj)
            {
                throw new NotSupportedException("Avoid using arrays as keys in hashtables. If you really have to do it, write your own comparer; I don't want to be responsible for your slow code.");
            }
        }
    }
}
