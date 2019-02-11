// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Linq
{
    internal static class ThrowHelper
    {
        internal static void ThrowArgumentNullException(ExceptionArgument argument) => throw new ArgumentNullException(GetArgumentString(argument));

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument) => throw new ArgumentOutOfRangeException(GetArgumentString(argument));

        internal static void ThrowMoreThanOneElementException() => throw new InvalidOperationException(SR.MoreThanOneElement);

        internal static void ThrowMoreThanOneMatchException() => throw new InvalidOperationException(SR.MoreThanOneMatch);

        internal static void ThrowNoElementsException() => throw new InvalidOperationException(SR.NoElements);

        internal static void ThrowNoMatchException() => throw new InvalidOperationException(SR.NoMatch);

        internal static void ThrowNotSupportedException() => throw new NotSupportedException();

        private static string GetArgumentString(ExceptionArgument argument)
        {
            switch (argument)
            {
                case ExceptionArgument.collectionSelector: return nameof(ExceptionArgument.collectionSelector);
                case ExceptionArgument.count: return nameof(ExceptionArgument.count);
                case ExceptionArgument.elementSelector: return nameof(ExceptionArgument.elementSelector);
                case ExceptionArgument.enumerable: return nameof(ExceptionArgument.enumerable);
                case ExceptionArgument.first: return nameof(ExceptionArgument.first);
                case ExceptionArgument.func: return nameof(ExceptionArgument.func);
                case ExceptionArgument.index: return nameof(ExceptionArgument.index);
                case ExceptionArgument.inner: return nameof(ExceptionArgument.inner);
                case ExceptionArgument.innerKeySelector: return nameof(ExceptionArgument.innerKeySelector);
                case ExceptionArgument.keySelector: return nameof(ExceptionArgument.keySelector);
                case ExceptionArgument.outer: return nameof(ExceptionArgument.outer);
                case ExceptionArgument.outerKeySelector: return nameof(ExceptionArgument.outerKeySelector);
                case ExceptionArgument.predicate: return nameof(ExceptionArgument.predicate);
                case ExceptionArgument.resultSelector: return nameof(ExceptionArgument.resultSelector);
                case ExceptionArgument.second: return nameof(ExceptionArgument.second);
                case ExceptionArgument.selector: return nameof(ExceptionArgument.selector);
                case ExceptionArgument.source: return nameof(ExceptionArgument.source);
                default:
                    Debug.Fail("The ExceptionArgument value is not defined.");
                    return string.Empty;
            }
        }
    }

    internal enum ExceptionArgument
    {
        collectionSelector,
        count,
        elementSelector,
        enumerable,
        first,
        func,
        index,
        inner,
        innerKeySelector,
        keySelector,
        outer,
        outerKeySelector,
        predicate,
        resultSelector,
        second,
        selector,
        source,
    }
}
