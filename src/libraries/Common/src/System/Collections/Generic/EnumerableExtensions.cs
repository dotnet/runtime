// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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


