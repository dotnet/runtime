// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public static partial class AppContext
    {
        private static string GetBaseDirectoryCore()
        {
            // GetEntryAssembly().Location returns an empty string for wasm
            // Until that can be easily changed, work around the problem here.
            return "/";
        }
    }
}
