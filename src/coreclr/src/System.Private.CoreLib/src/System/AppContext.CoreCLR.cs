// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection;

namespace System
{
    public static partial class AppContext
    {
        internal static unsafe void Setup(char** pNames, char** pValues, int count)
        {
            for (int i = 0; i < count; i++)
            {
                s_dataStore.Add(new string(pNames[i]), new string(pValues[i]));
            }
        }

        private static string GetBaseDirectoryCore()
        {
            // Fallback path for hosts that do not set APP_CONTEXT_BASE_DIRECTORY explicitly
            string? directory = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            if (directory != null && !Path.EndsInDirectorySeparator(directory))
                directory += PathInternal.DirectorySeparatorCharAsString;
            return directory ?? string.Empty;
        }
    }
}
