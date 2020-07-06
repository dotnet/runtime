// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;

namespace System
{
    public static partial class AppContext
    {
        private static string GetBaseDirectoryCore()
        {
            // Fallback path for hosts that do not set APP_CONTEXT_BASE_DIRECTORY explicitly
            string? directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

            if (directory == null)
                return string.Empty;

            if (!Path.EndsInDirectorySeparator(directory))
                directory += PathInternal.DirectorySeparatorCharAsString;

            return directory;
        }
    }
}