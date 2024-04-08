// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyModel
{
    internal static class LinqHelpers
    {
        public static IEnumerable<TResult> SelectMany<TSource, TResult, TParam1>(IEnumerable<TSource> src,
            Func<TSource, TParam1, IEnumerable<TResult>> func, TParam1 param1)
        {
            foreach (TSource? elem in src)
            {
                foreach (TResult? subElem in func(elem, param1))
                {
                    yield return subElem;
                }
            }
        }

        public static IEnumerable<TResult> SelectMany<TSource, TResult, TParam1, TParam2>(IEnumerable<TSource> src,
            Func<TSource, TParam1, TParam2, IEnumerable<TResult>> func, TParam1 param1, TParam2 param2)
        {
            foreach (TSource? elem in src)
            {
                foreach (TResult? subElem in func(elem, param1, param2))
                {
                    yield return subElem;
                }
            }
        }
    }
}
