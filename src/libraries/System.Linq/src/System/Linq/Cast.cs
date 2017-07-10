// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            return OfTypeIterator<TResult>(source);
        }

        private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
        {
            foreach (object obj in source)
            {
                if (obj is TResult)
                {
                    yield return (TResult)obj;
                }
            }
        }

        public static IEnumerable<TResult> Cast<TResult>(this IEnumerable source)
        {
            IEnumerable<TResult> typedSource = source as IEnumerable<TResult>;
            if (typedSource != null)
            {
                return typedSource;
            }
            
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }
            
            return CastIterator<TResult>(source);
        }
        private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
        {
            foreach (object obj in source)
            {
                yield return (TResult)obj;
            }
        }
    }
}
