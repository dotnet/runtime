// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.PlatformAbstractions
{
    public static class ApplicationEnvironment
    {
        public static string ApplicationBasePath { get; } = GetApplicationBasePath();

        private static string GetApplicationBasePath()
        {
            var basePath =
#if NET45
                (string)AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") ??
                AppDomain.CurrentDomain.BaseDirectory;
#else
                AppContext.BaseDirectory;
#endif
            return Path.GetFullPath(basePath);
        }
    }
}
