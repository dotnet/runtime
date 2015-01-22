// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System
{
    //
    // Generic array routines.
    //
    internal static class ArrayT<T>
    {
        //
        // Don't remove or change without reading this comment:
        //
        // On .NET Native, ArrayT<>.Copy()'s body never gets executed as the IL2IL transform stage redirects calls to it to
        // to Array.Copy<>(). We don't want to use the non-generic Array.Copy() as it is painfully slow on .NET Native.
        //
        // On Phone, this is a normal method and invokes the non-generic Array.Copy() (which on phone is a nice fast FCall.)
        //
        public static void Copy(T[] sourceArray, int sourceIndex, T[] destinationArray, int destinationIndex, int length)
        {
            Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
        }

        //
        // Copies the first "copyCount" elements from "src" to a newly allocated array of size "newLength".
        //
        // This is primarily used by collection classes when they grow their internal storage. Unlike its nongeneric BCL counterpart,
        // it does not attempt to validate inputs.
        //
        // Input validation:
        //    On checked builds, passing in bad input values assert. The only exception is "newLength"
        //      - since most collections don't check for overflow when "growing" their internal storage,
        //        we won't check that on debug.
        //
        //    On retail, the only guarantee on passing bad inputs is that we won't corrupt the GC heap.
        //      You may (or may not) get a managed exception of one sort or another but not necessarily the one that
        //      the desktop would have thrown under the same circumstances.
        //
        // Returned array always a new array:
        //    Unlike its BCL namespace, you always get back a new array even if the new size is identical to the old
        //    one. This corner case is not expected to be likely given this function's intended use and it may violate
        //    the caller's original expectations.
        //
        public static T[] Resize(T[] src, int newLength, int copyCount)
        {
            Debug.Assert(src != null);
            Debug.Assert(copyCount >= 0);
            Debug.Assert(copyCount <= src.Length);

            T[] dst = new T[newLength];

            Copy(src, 0, dst, 0, copyCount);

            return dst;
        }

        //
        // This is a generic version of Array.Clear() that emulates all of the original's input validation. This is always a safe
        // replacement for Array.Clear(), though specific callsites may be able to use more performant versions.
        //
        public static void Clear(T[] array, int index, int length)
        {
            if (array == null)
                throw new ArgumentNullException("array");
            if (index < 0 || index > array.Length || length < 0 || length > array.Length)
                throw new IndexOutOfRangeException();
            if (length > (array.Length - index))
                throw new IndexOutOfRangeException();

            T t = default(T);
            while (length-- != 0)
                array[index++] = t;
        }
    }
}
