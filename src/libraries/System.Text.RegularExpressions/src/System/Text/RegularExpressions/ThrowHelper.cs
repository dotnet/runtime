// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [DoesNotReturn]
        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument arg, ExceptionResource resource) =>
            throw new ArgumentOutOfRangeException(GetStringForExceptionArgument(arg), GetStringForExceptionResource(resource));

        private static string? GetStringForExceptionArgument(ExceptionArgument arg) =>
            arg switch
            {
                ExceptionArgument.assemblyname => nameof(ExceptionArgument.assemblyname),
                ExceptionArgument.array => nameof(ExceptionArgument.array),
                ExceptionArgument.arrayIndex => nameof(ExceptionArgument.arrayIndex),
                ExceptionArgument.count => nameof(ExceptionArgument.count),
                ExceptionArgument.evaluator => nameof(ExceptionArgument.evaluator),
                ExceptionArgument.i => nameof(ExceptionArgument.i),
                ExceptionArgument.inner => nameof(ExceptionArgument.inner),
                ExceptionArgument.input => nameof(ExceptionArgument.input),
                ExceptionArgument.length => nameof(ExceptionArgument.length),
                ExceptionArgument.matchTimeout => nameof(ExceptionArgument.matchTimeout),
                ExceptionArgument.name => nameof(ExceptionArgument.name),
                ExceptionArgument.options => nameof(ExceptionArgument.options),
                ExceptionArgument.pattern => nameof(ExceptionArgument.pattern),
                ExceptionArgument.replacement => nameof(ExceptionArgument.replacement),
                ExceptionArgument.startat => nameof(ExceptionArgument.startat),
                ExceptionArgument.str => nameof(ExceptionArgument.str),
                ExceptionArgument.value => nameof(ExceptionArgument.value),
                _ => null
            };

        private static string? GetStringForExceptionResource(ExceptionResource resource) =>
            resource switch
            {
                ExceptionResource.BeginIndexNotNegative => SR.BeginIndexNotNegative,
                ExceptionResource.CountTooSmall => SR.CountTooSmall,
                ExceptionResource.LengthNotNegative => SR.LengthNotNegative,
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
    }
}
