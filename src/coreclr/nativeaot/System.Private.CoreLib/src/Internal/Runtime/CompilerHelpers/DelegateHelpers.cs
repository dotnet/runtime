// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime.CompilerHelpers
{
    /// <summary>
    /// Delegate helpers for generated code.
    /// </summary>
    internal static class DelegateHelpers
    {
        private static object[] s_emptyObjectArray = Array.Empty<object>();

        private const int CacheLength = 16;
        [ThreadStatic]
        private static object[][] _arrayCache;

        internal static void ReturnObjectArray(object[] array)
        {
            int length = array.Length;
            if (length < CacheLength)
            {
                Array.Clear(array);
                _arrayCache[length] = array;
            }
        }

        internal static object[] GetObjectArray(int length)
        {
            if (length == 0)
                return s_emptyObjectArray;

            object[] result = null!;
            if (length < CacheLength)
            {
                _arrayCache ??= new object[CacheLength][];
                result = _arrayCache[length];
            }

            return result ?? new object[length];
        }
    }
}
