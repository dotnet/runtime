// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.ComponentModel.Tests
{
    public static class EnumeratorExtensions
    {
        public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator)
        {
            while(enumerator.MoveNext())
                yield return enumerator.Current;
        }

        public static IEnumerator<T> Cast<T>(this IEnumerator iterator)
        {
            while (iterator.MoveNext())
            {
                yield return (T) iterator.Current;
            }
        }
    }
}
