// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            for (int index = 0; index < length; index++)
            {
                data[index] = (byte)random.Next();
            }
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
            public static ArrayComparer<T> Instance = new ArrayComparer<T>();

            private ArrayComparer() // use the static Instance singleton
            {
            }

            public bool Equals(T[] x, T[] y)
            {
                if (x.Length != y.Length)
                {
                    return false;
                }
                for (int i = 0; i < x.Length; i++)
                {
                    if (x[i] == null)
                    {
                        if (y[i] == null)
                        {
                            return true;
                        }
                        else return false;
                    }
                    if (!x[i].Equals(y[i]))
                    {
                        return false;
                    }
                }
                return true;
            }

            public int GetHashCode(T[] obj)
            {
                throw new NotSupportedException("Avoid using arrays as keys in hashtables. If you really have to do it, write your own comparer; I don't want to be responsible for your slow code.");
            }
        }
    }
}
