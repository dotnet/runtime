// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace Internal
{
    // Some CoreCLR tests use it for internal printf-style debugging in System.Private.CoreLib
    public static class Console
    {
        public static void Write(string? s) => DebugProvider.WriteCore(s ?? string.Empty);

        public static void WriteLine(string? s) => Write(s + Environment.NewLineConst);

        public static void WriteLine() => Write(Environment.NewLineConst);
    }
}
