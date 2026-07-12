// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static members on <see cref="Array"/>.</summary>
internal static class ArrayPolyfills
{
    extension(Array)
    {
        public static int MaxLength => 0x7FFFFFC7;

        public static void Clear(Array array)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            Array.Clear(array, array.GetLowerBound(0), array.Length);
        }

        public static void Reverse<T>(T[] array, int index, int length)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            if (array.Length - index < length)
            {
                throw new ArgumentException();
            }

            int i = index;
            int j = index + length - 1;

            while (i < j)
            {
                T item = array[i];
                array[i] = array[j];
                array[j] = item;
                i++;
                j--;
            }
        }
    }
}
