// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System
{
    //
    // This pattern of easily inlinable "void Throw" routines that stack on top of NoInlining factory methods
    // is a compromise between older JITs and newer JITs (RyuJIT in .NET Core 1.1.0+ and .NET Framework in 4.6.3+).
    // This package is explicitly targeted at older JITs as newer runtimes expect to implement Span intrinsically for
    // best performance.
    //
    // The aim of this pattern is three-fold
    // 1. Extracting the throw makes the method preforming the throw in a conditional branch smaller and more inlinable
    // 2. Extracting the throw from generic method to non-generic method reduces the repeated codegen size for value types
    // 3a. Newer JITs will not inline the methods that only throw and also recognise them, move the call to cold section
    //     and not add stack prep and unwind before calling https://github.com/dotnet/coreclr/pull/6103
    // 3b. Older JITs will inline the throw itself and move to cold section; but not inline the non-inlinable exception
    //     factory methods - still maintaining advantages 1 & 2
    //

    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgument_DestinationTooShort() => throw new ArgumentException(SR.Argument_DestinationTooShort);

        [DoesNotReturn]
        public static void ThrowArgument_SpansMustHaveSameLength(string paramName1, string paramName2)
            => throw new ArgumentException(SR.Format(SR.Argument_SpansMustHaveSameLength, paramName1, paramName2), paramName1);
    }
}
