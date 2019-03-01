// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    internal static class ApplicationEnvironment
    {
        public static string ApplicationBasePath { get; } = GetApplicationBasePath();

        private static string GetApplicationBasePath()
        {
            string basePath =
#if NET451
                (string)AppDomain.CurrentDomain.GetData("APP_CONTEXT_BASE_DIRECTORY") ??
                AppDomain.CurrentDomain.BaseDirectory;
#else
                AppContext.BaseDirectory;
#endif
            return Path.GetFullPath(basePath);
        }
    }
}
