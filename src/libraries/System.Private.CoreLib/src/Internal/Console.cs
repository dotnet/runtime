// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

namespace Internal
{
    //
    // Simple limited console class for internal printf-style debugging in System.Private.CoreLib
    // and low-level tests that want to call System.Private.CoreLib directly
    //

    public static partial class Console
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine(string? s) =>
            Write(s + Environment.NewLineConst);

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static void WriteLine() =>
            Write(Environment.NewLineConst);

        public static partial class Error
        {
            [MethodImplAttribute(MethodImplOptions.NoInlining)]
            public static void WriteLine() =>
                Write(Environment.NewLineConst);
        }
    }
}
