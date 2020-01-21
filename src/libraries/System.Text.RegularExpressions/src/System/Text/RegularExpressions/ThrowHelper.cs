// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        internal static Exception CreateArgumentException(ExceptionResource resource) =>
            throw new ArgumentException(GetStringForExceptionResource(resource));

        [DoesNotReturn]
        internal static void ThrowArgumentNullException(ExceptionArgument arg) =>
            throw new ArgumentNullException(GetStringForExceptionArgument(arg));

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument arg) =>
            throw new ArgumentOutOfRangeException(GetStringForExceptionArgument(arg));

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument arg, ExceptionResource resource) =>
            throw new ArgumentOutOfRangeException(GetStringForExceptionArgument(arg), GetStringForExceptionResource(resource));

        [DoesNotReturn]
        internal static void ThrowNotSupportedException(ExceptionResource resource) =>
            throw new NotSupportedException(GetStringForExceptionResource(resource));

        private static string? GetStringForExceptionArgument(ExceptionArgument arg) =>
            arg switch
            {
                ExceptionArgument.assemblyname => "assemblyname",
                ExceptionArgument.array => "array",
                ExceptionArgument.arrayIndex => "arrayIndex",
                ExceptionArgument.count => "count",
                ExceptionArgument.evaluator => "evaluator",
                ExceptionArgument.i => "i",
                ExceptionArgument.inner => "inner",
                ExceptionArgument.input => "input",
                ExceptionArgument.length => "length",
                ExceptionArgument.matchTimeout => "matchTimeout",
                ExceptionArgument.name => "name",
                ExceptionArgument.options => "options",
                ExceptionArgument.pattern => "pattern",
                ExceptionArgument.regexinfos => "regexinfos",
                ExceptionArgument.replacement => "replacement",
                ExceptionArgument.startat => "startat",
                ExceptionArgument.str => "str",
                ExceptionArgument.value => "value",
                _ => null
            };

        private static string? GetStringForExceptionResource(ExceptionResource resource) =>
            resource switch
            {
                ExceptionResource.BeginIndexNotNegative => SR.BeginIndexNotNegative,
                ExceptionResource.CountTooSmall => SR.CountTooSmall,
                ExceptionResource.LengthNotNegative => SR.LengthNotNegative,
                ExceptionResource.OnlyAllowedOnce => SR.OnlyAllowedOnce,
                ExceptionResource.ReplacementError => SR.ReplacementError,
                _ => null
            };
    }

    internal enum ExceptionArgument
    {
        assemblyname,
        array,
        arrayIndex,
        count,
        evaluator,
        i,
        inner,
        input,
        length,
        matchTimeout,
        name,
        options,
        pattern,
        regexinfos,
        replacement,
        startat,
        str,
        value,
    }

    internal enum ExceptionResource
    {
        BeginIndexNotNegative,
        CountTooSmall,
        LengthNotNegative,
        OnlyAllowedOnce,
        ReplacementError,
    }
}
