// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System
{
    internal static class ThrowHelper
    {
        internal static void ThrowArgumentNullException(ExceptionArgument argument) =>
            throw GetArgumentNullException(argument);

        internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument) =>
            throw GetArgumentOutOfRangeException(argument);

        private static ArgumentNullException GetArgumentNullException(ExceptionArgument argument) =>
            new ArgumentNullException(GetArgumentName(argument));

        private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument) =>
            new ArgumentOutOfRangeException(GetArgumentName(argument));

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetArgumentName(ExceptionArgument argument)
        {
            Debug.Assert(Enum.IsDefined(typeof(ExceptionArgument), argument),
                $"The enum value is not defined, please check the {nameof(ExceptionArgument)} enum.");

            return argument.ToString();
        }
    }

    internal enum ExceptionArgument
    {
        task,
        source,
        state
    }
}
