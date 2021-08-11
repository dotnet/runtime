// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        private static string? s_osDescription;

        public static string OSDescription => s_osDescription ??= Interop.Sys.GetUnixVersion();

        public static Architecture OSArchitecture { get; } = ProcessArchitecture;
    }
}
