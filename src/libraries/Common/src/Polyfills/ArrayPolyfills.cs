// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System;

/// <summary>Provides downlevel polyfills for static members on <see cref="Array"/>.</summary>
internal static class ArrayPolyfills
{
    extension(Array)
    {
        public static int MaxLength => 0x7FFFFFC7;

        public static void Reverse<T>(T[] array, int index, int length)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            array.AsSpan(index, length).Reverse();
        }

        public static void Clear(Array array)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            Array.Clear(array, array.GetLowerBound(0), array.Length);
        }
    }
}
