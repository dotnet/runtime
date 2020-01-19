// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        internal static void ThrowArgumentNullException(ExceptionArgument arg) =>
            throw new ArgumentNullException(GetStringForExceptionArgument(arg));

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument arg) =>
            throw new ArgumentOutOfRangeException(GetStringForExceptionArgument(arg));

        private static string? GetStringForExceptionArgument(ExceptionArgument arg) =>
            arg switch
            {
                ExceptionArgument.assemblyname => "assemblyname",
                ExceptionArgument.array => "array",
                ExceptionArgument.arrayIndex => "arrayIndex",
                ExceptionArgument.evaluator => "evaluator",
                ExceptionArgument.i => "i",
                ExceptionArgument.inner => "inner",
                ExceptionArgument.input => "input",
                ExceptionArgument.matchTimeout => "matchTimeout",
                ExceptionArgument.name => "name",
                ExceptionArgument.options => "options",
                ExceptionArgument.pattern => "pattern",
                ExceptionArgument.regexinfos => "regexinfos",
                ExceptionArgument.replacement => "replacement",
                ExceptionArgument.str => "str",
                ExceptionArgument.value => "value",
                _ => null
            };
    }

    internal enum ExceptionArgument
    {
        assemblyname,
        array,
        arrayIndex,
        evaluator,
        i,
        inner,
        input,
        matchTimeout,
        name,
        options,
        pattern,
        regexinfos,
        replacement,
        str,
        value,
    }
}
