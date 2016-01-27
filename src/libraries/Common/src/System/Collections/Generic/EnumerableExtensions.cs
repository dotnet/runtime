// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Collections.Generic
{
    internal static class EnumerableExtensions
    {
        // Used to prevent returning values out of IEnumerable<>-typed properties
        // that an untrusted caller could cast back to array or List.
        public static IEnumerable<T> AsNothingButIEnumerable<T>(this IEnumerable<T> en)
        {
            foreach (T t in en)
                yield return t;
        }
    }
}


