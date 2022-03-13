// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.IO
{
    public static partial class Path
    {
        private static string? s_defaultTempPath;

        private static string DefaultTempPath =>
            s_defaultTempPath ?? (s_defaultTempPath = Interop.Sys.SearchPathTempDirectory()) ??
            throw new InvalidOperationException();
    }
}
