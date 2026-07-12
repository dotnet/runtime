// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static members on <see cref="Array"/>.</summary>
internal static class ArrayPolyfills
{
    extension(Array)
    {
        public static int MaxLength => 0x7FFFFFC7;

        public static void Clear(Array array) =>
            Array.Clear(array, 0, array.Length);

        public static void Reverse<T>(T[] array, int index, int length)
        {
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
