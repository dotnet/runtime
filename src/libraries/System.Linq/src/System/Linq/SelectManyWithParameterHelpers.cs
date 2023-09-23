// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace System.Linq
{
    internal static class SelectManyWithParameterHelpers
    {
        public static IEnumerable<TResult> SelectMany<TSource, TResult, TParam1>(IEnumerable<TSource> src,
            Func<TSource, TParam1, IEnumerable<TResult>> func, TParam1 param1)
        {
            if (src is TSource[] srcArray)
            {
                return SelectManyArray(srcArray, func, param1);
            }

            return SelectManyNonArray(src, func, param1);
        }

        private static IEnumerable<TResult> SelectManyArray<TSource, TResult, TParam1>(TSource[] src,
            Func<TSource, TParam1, IEnumerable<TResult>> func, TParam1 param1)
        {
            foreach (var elem in src)
            {
                foreach (var subElem in func(elem, param1))
                {
                    yield return subElem;
                }
            }
        }
        private static IEnumerable<TResult> SelectManyNonArray<TSource, TResult, TParam1>(IEnumerable<TSource> src,
            Func<TSource, TParam1, IEnumerable<TResult>> func, TParam1 param1)
        {
            foreach (var elem in src)
            {
                foreach (var subElem in func(elem, param1))
                {
                    yield return subElem;
                }
            }
        }
        public static IEnumerable<TResult> SelectMany<TSource, TResult, TParam1, TParam2>(IEnumerable<TSource> src,
            Func<TSource, TParam1, TParam2, IEnumerable<TResult>> func, TParam1 param1, TParam2 param2)
        {
            if (src is TSource[] srcArray)
            {
                return SelectManyArray(srcArray, func, param1, param2);
            }

            return SelectManyNonArray(src, func, param1, param2);
        }

        private static IEnumerable<TResult> SelectManyNonArray<TSource, TResult, TParam1, TParam2>(IEnumerable<TSource> src,
            Func<TSource, TParam1, TParam2, IEnumerable<TResult>> func, TParam1 param1, TParam2 param2)
        {
            foreach (var elem in src)
            {
                foreach (var subElem in func(elem, param1, param2))
                {
                    yield return subElem;
                }
            }
        }
        private static IEnumerable<TResult> SelectManyArray<TSource, TResult, TParam1, TParam2>(TSource[] src,
            Func<TSource, TParam1, TParam2, IEnumerable<TResult>> func, TParam1 param1, TParam2 param2)
        {
            foreach (var elem in src)
            {
                foreach (var subElem in func(elem, param1, param2))
                {
                    yield return subElem;
                }
            }
        }
    }
}
