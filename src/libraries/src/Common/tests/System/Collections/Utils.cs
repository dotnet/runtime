// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Tests.Collections
{
    public static class ArrayExtensions
    {
        public static T[] Slice<T>(
            this T[] array,
            int startIndex,
            int length = -1)
        {
            if (length == -1)
                length = array.Length - startIndex;
            var tmp = new T[length];
            Array.Copy(array, startIndex, tmp, 0, length);
            return tmp;
        }

        public static T[] Push<T>(this T[] array, params T[] arguments)
        {
            if (arguments == null && default(T) == null)
                return array.Push(default(T));
            if (arguments == null)
                throw new ArgumentNullException(nameof(arguments));
            var ret = new T[array.Length + arguments.Length];
            Array.Copy(array, ret, array.Length);
            Array.Copy(
                arguments,
                0,
                ret,
                array.Length,
                arguments.Length);
            return ret;
        }

        public static T[] RemoveAt<T>(this T[] array, int removeIndex)
        {
            var ret = new T[array.Length - 1];
            Array.Copy(array, 0, ret, 0, removeIndex);
            Array.Copy(
                array,
                removeIndex + 1,
                ret,
                removeIndex,
                array.Length - 1 - removeIndex);
            return ret;
        }
    }
}
