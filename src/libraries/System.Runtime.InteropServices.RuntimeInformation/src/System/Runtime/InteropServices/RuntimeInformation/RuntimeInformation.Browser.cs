// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    public static partial class RuntimeInformation
    {
        internal static bool IsCurrentOSPlatform(string osPlatform) => osPlatform.Equals("BROWSER", StringComparison.OrdinalIgnoreCase);

        public static string OSDescription => "Browser";

        public static Architecture OSArchitecture => Architecture.Wasm;

        public static Architecture ProcessArchitecture => Architecture.Wasm;
    }
}
