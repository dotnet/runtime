// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace Microsoft.NETCore.Platforms.BuildTasks
{
    public static class Extensions
    {
        public static string GetString(this ITaskItem taskItem, string metadataName)
        {
            var metadataValue = taskItem.GetMetadata(metadataName)?.Trim();
            return string.IsNullOrEmpty(metadataValue) ? null : metadataValue;
        }

        public static bool GetBoolean(this ITaskItem taskItem, string metadataName, bool defaultValue = false)
        {
            bool result = false;
            var metadataValue = taskItem.GetMetadata(metadataName);
            if (!bool.TryParse(metadataValue, out result))
            {
                result = defaultValue;
            }
            return result;
        }

        public static IEnumerable<string> GetStrings(this ITaskItem taskItem, string metadataName)
        {
            var metadataValue = taskItem.GetMetadata(metadataName)?.Trim();
            if (!string.IsNullOrEmpty(metadataValue))
            {
                return metadataValue.Split(';').Where(v => !string.IsNullOrEmpty(v.Trim())).ToArray();
            }

            return Enumerable.Empty<string>();
        }

        public static IEnumerable<T> NullAsEmpty<T>(this IEnumerable<T> source)
        {
            return source ?? Enumerable.Empty<T>();
        }

    }
}
