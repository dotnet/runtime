// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace Microsoft.DotNet.TestUtils
{
    public static class GlobalJson
    {
        public static string CreateEmpty(string directory)
            => Write(directory, "{}");

        public static string CreateWithVersion(string directory, string version)
            => Write(directory, @$"{{ ""sdk"": {{ ""version"": ""{version}"" }} }}");

        public static string CreateWithVersionSettings(string directory, string version = null, string policy = null, bool? allowPrerelease = null)
            => Write(directory, FormatVersionSettings(version, policy, allowPrerelease));

        public static string FormatVersionSettings(string version = null, string policy = null, bool? allowPrerelease = null)
            => $$"""
                {
                    "sdk": {
                        "version": {{(version != null ? $"\"{version}\"" : "null")}},
                        "rollForward": {{(policy != null ? $"\"{policy}\"" : "null")}},
                        "allowPrerelease": {{(allowPrerelease.HasValue ? $"{allowPrerelease.Value.ToString().ToLowerInvariant()}" : "null")}}
                    }
                }
                """;

        public static string Write(string directory, string contents)
        {
            string file = Path.Combine(directory, "global.json");
            File.WriteAllText(file, contents);
            return file;
        }

    }
}
